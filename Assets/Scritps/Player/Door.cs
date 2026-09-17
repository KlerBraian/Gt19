using UnityEngine;

public class Door : MonoBehaviour, IInteractuable
{
    [SerializeField] private string textoPrompt = "Presioná E para abrir la puerta";
    [SerializeField] private AudioClip sonidoAlAbrir;
    [SerializeField] private GameObject efectoParticulas;
    [SerializeField] private float velocidad = 100f;
    [SerializeField] private float anguloApertura = 90f;
    private bool abrir = false;
    private float anguloActual = 0f;

    public string TextoPrompt() => textoPrompt;

    public void Interactuar()
    {
        Destroy(gameObject);
    }

}