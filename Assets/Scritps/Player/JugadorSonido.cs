using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CharacterController))]
public class JugadorSonido : MonoBehaviour
{
    [Header("Pasos")]
    [SerializeField] private AudioClip[] pasosTierra;
    [SerializeField] private AudioClip[] pasosMadera;
    [SerializeField] private float distanciaPasoCaminando = 1.6f;
    [SerializeField] private float distanciaPasoCorriendo = 2.1f;
    [SerializeField] private float velocidadMinima = 0.3f;
    [SerializeField] private float velocidadCorrer = 4.5f;
    [SerializeField] private Vector2 variacionTono = new Vector2(0.92f, 1.08f);
    [SerializeField, Range(0f, 1f)] private float volumenCaminando = 0.45f;
    [SerializeField, Range(0f, 1f)] private float volumenCorriendo = 0.8f;
    [SerializeField] private float duracionMaximaPaso = 0.35f;
    [SerializeField] private float inicioClipPaso = 0f;
    [SerializeField, Range(0f, 1f)] private float fraccionAlArrancar = 0.85f;

    [Header("Superficie")]
    [SerializeField] private string tagMadera = "Madera";
    [SerializeField] private LayerMask capasSuelo = ~0;
    [SerializeField] private float alturaRayo = 0.3f;
    [SerializeField] private float distanciaRayo = 1.5f;

    [Header("Aterrizaje y respiración")]
    [SerializeField] private AudioClip sonidoAterrizaje;
    [SerializeField] private AudioClip sonidoJadeo;
    [SerializeField, Range(0f, 1f)] private float volumenAterrizaje = 0.9f;
    [SerializeField, Range(0f, 1f)] private float volumenJadeo = 0.5f;
    [SerializeField] private float velocidadCaidaMinima = 4f;
    [SerializeField] private float segundosCorriendoParaJadear = 4f;
    [SerializeField] private float intervaloJadeo = 4f;

    [Header("Tensión: distancia al enemigo")]
    [SerializeField] private float distanciaInicioTension = 25f;
    [SerializeField] private float distanciaTensionMaxima = 4f;
    [SerializeField] private float suavizado = 2f;
    [SerializeField] private float intervaloBusqueda = 0.25f;

    [Header("Tensión: vida baja")]
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
    [SerializeField] private AmbienteJuego ambiente;
    [SerializeField, Range(0f, 1f)] private float silencioAmbiente = 0.6f;

    private CharacterController controller;
    private PlayerSalud salud;
    private AudioSource fuentePasos;
    private AudioSource fuenteEfectos;
    private AudioSource fuenteLatido;
    private AudioClip ultimoClip;
    private float finPaso;
    private float volumenPasoActual;
    private float distanciaAcumulada;
    private float ultimoMomentoEnSuelo;
    private float caidaMaxima;
    private float tiempoCorriendo;
    private float proximoJadeo;
    private bool tocabaSuelo;

    private Vignette viñeta;
    private ChromaticAberration aberracion;
    private float viñetaBase;
    private float aberracionBase;
    private float intensidadLinterna;
    private float tension;
    private float tensionObjetivo;
    private float proximaBusqueda;
    private float proximoLatido;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        salud = GetComponent<PlayerSalud>();

        fuentePasos = gameObject.AddComponent<AudioSource>();
        fuentePasos.playOnAwake = false;
        fuentePasos.spatialBlend = 0f;

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;
        fuenteEfectos.spatialBlend = 0f;

        fuenteLatido = gameObject.AddComponent<AudioSource>();
        fuenteLatido.playOnAwake = false;
        fuenteLatido.spatialBlend = 0f;
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

        if (ambiente == null) ambiente = FindFirstObjectByType<AmbienteJuego>();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada()) return;

        RecortarPaso();
        ActualizarPasos();
        ActualizarTension();
    }

    private void RecortarPaso()
    {
        if (finPaso <= 0f || !fuentePasos.isPlaying) return;

        float restante = finPaso - Time.time;

        if (restante <= 0f)
        {
            fuentePasos.Stop();
            finPaso = 0f;
            return;
        }

        fuentePasos.volume = volumenPasoActual * Mathf.Clamp01(restante / 0.05f);
    }

    private void ActualizarPasos()
    {
        bool tocaSuelo = controller.isGrounded;
        Vector3 velocidad = controller.velocity;
        float vertical = velocidad.y;
        velocidad.y = 0f;
        float rapidez = velocidad.magnitude;

        if (tocaSuelo) ultimoMomentoEnSuelo = Time.time;
        bool enSuelo = Time.time - ultimoMomentoEnSuelo < 0.15f;

        if (!tocaSuelo)
        {
            caidaMaxima = Mathf.Max(caidaMaxima, -vertical);
        }
        else if (!tocabaSuelo)
        {
            if (caidaMaxima >= velocidadCaidaMinima) Aterrizar();
            caidaMaxima = 0f;
        }
        tocabaSuelo = tocaSuelo;

        bool corriendo = rapidez >= velocidadCorrer;

        if (enSuelo && rapidez > velocidadMinima)
        {
            distanciaAcumulada += rapidez * Time.deltaTime;
            float largoPaso = corriendo ? distanciaPasoCorriendo : distanciaPasoCaminando;

            if (distanciaAcumulada >= largoPaso)
            {
                distanciaAcumulada = 0f;
                ReproducirPaso(corriendo ? volumenCorriendo : volumenCaminando);
            }
        }
        else if (rapidez <= velocidadMinima)
        {
            distanciaAcumulada = distanciaPasoCaminando * fraccionAlArrancar;
        }

        ActualizarRespiracion(corriendo && enSuelo);
    }

    private void ActualizarRespiracion(bool corriendo)
    {
        if (corriendo) tiempoCorriendo += Time.deltaTime;
        else tiempoCorriendo = Mathf.Max(0f, tiempoCorriendo - Time.deltaTime * 2f);

        if (sonidoJadeo == null) return;

        if (tiempoCorriendo >= segundosCorriendoParaJadear && Time.time >= proximoJadeo)
        {
            proximoJadeo = Time.time + intervaloJadeo;
            fuenteEfectos.PlayOneShot(sonidoJadeo, volumenJadeo);
        }
    }

    private void Aterrizar()
    {
        if (sonidoAterrizaje == null) return;
        fuenteEfectos.PlayOneShot(sonidoAterrizaje, volumenAterrizaje);
    }

    private void ReproducirPaso(float volumenPaso)
    {
        AudioClip[] lista = EstaEnMadera() && pasosMadera != null && pasosMadera.Length > 0 ? pasosMadera : pasosTierra;
        if (lista == null || lista.Length == 0) return;

        AudioClip clip = lista[Random.Range(0, lista.Length)];
        if (lista.Length > 1 && clip == ultimoClip)
            clip = lista[(System.Array.IndexOf(lista, clip) + 1) % lista.Length];

        ultimoClip = clip;
        volumenPasoActual = volumenPaso;

        fuentePasos.Stop();
        fuentePasos.clip = clip;
        fuentePasos.pitch = Random.Range(variacionTono.x, variacionTono.y);
        fuentePasos.volume = volumenPaso;
        fuentePasos.Play();

        if (inicioClipPaso > 0f)
            fuentePasos.time = Mathf.Min(inicioClipPaso, Mathf.Max(0f, clip.length - 0.05f));

        finPaso = duracionMaximaPaso > 0f ? Time.time + duracionMaximaPaso : 0f;
    }

    private bool EstaEnMadera()
    {
        Vector3 origen = transform.position + Vector3.up * alturaRayo;

        if (!Physics.Raycast(origen, Vector3.down, out RaycastHit golpe, distanciaRayo, capasSuelo, QueryTriggerInteraction.Ignore))
            return false;

        Transform actual = golpe.collider.transform;
        while (actual != null)
        {
            if (actual.tag == tagMadera) return true;
            actual = actual.parent;
        }

        return false;
    }

    private void ActualizarTension()
    {
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
            resultado = Mathf.InverseLerp(distanciaInicioTension, distanciaTensionMaxima, mejor);

        if (salud != null && !salud.EstaMuerto && salud.Porcentaje <= vidaCritica)
            resultado = Mathf.Max(resultado, tensionConVidaCritica);

        return resultado;
    }

    private void ActualizarLatido()
    {
        if (sonidoLatido == null || tension < tensionMinimaLatido || Time.time < proximoLatido) return;

        float k = Mathf.InverseLerp(tensionMinimaLatido, 1f, tension);
        fuenteLatido.PlayOneShot(sonidoLatido, Mathf.Lerp(volumenLatidoMinimo, volumenLatidoMaximo, k));
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