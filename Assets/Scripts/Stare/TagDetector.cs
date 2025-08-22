using UnityEngine;
using System.Linq;
using TMPro;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

public class TagDetector : MonoBehaviour
{
    [SerializeField] private ARCameraSource _source = null;
    [SerializeField] private Material _tagMaterial = null;
    [SerializeField] private TextMeshProUGUI _debugText = null;
    private AprilTag.TagDetector _detector;
    private TagVisualiser _visualiser;
    private int _decimation = -1;
    private Vector2Int _dimensions;
    private class CameraStatus
    {
        public float Timestamp;
        public float FOV;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private const int MaxCameraStatuses = 8;
    private Queue<CameraStatus> _cameraStatuses;
    

    private class DetectionParams
    {
        public AprilTag.TagDetector Detector;
        public ARCameraSource.Frame Frame;
        public float TagSize;
        public float FOV;
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
        public bool IsHandHeld;
        public float Time;
        public int FrameCount;
    }
    private DetectionParams _nextDetectionRequest;
    private ConcurrentQueue<DetectionResult> _detectionResults;
    private ConcurrentBag<AprilTag.TagDetector> _detectorsToDispose;
    private volatile bool _stopDetection;
    private Thread _detectionThread;
    private static ManualResetEvent _detectionThreadSignaler = new ManualResetEvent(false);

    private class DetectionResult
    {
        public IReadOnlyCollection<Detection> Detections;
        public string DebugInfo;
    }

    public class Detection
    {
        public int TagID { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public float Timestamp { get; private set; }
        public int FrameNumber { get; private set; }
        public Detection(int id, Vector3 position, Quaternion rotation, float timestamp, int frameNumber)
        {
            TagID = id;
            Position = position;
            Rotation = rotation;
            Timestamp = timestamp;
            FrameNumber = frameNumber;
        }
    }
    public event Action<IReadOnlyCollection<Detection>> TagsDetected;

    void Start()
    {
        _visualiser = new TagVisualiser(_tagMaterial);
        _cameraStatuses = new Queue<CameraStatus>();
        _detectionResults = new ConcurrentQueue<DetectionResult>();
        _detectorsToDispose = new ConcurrentBag<AprilTag.TagDetector>();
        _stopDetection = false;

        _detectionThread = new Thread(DetectionLoop);
        _detectionThread.Start();
    }

    private void OnDestroy()
    {
        _stopDetection = true;
        _detectionThreadSignaler.Set();
        _detectionThread.Join();

        foreach (AprilTag.TagDetector d in _detectorsToDispose)
            d.Dispose();

        _visualiser.Dispose();
        _detector?.Dispose();
    }

    private void OnEnable()
    {
        _source.FrameReceived += NewFrame;
    }

    private void OnDisable()
    {
        _source.FrameReceived -= NewFrame;
    }

    private void LateUpdate()
    {
        AppendCurrentCameraStatus();

        DetectionResult result;
        while (_detectionResults.TryDequeue(out result))
        {
            if (Settings.Current.EnableDebug)
            {
                foreach (Detection d in result.Detections)
                    _visualiser.Draw(d.Position, d.Rotation, Settings.Current.TagSize);
            }
            if (result.DebugInfo != null)
                _debugText.text = result.DebugInfo;

            if (result.Detections.Count > 0)
                TagsDetected?.Invoke(result.Detections);
        }
    }

    private void AppendCurrentCameraStatus()
    {
        if (_cameraStatuses.Count() == 0 || _cameraStatuses.Last().Timestamp < Time.time)
        {
            while (_cameraStatuses.Count() > MaxCameraStatuses)
                _cameraStatuses.Dequeue();

            _cameraStatuses.Enqueue(new CameraStatus()
            {
                Timestamp = Time.time,
                FOV = Camera.main.fieldOfView * Mathf.Deg2Rad,
                Position = Camera.main.transform.position,
                Rotation = Camera.main.transform.rotation,
            });
        }
    }

    private void NewFrame(ARCameraSource.Frame frame)
    {
        AppendCurrentCameraStatus();

        if (_cameraStatuses.Count() == 0)
            return;

        if (_detector == null || _dimensions.x != frame.Width || _dimensions.y != frame.Height || Settings.Current.Decimation != _decimation)
        {
            if (_detector != null)
                _detectorsToDispose.Add(_detector);
            _dimensions = new Vector2Int(frame.Width, frame.Height);
            _detector = new AprilTag.TagDetector(_dimensions.x, _dimensions.y, Settings.Current.Decimation);
            _decimation = Settings.Current.Decimation;
        }

        if (frame.ImageBuffer.IsEmpty)
            return;

        GetCameraStatusDuringFrame(frame, out Vector3 cameraPosition, out Quaternion cameraRotation, out float fov);

        bool isHandHeld = SystemInfo.deviceType == DeviceType.Handheld;

        fov = frame.FOV.GetValueOrDefault(fov); // These FOVs should be and seemingly are the same.
        if (isHandHeld)
            fov = (fov / frame.Width) * frame.Height; // 'Flip' the FOV axis/dimension

        DetectionParams p = new DetectionParams()
        {
            Detector = _detector,
            Frame = frame,
            TagSize = Settings.Current.TagSize,
            FOV = fov,
            CameraPosition = cameraPosition,
            CameraRotation = cameraRotation,
            IsHandHeld = isHandHeld,
            Time = Time.time,
            FrameCount = Time.frameCount
        };
        Interlocked.Exchange(ref _nextDetectionRequest, p);
        _detectionThreadSignaler.Set();
    }

    private void GetCameraStatusDuringFrame(ARCameraSource.Frame frame, out Vector3 cameraPosition, out Quaternion rawCameraRotation, out float fov)
    {
        CameraStatus prevPopped = null;
        while (true)
        {
            CameraStatus popped = _cameraStatuses.Peek();
            if (popped.Timestamp >= frame.Timestamp || _cameraStatuses.Count() == 1)
            {
                if (prevPopped == null)
                {
                    cameraPosition = popped.Position;
                    rawCameraRotation = popped.Rotation;
                    fov = popped.FOV;
                }
                else
                {
                    float t = (float)(frame.Timestamp - prevPopped.Timestamp) / (popped.Timestamp - prevPopped.Timestamp);
                    cameraPosition = Vector3.Lerp(prevPopped.Position, popped.Position, t);
                    rawCameraRotation = Quaternion.Lerp(prevPopped.Rotation, popped.Rotation, t);
                    fov = Mathf.Lerp(prevPopped.FOV, popped.FOV, t);
                }
                break;
            }
            _cameraStatuses.Dequeue();
        }
    }

    private void DetectionLoop()
    {
        while (true)
        {
            _detectionThreadSignaler.WaitOne();
            _detectionThreadSignaler.Reset();
            if (_stopDetection)
                break;
            DetectionParams next = Interlocked.Exchange(ref _nextDetectionRequest, null);
            _detectionResults.Enqueue(Detect(next));
            foreach (AprilTag.TagDetector d in _detectorsToDispose)
                d.Dispose();
        }
    }

    private static DetectionResult Detect(DetectionParams p)
    {
        p.Detector.ProcessImage(p.Frame.ImageBuffer, p.FOV, p.TagSize);

        Detection[] detections = new Detection[p.Detector.DetectedTags.Count()];

        int i = 0;
        foreach (AprilTag.TagPose tag in p.Detector.DetectedTags)
        {
            Vector3 cameraForward = p.CameraRotation * Vector3.forward;
            Quaternion cameraRotation = p.IsHandHeld ? Quaternion.AngleAxis(90, cameraForward) : Quaternion.identity;
            cameraRotation *= p.CameraRotation;

            Vector3 position = p.CameraPosition + cameraRotation * tag.Position;
            Quaternion rotation = cameraRotation * tag.Rotation;

            detections[i++] = new Detection(tag.ID, position, rotation, p.Time, p.FrameCount);
        }

        string debugInfo = null;
        if (p.FrameCount % 30 == 0)
            debugInfo = p.Detector.ProfileData.Aggregate("AprilTag runtime (usec)", (c, n) => $"{c}\n{n.name}: {n.time}");

        return new DetectionResult() { Detections = detections, DebugInfo = debugInfo };
    }
}
