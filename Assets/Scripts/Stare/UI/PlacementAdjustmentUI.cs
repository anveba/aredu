using TMPro;
using UnityEngine;

public class PlacementAdjustmentUI : MonoBehaviour
{
    public static PlacementAdjustmentUI Instance { get; private set; }
    [SerializeField] private GameObject _translationMenu;
    [SerializeField] private TMP_InputField _translationXInput;
    [SerializeField] private TMP_InputField _translationYInput;
    [SerializeField] private TMP_InputField _translationZInput;
    [SerializeField] private GameObject _rotationMenu;
    [SerializeField] private TMP_InputField _rotationXInput;
    [SerializeField] private TMP_InputField _rotationYInput;
    [SerializeField] private TMP_InputField _rotationZInput;
    public ModelBundle CurrentModel { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Several instances exist! There should only be one.");
            return;
        }
        Instance = this;
    }

    public void BumpModel(ModelBundle model)
    {
        CurrentModel = model;
    }

    public void OpenTranslationMenu(ModelBundle model)
    {
        CurrentModel = model;
        _translationMenu.SetActive(true);
    }

    public void CloseTranslationMenu(ModelBundle model)
    {
        if (CurrentModel == model)
            _translationMenu.SetActive(false);
    }

    public void OpenRotationMenu(ModelBundle model)
    {
        CurrentModel = model;
        _rotationMenu.SetActive(true);
    }

    public void CloseRotationMenu(ModelBundle model)
    {
        if (CurrentModel == model)
            _rotationMenu.SetActive(false);
    }

    private void Update()
    {
        if (CurrentModel != null)
        {
            TagPlacement tag = CurrentModel.TagPlacements[0];
            _translationXInput.text = FormatFloat(tag.Position.x);
            _translationYInput.text = FormatFloat(tag.Position.y);
            _translationZInput.text = FormatFloat(tag.Position.z);
            _rotationXInput.text = FormatFloat(tag.Rotation.x);
            _rotationYInput.text = FormatFloat(tag.Rotation.y);
            _rotationZInput.text = FormatFloat(tag.Rotation.z);
        }
    }

    public void ChangeXTranslation()
    {
        ChangeTranslation(_translationXInput, 0);
    }

    public void ChangeYTranslation()
    {
        ChangeTranslation(_translationYInput, 1);
    }

    public void ChangeZTranslation()
    {
        ChangeTranslation(_translationZInput, 2);
    }

    private void ChangeTranslation(TMP_InputField field, int axis)
    {
        float value;
        try
        {
            value = float.Parse(field.text);
        }
        catch
        {
            Debug.LogError("Float parse error: " + field.text);
            return;
        }
        CurrentModel.TagPlacements[0].Position[axis] = value;
    }

    public void ChangeXRotation()
    {
        ChangeRotation(_rotationXInput, 0);
    }

    public void ChangeYRotation()
    {
        ChangeRotation(_rotationYInput, 1);
    }

    public void ChangeZRotation()
    {
        ChangeRotation(_rotationZInput, 2);
    }

    private void ChangeRotation(TMP_InputField field, int axis)
    {
        float value;
        try
        {
            value = float.Parse(field.text);
        }
        catch
        {
            Debug.LogError("Float parse error: " + field.text);
            return;
        }
        CurrentModel.TagPlacements[0].Rotation[axis] = value;
    }
    
    private static string FormatFloat(float f)
    {
        return string.Format("{0:0.000}", f);
    }
}
