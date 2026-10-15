using Unity.VisualScripting;
using UnityEngine;
using System;
using Unity.Mathematics;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La camara que sigue al jugador. Si lo dejas vacio, busca la MainCamera.")]
    [SerializeField] private Transform camaraTransform;

    [Header("Moviemiento")]
    [SerializeField] private float velocidadCaminar = 4f;
    [SerializeField] private float velocidadCorrer = 7f;
    [Tooltip("Que tan rapido rota el personaje hacia la direccion de moviemiento (menor = mas rapido).")]
    [SerializeField] private float suavizadoRotacion = 0.1f;
    private Vector2 moveInput; //REFERENCIA AL MOVIMIENTO
    private float currentSpeed; //VELOCIDAD ACTUAL
    private bool isSprinting; //ESTA CORRIENDO (BOOL)

    [Header("Salto y gravedad")]
    [SerializeField] private float alturaSalto = 1.5f;
    [SerializeField] private float gravedad = -20f;
    private float verticalVelocity;
    [SerializeField] private float fallMultiplier = 1.5f; // Hace que la caída sea más rápida que el salto

    [Header("Chequeo de suelo")]
    [Tooltip("Objeto vacio ubicado a los pies del juegador, si lo dejas vacio, usa la base del CharacterController.")]
    [SerializeField] private Transform chequeoSuelo;
    [SerializeField] private float radioChequeoSuelo = 0.25f;
    [SerializeField] private LayerMask capaSuelo = -0;

    [Header("Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;
    private Vector2 lookInput;
    private float pitch;

    private CharacterController controller;
    private Vector3 velocidadVertical;
    private float velocidadRotacion;
    private bool enSuelo;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (camaraTransform == null && Camera.main != null)
            camaraTransform = Camera.main.transform;
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
            //AplicarGravedadYSalto();
        }
    }


    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.EstaTerminada())
            return;

        //ChequearSuelo();
        Mover();
        //AplicarGravedadYSalto();

    }

    private void ChequearSuelo()
    {
        if (chequeoSuelo != null)
        {
            enSuelo = Physics.CheckSphere(chequeoSuelo.position, radioChequeoSuelo, capaSuelo, QueryTriggerInteraction.Ignore);
        }
        else
        {
            enSuelo = controller.isGrounded;
        }

        if (enSuelo && velocidadVertical.y < 0f)
            velocidadVertical.y = -2f;
    }
      private void Mover()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 horizontalMovement = (forward * moveInput.y + right * moveInput.x);
        currentSpeed = isSprinting ? velocidadCorrer : velocidadCaminar;


        Vector3 finalMovement = horizontalMovement * currentSpeed;

        float intensidad = Mathf.Clamp01(finalMovement.magnitude);
        finalMovement = finalMovement.normalized;

        if (intensidad < 0.1f) return;

        // 2. Aplicar gravedad por separado (sin multiplicarlo por currentSpeed)
        finalMovement.y = verticalVelocity;

        controller.Move(finalMovement * intensidad * Time.deltaTime);




    }

    //private void AplicarGravedadYSalto()
    //{
    //    if (puedeSaltar && enSuelo && Input.GetButtonDown("Jump"))
    //    {
    //        velocidadVertical.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
    //    }

    //    velocidadVertical.y += gravedad * Time.deltaTime;
    //    controller.Move(velocidadVertical * Time.deltaTime);
    //}

    //private void OnDrawGizmosSelected()
    //{
    //    if (chequeoSuelo == null) return;
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawWireSphere(chequeoSuelo.position, radioChequeoSuelo);
    //}


}
