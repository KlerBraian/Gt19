using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Coleccionable : MonoBehaviour, IInteractuable
{
    [Header("Texto del prompt")]
    [Tooltip("Ej: 'Presioná E para recoger el tronco'")]
    [SerializeField] private string textoPrompt = "Presioná E para recoger";

    [Header("Opcional")]
    [SerializeField] private AudioClip sonidoAlRecoger;
    [SerializeField] private GameObject efectoParticulas;

    public string TextoPrompt() => textoPrompt;

    public void Interactuar()
    {
        if (sonidoAlRecoger != null)
            AudioSource.PlayClipAtPoint(sonidoAlRecoger, transform.position);

        if (efectoParticulas != null)
            Instantiate(efectoParticulas, transform.position, Quaternion.identity);

        GameManager.Instance.RecolectarMaterialPuente();

        Destroy(gameObject);
    }
}