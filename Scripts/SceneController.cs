#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReference : MonoBehaviour
{
#if UNITY_EDITOR
    public SceneAsset sceneAsset; // 👈 Aquí puedes arrastrar la escena en el inspector
#endif

    [HideInInspector] 
    public string sceneName; // 👈 Este se guarda con el nombre real

    void OnValidate()
    {
#if UNITY_EDITOR
        if (sceneAsset != null)
            sceneName = sceneAsset.name;
#endif
    }

    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning("⚠ No se asignó ninguna escena.");
    }
}
