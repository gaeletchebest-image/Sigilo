using UnityEngine;

public class PlayerScr : MonoBehaviour
{

    [SerializeField] float playerSpeed;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minLookAngle = -35f;
    [SerializeField] private float maxLookAngle = 65f;
    [SerializeField, Min(0f)] private float cameraPivotHeight = 1.45f;
    [SerializeField] private float cameraShoulderOffset = 0.3f;
    [SerializeField, Min(0.1f)] private float cameraDistance = 4.2f;
    [Header("Camera collision")]
    [SerializeField] private LayerMask cameraCollisionMask = ~0;
    [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.2f;
    [SerializeField, Min(0f)] private float cameraCollisionPadding = 0.08f;
    [SerializeField, Min(0.1f)] private float cameraProbeStartDistance = 0.75f;
    [SerializeField, Min(0.1f)] private float minimumCameraDistance = 0.9f;
    [SerializeField, Min(0f)] private float cameraReturnSpeed = 5f;
    [Header("Player turning")]
    [SerializeField, Min(0f)] private float playerTurnSpeed = 540f;
    private float cameraRotationX;
    private float cameraRotationY;
    private float playerRotationY;
    private float currentCameraDistance;
    private Transform camera;
    private readonly RaycastHit[] cameraCollisionHits = new RaycastHit[8];

    Rigidbody rb;
    GameController gc;
    ControlsController controls;

    private void Start()
    {
        gc = GameController.Instance;
        controls = gc.GetControlsController();
        rb = GetComponent<Rigidbody>();

        Camera childCamera = cameraTransform.GetComponentInChildren<Camera>();
        if (childCamera == null)
        {
            Debug.LogError("PlayerScr requires a Camera under the assigned camera pivot.", this);
            enabled = false;
            return;
        }
        camera = childCamera.transform;

        // The camera orbit must not inherit the character's rotation.
        cameraTransform.SetParent(null, true);

        // Interpolation smooths the rendered camera/player pose between physics steps.
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        playerRotationY = rb.rotation.eulerAngles.y;
        cameraRotationY = playerRotationY;
        cameraRotationX = 10f;
        currentCameraDistance = cameraDistance;
        camera.localPosition = new Vector3(cameraShoulderOffset, 0f, -currentCameraDistance);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        ReadLookInput();
    }

    private void LateUpdate()
    {
        UpdateCameraOrbit();
        UpdateCameraCollision();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        Vector2 input = Vector2.ClampMagnitude(controls.Move, 1f);
        Vector3 direction = Quaternion.Euler(0f, cameraRotationY, 0f) * new Vector3(input.x, 0f, input.y);
        if (direction.sqrMagnitude > 0.001f)
        {
            playerRotationY = Quaternion.LookRotation(direction, Vector3.up).eulerAngles.y;
            rb.MoveRotation(Quaternion.RotateTowards(
                rb.rotation,
                Quaternion.Euler(0f, playerRotationY, 0f),
                playerTurnSpeed * Time.fixedDeltaTime));
        }
        rb.MovePosition(rb.position + direction * playerSpeed * Time.fixedDeltaTime);
    }

    void ReadLookInput()
    {
        float mouseX = controls.MouseDelta.x * mouseSensitivity;
        float mouseY = controls.MouseDelta.y * mouseSensitivity;

        cameraRotationY += mouseX;

        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, minLookAngle, maxLookAngle);
    }

    void UpdateCameraOrbit()
    {
        cameraTransform.position = transform.position + Vector3.up * cameraPivotHeight;
        cameraTransform.rotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0f);
        camera.localPosition = new Vector3(cameraShoulderOffset, 0f, -currentCameraDistance);
    }

    void UpdateCameraCollision()
    {
        if (cameraTransform == null || camera == null) return;

        float probeStart = Mathf.Min(cameraProbeStartDistance, cameraDistance - minimumCameraDistance);
        Vector3 castOrigin = cameraTransform.TransformPoint(new Vector3(cameraShoulderOffset, 0f, -probeStart));
        Vector3 desiredPosition = cameraTransform.TransformPoint(new Vector3(cameraShoulderOffset, 0f, -cameraDistance));
        Vector3 castVector = desiredPosition - castOrigin;
        float castDistance = castVector.magnitude;

        if (castDistance > 0f)
        {
            Vector3 castDirection = castVector / castDistance;
            int hitCount = Physics.SphereCastNonAlloc(
                castOrigin,
                cameraCollisionRadius,
                castDirection,
                cameraCollisionHits,
                castDistance,
                cameraCollisionMask,
                QueryTriggerInteraction.Ignore);

            float obstructionDistance = cameraDistance;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = cameraCollisionHits[i];
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                obstructionDistance = Mathf.Min(
                    obstructionDistance,
                    probeStart + hit.distance - cameraCollisionPadding);
            }

            float collisionDistance = Mathf.Max(minimumCameraDistance, obstructionDistance);
            if (collisionDistance < currentCameraDistance)
                currentCameraDistance = collisionDistance;
            else
                currentCameraDistance = Mathf.MoveTowards(
                    currentCameraDistance,
                    collisionDistance,
                    cameraReturnSpeed * Time.deltaTime);

            camera.localPosition = new Vector3(cameraShoulderOffset, 0f, -currentCameraDistance);
        }
    }

}
