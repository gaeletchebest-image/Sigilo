using UnityEngine;

public sealed class MissionFolder : ContextualInteractable
{
    [SerializeField, Min(.1f)] private float interactionRange = 1.8f;
    private Renderer[] visuals;
    public bool IsCollected { get; private set; }
    public int SuccessfulInteractions { get; private set; }

    public override string InteractionPrompt => "E — recoger carpeta";

    private void Awake()
    {
        visuals = GetComponentsInChildren<Renderer>(true);
    }

    public override bool CanInteract(PlayerStealthState player)
    {
        MissionManager manager = MissionManager.Instance;
        return base.CanInteract(player) && !IsCollected &&
               Vector3.Distance(player.transform.position, transform.position) <= interactionRange &&
               (manager == null || manager.IsPlaying);
    }

    public override void Interact(PlayerStealthState player)
    {
        if (!CanInteract(player)) return;
        MissionManager manager = MissionManager.Instance;
        if (manager == null || !manager.RegisterFolderCollected()) return;

        IsCollected = true;
        SuccessfulInteractions++;
        foreach (Renderer visual in visuals)
            if (visual != null) visual.enabled = false;
    }
}
