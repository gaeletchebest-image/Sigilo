using UnityEngine;

public sealed class BuzzerSignal : MonoBehaviour
{
    [SerializeField] private Light signalLight;
    [SerializeField, Min(0.1f)] private float pulseDuration = 2.2f;
    [SerializeField] private Color alertColor = new Color(1f, 0.34f, 0.08f);
    private float pulseUntil;
    private float idleIntensity;
    private Color idleColor;
    private float pulseStartedAt;

    private void Awake()
    {
        if (signalLight == null) signalLight = GetComponentInChildren<Light>();
        if (signalLight != null)
        {
            idleIntensity = signalLight.intensity;
            idleColor = signalLight.color;
        }
    }

    public void Ring()
    {
        pulseStartedAt = Time.unscaledTime;
        pulseUntil = pulseStartedAt + pulseDuration;
    }

    private void Update()
    {
        if (signalLight == null) return;
        if (Time.unscaledTime >= pulseUntil)
        {
            signalLight.intensity = idleIntensity;
            signalLight.color = idleColor;
            return;
        }

        float progress = Mathf.Clamp01((Time.unscaledTime - pulseStartedAt) / pulseDuration);
        float envelope = Mathf.Sin(progress * Mathf.PI);
        float flicker = .7f + .3f * Mathf.Sin((Time.unscaledTime - pulseStartedAt) * 13f);
        signalLight.intensity = Mathf.Max(idleIntensity, .1f) * (1f + 4f * envelope * flicker);
        signalLight.color = Color.Lerp(idleColor, alertColor, envelope);
    }
}
