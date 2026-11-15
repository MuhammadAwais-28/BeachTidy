using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlacedRecord
{
    public string id;               // PlaceableData.Id of the object
    public PlaceableType type;
    public Vector3 position;
    public Quaternion rotation;
}

[Serializable]
public class SessionData
{
    public int stageIndex;
    public bool complete;
    public List<PlacedRecord> placed = new List<PlacedRecord>();
}

// Static, so it survives scene loads for as long as the app is running.
// It is wiped automatically when the app closes (unless InstructionManager.persistToDisk is on).
public static class GameSession
{
    private const string PrefsKey = "BeachTidy.Session";

    public static SessionData data = new SessionData();
    public static bool hasData;

    // Keeps the Editor honest if "Enter Play Mode Options" has Domain Reload turned off
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        data = new SessionData();
        hasData = false;
    }

    public static void SaveToDisk()
    {
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool LoadFromDisk()
    {
        if (!PlayerPrefs.HasKey(PrefsKey))
            return false;

        try
        {
            SessionData loaded = JsonUtility.FromJson<SessionData>(PlayerPrefs.GetString(PrefsKey));
            if (loaded == null)
                return false;

            data = loaded;
            hasData = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameSession: could not read saved progress: " + e.Message);
            return false;
        }
    }

    // Start a fresh game (also deletes the disk save)
    public static void Clear()
    {
        data = new SessionData();
        hasData = false;
        PlayerPrefs.DeleteKey(PrefsKey);
    }
}
