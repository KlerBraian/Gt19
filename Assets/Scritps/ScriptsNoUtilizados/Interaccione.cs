using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class InteraccionE : MonoBehaviour, IInteractuable
{
    [Tooltip("Texto que ve el jugador en el prompt, ej: 'Presioná E para activar la radio'")]
    [SerializeField] private string textoPrompt = "Presioná E para interactuar";

    public string TextoPrompt() => textoPrompt;

    public void Interactuar()
    {
        GameManager.Instance.Victoria ();
    }
}