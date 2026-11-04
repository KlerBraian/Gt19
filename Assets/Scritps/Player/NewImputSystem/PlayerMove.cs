using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMove : MonoBehaviour
{
    private CharacterController controller;

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    [Tooltip("Qué tan rápido gira el personaje hacia donde camina (mayor = más rápido)")]
    [SerializeField] private float velocidadGiroPersonaje = 12f;
    private Vector2 moveInput;
    private float currentSpeed;
    private bool isSprinting;

    [Header("Salto")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20;
    private float verticalVelocity;
    [SerializeField] private float fallMultiplier = 1.5f;

    [Header("Cámara orbital")]
    [Tooltip("El objeto 'puntoDeCamara': queda a la altura de la cabeza y la cámara cuelga de él, hacia atrás.")]
    [SerializeField] private Transform cameraPivot;
    [Tooltip("La cámara (hija del pivote). Vacío = la Main Camera.")]
    [SerializeField] private Transform camara;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [Tooltip("Cuánto puede mirar hacia arriba (negativo = la cámara baja)")]
    [SerializeField] private float anguloMinimoCamara = -25f;
    [Tooltip("Cuánto puede mirar hacia abajo (la cámara sube)")]
    [SerializeField] private float anguloMaximoCamara = 65f;
    [Tooltip("Opcional: apunta a donde mira la cámara. Usalo como origen del rayo del interactor.")]
    [SerializeField] private Transform ojosTransform;

    [Header("Colisión de la cámara")]
    [SerializeField] private bool evitarParedes = true;
    [SerializeField] private float radioCamara = 0.25f;
    [Tooltip("Capas que acercan la cámara. Si la cámara se pega al personaje sin motivo, sacá la capa del Player.")]
    [SerializeField] private LayerMask capaColisionCamara = ~0;
    [SerializeField] private float suavizadoAcercamiento = 15f;

    [Header("Zoom (rueda del mouse)")]
    [SerializeField] private bool permitirZoom = true;
    [Tooltip("Lo más cerca que puede quedar la cámara del personaje")]
    [SerializeField] private float distanciaMinima = 1.5f;
    [Tooltip("Lo más lejos que puede quedar")]
    [SerializeField] private float distanciaMaxima = 7f;
    [Tooltip("Cuánto se acerca o se aleja con cada tick de la rueda")]
    [SerializeField] private float pasoZoom = 0.6f;

    private float yaw;
    private float pitch;
    private Vector2 lookInput;
    private Vector3 direccionLocalCamara;
    private float distanciaObjetivo;
    private float distanciaActual;
    private bool camaraLista;
    private GameManager gameManager;
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

        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;

        if (cameraPivot == null)
        {
            Debug.LogWarning("[PlayerMove] Falta arrastrar 'puntoDeCamara' al campo Camera Pivot.", this);
            return;
        }
        yaw = cameraPivot.eulerAngles.y;
        pitch = cameraPivot.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        if (camara != null && camara.IsChildOf(cameraPivot))
        {
            Vector3 offset = camara.localPosition;
            distanciaObjetivo = Mathf.Clamp(offset.magnitude, distanciaMinima, distanciaMaxima);
            distanciaActual = distanciaObjetivo;

            if (offset.magnitude < 0.5f)
            {
                Debug.LogWarning("[PlayerMove] La cámara está pegada al pivote. Para tercera persona ponele en Position un Z negativo, por ejemplo (0, 0.5, -4).", camara);
                direccionLocalCamara = Vector3.back;
            }
            else
            {
                direccionLocalCamara = offset.normalized;
            }

            camaraLista = true;
        }
        else
        {
            Debug.LogWarning("[PlayerMove] La cámara tiene que ser hija de 'puntoDeCamara' para orbitar.", this);
        }
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
        if (context.performed && controller.isGrounded)
        {
            HandleJump();
            if (animator != null) animator.SetTrigger("Jump");
        }
    }

    ////////////////////////// UPDATE //////////////////////////////////////

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada()) return;

        HandleLook();
        HandleZoom();
        HandleMove();
        HandleGravity();
        if (animator != null) animator.SetBool("IsGrounded", controller.isGrounded);
    }

    private void HandleZoom()
    {
        if (!permitirZoom || !camaraLista) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        float rueda = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(rueda, 0f)) return;

        distanciaObjetivo = Mathf.Clamp(distanciaObjetivo - Mathf.Sign(rueda) * pasoZoom,
                                        distanciaMinima, distanciaMaxima);
    }

    private void LateUpdate()
    {
        if (cameraPivot == null) return;

        Quaternion rotacion = Quaternion.Euler(pitch, yaw, 0f);
        cameraPivot.rotation = rotacion;

        if (ojosTransform != null)
            ojosTransform.rotation = rotacion;

        if (camaraLista)
            AcomodarCamara();
    }

    private void AcomodarCamara()
    {
        float distanciaLibre = distanciaObjetivo;

        if (evitarParedes)
        {
            Vector3 direccionMundo = cameraPivot.TransformDirection(direccionLocalCamara);

            if (Physics.SphereCast(cameraPivot.position, radioCamara, direccionMundo, out RaycastHit hit,
                                   distanciaObjetivo, capaColisionCamara, QueryTriggerInteraction.Ignore))
            {
                distanciaLibre = Mathf.Max(hit.distance - 0.05f, 0.4f);
            }
        }

        float velocidad = distanciaLibre < distanciaActual ? suavizadoAcercamiento * 2f : suavizadoAcercamiento;
        distanciaActual = Mathf.Lerp(distanciaActual, distanciaLibre, 1f - Mathf.Exp(-velocidad * Time.deltaTime));

        camara.localPosition = direccionLocalCamara * distanciaActual;
    }

    private void HandleLook()
    {

        yaw += lookInput.x * mouseSensitivity;
        pitch -= lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, anguloMinimoCamara, anguloMaximoCamara);

        lookInput = Vector2.zero;
    }

    private void HandleJump()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        float currentGravity = gravity;
        if (verticalVelocity < 0f)
        {
            currentGravity *= fallMultiplier;
        }

        verticalVelocity += currentGravity * Time.deltaTime;
    }

    private void HandleMove()
    {

        Quaternion giroCamara = Quaternion.Euler(0f, yaw, 0f);
        Vector3 direccion = giroCamara * new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveInput.magnitude > 0.1f)
        {
            currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            Quaternion objetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, objetivo,
                                                  1f - Mathf.Exp(-velocidadGiroPersonaje * Time.deltaTime));
        }
        else
        {
            currentSpeed = 0f;
            direccion = Vector3.zero;
        }

        Vector3 finalMovement = direccion.normalized * currentSpeed;
        finalMovement.y = verticalVelocity;

        controller.Move(finalMovement * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat("Walk", currentSpeed);
            animator.SetBool("isRun", currentSpeed == sprintSpeed);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Rio"))
        {      
            gameManager.JugadorMurio();
          
        }
    }
  
}