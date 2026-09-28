using UnityEngine;

public abstract class ContextualInteractable : MonoBehaviour
{
    public abstract string InteractionPrompt { get; }

    public virtual bool CanInteract(PlayerStealthState player)
    {
        return player != null && !player.IsHidden;
    }

    public abstract void Interact(PlayerStealthState player);
}
