using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform cam;
    public float mouseSensitivity = 0.12f;
    public float followSmoothness = 10f; // que tan suave la camara sigue al jugador

    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpForce = 7f;

    private Rigidbody rb;
    private float yaw;
    private float pitch;
    private Vector3 cameraOffset; // la distancia de la camara

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.angularVelocity = Vector3.zero;

        Cursor.lockState = CursorLockMode.Locked;

     
        if (cam != null)
        {
            cameraOffset = cam.position - transform.position;

            // inicia la rotación del mouse con la rotacion que ya tiene la camara puesta
            yaw = cam.eulerAngles.y;
            pitch = cam.eulerAngles.x;
        }
    }

    void Update()
    {
        Look();
        Jump();
    }

    void FixedUpdate()
    {
        Move();
    }

    void LateUpdate()
    {
        PositionCamera();
    }

    void Look()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -30f, 60f); // Evita giros raros en vertical

        // Rota al jugador en base al giro horizontal del mouse
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    Vector3 GetMoveInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector3.zero;

        float x = 0f;
        float z = 0f;

        if (kb.wKey.isPressed) z += 1f;
        if (kb.sKey.isPressed) z -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.aKey.isPressed) x -= 1f;

        return new Vector3(x, 0f, z).normalized;
    }

    void Move()
    {
        Vector3 input = GetMoveInput();
        Vector3 moveDirection = transform.forward * input.z + transform.right * input.x;

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;

        if (input == Vector3.zero)
        {
            rb.angularVelocity = Vector3.zero;
        }
    }

    void Jump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    void PositionCamera()
    {
        if (cam == null) return;

        // calcula la rotacion deseada con el mouse
        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0f);

        // mantiene la perspectiva sobre el hombro 
        Vector3 targetCamPosition = transform.position + (Quaternion.Euler(0f, yaw, 0f) * cameraOffset);

        // aplicamos la posicion y rotación de forma fluida
        cam.position = Vector3.Lerp(cam.position, targetCamPosition, Time.deltaTime * followSmoothness);
        cam.rotation = camRotation;
    }
}