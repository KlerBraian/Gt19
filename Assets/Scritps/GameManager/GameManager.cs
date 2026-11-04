using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuración del tiempo")]
    [SerializeField] private float minutosDuracion = 15f;
    [SerializeField] private float segundosParaAlarma = 60f;
    [SerializeField] private float velocidadParpadeo = 4f;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorAlarma = Color.red;

    [Header("Puente (troncos y cuerdas)")]
    [SerializeField] private int totalObjetosNecesarios = 5;
    [SerializeField] private GameObject puenteConstruido;
    [SerializeField] private GameObject bloqueoDePaso;
    [SerializeField] private AudioClip sonidoConstruccion;

    [Header("Enemigo")]
    [SerializeField] private GameObject enemigo;
    [SerializeField] private bool enemigoEmpiezaOculto = true;
    [SerializeField] private string mensajeInicioCaza = "El puente está listo... y algo viene por vos.";
    [SerializeField] private AudioClip sonidoInicioCaza;
    [SerializeField, Range(0f, 1f)] private float volumenInicioCaza = 0.9f;
    [SerializeField] private float retrasoSonidoCaza = 1.5f;

    [Header("Zona donde aparece el enemigo")]
    [SerializeField] private Transform centroZonaEnemigo;
    [SerializeField] private float radioZonaEnemigo = 12f;
    [SerializeField] private Transform puntoQueDebeMirar;
    [SerializeField, Range(0f, 0.4f)] private float margenPantalla = 0.1f;
    [SerializeField] private float distanciaMaximaVision = 40f;
    [SerializeField] private float alturaOjos = 1.6f;
    [SerializeField] private LayerMask capasObstaculos = ~0;
    [SerializeField] private string mensajeAparicion = "Del otro lado del río se oye algo moverse...";
    [SerializeField] private AudioClip sonidoAparicion;
    [SerializeField, Range(0f, 1f)] private float volumenAparicion = 0.8f;

    [Header("Punto de aparición (la vida y el daño están en PlayerSalud)")]
    [SerializeField] private Transform puntoReaparicion;
    [SerializeField] private bool reaparecerAlPerderVida = false;

    [Header("Escenas (nombres exactos, agregadas en Build Profiles)")]
    [SerializeField] private string escenaVictoria = "Victoria";
    [SerializeField] private string escenaDerrota = "Derrota";

    [Header("UI (opcional)")]
    [SerializeField] private TextMeshProUGUI textoTimer;
    [SerializeField] private TextMeshProUGUI textoObjetos;
    [SerializeField] private TextMeshProUGUI textoAviso;
    [SerializeField] private TextMeshProUGUI textoObjetivos;

    private float tiempoRestante;
    private int segundoMostrado = -1;
    private bool partidaTerminada;
    private int objetosRecolectados;
    private bool puenteHabilitado;
    private bool tieneFusible;
    private bool zonaUsada;
    private float proximoChequeoZona;
    private PlayerSalud salud;
    private CharacterController controllerJugador;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (enemigo != null && enemigoEmpiezaOculto)
            enemigo.SetActive(false);
    }

    private void Start()
    {
        tiempoRestante = minutosDuracion * 60f;
        ActualizarUIObjetos();
        ActualizarUIObjetivos();

        if (textoAviso != null) textoAviso.gameObject.SetActive(false);
        if (puenteConstruido != null) puenteConstruido.SetActive(false);

        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            controllerJugador = jugadorObj.GetComponent<CharacterController>();
            salud = jugadorObj.GetComponent<PlayerSalud>();
        }

        if (salud == null)
            salud = FindFirstObjectByType<PlayerSalud>();

        if (salud != null && controllerJugador == null)
            controllerJugador = salud.GetComponent<CharacterController>();

        if (salud == null)
            Debug.LogWarning("[GameManager] No hay ningún PlayerSalud en la escena: agregale el script al Player.", this);

        if (enemigo != null && enemigoEmpiezaOculto && centroZonaEnemigo == null)
            Debug.LogWarning("[GameManager] El enemigo empieza oculto y no hay 'Centro Zona Enemigo': recién va a aparecer al construir el puente.", this);

        ReaparecerJugador();
    }

    private void Update()
    {
        if (partidaTerminada) return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            ActualizarUITimer();
            Derrota("¡Se acabó el tiempo!");
            return;
        }

        ActualizarUITimer();
        ChequearZonaEnemigo();
    }

    public void RecolectarMaterialPuente()
    {
        if (partidaTerminada || puenteHabilitado) return;

        objetosRecolectados++;
        ActualizarUIObjetos();

        if (objetosRecolectados >= totalObjetosNecesarios)
            HabilitarPuente();

        ActualizarUIObjetivos();
    }

    private void HabilitarPuente()
    {
        puenteHabilitado = true;

        if (puenteConstruido != null)
        {
            puenteConstruido.SetActive(true);

            if (!puenteConstruido.activeInHierarchy)
                Debug.LogWarning("[GameManager] El puente se activó pero sigue oculto: algún objeto padre está desactivado.", puenteConstruido);
        }
        else
        {
            Debug.LogWarning("[GameManager] Falta asignar 'Puente Construido' en el Inspector.", this);
        }

        if (bloqueoDePaso != null) bloqueoDePaso.SetActive(false);

        if (sonidoConstruccion != null)
        {
            Vector3 posicion = puenteConstruido != null ? puenteConstruido.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(sonidoConstruccion, posicion);
        }

        ActivarEnemigo();

        if (!string.IsNullOrEmpty(mensajeInicioCaza))
            MostrarAviso(mensajeInicioCaza);

        if (sonidoInicioCaza != null)
            Invoke(nameof(SonarInicioCaza), retrasoSonidoCaza);
    }

    public bool ActivarEnemigo()
    {
        if (partidaTerminada) return false;

        if (enemigo == null)
        {
            Debug.LogWarning("[GameManager] Falta arrastrar al enemigo al campo 'Enemigo'.", this);
            return false;
        }

        if (enemigo.activeSelf) return false;

        enemigo.SetActive(true);
        return true;
    }

    private void SonarInicioCaza()
    {
        if (partidaTerminada || sonidoInicioCaza == null) return;

        Vector3 posicion = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(sonidoInicioCaza, posicion, volumenInicioCaza);
    }

    private void ChequearZonaEnemigo()
    {
        if (zonaUsada || centroZonaEnemigo == null || puenteHabilitado || salud == null) return;
        if (Time.time < proximoChequeoZona) return;
        proximoChequeoZona = Time.time + 0.2f;

        Transform jugador = salud.transform;
        Vector3 plano = jugador.position - centroZonaEnemigo.position;
        plano.y = 0f;

        if (plano.sqrMagnitude > radioZonaEnemigo * radioZonaEnemigo) return;
        if (puntoQueDebeMirar != null && !JugadorMiraElPunto(jugador)) return;

        zonaUsada = true;
        Debug.Log("[GameManager] Zona del enemigo activada.", this);

        if (!ActivarEnemigo()) return;

        if (!string.IsNullOrEmpty(mensajeAparicion))
            MostrarAviso(mensajeAparicion);

        if (sonidoAparicion != null)
        {
            Vector3 posicion = Camera.main != null ? Camera.main.transform.position : jugador.position;
            AudioSource.PlayClipAtPoint(sonidoAparicion, posicion, volumenAparicion);
        }
    }

    private bool JugadorMiraElPunto(Transform jugador)
    {
        Camera camara = Camera.main;
        if (camara == null) return true;

        Vector3 posicionPunto = puntoQueDebeMirar.position;
        Vector3 enPantalla = camara.WorldToViewportPoint(posicionPunto);

        if (enPantalla.z <= 0f || enPantalla.z > distanciaMaximaVision) return false;

        float minimo = margenPantalla;
        float maximo = 1f - margenPantalla;
        if (enPantalla.x < minimo || enPantalla.x > maximo || enPantalla.y < minimo || enPantalla.y > maximo)
            return false;

        Vector3 origen = jugador.position + Vector3.up * alturaOjos;
        Vector3 haciaPunto = posicionPunto - origen;
        float distancia = haciaPunto.magnitude;

        if (distancia <= 0.75f) return true;

        return !Physics.Raycast(origen, haciaPunto / distancia, distancia - 0.75f, capasObstaculos, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        if (centroZonaEnemigo == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centroZonaEnemigo.position, radioZonaEnemigo);

        if (puntoQueDebeMirar == null) return;

        Gizmos.DrawLine(centroZonaEnemigo.position, puntoQueDebeMirar.position);
        Gizmos.DrawWireSphere(puntoQueDebeMirar.position, 0.5f);
    }

    private void ActualizarUIObjetos()
    {
        if (textoObjetos != null)
            textoObjetos.text = $"Materiales: {objetosRecolectados}/{totalObjetosNecesarios}";
    }

    public void RecolectarFusible()
    {
        if (partidaTerminada) return;
        tieneFusible = true;
        ActualizarUIObjetivos();
    }
    public void IntentarEscapar()
    {
        if (partidaTerminada) return;

        if (tieneFusible) Victoria();
        else MostrarAviso("Te falta el fusible para activar la radio.");
    }

    public void MostrarAviso(string mensaje)
    {
        if (textoAviso == null)
        {
            Debug.LogWarning($"[GameManager] Quiso mostrar el aviso \"{mensaje}\", pero falta asignar 'Texto Aviso'.", this);
            return;
        }

        textoAviso.text = mensaje;
        textoAviso.gameObject.SetActive(true);

        if (!textoAviso.gameObject.activeInHierarchy)
            Debug.LogWarning("[GameManager] El aviso se activó pero sigue oculto: algún objeto padre está desactivado.", textoAviso);

        CancelInvoke(nameof(OcultarAviso));
        Invoke(nameof(OcultarAviso), 3f);
    }

    [ContextMenu("Probar: mostrar aviso")]
    private void ProbarAviso()
    {
        MostrarAviso("Aviso de prueba");
    }

    [ContextMenu("Probar: aparecer enemigo")]
    private void ProbarAparicionEnemigo()
    {
        if (ActivarEnemigo()) MostrarAviso(mensajeAparicion);
    }

    private void OcultarAviso()
    {
        if (textoAviso != null) textoAviso.gameObject.SetActive(false);
    }

    public void JugadorAtrapado()
    {
        if (partidaTerminada) return;

        if (salud == null)
        {
            Debug.LogWarning("[GameManager] Llegó el golpe, pero no hay PlayerSalud: el Player necesita el tag 'Player' y el script PlayerSalud.", this);
            return;
        }

        bool recibioDaño = salud.RecibirDaño(salud.DañoPorGolpe);

        if (recibioDaño && !salud.EstaMuerto && reaparecerAlPerderVida)
            ReaparecerJugador();
    }

    public void JugadorMurio()
    {
        Derrota("¡Moriste!");
    }

    private void ActualizarUIObjetivos()
    {
        if (textoObjetivos == null) return;

        bool hayActivo = false;
        string texto = "<b>OBJETIVOS</b>\n";
        texto += LineaObjetivo($"Juntar materiales del puente ({objetosRecolectados}/{totalObjetosNecesarios})", puenteHabilitado, ref hayActivo);
        texto += LineaObjetivo("Cruzar el puente y encontrar el fusible", tieneFusible, ref hayActivo);
        texto += LineaObjetivo("Activar la radio en la torre", false, ref hayActivo);

        textoObjetivos.text = texto;
    }

    private static string LineaObjetivo(string descripcion, bool cumplido, ref bool hayActivo)
    {
        if (cumplido)
            return $"<color=#8A8A8A><s>[X] {descripcion}</s></color>\n";

        if (!hayActivo)
        {
            hayActivo = true;
            return $"<color=#FFD54F>[ ] {descripcion}</color>\n";
        }

        return $"[ ] {descripcion}\n";
    }

    private void ReaparecerJugador()
    {
        if (controllerJugador == null || puntoReaparicion == null) return;
        controllerJugador.enabled = false;
        controllerJugador.transform.position = puntoReaparicion.position;
        controllerJugador.enabled = true;
    }


    private void Victoria()
    {
        if (partidaTerminada) return;
        partidaTerminada = true;
        LiberarCursor();
        SceneManager.LoadScene(escenaVictoria);
    }

    private void Derrota(string motivo)
    {
        if (partidaTerminada) return;
        partidaTerminada = true;

        Debug.Log("DERROTA: " + motivo);
        LiberarCursor();
        SceneManager.LoadScene(escenaDerrota);
    }

    private static void LiberarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ActualizarUITimer()
    {
        if (textoTimer == null) return;
        int segundos = Mathf.FloorToInt(tiempoRestante);
        if (segundos != segundoMostrado)
        {
            segundoMostrado = segundos;
            textoTimer.text = $"{segundos / 60:00}:{segundos % 60:00}";
        }

        if (tiempoRestante <= segundosParaAlarma)
        {
            float t = Mathf.PingPong(Time.time * velocidadParpadeo, 1f);
            textoTimer.color = Color.Lerp(colorNormal, colorAlarma, t);
        }
        else
        {
            textoTimer.color = colorNormal;
        }
    }

    public bool EstaTerminada() => partidaTerminada;
    public bool PuenteHabilitado => puenteHabilitado;
}