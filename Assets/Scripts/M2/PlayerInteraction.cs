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

        if (MissionManager.Instance != null &&
            (!MissionManager.Instance.IsPlaying || MissionManager.Instance.IsCinematic))
        {
            SetSelectedInteractable(null);
            return;
        }

        if (stealthState.IsHidden)
        {
            SetSelectedInteractable(null);
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

        ContextualInteractable nextSelection = previous != null &&
            Mathf.Sqrt(previousDistance) <= Mathf.Sqrt(bestDistance) + selectionHysteresis
                ? previous
                : best;
        SetSelectedInteractable(nextSelection);
    }

    private void SetSelectedInteractable(ContextualInteractable nextSelection)
    {
        if (selectedInteractable == nextSelection) return;
        if (selectedInteractable is HideoutInteractable previousHideout)
            previousHideout.SetPromptSelected(false);
        if (selectedInteractable is MissionFolder previousFolder)
            previousFolder.SetPromptSelected(false);
        if (selectedInteractable is ServicePanel previousServicePanel)
            previousServicePanel.SetPromptSelected(false);

        selectedInteractable = nextSelection;
        if (selectedInteractable is HideoutInteractable nextHideout)
            nextHideout.SetPromptSelected(true);
        if (selectedInteractable is MissionFolder nextFolder)
            nextFolder.SetPromptSelected(true);
        if (selectedInteractable is ServicePanel nextServicePanel)
            nextServicePanel.SetPromptSelected(true);
    }

    private void OnGUI()
    {
        if (selectedInteractable == null || stealthState.IsHidden) return;
        if (selectedInteractable is HideoutInteractable || selectedInteractable is ServicePanel || selectedInteractable is MissionFolder) return;
        GUI.Label(new Rect(Screen.width * .5f - 140f, Screen.height - 78f, 280f, 36f),
            selectedInteractable.InteractionPrompt);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
