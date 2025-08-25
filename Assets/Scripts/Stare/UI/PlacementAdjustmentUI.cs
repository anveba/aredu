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

    private const int ReferenceTag = 0;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Several instances exist! There should only be one.");
            return;
        }
        Instance = this;
    }

    public void SetModel(ModelBundle model, bool translation, bool rotation)
    {
        CurrentModel = model;
        _translationMenu.SetActive(translation);
        _rotationMenu.SetActive(rotation);
    }

    private void Update()
    {
        bool anyFocused = _translationXInput.isFocused || _translationYInput.isFocused || _translationZInput.isFocused ||
            _rotationXInput.isFocused || _rotationYInput.isFocused || _rotationZInput.isFocused;
        if (CurrentModel != null && !anyFocused)
        {
            TagPlacement tag = CurrentModel.TagPlacements[ReferenceTag];
            _translationXInput.text = FormatFloat(-tag.Position.x);
            _translationYInput.text = FormatFloat(-tag.Position.y);
            _translationZInput.text = FormatFloat(-tag.Position.z);
            _rotationXInput.text = FormatFloat(-tag.Rotation.x);
            _rotationYInput.text = FormatFloat(-tag.Rotation.y);
            _rotationZInput.text = FormatFloat(-tag.Rotation.z);
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
        if (field.text == "")
            return;
        float value;
        try
        {
            value = -float.Parse(field.text);
        }
        catch
        {
            Debug.LogWarning("Float parse error: " + field.text);
            return;
        }
        float delta = value - CurrentModel.TagPlacements[ReferenceTag].Position[axis];
        foreach (TagPlacement t in CurrentModel.TagPlacements)
            t.Position[axis] += delta;
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
        if (field.text == "")
            return;
        float value;
        try
        {
            value = -float.Parse(field.text);
        }
        catch
        {
            Debug.LogWarning("Float parse error: " + field.text);
            return;
        }
        float delta = value - CurrentModel.TagPlacements[ReferenceTag].Rotation[axis];
        foreach (TagPlacement t in CurrentModel.TagPlacements)
            t.Rotation[axis] += delta;
    }
    
    private static string FormatFloat(float f)
    {
        return string.Format("{0:0.000}", f);
    }
}
