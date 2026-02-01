using UnityEngine;
using UnityEngine.UI;

public class BadnessMeterUI_Segments : MonoBehaviour
{
    [Header("References")]
    public BadnessMeter meter;
    public Image[] segments; // 12个Image拖进来

    [Header("Visual")]
    public Color onColor = Color.red;
    public Color offColor = new Color(1f, 1f, 1f, 0.2f);

    void Start()
    {
        if (meter != null)
        {
            meter.OnValueChanged += Refresh;
            Refresh(meter.value, meter.maxValue);
        }
    }

    void OnDestroy()
    {
        if (meter != null)
            meter.OnValueChanged -= Refresh;
    }

    private void Refresh(int value, int max)
    {
        if (segments == null || segments.Length == 0) return;

        int filled = Mathf.Clamp(value / 10, 0, segments.Length);

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null) continue;
            segments[i].color = (i < filled) ? onColor : offColor;
        }
    }
}