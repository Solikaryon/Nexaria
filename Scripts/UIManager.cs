using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Cartas")]
    public TMP_Text titleText;
    public TMP_Text descText;
    public Button btnA;
    public Button btnB;
    public CharacterManager characterManager;
    private CardModel current;

    [Header("Escenas")]
    public string defeatSceneName;

    [Header("Evaluación final")]
    public GameObject finalEvaluationPanel;
    public TMP_Text finalEvaluationText;

    [Header("Botón personalizado de escena")]
    [Tooltip("Botón que al pulsarse llevará a la escena asignada.")]
    public Button sceneButton;
    [Tooltip("Texto del botón que muestra el nombre o descripción de la escena.")]
    public TMP_Text sceneButtonText;
    [Tooltip("Nombre de la escena a la que se dirigirá este botón.")]
    public string targetSceneName;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        btnA.onClick.AddListener(OnClickA);
        btnB.onClick.AddListener(OnClickB);

        if (finalEvaluationPanel != null)
            finalEvaluationPanel.SetActive(false);

        // Configurar el botón personalizado si está asignado
        if (sceneButton != null)
        {
            sceneButton.onClick.AddListener(OnClickSceneButton);
            if (sceneButtonText != null && !string.IsNullOrEmpty(targetSceneName))
                sceneButtonText.text = targetSceneName; // Muestra el nombre de la escena o puedes cambiarlo por texto personalizado
        }

        StartCoroutine(DelayedReconnect());
    }

    IEnumerator DelayedReconnect()
    {
        yield return null; // 👈 espera un frame
        ReconnectManagers();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedReconnect());
    }

    void ReconnectManagers()
    {
        // Buscar CharacterManager persistente
        if (characterManager == null)
        {
            characterManager = FindObjectOfType<CharacterManager>(true);
            if (characterManager != null)
                Debug.Log("✅ CharacterManager reconectado correctamente por UIManager.");
            else
                Debug.LogWarning("⚠ No se encontró CharacterManager tras cambiar de escena.");
        }

        // Verificar también GameManager si lo usas
        if (GameManager.Instance == null)
        {
            var gm = FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                GameManager.Instance = gm;
                Debug.Log("✅ GameManager reconectado correctamente.");
            }
            else
            {
                Debug.LogWarning("⚠ No se encontró GameManager activo tras el cambio de escena.");
            }
        }
    }

    public void DisplayCard(CardModel card)
    {
        current = card;
        titleText.text = card.title;
        descText.text = card.description;

        var ta = btnA.GetComponentInChildren<TMP_Text>();
        var tb = btnB.GetComponentInChildren<TMP_Text>();
        if (ta != null) ta.text = card.optionA.text;
        if (tb != null) tb.text = card.optionB.text;

        // Mostrar personaje según JSON
        if (characterManager != null && !string.IsNullOrEmpty(card.character))
        {
            characterManager.ShowCharacter(card.character);
        }
        else if (characterManager == null)
        {
            Debug.LogWarning("⚠ No se pudo mostrar el personaje, CharacterManager no está asignado.");
        }
    }

    void OnClickA()
    {
        if (GameManager.Instance == null || current == null)
        {
            Debug.LogWarning("⚠ No se puede aplicar opción A: referencias perdidas.");
            ReconnectManagers();
            return;
        }
        GameManager.Instance.ApplyOption(current.optionA);
    }

    void OnClickB()
    {
        if (GameManager.Instance == null || current == null)
        {
            Debug.LogWarning("⚠ No se puede aplicar opción B: referencias perdidas.");
            ReconnectManagers();
            return;
        }
        GameManager.Instance.ApplyOption(current.optionB);
    }

    public void ShowDefeat(ResourceType reason)
    {
        Debug.Log("DERROTA por: " + reason);

        if (!string.IsNullOrEmpty(defeatSceneName))
        {
            SceneManager.LoadScene(defeatSceneName);
        }
        else
        {
            var totals = BiasTracker.Instance != null ? BiasTracker.Instance.GetTotals() : new Dictionary<ResourceType, int>();
            var finals = ResourceManager.Instance.GetAll();
            var eval = DecisionEvaluator.Evaluate(totals, finals);

            ShowFinalEvaluation(eval.summaryText);
        }
    }

    public void ShowEndOfBranch()
    {
        Debug.Log("FIN de la rama narrativa.");
    }

    public void ShowVictory()
    {
        Debug.Log("VICTORIA alcanzada.");

        var totals = BiasTracker.Instance != null ? BiasTracker.Instance.GetTotals() : new Dictionary<ResourceType, int>();
        var finals = ResourceManager.Instance.GetAll();
        var eval = DecisionEvaluator.Evaluate(totals, finals);

        ShowFinalEvaluation(eval.summaryText);
    }

    public void ShowFinalEvaluation(string summary)
    {
        if (finalEvaluationPanel != null)
        {
            finalEvaluationPanel.SetActive(true);
            if (finalEvaluationText != null)
                finalEvaluationText.text = summary;
        }
        else
        {
            Debug.Log("Evaluación final:\n" + summary);
        }
    }

    public void Creditos()
    {
        SceneManager.LoadScene("Credits");
    }

    // 🔹 Nuevo método del botón personalizado
    void OnClickSceneButton()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"Cargando escena: {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("⚠ No se ha asignado un nombre de escena al botón personalizado.");
        }
    }
}
