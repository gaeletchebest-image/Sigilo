using UnityEngine;
using UnityEngine.UI;

public sealed class GuardSuspicionBar : MonoBehaviour
{
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.3f, 0f);

    private GuardBrain brain;
    private Canvas canvas;
    private Image fill;
    private RectTransform fillRect;
    private Text attentionLabel;
    private RectTransform attentionRect;

    private void Awake()
    {
        brain = GetComponent<GuardBrain>();
        GameObject barObject = new GameObject("SuspicionBar", typeof(RectTransform), typeof(Canvas));
        barObject.transform.SetParent(transform, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.localPosition = localOffset;
        barRect.localScale = Vector3.one * .01f;
        barRect.sizeDelta = new Vector2(100f, 12f);

        canvas = barObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;

        Image background = CreateImage("Background", barRect);
        background.color = new Color(.04f, .05f, .06f, .9f);

        fill = CreateImage("Fill", barRect);
        fill.color = new Color(.35f, .85f, .45f);
        fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, .5f);
        fillRect.sizeDelta = Vector2.zero;

        GameObject attentionObject = new GameObject("SoundAttention", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text));
        attentionObject.transform.SetParent(barRect, false);
        attentionRect = attentionObject.GetComponent<RectTransform>();
        attentionRect.anchorMin = new Vector2(.5f, .5f);
        attentionRect.anchorMax = new Vector2(.5f, .5f);
        attentionRect.pivot = new Vector2(.5f, .5f);
        attentionRect.sizeDelta = new Vector2(180f, 36f);
        attentionRect.anchoredPosition = new Vector2(0f, 38f);
        attentionLabel = attentionObject.GetComponent<Text>();
        attentionLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        attentionLabel.text = "¡TE ESCUCHÓ!";
        attentionLabel.fontSize = 22;
        attentionLabel.fontStyle = FontStyle.Bold;
        attentionLabel.alignment = TextAnchor.MiddleCenter;
        attentionLabel.color = new Color(1f, .72f, .12f);
        attentionLabel.raycastTarget = false;
        Outline outline = attentionObject.AddComponent<Outline>();
        outline.effectColor = new Color(.15f, .035f, .01f, .95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        attentionObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (brain == null || canvas == null) return;

        MissionManager mission = MissionManager.Instance;
        canvas.enabled = mission == null || mission.IsPlaying;
        if (!canvas.enabled) return;

        Camera view = Camera.main;
        if (view != null)
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - view.transform.position);

        fillRect.anchorMax = new Vector2(Mathf.Clamp01(brain.Suspicion / 100f), 1f);
        if (attentionLabel != null)
        {
            bool alertedBySound = brain.IsSoundAlerted;
            attentionLabel.gameObject.SetActive(alertedBySound);
            if (alertedBySound)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * .08f;
                attentionRect.localScale = Vector3.one * pulse;
                attentionRect.anchoredPosition = new Vector2(0f, 38f + Mathf.Sin(Time.unscaledTime * 5f) * 2f);
                attentionLabel.color = Color.Lerp(new Color(1f, .55f, .08f),
                    new Color(1f, .9f, .35f), (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * .5f);
            }
        }
        fill.color = brain.CurrentState == GuardBrain.State.Chase ? new Color(.9f, .12f, .1f) :
            brain.CurrentState == GuardBrain.State.Investigate || brain.CurrentState == GuardBrain.State.Search
                ? new Color(1f, .62f, .08f)
                : new Color(.35f, .85f, .45f);
    }

    private static Image CreateImage(string objectName, RectTransform parent)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
}
