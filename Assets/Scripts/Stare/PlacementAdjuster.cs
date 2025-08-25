using UnityEngine;

public class PlacementAdjuster : MonoBehaviour
{
    [SerializeField] private Transform _handleTransform;
    private Transform _modelTransform;
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private ModelBundle _modelBundle;
    private bool _isSelected = false;
    private int _referenceTag;

    public void SetModel(ModelBundle modelBundle, Transform modelTransform, Transform handleTransform, int referenceTag)
    {
        _modelBundle = modelBundle;
        _modelTransform = modelTransform;
        _handleTransform = handleTransform;
        _referenceTag = referenceTag;

        _lastPosition = _handleTransform.position;
        _lastRotation = _handleTransform.rotation;
    }

    void Update()
    {
        if (_isSelected && _modelBundle != null)
        {
            Vector3 deltaPosition = _handleTransform.position - _lastPosition;
            Quaternion deltaRotation = _handleTransform.rotation * Quaternion.Inverse(_lastRotation);

            foreach (TagPlacement tagPlacement in _modelBundle.TagPlacements)
            {
                tagPlacement.Position -= Quaternion.Inverse(Quaternion.Euler(tagPlacement.Rotation)) * Quaternion.Inverse(_modelTransform.localRotation) * deltaPosition;
                tagPlacement.Rotation = (Quaternion.Inverse(_modelTransform.localRotation) * Quaternion.Inverse(deltaRotation) * _modelTransform.localRotation * Quaternion.Euler(tagPlacement.Rotation)).eulerAngles;
            }

            _lastPosition = _handleTransform.position;
            _lastRotation = _handleTransform.rotation;
        }
        else
        {
            _handleTransform.rotation = _modelTransform.rotation * Quaternion.Euler(_modelBundle.TagPlacements[_referenceTag].Rotation);
            _handleTransform.position = _modelTransform.position + _handleTransform.rotation * _modelBundle.TagPlacements[_referenceTag].Position;

            _lastPosition = _handleTransform.position;
            _lastRotation = _handleTransform.rotation;
        }
    }

    public void Selected()
    {
        _isSelected = true;
        if (_modelBundle != null)
            PlacementAdjustmentUI.Instance.BumpModel(_modelBundle);
    }

    public void Deselected()
    {
        _isSelected = false;
    }
}
