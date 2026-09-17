using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private string mappruebas = "Game"; 

    public void PlayGame()
    {
        SceneManager.LoadScene(mappruebas);
    }
}