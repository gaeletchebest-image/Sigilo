using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerScr))]
public sealed class PlayerStealthState : MonoBehaviour
{
    private Rigidbody body;
    private CapsuleCollider capsule;
    private PlayerScr playerController;
    private Renderer[] renderers;
    private bool[] rendererWasEnabled;
    private HideoutInteractable currentHideout;
    private bool bodyWasKinematic;

    public bool IsHidden { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerController = GetComponent<PlayerScr>();
        renderers = GetComponentsInChildren<Renderer>(true);
        rendererWasEnabled = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) rendererWasEnabled[i] = renderers[i].enabled;
    }

    public bool EnterHideout(HideoutInteractable hideout)
    {
        if (IsHidden || hideout == null || hideout.IsOccupied || playerController == null) return false;

        currentHideout = hideout;
        bodyWasKinematic = body.isKinematic;
        IsHidden = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        body.position = hideout.PlayerAnchor.position;
        body.rotation = hideout.PlayerAnchor.rotation;
        capsule.enabled = false;
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
        playerController.SetShelterMode(true, hideout.CameraAnchor);
        hideout.NotifyPlayerEntered();
        return true;
    }

    public bool ExitHideout()
    {
        if (!IsHidden || currentHideout == null) return false;

        HideoutInteractable hideout = currentHideout;
        body.position = hideout.ExitPoint.position;
        body.rotation = hideout.ExitPoint.rotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = bodyWasKinematic;
        capsule.enabled = true;
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = rendererWasEnabled[i];
        IsHidden = false;
        currentHideout = null;
        playerController.SetShelterMode(false);
        hideout.NotifyPlayerExited();
        Physics.SyncTransforms();
        return true;
    }

    private void OnGUI()
    {
        if (!IsHidden) return;
        GUI.Label(new Rect(Screen.width * .5f - 100f, Screen.height - 78f, 200f, 36f), "E — salir del armario");
    }
}
