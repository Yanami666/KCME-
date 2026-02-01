using UnityEngine;
using System;

public class BadnessMeter : MonoBehaviour
{
    [Header("Meter Settings")]
    public int maxValue = 120;     // 12格*10
    public int startValue = 20;    // 开局2格
    public int value = 0;

    public event Action<int, int> OnValueChanged; // (value, max)
    public event Action OnMeterFull;

    void Awake()
    {
        value = Mathf.Clamp(startValue, 0, maxValue);
        OnValueChanged?.Invoke(value, maxValue);
    }

    public void Add(int amount)
    {
        if (amount == 0) return;

        int old = value;
        value = Mathf.Clamp(value + amount, 0, maxValue);

        if (value != old)
            OnValueChanged?.Invoke(value, maxValue);

        if (value >= maxValue)
            OnMeterFull?.Invoke();
    }

    public void Reduce(int amount)
    {
        if (amount == 0) return;
        Add(-Mathf.Abs(amount));
    }
}