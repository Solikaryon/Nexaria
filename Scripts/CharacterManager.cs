using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CharacterManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterEntry
    {
        public string name;      // Nombre que vendrá del JSON
        public GameObject image; // El objeto UI (la carta/imagen)
    }

    public List<CharacterEntry> characters = new List<CharacterEntry>();
    private GameObject currentActive;

    void Awake()
    {
        // Mantener este objeto entre escenas
        DontDestroyOnLoad(gameObject);

        // Volver a reconectar al cargar una nueva escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 🔁 Intentar reconectar las imágenes automáticamente
        foreach (var entry in characters)
        {
            if (entry.image == null)
            {
                var found = GameObject.Find(entry.name);
                if (found != null)
                {
                    entry.image = found;
                    Debug.Log($"✅ Reasignado personaje '{entry.name}' a objeto {found.name}");
                }
                else
                {
                    Debug.LogWarning($"⚠ No se encontró GameObject para el personaje '{entry.name}' en la escena {scene.name}");
                }
            }
        }
    }

    public void ShowCharacter(string name)
    {
        // Apagar el que estaba activo
        if (currentActive != null)
        {
            currentActive.SetActive(false);
            Debug.Log("Desactivando personaje anterior: " + currentActive.name);
        }

        // Buscar personaje por nombre
        CharacterEntry entry = characters.Find(c => c.name == name);
        if (entry != null && entry.image != null)
        {
            entry.image.SetActive(true);
            currentActive = entry.image;
            Debug.Log("Mostrando personaje: " + name);
        }
        else
        {
            Debug.LogWarning("No se encontró personaje con el nombre: " + name);
        }
    }
}
