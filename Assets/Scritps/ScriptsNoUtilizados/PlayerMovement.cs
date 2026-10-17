using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementRigidbody : MonoBehaviour
{
    [SerializeField] private float Speed = 15f;

    private Rigidbody rb;
    private Vector3 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // SOLUCIÓN FÍSICA: Apagamos la fricción angular para que los choques no hagan rotar al personaje
        rb.angularVelocity = Vector3.zero;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(moveX, 0f, moveZ).normalized;
    }

    void FixedUpdate()
    {
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = Camera.main.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDirection = (camForward * moveInput.z + camRight * moveInput.x).normalized;

        Vector3 moveVelocity = moveDirection * Speed;
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);

        if (moveDirection != Vector3.zero)
        {
            // SOLUCIÓN DE ROTACIÓN: Forzamos a que el cálculo ignore por completo cualquier rebote del choque
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 12f);
        }
        else
        {
            // Si el jugador no toca ninguna tecla, matamos cualquier rotación fantasma provocada por el choque
            rb.angularVelocity = Vector3.zero;
        }
    }
}
