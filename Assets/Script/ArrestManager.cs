using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class ArrestManager : MonoBehaviour
{
    [Header("UI (optional)")]
    public GameObject arrestedPanel;       // 一个Panel（可选）
    public TextMeshProUGUI arrestedText;   // TMP 文本（可选）

    [Header("Freeze")]
    public bool pauseTimeOnArrest = true;

    private bool arrested;
    public bool IsArrested => arrested;

    public void Arrest(string reason)
    {
        if (arrested) return;
        arrested = true;

        Debug.Log($"[ARREST] {reason}");

        if (pauseTimeOnArrest)
            Time.timeScale = 0f;

        if (arrestedPanel != null)
            arrestedPanel.SetActive(true);

        if (arrestedText != null)
            arrestedText.text = $"BUSTED!\n{reason}";
    }

    // Debug reset: Press R
    void Update()
    {
        if (!arrested) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            arrested = false;
            if (arrestedPanel != null) arrestedPanel.SetActive(false);
        }
    }
}