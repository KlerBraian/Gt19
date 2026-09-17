using Unity.VisualScripting;
using UnityEngine;

public class CamaraTerceraPersona : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("El jugador, si lo dejas vacio, busca el objeto con el tag 'Player'.")]
    [SerializeField] private Transform objetivo;
    [Tooltip("Desplazamiento respecto al pivote del jugador (subilo para mirar a la altura de la cabeza).")]
    [SerializeField] private Vector3 offsetObjetivo = new Vector3 (0f, 1.6f, 0);

    [Header("Distancia")]
    [SerializeField] private float distancia = 4.5f;
    [SerializeField] private float distanciaMinima = 1f;

    [Header("Sensibilidad")]
    [SerializeField] private float sensibilidadMouse = 3f;
    [SerializeField] private float sensiblidadJoystick = 150f;
    [SerializeField] private bool invertirY = false;

    [Header("Limites verticales")]
    [SerializeField] private float anguloMinimoY = -30f;
    [SerializeField] private float anguloMaximoY = 70f;

    [Header("Colision con el entorno")]
    [SerializeField] private bool evitarParedes = true;
    [SerializeField] private LayerMask capaObstaculos = ~0;
    [Tooltip("Margen para que la camara no quede pegada exactamente a la pared.")]
    [SerializeField] private float margenColision = 0.2f;

    [Header("Ejes de joystick (Input Manager)")]
    [Tooltip("Nombre del eje horizontal del stick derecho, hay que crearlo en edit > Project Settings > Input Manager.")]
    [SerializeField] private string nombreEjeXJoystick = "RightStickX";
    [SerializeField] private string nombreEjeYJoystick = "RightStickY";

    [Header("Mouse")]
    [SerializeField] private bool bloquearCursor = true;

    private float anguloX;
    private float anguloY = 15f;
    private bool ejesJoystickDisponibles = true;
    
    
    
    
    private void Start()
    {
        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador == null) objetivo = jugador.transform;
        }

        if (bloquearCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (objetivo != null)
            anguloX = objetivo.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        if (GameManager.Instance != null && GameManager.Instance.EstaTerminada())
        {
            LiberarCursor();
            return;
        }

        LeerInput();
        PosicionarCamara();

    }
    private void LeerInput()
    {
        float entradaX = Input.GetAxis("Mouse X") * sensibilidadMouse;
        float entradaY = Input.GetAxis("Mouse Y") * sensibilidadMouse;

        if (ejesJoystickDisponibles)
        {
            try
            {
                entradaX += Input.GetAxis(nombreEjeXJoystick) * sensiblidadJoystick * Time.deltaTime;
                entradaY += Input.GetAxis(nombreEjeYJoystick) * sensiblidadJoystick * Time.deltaTime;
            }
            catch (System.ArgumentException)
            {
                ejesJoystickDisponibles = false;
                Debug.LogWarning($"[CamaraTerceraPersona] Los ejes '{nombreEjeXJoystick}' y/o" + $"'{nombreEjeYJoystick}' no estan configurados en el Input Manager.");
            }
        }
        anguloX += entradaX;
        anguloY += invertirY ? entradaY : -entradaY;

        anguloY = Mathf.Clamp(anguloY, anguloMinimoY, anguloMaximoY);
    }

    private void PosicionarCamara()
    {
        Vector3 puntoFoco = objetivo.position + offsetObjetivo;
        Quaternion rotacion = Quaternion.Euler(anguloY, anguloX, 0f);

        float distanciaFinal = distancia;

        if (evitarParedes)
        {
            Vector3 direccionCamara = rotacion * Vector3.back;
            if (Physics.Raycast(puntoFoco, direccionCamara, out RaycastHit hit, distancia, capaObstaculos, QueryTriggerInteraction.Ignore))
            {
                distanciaFinal = Mathf.Max(hit.distance - margenColision, distanciaMinima);
            }
        }

        transform.position = puntoFoco + rotacion * Vector3.back * distanciaFinal;
        transform.LookAt(puntoFoco);
    }

    private void LiberarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

}
