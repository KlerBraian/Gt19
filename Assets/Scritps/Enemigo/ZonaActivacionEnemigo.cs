using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZonaActivacionEnemigo : MonoBehaviour
{
    [Header("Condición")]
    [SerializeField] private bool soloSiPuenteNoConstruido = true;
    [SerializeField] private Transform puntoQueDebeMirar;
    [SerializeField, Range(0f, 0.4f)] private float margenPantalla = 0.1f;
    [SerializeField] private float distanciaMaximaVision = 40f;
    [SerializeField] private float alturaOjos = 1.6f;
    [SerializeField] private LayerMask capasObstaculos = ~0;
    [SerializeField] private float intervaloChequeo = 0.2f;

    [Header("Aviso")]
    [SerializeField] private string mensaje = "Del otro lado del río se oye algo moverse...";
    [SerializeField] private AudioClip sonidoAparicion;
    [SerializeField, Range(0f, 1f)] private float volumenAparicion = 0.8f;
    [SerializeField] private bool desactivarAlUsarse = true;

    private Transform jugador;
    private float proximoChequeo;
    private bool usada;

    private void Reset()
    {
        Collider colision = GetComponent<Collider>();
        colision.isTrigger = true;
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (usada) return;

        PlayerSalud salud = otro.GetComponentInParent<PlayerSalud>();
        if (salud == null) return;

        jugador = salud.transform;
    }

    private void OnTriggerExit(Collider otro)
    {
        if (jugador == null) return;
        if (otro.GetComponentInParent<PlayerSalud>() == null) return;

        jugador = null;
    }

    private void Update()
    {
        if (usada || jugador == null) return;
        if (Time.time < proximoChequeo) return;
        proximoChequeo = Time.time + intervaloChequeo;

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.EstaTerminada()) return;
        if (soloSiPuenteNoConstruido && gm.PuenteHabilitado) return;
        if (puntoQueDebeMirar != null && !EstaViendoElPunto()) return;

        usada = true;

        if (gm.ActivarEnemigo())
        {
            if (!string.IsNullOrEmpty(mensaje)) gm.MostrarAviso(mensaje);

            if (sonidoAparicion != null)
            {
                Vector3 posicion = Camera.main != null ? Camera.main.transform.position : jugador.position;
                AudioSource.PlayClipAtPoint(sonidoAparicion, posicion, volumenAparicion);
            }
        }

        if (desactivarAlUsarse) gameObject.SetActive(false);
    }

    private bool EstaViendoElPunto()
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
        if (puntoQueDebeMirar == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, puntoQueDebeMirar.position);
        Gizmos.DrawWireSphere(puntoQueDebeMirar.position, 0.5f);
    }
}
