using System;
using System.IO;
using UnityEngine;

public class Settings
{
    [Serializable]
    private class SettingsData
    {
        public float TagSize;
        public int Decimation;
        public float Downscaling;
        public float Smoothing;
        public bool EnableDebug;
    }

    private SettingsData _data;
    public float TagSize { get { return _data.TagSize; } set { if (float.IsFinite(value) && value > 0.0f) _data.TagSize = value; } }
    public int Decimation { get { return _data.Decimation; } set { if (value > 0) _data.Decimation = value; } }
    public float Downscaling { get { return _data.Downscaling; } set { if (float.IsFinite(value) && value >= 1.0f) _data.Downscaling = value; } }
    public float Smoothing { get { return _data.Smoothing; } set { if (float.IsFinite(value) && value >= 0.0f) _data.Smoothing = value; } }
    public bool EnableDebug { get { return _data.EnableDebug; } set { _data.EnableDebug = value; } }


    private static Settings _current;
    public static Settings Current { get { if (_current == null) _current = LoadSettings(); return _current; } }
    private static string _settingsPath { get { return Application.persistentDataPath + Path.DirectorySeparatorChar + "settings.json"; } }

    private Settings(SettingsData data)
    {
        if (data == null)
            throw new ArgumentException("Data was null.");
        _data = data;
    }

    private static Settings LoadSettings()
    {
        // TODO check if values are valid

        Debug.Log("Loading settings.");
        if (!File.Exists(_settingsPath))
            return DefaultSettings();

        try
        {
            string json = File.ReadAllText(_settingsPath);
            return new Settings(JsonUtility.FromJson<SettingsData>(json));
        }
        catch (Exception e)
        {
            Debug.LogError("Could not load settings: " + e.Message);
            return DefaultSettings();
        }
    }

    public static Settings DefaultSettings()
    {
        return new Settings(new SettingsData()
        {
            TagSize = 0.0556f,
            Decimation = 2,
            Downscaling = 1.0f,
            Smoothing = 2.0f,
            EnableDebug = false
        });
    }

    public void SaveSettings()
    {
        Debug.Log("Saving settings.");
        try
        {
            string json = JsonUtility.ToJson(_data);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception e)
        {
            Debug.LogError("Could not save settings: " + e.Message);
        }
    }
}