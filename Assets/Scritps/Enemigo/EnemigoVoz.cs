using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemigoVoz : MonoBehaviour
{
    [Header("Gruñidos")]
    [SerializeField] private AudioClip[] sonidosGruñido;
    [SerializeField, Range(0f, 1f)] private float volumenGruñido = 0.8f;
    [SerializeField] private Vector2 intervaloPatrulla = new Vector2(7f, 16f);
    [SerializeField] private Vector2 intervaloPersecucion = new Vector2(2.5f, 5f);
    [SerializeField] private float distanciaMaximaVoz = 40f;

    [Header("Respiración (loop)")]
    [SerializeField] private AudioClip respiracion;
    [SerializeField, Range(0f, 1f)] private float volumenRespiracionPatrulla = 0.35f;
    [SerializeField, Range(0f, 1f)] private float volumenRespiracionPersecucion = 0.8f;
    [SerializeField] private float distanciaMaximaRespiracion = 14f;
    [SerializeField] private float velocidadCambioVolumen = 0.6f;

    [Header("Estado")]
    [SerializeField] private float velocidadConsideradaPersecucion = 2.5f;

    private NavMeshAgent agente;
    private AudioSource voz;
    private AudioSource respiro;
    private float proximaVoz;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        voz = CrearFuente("Voz", false, 4f, distanciaMaximaVoz);
        respiro = CrearFuente("Respiracion", true, 2f, distanciaMaximaRespiracion);
        respiro.clip = respiracion;
        respiro.volume = 0f;
    }

    private void OnEnable()
    {
        proximaVoz = Time.time + Random.Range(1f, 3f);

        if (respiro != null && respiracion != null)
        {
            respiro.volume = 0f;
            respiro.Play();
        }
    }

    private void OnDisable()
    {
        if (respiro != null) respiro.Stop();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada())
        {
            respiro.Stop();
            enabled = false;
            return;
        }

        bool persiguiendo = agente.velocity.magnitude >= velocidadConsideradaPersecucion;

        float objetivo = persiguiendo ? volumenRespiracionPersecucion : volumenRespiracionPatrulla;
        respiro.volume = Mathf.MoveTowards(respiro.volume, objetivo, velocidadCambioVolumen * Time.deltaTime);

        if (Time.time < proximaVoz || sonidosGruñido == null || sonidosGruñido.Length == 0) return;

        AudioClip clip = sonidosGruñido[Random.Range(0, sonidosGruñido.Length)];
        if (clip != null)
        {
            voz.pitch = Random.Range(0.9f, 1.05f);
            voz.PlayOneShot(clip, volumenGruñido);
        }

        Vector2 rango = persiguiendo ? intervaloPersecucion : intervaloPatrulla;
        proximaVoz = Time.time + Random.Range(rango.x, rango.y);
    }

    private AudioSource CrearFuente(string nombre, bool bucle, float distanciaMinima, float distanciaMaxima)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        objeto.transform.localPosition = Vector3.up * 1.5f;

        AudioSource fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = bucle;
        fuente.spatialBlend = 1f;
        fuente.rolloffMode = AudioRolloffMode.Linear;
        fuente.minDistance = distanciaMinima;
        fuente.maxDistance = distanciaMaxima;
        fuente.dopplerLevel = 0f;
        return fuente;
    }
}
