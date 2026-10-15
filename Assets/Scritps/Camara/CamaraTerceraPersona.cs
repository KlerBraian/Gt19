using UnityEngine;

public class CamaraTerceraPersona : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform objetivo;
    [SerializeField] private Vector3 offsetObjetivo = new Vector3(0f, 1.6f, 0f);

    [Header("Distancia")]
    [SerializeField] private float distancia = 4.5f;
    [SerializeField] private float distanciaMinima = 1f;

    [Header("Sensibilidad")]
    [SerializeField] private float sensibilidadMouse = 0.1f;

    [Header("Limites verticales")]
    [SerializeField] private float anguloMinimoY = -30f;
    [SerializeField] private float anguloMaximoY = 70f;

    [Header("Colision con el entorno")]
    [SerializeField] private bool evitarParedes = true;
    [SerializeField] private LayerMask capaObstaculos = ~0;
    [SerializeField] private float margenColision = 0.2f;

    private float anguloX;
    private float anguloY = 15f;

    private void Start()
    {
        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null) objetivo = jugador.transform;
        }

        if (objetivo != null)
        {
            anguloX = objetivo.eulerAngles.y;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        PosicionarCamara();
    }

    public void ProcesarLook(Vector2 lookInput)
    {
        float entradaX = lookInput.x * sensibilidadMouse;
        float entradaY = lookInput.y * sensibilidadMouse;

        anguloX += entradaX;
        anguloY -= entradaY;

        anguloY = Mathf.Clamp(anguloY, anguloMinimoY, anguloMaximoY);

        // Rotamos al jugador horizontalmente para que siempre apunte hacia donde mira la cámara
        if (objetivo != null)
        {
            objetivo.rotation = Quaternion.Euler(0f, anguloX, 0f);
        }
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
}
