using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class InteraccionE : MonoBehaviour, IInteractuable
{
    [Tooltip("Texto que ve el jugador en el prompt, ej: 'Presioná E para activar la radio'")]
    [SerializeField] private string textoPrompt = "Presioná E para interactuar";

    [Header("Evento")]
    public UnityEvent OnInteractuar;

    public string TextoPrompt() => textoPrompt;

    public void Interactuar()
    {
        OnInteractuar?.Invoke();
    }
}