using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DeckLoader : MonoBehaviour
{
    [Header("Fuentes de datos")]
    public TextAsset eventsJsonAsset;      // opcional: arrastra events.json

    [Header("Zonas de calor")]
    public bool useHotZones = true;
    public int hotLow = 15;   // umbral bajo (incl.)
    public int hotHigh = 85;  // umbral alto (incl.)

    private List<CardModel> allCards = new List<CardModel>();
    private Queue<CardModel> tutorialQueue = new Queue<CardModel>();
    private HashSet<string> shownCards = new HashSet<string>();

    void Start() => Load();

    public void Load()
    {
        // 1) Cargar events.json
        string json = null;

        if (eventsJsonAsset != null) json = eventsJsonAsset.text;
        else
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "events.json");
            if (System.IO.File.Exists(path)) json = System.IO.File.ReadAllText(path);
        }

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("DeckLoader: no se encontró events.json (asigna TextAsset o colócalo en StreamingAssets).");
            return;
        }

        var list = JsonUtility.FromJson<CardList>(json);
        allCards = list?.cards ?? new List<CardModel>();

        // Separar tutorial
        var tutorials = allCards.Where(c => c.isTutorial).OrderBy(c => c.id).ToList();
        tutorialQueue = new Queue<CardModel>(tutorials);

        // Si ya marcamos el tutorial como completado (bandera persistida), vaciar la cola
        if (FlagManager.Instance != null && FlagManager.Instance.GetFlag("tutorial_completed"))
        {
            tutorialQueue.Clear();
        }

        Debug.Log($"DeckLoader: cargadas {allCards.Count} cartas. Tutorial: {tutorialQueue.Count}.");

        // Imprimir todas las cartas en consola
        foreach (var c in allCards)
        {
            string type = c.isTutorial ? "Tutorial" : "Normal";
            Debug.Log($"Carta: {c.id} | Tipo: {type} | TribuId: {c.tribeId}");
        }
    }

    public CardModel GetCardById(string id) => allCards.FirstOrDefault(c => c.id == id);

    private bool IsCardEligible(CardModel card)
    {
        if (card == null) return false;

        foreach (var f in card.requiredFlags)
        {
            if (string.IsNullOrWhiteSpace(f)) continue;
            if (f.StartsWith("!"))
            {
                string name = f.Substring(1);
                if (FlagManager.Instance.GetFlag(name)) return false;
            }
            else
            {
                if (!FlagManager.Instance.GetFlag(f)) return false;
            }
        }

        foreach (var cond in card.requiredResourceConditions)
            if (!EvaluateResourceCondition(cond)) return false;

        return true;
    }

    private bool EvaluateResourceCondition(string cond)
    {
        cond = cond?.Trim();
        if (string.IsNullOrEmpty(cond)) return true;

        string[] ops = new string[] { ">=", "<=", "==", "!=", ">", "<" };
        foreach (var op in ops)
        {
            int idx = cond.IndexOf(op);
            if (idx > 0)
            {
                string resName = cond.Substring(0, idx).Trim();
                string valStr = cond.Substring(idx + op.Length).Trim();
                if (!int.TryParse(valStr, out int val)) return false;
                if (!ResourceManager.Instance.TryParseResource(resName, out ResourceType r)) return false;
                int curr = ResourceManager.Instance.Get(r);
                return op switch
                {
                    ">=" => curr >= val,
                    "<=" => curr <= val,
                    "==" => curr == val,
                    "!=" => curr != val,
                    ">" => curr > val,
                    "<" => curr < val,
                    _ => false
                };
            }
        }
        Debug.LogWarning("DeckLoader: no se pudo parsear condición: " + cond);
        return false;
    }

    public CardModel GetNextCard(CardModel current, OptionData chosenOption)
    {
        CardModel nextCard = null;

        // 0) Opción fuerza "next"
        if (chosenOption != null && !string.IsNullOrEmpty(chosenOption.next))
        {
            var forced = GetCardById(chosenOption.next);
            if (forced != null && IsCardEligible(forced)) nextCard = forced;
        }

        // 0.5) Si la victoria ya está marcada, priorizar endings elegibles que NO hayan sido ya mostrados
        if (nextCard == null && FlagManager.Instance != null && FlagManager.Instance.GetFlag("victory_done"))
        {
            Debug.Log("DeckLoader: victory_done activo. shownCards: " + string.Join(",", shownCards));
            var candidates = allCards.Where(c => c != null && c.isEnding && !shownCards.Contains(c.id)).ToList();
            Debug.Log("DeckLoader: ending candidates (not shown): " + string.Join(",", candidates.Select(x => x.id)));

            foreach (var cand in candidates)
            {
                bool ok = IsCardEligible(cand);
                Debug.Log($"DeckLoader: ending {cand.id} IsCardEligible={ok}");
                if (ok)
                {
                    nextCard = cand;
                    break;
                }
            }
        }

        // 1) Tutorial primero, salvo que ya se haya completado
        if (nextCard == null && tutorialQueue.Count > 0 && !(FlagManager.Instance != null && FlagManager.Instance.GetFlag("tutorial_completed")))
        {
            nextCard = tutorialQueue.Dequeue();
            // Si era la última carta del tutorial, marcarlo como completado para no reencolar
            if (tutorialQueue.Count == 0 && FlagManager.Instance != null)
            {
                FlagManager.Instance.SetFlag("tutorial_completed", true);
            }
        }

        // 2) Zonas de calor
        if (nextCard == null && useHotZones)
        {
            var values = ResourceManager.Instance.GetAll();
            foreach (var kv in values)
            {
                int val = kv.Value;
                var rt = kv.Key;
                int tribeId = (int)rt;

                List<CardModel> crisisPool = null;

                if (val <= hotLow)
                {
                    // Muy bajo → buscar cartas de rescate
                    crisisPool = allCards.Where(c =>
                        !c.isTutorial &&
                        c.tribeId == tribeId &&
                        c.tags != null && c.tags.Contains("crisis_low") &&
                        IsCardEligible(c)
                    ).ToList();
                }
                else if (val >= hotHigh)
                {
                    // Muy alto → buscar cartas de castigo
                    crisisPool = allCards.Where(c =>
                        !c.isTutorial &&
                        c.tribeId == tribeId &&
                        c.tags != null && c.tags.Contains("crisis_high") &&
                        IsCardEligible(c)
                    ).ToList();
                }

                if (crisisPool != null && crisisPool.Count > 0)
                {
                    nextCard = crisisPool[UnityEngine.Random.Range(0, crisisPool.Count)];
                    break;
                }
            }
        }


        // 3) Sorteo por tribu
        if (nextCard == null)
        {
            int targetTribe = UnityEngine.Random.Range(0, 5);

            var tribePool = allCards.Where(c =>
                !c.isTutorial &&
                c.tribeId == targetTribe &&
                (c.tags == null || !c.tags.Contains("crisis")) &&
                IsCardEligible(c)
            ).ToList();

            if (tribePool.Count == 0)
            {
                tribePool = allCards.Where(c =>
                    !c.isTutorial &&
                    (c.tags == null || !c.tags.Contains("crisis")) &&
                    IsCardEligible(c)
                ).ToList();

                if (tribePool.Count == 0) return null;
            }

            int totalWeight = tribePool.Sum(c => Mathf.Max(1, c.weight));
            int r = UnityEngine.Random.Range(0, totalWeight);
            int accum = 0;
            foreach (var c in tribePool)
            {
                accum += Mathf.Max(1, c.weight);
                if (r < accum)
                {
                    nextCard = c;
                    break;
                }
            }

            if (nextCard == null) nextCard = tribePool[0];
        }

        if (nextCard != null)
            Debug.Log("Carta seleccionada: " + nextCard.id);

        return nextCard;
    }

    public List<CardModel> GetAll() => new List<CardModel>(allCards);

    // Marcar una carta como ya mostrada para evitar repetirla (útil para endings)
    public void MarkCardShown(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        shownCards.Add(id);
        Debug.Log("DeckLoader: MarkCardShown -> " + id);
    }

    // Métodos para el sistema de guardado
    public List<string> GetShownCards()
    {
        return new List<string>(shownCards);
    }

    public List<string> GetRemainingTutorialCards()
    {
        return tutorialQueue.Select(card => card.id).ToList();
    }

    public void SetShownCards(List<string> shownCardIds)
    {
        shownCards.Clear();
        if (shownCardIds != null)
        {
            foreach (string id in shownCardIds)
                shownCards.Add(id);
        }
    }

    public void SetRemainingTutorialCards(List<string> remainingTutorialIds)
    {
        tutorialQueue.Clear();
        if (remainingTutorialIds != null)
        {
            foreach (string id in remainingTutorialIds)
            {
                var card = GetCardById(id);
                if (card != null && card.isTutorial)
                    tutorialQueue.Enqueue(card);
            }
        }
    }

    // Indica si el tutorial ya no tiene cartas pendientes
    public bool IsTutorialCompleted()
    {
        return tutorialQueue == null || tutorialQueue.Count == 0;
    }

    // Marca el tutorial como completado (vacía la cola)
    public void SetTutorialCompleted(bool completed)
    {
        if (completed)
        {
            tutorialQueue.Clear();
            if (FlagManager.Instance != null)
            {
                FlagManager.Instance.SetFlag("tutorial_completed", true);
            }
        }
    }
}
