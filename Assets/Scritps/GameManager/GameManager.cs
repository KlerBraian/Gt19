using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

    [Header("Puente (troncos y sogas)")]
    [SerializeField] private int troncosNecesarios = 3;
    [SerializeField] private int sogasNecesarias = 3;
    [SerializeField] private GameObject puenteConstruido;
    [SerializeField] private AudioClip sonidoConstruccion;

    [Header("Música de tensión (empieza al construir el puente)")]
    [SerializeField] private AudioClip musicaTension;
    [SerializeField, Range(0f, 1f)] private float volumenMusicaTension = 0.5f;
    [SerializeField] private float duracionFundidoMusica = 4f;
    [SerializeField] private float retrasoMusicaTension = 0.5f;

    [Header("Enemigo")]
    [SerializeField] private GameObject enemigo;
    [SerializeField] private bool enemigoEmpiezaOculto = true;
    [SerializeField] private string mensajeInicioCaza = "El puente está listo... y algo viene por vos.";
    [SerializeField] private AudioClip sonidoInicioCaza;
    [SerializeField, Range(0f, 1f)] private float volumenInicioCaza = 0.9f;
    [SerializeField] private float retrasoSonidoCaza = 1.5f;
    [SerializeField] private bool ocultarEnemigoAlReaparecer = true;

    [Header("Zona donde aparece el enemigo")]
    [SerializeField] private Collider zonaActivacionEnemigo;
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
    [SerializeField] private bool mostrarDebugZona = true;

    [Header("Troncos y sogas (aparecen junto con el enemigo)")]
    [SerializeField] private bool materialesEmpiezanOcultos = true;

    [Header("Fusible (aparece junto con el enemigo, los troncos y las sogas)")]
    [SerializeField] private bool fusibleEmpiezaOculto = true;
    [SerializeField] private bool ocultarHudFusibleHastaPuente = true;

    [Header("Río")]
    [SerializeField] private Transform nivelDelAgua;
    [SerializeField] private float profundidadParaMorir = 0.3f;
    [SerializeField] private string mensajeAhogado = "¡Te ahogaste en el río!";

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
    [SerializeField] private TextMeshProUGUI textoFusible;
    [SerializeField] private Image iconoFusible;
    [SerializeField] private Color colorFusibleFaltante = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color colorFusibleObtenido = Color.white;

    private float tiempoRestante;
    private int segundoMostrado = -1;
    private bool partidaTerminada;
    private int troncosRecolectados;
    private int sogasRecolectadas;
    private float proximoDebugZona;
    private string motivoNoVe = "";
    private readonly List<GameObject> materialesOcultos = new List<GameObject>();
    private readonly List<GameObject> fusiblesOcultos = new List<GameObject>();
    private bool puenteHabilitado;
    private bool tieneFusible;
    private bool zonaUsada;
    private float proximoChequeoZona;
    private PlayerSalud salud;
    private CharacterController controllerJugador;
    private Vector3 posicionInicialEnemigo;
    private Quaternion rotacionInicialEnemigo;
    private AudioSource fuenteMusica;

    private bool HayZona => zonaActivacionEnemigo != null || centroZonaEnemigo != null;

    private void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (enemigo != null)
        {
            posicionInicialEnemigo = enemigo.transform.position;
            rotacionInicialEnemigo = enemigo.transform.rotation;
        }

        if (enemigo != null && enemigoEmpiezaOculto)
            enemigo.SetActive(false);

        if (materialesEmpiezanOcultos && HayZona)
            RegistrarYOcultarMateriales();

        if (fusibleEmpiezaOculto)
            RegistrarYOcultarFusibles();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void RegistrarYOcultarMateriales()
    {
        materialesOcultos.Clear();

        Interactuable[] todos = FindObjectsByType<Interactuable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Interactuable item in todos)
        {
            if (item.Tipo != TipoInteraccion.Tronco && item.Tipo != TipoInteraccion.Soga) continue;

            materialesOcultos.Add(item.gameObject);
            item.gameObject.SetActive(false);
        }
    }

    private void PonerMateriales(bool visibles)
    {
        foreach (GameObject material in materialesOcultos)
            if (material != null) material.SetActive(visibles);
    }

    private void RegistrarYOcultarFusibles()
    {
        fusiblesOcultos.Clear();

        Interactuable[] todos = FindObjectsByType<Interactuable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Interactuable item in todos)
        {
            if (item.Tipo != TipoInteraccion.Fusible) continue;

            fusiblesOcultos.Add(item.gameObject);
            item.gameObject.SetActive(false);
        }
    }

    private void PonerFusibles(bool visibles)
    {
        foreach (GameObject fusible in fusiblesOcultos)
            if (fusible != null) fusible.SetActive(visibles);
    }

    private void PonerHudFusible(bool visible)
    {
        if (textoFusible != null) textoFusible.gameObject.SetActive(visible);
        if (iconoFusible != null) iconoFusible.gameObject.SetActive(visible);
    }

    private void ValidarMateriales()
    {
        Interactuable[] todos = FindObjectsByType<Interactuable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int troncos = 0;
        int sogas = 0;

        foreach (Interactuable item in todos)
        {
            if (item.Tipo == TipoInteraccion.Tronco) troncos++;
            else if (item.Tipo == TipoInteraccion.Soga) sogas++;
        }

        if (troncos < troncosNecesarios)
            Debug.LogWarning($"[GameManager] Hay {troncos} troncos en la escena y se necesitan {troncosNecesarios}: el puente no se va a poder construir.", this);

        if (sogas < sogasNecesarias)
            Debug.LogWarning($"[GameManager] Hay {sogas} sogas en la escena y se necesitan {sogasNecesarias}: el puente no se va a poder construir. Revisá que cada soga tenga Interactuable con Tipo = Soga.", this);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        tiempoRestante = minutosDuracion * 60f;
        ActualizarUIObjetos();
        ActualizarUIFusible();
        ActualizarUIObjetivos();

        if (ocultarHudFusibleHastaPuente) PonerHudFusible(false);

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

        if (enemigo != null && enemigoEmpiezaOculto && !HayZona)
            Debug.LogWarning("[GameManager] El enemigo empieza oculto y no hay zona ('Zona Activacion Enemigo' ni 'Centro Zona Enemigo'): recién va a aparecer al construir el puente.", this);

        ValidarMateriales();

        if (fusibleEmpiezaOculto && fusiblesOcultos.Count == 0)
            Debug.LogWarning("[GameManager] 'Fusible Empieza Oculto' está tildado pero no hay ningún Interactuable con Tipo = Fusible en la escena: si el fusible se sigue viendo, su Interactuable no tiene ese tipo.", this);

        Debug.Log($"[GameManager] Escena '{SceneManager.GetActiveScene().name}' lista | zona: {(HayZona ? "sí" : "NO")} | enemigo: {(enemigo != null ? "sí" : "NO")} | troncos y sogas ocultos: {materialesOcultos.Count} | fusibles ocultos: {fusiblesOcultos.Count} | río: {(nivelDelAgua != null ? "sí" : "NO")}", this);

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
        ActualizarMusica();

        ChequearAgua();
        if (partidaTerminada) return;

        ChequearZonaEnemigo();
    }

    private void ChequearAgua()
    {
        if (nivelDelAgua == null) return;

        float alturaPies;
        if (controllerJugador != null) alturaPies = controllerJugador.bounds.min.y;
        else if (salud != null) alturaPies = salud.transform.position.y;
        else return;

        if (alturaPies < nivelDelAgua.position.y - profundidadParaMorir)
            Derrota(mensajeAhogado);
    }

    public void RecolectarTronco()
    {
        if (partidaTerminada || puenteHabilitado) return;

        troncosRecolectados++;
        ProgresoDelPuente();
    }

    public void RecolectarSoga()
    {
        if (partidaTerminada || puenteHabilitado) return;

        sogasRecolectadas++;
        ProgresoDelPuente();
    }

    private void ProgresoDelPuente()
    {
        ActualizarUIObjetos();

        if (troncosRecolectados >= troncosNecesarios && sogasRecolectadas >= sogasNecesarias)
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

        if (musicaTension != null)
            Invoke(nameof(EmpezarMusicaTension), retrasoMusicaTension);
    }

    private void EmpezarMusicaTension()
    {
        if (partidaTerminada || musicaTension == null) return;

        if (fuenteMusica == null)
        {
            fuenteMusica = gameObject.AddComponent<AudioSource>();
            fuenteMusica.playOnAwake = false;
            fuenteMusica.loop = true;
            fuenteMusica.spatialBlend = 0f;
            fuenteMusica.clip = musicaTension;
        }

        fuenteMusica.volume = duracionFundidoMusica <= 0f ? volumenMusicaTension : 0f;
        fuenteMusica.Play();
    }

    private void ActualizarMusica()
    {
        if (fuenteMusica == null || !fuenteMusica.isPlaying) return;

        if (duracionFundidoMusica <= 0f)
        {
            fuenteMusica.volume = volumenMusicaTension;
            return;
        }

        float velocidad = volumenMusicaTension / duracionFundidoMusica;
        fuenteMusica.volume = Mathf.MoveTowards(fuenteMusica.volume, volumenMusicaTension, velocidad * Time.deltaTime);
    }

    public bool ActivarEnemigo()
    {
        if (partidaTerminada) return false;

        PonerMateriales(true);
        PonerFusibles(true);
        PonerHudFusible(true);

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
        if (zonaUsada || !HayZona || puenteHabilitado || salud == null) return;
        if (Time.time < proximoChequeoZona) return;
        proximoChequeoZona = Time.time + 0.2f;

        Transform jugador = salud.transform;

        if (!JugadorActivaLaZona(jugador)) return;

        zonaUsada = true;
        Debug.Log("[GameManager] Zona del enemigo activada.", this);

        ActivarEnemigo();

        if (!string.IsNullOrEmpty(mensajeAparicion))
            MostrarAviso(mensajeAparicion);

        if (sonidoAparicion != null)
        {
            Vector3 posicion = Camera.main != null ? Camera.main.transform.position : jugador.position;
            AudioSource.PlayClipAtPoint(sonidoAparicion, posicion, volumenAparicion);
        }
    }

    private bool JugadorActivaLaZona(Transform jugador)
    {
        if (zonaActivacionEnemigo != null)
            return JugadorEnZonaCaja(jugador);

        Vector3 plano = jugador.position - centroZonaEnemigo.position;
        plano.y = 0f;

        float distanciaPlano = plano.magnitude;

        if (distanciaPlano > radioZonaEnemigo)
        {
            if (mostrarDebugZona && distanciaPlano <= radioZonaEnemigo * 2f && Time.time >= proximoDebugZona)
            {
                proximoDebugZona = Time.time + 2f;
                Debug.Log($"[GameManager] Cerca de la zona pero fuera del radio: estás a {distanciaPlano:0.#} m del centro y el radio es {radioZonaEnemigo:0.#} m.", this);
            }
            return false;
        }

        if (puntoQueDebeMirar != null && !JugadorMiraElPunto(jugador))
        {
            if (mostrarDebugZona && Time.time >= proximoDebugZona)
            {
                proximoDebugZona = Time.time + 2f;
                Debug.Log($"[GameManager] Dentro de la zona, pero todavía no ve el puente: {motivoNoVe}.", this);
            }
            return false;
        }

        return true;
    }

    private bool JugadorEnZonaCaja(Transform jugador)
    {
        if (!zonaActivacionEnemigo.enabled || !zonaActivacionEnemigo.gameObject.activeInHierarchy)
        {
            if (mostrarDebugZona && Time.time >= proximoDebugZona)
            {
                proximoDebugZona = Time.time + 2f;
                Debug.LogWarning("[GameManager] El collider de 'Zona Activacion Enemigo' está desactivado: tiene que estar tildado y el objeto activo.", this);
            }
            return false;
        }

        Vector3 pies = jugador.position;
        Vector3 centro = controllerJugador != null ? controllerJugador.bounds.center : pies + Vector3.up;

        if (PuntoEnZona(pies) || PuntoEnZona(centro)) return true;

        if (mostrarDebugZona && Time.time >= proximoDebugZona)
        {
            float distancia = Vector3.Distance(zonaActivacionEnemigo.ClosestPoint(centro), centro);

            if (distancia <= 15f)
            {
                proximoDebugZona = Time.time + 2f;
                Debug.Log($"[GameManager] Te faltan {distancia:0.#} m para entrar en la zona del enemigo.", this);
            }
        }

        return false;
    }

    private bool PuntoEnZona(Vector3 punto)
    {
        return (zonaActivacionEnemigo.ClosestPoint(punto) - punto).sqrMagnitude < 0.0001f;
    }

    private bool JugadorMiraElPunto(Transform jugador)
    {
        Camera camara = Camera.main;
        if (camara == null) return true;

        Vector3 posicionPunto = puntoQueDebeMirar.position;
        Vector3 enPantalla = camara.WorldToViewportPoint(posicionPunto);

        if (enPantalla.z <= 0f)
        {
            motivoNoVe = "el punto está detrás de la cámara";
            return false;
        }

        if (enPantalla.z > distanciaMaximaVision)
        {
            motivoNoVe = $"el punto está a {enPantalla.z:0.#} m de la cámara y el máximo es {distanciaMaximaVision:0.#} m";
            return false;
        }

        float minimo = margenPantalla;
        float maximo = 1f - margenPantalla;
        if (enPantalla.x < minimo || enPantalla.x > maximo || enPantalla.y < minimo || enPantalla.y > maximo)
        {
            motivoNoVe = $"el punto no está en pantalla (x {enPantalla.x:0.00}, y {enPantalla.y:0.00}; tienen que estar entre {minimo:0.00} y {maximo:0.00})";
            return false;
        }

        Vector3 origen = jugador.position + Vector3.up * alturaOjos;
        Vector3 haciaPunto = posicionPunto - origen;
        float distancia = haciaPunto.magnitude;

        if (distancia <= 0.75f) return true;

        if (Physics.Raycast(origen, haciaPunto / distancia, out RaycastHit golpe, distancia - 0.75f, capasObstaculos, QueryTriggerInteraction.Ignore))
        {
            motivoNoVe = $"'{golpe.collider.name}' tapa la vista";
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (nivelDelAgua != null)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f);
            Vector3 centroAgua = nivelDelAgua.position + Vector3.down * profundidadParaMorir;
            Gizmos.DrawWireCube(centroAgua, new Vector3(60f, 0f, 60f));
        }

        if (zonaActivacionEnemigo != null)
        {
            Gizmos.color = Color.yellow;
            Bounds caja = zonaActivacionEnemigo.bounds;
            Gizmos.DrawWireCube(caja.center, caja.size);
            return;
        }

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
            textoObjetos.text = $"Troncos: {Mathf.Min(troncosRecolectados, troncosNecesarios)}/{troncosNecesarios} - Sogas: {Mathf.Min(sogasRecolectadas, sogasNecesarias)}/{sogasNecesarias}";
    }

    public void RecolectarFusible()
    {
        if (partidaTerminada) return;
        tieneFusible = true;
        ActualizarUIFusible();
        ActualizarUIObjetivos();
    }

    private void ActualizarUIFusible()
    {
        if (textoFusible != null)
            textoFusible.text = tieneFusible ? "Fusible: 1/1" : "Fusible: 0/1";

        if (iconoFusible != null)
            iconoFusible.color = tieneFusible ? colorFusibleObtenido : colorFusibleFaltante;
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
        {
            ReaparecerJugador();
            Invoke(nameof(RearmarEnemigo), 0.1f);
        }
    }

    private void RearmarEnemigo()
    {
        if (!ocultarEnemigoAlReaparecer || puenteHabilitado || partidaTerminada) return;

        bool reinicioAlgo = false;

        if (enemigo != null && enemigoEmpiezaOculto)
        {
            enemigo.SetActive(false);
            enemigo.transform.SetPositionAndRotation(posicionInicialEnemigo, rotacionInicialEnemigo);
            reinicioAlgo = true;
        }

        if (materialesOcultos.Count > 0)
        {
            PonerMateriales(false);
            reinicioAlgo = true;
        }

        if (fusiblesOcultos.Count > 0)
        {
            PonerFusibles(false);
            if (ocultarHudFusibleHastaPuente && !tieneFusible) PonerHudFusible(false);
            reinicioAlgo = true;
        }

        if (!reinicioAlgo) return;

        zonaUsada = false;
        proximoChequeoZona = 0f;

        Debug.Log("[GameManager] Enemigo, troncos y sogas ocultos otra vez: la zona vuelve a estar lista.", this);
    }

    public void JugadorMurio()
    {
        Derrota("¡El bicho te mató!");
    }

    private void ActualizarUIObjetivos()
    {
        if (textoObjetivos == null) return;

        bool hayActivo = false;
        string texto = "<b>OBJETIVOS</b>\n";
        texto += LineaObjetivo($"Juntar troncos ({Mathf.Min(troncosRecolectados, troncosNecesarios)}/{troncosNecesarios}) y sogas ({Mathf.Min(sogasRecolectadas, sogasNecesarias)}/{sogasNecesarias}) para el puente", puenteHabilitado, ref hayActivo);
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