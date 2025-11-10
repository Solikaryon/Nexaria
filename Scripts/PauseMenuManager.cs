using UnityEngine;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public Canvas mainCanvas;
    public GameObject mainMenuUI; // 👈 Asigna aquí tu panel del menú principal
    private bool isPaused = false;
    private GraphicRaycaster mainRaycaster;

    void Awake()
    {
        mainRaycaster = mainCanvas.GetComponent<GraphicRaycaster>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        if (mainRaycaster != null) mainRaycaster.enabled = true;
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        if (mainRaycaster != null) mainRaycaster.enabled = false;

        CanvasGroup panelGroup = pauseMenuUI.GetComponent<CanvasGroup>();
        if (panelGroup != null)
        {
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f;
        isPaused = true;
    }

    // ========== GUARDADO (usa GameManager) ==========
    public void SaveGame()
    {
        GameManager.Instance.SaveGame();
        Debug.Log("✅ Partida guardada desde PauseMenu.");
    }

    public void DeleteSave()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DeleteSave();
        }
        else
        {
            Debug.LogWarning("⚠ No se encontró GameManager al intentar borrar la partida.");
        }
    }

    public void SaveAndQuit()
    {
        SaveGame();
        Debug.Log("💾 Guardar y salir.");
        Application.Quit();
    }

    public void QuitWithoutSaving()
    {
        Debug.Log("❌ Salir sin guardar.");
        Application.Quit();
    }

    // ========== OTROS BOTONES ==========
    public void SetMusicVolume(float value) => Debug.Log("Volumen música: " + value);
    public void SetEffectsVolume(float value) => Debug.Log("Volumen efectos: " + value);

    // ========== VOLVER AL MENÚ PRINCIPAL ==========
    public void LoadMenu()
    {
        Debug.Log("↩ Volviendo al menú principal.");
        Time.timeScale = 1f;

        // 🔹 Ocultar menú de pausa
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        // 🔹 Reactivar el menú principal
        if (mainMenuUI != null)
            mainMenuUI.SetActive(true);
        else
            Debug.LogWarning("⚠ mainMenuUI no asignado en el PauseMenuManager.");

        // 🔹 Rehabilitar el raycaster para que los botones del menú funcionen
        if (mainRaycaster != null)
            mainRaycaster.enabled = true;

        isPaused = false;
    }
}
