using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactRange = 0.8f;
    [SerializeField] private float interactRadius = 1.1f; 

    void Update()
    {
        
        Vector3 rayStart = transform.position + (transform.forward);
        Ray ray = new Ray(transform.position, transform.forward);
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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 endPoint = transform.position + (transform.forward * interactRange);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
        Gizmos.DrawWireSphere(endPoint, interactRadius);
        Gizmos.DrawLine(transform.position, endPoint);
    }


}