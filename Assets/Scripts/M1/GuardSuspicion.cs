using UnityEngine;

public sealed class GuardSuspicion : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float visualDetectionSeconds = 2f;
    [SerializeField, Min(0.1f)] private float proximityDetectionSeconds = 1f;
    [SerializeField, Min(0f)] private float decayPerSecond = 14f;
    [SerializeField, Range(0f, 100f)] private float value;

    public float Value => value;

    public void Evaluate(bool visible, bool near, bool preserveFull)
    {
        if (preserveFull)
        {
            value = 100f;
            return;
        }
        if (visible)
            value = Mathf.MoveTowards(value, 100f, 100f / visualDetectionSeconds * Time.deltaTime);
        else if (near)
            value = Mathf.MoveTowards(value, 100f, 100f / proximityDetectionSeconds * Time.deltaTime);
        else
            value = Mathf.MoveTowards(value, 0f, decayPerSecond * Time.deltaTime);
    }

    public void ResetValue() => value = 0f;
}