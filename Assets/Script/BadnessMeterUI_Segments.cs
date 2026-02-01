using UnityEngine;
using UnityEngine.UI;

public class BadnessMeterUI_Segments : MonoBehaviour
{
    [Header("12 segment Images (left -> right)")]
    public Image[] segments;

    [Header("Colors")]
    public Color emptyColor = new Color(1f, 1f, 1f, 0.25f);
    public Color filledColor = new Color(1f, 0.2f, 0.2f, 1f);

    /// <summary>
    /// filled = how many segments are filled (0..12)
    /// max is kept for compatibility; we use segments.Length as truth.
    /// </summary>
    public void SetFilled(int filled, int max)
    {
        if (segments == null) return;

        int n = segments.Length;
        int clamped = Mathf.Clamp(filled, 0, n);

        for (int i = 0; i < n; i++)
        {
            if (segments[i] == null) continue;
            segments[i].color = (i < clamped) ? filledColor : emptyColor;
        }
    }
}