using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena de juego (debe estar en Build Settings)")]
    [SerializeField] private string escenaDeJuego = "Juego";
    public void Jugar()
    {
        SceneManager.LoadScene(escenaDeJuego);
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}