using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemigoPersigue : MonoBehaviour
{
    private enum Estado { Patrullando, Persiguiendo, Buscando, Rugiendo, Golpeando }

    [Header("Referencias")]
    [SerializeField] private Transform jugador;
    [Tooltip("Objeto vacío cuyos HIJOS son los puntos de la ruta, en orden. No puede ser hijo del enemigo.")]
    [SerializeField] private Transform ruta;
    [Tooltip("Vacío = lo busca en este objeto o en sus hijos")]
    [SerializeField] private Animator animator;
    [Tooltip("Vacío = se crea uno solo con sonido 3D")]
    [SerializeField] private AudioSource fuenteAudio;

    [Header("Velocidades")]
    [SerializeField] private float velocidadPatrulla = 1.5f;
    [Tooltip("Tiene que ser mayor que la de patrulla")]
    [SerializeField] private float velocidadPersecucion = 3.5f;

    [Header("Visión")]
    [Tooltip("Hasta dónde ve")]
    [SerializeField] private float distanciaDeteccion = 15f;
    [Tooltip("Ancho total del cono de visión, en grados")]
    [SerializeField, Range(10f, 360f)] private float anguloVision = 110f;
    [Tooltip("Dentro de esta distancia te detecta aunque estés de espaldas (igual no ve a través de paredes)")]
    [SerializeField] private float distanciaCercana = 2.5f;
    [SerializeField] private float alturaOjos = 1.6f;
    [Tooltip("Capas que bloquean la vista (paredes, puertas)")]
    [SerializeField] private LayerMask capaObstaculos = ~0;

    [Header("Ruta")]
    [SerializeField] private float esperaEnPunto = 2f;

    [Header("Búsqueda")]
    [Tooltip("Segundos que busca en el último lugar donde te vio, antes de volver a patrullar")]
    [SerializeField] private float tiempoBusqueda = 4f;
    [SerializeField] private float velocidadGiroBusqueda = 90f;

    [Header("Rugido")]
    [SerializeField] private float duracionRugido = 1.5f;
    [Tooltip("Mínimo de segundos entre un rugido y otro, para que no ruja cada vez que te re-descubre")]
    [SerializeField] private float cooldownRugido = 10f;

    [Header("Ataque")]
    [Tooltip("Qué tan cerca tiene que estar para golpearte (subilo si el modelo es grande)")]
    [SerializeField] private float distanciaParaMatar = 1.5f;
    [Tooltip("Segundos desde que arranca la animación hasta que el golpe impacta. Ajustalo al momento en que el brazo pega.")]
    [SerializeField] private float retrasoDaño = 0.5f;
    [Tooltip("Duración total de la animación de golpe")]
    [SerializeField] private float duracionGolpe = 1.2f;
    [Tooltip("Segundos entre un golpe y el siguiente (tiene que ser >= la duración del golpe)")]
    [SerializeField] private float cooldownDespuesDeAtrapar = 2f;
    [SerializeField] private float intervaloActualizacionRuta = 0.2f;
    [Tooltip("Qué tan rápido gira para mirarte mientras ruge o golpea")]
    [SerializeField] private float velocidadGiroAccion = 360f;

    [Header("Animaciones (nombres EXACTOS de los estados del Animator)")]
    [SerializeField] private string estadoQuieto = "Idle";
    [SerializeField] private string estadoPatrulla = "CaminarAgachado";
    [SerializeField] private string estadoPersecucion = "Correr";
    [SerializeField] private string estadoGolpe = "Golpear";
    [SerializeField] private string estadoRugido = "Rugir";
    [SerializeField] private float transicionAnimacion = 0.15f;

    [Header("Sonidos")]
    [SerializeField] private AudioClip sonidoRugido;
    [SerializeField] private AudioClip sonidoGolpe;
    [Tooltip("Uno al azar en cada paso")]
    [SerializeField] private AudioClip[] sonidosPasos;
    [SerializeField] private float intervaloPasosPatrulla = 0.8f;
    [SerializeField] private float intervaloPasosPersecucion = 0.4f;
    [Tooltip("Los pasos agachado suenan más bajo")]
    [SerializeField, Range(0f, 1f)] private float volumenPasosPatrulla = 0.35f;

    private const float IntervaloChequeoVision = 0.1f;
    private const float DistanciaLlegada = 0.3f;

    private NavMeshAgent agente;
    private Transform[] puntos = new Transform[0];
    private Estado estado;
    private bool modoCaza;

    private int indicePunto;
    private bool esperando;
    private float finEspera;

    private bool veJugador;
    private Vector3 ultimaPosicionVista;
    private float proximoChequeoVision;
    private float proximaActualizacionRuta;

    private float finAccion;       // fin del rugido o del golpe en curso
    private float momentoDano;
    private bool danoAplicado;
    private float proximoAtaque;
    private float proximoRugido;
    private float proximoPaso;

    private int hashQuieto, hashPatrulla, hashPersecucion, hashGolpe, hashRugido;
    private int animacionActual;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();

        if (jugador == null)
        {
            GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
            if (jugadorObj != null) jugador = jugadorObj.transform;
        }

        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (fuenteAudio == null) fuenteAudio = GetComponent<AudioSource>();
        if (fuenteAudio == null)
        {
            fuenteAudio = gameObject.AddComponent<AudioSource>();
            fuenteAudio.playOnAwake = false;
            fuenteAudio.spatialBlend = 1f; // sonido 3D: se oye de dónde viene
            fuenteAudio.maxDistance = 30f;
            fuenteAudio.rolloffMode = AudioRolloffMode.Linear;
        }

        hashQuieto = Animator.StringToHash(estadoQuieto);
        hashPatrulla = Animator.StringToHash(estadoPatrulla);
        hashPersecucion = Animator.StringToHash(estadoPersecucion);
        hashGolpe = Animator.StringToHash(estadoGolpe);
        hashRugido = Animator.StringToHash(estadoRugido);

        if (ruta != null)
        {
            if (ruta.IsChildOf(transform))
                Debug.LogWarning("[Enemigo] La ruta es hija del enemigo: los puntos se van a mover con él. Sacala afuera.", this);

            puntos = new Transform[ruta.childCount];
            for (int i = 0; i < puntos.Length; i++)
                puntos[i] = ruta.GetChild(i);
        }
    }

    private void Start()
    {
        CambiarEstado(Estado.Patrullando);
    }

    private void Update()
    {
        if (jugador == null) return;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada())
        {
            Detener();
            ReproducirAnimacion(hashQuieto, false);
            return;
        }

        // Al juntar los materiales del puente empieza la caza total.
        if (!modoCaza && gm != null && gm.PuenteHabilitado)
        {
            modoCaza = true;
            proximoRugido = 0f; // fuerza un rugido para avisar que cambió todo
            proximoChequeoVision = 0f;
        }

        ActualizarVision();

        switch (estado)
        {
            case Estado.Patrullando: Patrullar(); break;
            case Estado.Persiguiendo: Perseguir(); break;
            case Estado.Buscando: Buscar(); break;
            case Estado.Rugiendo: Rugir(); break;
            case Estado.Golpeando: Golpear(gm); break;
        }

        ActualizarAnimacionYPasos();
    }

    // ---------- VISIÓN ----------

    private void ActualizarVision()
    {
        // No hace falta lanzar el rayo en cada frame.
        if (Time.time < proximoChequeoVision) return;
        proximoChequeoVision = Time.time + IntervaloChequeoVision;

        // En modo caza siempre sabe dónde estás.
        veJugador = modoCaza || VeAlJugador();

        if (veJugador)
        {
            ultimaPosicionVista = jugador.position;
            if (estado == Estado.Patrullando || estado == Estado.Buscando)
                IniciarPersecucion();
        }
        else if (estado == Estado.Persiguiendo)
        {
            CambiarEstado(Estado.Buscando);
        }
    }

    private bool VeAlJugador()
    {
        Vector3 ojos = transform.position + Vector3.up * alturaOjos;
        Vector3 objetivo = jugador.position + Vector3.up; // al pecho, no a los pies
        Vector3 haciaJugador = objetivo - ojos;
        float distancia = haciaJugador.magnitude;

        if (distancia > distanciaDeteccion) return false;

        bool muyCerca = distancia <= distanciaCercana;
        if (!muyCerca && Vector3.Angle(transform.forward, haciaJugador) > anguloVision * 0.5f)
            return false;

        // Línea de visión: si lo primero que toca el rayo no es el jugador, hay algo en el medio.
        if (Physics.Raycast(ojos, haciaJugador / distancia, out RaycastHit hit, distancia,
                            capaObstaculos, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == jugador || hit.transform.IsChildOf(jugador);
        }

        return true;
    }

    private void IniciarPersecucion()
    {
        CambiarEstado(Time.time >= proximoRugido ? Estado.Rugiendo : Estado.Persiguiendo);
    }

    // ---------- ESTADOS ----------

    private void CambiarEstado(Estado nuevo)
    {
        estado = nuevo;
        esperando = false;
        if (agente.isOnNavMesh) agente.isStopped = false;

        switch (nuevo)
        {
            case Estado.Patrullando:
                agente.speed = velocidadPatrulla;
                agente.stoppingDistance = DistanciaLlegada;
                IrA(PuntoActual());
                break;

            case Estado.Persiguiendo:
                agente.speed = velocidadPersecucion;
                agente.stoppingDistance = Mathf.Max(DistanciaLlegada, distanciaParaMatar * 0.8f);
                proximaActualizacionRuta = 0f;
                break;

            case Estado.Buscando:
                agente.speed = velocidadPersecucion;
                agente.stoppingDistance = DistanciaLlegada;
                IrA(ultimaPosicionVista);
                break;

            case Estado.Rugiendo:
                Detener();
                ReproducirAnimacion(hashRugido, true);
                Sonar(sonidoRugido, 1f);
                finAccion = Time.time + duracionRugido;
                proximoRugido = Time.time + cooldownRugido;
                break;

            case Estado.Golpeando:
                Detener();
                ReproducirAnimacion(hashGolpe, true);
                momentoDano = Time.time + retrasoDaño;
                finAccion = Time.time + duracionGolpe;
                proximoAtaque = Time.time + cooldownDespuesDeAtrapar;
                danoAplicado = false;
                break;
        }
    }

    private void Patrullar()
    {
        if (puntos.Length == 0) return; // sin ruta, se queda quieto vigilando

        if (esperando)
        {
            if (Time.time >= finEspera)
            {
                esperando = false;
                indicePunto = (indicePunto + 1) % puntos.Length;
                IrA(PuntoActual());
            }
        }
        else if (Llego())
        {
            esperando = true;
            finEspera = Time.time + esperaEnPunto;
        }
    }

    private void Perseguir()
    {
        if (Time.time >= proximaActualizacionRuta)
        {
            IrA(jugador.position);
            proximaActualizacionRuta = Time.time + intervaloActualizacionRuta;
        }

        if (veJugador && Time.time >= proximoAtaque && EnRango(distanciaParaMatar))
            CambiarEstado(Estado.Golpeando);
    }

    private void Buscar()
    {
        if (esperando)
        {
            transform.Rotate(0f, velocidadGiroBusqueda * Time.deltaTime, 0f); // mira alrededor

            if (Time.time >= finEspera)
                CambiarEstado(Estado.Patrullando);
        }
        else if (Llego())
        {
            esperando = true;
            finEspera = Time.time + tiempoBusqueda;
        }
    }

    private void Rugir()
    {
        MirarHacia(ultimaPosicionVista);

        if (Time.time >= finAccion)
            CambiarEstado(veJugador ? Estado.Persiguiendo : Estado.Buscando);
    }

    private void Golpear(GameManager gm)
    {
        MirarHacia(jugador.position);
        if (!danoAplicado && Time.time >= momentoDano)
        {
            danoAplicado = true;

            if (EnRango(distanciaParaMatar * 1.3f))
            {
                Sonar(sonidoGolpe, 1f);
                if (gm != null) gm.JugadorAtrapado();
            }
        }

        if (Time.time >= finAccion)
            CambiarEstado(veJugador ? Estado.Persiguiendo : Estado.Buscando);
    }

    // ---------- ANIMACIÓN Y SONIDO ----------

    private void ActualizarAnimacionYPasos()
    {
        if (estado == Estado.Rugiendo || estado == Estado.Golpeando) return;

        bool seMueve = agente.velocity.sqrMagnitude > 0.04f;
        bool agachado = estado == Estado.Patrullando;

        int deseada = !seMueve ? hashQuieto : (agachado ? hashPatrulla : hashPersecucion);
        ReproducirAnimacion(deseada, false);

        if (seMueve && sonidosPasos.Length > 0 && Time.time >= proximoPaso)
        {
            proximoPaso = Time.time + (agachado ? intervaloPasosPatrulla : intervaloPasosPersecucion);
            Sonar(sonidosPasos[Random.Range(0, sonidosPasos.Length)], agachado ? volumenPasosPatrulla : 1f);
        }
    }
    private void ReproducirAnimacion(int hash, bool reiniciar)
    {
        if (animator == null) return;
        if (!reiniciar && hash == animacionActual) return;

        animacionActual = hash;
        animator.CrossFadeInFixedTime(hash, transicionAnimacion);
    }

    private void Sonar(AudioClip clip, float volumen)
    {
        if (clip != null && fuenteAudio != null)
            fuenteAudio.PlayOneShot(clip, volumen);
    }

    // ---------- UTILIDADES ----------

    private Vector3 PuntoActual()
    {
        return puntos.Length > 0 ? puntos[indicePunto].position : transform.position;
    }

    private void IrA(Vector3 destino)
    {
        if (agente.isOnNavMesh) agente.SetDestination(destino);
    }

    private void Detener()
    {
        if (!agente.isOnNavMesh) return;
        agente.isStopped = true;
        agente.velocity = Vector3.zero;
    }

    private bool Llego()
    {
        return !agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.1f;
    }

    private bool EnRango(float radio)
    {
        Vector3 plano = jugador.position - transform.position;
        float desnivel = Mathf.Abs(plano.y);
        plano.y = 0f;
        return plano.sqrMagnitude <= radio * radio && desnivel < 2f;
    }

    private void MirarHacia(Vector3 punto)
    {
        Vector3 direccion = punto - transform.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(direccion), velocidadGiroAccion * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaParaMatar);

        // Cono de visión
        Gizmos.color = Color.yellow;
        Vector3 izquierda = Quaternion.Euler(0f, -anguloVision * 0.5f, 0f) * transform.forward * distanciaDeteccion;
        Vector3 derecha = Quaternion.Euler(0f, anguloVision * 0.5f, 0f) * transform.forward * distanciaDeteccion;
        Gizmos.DrawRay(transform.position, izquierda);
        Gizmos.DrawRay(transform.position, derecha);
        Gizmos.DrawWireSphere(transform.position, distanciaCercana);

        // Ruta
        if (ruta == null || ruta.childCount == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < ruta.childCount; i++)
        {
            Vector3 actual = ruta.GetChild(i).position;
            Vector3 siguiente = ruta.GetChild((i + 1) % ruta.childCount).position;
            Gizmos.DrawSphere(actual, 0.25f);
            Gizmos.DrawLine(actual, siguiente);
        }
    }
}