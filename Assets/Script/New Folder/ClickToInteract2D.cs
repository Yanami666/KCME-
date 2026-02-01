using UnityEngine;

public class ClickToInteract2D : MonoBehaviour
{
    public InteractableAction2D interactable;

    void Awake()
    {
        if (interactable == null) interactable = GetComponent<InteractableAction2D>();
    }

    void OnMouseDown()
    {
        if (interactable != null)
            interactable.TryInteract();
    }
}