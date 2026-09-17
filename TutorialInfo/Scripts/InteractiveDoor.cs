<<<<<<< Updated upstream:TutorialInfo/Scripts/InteractiveDoor.cs
using UnityEngine;

public class InteractiveDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 90f;
    public float rotationSpeed = 3f;

    private bool isOpen = false;
    private bool playerNearby = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        // Store the starting rotation as the "closed" position
        closedRotation = transform.rotation;

        // Calculate the "open" rotation around the Y axis
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
    }

    void Update()
    {
        // Check if the player is nearby and presses the interaction key (e.g., "E")
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen;
        }

        // Smoothly rotate the door toward the target position
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    // Detect when the player enters the trigger zone
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
        }
    }

    // Detect when the player leaves the trigger zone
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
        }
    }
}
=======
using UnityEngine;

public class InteractiveDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 90f;
    public float rotationSpeed = 3f;

    private bool isOpen = false;
    private bool playerNearby = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        
        closedRotation = transform.rotation;

        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
    }

    void Update()
    {
        
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen;
        }

       
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
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
}
>>>>>>> Stashed changes:Assets/Scritps/InteractiveDoor.cs
