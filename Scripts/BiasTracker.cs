using System;
using System.Collections.Generic;
using UnityEngine;

public class BiasTracker : MonoBehaviour
{
    public static BiasTracker Instance;

    // Suma total de deltas aplicados por recurso (positivo = favorecido, negativo = perjudicado)
    private Dictionary<ResourceType,int> totalDeltas = new Dictionary<ResourceType,int>();
    // Contador de veces que se afectó cada recurso (útil para métricas adicionales)
    private Dictionary<ResourceType,int> counts = new Dictionary<ResourceType,int>();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        ResetTracker();
    }

    public void ResetTracker()
    {
        totalDeltas.Clear();
        counts.Clear();
        foreach (ResourceType r in Enum.GetValues(typeof(ResourceType)))
        {
            totalDeltas[r] = 0;
            counts[r] = 0;
        }
    }

    public void RegisterEffect(ResourceType r, int delta)
    {
        if (!totalDeltas.ContainsKey(r)) totalDeltas[r] = 0;
        if (!counts.ContainsKey(r)) counts[r] = 0;
        totalDeltas[r] += delta;
        counts[r] += 1;
        Debug.Log($"BiasTracker: {r} delta {delta} -> total {totalDeltas[r]} (count {counts[r]})");
    }

    public Dictionary<ResourceType,int> GetTotals() => new Dictionary<ResourceType,int>(totalDeltas);
    public Dictionary<ResourceType,int> GetCounts() => new Dictionary<ResourceType,int>(counts);

    // Debug util
    public void DumpToConsole()
    {
        Debug.Log("=== BiasTracker dump ===");
        foreach (var kv in totalDeltas) Debug.Log($"{kv.Key}: totalDelta={kv.Value}, count={counts[kv.Key]}");
    }
}
