using UnityEngine;

public class ProximityZone2D : MonoBehaviour
{
    [Header("Receiver")]
    public MonoBehaviour receiver; // InteractableAction2D or StealableNPCItem

    [Header("Player Tag")]
    public string playerTag = "Player";

    void Awake()
    {
        if (receiver == null)
            receiver = GetComponentInParent<MonoBehaviour>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag(playerTag)) return;

        if (receiver is InteractableAction2D a) a.SetInRange(true);
        if (receiver is StealableNPCItem s) s.SetInRange(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other || !other.CompareTag(playerTag)) return;

        if (receiver is InteractableAction2D a) a.SetInRange(false);
        if (receiver is StealableNPCItem s) s.SetInRange(false);
    }
}