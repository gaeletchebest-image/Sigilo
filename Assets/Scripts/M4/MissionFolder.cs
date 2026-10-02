using UnityEngine;


public sealed class MissionFolder : ContextualInteractable
{
    [SerializeField, Min(0.01f)] private float promptFadeDuration = 0.18f;
    [SerializeField] private Vector3 promptLocalPosition = new Vector3(0f, 0.8f, 0f);
    private bool promptSelected;
    private float promptAlpha;
    private GUIStyle promptStyle;
    [SerializeField, Min(.1f)] private float interactionRange = 1.8f;
    private Renderer[] visuals;
    public bool IsCollected { get; private set; }
    public int SuccessfulInteractions { get; private set; }

    public override string InteractionPrompt => "E — recoger carpeta";

    private void Update()
    {
        float targetAlpha = promptSelected && !IsCollected ? 1f : 0f;
        promptAlpha = Mathf.MoveTowards(promptAlpha, targetAlpha,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, promptFadeDuration));
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
        Color previousColor = GUI.color;
        GUI.color = new Color(0.18f, 0.82f, 0.67f, promptAlpha * 0.2f);
        GUI.DrawTexture(new Rect(badge.x - 4f, badge.y - 4f, size + 8f, size + 8f), Texture2D.whiteTexture);
        GUI.color = new Color(0.18f, 0.82f, 0.67f, promptAlpha);
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
        }
        promptStyle.normal.textColor = new Color(0.9f, 1f, 0.97f, promptAlpha);
        GUI.color = Color.white;
        GUI.Label(badge, "E", promptStyle);
        GUI.color = previousColor;
    }


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
        promptSelected = false;
    }
}
