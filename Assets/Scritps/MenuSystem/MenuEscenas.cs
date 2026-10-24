using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reemplaza a MenuPrincipal y MenuFinal. Un solo script para las tres
/// escenas de menú (Menú principal, Victoria y Derrota): ponelo en el Canvas
/// y conectá cada botón a la función que corresponda en su OnClick().
///
/// Menú:     Jugar, Salir
/// Victoria / Derrota: Jugar (sirve como "Reintentar"), VolverAlMenu, Salir
/// </summary>
public class MenuEscenas : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena de juego")]
    [SerializeField] private string escenaDeJuego = "Game";
    [Tooltip("Nombre exacto de la escena del menú principal")]
    [SerializeField] private string escenaMenuPrincipal = "MenuPrincipal";

    private void Start()
    {
        // La cámara del juego bloquea el cursor; en los menús tiene que verse.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Jugar() => Cargar(escenaDeJuego);

    public void VolverAlMenu() => Cargar(escenaMenuPrincipal);

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit(); // en el editor no hace nada, solo en el build
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
