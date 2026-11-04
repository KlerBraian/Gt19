using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbienteSonoro : MonoBehaviour
{
    [System.Serializable]
    public class CapaAmbiente
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volumen = 0.5f;
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
        StartCoroutine(RutinaAleatoria());
    }

    private void Update()
    {
        entrada = Mathf.MoveTowards(entrada, 1f, Time.deltaTime / Mathf.Max(0.01f, segundosEntrada));
        silencio = Mathf.MoveTowards(silencio, silencioObjetivo, Time.deltaTime * suavizadoSilencio);

        float factor = Mathf.SmoothStep(0f, 1f, entrada) * (1f - silencio);

        for (int i = 0; i < fuentesCapas.Count; i++)
            fuentesCapas[i].volume = volumenesCapas[i] * factor;
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
