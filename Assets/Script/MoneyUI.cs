using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    [Header("Refs")]
    public MoneyWallet wallet;
    public TextMeshProUGUI moneyText;

    [Header("Format")]
    public string prefix = "$";
    public bool padTo3Digits = false; // optional

    private int lastValue = int.MinValue;

    void Awake()
    {
        if (wallet == null) wallet = FindObjectOfType<MoneyWallet>();
        Refresh(true);
    }

    void Update()
    {
        Refresh(false);
    }

    private void Refresh(bool force)
    {
        if (wallet == null || moneyText == null) return;

        int v = wallet.Money;
        if (!force && v == lastValue) return;

        lastValue = v;

        if (padTo3Digits)
            moneyText.text = prefix + v.ToString("000");
        else
            moneyText.text = prefix + v.ToString();
    }
}