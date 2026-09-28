using UnityEngine;

public sealed class ServicePanel : ContextualInteractable
{
    [SerializeField] private GuardBrain respondingGuard;
    [SerializeField] private BuzzerSignal buzzer;
    [SerializeField, Min(0.1f)] private float interactionRange = 1.8f;
    [SerializeField, Min(0.1f)] private float hearingRange = 16f;
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 20f;
    [SerializeField, Min(0.1f)] private float investigationSeconds = 4f;

    private float availableAt;

    public bool IsAvailable => Time.time >= availableAt;
    public float CooldownRemaining => Mathf.Max(0f, availableAt - Time.time);
    public int ActivationCount { get; private set; }
    public override string InteractionPrompt => IsAvailable
        ? "E — activar zumbador"
        : $"Panel en recarga: {Mathf.CeilToInt(CooldownRemaining)} s";

    public void Configure(GuardBrain guard, BuzzerSignal signal)
    {
        respondingGuard = guard;
        buzzer = signal;
    }

    public override bool CanInteract(PlayerStealthState player)
    {
        return base.CanInteract(player) &&
               Vector3.Distance(player.transform.position, transform.position) <= interactionRange;
    }

    public override void Interact(PlayerStealthState player)
    {
        if (!CanInteract(player) || !IsAvailable) return;

        ActivationCount++;
        availableAt = Time.time + cooldownSeconds;
        if (buzzer != null) buzzer.Ring();
        if (respondingGuard != null &&
            Vector3.Distance(respondingGuard.transform.position, buzzer != null ? buzzer.transform.position : transform.position) <= hearingRange)
        {
            respondingGuard.InvestigateSound(buzzer != null ? buzzer.transform.position : transform.position,
                investigationSeconds);
        }
    }
}
