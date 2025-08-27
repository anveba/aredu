using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ZXing;

public class QRToModel : MonoBehaviour
{

    [SerializeField] private QRCodeReader _qrReader;
    [SerializeField] private ModelRetriever _modelRetriever;
    [SerializeField] private ModelPlacer _modelPlacer;
    private HashSet<string> _alreadyImportedCodes;

    private void Start()
    {
        _alreadyImportedCodes = new HashSet<string>();
    }

    private void OnEnable()
    {
        _qrReader.CodeDetected += QRCodeDetected;
    }

    private void OnDisable()
    {
        _qrReader.CodeDetected -= QRCodeDetected;
    }

    private async void QRCodeDetected(Result result)
    {
        if (result.Text == null || result.Text.Length == 0)
        {
            QRScanningUI.Instance.ReportScanFinished("QR code contents is empty.", isError: true);
            return;
        }

        if (_alreadyImportedCodes.Contains(result.Text))
        {
            QRScanningUI.Instance.ReportScanFinished("QR code already scanned.", isError: true);
            return;
        }

        string json;
        if (result.Text[0] == '{')
        {
            json = result.Text;
        }
        else
        {
            UnityWebRequest request = UnityWebRequest.Get(result.Text);
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                QRScanningUI.Instance.ReportScanFinished("Web request error: " + request.error, isError: true);
                return;
            }
            json = request.downloadHandler.text;
        }

        ModelBundle modelBundle;
        try
        {
            modelBundle = JsonUtility.FromJson<ModelBundle>(json);
        }
        catch (Exception e)
        {
            QRScanningUI.Instance.ReportScanFinished("JSON parsing error: " + e.Message, isError: true);
            return;
        }

        Debug.Log("Bundle " + modelBundle.Name + " has been read. JSON:\n" + json);

        string error;
        if (!modelBundle.IsValid(out error))
        {
            QRScanningUI.Instance.ReportScanFinished(error, isError: true);
            return;
        }

        QRScanningUI.Instance.ReportImportStarted();

        GameObject model;
        (model, error) = await _modelRetriever.Retrieve(modelBundle);
        if (error == null)
        {
            _alreadyImportedCodes.Add(result.Text);
            _modelPlacer.SetModelPlacement(model.transform, modelBundle.TagPlacements);
        }

        QRScanningUI.Instance.ReportScanFinished(error == null ? "Done!" : error, isError: false);
    }
}