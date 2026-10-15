using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{

    private CharacterController controller;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    private Vector2 moveInput; //REFERENCIA AL MOVIMIENTO
    private float currentSpeed; //VELOCIDAD ACTUAL
    private bool isSprinting; //ESTA CORRIENDO (BOOL)

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20;
    private float verticalVelocity;
    [SerializeField] private float fallMultiplier = 1.5f; // Hace que la caída sea más rápida que el salto


    [Header("Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;
    private Vector2 lookInput;
    private float pitch;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 3f; // Distancia máxima para agarrar objetos
    [SerializeField] private LayerMask interactableLayer; // Capa para filtrar objetos interactuables

    [SerializeField] private PlayerInput playerInput;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();

    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleJump();
        }
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TryInteract();
        }
    }



    private void Update()
    {
        HandleLook();
        HandleMove();
        HandleGravity();
    }


    private void HandleJump()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity
            );
        }
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Si el personaje está cayendo, multiplicamos la gravedad para una caída más fluida y menos flotante
        float currentGravity = gravity;
        if (verticalVelocity < 0f)
        {
            currentGravity *= fallMultiplier;
        }

        verticalVelocity += currentGravity * Time.deltaTime;
    }
    private void HandleMove()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 horizontalMovement = (forward * moveInput.y + right * moveInput.x);
        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 finalMovement = horizontalMovement * currentSpeed;

        // 2. Aplicar gravedad por separado (sin multiplicarlo por currentSpeed)
        finalMovement.y = verticalVelocity;

        controller.Move(finalMovement * Time.deltaTime);
    }

    private void HandleLook()
    {
        // Horizontal: rotate the player
        transform.Rotate(
            Vector3.up,
            lookInput.x * mouseSensitivity
        );

        // Vertical: rotate the camera pivot
        pitch -= lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minLookAngle, maxLookAngle);

        cameraPivot.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);

        // Mouse Delta is a per-frame movement value.
        // Clear it after applying the rotation.
        lookInput = Vector2.zero;
    }
    private void TryInteract()
    {
        // Lanzamos un raycast desde el centro de la cámara hacia enfrente
        Ray ray = new Ray(cameraPivot.position, cameraPivot.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            // Verificamos si golpeó un objeto interactuable (puedes usar un Tag o una interfaz)
            Debug.Log("Objeto tocado: " + hit.collider.gameObject.name);

            // Ejemplo: Destruir el objeto o agregarlo al inventario
            // hit.collider.gameObject.GetComponent<IInteractable>()?.Interact();
        }
    }
}
