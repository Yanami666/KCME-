using UnityEngine;

public class ThiefProgress : MonoBehaviour
{
    [Header("State")]
    public bool IsThiefMode { get; private set; }
    public int TotalSteals { get; private set; }

    [Header("Player Grow")]
    public Transform playerTransform;
    public float growAt3 = 0.15f;
    public float growAt5 = 0.15f;

    private bool grew3;
    private bool grew5;

    public void EnterThiefMode()
    {
        IsThiefMode = true;
    }

    public void RegisterSteal()
    {
        TotalSteals++;

        if (playerTransform == null) return;

        if (!grew3 && TotalSteals >= 3)
        {
            grew3 = true;
            Vector3 s = playerTransform.localScale;
            s.x += growAt3;
            playerTransform.localScale = s;
        }

        if (!grew5 && TotalSteals >= 5)
        {
            grew5 = true;
            Vector3 s = playerTransform.localScale;
            s.x += growAt5;
            playerTransform.localScale = s;
        }
    }
}