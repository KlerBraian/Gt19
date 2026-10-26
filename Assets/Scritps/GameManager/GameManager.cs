using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuración del tiempo")]
    [SerializeField] private float minutosDuracion = 15f;
    [Tooltip("Segundos restantes a partir de los cuales el timer parpadea en rojo")]
    [SerializeField] private float segundosParaAlarma = 60f;
    [SerializeField] private float velocidadParpadeo = 4f;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorAlarma = Color.red;

    [Header("Puente (troncos y cuerdas)")]
    [Tooltip("Cantidad de troncos/cuerdas necesarios para habilitar el puente")]
    [SerializeField] private int totalObjetosNecesarios = 5;
    [Tooltip("El puente ya armado. Se desactiva al empezar y se activa al juntar todo.")]
    [SerializeField] private GameObject puenteConstruido;
    [Tooltip("Opcional: muro invisible que se desactiva al construir el puente")]
    [SerializeField] private GameObject bloqueoDePaso;
    [SerializeField] private AudioClip sonidoConstruccion;

    [Header("Vidas")]
    [SerializeField] private int vidasIniciales = 3;
    [Tooltip("Dónde reaparece el jugador al perder una vida (no en la última)")]
    [SerializeField] private Transform puntoReaparicion;
    [Tooltip("Si está apagado, al recibir daño el jugador se queda donde está (el enemigo espera su cooldown antes de volver a golpear)")]
    [SerializeField] private bool reaparecerAlPerderVida = false;

    [Header("Escenas (nombres exactos, agregadas en Build Profiles)")]
    [SerializeField] private string escenaVictoria = "Victoria";
    [SerializeField] private string escenaDerrota = "Derrota";

    [Header("UI (opcional)")]
    [SerializeField] private TextMeshProUGUI textoTimer;
    [SerializeField] private TextMeshProUGUI textoObjetos;
    [SerializeField] private TextMeshProUGUI textoAviso;
    [SerializeField] private TextMeshProUGUI textoVidas;

    private float tiempoRestante;
    private int segundoMostrado = -1;
    private bool partidaTerminada;
    private int objetosRecolectados;
    private bool puenteHabilitado;
    private bool tieneFusible;
    private int vidasActuales;
    private CharacterController controllerJugador;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        tiempoRestante = minutosDuracion * 60f;
        vidasActuales = vidasIniciales;
        ActualizarUIObjetos();
        ActualizarUIVidas();

        if (textoAviso != null) textoAviso.gameObject.SetActive(false);
        if (puenteConstruido != null) puenteConstruido.SetActive(false);

        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
            controllerJugador = jugadorObj.GetComponent<CharacterController>();
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
    }

    // ---------- PUENTE ----------
    public void RecolectarMaterialPuente()
    {
        if (partidaTerminada || puenteHabilitado) return;

        objetosRecolectados++;
        ActualizarUIObjetos();

        if (objetosRecolectados >= totalObjetosNecesarios)
            HabilitarPuente();
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
    }

    private void ActualizarUIObjetos()
    {
        if (textoObjetos != null)
            textoObjetos.text = $"Materiales: {objetosRecolectados}/{totalObjetosNecesarios}";
    }

    // ---------- FUSIBLE / ESCAPE ----------
    public void RecolectarFusible()
    {
        if (partidaTerminada) return;
        tieneFusible = true;
    }
    public void IntentarEscapar()
    {
        if (partidaTerminada) return;

        if (tieneFusible) Victoria();
        else MostrarAviso("Te falta el fusible para activar la radio.");
    }

    private void MostrarAviso(string mensaje)
    {
        if (textoAviso == null) return;

        textoAviso.text = mensaje;
        textoAviso.gameObject.SetActive(true);
        CancelInvoke(nameof(OcultarAviso));
        Invoke(nameof(OcultarAviso), 3f);
    }

    private void OcultarAviso()
    {
        if (textoAviso != null) textoAviso.gameObject.SetActive(false);
    }

    // ---------- VIDAS ----------
    public void JugadorAtrapado()
    {
        if (partidaTerminada) return;

        vidasActuales--;
        ActualizarUIVidas();

        if (vidasActuales <= 0)
            Derrota("¡El bicho te atrapó y se acabaron tus vidas!");
        else if (reaparecerAlPerderVida)
            ReaparecerJugador();
    }

    private void ActualizarUIVidas()
    {
        if (textoVidas != null)
            textoVidas.text = $"Vidas: {vidasActuales}/{vidasIniciales}";
    }

    private void ReaparecerJugador()
    {
        if (controllerJugador == null || puntoReaparicion == null) return;
        controllerJugador.enabled = false;
        controllerJugador.transform.position = puntoReaparicion.position;
        controllerJugador.enabled = true;
    }

    // ---------- VICTORIA / DERROTA ----------

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

    // ---------- TIMER UI ----------

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