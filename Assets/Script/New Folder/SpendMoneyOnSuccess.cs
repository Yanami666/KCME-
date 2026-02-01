using UnityEngine;

public class SpendMoneyOnSuccess : MonoBehaviour
{
    public MoneyWallet wallet;

    [Range(0f, 1f)] public float percent = 0.10f; // 10%
    public int minCost = 1;

    [System.Obsolete]
    void Awake()
    {
        if (wallet == null) wallet = FindObjectOfType<MoneyWallet>();
    }

    // 给 UnityEvent 绑定用
    public void SpendPercent()
    {
        if (wallet == null) return;
        if (wallet.Money <= 0) return;

        int cost = Mathf.CeilToInt(wallet.Money * percent);
        cost = Mathf.Max(minCost, cost);

        wallet.AddMoney(-cost);
    }
}