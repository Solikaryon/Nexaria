using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceBarUI : MonoBehaviour
{
    public ResourceType resourceType;
    public Slider slider;
    public TMP_Text valueText;

    void Start()
    {
        if (ResourceManager.Instance == null) return;
        if (slider != null) slider.maxValue = ResourceManager.Instance.max;
        UpdateUI(ResourceManager.Instance.Get(resourceType));
        ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
    }

    void OnDestroy()
    {
        if (ResourceManager.Instance != null) ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
    }

    void OnResourceChanged(ResourceType r, int val)
    {
        if (r == resourceType) UpdateUI(val);
    }

    void UpdateUI(int val)
    {
        if (slider != null) slider.value = val;
        if (valueText != null) valueText.text = val.ToString();
    }
}
