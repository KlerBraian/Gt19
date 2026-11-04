using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PantallaUI : MonoBehaviour
{
    [Header("Música o sonido de la pantalla")]
    [SerializeField] private AudioClip musica;
    [SerializeField, Range(0f, 1f)] private float volumenMusica = 0.5f;
    [SerializeField] private bool musicaEnBucle = true;
    [SerializeField] private float segundosEntradaMusica = 3f;

    [Header("Sonidos de los botones (todos los de la escena)")]
    [SerializeField] private AudioClip sonidoHover;
    [SerializeField] private AudioClip sonidoClick;
    [SerializeField, Range(0f, 1f)] private float volumenBotones = 0.6f;

    [Header("Guía (dejar 'Texto' vacío si no es la escena de guía)")]
    [SerializeField] private string escenaSiguiente = "Game";
    [SerializeField] private TextMeshProUGUI texto;
    [SerializeField] private TextMeshProUGUI textoPagina;
    [SerializeField] private TextMeshProUGUI textoBoton;
    [SerializeField, TextArea(5, 14)] private string[] paginas =
    {
        "<size=130%><b>ESTÁS SOLO EN EL BOSQUE</b></size>\n\nTu auto quedó tirado en la ruta y la noche cayó hace horas.\n\nLa única salida es la <color=#FFD54F>torre de radio</color> que se ve del otro lado del río, pero el puente está roto.\n\nTenés <color=#FFD54F>15 minutos</color>. Si se acaba el tiempo, no vas a salir de acá.",
        "<size=130%><b>QUÉ TENÉS QUE HACER</b></size>\n\n<color=#FFD54F>1.</color> Juntá los <b>5 troncos y cuerdas</b> que hay por el bosque para reparar el puente.\n\n<color=#FFD54F>2.</color> Cruzá el puente y encontrá el <b>fusible</b>.\n\n<color=#FFD54F>3.</color> Llegá a la torre y apretá <b>E</b> sobre el botón de la radio para pedir ayuda.",
        "<size=130%><b>NO ESTÁS SOLO</b></size>\n\nAlgo vive en este bosque. Patrulla de noche y si te ve, te persigue. Cada golpe te saca vida.\n\nSi te perdés de vista, te busca un rato y vuelve a patrullar. Escondete, apagá la linterna y no corras si no hace falta: <color=#FFD54F>los pasos se escuchan</color>.\n\nPero cuando termines el puente, <color=#FF5A4F>va a venir directo por vos</color>. Ya no se va a dar por vencido.",
        "<size=130%><b>CONTROLES</b></size>\n\n<color=#FFD54F>W A S D</color>  —  moverte\n<color=#FFD54F>Shift</color>  —  correr\n<color=#FFD54F>Espacio</color>  —  saltar\n<color=#FFD54F>Mouse</color>  —  mover la cámara\n<color=#FFD54F>Rueda del mouse</color>  —  acercar o alejar la cámara\n<color=#FFD54F>F</color>  —  prender o apagar la linterna\n<color=#FFD54F>E</color>  —  agarrar, abrir y usar\n\nMucha suerte."
    };

    [Header("Guía: efecto de escritura")]
    [SerializeField] private float caracteresPorSegundo = 45f;
    [SerializeField] private int caracteresPorSonido = 2;
    [SerializeField] private AudioClip[] sonidosTecla;
    [SerializeField, Range(0f, 1f)] private float volumenTecla = 0.5f;

    [Header("Guía: teclas")]
    [SerializeField] private Key teclaSiguiente = Key.Space;
    [SerializeField] private Key teclaAlternativa = Key.Enter;
    [SerializeField] private Key teclaSaltar = Key.Escape;

    private AudioSource fuenteMusica;
    private AudioSource fuenteEfectos;
    private Coroutine escritura;
    private int pagina;
    private bool modoGuia;
    private bool escribiendo;
    private bool cargando;
    private float ultimoAvance = -10f;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;
        fuenteEfectos.spatialBlend = 0f;

        if (musica != null)
        {
            fuenteMusica = gameObject.AddComponent<AudioSource>();
            fuenteMusica.clip = musica;
            fuenteMusica.loop = musicaEnBucle;
            fuenteMusica.spatialBlend = 0f;
            fuenteMusica.volume = 0f;
            fuenteMusica.Play();
        }

        ConectarBotones();

        modoGuia = texto != null && paginas != null && paginas.Length > 0;
        if (modoGuia) MostrarPagina(0);
    }

    private void Update()
    {
        if (fuenteMusica != null)
            fuenteMusica.volume = Mathf.MoveTowards(fuenteMusica.volume, volumenMusica,
                volumenMusica / Mathf.Max(0.01f, segundosEntradaMusica) * Time.unscaledDeltaTime);

        if (!modoGuia || cargando) return;

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (teclaSaltar != Key.None && teclado[teclaSaltar].wasPressedThisFrame)
        {
            Comenzar();
            return;
        }

        bool siguiente = (teclaSiguiente != Key.None && teclado[teclaSiguiente].wasPressedThisFrame)
                         || (teclaAlternativa != Key.None && teclado[teclaAlternativa].wasPressedThisFrame);

        if (siguiente) Avanzar();
    }

    public void Avanzar()
    {
        if (!modoGuia || cargando) return;
        if (Time.unscaledTime - ultimoAvance < 0.15f) return;
        ultimoAvance = Time.unscaledTime;

        if (escribiendo)
        {
            CompletarPagina();
            return;
        }

        if (pagina < paginas.Length - 1) MostrarPagina(pagina + 1);
        else Comenzar();
    }

    public void Comenzar()
    {
        if (cargando) return;
        cargando = true;
        SceneManager.LoadScene(escenaSiguiente);
    }

    private void ConectarBotones()
    {
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button boton in botones)
        {
            EventTrigger disparador = boton.GetComponent<EventTrigger>();
            if (disparador == null) disparador = boton.gameObject.AddComponent<EventTrigger>();

            Button actual = boton;
            AgregarEvento(disparador, EventTriggerType.PointerEnter, () =>
            {
                if (actual.interactable) SonarInterfaz(sonidoHover, volumenBotones * 0.6f);
            });
            AgregarEvento(disparador, EventTriggerType.PointerDown, () =>
            {
                if (actual.interactable) SonarInterfaz(sonidoClick, volumenBotones);
            });
        }
    }

    private static void AgregarEvento(EventTrigger disparador, EventTriggerType tipo, Action accion)
    {
        EventTrigger.Entry entrada = new EventTrigger.Entry { eventID = tipo };
        entrada.callback.AddListener(_ => accion());
        disparador.triggers.Add(entrada);
    }

    private void SonarInterfaz(AudioClip clip, float volumen)
    {
        if (clip == null) return;
        fuenteEfectos.pitch = 1f;
        fuenteEfectos.PlayOneShot(clip, volumen);
    }

    private void MostrarPagina(int indice)
    {
        pagina = indice;

        if (escritura != null) StopCoroutine(escritura);

        texto.text = paginas[indice];
        texto.maxVisibleCharacters = 0;
        texto.ForceMeshUpdate();

        if (textoPagina != null) textoPagina.text = $"{indice + 1}/{paginas.Length}";
        if (textoBoton != null) textoBoton.text = indice == paginas.Length - 1 ? "COMENZAR" : "SIGUIENTE";

        escritura = StartCoroutine(Escribir());
    }

    private IEnumerator Escribir()
    {
        escribiendo = true;

        int total = texto.textInfo.characterCount;
        int visibles = 0;
        int sinSonido = 0;
        float acumulado = 0f;

        while (visibles < total)
        {
            acumulado += Time.unscaledDeltaTime * caracteresPorSegundo;
            int nuevos = Mathf.FloorToInt(acumulado);

            if (nuevos > 0)
            {
                acumulado -= nuevos;
                visibles = Mathf.Min(total, visibles + nuevos);
                texto.maxVisibleCharacters = visibles;

                sinSonido += nuevos;
                if (sinSonido >= caracteresPorSonido)
                {
                    sinSonido = 0;
                    SonarTecla();
                }
            }

            yield return null;
        }

        escribiendo = false;
    }

    private void CompletarPagina()
    {
        if (escritura != null) StopCoroutine(escritura);
        texto.maxVisibleCharacters = int.MaxValue;
        escribiendo = false;
    }

    private void SonarTecla()
    {
        if (sonidosTecla == null || sonidosTecla.Length == 0) return;

        AudioClip clip = sonidosTecla[UnityEngine.Random.Range(0, sonidosTecla.Length)];
        if (clip == null) return;

        fuenteEfectos.pitch = UnityEngine.Random.Range(0.95f, 1.08f);
        fuenteEfectos.PlayOneShot(clip, volumenTecla);
    }
}
