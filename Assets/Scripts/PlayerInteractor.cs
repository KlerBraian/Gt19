using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactRange = 1.8f;
    [SerializeField] private float interactRadius = 0.7f; 

    void Update()
    {
        
        Vector3 rayStart = transform.position + (transform.forward);
        Ray ray = new Ray(rayStart, transform.forward);
        RaycastHit hit;

        
        if (Physics.SphereCast(ray, interactRadius, out hit, interactRange))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();
                }
            }
        }
    }

   
}