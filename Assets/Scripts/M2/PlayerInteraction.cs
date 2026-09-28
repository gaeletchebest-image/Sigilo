using UnityEngine;

[RequireComponent(typeof(PlayerStealthState))]
public sealed class PlayerInteraction : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float interactionRange = 1.8f;
    [SerializeField, Min(0f)] private float selectionHysteresis = .25f;
    [SerializeField] private LayerMask interactionLayers = ~0;
    private readonly Collider[] overlapBuffer = new Collider[32];
    private ControlsController controls;
    private PlayerStealthState stealthState;
    private ContextualInteractable selectedInteractable;

    public ContextualInteractable SelectedInteractable => selectedInteractable;

    private void Awake()
    {
        stealthState = GetComponent<PlayerStealthState>();
        GameController gameController = GameController.Instance;
        if (gameController != null) controls = gameController.GetControlsController();
    }

    private void Update()
    {
        if (controls == null)
        {
            GameController gameController = GameController.Instance;
            if (gameController != null) controls = gameController.GetControlsController();
        }

        if (MissionManager.Instance != null && !MissionManager.Instance.IsPlaying)
        {
            selectedInteractable = null;
            return;
        }

        if (stealthState.IsHidden)
        {
            selectedInteractable = null;
            if (controls != null && controls.InteractPressedThisFrame) stealthState.ExitHideout();
            return;
        }

        RefreshSelection();
        if (controls != null && controls.InteractPressedThisFrame && selectedInteractable != null)
            selectedInteractable.Interact(stealthState);
    }

    private void RefreshSelection()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, interactionRange, overlapBuffer,
            interactionLayers, QueryTriggerInteraction.Ignore);
        ContextualInteractable best = null;
        ContextualInteractable previous = null;
        float bestDistance = float.MaxValue;
        float previousDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider candidateCollider = overlapBuffer[i];
            if (candidateCollider == null) continue;
            ContextualInteractable candidate = candidateCollider.GetComponentInParent<ContextualInteractable>();
            if (candidate == null || !candidate.CanInteract(stealthState)) continue;

            float distance = (candidateCollider.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
            if (candidate == selectedInteractable && distance < previousDistance)
            {
                previous = candidate;
                previousDistance = distance;
            }
            if (distance < bestDistance - .0001f ||
                (Mathf.Abs(distance - bestDistance) <= .0001f &&
                 (best == null || candidate.GetInstanceID() < best.GetInstanceID())))
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        selectedInteractable = previous != null &&
            Mathf.Sqrt(previousDistance) <= Mathf.Sqrt(bestDistance) + selectionHysteresis
                ? previous
                : best;
    }

    private void OnGUI()
    {
        if (selectedInteractable == null || stealthState.IsHidden) return;
        GUI.Label(new Rect(Screen.width * .5f - 140f, Screen.height - 78f, 280f, 36f),
            selectedInteractable.InteractionPrompt);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
