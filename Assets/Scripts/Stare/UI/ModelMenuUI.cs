using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ModelMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _headsUpDisplay;
    [SerializeField] private GameObject _modelMenuRoot;
    [SerializeField] private ModelRetriever _modelRetriever;
    [SerializeField] private GameObject _modelButtonPrefab;
    [SerializeField] private float _modelButtonSpacing = 64.0f;
    [SerializeField] private GameObject _modelListRoot;
    [SerializeField] private GameObject _modelListContent;
    [SerializeField] private GameObject _modelEditMenu;
    [SerializeField] private Slider _enableModelSlider;
    [SerializeField] private Slider _adjustModelPositionSlider;
    [SerializeField] private Slider _adjustModelRotationSlider;
    [SerializeField] private GameObject _rayInteractor;
    private List<GameObject> _modelButtons;
    private LoadedModel[] _listedModels;
    private int _selectedModel = -1;

    private void Start()
    {
        _modelButtons = new List<GameObject>();
    }

    public void OpenModelList()
    {
        _listedModels = _modelRetriever.GetLoadedModels().ToArray();

        RectTransform contentTransform = _modelListContent.GetComponent<RectTransform>();
        contentTransform.sizeDelta = new Vector2(contentTransform.sizeDelta.x, _listedModels.Length * _modelButtonSpacing);

        for (int i = 0; i < _listedModels.Length; i++)
        {
            GameObject m = Instantiate(_modelButtonPrefab);
            m.transform.SetParent(_modelListContent.transform, worldPositionStays: false);
            m.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, -i * _modelButtonSpacing);
            int local_i = i;
            m.GetComponent<Button>().onClick.AddListener(delegate { ModelButtonClicked(local_i); });
            m.GetComponentInChildren<TextMeshProUGUI>().text = _listedModels[i].ModelBundle.Name;
            _modelButtons.Add(m);
        }

        _modelMenuRoot.SetActive(true);
        _modelListRoot.SetActive(true);
        _rayInteractor.SetActive(false);
        _headsUpDisplay.SetActive(false);
    }

    private void ModelButtonClicked(int i)
    {
        _selectedModel = i;
        _modelEditMenu.SetActive(true);
        _modelListRoot.SetActive(false);
        _modelEditMenu.SetActive(true);
    }

    public void CloseModelList()
    {
        foreach (GameObject m in _modelButtons)
            Destroy(m);

        _modelButtons.Clear();
        _listedModels = null;
        _selectedModel = -1;

        _modelMenuRoot.SetActive(false);
        _modelListRoot.SetActive(false);
        _modelEditMenu.SetActive(false);
        _rayInteractor.SetActive(true);
        _headsUpDisplay.SetActive(true);
    }

    public void EnableModel()
    {
        bool currentValue = _enableModelSlider.value > 0.5f;
        _enableModelSlider.value = !currentValue == false ? 0.0f : 1.0f;
        _listedModels[_selectedModel].GameObject.SetActive(!currentValue);
    }

    public void AdjustModelPosition()
    {
        bool currentValue = _adjustModelPositionSlider.value > 0.5f;
        _adjustModelPositionSlider.value = !currentValue == false ? 0.0f : 1.0f;
        _listedModels[_selectedModel].AdjustmentHandle.GetComponent<XRGrabInteractable>().trackPosition = !currentValue;
        _listedModels[_selectedModel].AdjustmentHandle.SetActive(!currentValue || _adjustModelRotationSlider.value > 0.5f);
    }

    public void AdjustModelRotation()
    {
        bool currentValue = _adjustModelRotationSlider.value > 0.5f;
        _adjustModelRotationSlider.value = !currentValue == false ? 0.0f : 1.0f;
        _listedModels[_selectedModel].AdjustmentHandle.GetComponent<XRGrabInteractable>().trackRotation = !currentValue;
        _listedModels[_selectedModel].AdjustmentHandle.SetActive(!currentValue || _adjustModelPositionSlider.value > 0.5f);
    }

    public void CopyJSON()
    {
        string json = JsonUtility.ToJson(_listedModels[_selectedModel].ModelBundle);
        GUIUtility.systemCopyBuffer = json;
        Debug.Log("JSON copied:\n" + json);
    }
}
