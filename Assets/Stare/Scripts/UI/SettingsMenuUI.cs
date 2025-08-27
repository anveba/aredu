using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _headsUpDisplay;
    [SerializeField] private GameObject _settingsMenuRoot;

    [SerializeField] private TMP_InputField _tagSizeInput;
    [SerializeField] private Slider _decimationSlider;
    [SerializeField] private TextMeshProUGUI _decimationValueText;
    [SerializeField] private TMP_InputField _downscaleInput;
    [SerializeField] private Slider _smoothingSlider;
    [SerializeField] private TextMeshProUGUI _smoothingText;
    [SerializeField] private Slider _debugInfoSlider;

    public void Open()
    {
        _headsUpDisplay.SetActive(false);
        _settingsMenuRoot.SetActive(true);

        PopulateWithActualValues();
    }

    private void PopulateWithActualValues()
    {
        _tagSizeInput.text = (Settings.Current.TagSize * 100.0f).ToString();

        _decimationSlider.value = Settings.Current.Decimation;
        _decimationValueText.text = _decimationSlider.value.ToString();

        _downscaleInput.text = FormatFloat(Settings.Current.Downscaling);

        _smoothingSlider.value = Settings.Current.Smoothing;
        _smoothingText.text = FormatFloat(_smoothingSlider.value);

        _debugInfoSlider.value = Settings.Current.EnableDebug ? 1.0f : 0.0f;
    }

    public void Close()
    {
        _headsUpDisplay.SetActive(true);
        _settingsMenuRoot.SetActive(false);
        Settings.Current.SaveSettings();
    }

    public void ChangeTagSize()
    {
        if (_tagSizeInput.text == "")
            return;
        float value;
        try
        {
            value = float.Parse(_tagSizeInput.text);
        }
        catch
        {
            Debug.LogWarning("Float parse error: " + _tagSizeInput.text);
            return;
        }
        Settings.Current.TagSize = value / 100.0f;
        _tagSizeInput.text = FormatFloat(Settings.Current.TagSize * 100.0f);
    }

    public void ChangeDecimation()
    {
        Settings.Current.Decimation = Mathf.RoundToInt(_decimationSlider.value);
        _decimationValueText.text = _decimationSlider.value.ToString();
    }

    public void ChangeResolutionDownscale()
    {
        if (_downscaleInput.text == "")
            return;
        float value;
        try
        {
            value = float.Parse(_downscaleInput.text);
        }
        catch
        {
            Debug.LogWarning("Float parse error: " + _downscaleInput.text);
            return;
        }
        Settings.Current.Downscaling = value;
        _downscaleInput.text = FormatFloat(Settings.Current.Downscaling);
    }

    public void ChangeSmoothing()
    {
        Settings.Current.Smoothing = _smoothingSlider.value;
        _smoothingText.text = FormatFloat(_smoothingSlider.value);
    }

    public void ToggleDebugInfo()
    {
        bool wasEnabled = _debugInfoSlider.value > 0.5f;
        _debugInfoSlider.value = !wasEnabled == false ? 0.0f : 1.0f;
        Settings.Current.EnableDebug = !wasEnabled;
        DebugUI.Instance.SetActive(!wasEnabled);
    }
    
    private static string FormatFloat(float f)
    {
        return string.Format("{0:0.000}", f);
    }
}
