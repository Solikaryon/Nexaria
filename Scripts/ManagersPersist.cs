using UnityEngine;

public class ManagersPersist : MonoBehaviour
{
    private static ManagersPersist instance;

    void Awake()
    {
        // Si no hay una instancia, esta se convierte en la principal
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 👈 mantiene el objeto al cambiar de escena
        }
        else if (instance != this)
        {
            // Si ya hay otro "Managers" persistente, destruye este duplicado
            Destroy(gameObject);
        }
    }
}
