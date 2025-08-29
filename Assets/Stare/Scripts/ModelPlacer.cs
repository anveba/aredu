using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModelPlacer : MonoBehaviour
{
    [SerializeField] private TagDetector _detector = null;
    public bool EnableStaling = true;
    [SerializeField] private int _maxAccumulatedDetections = 1000;
    [SerializeField] private float _filterAngle = 10.0f;
    [SerializeField] private float _filterPosition = 0.1f;

    private Dictionary<int, Queue<TagDetector.Detection>> _accumulatedDetections;
    private Dictionary<Transform, IReadOnlyCollection<TagPlacement>> _associatedTagPlacements;
    private Dictionary<int, TagTransform> _tagTransforms;

    private struct TagTransform
    {
        public Vector3 SmoothedPosition;
        public Quaternion SmoothedRotation;
        public Vector3 AveragePosition;
        public Quaternion AverageRotation;
    }


    private void Start()
    {
        _accumulatedDetections = new Dictionary<int, Queue<TagDetector.Detection>>();
        _associatedTagPlacements = new Dictionary<Transform, IReadOnlyCollection<TagPlacement>>();
        _tagTransforms = new Dictionary<int, TagTransform>();
    }

    public void SetModelPlacement(Transform modelTransform, IReadOnlyCollection<TagPlacement> placements)
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

            if (EnableStaling)
            {
                var q = _accumulatedDetections[tag.TagID];
                while (q.Count > 0 && Time.time - q.Peek().Timestamp > Settings.Current.Smoothing)
                    q.Dequeue();
            }
        }
    }

    private void Update()
    {
        UpdateTagTransforms();
        UpdateModelTransforms();
    }

    private void UpdateTagTransforms()
    {
        List<Vector3> positions = new List<Vector3>(_maxAccumulatedDetections);
        List<Vector3> filteredPositions = new List<Vector3>(_maxAccumulatedDetections);
        List<Quaternion> rotations = new List<Quaternion>(_maxAccumulatedDetections);
        List<Quaternion> filteredRotations = new List<Quaternion>(_maxAccumulatedDetections);
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

            Vector3 averagePosition = AveragePosition(positions);
            Quaternion averageRotation = AverageRotation(rotations);

            foreach (TagDetector.Detection d in detections)
            {
                if (Vector3.Distance(averagePosition, d.Position) <= _filterPosition) // TODO use standard deviation(s) instead of a fixed value?
                    filteredPositions.Add(d.Position);
                if (Quaternion.Angle(averageRotation, d.Rotation) <= _filterAngle)
                    filteredRotations.Add(d.Rotation);
            }

            Vector3 smoothedPosition = filteredPositions.Count > 0 ? AveragePosition(filteredPositions) : averagePosition;
            Quaternion smoothedRotation = filteredRotations.Count > 0 ? AverageRotation(filteredRotations) : averageRotation;

            _tagTransforms[id] = new TagTransform() { SmoothedPosition = smoothedPosition, SmoothedRotation = smoothedRotation, AveragePosition = averagePosition, AverageRotation = averageRotation };
        }
    }

    private void UpdateModelTransforms()
    {
        foreach ((Transform transform, IReadOnlyCollection<TagPlacement> placements) in _associatedTagPlacements)
        {
            List<Vector3> positions = new List<Vector3>(placements.Count);
            List<Quaternion> rotations = new List<Quaternion>(placements.Count);

            foreach (TagPlacement p in placements)
            {
                if (!_tagTransforms.ContainsKey(p.TagID))
                    continue;
                positions.Add(_tagTransforms[p.TagID].SmoothedPosition - (_tagTransforms[p.TagID].SmoothedRotation * p.Position));
                rotations.Add(_tagTransforms[p.TagID].SmoothedRotation * Quaternion.Inverse(Quaternion.Euler(p.Rotation)));
            }

            if (positions.Count == 0)
                continue;

            transform.localPosition = AveragePosition(positions);
            transform.localRotation = AverageRotation(rotations);
        }
    }

    private Quaternion AverageRotation(IReadOnlyCollection<Quaternion> rotations)
    {
        Quaternion avg = rotations.ElementAt(0);
        for (int i = 1; i < rotations.Count; i++)
        {
            float t = 1.0f / (i + 1.0f);
            avg = Quaternion.Slerp(avg, rotations.ElementAt(i), t);
        }
        return avg;
    }

    private Vector3 AveragePosition(IReadOnlyCollection<Vector3> positions)
    {
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < positions.Count; i++)
            sum += positions.ElementAt(i);
        return sum / positions.Count;
    }
}
