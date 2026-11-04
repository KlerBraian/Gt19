using UnityEngine;

public enum TipoInteraccion
{
    MaterialPuente, // tronco o cuerda: suma al contador del puente
    Fusible,        // habilita el escape en la torre
    Escape,         // botón de la torre: gana si tenés el fusible
    Puerta          // se abre y se cierra
}
public class Interactuable : MonoBehaviour
{
    [SerializeField] private TipoInteraccion tipo = TipoInteraccion.MaterialPuente;

    [Header("Prompt")]
    [SerializeField] private string textoPrompt = "Presioná E para interactuar";
    [Tooltip("Solo puerta: texto mientras está abierta")]
    [SerializeField] private string textoPromptCerrar = "Presioná E para cerrar la puerta";

    [Header("Opcional")]
    [Tooltip("Sonido al recoger, usar o abrir")]
    [SerializeField] private AudioClip sonido;
    [Tooltip("Solo puerta: sonido al cerrar")]
    [SerializeField] private AudioClip sonidoAlCerrar;
    [SerializeField] private GameObject efectoParticulas;

    [Header("Solo puerta")]
    [Tooltip("Grados por segundo")]
    [SerializeField] private float velocidad = 100f;
    [Tooltip("Cuántos grados gira al abrirse (probá con -90 si abre para el lado equivocado)")]
    [SerializeField] private float anguloApertura = 90f;

    private bool abierta;
    private float anguloActual;
    private float anguloObjetivo;
    private Quaternion rotacionInicial;

    private void Awake()
    {
        rotacionInicial = transform.localRotation;

        enabled = false;
    }

    private void Update()
    {
        anguloActual = Mathf.MoveTowards(anguloActual, anguloObjetivo, velocidad * Time.deltaTime);
        transform.localRotation = rotacionInicial * Quaternion.Euler(0f, 0f, anguloActual);

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
        }
        if (tipo == TipoInteraccion.Fusible)
            gm.RecolectarFusible();
        else
            gm.RecolectarMaterialPuente();

        Efectos(sonido, true);
        Destroy(gameObject);
    }

    private void AlternarPuerta()
    {
        abierta = !abierta;
        anguloObjetivo = abierta ? anguloApertura : 0f;
        Efectos(abierta ? sonido : sonidoAlCerrar, abierta);
        enabled = true; // arranca el Update para animar el giro
    }

    private void Efectos(AudioClip clip, bool conParticulas)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, transform.position);

        if (conParticulas && efectoParticulas != null)
            Instantiate(efectoParticulas, transform.position, Quaternion.identity);
    }
}
