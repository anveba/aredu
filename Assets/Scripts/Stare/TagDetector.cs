using UnityEngine;
using System.Linq;
using TMPro;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

public class TagDetector : MonoBehaviour
{
    [SerializeField] private ARCameraSource _source = null;
    [SerializeField] private int _decimation = 4;
    [SerializeField] private float _tagSize = 0.05f;
    [SerializeField] private Material _tagMaterial = null;
    [SerializeField] private TextMeshProUGUI _debugText = null;
    [SerializeField] private bool _visualise = true;
    private Vector2Int _dimensions;
    private AprilTag.TagDetector _detector;
    private TagVisualiser _visualiser;
    private class CameraStatus
    {
        public float Timestamp;
        public float FOV;
        public Vector3 Position;
        public  Quaternion Rotation;
    }

    private const int MaxCameraStatuses = 8;
    private Queue<CameraStatus> _cameraStatuses;

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
    public event Action<ReadOnlyCollection<Detection>> TagsDetected;

    void Start()
    {
        _visualiser = new TagVisualiser(_tagMaterial);
        _cameraStatuses = new Queue<CameraStatus>();
    }

    private void OnDestroy()
    {
        _detector?.Dispose();
        _visualiser.Dispose();
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

        if (_detector == null || _dimensions.x != frame.Width || _dimensions.y != frame.Height)
        {
            _dimensions = new Vector2Int(frame.Width, frame.Height);
            _detector = new AprilTag.TagDetector(_dimensions.x, _dimensions.y, _decimation);
        }

        if (frame.ImageBuffer.IsEmpty)
            return;

        GetCameraStatusDuringFrame(frame, out Vector3 cameraPosition, out Quaternion rawCameraRotation, out float fov);
        
        fov = frame.FOV.GetValueOrDefault(fov); // These FOVs should be and seemingly are the same.
        if (SystemInfo.deviceType == DeviceType.Handheld)
            fov = (fov / frame.Width) * frame.Height; // 'Flip' the FOV axis/dimension

        _detector.ProcessImage(frame.ImageBuffer, fov, _tagSize);

        Detection[] detections = new Detection[_detector.DetectedTags.Count()];

        int i = 0;
        foreach (AprilTag.TagPose tag in _detector.DetectedTags)
        {
            Vector3 cameraForward = rawCameraRotation * Vector3.forward;
            Quaternion cameraRotation = SystemInfo.deviceType == DeviceType.Handheld ? Quaternion.AngleAxis(90, cameraForward) : Quaternion.identity;
            cameraRotation *= rawCameraRotation;

            Vector3 position = cameraPosition + cameraRotation * tag.Position;
            Quaternion rotation = cameraRotation * tag.Rotation;

            if (_visualise)
                _visualiser.Draw(position, rotation, _tagSize);

            detections[i++] = new Detection(tag.ID, position, rotation, Time.time, Time.frameCount);
        }

        if (Time.frameCount % 30 == 0)
            _debugText.text = _detector.ProfileData.Aggregate("AprilTag runtime (usec)", (c, n) => $"{c}\n{n.name}: {n.time}");

        TagsDetected?.Invoke(Array.AsReadOnly(detections));
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
}
