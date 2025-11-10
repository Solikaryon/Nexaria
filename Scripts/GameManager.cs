using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public DeckLoader deckLoader;
    public UIManager uiManager;
    private bool victoryHandled = false;

    private CardModel currentCard;
    private string savePath;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "save.json");
    }

    [System.Serializable]
    public class SaveData
    {
        [System.Serializable]
        public class ResourceEntry { public string key; public int value; }
        [System.Serializable]
        public class FlagEntry { public string key; public bool value; }

        // Listas serializables (JsonUtility no soporta Dictionary)
        public List<ResourceEntry> resources = new List<ResourceEntry>();
        public int harmonyPoints;
        public List<FlagEntry> flags = new List<FlagEntry>();
        public string currentCardId;
        public List<string> shownCards = new List<string>();
        public List<string> remainingTutorialCards = new List<string>();
        public bool tutorialCompleted;
    }

    void Start()
    {
        if (deckLoader == null) Debug.LogError("DeckLoader no asignado en GameManager.");
        if (uiManager == null) Debug.LogError("UIManager no asignado en GameManager.");

        if (File.Exists(savePath))
        {
            LoadGame();
        }
        else
        {
            // Solo inicializar recursos cuando no hay partida guardada
            ResourceManager.Instance.InitializeResources();
            deckLoader.Load();
            if (BiasTracker.Instance != null) BiasTracker.Instance.ResetTracker();
            ShowNextCard(null);
        }
    }

    public void HandleVictory()
    {
        if (victoryHandled) return;
        victoryHandled = true;

        var totals = BiasTracker.Instance != null ? BiasTracker.Instance.GetTotals() : new Dictionary<ResourceType, int>();
        var finals = ResourceManager.Instance.GetAll();
        var eval = DecisionEvaluator.Evaluate(totals, finals);

        if (uiManager != null)
        {
            uiManager.ShowVictory();
            uiManager.ShowFinalEvaluation(eval.summaryText);
        }

        // Ya no cargamos automáticamente los créditos
        // El jugador puede usar el botón que asignes para ir a los créditos
    }

    // Método LoadCreditsAfterDelay eliminado - ya no se carga automáticamente

    public void ShowNextCard(OptionData chosenOption)
    {
        CardModel next = null;
        if (currentCard == null)
            next = deckLoader.GetNextCard(null, null);
        else
            next = deckLoader.GetNextCard(currentCard, chosenOption);

        if (next == null)
        {
            Debug.Log("No hay cartas elegibles. Fin de rama.");
            uiManager.ShowEndOfBranch();
            return;
        }

        currentCard = next;
        uiManager.DisplayCard(next);
    // Marcar carta como mostrada para evitar repetirla (especialmente endings)
    if (deckLoader != null) deckLoader.MarkCardShown(next.id);

        // ✅ Si se alcanza la meta de armonía, marcar victory_done y esperar a la secuencia de endings
        if (HarmonyManager.Instance != null && HarmonyManager.Instance.CheckVictoryAfterCard())
        {
            if (FlagManager.Instance != null)
                FlagManager.Instance.SetFlag("victory_done", true);

            // Iniciar espera por la secuencia de ending antes de completar la partida.
            Instance.StartCoroutine(WaitForEndingsAndVictory());
            return;
        }
    }

    public void ApplyOption(OptionData option)
    {
        if (option == null) return;

        // efectos
        if (option.effects != null)
        {
            foreach (var eff in option.effects)
            {
                if (ResourceManager.Instance.TryParseResource(eff.resource, out ResourceType r))
                {
                    ResourceManager.Instance.Modify(r, eff.delta);

                    if (BiasTracker.Instance != null)
                        BiasTracker.Instance.RegisterEffect(r, eff.delta);
                }
                else
                    Debug.LogWarning("Resource parse fail: " + eff.resource);
            }
        }

        // flags
        if (option.setFlags != null)
            foreach (var f in option.setFlags) FlagManager.Instance.SetFlag(f, true);

        // chequeo armonía
        HarmonyManager.Instance.CheckHarmony();

        // Si alcanzamos la meta de armonía, marcar victory_done aquí para que el deck pueda
        // considerar cartas de ending en la siguiente selección inmediata.
        if (HarmonyManager.Instance != null && HarmonyManager.Instance.CheckVictoryAfterCard())
        {
            if (FlagManager.Instance != null)
                FlagManager.Instance.SetFlag("victory_done", true);
        }

        // chequeo derrota
        if (ResourceManager.Instance.CheckLoss(out ResourceType reason))
        {
            var totals = BiasTracker.Instance != null ? BiasTracker.Instance.GetTotals() : new Dictionary<ResourceType, int>();
            var finals = ResourceManager.Instance.GetAll();
            var eval = DecisionEvaluator.Evaluate(totals, finals);

            uiManager.ShowDefeat(reason);
            uiManager.ShowFinalEvaluation(eval.summaryText);
            return;
        }

        // siguiente carta
        ShowNextCard(option);
    }

    // ================== GUARDADO / CARGA ==================
    public void SaveGame()
    {
        SaveData data = new SaveData();

        var resources = ResourceManager.Instance.GetAll();
        Debug.Log($"Guardando {resources.Count} recursos:");
        foreach (var kv in resources)
        {
            data.resources.Add(new SaveData.ResourceEntry { key = kv.Key.ToString(), value = kv.Value });
            Debug.Log($"  {kv.Key}: {kv.Value}");
        }

        data.harmonyPoints = HarmonyManager.Instance.harmonyPoints;

        var flags = FlagManager.Instance.GetAllFlags();
        foreach (var kv in flags)
            data.flags.Add(new SaveData.FlagEntry { key = kv.Key, value = kv.Value });

        data.currentCardId = currentCard != null ? currentCard.id : null;

        // Guardar cartas ya mostradas del DeckLoader
        if (deckLoader != null)
        {
            data.shownCards = deckLoader.GetShownCards();
            data.remainingTutorialCards = deckLoader.GetRemainingTutorialCards();
            data.tutorialCompleted = deckLoader.IsTutorialCompleted();
            Debug.Log($"Guardando {data.shownCards.Count} cartas mostradas y {data.remainingTutorialCards.Count} cartas de tutorial restantes");
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log("✅ Partida guardada en " + savePath);
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("⚠️ No hay archivo de guardado.");
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // Restaurar recursos (puntos de cada tribu)
        Debug.Log($"Cargando {data.resources.Count} recursos desde el save:");
        foreach (var entry in data.resources)
        {
            Debug.Log($"  {entry.key}: {entry.value}");
            if (ResourceManager.Instance.TryParseResource(entry.key, out ResourceType r))
            {
                ResourceManager.Instance.SetValue(r, entry.value);
                Debug.Log($"  ✅ {r} establecido a {entry.value}");
            }
            else
            {
                Debug.LogWarning($"  ❌ No se pudo parsear el recurso: {entry.key}");
            }
        }

        // Restaurar puntos de armonía
        HarmonyManager.Instance.harmonyPoints = data.harmonyPoints;

        // Restaurar flags
        foreach (var entry in data.flags)
            FlagManager.Instance.SetFlag(entry.key, entry.value);

        Debug.Log("✅ Partida cargada");

        // Asegurar que todos los tipos de recursos estén inicializados
        ResourceManager.Instance.InitializeResources();

    // Cargar el deck primero
    deckLoader.Load();

        // Restaurar el estado del DeckLoader (cartas mostradas y tutorial)
        Debug.Log($"Restaurando cartas mostradas: {data.shownCards?.Count ?? 0}");
        if (data.shownCards != null)
        {
            deckLoader.SetShownCards(data.shownCards);
            Debug.Log($"  Cartas mostradas: {string.Join(", ", data.shownCards)}");
        }

        // Si hay lista explícita de tutorial restante, usarla.
        // Si no, pero tutorialCompleted=true, vaciar la cola de tutorial para no repetirlo.
        Debug.Log($"Restaurando tutorial restante: {data.remainingTutorialCards?.Count ?? 0}, tutorialCompleted={data.tutorialCompleted}");
        if (data.remainingTutorialCards != null && data.remainingTutorialCards.Count > 0)
        {
            deckLoader.SetRemainingTutorialCards(data.remainingTutorialCards);
            Debug.Log($"  Tutorial restante: {string.Join(", ", data.remainingTutorialCards)}");
        }
        else if (data.tutorialCompleted)
        {
            deckLoader.SetTutorialCompleted(true);
            Debug.Log("  Tutorial marcado como completado (cola vaciada)");
        }
        else
        {
            // Fallback para saves antiguos: si la carta actual no es de tutorial, dar por completado el tutorial
            if (!string.IsNullOrEmpty(data.currentCardId))
            {
                var maybeCard = deckLoader.GetCardById(data.currentCardId);
                if (maybeCard != null && !maybeCard.isTutorial)
                {
                    deckLoader.SetTutorialCompleted(true);
                    Debug.Log("  Fallback: tutorial marcado como completado porque currentCard no es tutorial y no hay estado guardado.");
                }
            }
        }

        // Restaurar carta actual
        if (!string.IsNullOrEmpty(data.currentCardId))
        {
            CardModel card = deckLoader.GetCardById(data.currentCardId);
            if (card != null)
            {
                currentCard = card;
                uiManager.DisplayCard(card);
                Debug.Log("📖 Carta restaurada: " + card.id);
            }
            else
            {
                Debug.LogWarning("⚠️ No se encontró la carta guardada, iniciando nueva rama.");
                ShowNextCard(null);
            }
        }
        else
        {
            ShowNextCard(null);
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("🗑️ Guardado eliminado");
        }
        else
        {
            Debug.Log("⚠️ No había guardado para borrar");
        }
    }

    System.Collections.IEnumerator WaitForEndingsAndVictory()
    {
        // Esperar hasta que la flag 'ending_done' sea true, pero con un timeout de seguridad (ej. 30s)
        float timeout = 30f;
        float timer = 0f;

        Debug.Log("GameManager: victory_done seteado. Esperando secuencia de endings (ending_done)...");

        while (timer < timeout)
        {
            if (FlagManager.Instance != null && FlagManager.Instance.GetFlag("ending_done"))
            {
                Debug.Log("GameManager: ending_done detectado. Procediendo a HandleVictory().");
                HandleVictory();
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("GameManager: timeout esperando ending_done. Procediendo a HandleVictory() de todos modos.");
        HandleVictory();
    }
}
