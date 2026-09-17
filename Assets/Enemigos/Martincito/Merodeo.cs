using Unity.VisualScripting;
using UnityEngine;

public class Merodeo : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 2f;

    [Header("Detección del jugador")]
    public float radioDeteccion = 10f;
    public float radioDeteccionAlerta = 100f;

    private Transform player;
    private Vector3 destino;

    private bool merodeando = true;
    private bool alerta = false;


    void Start()
    {
        // Buscar al jugador por su Tag
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");

        if (jugador != null)
        {
            player = jugador.transform;
        }
        else
        {
            Debug.LogError("No se encontró ningún objeto con el Tag 'Player'.");
        }


        // Buscar Punto b para comenzar el merodeo
        GameObject puntoB = GameObject.FindGameObjectWithTag("Punto b");

        if (puntoB != null)
        {
            destino = puntoB.transform.position;
        }
        else
        {
            Debug.LogError("No se encontró ningún objeto con el Tag 'Punto b'.");
        }
    }


    void Update()
    {
        if (player == null)
            return;


      
        DetectarJugador();
        MirarAlJugador();


    
        if (merodeando)
        {
            Merodear();
        }
        else
        {
          
            Perseguir();
        }
    }


    // =====================================================
    // MERODEO
    // =====================================================

    void Merodear()
    {
        Vector3 direccion = destino - transform.position;

    
        direccion.y = 0;

        if (direccion.magnitude > 0.1f)
        {
            direccion.Normalize();

            transform.position += direccion * velocidad * Time.deltaTime;

            transform.rotation = Quaternion.LookRotation(direccion);
        }
    }


    // =====================================================
    // DETECTAR JUGADOR
    // =====================================================

    void DetectarJugador()
    {
        float distancia = Vector3.Distance(
            transform.position,
            player.position
        );


        float radioActual;

        if (alerta)
        {
            radioActual = radioDeteccionAlerta;
        }
        else
        {
            radioActual = radioDeteccion;
        }


        if (distancia <= radioActual)
        {
            merodeando = false;
        }
    }


    // =====================================================
    // PERSEGUIR
    // =====================================================

    void Perseguir()
    {
        Vector3 direccion = player.position - transform.position;

        direccion.y = 0;

        if (direccion.magnitude > 0.1f)
        {
            direccion.Normalize();

            transform.position += direccion * velocidad * Time.deltaTime;

            transform.rotation = Quaternion.LookRotation(direccion);
        }
    }


    // =====================================================
    // CAMBIAR ENTRE PUNTO A Y PUNTO B
    // =====================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!merodeando)
            return;


        if (other.CompareTag("Punto a"))
        {
            GameObject puntoB =
                GameObject.FindGameObjectWithTag("Punto b");

            if (puntoB != null)
            {
                destino = puntoB.transform.position;
            }
        }

        if (other.CompareTag("Punto b"))
        {
            GameObject puntoA =
                GameObject.FindGameObjectWithTag("Punto a");

            if (puntoA != null)
            {
                destino = puntoA.transform.position;
            }
        }
    }


    // =====================================================
    // ACTIVAR ALERTA
    // =====================================================

    public void ActivarAlerta()
    {
        alerta = true;

        merodeando = false;

        Debug.Log("El enemigo está alerta.");
    }


    // =====================================================
    // MOSTRAR RADIO EN EL EDITOR
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        float radio;

        if (alerta)
        {
            radio = radioDeteccionAlerta;
        }
        else
        {
            radio = radioDeteccion;
        }

        Gizmos.DrawWireSphere(
            transform.position,
            radio
        );
    }
         void MirarAlJugador()
        {
        Vector3 direccion = player.position - transform.position;

        // Evita que mire hacia arriba o hacia abajo
        direccion.y = 0;

        if (direccion != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direccion);
        }

        }
    }

