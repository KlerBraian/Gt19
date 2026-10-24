using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Va en la Main Camera. Tira un rayo desde el centro de la pantalla, detecta
/// objetos con Interactuable, muestra el prompt y ejecuta la acción al apretar E.
/// </summary>
public class InteractorJugador : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Desde dónde sale el rayo. Vacío = la Main Camera.")]
    [SerializeField] private Transform origenRaycast;

    [Header("Configuración")]
    [SerializeField] private float distanciaInteraccion = 8f;
    [SerializeField] private LayerMask capaInteractuable = ~0;
    [SerializeField] private KeyCode teclaInteraccion = KeyCode.E;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textoPrompt;
    [SerializeField] private Image crosshair;
    [SerializeField] private Color colorCrosshairNormal = Color.white;
    [SerializeField] private Color colorCrosshairActivo = Color.yellow;

    private Interactuable objetivo;
    private bool promptVisible;

    private void Awake()
    {
        if (origenRaycast == null)
            origenRaycast = Camera.main != null ? Camera.main.transform : transform;

        if (textoPrompt != null) textoPrompt.gameObject.SetActive(false);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada())
        {
            objetivo = null;
            OcultarPrompt();
            return;
        }

        Detectar();

        if (objetivo != null && Input.GetKeyDown(teclaInteraccion))
            objetivo.Interactuar();
    }

    private void Detectar()
    {
        objetivo = null;

        if (Physics.Raycast(origenRaycast.position, origenRaycast.forward, out RaycastHit hit,
                            distanciaInteraccion, capaInteractuable, QueryTriggerInteraction.Collide))
        {
            // InParent: el collider puede estar en un hijo (ej. el mesh de la puerta).
            objetivo = hit.collider.GetComponentInParent<Interactuable>();
        }

        if (objetivo != null) MostrarPrompt(objetivo.ObtenerPrompt());
        else OcultarPrompt();
    }

    private void MostrarPrompt(string mensaje)
    {
        if (textoPrompt != null)
        {
            if (!promptVisible) textoPrompt.gameObject.SetActive(true);
            if (textoPrompt.text != mensaje) textoPrompt.text = mensaje;
        }

        if (!promptVisible && crosshair != null) crosshair.color = colorCrosshairActivo;
        promptVisible = true;
    }

    private void OcultarPrompt()
    {
        if (!promptVisible) return;
        promptVisible = false;

        if (textoPrompt != null) textoPrompt.gameObject.SetActive(false);
        if (crosshair != null) crosshair.color = colorCrosshairNormal;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origen = origenRaycast != null ? origenRaycast : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origen.position, origen.position + origen.forward * distanciaInteraccion);
    }
}