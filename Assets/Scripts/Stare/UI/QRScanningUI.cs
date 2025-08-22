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
    [SerializeField] private RectTransform _qrMaskTransform;
    [SerializeField] private CanvasScaler _canvasScaler;
    private Color32 _neutralTextColour = new Color32(255, 255, 255, 255);
    private Color32 _errorTextColour = new Color32(255, 64, 64, 255);

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Several UserInterface instances exist! There should only be one.");
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
        _qrScanText.text = "Looking for QR code...";
    }

    public void StopQRScanning()
    {
        _qrReader.gameObject.SetActive(false);
        _qrToModel.gameObject.SetActive(false);
        _headsUpDisplay.gameObject.SetActive(true);
        _qrScanMenuRoot.gameObject.SetActive(false);
        _qrStopScanButton.gameObject.SetActive(false);
        _qrScanText.text = "";
    }

    public void ReportImportStarted()
    {
        _qrReader.gameObject.SetActive(false);
        _qrToModel.gameObject.SetActive(false);
        _qrStopScanButton.gameObject.SetActive(false);
        _qrScanText.text = "Importing...";
    }

    public void ReportScanFinished(string message, bool isError)
    {
        _qrReader.gameObject.SetActive(false);
        _qrToModel.gameObject.SetActive(false);
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

    public void SetQRScanMaskSize(float size)
    {
        // The square sometimes won't align with the scanning area. This is due to the acquired image and the image shown
        // on screen have different FOVs for some reason that I suppose we just have to live with. 
        _qrMaskTransform.sizeDelta = new Vector2(size, size) * (_canvasScaler.referenceResolution.x / Screen.width);
    }
}
