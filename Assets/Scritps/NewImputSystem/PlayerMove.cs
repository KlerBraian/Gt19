using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]

public class PlayerMove : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La camara que sigue al jugador. Si lo dejas vacio, busca la MainCamera.")]
    private CharacterController controller;

    [Header("Moviemiento")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    [Tooltip("Que tan rapido rota el personaje hacia la direccion de moviemiento (menor = mas rapido).")]
    private Vector2 moveInput; //REFERENCIA AL MOVIMIENTO
    private float currentSpeed; //VELOCIDAD ACTUAL
    private bool isSprinting; //ESTA CORRIENDO (BOOL)

    [Header("Salto")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20;
    private float verticalVelocity;
    [SerializeField] private float fallMultiplier = 1.5f; // Hace que la caída sea más rápida que el salto


    [Header("Vista Camara")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;
    private Vector2 lookInput;
    private float pitch;

    [Header("Interaccion")]
    [SerializeField] private Transform ojosTransform;
    [SerializeField] private float interactRange = 3f; // Distancia máxima para agarrar objetos
    [SerializeField] private LayerMask interactableLayer; // Capa para filtrar objetos interactuables


    private void Awake()
    {
        controller = GetComponent<CharacterController>();

    }
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }




    ///////////////////// UNITY EVENTS ////////////////////////
    
  
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


    ////////////////////////// UPDATE//////////////////////////////////////
    private void Update()
    {
        HandleLook(); 
        HandleMove();
        HandleGravity();
    }


    private void HandleJump() // SALTO
    {
        if (controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity
            );
        }
    }

    private void HandleGravity() //MANEJO DE GRAVEDAD
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
    
    private void HandleMove() //MOVIMIENTO
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

        finalMovement.y = verticalVelocity;

        controller.Move(finalMovement * Time.deltaTime);
    }

    //private void HandleLook()   // VISTA CAMARA
    //{
    //    transform.Rotate(
    //        Vector3.up,
    //        lookInput.x * mouseSensitivity
    //    );

    //    pitch -= lookInput.y * mouseSensitivity;
    //    pitch = Mathf.Clamp(pitch, minLookAngle, maxLookAngle);

    //    cameraPivot.localRotation =
    //        Quaternion.Euler(pitch, 0f, 0f);

    //    lookInput = Vector2.zero;
    //}
    private void HandleLook()   // VISTA CAMARA
    {
        // 1. Rota el cuerpo del jugador horizontalmente (eje Y)
        transform.Rotate(
            Vector3.up,
            lookInput.x * mouseSensitivity
        );

        // 2. Calcula la inclinación vertical (pitch)
        pitch -= lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minLookAngle, maxLookAngle);

        // 3. Aplica la rotación al pivote de la cámara
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // 4. NUEVO: Sincroniza los ojos con la misma rotación vertical (o copia la rotación exacta del pivot)
        if (ojosTransform != null)
        {
            // Opción A: Mantener la rotación horizontal del cuerpo pero aplicar el pitch vertical
            ojosTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // (O puedes usar eyesTransform.rotation = cameraPivot.rotation si quieres que copien absolutamente todo)
        }

        lookInput = Vector2.zero;
    }
    private void TryInteract()
    {
        Ray ray = new Ray(ojosTransform.position, ojosTransform.forward);

        // El LayerMask filtra rápidamente cualquier objeto que no sea interactuable
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            // Buscamos si el objeto tocado tiene la interfaz de interacción
            if (hit.collider.TryGetComponent<IInteractuable>(out IInteractuable interactable))
            {
                interactable.Interactuar(); // Ejecuta la lógica específica de ese objeto (puerta, fusible, etc.)
            }
        }
    }
    private void OnDrawGizmos()
    {
        // Asegúrate de que cameraPivot esté asignado para evitar errores si aún no cargó
        if (ojosTransform != null)
        {
            // Cambia el color del gizmo (por ejemplo, rojo para que se note bien)
            Gizmos.color = Color.red;

            // Dibuja una línea desde la posición del pivote de la cámara en la dirección que apunta
            Gizmos.DrawRay(ojosTransform.position, ojosTransform.forward * interactRange);
        }
    }
}
