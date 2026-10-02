using System.Collections;
using UnityEngine;

public sealed class HideoutInteractable : ContextualInteractable
{
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform doorPivot;
    [SerializeField, Min(.1f)] private float interactionRange = 1.8f;
    [SerializeField, Range(0f, 90f)] private float frontAccessAngle = 55f;
    [SerializeField, Range(0f, 90f)] private float playerFacingAngle = 75f;
    [SerializeField] private float doorOpenAngle = -105f;
    [SerializeField, Min(.01f)] private float promptFadeDuration = .18f;
    [SerializeField] private Vector3 promptLocalPosition = new Vector3(0f, 2f, .74f);

    public Transform PlayerAnchor => playerAnchor != null ? playerAnchor : transform;
    public Transform CameraAnchor => cameraAnchor != null ? cameraAnchor : PlayerAnchor;
    public Transform EntryPoint => entryPoint != null ? entryPoint : ExitPoint;
    public Transform ExitPoint => exitPoint != null ? exitPoint : transform;
    public Transform DoorPivot => doorPivot;
    public bool IsOccupied { get; private set; }
    public PlayerStealthState Occupant { get; private set; }
    public bool IsPlayerTransitioning { get; private set; }
    public override string InteractionPrompt => "E — esconderse";
    private Quaternion doorClosedRotation;
    private bool promptSelected;
    private float promptAlpha;
    private GUIStyle promptStyle;
    private Collider[] cabinetColliders;
    private bool[] colliderWasEnabled;

    private void Update()
    {
        float targetAlpha = promptSelected && !IsOccupied ? 1f : 0f;
        promptAlpha = Mathf.MoveTowards(promptAlpha, targetAlpha,
            Time.deltaTime / Mathf.Max(.01f, promptFadeDuration));
    }

    private void Awake()
    {
        if (playerAnchor == null) playerAnchor = transform.Find("PlayerAnchor");
        if (cameraAnchor == null) cameraAnchor = transform.Find("CameraAnchor");
        if (entryPoint == null) entryPoint = transform.Find("EntryPoint");
        if (exitPoint == null) exitPoint = transform.Find("ExitPoint");
        if (doorPivot == null) doorPivot = transform.Find("DoorPivot");
        if (doorPivot != null) doorClosedRotation = doorPivot.localRotation;
        cabinetColliders = GetComponentsInChildren<Collider>(true);
        colliderWasEnabled = new bool[cabinetColliders.Length];
        for (int i = 0; i < cabinetColliders.Length; i++)
            colliderWasEnabled[i] = cabinetColliders[i].enabled;
    }

    public override bool CanInteract(PlayerStealthState player)
    {
        if (!base.CanInteract(player) || IsOccupied) return false;

        Vector3 toPlayer = Vector3.ProjectOnPlane(
            player.transform.position - transform.position, transform.up);
        if (toPlayer.sqrMagnitude < .0001f || toPlayer.magnitude > interactionRange) return false;

        Vector3 directionToPlayer = toPlayer.normalized;
        float frontThreshold = Mathf.Cos(frontAccessAngle * Mathf.Deg2Rad);
        if (Vector3.Dot(transform.forward, directionToPlayer) < frontThreshold) return false;

        Vector3 playerForward = Vector3.ProjectOnPlane(player.transform.forward, transform.up);
        if (playerForward.sqrMagnitude < .0001f) return false;
        float facingThreshold = Mathf.Cos(playerFacingAngle * Mathf.Deg2Rad);
        return Vector3.Dot(playerForward.normalized, -directionToPlayer) >= facingThreshold;
    }

    public override void Interact(PlayerStealthState player)
    {
        if (CanInteract(player))
        {
            SetPromptSelected(false);
            player.EnterHideout(this);
        }
    }

    public void SetPromptSelected(bool selected)
    {
        promptSelected = selected;
    }

    public IEnumerator AnimateDoor(bool open, float duration)
    {
        if (open) SetCabinetCollisionForDoor(true);
        if (doorPivot == null)
        {
            if (!open) SetCabinetCollisionForDoor(false);
            yield break;
        }

        Quaternion start = doorPivot.localRotation;
        Quaternion target = doorClosedRotation * Quaternion.Euler(0f, open ? doorOpenAngle : 0f, 0f);
        if (duration <= 0f)
        {
            doorPivot.localRotation = target;
            if (!open) SetCabinetCollisionForDoor(false);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);
            doorPivot.localRotation = Quaternion.Slerp(start, target, progress);
            yield return null;
        }

        doorPivot.localRotation = target;
        if (!open) SetCabinetCollisionForDoor(false);
    }

    private void SetCabinetCollisionForDoor(bool doorOpen)
    {
        if (cabinetColliders == null) return;
        for (int i = 0; i < cabinetColliders.Length; i++)
        {
            Collider cabinetCollider = cabinetColliders[i];
            if (cabinetCollider == null) continue;
            cabinetCollider.enabled = doorOpen ? false : colliderWasEnabled[i];
        }
    }

    private void OnGUI()
    {
        if (promptAlpha <= .001f) return;
        Camera viewCamera = Camera.main;
        if (viewCamera == null) return;

        Vector3 worldPosition = transform.TransformPoint(promptLocalPosition);
        worldPosition.y += Mathf.Sin(Time.unscaledTime * 2f) * .035f;
        Vector3 screenPosition = viewCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f) return;

        const float size = 42f;
        Rect badge = new Rect(screenPosition.x - size * .5f,
            Screen.height - screenPosition.y - size * .5f, size, size);
        Color previousColor = GUI.color;
        GUI.color = new Color(.2f, .95f, .76f, promptAlpha * .2f);
        GUI.DrawTexture(new Rect(badge.x - 4f, badge.y - 4f, size + 8f, size + 8f), Texture2D.whiteTexture);
        GUI.color = new Color(.18f, .82f, .67f, promptAlpha);
        GUI.DrawTexture(badge, Texture2D.whiteTexture);
        GUI.color = new Color(.035f, .07f, .075f, promptAlpha * .96f);
        GUI.DrawTexture(new Rect(badge.x + 2f, badge.y + 2f, size - 4f, size - 4f), Texture2D.whiteTexture);

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
        }
        promptStyle.normal.textColor = new Color(.9f, 1f, .97f, promptAlpha);
        GUI.color = Color.white;
        GUI.Label(badge, "E", promptStyle);
        GUI.color = previousColor;
    }

    public void NotifyPlayerEntered(PlayerStealthState player)
    {
        Occupant = player;
        IsOccupied = player != null;
        IsPlayerTransitioning = true;
    }

    public void NotifyPlayerEntryFinished() => IsPlayerTransitioning = false;

    public void NotifyPlayerExitStarted()
    {
        IsOccupied = false;
        IsPlayerTransitioning = true;
    }

    public void NotifyPlayerExited()
    {
        IsOccupied = false;
        Occupant = null;
        IsPlayerTransitioning = false;
    }
}
