using UnityEngine;

public class Door : MonoBehaviour, IInteractuable
{
    [SerializeField] private string textoPrompt = "Presioná E para abrir la puerta";
    [SerializeField] private AudioClip sonidoAlAbrir;
    [SerializeField] private AudioClip sonidoAlCerrar;
    [SerializeField] private GameObject efectoParticulas;
    [SerializeField] private float velocidad = 100f;
    [SerializeField] private float anguloApertura = 90f;
    private bool abrir = false;
    private float anguloObjetivo = 0f;
    private float anguloActual = 0f;
    private Quaternion rotacionInicial;

    private void Awake()
    {
        rotacionInicial = transform.rotation;
    }

    private void Update()
    {
        anguloActual = Mathf.MoveTowards(anguloActual, anguloObjetivo, velocidad * Time.deltaTime);
        transform.rotation = rotacionInicial * Quaternion.Euler(0f, anguloActual, 0f);
    }

    public string TextoPrompt() => abrir ? "Presiona E para cerrar la puerta" : textoPrompt;

    public void Interactuar()
    {
        abrir = !abrir;
        anguloObjetivo = abrir ? anguloApertura : 0f;

        AudioClip sonido = abrir ? sonidoAlAbrir : sonidoAlCerrar;
        if (sonido != null)
            AudioSource.PlayClipAtPoint(sonido, transform.position);

        if (abrir && efectoParticulas != null)
            Instantiate(efectoParticulas, transform.position, Quaternion.identity);
    }
}