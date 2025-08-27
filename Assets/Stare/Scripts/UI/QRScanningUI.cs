using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QRScanningUI : MonoBehaviour
{
    public static QRScanningUI Instance { get; private set; }
    [SerializeField] private GameObject _headsUpDisplay;
    [SerializeField] private GameObject _qrScanMenuRoot;
    [SerializeField] private Button _qrStopScanButton;
    [SerializeField] private QRCodeReader _qrReader;
    [SerializeField] private QRToModel _qrToModel;
    [SerializeField] private TextMeshProUGUI _qrScanText;
    [SerializeField] private GameObject _scanFocus;
    [SerializeField] private RectTransform _qrMaskTransform;
    [SerializeField] private CanvasScaler _canvasScaler;
    private Color32 _neutralTextColour = new Color32(255, 255, 255, 255);
    private Color32 _errorTextColour = new Color32(255, 64, 64, 255);

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Several instances exist! There should only be one.");
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _qrScanText.faceColor = _neutralTextColour;
    }

    public void StartQRScanning()
    {
        _qrReader.gameObject.SetActive(true);
        _qrToModel.gameObject.SetActive(true);
        _headsUpDisplay.gameObject.SetActive(false);
        _qrScanMenuRoot.gameObject.SetActive(true);
        _qrStopScanButton.gameObject.SetActive(true);
        _scanFocus.SetActive(true);
        _qrScanText.text = "Looking for QR code...";
    }

    public void StopQRScanning()
    {
        _qrReader.gameObject.SetActive(false);
        _qrToModel.gameObject.SetActive(false);
        _headsUpDisplay.gameObject.SetActive(true);
        _qrScanMenuRoot.gameObject.SetActive(false);
        _qrStopScanButton.gameObject.SetActive(false);
        _scanFocus.SetActive(false);
        _qrScanText.text = "";
    }

    public void ReportImportStarted()
    {
        _qrReader.gameObject.SetActive(false);
        _qrToModel.gameObject.SetActive(false);
        _qrStopScanButton.gameObject.SetActive(false);
        _scanFocus.SetActive(false);
        _qrScanText.text = "Importing...";
    }

    public void ReportScanFinished(string message, bool isError)
    {
        _qrReader.gameObject.SetActive(false);
        _qrToModel.gameObject.SetActive(false);
        _qrStopScanButton.gameObject.SetActive(false);
        _scanFocus.SetActive(false);
        _qrScanText.text = message;
        if (isError)
            _qrScanText.faceColor = _errorTextColour;
        StartCoroutine(ResetScanAfterTime(isError ? 5.0f : 2.5f));
    }

    private IEnumerator ResetScanAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        _headsUpDisplay.gameObject.SetActive(true);
        _qrScanMenuRoot.gameObject.SetActive(false);
        _qrScanText.faceColor = _neutralTextColour;
    }

    public void SetQRScanMaskSize(float widthFraction, Matrix4x4? displayMatrix)
    {
        // This might be a sketchy use of the display matrix
        float displayCorrection = displayMatrix.HasValue && displayMatrix.Value.m01 != 0.0f ? Mathf.Abs(displayMatrix.Value.m01) : 1.0f;

        float v = (_canvasScaler.referenceResolution.x * widthFraction) / Mathf.Abs(displayCorrection);
        _qrMaskTransform.sizeDelta = new Vector2(v, v);
    }
}
