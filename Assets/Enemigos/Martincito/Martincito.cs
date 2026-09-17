using UnityEngine;

public class Martincito : MonoBehaviour
{
    [SerializeField] private float radioDeteccion = 1000f;
    private Transform player;

    void Start()
    {
        GameObject objetoPlayer = GameObject.FindGameObjectWithTag("Player");

        if (objetoPlayer != null)
        {
            player = objetoPlayer.transform;
        }
        else
        {
            Debug.LogError("No se encontró ningún objeto con el Tag 'Player'.");
        }
    }

    void Update()
    {
        if (player == null)
            return;

        float distancia = Vector3.Distance(transform.position, player.position);

        if (distancia <= radioDeteccion)
        {
            Vector3 direccion = player.position;

            // Mirar al jugador
            transform.rotation = Quaternion.LookRotation(direccion);
        }
    }


}
