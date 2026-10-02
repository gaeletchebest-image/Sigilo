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
    [Header("Hideout camera")]
    [SerializeField, Range(0f, 80f)] private float hideoutLookYawLimit = 35f;
    [SerializeField, Range(0f, 30f)] private float hideoutLookPitchLimit = 12f;
    [SerializeField, Min(0f)] private float hideoutCameraBlendDuration = 0.3f;
    [Header("Alert camera")]
    [SerializeField, Min(0.05f)] private float alertTransitionDuration = 0.6f;
    [SerializeField, Min(0.05f)] private float alertReturnDuration = 0.45f;
    [SerializeField, Min(0f)] private float buzzerShotHoldDuration = 1f;
    [SerializeField, Min(0f)] private float guardShotHoldDuration = 1f;
    [SerializeField, Min(0.5f)] private float buzzerShotDistance = 2.2f;
    [SerializeField, Min(0f)] private float buzzerShotHeight = 0.7f;
    [SerializeField, Min(0.5f)] private float guardShotDistance = 3.2f;
    [SerializeField, Min(0f)] private float guardShotHeight = 1.2f;
    [Header("Player turning")]
    [SerializeField, Min(0f)] private float playerTurnSpeed = 540f;
    private float cameraRotationX;
    private float cameraRotationY;
    private float playerRotationY;
    private float currentCameraDistance;
    private Transform camera;
    private Transform hideoutCameraAnchor;
    private float hideoutCameraYaw;
    private float hideoutCameraPitch;
    private float hideoutCameraBlendRemaining;
    private Vector3 hideoutCameraBlendStartPosition;
    private Quaternion hideoutCameraBlendStartRotation;
    private Vector3 hideoutCameraBlendStartLocalPosition;
    private float outsideCameraBlendRemaining;
    private Vector3 outsideCameraBlendStartPosition;
    private Quaternion outsideCameraBlendStartRotation;
    private Vector3 outsideCameraBlendStartLocalPosition;
    private enum AlertSequencePhase { None, MoveToBuzzer, HoldBuzzer, MoveToGuard, HoldGuard, ReturnToPlayer }
    private AlertSequencePhase alertSequencePhase;
    private Transform alertBuzzerTarget;
    private Transform alertGuardTarget;
    private Transform alertFocusTarget;
    private bool alertSequenceActive;
    private bool missionCinematicStarted;
    private bool movementWasLockedBeforeAlert;
    private float alertPhaseElapsed;
    private float alertFocusLookHeight;
    private Vector3 alertPhaseStartPosition;
    private Quaternion alertPhaseStartRotation;
    private Vector3 alertCameraOffset;
    private bool movementLocked;
    private readonly RaycastHit[] cameraCollisionHits = new RaycastHit[8];

    Rigidbody rb;
    GameController gc;
    ControlsController controls;
    MissionManager mission;

    private void Start()
    {
        gc = GameController.Instance;
        controls = gc.GetControlsController();
        mission = MissionManager.Instance;
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

        if (mission == null || mission.IsPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        if (mission != null && !mission.IsPlaying) return;
        if (alertSequenceActive) return;
        if (movementLocked)
        {
            if (hideoutCameraAnchor != null && hideoutCameraBlendRemaining <= 0f)
                ReadHideoutLookInput();
            return;
        }

        ReadLookInput();
    }

    private void LateUpdate()
    {
        if (hideoutCameraAnchor != null)
        {
            Quaternion targetRotation = hideoutCameraAnchor.rotation *
                Quaternion.Euler(hideoutCameraPitch, hideoutCameraYaw, 0f);
            if (hideoutCameraBlendRemaining > 0f && hideoutCameraBlendDuration > 0f)
            {
                float progress = 1f - hideoutCameraBlendRemaining / hideoutCameraBlendDuration;
                cameraTransform.position = Vector3.Lerp(
                    hideoutCameraBlendStartPosition, hideoutCameraAnchor.position, progress);
                cameraTransform.rotation = Quaternion.Slerp(
                    hideoutCameraBlendStartRotation, targetRotation, progress);
                camera.localPosition = Vector3.Lerp(
                    hideoutCameraBlendStartLocalPosition, Vector3.zero, progress);
                hideoutCameraBlendRemaining = Mathf.Max(
                    0f, hideoutCameraBlendRemaining - Time.deltaTime);
            }
            else
            {
                cameraTransform.position = hideoutCameraAnchor.position;
                cameraTransform.rotation = targetRotation;
                camera.localPosition = Vector3.zero;
            }
            return;
        }

        UpdateCameraOrbit();
        UpdateCameraCollision();
        if (alertSequenceActive)
        {
            UpdateAlertSequence();
            return;
        }
        if (outsideCameraBlendRemaining > 0f && hideoutCameraBlendDuration > 0f)
        {
            float progress = 1f - outsideCameraBlendRemaining / hideoutCameraBlendDuration;
            progress = progress * progress * (3f - 2f * progress);
            cameraTransform.position = Vector3.Lerp(
                outsideCameraBlendStartPosition, cameraTransform.position, progress);
            cameraTransform.rotation = Quaternion.Slerp(
                outsideCameraBlendStartRotation, cameraTransform.rotation, progress);
            camera.localPosition = Vector3.Lerp(
                outsideCameraBlendStartLocalPosition, camera.localPosition, progress);
            outsideCameraBlendRemaining = Mathf.Max(
                0f, outsideCameraBlendRemaining - Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (mission != null && (!mission.IsPlaying || mission.IsCinematic)) return;
        if (!movementLocked) Move();
    }

    public void SetShelterMode(bool hidden, Transform cameraAnchor = null)
    {
        if (!hidden && hideoutCameraAnchor != null && camera != null && cameraTransform != null)
        {
            outsideCameraBlendStartPosition = cameraTransform.position;
            outsideCameraBlendStartRotation = cameraTransform.rotation;
            outsideCameraBlendStartLocalPosition = camera.localPosition;
            outsideCameraBlendRemaining = hideoutCameraBlendDuration;
        }

        if (hidden && alertSequenceActive) CancelAlertSequence();
        movementLocked = hidden;
        hideoutCameraAnchor = hidden ? cameraAnchor : null;
        hideoutCameraYaw = 0f;
        hideoutCameraPitch = 0f;
        hideoutCameraBlendRemaining = 0f;
        if (hidden) outsideCameraBlendRemaining = 0f;
        if (hidden && cameraAnchor != null && camera != null && cameraTransform != null)
        {
            hideoutCameraBlendStartPosition = cameraTransform.position;
            hideoutCameraBlendStartRotation = cameraTransform.rotation;
            hideoutCameraBlendStartLocalPosition = camera.localPosition;
            hideoutCameraBlendRemaining = hideoutCameraBlendDuration;
        }
        if (!hidden) currentCameraDistance = cameraDistance;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
    }

    public void PlayBuzzerSequence(Transform buzzerTarget, Transform guardTarget)
    {
        if (buzzerTarget == null || camera == null || movementLocked || mission == null ||
            !mission.BeginCinematic()) return;

        missionCinematicStarted = true;
        movementWasLockedBeforeAlert = movementLocked;
        movementLocked = true;
        alertSequenceActive = true;
        alertBuzzerTarget = buzzerTarget;
        alertGuardTarget = guardTarget;
        alertSequencePhase = AlertSequencePhase.MoveToBuzzer;
        BeginAlertCameraMove(alertBuzzerTarget, buzzerShotDistance, buzzerShotHeight, 0.25f);
    }

    private void BeginAlertCameraMove(Transform target, float distance, float height, float lookHeight)
    {
        alertFocusTarget = target;
        alertFocusLookHeight = lookHeight;
        alertPhaseElapsed = 0f;
        alertPhaseStartPosition = camera.position;
        alertPhaseStartRotation = camera.rotation;
        Vector3 horizontalOffset = camera.position - target.position;
        horizontalOffset.y = 0f;
        if (horizontalOffset.sqrMagnitude < 0.01f)
            horizontalOffset = -target.forward;
        alertCameraOffset = horizontalOffset.normalized * distance + Vector3.up * height;
    }

    private void UpdateAlertSequence()
    {
        Vector3 followPosition = camera.position;
        Quaternion followRotation = camera.rotation;
        alertPhaseElapsed += Time.unscaledDeltaTime;

        switch (alertSequencePhase)
        {
            case AlertSequencePhase.MoveToBuzzer:
                if (alertBuzzerTarget == null) { BeginReturnToPlayer(); break; }
                UpdateFocusTransition(alertTransitionDuration);
                if (alertPhaseElapsed >= alertTransitionDuration)
                {
                    ApplyAlertFocusPose(alertBuzzerTarget);
                    alertSequencePhase = AlertSequencePhase.HoldBuzzer;
                    alertPhaseElapsed = 0f;
                }
                break;

            case AlertSequencePhase.HoldBuzzer:
                if (alertBuzzerTarget == null) { BeginReturnToPlayer(); break; }
                ApplyAlertFocusPose(alertBuzzerTarget);
                if (alertPhaseElapsed >= buzzerShotHoldDuration)
                {
                    if (alertGuardTarget != null)
                    {
                        alertSequencePhase = AlertSequencePhase.MoveToGuard;
                        BeginAlertCameraMove(alertGuardTarget, guardShotDistance, guardShotHeight, 1.1f);
                    }
                    else BeginReturnToPlayer();
                }
                break;

            case AlertSequencePhase.MoveToGuard:
                if (alertGuardTarget == null) { BeginReturnToPlayer(); break; }
                UpdateFocusTransition(alertTransitionDuration);
                if (alertPhaseElapsed >= alertTransitionDuration)
                {
                    ApplyAlertFocusPose(alertGuardTarget);
                    alertSequencePhase = AlertSequencePhase.HoldGuard;
                    alertPhaseElapsed = 0f;
                }
                break;

            case AlertSequencePhase.HoldGuard:
                if (alertGuardTarget == null) { BeginReturnToPlayer(); break; }
                ApplyAlertFocusPose(alertGuardTarget);
                if (alertPhaseElapsed >= guardShotHoldDuration)
                    BeginReturnToPlayer();
                break;

            case AlertSequencePhase.ReturnToPlayer:
                float returnDuration = Mathf.Max(0.05f, alertReturnDuration);
                float returnProgress = Mathf.Clamp01(alertPhaseElapsed / returnDuration);
                returnProgress = returnProgress * returnProgress * (3f - 2f * returnProgress);
                camera.position = Vector3.Lerp(alertPhaseStartPosition, followPosition, returnProgress);
                camera.rotation = Quaternion.Slerp(alertPhaseStartRotation, followRotation, returnProgress);
                if (returnProgress >= 1f)
                {
                    camera.position = followPosition;
                    camera.rotation = followRotation;
                    CompleteAlertSequence();
                }
                break;
        }
    }

    private void UpdateFocusTransition(float duration)
    {
        float transitionDuration = Mathf.Max(0.05f, duration);
        float progress = Mathf.Clamp01(alertPhaseElapsed / transitionDuration);
        progress = progress * progress * (3f - 2f * progress);
        GetAlertFocusPose(alertFocusTarget, out Vector3 targetPosition, out Quaternion targetRotation);
        camera.position = Vector3.Lerp(alertPhaseStartPosition, targetPosition, progress);
        camera.rotation = Quaternion.Slerp(alertPhaseStartRotation, targetRotation, progress);
    }

    private void ApplyAlertFocusPose(Transform target)
    {
        GetAlertFocusPose(target, out Vector3 position, out Quaternion rotation);
        camera.position = position;
        camera.rotation = rotation;
    }

    private void GetAlertFocusPose(Transform target, out Vector3 position, out Quaternion rotation)
    {
        Vector3 lookPoint = target.position + Vector3.up * alertFocusLookHeight;
        position = lookPoint + alertCameraOffset;
        rotation = Quaternion.LookRotation(lookPoint - position, Vector3.up);
    }

    private void BeginReturnToPlayer()
    {
        alertSequencePhase = AlertSequencePhase.ReturnToPlayer;
        alertFocusTarget = null;
        alertPhaseElapsed = 0f;
        alertPhaseStartPosition = camera.position;
        alertPhaseStartRotation = camera.rotation;
    }

    private void CompleteAlertSequence()
    {
        alertSequenceActive = false;
        alertSequencePhase = AlertSequencePhase.None;
        alertBuzzerTarget = null;
        alertGuardTarget = null;
        alertFocusTarget = null;
        movementLocked = movementWasLockedBeforeAlert;
        if (missionCinematicStarted && mission != null)
            mission.EndCinematic();
        missionCinematicStarted = false;
    }

    private void CancelAlertSequence()
    {
        if (alertSequenceActive)
            CompleteAlertSequence();
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

    void ReadHideoutLookInput()
    {
        float mouseX = controls.MouseDelta.x * mouseSensitivity;
        float mouseY = controls.MouseDelta.y * mouseSensitivity;

        hideoutCameraYaw = Mathf.Clamp(
            hideoutCameraYaw + mouseX,
            -hideoutLookYawLimit,
            hideoutLookYawLimit);
        hideoutCameraPitch = Mathf.Clamp(
            hideoutCameraPitch - mouseY,
            -hideoutLookPitchLimit,
            hideoutLookPitchLimit);
    }

    void UpdateCameraOrbit()
    {
        cameraTransform.position = transform.position + Vector3.up * cameraPivotHeight;
        cameraTransform.rotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0f);
        camera.localPosition = new Vector3(cameraShoulderOffset, 0f, -currentCameraDistance);
        camera.localRotation = Quaternion.identity;
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
