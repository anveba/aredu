using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class ModelPlacer : MonoBehaviour
{
    [SerializeField] private TagDetector _detector = null;
    public bool EnableStaling = false;
    [SerializeField] private float _staleTime = 10.0f;
    [SerializeField] private int _maxAccumulatedDetections = 30;

    private Dictionary<int, Queue<TagDetector.Detection>> _accumulatedDetections;
    private Dictionary<Transform, ReadOnlyCollection<TagPlacement>> _associatedTagPlacements;
    private Dictionary<int, Tuple<Vector3, Quaternion>> _tagTransforms;
    

    private void Start()
    {
        _accumulatedDetections = new Dictionary<int, Queue<TagDetector.Detection>>();
        _associatedTagPlacements = new Dictionary<Transform, ReadOnlyCollection<TagPlacement>>();
        _tagTransforms = new Dictionary<int, Tuple<Vector3, Quaternion>>();
    }

    public void SetModelPlacement(Transform modelTransform, ReadOnlyCollection<TagPlacement> placements)
    {
        _associatedTagPlacements[modelTransform] = placements;
    }

    public void RemoveModelPlacement(Transform modelTransform)
    {
        _associatedTagPlacements.Remove(modelTransform);
    }

    private void OnEnable()
    {
        _detector.TagsDetected += TagsDetected;
    }

    private void OnDisable()
    {
        _detector.TagsDetected -= TagsDetected;
    }

    private void TagsDetected(IEnumerable<TagDetector.Detection> tags)
    {
        if (EnableStaling)
            foreach (var q in _accumulatedDetections.Values)
                while (q.Count > 0 && Time.time - q.Peek().Timestamp > _staleTime)
                    q.Dequeue();
        
        foreach (TagDetector.Detection tag in tags)
        {
            if (!_accumulatedDetections.ContainsKey(tag.TagID))
            {
                Queue<TagDetector.Detection> q = new Queue<TagDetector.Detection>();
                _accumulatedDetections.Add(tag.TagID, q);
                q.Enqueue(tag);
            }
            else
            {
                Queue<TagDetector.Detection> q = _accumulatedDetections[tag.TagID];
                while (q.Count > _maxAccumulatedDetections)
                    q.Dequeue();
                q.Enqueue(tag);
            }
        }

        UpdateTagTransforms();
        UpdateModelTransforms();
    }

    private void UpdateTagTransforms()
    {
        List<Vector3> positions = new List<Vector3>(_maxAccumulatedDetections);
        List<Quaternion> rotations = new List<Quaternion>(_maxAccumulatedDetections);
        foreach (int id in _accumulatedDetections.Keys)
        {
            positions.Clear();
            rotations.Clear();

            Queue<TagDetector.Detection> detections = _accumulatedDetections[id];
            if (detections.Count == 0)
                continue;

            foreach (TagDetector.Detection d in detections)
            {
                positions.Add(d.Position);
                rotations.Add(d.Rotation);
            }

            _tagTransforms[id] = new Tuple<Vector3, Quaternion>(AveragePosition(positions.AsReadOnly()), AverageRotation(rotations.AsReadOnly()));
        }
    }

    private void UpdateModelTransforms()
    {
        foreach ((Transform transform, ReadOnlyCollection<TagPlacement> placements) in _associatedTagPlacements)
        {
            List<Vector3> positions = new List<Vector3>(placements.Count);
            List<Quaternion> rotations = new List<Quaternion>(placements.Count);

            foreach (TagPlacement p in placements)
            {
                if (!_tagTransforms.ContainsKey(p.TagID))
                    continue;
                positions.Add(-(_tagTransforms[p.TagID].Item2 * p.Position) + _tagTransforms[p.TagID].Item1);
                rotations.Add(Quaternion.Inverse(Quaternion.Euler(p.Rotation)) * _tagTransforms[p.TagID].Item2);
            }

            if (positions.Count == 0)
                continue;

            transform.localPosition = AveragePosition(positions.AsReadOnly());
            transform.localRotation = AverageRotation(rotations.AsReadOnly());
        }
    }

    private Quaternion AverageRotation(ReadOnlyCollection<Quaternion> rotations)
    {
        Quaternion avg = rotations[0];
        for (int i = 1; i < rotations.Count; i++)
        {
            float t = 1.0f / (i + 1.0f);
            avg = Quaternion.Slerp(avg, rotations[i], t);
        }
        return avg;
    }
    
    private Vector3 AveragePosition(ReadOnlyCollection<Vector3> positions)
    {
        Vector3 avg = Vector3.zero;
        for (int i = 0; i < positions.Count; i++)
            avg += positions[i] / positions.Count;
        return avg;
    }
}
