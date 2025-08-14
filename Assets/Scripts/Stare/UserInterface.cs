using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserInterface : MonoBehaviour
{
    public static UserInterface Instance { get; private set; }
    [SerializeField] private QRCodeReader _qrReader;
    [SerializeField] private QRToModel _qrToModel;
    [SerializeField] private TextMeshProUGUI _qrScanText;
    [SerializeField] private Button _qrScanButton;
    [SerializeField] private Button _qrStopScanButton;

    public void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Several UserInterface instances exist! There should only be one.");
            return;
        }
        Instance = this;
    }

    public void StartQRScanning()
    {
        _qrReader.gameObject.SetActive(true);
        _qrToModel.gameObject.SetActive(true);
        _qrScanButton.gameObject.SetActive(false);
        _qrStopScanButton.gameObject.SetActive(true);
        _qrScanText.gameObject.SetActive(true);
        _qrScanText.text = "Looking for QR code...";
    }

    public void StopQRScanning()
    {
        _qrReader.gameObject.SetActive(false);
        _qrToModel.gameObject.SetActive(false);
        _qrStopScanButton.gameObject.SetActive(false);
        _qrScanButton.gameObject.SetActive(true);
        _qrScanText.gameObject.SetActive(false);
        _qrScanText.text = "";
    }

    public void ReportImportStarted()
    {
        _qrReader.gameObject.SetActive(false);
        _qrToModel.gameObject.SetActive(false);
        _qrStopScanButton.gameObject.SetActive(false);
        _qrScanText.text = "Importing...";
    }

    public void ReportScanFinished(string message)
    {
        _qrReader.gameObject.SetActive(false);
        _qrToModel.gameObject.SetActive(false);
        _qrStopScanButton.gameObject.SetActive(false);
        _qrScanText.text = message;
        StartCoroutine(ResetScanAfterTime(2.0f));
    }

    private IEnumerator ResetScanAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        _qrScanButton.gameObject.SetActive(true);
        _qrScanText.gameObject.SetActive(false);
    }
}
