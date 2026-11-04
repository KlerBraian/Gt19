using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerPasos : MonoBehaviour
{
    [Header("Sonidos")]
    [SerializeField] private AudioClip[] pasosTierra;
    [SerializeField] private AudioClip[] pasosMadera;
    [SerializeField] private AudioClip sonidoAterrizaje;
    [SerializeField] private AudioClip sonidoJadeo;

    [Header("Cadencia")]
    [SerializeField] private float distanciaPasoCaminando = 1.6f;
    [SerializeField] private float distanciaPasoCorriendo = 2.1f;
    [SerializeField] private float velocidadMinima = 0.3f;
    [SerializeField] private float velocidadCorrer = 4.5f;
    [SerializeField] private Vector2 variacionTono = new Vector2(0.92f, 1.08f);

    [Header("Volumen")]
    [SerializeField, Range(0f, 1f)] private float volumenCaminando = 0.45f;
    [SerializeField, Range(0f, 1f)] private float volumenCorriendo = 0.8f;
    [SerializeField, Range(0f, 1f)] private float volumenAterrizaje = 0.9f;
    [SerializeField, Range(0f, 1f)] private float volumenJadeo = 0.5f;

    [Header("Superficie")]
    [SerializeField] private string tagMadera = "Madera";
    [SerializeField] private LayerMask capasSuelo = ~0;
    [SerializeField] private float alturaRayo = 0.3f;
    [SerializeField] private float distanciaRayo = 1.5f;

    [Header("Aterrizaje y respiración")]
    [SerializeField] private float velocidadCaidaMinima = 4f;
    [SerializeField] private float segundosCorriendoParaJadear = 4f;
    [SerializeField] private float intervaloJadeo = 4f;

    private CharacterController controller;
    private AudioSource fuente;
    private AudioClip ultimoClip;
    private float distanciaAcumulada;
    private float ultimoMomentoEnSuelo;
    private float caidaMaxima;
    private float tiempoCorriendo;
    private float proximoJadeo;
    private bool tocabaSuelo;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.EstaTerminada()) return;

        bool tocaSuelo = controller.isGrounded;
        Vector3 velocidad = controller.velocity;
        float vertical = velocidad.y;
        velocidad.y = 0f;
        float rapidez = velocidad.magnitude;

        if (tocaSuelo) ultimoMomentoEnSuelo = Time.time;
        bool enSuelo = Time.time - ultimoMomentoEnSuelo < 0.15f;

        if (!tocaSuelo)
        {
            caidaMaxima = Mathf.Max(caidaMaxima, -vertical);
        }
        else if (!tocabaSuelo)
        {
            if (caidaMaxima >= velocidadCaidaMinima) Aterrizar();
            caidaMaxima = 0f;
        }
        tocabaSuelo = tocaSuelo;

        bool corriendo = rapidez >= velocidadCorrer;

        if (enSuelo && rapidez > velocidadMinima)
        {
            distanciaAcumulada += rapidez * Time.deltaTime;
            float largoPaso = corriendo ? distanciaPasoCorriendo : distanciaPasoCaminando;

            if (distanciaAcumulada >= largoPaso)
            {
                distanciaAcumulada = 0f;
                ReproducirPaso(corriendo ? volumenCorriendo : volumenCaminando);
            }
        }
        else if (rapidez <= velocidadMinima)
        {
            distanciaAcumulada = distanciaPasoCaminando * 0.5f;
        }

        ActualizarRespiracion(corriendo && enSuelo);
    }

    private void ActualizarRespiracion(bool corriendo)
    {
        if (corriendo) tiempoCorriendo += Time.deltaTime;
        else tiempoCorriendo = Mathf.Max(0f, tiempoCorriendo - Time.deltaTime * 2f);

        if (sonidoJadeo == null) return;

        if (tiempoCorriendo >= segundosCorriendoParaJadear && Time.time >= proximoJadeo)
        {
            proximoJadeo = Time.time + intervaloJadeo;
            fuente.pitch = 1f;
            fuente.PlayOneShot(sonidoJadeo, volumenJadeo);
        }
    }

    private void Aterrizar()
    {
        if (sonidoAterrizaje == null) return;
        fuente.pitch = 1f;
        fuente.PlayOneShot(sonidoAterrizaje, volumenAterrizaje);
    }

    private void ReproducirPaso(float volumen)
    {
        AudioClip[] lista = EstaEnMadera() && pasosMadera != null && pasosMadera.Length > 0 ? pasosMadera : pasosTierra;
        if (lista == null || lista.Length == 0) return;

        AudioClip clip = lista[Random.Range(0, lista.Length)];
        if (lista.Length > 1 && clip == ultimoClip)
            clip = lista[(System.Array.IndexOf(lista, clip) + 1) % lista.Length];

        ultimoClip = clip;
        fuente.pitch = Random.Range(variacionTono.x, variacionTono.y);
        fuente.PlayOneShot(clip, volumen);
    }

    private bool EstaEnMadera()
    {
        Vector3 origen = transform.position + Vector3.up * alturaRayo;

        if (!Physics.Raycast(origen, Vector3.down, out RaycastHit golpe, distanciaRayo, capasSuelo, QueryTriggerInteraction.Ignore))
            return false;

        Transform actual = golpe.collider.transform;
        while (actual != null)
        {
            if (actual.tag == tagMadera) return true;
            actual = actual.parent;
        }

        return false;
    }
}
