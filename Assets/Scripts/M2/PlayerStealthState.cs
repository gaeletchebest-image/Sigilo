using System.Collections;
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
    private bool capsuleWasEnabled;
    private bool isTransitioning;
    [SerializeField, Min(0f)] private float doorAnimationDuration = 0.35f;
    [SerializeField, Min(0f)] private float playerTransitionDuration = 0.45f;
    [SerializeField, Min(.01f)] private float exitPromptFadeDuration = .18f;
    private float exitPromptAlpha;
    private GUIStyle exitKeyStyle;
    private GUIStyle exitTextStyle;

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

    private void Update()
    {
        float targetAlpha = IsHidden && !isTransitioning ? 1f : 0f;
        exitPromptAlpha = Mathf.MoveTowards(exitPromptAlpha, targetAlpha,
            Time.deltaTime / Mathf.Max(.01f, exitPromptFadeDuration));
    }

    public bool EnterHideout(HideoutInteractable hideout)
    {
        if (IsHidden || isTransitioning || hideout == null || hideout.IsOccupied || playerController == null)
            return false;

        GuardBrain nearestGuardThatSawEntry = null;
        float nearestGuardDistance = float.PositiveInfinity;
        GuardBrain[] guards = FindObjectsByType<GuardBrain>(FindObjectsSortMode.None);
        foreach (GuardBrain guard in guards)
        {
            if (guard == null || !guard.CanSeePlayerEnteringHideout(this)) continue;
            float distance = (guard.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestGuardDistance)
            {
                nearestGuardDistance = distance;
                nearestGuardThatSawEntry = guard;
            }
        }
        nearestGuardThatSawEntry?.InvestigateHideoutEntry(hideout);

        currentHideout = hideout;
        IsHidden = true;
        isTransitioning = true;
        bodyWasKinematic = body.isKinematic;
        capsuleWasEnabled = capsule.enabled;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        body.isKinematic = true;
        capsule.enabled = false;
        playerController.SetShelterMode(true);
        hideout.NotifyPlayerEntered(this);
        StartCoroutine(EnterHideoutSequence(hideout));
        return true;
    }

public bool ExitHideout()
    {
        if (!IsHidden || isTransitioning || currentHideout == null) return false;

        isTransitioning = true;
        currentHideout.NotifyPlayerExitStarted();
        StartCoroutine(ExitHideoutSequence(currentHideout));
        return true;
    }

    private IEnumerator EnterHideoutSequence(HideoutInteractable hideout)
    {
        yield return hideout.AnimateDoor(true, doorAnimationDuration);
        yield return MovePlayerTo(hideout.EntryPoint, playerTransitionDuration * 0.45f);
        yield return MovePlayerTo(hideout.PlayerAnchor, playerTransitionDuration);

        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
        playerController.SetShelterMode(true, hideout.CameraAnchor);
        yield return hideout.AnimateDoor(false, doorAnimationDuration);
        isTransitioning = false;
        hideout.NotifyPlayerEntryFinished();
    }

    private IEnumerator ExitHideoutSequence(HideoutInteractable hideout)
    {
        yield return hideout.AnimateDoor(true, doorAnimationDuration);

        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = rendererWasEnabled[i];
        playerController.SetShelterMode(false);
        playerController.SetMovementLocked(true);
        yield return MovePlayerTo(hideout.ExitPoint, playerTransitionDuration);

        body.isKinematic = bodyWasKinematic;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        capsule.enabled = capsuleWasEnabled;
        yield return hideout.AnimateDoor(false, doorAnimationDuration);
        IsHidden = false;
        currentHideout = null;
        hideout.NotifyPlayerExited();
        Physics.SyncTransforms();
        playerController.SetMovementLocked(false);
        isTransitioning = false;
    }

    public IEnumerator EjectFromHideoutForCapture(HideoutInteractable hideout, float duration)
    {
        if (!IsHidden || isTransitioning || hideout == null || currentHideout != hideout)
            yield break;

        isTransitioning = true;
        hideout.NotifyPlayerExitStarted();
        IsHidden = false;
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = rendererWasEnabled[i];
        playerController.SetShelterMode(false);
        playerController.SetMovementLocked(true);
        yield return MovePlayerTo(hideout.ExitPoint, duration);

        body.isKinematic = bodyWasKinematic;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        capsule.enabled = capsuleWasEnabled;
        currentHideout = null;
        hideout.NotifyPlayerExited();
        Physics.SyncTransforms();
        playerController.SetMovementLocked(false);
        isTransitioning = false;
    }

    private IEnumerator MovePlayerTo(Transform target, float duration)
    {
        Vector3 startPosition = body.position;
        Quaternion startRotation = body.rotation;
        Vector3 endPosition = target.position;
        Quaternion endRotation = target.rotation;

        if (duration <= 0f)
        {
            body.position = endPosition;
            body.rotation = endRotation;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);
            body.position = Vector3.Lerp(startPosition, endPosition, progress);
            body.rotation = Quaternion.Slerp(startRotation, endRotation, progress);
            yield return null;
        }

        body.position = endPosition;
        body.rotation = endRotation;
    }

    private void OnGUI()
    {
        if (exitPromptAlpha <= .001f) return;

        const float width = 232f;
        const float height = 58f;
        Rect card = new Rect(Screen.width * .5f - width * .5f, Screen.height - height - 34f,
            width, height);
        Rect key = new Rect(card.x + 8f, card.y + 8f, 42f, 42f);
        Color previousColor = GUI.color;

        GUI.color = new Color(.2f, .95f, .76f, exitPromptAlpha * .2f);
        GUI.DrawTexture(new Rect(card.x - 4f, card.y - 4f, width + 8f, height + 8f), Texture2D.whiteTexture);
        GUI.color = new Color(.18f, .82f, .67f, exitPromptAlpha);
        GUI.DrawTexture(card, Texture2D.whiteTexture);
        GUI.color = new Color(.035f, .07f, .075f, exitPromptAlpha * .96f);
        GUI.DrawTexture(new Rect(card.x + 2f, card.y + 2f, width - 4f, height - 4f), Texture2D.whiteTexture);
        GUI.color = new Color(.18f, .82f, .67f, exitPromptAlpha);
        GUI.DrawTexture(key, Texture2D.whiteTexture);
        GUI.color = new Color(.035f, .07f, .075f, exitPromptAlpha * .96f);
        GUI.DrawTexture(new Rect(key.x + 2f, key.y + 2f, key.width - 4f, key.height - 4f),
            Texture2D.whiteTexture);

        if (exitKeyStyle == null)
        {
            exitKeyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            exitTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }
        exitKeyStyle.normal.textColor = new Color(.9f, 1f, .97f, exitPromptAlpha);
        exitTextStyle.normal.textColor = new Color(.9f, 1f, .97f, exitPromptAlpha);
        GUI.color = Color.white;
        GUI.Label(key, "E", exitKeyStyle);
        GUI.Label(new Rect(card.x + 62f, card.y + 6f, width - 72f, height - 12f),
            "SALIR DEL ARMARIO", exitTextStyle);
        GUI.color = previousColor;
    }
}
