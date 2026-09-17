using Unity.VisualScripting;
using UnityEngine;
using System;
using Unity.Mathematics;

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

    [Header("Salto y gravedad")]
    [SerializeField] private bool puedeSaltar = true;
    [SerializeField] private float alturaSalto = 1.5f;
    [SerializeField] private float gravedad = -20f;

    [Header("Chequeo de suelo")]
    [Tooltip("Objeto vacio ubicado a los pies del juegador, si lo dejas vacio, usa la base del CharacterController.")]
    [SerializeField] private Transform chequeoSuelo;
    [SerializeField] private float radioChequeoSuelo = 0.25f;
    [SerializeField] private LayerMask capaSuelo = -0;

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

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.EstaTerminada())
            return;

        ChequearSuelo();
        Mover();
        AplicarGravedadYSalto();

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
        float ejeX = Input.GetAxisRaw("Horizontal");
        float ejeZ = Input.GetAxisRaw("Vertical");

        Vector3 entrada = new Vector3(ejeX, 0f, ejeZ);

        float intesidad = Mathf.Clamp01(entrada.magnitude);
        entrada = entrada.normalized;

        if (intesidad < 0.1f) return;

        float anguloObjetivo = Mathf.Atan2(entrada.x, entrada.z) * Mathf.Rad2Deg;
        if (camaraTransform != null)
            anguloObjetivo += camaraTransform.eulerAngles.y;

        float anguloSuave = Mathf.SmoothDampAngle(transform.eulerAngles.y, anguloObjetivo, ref velocidadRotacion, suavizadoRotacion);
        transform.rotation = Quaternion.Euler(0f, anguloSuave, 0f);

        Vector3 direccion = Quaternion.Euler(0f, anguloObjetivo, 0f) * Vector3.forward;

        bool corriendo = Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Fire3");
        float velocidad = corriendo ? velocidadCorrer : velocidadCaminar;

        controller.Move(direccion * velocidad * intesidad * Time.deltaTime);
    }

    private void AplicarGravedadYSalto()
    {
        if (puedeSaltar && enSuelo && Input.GetButtonDown("Jump"))
        {
            velocidadVertical.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
        }

        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (chequeoSuelo == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(chequeoSuelo.position, radioChequeoSuelo);
    }


}
