using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float distanciaParaActivar = 5f;
    [SerializeField] private float distanciaParaDesactivar = 6f; // Margen de seguridad
    [SerializeField] private float velocidad = 3f;
    [SerializeField] private float velocidadRotacion = 10f;

    private Animator animator;
    private bool estaPersiguiendo = false; // Bandera de estado interna

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        MovimientoConHisteresis();
    }

    void MovimientoConHisteresis()
    {
        if (player != null)
        {
            float distanciaActual = Vector3.Distance(transform.position, player.transform.position);

            // Si no está persiguiendo y entra al radio cercano
            if (!estaPersiguiendo && distanciaActual <= distanciaParaActivar)
            {
                estaPersiguiendo = true;
            }
            // Si está persiguiendo pero el jugador se aleja más allá del margen amplio
            else if (estaPersiguiendo && distanciaActual >= distanciaParaDesactivar)
            {
                estaPersiguiendo = false;
            }

            // Ejecutamos según el estado estable
            if (estaPersiguiendo)
            {
                // Rotar hacia el jugador
                Vector3 direccion = (player.transform.position - transform.position).normalized;
                direccion.y = 0;
                if (direccion != Vector3.zero)
                {
                    Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
                }

                // Mover
                transform.position = Vector3.MoveTowards(transform.position, player.transform.position, velocidad * Time.deltaTime);

                if (animator != null) animator.SetBool("IsWalking", true);
            }
            else
            {
                if (animator != null) animator.SetBool("IsWalking", false);
            }
        }
    }
}