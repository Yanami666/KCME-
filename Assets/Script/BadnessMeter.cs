using UnityEngine;

public class BadnessMeter : MonoBehaviour
{
    [Header("Value (0..120)")]
    public int maxValue = 120;
    public int startValue = 20;   // 开局2格=20
    public int cellSize = 10;     // 1格=10

    [Header("Refs")]
    public BadnessMeterUI_Segments ui;
    public ArrestManager arrestManager;

    [Header("Arrest Rule")]
    public bool arrestWhenFull = true;
    public string arrestReason = "Badness is full.";

    public int Value { get; private set; }

    void Awake()
    {
        Value = Mathf.Clamp(startValue, 0, maxValue);
        RefreshUIAndCheck();
    }

    // ✅ IMPORTANT: ActionExecutor is calling meter.Add(...)
    // So we provide this wrapper to avoid CS1061.
    public void Add(int delta)
    {
        AddValue(delta);
    }

    /// <summary>
    /// +delta / -delta. Example: +20 means +2 cells, +10 means +1 cell, -10 means -1 cell.
    /// </summary>
    public void AddValue(int delta)
    {
        if (arrestManager != null && arrestManager.IsArrested) return;

        Value = Mathf.Clamp(Value + delta, 0, maxValue);
        RefreshUIAndCheck();
    }

    public void SetValue(int newValue)
    {
        if (arrestManager != null && arrestManager.IsArrested) return;

        Value = Mathf.Clamp(newValue, 0, maxValue);
        RefreshUIAndCheck();
    }

    private void RefreshUIAndCheck()
    {
        int maxCells = Mathf.Max(1, maxValue / Mathf.Max(1, cellSize)); // 120/10=12
        int filledCells = Mathf.Clamp(Value / Mathf.Max(1, cellSize), 0, maxCells);

        if (ui != null)
            ui.SetFilled(filledCells, maxCells);

        // ✅ simplest rule: if all 12 cells are filled => arrest
        if (arrestWhenFull && Value >= maxValue)
        {
            if (arrestManager != null && !arrestManager.IsArrested)
                arrestManager.Arrest(arrestReason);
        }
    }
}