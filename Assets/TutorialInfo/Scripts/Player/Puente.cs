using UnityEngine;

public class Puente : MonoBehaviour
{
    [Header("Referencias del puente")]
    [Tooltip("Objeto visual del puente ya construido (arranca desactivado)")]
    [SerializeField] private GameObject puenteConstruido;

    [Tooltip("Muro/collider invisible que bloquea el paso antes de construir el puente (opcional)")]
    [SerializeField] private GameObject bloqueoDePaso;

    [Header("Opcional")]
    [SerializeField] private AudioClip sonidoConstruccion;
    [SerializeField] private Animator animator;
    [SerializeField] private string nombreTriggerAnimacion = "Construir";

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Puente] No se encontró GameManager.Instance en Start().");
            return;
        }

        GameManager.Instance.OnPuenteListo += ConstruirPuente;

        if (GameManager.Instance.PuenteEstaHabilitado())
        {
            ConstruirPuente();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPuenteListo -= ConstruirPuente;
    }

    private void ConstruirPuente()
    {
        if (puenteConstruido != null)
        {
            puenteConstruido.SetActive(true);
            Debug.Log($"[Puente] Intentando activar '{puenteConstruido.name}'. " +
                     $"¿Quedó activo?: {puenteConstruido.activeSelf} | " +
                     $"¿Visible en jerarquía?: {puenteConstruido.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[Puente] El campo 'Puente Construido' está vacío (None).");
        }

        if (bloqueoDePaso != null)
            bloqueoDePaso.SetActive(false);

        if (animator != null)
            animator.SetTrigger(nombreTriggerAnimacion);

        if (sonidoConstruccion != null)
            AudioSource.PlayClipAtPoint(sonidoConstruccion, transform.position);

        Debug.Log("Puente construido, el jugador ya puede cruzar.");
    }
}