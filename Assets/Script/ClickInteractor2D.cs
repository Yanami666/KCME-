using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ClickInteractor2D : MonoBehaviour
{
    [Header("Pick")]
    public Camera cam;
    public LayerMask clickableMask = ~0; // Everything by default

    [Header("UI")]
    public bool ignoreClicksOverUI = false; // ✅ set false to avoid UI blocking issues

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (ignoreClicksOverUI && EventSystem.current != null)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryClick();
        }
    }

    private void TryClick()
    {
        if (cam == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(screenPos);
        Vector2 p = new Vector2(world.x, world.y);

        // ✅ stable 2D picking
        Collider2D[] hits = Physics2D.OverlapPointAll(p, clickableMask);
        if (hits == null || hits.Length == 0) return;

        // pick the "topmost" interactable by sprite sortingOrder (rough but practical)
        MonoBehaviour best = null;
        int bestOrder = int.MinValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            var ia = hits[i].GetComponentInParent<InteractableAction2D>();
            var steal = hits[i].GetComponentInParent<StealableNPCItem>();

            MonoBehaviour candidate = (MonoBehaviour)ia != null ? ia : (MonoBehaviour)steal;
            if (candidate == null) continue;

            int order = 0;
            var sr = hits[i].GetComponentInParent<SpriteRenderer>();
            if (sr != null) order = sr.sortingOrder;

            if (order >= bestOrder)
            {
                bestOrder = order;
                best = candidate;
            }
        }

        if (best == null) return;

        if (best is InteractableAction2D a) a.TryInteract();
        else if (best is StealableNPCItem s) s.TryInteract();
    }
}