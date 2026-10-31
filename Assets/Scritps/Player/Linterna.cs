using UnityEngine;
using UnityEngine.InputSystem;

public class Linterna : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El Spot Light que cuelga de la cámara. Vacío = busca una luz en los hijos de este objeto.")]
    [SerializeField] private Light luz;

    [Header("Control")]
    [SerializeField] private Key tecla = Key.F;
    [SerializeField] private bool empiezaEncendida = false;

    [Header("Sonido")]
    [Tooltip("Click al prender o apagar")]
    [SerializeField] private AudioClip sonidoClick;
    [SerializeField, Range(0f, 1f)] private float volumenClick = 0.6f;

    private bool encendida;

    private void Awake()
    {
        if (luz == null) luz = GetComponentInChildren<Light>(true);

        if (luz == null)
        {
            Debug.LogWarning("[Linterna] No hay ninguna luz asignada. Creá un Spot Light como hijo de la Main Camera y arrastralo al campo 'Luz'.", this);
            enabled = false;
            return;
        }

        Aplicar(empiezaEncendida);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada()) return;

        Keyboard teclado = Keyboard.current;
        if (teclado != null && tecla != Key.None && teclado[tecla].wasPressedThisFrame)
            Alternar();
    }

    public void OnLinterna(InputAction.CallbackContext context)
    {
        if (context.performed) Alternar();
    }

    public void Alternar()
    {
        Aplicar(!encendida);

        if (sonidoClick != null)
            AudioSource.PlayClipAtPoint(sonidoClick, luz.transform.position, volumenClick);
    }

    private void Aplicar(bool prendida)
    {
        encendida = prendida;
        luz.enabled = prendida;
    }
}