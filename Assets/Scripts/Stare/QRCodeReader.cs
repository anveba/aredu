using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

public class QRCodeReader : MonoBehaviour {

    [SerializeField] private ARCameraSource _cameraSource;
    [SerializeField] private float _scanWidthFraction = 0.5f;
    private Task<Result> _decoderTask;
    private Texture2D _cameraTexture;
    [SerializeField] private RawImage _debug;

    private IBarcodeReader _barcodeReader = new BarcodeReader
    {
        AutoRotate = false,
        Options = new ZXing.Common.DecodingOptions
        {
            TryHarder = false
        }
    };

    public event Action<Result> CodeDetected;

    private void Start()
    {
        List<BarcodeFormat> formats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
        _barcodeReader = new BarcodeReader
        {
            AutoRotate = false,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = false,
                PossibleFormats = formats,
            }
        };
    }

    private void OnDestroy()
    {
        _decoderTask?.Wait();
    }

    private void OnEnable()
    {
        _cameraSource.FrameReceived += NewFrame;
    }

    private void OnDisable()
    {
        _cameraSource.FrameReceived -= NewFrame;
    }

    private void Update()
    {
        if (_decoderTask != null && _decoderTask.IsCompleted)
        {
            Result result = _decoderTask.Result;
            _decoderTask = null;
            if (result != null)
                CodeDetected?.Invoke(result);
        }   
    }

    private void NewFrame(ARCameraSource.Frame frame)
    {
        if (_decoderTask == null)
        {
            int size = Mathf.RoundToInt(Mathf.Min(frame.Width, frame.Height) * _scanWidthFraction);
            if (size <= 0)
            {
                Debug.LogError("QR scan size was non-positive.");
                return;
            }

            QRScanningUI.Instance.SetQRScanMaskSize(size);

            Color32[] trimmed = new Color32[size * size];
            unsafe
            {
                fixed (Color32* sourcePtr = frame.ImageBuffer)
                {
                    fixed (Color32* destPtr = trimmed)
                    {
                        for (int y = 0; y < size; y++)
                        {
                            int srcOffset = frame.Width * (frame.Height / 2 - size / 2 + y) + (frame.Width - size) / 2;
                            int dstOffset = size * y;
                            Buffer.MemoryCopy(sourcePtr + srcOffset, destPtr + dstOffset, size * sizeof(Color32), size * sizeof(Color32));
                        }
                    }
                }
            }

            if (_cameraTexture == null || _cameraTexture.width != frame.Width || _cameraTexture.height != frame.Height)
            {
                if (_cameraTexture != null)
                    Destroy(_cameraTexture);
                _cameraTexture = new Texture2D(size, size);
            }
            _cameraTexture.SetPixels32(trimmed);
            _cameraTexture.Apply();
            _debug.texture = _cameraTexture;

            // TODO move to its own thread in a loop. It may be idling at some times when using a task like this.
            _decoderTask = Task.Run(() => _barcodeReader.Decode(trimmed, size, size));
        }
    }
}