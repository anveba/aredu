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
    private bool _configurationIsSet = false;

    public class Frame
    {
        public double Timestamp { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float? FOV { get; private set; }
        public Matrix4x4? DisplayMatrix;
        private Texture2D _cameraTexture;
        public Texture2D Texture => _cameraTexture;
        private ImageBuffer ImageBuffer;
        public ReadOnlySpan<Color32> ImageBufferSpan => ImageBuffer.NativeBuffer.Reinterpret<Color32>(1).AsReadOnlySpan();

        public Frame(double timestamp, int width, int height, float? fov, Matrix4x4? displayMatrix, ImageBuffer imageBuffer, Texture2D texture)
        {
            Timestamp = timestamp;
            Width = width;
            Height = height;
            FOV = fov;
            DisplayMatrix = displayMatrix;
            ImageBuffer = imageBuffer;
            _cameraTexture = texture;
        }
    }

    public class ImageBuffer
    {
        public NativeArray<byte> NativeBuffer { get; private set; }

        public ImageBuffer(NativeArray<byte> nativeBuffer)
        {
            NativeBuffer = nativeBuffer;
        }

        ~ImageBuffer()
        {
            NativeBuffer.Dispose();
        }
    }

    private Texture2D _cameraTexture;

    public event Action<Frame> FrameReceived;

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
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
            transformation = eventArgs.displayMatrix.HasValue ? XRCpuImage.Transformation.MirrorY : XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        ImageBuffer imageBuffer = new ImageBuffer(new NativeArray<byte>(size, Allocator.Persistent));
        image.Convert(conversionParams, imageBuffer.NativeBuffer);

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
            _cameraTexture.LoadRawTextureData(imageBuffer.NativeBuffer);
            _cameraTexture.Apply();
        }
        else if (_cameraTexture != null)
        {
            Destroy(_cameraTexture);
        }

        float? fov = null;
        if (eventArgs.projectionMatrix.HasValue)
            fov = Mathf.Atan(1.0f / eventArgs.projectionMatrix.Value.m11) * 2.0f;

        Frame frame = new Frame(image.timestamp, conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, fov, eventArgs.displayMatrix, imageBuffer, _cameraTexture);

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
        long bestScore = long.MinValue;

        foreach (XRCameraConfiguration config in configurations)
        {
            long score = config.width * config.height * config.framerate.GetValueOrDefault(1) + (config.depthSensorSupported == Supported.Supported ? 1 << 28 : 0);
            if (score > bestScore)
                bestConfig = config;
        }

        Debug.Log($"Using camera configuration: {bestConfig.Value.width}x{bestConfig.Value.height}{(bestConfig.Value.framerate.HasValue ? $" at {bestConfig.Value.framerate.Value} Hz" : "")}{(bestConfig.Value.depthSensorSupported == Supported.Supported ? " depth sensor" : "")}");
        _cameraManager.currentConfiguration = bestConfig;
    }
}