using UnityEngine;

public class ArrestOnTouch2D : MonoBehaviour
{
    [Header("Refs")]
    public ThiefProgress thiefProgress;
    public ArrestManager arrestManager;

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Message")]
    public string arrestReason = "Caught by security.";

    void Awake()
    {
        if (thiefProgress == null) thiefProgress = FindObjectOfType<ThiefProgress>();
        if (arrestManager == null) arrestManager = FindObjectOfType<ArrestManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag(playerTag)) return;
        if (arrestManager != null && arrestManager.IsArrested) return;

        if (thiefProgress != null && thiefProgress.IsThiefMode)
        {
            if (arrestManager != null)
                arrestManager.Arrest(arrestReason);
        }
    }
}