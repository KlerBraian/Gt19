using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuración del tiempo")]
    [Tooltip("Duración total en minutos")]
    [SerializeField] private float minutosDuracion = 15f;
    [Tooltip("Segundos restantes a partir de los cuales el timer parpadea en rojo")]
    [SerializeField] private float segundosParaAlarma = 60f;
    [Tooltip("Velocidad del parpadeo (más alto = más rápido)")]
    [SerializeField] private float velocidadParpadeo = 4f;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorAlarma = Color.red;
    private float tiempoRestante;
    private bool partidaTerminada = false;

    [Header("Puente (troncos y cuerdas)")]
    [Tooltip("Cantidad de troncos/cuerdas necesarios para habilitar el puente")]
    [SerializeField] private int totalObjetosNecesarios = 5;
    private int objetosRecolectados = 0;
    private bool puenteHabilitado = false;

    [Header("Fusible / escape")]
    private bool tieneFusible = false;

    [Header("Vidas")]
    [SerializeField] private int vidasIniciales = 3;
    private int vidasActuales;
    [Tooltip("Dónde reaparece el jugador después de perder una vida (no en la última)")]
    [SerializeField] private Transform puntoReaparicion;
    private CharacterController controllerJugador;

    [Header("Escenas")]
    [Tooltip("Nombre exacto de la escena de Victoria (debe estar en Build Settings)")]
    [SerializeField] private string escenaVictoria = "Victoria";
    [Tooltip("Nombre exacto de la escena de Derrota (debe estar en Build Settings)")]
    [SerializeField] private string escenaDerrota = "Derrota";

    [Header("UI (opcional, asigná en el Inspector)")]
    [SerializeField] private TextMeshProUGUI textoTimer;
    [SerializeField] private TextMeshProUGUI textoObjetos;
    [SerializeField] private TextMeshProUGUI textoAviso;
    [SerializeField] private TextMeshProUGUI textoVidas;

    [Header("Eventos")]
    public Action OnPuenteListo;
    public Action<bool> OnFusibleCambio; 
    public Action OnVidaPerdida; 
    public Action OnDerrota;

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

    // ---------- PUENTE (troncos / cuerdas) ----------
    public void RecolectarMaterialPuente()
    {
        if (partidaTerminada || puenteHabilitado) return;

        objetosRecolectados++;
        ActualizarUIObjetos();

        if (objetosRecolectados >= totalObjetosNecesarios)
        {
            HabilitarPuente();
        }
    }

    private void HabilitarPuente()
    {
        puenteHabilitado = true;
        Debug.Log("Puente habilitado: ya podés cruzar.");
        OnPuenteListo?.Invoke();
    }

    private void ActualizarUIObjetos()
    {
        if (textoObjetos != null)
            textoObjetos.text = $"Materiales: {objetosRecolectados}/{totalObjetosNecesarios}";
    }

    public bool PuenteEstaHabilitado() => puenteHabilitado;

    // ---------- FUSIBLE / ESCAPE ----------
    public void RecolectarFusible()
    {
        if (partidaTerminada) return;

        tieneFusible = true;
        Debug.Log("Fusible recolectado.");
        OnFusibleCambio?.Invoke(true);
    }

    public bool TieneFusible() => tieneFusible;
    public void IntentarEscapar()
    {
        if (partidaTerminada) return;

        if (tieneFusible)
        {
            Victoria();
        }
        else
        {
            MostrarAviso("Te falta el fusible para activar la radio.");
        }
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

    // ---------- MUERTE POR ENEMIGO ----------
    public void JugadorAtrapado()
    {
        if (partidaTerminada) return;

        vidasActuales--;
        ActualizarUIVidas();

        if (vidasActuales <= 0)
        {
            Derrota("¡El bicho te atrapó y se acabaron tus vidas!");
        }
        else
        {
            Debug.Log($"Te atrapó el bicho. Vidas restantes: {vidasActuales}");
            ReaparecerJugador();
            OnVidaPerdida?.Invoke();
        }
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

    public int GetVidasActuales() => vidasActuales;

    // ---------- VICTORIA / DERROTA ----------

    private void Victoria()
    {
        if (partidaTerminada) return;
        partidaTerminada = true;

        Debug.Log("VICTORIA: escapaste con el fusible.");
        SceneManager.LoadScene(escenaVictoria);
    }

    private void Derrota(string motivo)
    {
        if (partidaTerminada) return;
        partidaTerminada = true;

        Debug.Log("DERROTA: " + motivo);
        OnDerrota?.Invoke();
        SceneManager.LoadScene(escenaDerrota);
    }

    // ---------- UTILIDAD ----------

    public void ReiniciarPartida()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ActualizarUITimer()
    {
        if (textoTimer == null) return;

        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
        textoTimer.text = $"{minutos:00}:{segundos:00}";

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

    public float GetTiempoRestante() => tiempoRestante;
    public bool EstaTerminada() => partidaTerminada;
}