
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ModelBundle
{
    public string Name;
    public Model[] Models;
    public TagPlacement[] TagPlacements;

    public bool IsValid(out string error)
    {
        if (Name == null || Name == "")
        {
            error = "Bundle has no name.";
            return false;
        }
        if (Models.Length == 0)
        {
            error = "Bundle contains no models.";
            return false;
        }
        if (TagPlacements.Length == 0)
        {
            error = "Bundle has no associated tag placements.";
            return false;
        }

        HashSet<int> tagIDs = new HashSet<int>();
        foreach (TagPlacement p in TagPlacements)
        {
            if (p.TagID < 0)
            {
                error = "Tag ID " + p.TagID + " is negative and therefore invalid.";
                return false;
            }
            if (tagIDs.Contains(p.TagID))
            {
                error = "Tag ID " + p.TagID + " is included several times in the list of tag placements.";
                return false;
            }
            tagIDs.Add(p.TagID);
        }

        error = null;
        return true;
    }
}

[Serializable]
public class Model
{
    public string Name;
    public string Uri;
    public Vector3 Translation;
    public Vector3 Rotation;
    public Vector3 Scale = Vector3.one;
}

[Serializable]
public class TagPlacement
{
    public int TagID;
    public Vector3 Position;
    public Vector3 Rotation;
}

public class LoadedModel
{
    public ModelBundle ModelBundle { get; private set; }
    public GameObject GameObject { get; private set; }
    public GameObject AdjustmentHandle { get; private set; }

    public LoadedModel(ModelBundle modelBundle, GameObject gameObject, GameObject adjustmentHandle)
    {
        ModelBundle = modelBundle;
        GameObject = gameObject;
        AdjustmentHandle = adjustmentHandle;
    }
}