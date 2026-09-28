using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ServiceExit : MonoBehaviour
{
    private Collider exitCollider;

    private void Awake()
    {
        exitCollider = GetComponent<Collider>();
        exitCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerStealthState player = other.GetComponentInParent<PlayerStealthState>();
        if (player == null || player.IsHidden) return;

        MissionManager manager = MissionManager.Instance;
        if (manager == null) return;
        if (!manager.TryEscape()) manager.ShowMissingFolder();
    }

    private void OnTriggerStay(Collider other)
    {
        PlayerStealthState player = other.GetComponentInParent<PlayerStealthState>();
        if (player == null || player.IsHidden) return;

        MissionManager manager = MissionManager.Instance;
        if (manager != null && manager.HasFolder) manager.TryEscape();
    }
}
