using UnityEngine;

public sealed class ServicePanel : ContextualInteractable
{
    [SerializeField] private BuzzerSignal buzzer;
    [SerializeField, Min(0.1f)] private float interactionRange = 1.8f;
    [SerializeField, Min(0.1f)] private float hearingRange = 16f;
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 20f;
    [SerializeField, Min(0.1f)] private float investigationSeconds = 4f;
    [SerializeField, Min(0.01f)] private float promptFadeDuration = 0.18f;
    [SerializeField] private Vector3 promptLocalPosition = new Vector3(0f, 0.8f, 0f);

    private float availableAt;
    private bool promptSelected;
    private float promptAlpha;
    private GUIStyle promptStyle;
    private GUIStyle statusStyle;

    public bool IsAvailable => Time.time >= availableAt;
    public float CooldownProgress => IsAvailable || cooldownSeconds <= 0f
        ? 1f
        : Mathf.Clamp01(1f - (availableAt - Time.time) / cooldownSeconds);
    public int ActivationCount { get; private set; }
    public override string InteractionPrompt => IsAvailable
        ? "E — activar zumbador"
        : "Zumbador recargando";

    private void Update()
    {
        float targetAlpha = promptSelected ? 1f : 0f;
        promptAlpha = Mathf.MoveTowards(promptAlpha, targetAlpha,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, promptFadeDuration));
    }

    public void Configure(BuzzerSignal signal)
    {
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
        ProceduralAudioFeedback.Instance?.PlayBuzzer();
        if (buzzer != null) buzzer.Ring();
        Vector3 soundOrigin = buzzer != null ? buzzer.transform.position : transform.position;
        GuardBrain[] guards = FindObjectsByType<GuardBrain>(FindObjectsSortMode.None);
        GuardBrain firstResponder = null;
        float nearestResponderDistance = float.MaxValue;
        foreach (GuardBrain guard in guards)
        {
            float distance = Vector3.Distance(guard.transform.position, soundOrigin);
            if (distance > hearingRange || !guard.InvestigateSound(soundOrigin, investigationSeconds))
                continue;

            if (distance < nearestResponderDistance)
            {
                nearestResponderDistance = distance;
                firstResponder = guard;
            }
        }

        player.GetComponent<PlayerScr>()?.PlayBuzzerSequence(
            buzzer != null ? buzzer.transform : transform,
            firstResponder != null ? firstResponder.transform : null);
    }

    public void SetPromptSelected(bool selected)
    {
        promptSelected = selected;
    }

    private void OnGUI()
    {
        if (promptAlpha <= 0.001f) return;
        Camera viewCamera = Camera.main;
        if (viewCamera == null) return;

        Vector3 worldPosition = transform.TransformPoint(promptLocalPosition);
        worldPosition.y += Mathf.Sin(Time.unscaledTime * 2f) * 0.035f;
        Vector3 screenPosition = viewCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f) return;

        const float size = 42f;
        Rect badge = new Rect(screenPosition.x - size * 0.5f,
            Screen.height - screenPosition.y - size * 0.5f, size, size);
        bool available = IsAvailable;
        Color previousColor = GUI.color;
        Color accent = available ? new Color(0.18f, 0.82f, 0.67f) : new Color(0.82f, 0.57f, 0.18f);
        GUI.color = new Color(accent.r, accent.g, accent.b, promptAlpha * 0.2f);
        GUI.DrawTexture(new Rect(badge.x - 4f, badge.y - 4f, size + 8f, size + 8f), Texture2D.whiteTexture);
        GUI.color = new Color(accent.r, accent.g, accent.b, promptAlpha);
        GUI.DrawTexture(badge, Texture2D.whiteTexture);
        GUI.color = new Color(0.035f, 0.07f, 0.075f, promptAlpha * 0.96f);
        GUI.DrawTexture(new Rect(badge.x + 2f, badge.y + 2f, size - 4f, size - 4f), Texture2D.whiteTexture);

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
        }

        promptStyle.normal.textColor = available
            ? new Color(0.9f, 1f, 0.97f, promptAlpha)
            : new Color(0.62f, 0.68f, 0.66f, promptAlpha);
        GUI.color = Color.white;
        GUI.Label(badge, "E", promptStyle);

        if (!available)
        {
            Rect meterFrame = new Rect(badge.x - 11f, badge.yMax + 5f, size + 22f, 11f);
            GUI.color = new Color(accent.r, accent.g, accent.b, promptAlpha * 0.9f);
            GUI.DrawTexture(meterFrame, Texture2D.whiteTexture);
            Rect track = new Rect(meterFrame.x + 1f, meterFrame.y + 1f,
                meterFrame.width - 2f, meterFrame.height - 2f);
            GUI.color = new Color(0.035f, 0.07f, 0.075f, promptAlpha * 0.96f);
            GUI.DrawTexture(track, Texture2D.whiteTexture);
            float progress = CooldownProgress;
            GUI.color = new Color(accent.r, accent.g, accent.b, promptAlpha);
            GUI.DrawTexture(new Rect(track.x, track.y, track.width * progress, track.height),
                Texture2D.whiteTexture);
            float pulseX = track.x + (track.width - 8f) * Mathf.Repeat(Time.unscaledTime * 0.9f, 1f);
            GUI.color = new Color(1f, 0.9f, 0.65f, promptAlpha * 0.9f);
            GUI.DrawTexture(new Rect(pulseX, track.y, 8f, track.height), Texture2D.whiteTexture);
            statusStyle.normal.textColor = new Color(1f, 0.83f, 0.55f, promptAlpha);
            GUI.color = Color.white;
            GUI.Label(new Rect(badge.x - 32f, track.yMax + 1f, badge.width + 64f, 20f),
                "RECARGANDO", statusStyle);
        }

        GUI.color = previousColor;
    }
}
