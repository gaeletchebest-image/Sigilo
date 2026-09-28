using UnityEngine;

public sealed class BuzzerSignal : MonoBehaviour
{
    [SerializeField] private Light signalLight;
    [SerializeField, Min(0.1f)] private float pulseDuration = 1.5f;
    private float pulseUntil;
    private float idleIntensity;

    private void Awake()
    {
        if (signalLight == null) signalLight = GetComponentInChildren<Light>();
        if (signalLight != null) idleIntensity = signalLight.intensity;
    }

    public void Ring()
    {
        pulseUntil = Time.time + pulseDuration;
        if (signalLight != null) signalLight.intensity = Mathf.Max(idleIntensity, 0.1f) * 4f;
    }

    private void Update()
    {
        if (signalLight != null && Time.time >= pulseUntil)
            signalLight.intensity = idleIntensity;
    }
}
