using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HarmonyManager : MonoBehaviour
{
    public static HarmonyManager Instance;

    [Header("Armonía")]
    public int harmonyPoints = 0;
    public int harmonyGoal = 5;

    [Header("UI")]
    public Slider harmonySlider;
    public TMP_Text harmonyText; // Texto donde mostramos el progreso
    [Tooltip("Si está activo, mientras no termine el tutorial mostrará '(tutorial)' en el texto.")]
    public bool indicateTutorialLockInText = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (harmonySlider != null)
        {
            harmonySlider.maxValue = harmonyGoal;
            harmonySlider.value = harmonyPoints;
        }
        UpdateText();
    }

    /// <summary>
    /// Se llama después de cada decisión para actualizar armonía.
    /// Ya no activa victoria automáticamente.
    /// </summary>
    public void CheckHarmony()
    {
        if (!IsHarmonyActive())
        {
            UpdateText();
            return;
        }

        var values = ResourceManager.Instance.GetAll();
        bool todosEnRango = true;

        foreach (var kv in values)
        {
            int indicador = kv.Value;
            if (indicador < 25 || indicador > 75)
            {
                todosEnRango = false;
                break;
            }
        }

        if (todosEnRango)
        {
            // No incrementar si ya alcanzamos la meta
            if (harmonyPoints < harmonyGoal)
            {
                harmonyPoints++;
                if (harmonySlider != null)
                    harmonySlider.value = harmonyPoints;

                Debug.Log("Armonía +1 → Total: " + harmonyPoints);
            }
        }

        UpdateText();
    }

    void UpdateText()
    {
        if (harmonyText == null) return;

        if (!IsHarmonyActive() && indicateTutorialLockInText)
            harmonyText.text = $"Armonía: {harmonyPoints}/{harmonyGoal} (tutorial)";
        else
            harmonyText.text = $"Armonía: {harmonyPoints}/{harmonyGoal}";
    }

    bool IsHarmonyActive()
    {
        return FlagManager.Instance != null && FlagManager.Instance.GetFlag("tutorial_done");
    }

    /// <summary>
    /// Nuevo método: devuelve true si se puede activar victoria después de mostrar la carta.
    /// No modifica flags ni llama a GameManager.
    /// </summary>
    public bool CheckVictoryAfterCard()
    {
        return harmonyPoints >= harmonyGoal && IsHarmonyActive();
    }
}
