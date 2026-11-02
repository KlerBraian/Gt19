using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSalud : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    [Tooltip("Cuánta vida saca cada golpe del enemigo (34 = muere al tercer golpe)")]
    [SerializeField] private float dañoPorGolpe = 34f;
    [Tooltip("Segundos en los que no puede recibir otro golpe después de uno")]
    [SerializeField] private float invulnerabilidad = 0.5f;

    [Header("HUD de vida")]
    [Tooltip("Un Slider de UI (sin Handle e Interactable apagado)")]
    [SerializeField] private Slider barraVida;
    [Tooltip("Opcional: texto tipo '80/100'")]
    [SerializeField] private TextMeshProUGUI textoVida;
    [Tooltip("Qué tan rápido se vacía la barra al recibir daño (unidades de vida por segundo). Menor = más lento.")]
    [SerializeField] private float velocidadBarra = 40f;

    [Header("Efecto de daño")]
    [Tooltip("Opcional: una Image roja a pantalla completa (con Raycast Target apagado). Parpadea al recibir un golpe.")]
    [SerializeField] private Image pantallaDaño;
    [SerializeField, Range(0f, 1f)] private float intensidadDaño = 0.45f;
    [SerializeField] private float velocidadDesvanecer = 1.5f;
    [SerializeField] private AudioClip sonidoDaño;

    private float vidaActual;
    private float finInvulnerabilidad;
    private float valorMostrado;

    public float DañoPorGolpe => dañoPorGolpe;
    public bool EstaMuerto => vidaActual <= 0f;
    public float Porcentaje => vidaMaxima > 0f ? vidaActual / vidaMaxima : 0f;

    private void Awake()
    {
        vidaActual = vidaMaxima;
        valorMostrado = vidaMaxima;

        if (barraVida != null)
        {
            barraVida.wholeNumbers = false;
            barraVida.minValue = 0f;
            barraVida.maxValue = vidaMaxima;
            barraVida.value = vidaMaxima;
        }
        else
        {
            Debug.LogWarning("[PlayerSalud] Falta arrastrar el Slider al campo 'Barra Vida'.", this);
        }

        if (pantallaDaño != null)
        {
            Color c = pantallaDaño.color;
            c.a = 0f;
            pantallaDaño.color = c;
        }

        enabled = false;
    }

    private void Start()
    {
        ActualizarHUD();
    }

    private void Update()
    {
        bool sigue = false;

        if (barraVida != null && valorMostrado != vidaActual)
        {
            valorMostrado = Mathf.MoveTowards(valorMostrado, vidaActual, velocidadBarra * Time.deltaTime);
            barraVida.value = valorMostrado;
            sigue = valorMostrado != vidaActual;
        }

        if (pantallaDaño != null && pantallaDaño.color.a > 0f)
        {
            Color c = pantallaDaño.color;
            c.a = Mathf.MoveTowards(c.a, 0f, velocidadDesvanecer * Time.deltaTime);
            pantallaDaño.color = c;
            sigue = sigue || c.a > 0f;
        }

        if (!sigue) enabled = false;
    }

    public bool RecibirDaño(float cantidad)
    {
        if (EstaMuerto || cantidad <= 0f) return false;
        if (Time.time < finInvulnerabilidad) return false;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada()) return false;

        finInvulnerabilidad = Time.time + invulnerabilidad;
        vidaActual = Mathf.Max(0f, vidaActual - cantidad);
        ActualizarHUD();

        if (sonidoDaño != null)
            AudioSource.PlayClipAtPoint(sonidoDaño, transform.position);

        if (pantallaDaño != null)
        {
            Color c = pantallaDaño.color;
            c.a = intensidadDaño;
            pantallaDaño.color = c;
            enabled = true;
        }

        if (vidaActual <= 0f)
        {
            if (gm != null) gm.JugadorMurio();
            else Debug.LogWarning("[PlayerSalud] Murió, pero no hay GameManager en la escena para cargar la derrota.", this);
        }

        return true;
    }

    public void Curar(float cantidad)
    {
        if (EstaMuerto || cantidad <= 0f) return;

        vidaActual = Mathf.Min(vidaMaxima, vidaActual + cantidad);
        ActualizarHUD();
    }

    [ContextMenu("Probar: recibir daño")]
    private void ProbarDaño() => RecibirDaño(dañoPorGolpe);

    [ContextMenu("Probar: curar 20")]
    private void ProbarCurar() => Curar(20f);

    private void ActualizarHUD()
    {
        if (barraVida != null && valorMostrado != vidaActual)
            enabled = true;

        if (textoVida != null)
            textoVida.text = $"{Mathf.CeilToInt(vidaActual)}/{Mathf.CeilToInt(vidaMaxima)}";
    }
}