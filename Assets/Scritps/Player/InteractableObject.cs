using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable

{ 

[SerializeField] private string prompt = "Press E to interact";
public string InteractionPrompt => prompt;




   
    public void Interact()
    {
        
        Destroy(gameObject);
    }


}
