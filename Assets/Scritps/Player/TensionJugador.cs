using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TensionJugador : MonoBehaviour
{
    [Header("Distancia al enemigo")]
    [SerializeField] private float distanciaInicio = 25f;
    [SerializeField] private float distanciaMaxima = 4f;
    [SerializeField] private float suavizado = 2f;
    [SerializeField] private float intervaloBusqueda = 0.25f;

    [Header("Vida baja")]
    [SerializeField, Range(0f, 1f)] private float vidaCritica = 0.4f;
    [SerializeField, Range(0f, 1f)] private float tensionConVidaCritica = 0.55f;

    [Header("Latido")]
    [SerializeField] private AudioClip sonidoLatido;
    [SerializeField, Range(0f, 1f)] private float tensionMinimaLatido = 0.1f;
    [SerializeField, Range(0f, 1f)] private float volumenLatidoMinimo = 0.15f;
    [SerializeField, Range(0f, 1f)] private float volumenLatidoMaximo = 0.9f;
    [SerializeField] private float intervaloLento = 1.1f;
    [SerializeField] private float intervaloRapido = 0.5f;

    [Header("Efectos de cámara (Volume global)")]
    [SerializeField] private Volume volumen;
    [SerializeField, Range(0f, 1f)] private float viñetaMaxima = 0.55f;
    [SerializeField, Range(0f, 1f)] private float aberracionMaxima = 0.6f;

    [Header("Linterna")]
    [SerializeField] private Light luzLinterna;
    [SerializeField, Range(0f, 1f)] private float tensionParaParpadeo = 0.5f;
    [SerializeField, Range(0f, 1f)] private float fuerzaParpadeo = 0.7f;
    [SerializeField] private float velocidadParpadeo = 14f;

    [Header("Ambiente")]
    [SerializeField] private AmbienteSonoro ambiente;
    [SerializeField, Range(0f, 1f)] private float silencioAmbiente = 0.6f;

    private PlayerSalud salud;
    private AudioSource fuente;
    private Vignette viñeta;
    private ChromaticAberration aberracion;
    private float viñetaBase;
    private float aberracionBase;
    private float intensidadLinterna;
    private float tension;
    private float tensionObjetivo;
    private float proximaBusqueda;
    private float proximoLatido;

    public float Tension => tension;

    private void Awake()
    {
        salud = GetComponent<PlayerSalud>();

        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
    }

    private void Start()
    {
        if (volumen == null) volumen = FindFirstObjectByType<Volume>();

        if (volumen != null && volumen.profile != null)
        {
            if (volumen.profile.TryGet(out viñeta)) viñetaBase = viñeta.intensity.value;
            if (volumen.profile.TryGet(out aberracion)) aberracionBase = aberracion.intensity.value;
        }

        if (luzLinterna == null) luzLinterna = GetComponentInChildren<Light>(true);
        if (luzLinterna != null) intensidadLinterna = luzLinterna.intensity;

        if (ambiente == null) ambiente = FindFirstObjectByType<AmbienteSonoro>();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada()) return;

        if (Time.time >= proximaBusqueda)
        {
            proximaBusqueda = Time.time + intervaloBusqueda;
            tensionObjetivo = CalcularTension();
        }

        tension = Mathf.Lerp(tension, tensionObjetivo, 1f - Mathf.Exp(-suavizado * Time.deltaTime));

        ActualizarLatido();
        ActualizarCamara();
        ActualizarLinterna();

        if (ambiente != null) ambiente.BajarAmbiente(tension * silencioAmbiente);
    }

    private float CalcularTension()
    {
        float resultado = 0f;

        EnemigoPersigue[] enemigos = FindObjectsByType<EnemigoPersigue>(FindObjectsSortMode.None);
        float mejor = float.MaxValue;

        for (int i = 0; i < enemigos.Length; i++)
        {
            float distancia = Vector3.Distance(transform.position, enemigos[i].transform.position);
            if (distancia < mejor) mejor = distancia;
        }

        if (mejor < float.MaxValue)
            resultado = Mathf.InverseLerp(distanciaInicio, distanciaMaxima, mejor);

        if (salud != null && !salud.EstaMuerto && salud.Porcentaje <= vidaCritica)
            resultado = Mathf.Max(resultado, tensionConVidaCritica);

        return resultado;
    }

    private void ActualizarLatido()
    {
        if (sonidoLatido == null || tension < tensionMinimaLatido || Time.time < proximoLatido) return;

        float k = Mathf.InverseLerp(tensionMinimaLatido, 1f, tension);
        fuente.PlayOneShot(sonidoLatido, Mathf.Lerp(volumenLatidoMinimo, volumenLatidoMaximo, k));
        proximoLatido = Time.time + Mathf.Lerp(intervaloLento, intervaloRapido, k);
    }

    private void ActualizarCamara()
    {
        if (viñeta != null)
            viñeta.intensity.Override(Mathf.Lerp(viñetaBase, Mathf.Max(viñetaBase, viñetaMaxima), tension));

        if (aberracion != null)
            aberracion.intensity.Override(Mathf.Lerp(aberracionBase, Mathf.Max(aberracionBase, aberracionMaxima), tension));
    }

    private void ActualizarLinterna()
    {
        if (luzLinterna == null || !luzLinterna.enabled) return;

        float exceso = Mathf.InverseLerp(tensionParaParpadeo, 1f, tension);
        float ruido = Mathf.PerlinNoise(Time.time * velocidadParpadeo, 7.3f);
        float caida = exceso * fuerzaParpadeo * (1f - ruido) * (1f - ruido);

        luzLinterna.intensity = intensidadLinterna * (1f - caida);
    }
}
