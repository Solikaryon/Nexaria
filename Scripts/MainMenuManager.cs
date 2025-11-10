using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject menuUI; // 👈 Asigna aquí el panel del menú en el inspector

    public void NewGame()
    {
        Debug.Log("🎮 Nueva partida iniciada.");

        // 🔹 Borrar guardado previo (si existe GameManager)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DeleteSave();
        }

        // 🔹 Iniciar la partida directamente
        StartGame();
    }

    public void ContinueGame()
    {
        Debug.Log("▶ Continuar partida.");
        StartGame();
    }

    private void StartGame()
    {
        // 🔹 Ocultar menú
        if (menuUI != null)
            menuUI.SetActive(false);
        else
            gameObject.SetActive(false); // Por si no se asignó manualmente

        // 🔹 Aquí puedes agregar cualquier inicialización adicional si es necesario
        if (GameManager.Instance != null)
        {
            // Por ejemplo, cargar datos o mostrar la primera carta
            Debug.Log("Juego iniciado desde el menú principal.");
        }
    }

    public void Options()
    {
        Debug.Log("⚙ Opciones abiertas.");
        // Aquí abre tu panel de opciones dentro del menú principal
    }

    public void QuitGame()
    {
        Debug.Log("❌ Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
