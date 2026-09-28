using UnityEngine;

[RequireComponent(typeof(GuardNavigation))]
public sealed class GuardLookout : MonoBehaviour
{
    [SerializeField, Min(1f)] private float glanceAngle = 55f;
    [SerializeField, Min(0.1f)] private float glanceDuration = 0.75f;
    [SerializeField, Min(0f)] private float turnSpeed = 180f;

    private GuardNavigation navigation;
    private bool wasStopped;
    private Quaternion pauseRotation;
    private float pauseStarted;

    private void Awake() => navigation = GetComponent<GuardNavigation>();

    private void Update()
    {
        bool stopped = navigation.IsStopped;
        if (!stopped)
        {
            wasStopped = false;
            return;
        }

        if (!wasStopped)
        {
            wasStopped = true;
            pauseRotation = transform.rotation;
            pauseStarted = Time.time;
        }

        float phase = (Time.time - pauseStarted) % (glanceDuration * 3f);
        float yaw = phase < glanceDuration ? glanceAngle :
            phase < glanceDuration * 2f ? -glanceAngle : 0f;
        Quaternion target = pauseRotation * Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }
}
