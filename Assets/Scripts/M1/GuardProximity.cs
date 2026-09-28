using UnityEngine;

public sealed class GuardProximity : MonoBehaviour
{
    [SerializeField] private GuardBrain brain;

    private void Awake()
    {
        if (brain == null) brain = GetComponentInParent<GuardBrain>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (brain != null && other.CompareTag("Player")) brain.SetProximity(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (brain != null && other.CompareTag("Player")) brain.SetProximity(false);
    }

    private void OnDrawGizmosSelected()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null) return;
        Vector3 scale = transform.lossyScale;
        float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), radius);
    }
}

