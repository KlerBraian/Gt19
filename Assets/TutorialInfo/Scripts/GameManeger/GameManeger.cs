using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // si no usas TextMeshPro, cambiá por UnityEngine.UI.Text

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

    [Header("Escenas")]
    [Tooltip("Nombre exacto de la escena de Victoria (debe estar en Build Settings)")]
    [SerializeField] private string escenaVictoria = "Victoria";
    [Tooltip("Nombre exacto de la escena de Derrota (debe estar en Build Settings)")]
    [SerializeField] private string escenaDerrota = "Derrota";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textoTimer;
    [SerializeField] private TextMeshProUGUI textoObjetos;
    [SerializeField] private TextMeshProUGUI textoAviso; // para mensajes tipo "Te falta el fusible"

    [Header("Eventos")]
    public Action OnPuenteListo;      // se dispara una sola vez al juntar todos los materiales
    public Action<bool> OnFusibleCambio; // avisa cuando cambia el estado del fusible
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
        ActualizarUIObjetos();
        if (textoAviso != null) textoAviso.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (partidaTerminada) return;

        tiempoRestante -= Time.deltaTime;
        ActualizarUITimer();

        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            ActualizarUITimer();
            Derrota("¡Se acabó el tiempo!");
        }

        ActualizarUITimer();
    }

    //PUENTE (troncos / cuerdas)
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
            textoObjetos.text = $"Troncos: {objetosRecolectados}/{totalObjetosNecesarios}";
    }

    public bool PuenteEstaHabilitado() => puenteHabilitado;

    //FUSIBLE / ESCAPE
    public void RecolectarFusible()
    {
        if (partidaTerminada) return;

        tieneFusible = true;
        Debug.Log("Fusible recolectado.");
        OnFusibleCambio?.Invoke(true);
    }

    public bool TieneFusible() => tieneFusible;

    // llamado desde el botón/interacción de la torre (tecla E)
    // si tiene el fusible, gana. Si no, muestra aviso
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

    //MUERTE POR ENEMIGO
    public void JugadorAtrapado()
    {
        Derrota("¡El bicho te atrapó!");
    }

    //VICTORIA / DERROTA

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

        // tanto si se acabó el tiempo como si te atrapó el bicho, se carga
        // la misma escena de derrota (el "diario" donde aparecés muerto).
        SceneManager.LoadScene(escenaDerrota);
    }

    //UTILIDAD

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