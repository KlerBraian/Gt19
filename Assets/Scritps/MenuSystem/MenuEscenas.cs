using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuEscenas : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena de juego")]
    [SerializeField] private string escenaDeJuego = "Game";
    [Tooltip("Nombre exacto de la escena del menú principal")]
    [SerializeField] private string escenaMenuPrincipal = "MenuPrincipal";

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Jugar() => Cargar(escenaDeJuego);

    public void VolverAlMenu() => Cargar(escenaMenuPrincipal);

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    private void Cargar(string nombre)
    {
        if (!Application.CanStreamedLevelBeLoaded(nombre))
        {
            Debug.LogError($"[MenuEscenas] La escena '{nombre}' no existe o no está en File > Build Profiles. " +
                           "Revisá que el nombre coincida letra por letra.", this);
            return;
        }

        SceneManager.LoadScene(nombre);
    }
}
