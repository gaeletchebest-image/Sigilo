using UnityEngine;

public sealed class GuardPerception : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float visionRange = 9f;
    [SerializeField, Range(1f, 180f)] private float visionAngle = 100f;
    [SerializeField] private LayerMask occluders = ~0;
    private Transform player;
    private PlayerStealthState playerStealth;
    private bool nearPlayer;

    public float VisionRange => visionRange;
    public float VisionAngle => visionAngle;
    public float ProximityRadius => 2f;
    public bool IsNear => !IsPlayerHidden && nearPlayer && HasClearLine();

    private bool IsPlayerHidden => playerStealth != null && playerStealth.IsHidden;

    public void Initialize(Transform target)
    {
        player = target;
        playerStealth = target != null ? target.GetComponent<PlayerStealthState>() : null;
    }
    public void SetProximity(bool value) => nearPlayer = value;

    public bool CanSeePlayer()
    {
        if (player == null || IsPlayerHidden) return false;
        Vector3 delta = player.position + Vector3.up - (transform.position + Vector3.up * 1.5f);
        return delta.magnitude <= visionRange &&
               Vector3.Angle(transform.forward, delta) <= visionAngle * .5f &&
               HasClearLine();
    }

    public bool HasClearLine()
    {
        if (player == null || IsPlayerHidden) return false;
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up;
        Vector3 delta = target - origin;
        if (Physics.Raycast(origin, delta.normalized, out RaycastHit hit, delta.magnitude, occluders, QueryTriggerInteraction.Ignore))
            return hit.transform == player || hit.transform.IsChildOf(player);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Vector3 left = Quaternion.Euler(0, -visionAngle * .5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, visionAngle * .5f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * visionRange);
        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.red : Color.white;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, player.position + Vector3.up);
        }
    }
}
