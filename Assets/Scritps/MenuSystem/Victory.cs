using UnityEngine;
using UnityEngine.SceneManagement;

public class Victory : MonoBehaviour
{
   
    [SerializeField] private string victory = "Victory";

    private bool youWon = false;
    private bool playerNearby = false; 
    void Update()
    {
       
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            YouWin();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
        }
    }

    private void YouWin()
    {
       
        if (!youWon)
        {
            youWon = true;
            SceneManager.LoadScene(victory);
        }
    }
}
