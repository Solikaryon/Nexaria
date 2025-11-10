using System.Collections.Generic;
using UnityEngine;

public class FlagManager : MonoBehaviour
{
    public static FlagManager Instance;
    private Dictionary<string,bool> flags = new Dictionary<string,bool>();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void SetFlag(string name, bool value)
    {
        flags[name] = value;
        Debug.Log($"Flag {name} = {value}");
    }

    public bool GetFlag(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (flags.TryGetValue(name, out bool v)) return v;
        return false;
    }

    public void ToggleFlag(string name) => SetFlag(name, !GetFlag(name));
    public void ClearAll() => flags.Clear();
    public Dictionary<string,bool> GetAllFlags() => new Dictionary<string,bool>(flags);
}
