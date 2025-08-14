using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using ZXing;

public class QRCodeReader : MonoBehaviour {

    [SerializeField] private ARCameraSource _cameraSource;
    private Task<Result> _decoderTask;

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
            _decoderTask = Task.Run(() => _barcodeReader.Decode(MemoryMarshal.Cast<Color32, Color32>(frame.ImageBuffer).ToArray(), frame.Width, frame.Height));
    }
}