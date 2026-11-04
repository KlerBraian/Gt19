using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbienteJuego : MonoBehaviour
{
    [System.Serializable]
    public class CapaAmbiente
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volumen = 0.5f;
    }

    [System.Serializable]
    public class LuzAnimada
    {
        public Light luz;
        [Range(0f, 1f)] public float variacion = 0.25f;
        public float velocidad = 6f;
        public float probabilidadApagon = 0f;
        public Vector2 duracionApagon = new Vector2(0.05f, 0.4f);
        [Range(0f, 1f)] public float intensidadEnApagon = 0.03f;
        public bool variarColor = false;
        public Color colorAlternativo = new Color(1f, 0.35f, 0.08f);
        public float desplazamiento = 0f;
        public AudioSource audioAsociado;

        public bool modoBaliza = false;
        public float periodoBaliza = 1.6f;
        [Range(0.05f, 0.9f)] public float fraccionEncendida = 0.25f;
        public float velocidadSubida = 18f;
        public float velocidadBajada = 6f;
        public Renderer renderEmisivo;
        public Color colorEmision = new Color(1f, 0.05f, 0.02f);
        public float intensidadEmision = 8f;
        public SpriteRenderer halo;
        public float tamañoHaloPorMetro = 0.05f;
        public float tamañoHaloMinimo = 0.8f;
        public float tamañoHaloMaximo = 8f;

        private float intensidadBase;
        private Color colorBase;
        private Vector3 posicionBase;
        private float volumenBase;
        private float semilla;
        private float finApagon;
        private bool lista;
        private Material materialEmisivo;
        private Color emisionBase;
        private bool tieneEmision;
        private float nivelBaliza;
        private Camera camara;

        public void Iniciar()
        {
            if (luz == null) return;

            intensidadBase = luz.intensity;
            colorBase = luz.color;
            posicionBase = luz.transform.localPosition;
            semilla = Random.value * 100f;
            if (audioAsociado != null) volumenBase = audioAsociado.volume;

            camara = Camera.main;

            if (renderEmisivo != null)
            {
                materialEmisivo = renderEmisivo.material;

                if (materialEmisivo.HasProperty("_EmissionColor"))
                {
                    materialEmisivo.EnableKeyword("_EMISSION");
                    emisionBase = materialEmisivo.GetColor("_EmissionColor");
                    tieneEmision = true;
                }
                else
                {
                    Debug.LogWarning("[AmbienteJuego] El material de 'Render Emisivo' no tiene emisión: usá un material URP/Lit con Emission tildado.", renderEmisivo);
                }
            }

            lista = true;
            if (modoBaliza) AplicarBaliza();
        }

        public void Actualizar()
        {
            if (!lista || luz == null) return;

            if (modoBaliza)
            {
                float fase = Mathf.Repeat(Time.time, Mathf.Max(0.1f, periodoBaliza)) / Mathf.Max(0.1f, periodoBaliza);
                float objetivo = fase < fraccionEncendida ? 1f : 0f;
                float ritmo = objetivo > nivelBaliza ? velocidadSubida : velocidadBajada;
                nivelBaliza = Mathf.MoveTowards(nivelBaliza, objetivo, ritmo * Time.deltaTime);
                AplicarBaliza();
                return;
            }

            float tiempo = Time.time * velocidad;
            float ruido = Mathf.PerlinNoise(semilla, tiempo);
            float factor = 1f + (ruido - 0.5f) * 2f * variacion;

            if (Time.time < finApagon)
            {
                factor *= intensidadEnApagon;
            }
            else if (probabilidadApagon > 0f && Random.value < probabilidadApagon * Time.deltaTime)
            {
                finApagon = Time.time + Random.Range(duracionApagon.x, duracionApagon.y);
            }

            factor = Mathf.Max(0f, factor);
            luz.intensity = intensidadBase * factor;

            if (variarColor)
                luz.color = Color.Lerp(colorBase, colorAlternativo, ruido);

            if (desplazamiento > 0f)
            {
                Vector3 salto = new Vector3(
                    Mathf.PerlinNoise(semilla + 10f, tiempo) - 0.5f,
                    Mathf.PerlinNoise(semilla + 20f, tiempo) - 0.5f,
                    Mathf.PerlinNoise(semilla + 30f, tiempo) - 0.5f);
                luz.transform.localPosition = posicionBase + salto * 2f * desplazamiento;
            }

            if (audioAsociado != null)
                audioAsociado.volume = volumenBase * Mathf.Clamp01(factor);
        }

        public void Restaurar()
        {
            if (!lista || luz == null) return;

            luz.intensity = intensidadBase;
            luz.color = colorBase;
            luz.transform.localPosition = posicionBase;
            if (audioAsociado != null) audioAsociado.volume = volumenBase;
            if (tieneEmision) materialEmisivo.SetColor("_EmissionColor", emisionBase);
            if (halo != null) halo.color = new Color(halo.color.r, halo.color.g, halo.color.b, 0f);
        }

        private void AplicarBaliza()
        {
            luz.intensity = intensidadBase * nivelBaliza;

            if (tieneEmision)
                materialEmisivo.SetColor("_EmissionColor", colorEmision * (intensidadEmision * nivelBaliza));

            if (halo == null) return;

            if (camara == null) camara = Camera.main;
            if (camara == null) return;

            Transform t = halo.transform;
            t.rotation = camara.transform.rotation;

            float distancia = Vector3.Distance(camara.transform.position, t.position);
            float escala = Mathf.Clamp(distancia * tamañoHaloPorMetro, tamañoHaloMinimo, tamañoHaloMaximo);
            t.localScale = Vector3.one * escala;

            Color c = halo.color;
            c.a = nivelBaliza;
            halo.color = c;
        }
    }

    [Header("Capas continuas (en loop)")]
    [SerializeField] private CapaAmbiente[] capas;
    [SerializeField] private float segundosEntrada = 4f;
    [SerializeField] private float suavizadoSilencio = 1.5f;

    [Header("Sonidos al azar alrededor del jugador")]
    [SerializeField] private AudioClip[] sonidosAleatorios;
    [SerializeField] private float intervaloMinimo = 12f;
    [SerializeField] private float intervaloMaximo = 35f;
    [SerializeField] private float distanciaMinima = 12f;
    [SerializeField] private float distanciaMaxima = 30f;
    [SerializeField] private float alturaMaxima = 3f;
    [SerializeField, Range(0f, 1f)] private float volumenAleatorio = 0.7f;
    [SerializeField] private int fuentesSimultaneas = 3;

    [Header("Luces que parpadean")]
    [SerializeField] private LuzAnimada[] luces;

    private readonly List<AudioSource> fuentesCapas = new List<AudioSource>();
    private readonly List<float> volumenesCapas = new List<float>();
    private AudioSource[] fuentesAleatorias;
    private int indiceFuente;
    private Transform oyente;
    private float entrada;
    private float silencio;
    private float silencioObjetivo;

    private void Start()
    {
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener != null) oyente = listener.transform;
        else if (Camera.main != null) oyente = Camera.main.transform;

        CrearCapas();
        CrearFuentesAleatorias();

        if (luces != null)
            foreach (LuzAnimada luz in luces)
                if (luz != null) luz.Iniciar();

        StartCoroutine(RutinaAleatoria());
    }

    private void Update()
    {
        entrada = Mathf.MoveTowards(entrada, 1f, Time.deltaTime / Mathf.Max(0.01f, segundosEntrada));
        silencio = Mathf.MoveTowards(silencio, silencioObjetivo, Time.deltaTime * suavizadoSilencio);

        float factor = Mathf.SmoothStep(0f, 1f, entrada) * (1f - silencio);

        for (int i = 0; i < fuentesCapas.Count; i++)
            fuentesCapas[i].volume = volumenesCapas[i] * factor;

        if (luces == null) return;

        foreach (LuzAnimada luz in luces)
            if (luz != null) luz.Actualizar();
    }

    private void OnDisable()
    {
        if (luces == null) return;

        foreach (LuzAnimada luz in luces)
            if (luz != null) luz.Restaurar();
    }

    public void BajarAmbiente(float cantidad)
    {
        silencioObjetivo = Mathf.Clamp01(cantidad);
    }

    private void CrearCapas()
    {
        if (capas == null) return;

        foreach (CapaAmbiente capa in capas)
        {
            if (capa == null || capa.clip == null) continue;

            GameObject objeto = new GameObject("Capa_" + capa.clip.name);
            objeto.transform.SetParent(transform, false);

            AudioSource fuente = objeto.AddComponent<AudioSource>();
            fuente.clip = capa.clip;
            fuente.loop = true;
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
            fuente.volume = 0f;
            fuente.Play();
            fuente.time = Random.Range(0f, capa.clip.length * 0.9f);

            fuentesCapas.Add(fuente);
            volumenesCapas.Add(capa.volumen);
        }
    }

    private void CrearFuentesAleatorias()
    {
        int cantidad = Mathf.Max(1, fuentesSimultaneas);
        fuentesAleatorias = new AudioSource[cantidad];

        for (int i = 0; i < cantidad; i++)
        {
            GameObject objeto = new GameObject("Azar_" + i);
            objeto.transform.SetParent(transform, false);

            AudioSource fuente = objeto.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 1f;
            fuente.rolloffMode = AudioRolloffMode.Linear;
            fuente.minDistance = 10f;
            fuente.maxDistance = Mathf.Max(distanciaMaxima * 2.5f, 40f);
            fuente.dopplerLevel = 0f;

            fuentesAleatorias[i] = fuente;
        }
    }

    private IEnumerator RutinaAleatoria()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervaloMinimo, intervaloMaximo));

            GameManager gm = GameManager.Instance;
            if (gm != null && gm.EstaTerminada()) yield break;

            ReproducirAleatorio();
        }
    }

    private void ReproducirAleatorio()
    {
        if (sonidosAleatorios == null || sonidosAleatorios.Length == 0 || oyente == null) return;

        AudioClip clip = sonidosAleatorios[Random.Range(0, sonidosAleatorios.Length)];
        if (clip == null) return;

        Vector3 direccion = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
        Vector3 posicion = oyente.position + direccion * Random.Range(distanciaMinima, distanciaMaxima)
                           + Vector3.up * Random.Range(0f, alturaMaxima);

        AudioSource fuente = fuentesAleatorias[indiceFuente];
        indiceFuente = (indiceFuente + 1) % fuentesAleatorias.Length;

        fuente.transform.position = posicion;
        fuente.pitch = Random.Range(0.92f, 1.08f);
        fuente.PlayOneShot(clip, volumenAleatorio);
    }
}