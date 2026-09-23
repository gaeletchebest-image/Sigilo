using UnityEngine;

public class PlayerScr : MonoBehaviour
{

    [SerializeField] float playerSpeed;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float maxLookAngle = 90f;
    private float cameraRotationX;

    Rigidbody rb;
    GameController gc;
    ControlsController controls;

    private void Start()
    {
        gc = GameController.Instance;
        controls = gc.GetControlsController();
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Look();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        if (controls.Move == Vector2.zero) return;

        Vector3 direction = transform.forward * controls.Move.y + transform.right * controls.Move.x;
        rb.MovePosition(rb.position + direction * playerSpeed * Time.fixedDeltaTime);
    }

    void Look()
    {
        float mouseX = controls.MouseDelta.x * mouseSensitivity;
        float mouseY = controls.MouseDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(
            cameraRotationX,
            -maxLookAngle,
            maxLookAngle
        );

        cameraTransform.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
    }

}
