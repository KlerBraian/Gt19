using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuFinal : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena de juego (debe estar en Build Settings)")]
    [SerializeField] private string escenaDeJuego = "Juego";

    [Tooltip("Nombre exacto de la escena del menú principal (debe estar en Build Settings)")]
    [SerializeField] private string escenaMenuPrincipal = "MenuPrincipal";
    public void Reintentar()
    {
        SceneManager.LoadScene(escenaDeJuego);
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene(escenaMenuPrincipal);
    }


    public void Salir()
    {
        Application.Quit();
    }
}