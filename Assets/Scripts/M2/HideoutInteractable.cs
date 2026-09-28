using UnityEngine;

public sealed class HideoutInteractable : ContextualInteractable
{
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(.1f)] private float interactionRange = 1.8f;

    public Transform PlayerAnchor => playerAnchor != null ? playerAnchor : transform;
    public Transform CameraAnchor => cameraAnchor != null ? cameraAnchor : PlayerAnchor;
    public Transform ExitPoint => exitPoint != null ? exitPoint : transform;
    public bool IsOccupied { get; private set; }
    public override string InteractionPrompt => "E — esconderse";

    private void Awake()
    {
        if (playerAnchor == null) playerAnchor = transform.Find("PlayerAnchor");
        if (cameraAnchor == null) cameraAnchor = transform.Find("CameraAnchor");
        if (exitPoint == null) exitPoint = transform.Find("ExitPoint");
    }

    public override bool CanInteract(PlayerStealthState player)
    {
        return base.CanInteract(player) && !IsOccupied &&
               Vector3.Distance(player.transform.position, transform.position) <= interactionRange;
    }

    public override void Interact(PlayerStealthState player)
    {
        if (CanInteract(player)) player.EnterHideout(this);
    }

    public void NotifyPlayerEntered() => IsOccupied = true;
    public void NotifyPlayerExited() => IsOccupied = false;
}
