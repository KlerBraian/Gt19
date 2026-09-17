using UnityEngine;
using TMPro;

public class InteractorJugador : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Desde dónde sale el rayo. Si lo dejás vacío, usa este mismo transform.")]
    [SerializeField] private Transform origenRaycast;

    [Header("Configuración")]
    [SerializeField] private float distanciaInteraccion = 3f;
    [SerializeField] private LayerMask capaInteractuable = ~0;
    [SerializeField] private KeyCode teclaInteraccion = KeyCode.E;

    [Header("UI")]
    [Tooltip("Texto que muestra el nombre de la acción, ej: 'Presioná E para...'")]
    [SerializeField] private TextMeshProUGUI textoPrompt;
    [Tooltip("Opcional: el punto de mira (crosshair). Se agranda o cambia de color al detectar algo interactuable.")]
    [SerializeField] private UnityEngine.UI.Image crosshair;
    [SerializeField] private Color colorCrosshairNormal = Color.white;
    [SerializeField] private Color colorCrosshairActivo = Color.yellow;

    private IInteractuable objetivoActual;

    private void Awake()
    {
        if (origenRaycast == null) origenRaycast = transform;
        if (textoPrompt != null) textoPrompt.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.EstaTerminada())
        {
            LimpiarObjetivo();
            return;
        }

        DetectarInteractuable();

        if (objetivoActual != null && Input.GetKeyDown(teclaInteraccion))
        {
            objetivoActual.Interactuar();
        }
    }

    private void DetectarInteractuable()
    {
        Ray rayo = new Ray(origenRaycast.position, origenRaycast.forward);

        if (Physics.Raycast(rayo, out RaycastHit hit, distanciaInteraccion, capaInteractuable,
                            QueryTriggerInteraction.Collide))
        {
            if (hit.collider.TryGetComponent<IInteractuable>(out IInteractuable interactuable))
            {
                MostrarObjetivo(interactuable);
                return;
            }
        }

        LimpiarObjetivo();
    }

    private void MostrarObjetivo(IInteractuable nuevo)
    {
        objetivoActual = nuevo;

        if (textoPrompt != null)
        {
            textoPrompt.text = nuevo.TextoPrompt();
            textoPrompt.gameObject.SetActive(true);
        }

        if (crosshair != null) crosshair.color = colorCrosshairActivo;
    }

    private void LimpiarObjetivo()
    {
        if (objetivoActual == null) return;

        objetivoActual = null;
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