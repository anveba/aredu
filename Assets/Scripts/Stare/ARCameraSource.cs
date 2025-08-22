using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARCameraSource : MonoBehaviour
{

    [SerializeField] private ARCameraManager _cameraManager;
    [SerializeField] private RawImage _cameraPreview;
    [SerializeField] private bool _createTexture = false;
    public XRCpuImage.Transformation Transformation { get; private set; }
    private bool _configurationIsSet = false;

    public class Frame
    {
        public double Timestamp { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float? FOV { get; private set; }
        private Texture2D _cameraTexture;
        public Texture2D Texture => _cameraTexture;
        private NativeArray<byte> _nativeImageBuffer;
        public ReadOnlySpan<Color32> ImageBuffer => _nativeImageBuffer.Reinterpret<Color32>(1).AsReadOnlySpan();

        public Frame(double timestamp, int width, int height, float? fov, NativeArray<byte> nativeImageBuffer, Texture2D texture)
        {
            Timestamp = timestamp;
            Width = width;
            Height = height;
            FOV = fov;
            _nativeImageBuffer = nativeImageBuffer;
            _cameraTexture = texture;
        }
    }

    private Texture2D _cameraTexture;
    private NativeArray<byte> _nativeImageBuffer;

    public event Action<Frame> FrameReceived;

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Transformation = SystemInfo.deviceType == DeviceType.Handheld ? XRCpuImage.Transformation.MirrorY : XRCpuImage.Transformation.None;
    }

    private void OnEnable()
    {
        _cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        _cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnDestroy()
    {
        if (_nativeImageBuffer != null)
            _nativeImageBuffer.Dispose();
        if (_cameraTexture != null)
            Destroy(_cameraTexture);
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!_configurationIsSet)
        {
            UpdateConfiguration();
            _configurationIsSet = true;
        }

        if (!_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;

        XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int((int)(image.width / Settings.Current.Downscaling), (int)(image.height / Settings.Current.Downscaling)),
            outputFormat = TextureFormat.RGBA32,
            transformation = Transformation
        };

        int size = image.GetConvertedDataSize(conversionParams);

        if (_nativeImageBuffer == null || _nativeImageBuffer.Length != size)
        {
            // TODO disposing here may cause invalid memory accesses
            if (_nativeImageBuffer != null)
                _nativeImageBuffer.Dispose();
            _nativeImageBuffer = new NativeArray<byte>(size, Allocator.Persistent);
        }

        image.Convert(conversionParams, _nativeImageBuffer);

        if (_createTexture)
        {
            if (_cameraTexture == null || _cameraTexture.width != image.width || _cameraTexture.height != image.height)
            {
                if (_cameraTexture != null)
                    Destroy(_cameraTexture);
                _cameraTexture = new Texture2D(
                    conversionParams.outputDimensions.x,
                    conversionParams.outputDimensions.y,
                    conversionParams.outputFormat,
                    false);
            }
            _cameraTexture.LoadRawTextureData(_nativeImageBuffer);
            _cameraTexture.Apply();
        }
        else if (_cameraTexture != null)
        {
            Destroy(_cameraTexture);
        }

        float? fov = null;
        if (eventArgs.projectionMatrix.HasValue)
            fov = Mathf.Atan(1.0f / eventArgs.projectionMatrix.Value.m11) * 2.0f;

        Frame frame = new Frame(image.timestamp, conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, fov, _nativeImageBuffer, _cameraTexture);

        image.Dispose();

        if (_cameraPreview != null && _cameraTexture != null)
            _cameraPreview.texture = _cameraTexture;

        FrameReceived?.Invoke(frame);
    }

    private void UpdateConfiguration()
    {
        NativeArray<XRCameraConfiguration> configurations = _cameraManager.GetConfigurations(Allocator.Temp);
        if (configurations.Length == 0)
            return;

        XRCameraConfiguration? bestConfig = null;
        float bestScore = -Mathf.Infinity;

        foreach (XRCameraConfiguration config in configurations)
        {
            float score = config.width * config.height;
            if (score > bestScore)
                bestConfig = config;
        }

        Debug.Log($"Using camera configuration: {bestConfig.Value.width}x{bestConfig.Value.height}{(bestConfig.Value.framerate.HasValue ? $" at {bestConfig.Value.framerate.Value} Hz" : "")}{(bestConfig.Value.depthSensorSupported == Supported.Supported ? " depth sensor" : "")}");
        _cameraManager.currentConfiguration = bestConfig;
    }
}