using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class MoneyWallet : MonoBehaviour
{
    [Header("Money")]
    public int startMoney = 50;
    public int Money { get; private set; }

    [Header("UI (TMP, optional)")]
    public TextMeshProUGUI moneyText;
    public string prefix = "$ ";

    [Header("Debug Test Keys (optional)")]
    public bool enableDebugKeys = true;
    public int debugStep = 10; // 每次加/减多少
    public KeyCode addKey = KeyCode.Equals;   // =  (美式键盘) 也就是 + 的那个键
    public KeyCode subKey = KeyCode.Minus;    // -
    public KeyCode setKey = KeyCode.BackQuote; // `  一键重置到startMoney

    void Awake()
    {
        Money = Mathf.Max(0, startMoney);
        RefreshUI();
    }

    void Update()
    {
        if (!enableDebugKeys) return;

        var k = Keyboard.current;
        if (k == null) return;

        // Input System keys (avoid old Input.GetKeyDown)
        if (k.minusKey.wasPressedThisFrame) AddMoney(-debugStep);
        if (k.equalsKey.wasPressedThisFrame) AddMoney(+debugStep);
        if (k.backquoteKey.wasPressedThisFrame) SetMoney(startMoney);

        // 兼容你以后想改键：这里给 KeyCode 字段留着，但不使用旧Input，避免报错
        // 如果你要自定义成别的键，我再帮你把 KeyCode 映射到 InputSystem。
    }

    public void AddMoney(int delta)
    {
        Money = Mathf.Max(0, Money + delta);
        RefreshUI();
    }

    public void SetMoney(int value)
    {
        Money = Mathf.Max(0, value);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (moneyText != null)
            moneyText.text = prefix + Money.ToString();
    }
}