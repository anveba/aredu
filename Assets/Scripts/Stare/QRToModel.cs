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
            UserInterface.Instance.ReportScanFinished("QR code contents is empty.");
            return;
        }

        if (_alreadyImportedCodes.Contains(result.Text))
        {
            UserInterface.Instance.ReportScanFinished("QR code already scanned.");
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
                UserInterface.Instance.ReportScanFinished("Web request error: " + request.error);
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
            UserInterface.Instance.ReportScanFinished("JSON parsing error: " + e.Message);
            return;
        }

        Debug.Log("Bundle " + modelBundle.Name + " has been read. JSON:\n" + json);

        string error;
        if (!modelBundle.IsValid(out error))
        {
            UserInterface.Instance.ReportScanFinished(error);
            return;
        }

        UserInterface.Instance.ReportImportStarted();

        GameObject model;
        (model, error) = await _modelRetriever.Retrieve(modelBundle);
        if (error == null)
        {
            _alreadyImportedCodes.Add(result.Text);
            _modelPlacer.SetModelPlacement(model.transform, Array.AsReadOnly(modelBundle.TagPlacements));
        }

        UserInterface.Instance.ReportScanFinished(error == null ? "Done!" : error);
    }
}