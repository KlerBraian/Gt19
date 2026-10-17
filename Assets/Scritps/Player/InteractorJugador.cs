using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractorJugador : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La cámara principal del juego (para el centro de la pantalla).")]
    [SerializeField] private Camera camaraPrincipal;
    [Tooltip("Objeto que representa los ojos del jugador (donde nace el rayo).")]
    [SerializeField] private Transform ojosTransform;

    [Header("Configuración")]
    [SerializeField] private float distanciaInteraccion = 3f;
    [SerializeField] private LayerMask capaInteractuable;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textoPrompt;
    [SerializeField] private UnityEngine.UI.Image crosshair;
    [SerializeField] private Color colorCrosshairNormal = Color.white;
    [SerializeField] private Color colorCrosshairActivo = Color.yellow;

    private IInteractuable objetivoActual;

    private void Awake()
    {
        if (ojosTransform == null) ojosTransform = transform;

        // Si no asignaste la cámara en el Inspector, la busca automáticamente
        if (camaraPrincipal == null) camaraPrincipal = Camera.main;

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
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && objetivoActual != null)
        {
            objetivoActual.Interactuar();
        }
    }

    private void DetectarInteractuable()
    {
        if (camaraPrincipal == null || ojosTransform == null) return;

        // 1. Lanzamos un rayo desde el centro de la pantalla de la cámara
        Ray screenRay = camaraPrincipal.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        Vector3 puntoDestino;
        if (Physics.Raycast(screenRay, out RaycastHit screenHit, distanciaInteraccion + 2f))
        {
            puntoDestino = screenHit.point;
        }
        else
        {
            puntoDestino = screenRay.GetPoint(distanciaInteraccion);
        }

        // 2. Dirección desde los ojos hacia el punto de la cámara
        Vector3 direccionReal = (puntoDestino - ojosTransform.position).normalized;
        Ray rayoReal = new Ray(ojosTransform.position, direccionReal);

        // 3. Usamos RaycastAll para asegurarnos de ignorar al propio jugador si llegara a cruzarse
        RaycastHit[] hits = Physics.RaycastAll(rayoReal, distanciaInteraccion, capaInteractuable, QueryTriggerInteraction.Collide);

        // Ordenamos los impactos por distancia para tomar el primero que encuentre
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (var hit in hits)
        {
            // Nos aseguramos de que no nos detecte a nosotros mismos (si el jugador tuviera la capa interactuable por error)
            if (hit.transform.root == transform.root) continue;

            if (hit.collider.TryGetComponent<IInteractuable>(out IInteractuable interactable))
            {
                MostrarObjetivo(interactable);
                return;
            }
        }

        LimpiarObjetivo();
    }

    private void OnDrawGizmosSelected()
    {
        if (ojosTransform == null) return;

        Camera cam = camaraPrincipal != null ? camaraPrincipal : Camera.main;
        if (cam != null)
        {
            Ray screenRay = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            Vector3 puntoDestino = Physics.Raycast(screenRay, out RaycastHit screenHit, distanciaInteraccion + 2f)
                ? screenHit.point
                : screenRay.GetPoint(distanciaInteraccion);

            Vector3 direccionReal = (puntoDestino - ojosTransform.position).normalized;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(ojosTransform.position, direccionReal * distanciaInteraccion);
        }
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
}