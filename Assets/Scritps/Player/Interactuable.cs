using UnityEngine;

public enum TipoInteraccion
{
    Tronco,
    Fusible,
    Escape,
    Puerta,
    Soga
}

public class Interactuable : MonoBehaviour
{
    [SerializeField] private TipoInteraccion tipo = TipoInteraccion.Tronco;

    [Header("Prompt")]
    [SerializeField] private string textoPrompt = "Presioná E para interactuar";
    [SerializeField] private string textoPromptCerrar = "Presioná E para cerrar la puerta";

    [Header("Opcional")]
    [SerializeField] private AudioClip sonido;
    [SerializeField] private AudioClip sonidoAlCerrar;
    [SerializeField] private GameObject efectoParticulas;

    [Header("Solo puerta")]
    [SerializeField] private float velocidad = 100f;
    [SerializeField] private float anguloApertura = 90f;

    private bool abierta;
    private float anguloActual;
    private float anguloObjetivo;
    private Quaternion rotacionInicial;

    public TipoInteraccion Tipo => tipo;

    private void Awake()
    {
        rotacionInicial = transform.localRotation;
        enabled = false;
    }

    private void Update()
    {
        anguloActual = Mathf.MoveTowards(anguloActual, anguloObjetivo, velocidad * Time.deltaTime);
        transform.localRotation = rotacionInicial * Quaternion.Euler(0f, anguloActual, 0f);

        if (Mathf.Approximately(anguloActual, anguloObjetivo))
            enabled = false;
    }

    public string ObtenerPrompt()
    {
        return tipo == TipoInteraccion.Puerta && abierta ? textoPromptCerrar : textoPrompt;
    }

    public void Interactuar()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null && tipo != TipoInteraccion.Puerta)
        {
            Debug.LogWarning("[Interactuable] No hay GameManager en la escena.", this);
            return;
        }

        switch (tipo)
        {
            case TipoInteraccion.Puerta:
                AlternarPuerta();
                return;

            case TipoInteraccion.Escape:
                gm.IntentarEscapar();
                return;

            case TipoInteraccion.Fusible:
                gm.RecolectarFusible();
                break;

            case TipoInteraccion.Tronco:
                gm.RecolectarTronco();
                break;

            case TipoInteraccion.Soga:
                gm.RecolectarSoga();
                break;
        }

        Efectos(sonido, true);
        Destroy(gameObject);
    }

    private void AlternarPuerta()
    {
        abierta = !abierta;
        anguloObjetivo = abierta ? anguloApertura : 0f;
        Efectos(abierta ? sonido : sonidoAlCerrar, abierta);
        enabled = true;
    }

    private void Efectos(AudioClip clip, bool conParticulas)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, transform.position);

        if (conParticulas && efectoParticulas != null)
            Instantiate(efectoParticulas, transform.position, Quaternion.identity);
    }
}