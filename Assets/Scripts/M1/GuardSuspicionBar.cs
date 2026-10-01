using UnityEngine;
using UnityEngine.UI;

public sealed class GuardSuspicionBar : MonoBehaviour
{
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.3f, 0f);

    private GuardBrain brain;
    private Canvas canvas;
    private Image fill;
    private RectTransform fillRect;

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
        fill.color = brain.CurrentState == GuardBrain.State.Chase ? new Color(.9f, .12f, .1f) :
            brain.CurrentState == GuardBrain.State.Investigate ? new Color(1f, .62f, .08f) : new Color(.35f, .85f, .45f);
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
