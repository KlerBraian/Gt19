using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Fusible : MonoBehaviour, IInteractuable
{
    [SerializeField] private string textoPrompt = "Presioná E para recoger el fusible";
    [SerializeField] private AudioClip sonidoAlRecoger;
    [SerializeField] private GameObject efectoParticulas;

    public string TextoPrompt() => textoPrompt;

    public void Interactuar()
    {
        if (sonidoAlRecoger != null)
            AudioSource.PlayClipAtPoint(sonidoAlRecoger, transform.position);

        if (efectoParticulas != null)
            Instantiate(efectoParticulas, transform.position, Quaternion.identity);

        GameManager.Instance.RecolectarFusible();

        Destroy(gameObject);
    }
}