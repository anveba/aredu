using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
    public static DebugUI Instance { get; private set; }
    [SerializeField] private GameObject _debugDisplay;
    [SerializeField] private RawImage _debugImage;

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

    public void SetCameraDebugImage(Texture2D texture)
    {
        _debugImage.texture = texture;
    }
}