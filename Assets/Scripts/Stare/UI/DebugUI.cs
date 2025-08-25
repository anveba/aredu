using UnityEngine;

public class DebugUI : MonoBehaviour
{
    public static DebugUI Instance { get; private set; }
    [SerializeField] private GameObject _debugDisplay;

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
        SetActive(Settings.Current.EnableDebug);
    }

    public void SetActive(bool active)
    {
        _debugDisplay.SetActive(active);
    }
}