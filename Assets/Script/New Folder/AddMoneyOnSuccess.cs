using UnityEngine;

public class AddMoneyOnSuccess : MonoBehaviour
{
    public MoneyWallet wallet;
    public int addAmount = 25;

    void Awake()
    {
        if (wallet == null) wallet = FindObjectOfType<MoneyWallet>();
    }

    public void Add()
    {
        if (wallet == null) return;
        wallet.AddMoney(addAmount);
    }
}