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
    [SerializeField] private Transform ojosTransform;

    private Animator animator;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();

    }
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        animator = GetComponentInChildren<Animator>();
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
            if (animator != null) animator.SetTrigger("Jump");
        }
    }



    ////////////////////////// UPDATE//////////////////////////////////////
    private void Update()
    {
        // Partida terminada (victoria o derrota): el jugador ya no se mueve ni mira.
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada()) return;

        HandleLook();
        HandleMove();
        HandleGravity();
        if (animator != null) animator.SetBool("IsGrounded", controller.isGrounded);
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
        if (moveInput.magnitude > 0f)
        {
            // Si se mueve, decide si corre o camina
            currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        }
        else
        {
            // Si NO se mueve, la velocidad es 0
            currentSpeed = 0f;
        }

        Vector3 finalMovement = horizontalMovement * currentSpeed;

        finalMovement.y = verticalVelocity;

        controller.Move(finalMovement * Time.deltaTime);
        if (animator != null)
        {
            animator.SetFloat("Walk", currentSpeed);
        }
        if (animator != null)
            animator.SetBool("isRun", currentSpeed == sprintSpeed);
    }
    private void HandleLook()   // VISTA CAMARA
    {

        transform.Rotate(
            Vector3.up,
            lookInput.x * mouseSensitivity
        );

        pitch -= lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minLookAngle, maxLookAngle);

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (ojosTransform != null)
        {
            ojosTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        }

        lookInput = Vector2.zero;
    }
}