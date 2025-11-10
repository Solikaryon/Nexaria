using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResourceType { Oro, Prisma, AlmaDeHierro, FeEterna, RaicesAntiguas }

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;
    public int min = 0, max = 100;
    private Dictionary<ResourceType, int> values = new Dictionary<ResourceType, int>();

    public event Action<ResourceType,int> OnResourceChanged;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // No inicializar automáticamente - será manejado por GameManager
    }

    public void InitializeResources()
    {
        foreach (ResourceType r in Enum.GetValues(typeof(ResourceType)))
        {
            if (!values.ContainsKey(r))
                values[r] = 50; // valor inicial en equilibrio solo si no existe
        }
    }

    public int Get(ResourceType r) => values.ContainsKey(r) ? values[r] : 50;

    public bool TryParseResource(string name, out ResourceType r)
    {
        return Enum.TryParse(name, out r);
    }

    public void Modify(ResourceType r, int delta)
    {
        int old = Get(r); // Usa Get() que es seguro y devuelve 50 por defecto
        int nv = Mathf.Clamp(old + delta, min, max);
        values[r] = nv;
        OnResourceChanged?.Invoke(r, nv);
    }

    public void SetValue(ResourceType r, int value)
    {
        int nv = Mathf.Clamp(value, min, max);
        values[r] = nv;
        OnResourceChanged?.Invoke(r, nv);
    }

    public bool CheckLoss(out ResourceType reason)
    {
        // Verificar todos los tipos de recursos usando Get() que es seguro
        foreach (ResourceType r in System.Enum.GetValues(typeof(ResourceType)))
        {
            int value = Get(r);
            if (value <= min) { reason = r; return true; }
            if (value >= max) { reason = r; return true; }
        }
        reason = default;
        return false;
    }

    public Dictionary<ResourceType,int> GetAll() 
    {
        var result = new Dictionary<ResourceType,int>();
        foreach (ResourceType r in Enum.GetValues(typeof(ResourceType)))
        {
            result[r] = Get(r); // Usa Get() que devuelve 50 por defecto
        }
        return result;
    }
}
