using UnityEngine;

public class VisibilityProbe2D : MonoBehaviour
{
    [Tooltip("How many FOV triggers are overlapping this object right now.")]
    [SerializeField] private int seenCount = 0;

    public bool IsSeen => seenCount > 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<NPCFOV>() != null)
        {
            seenCount++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<NPCFOV>() != null)
        {
            seenCount = Mathf.Max(0, seenCount - 1);
        }
    }
}