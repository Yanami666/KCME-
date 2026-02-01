using UnityEngine;

public class MoneyWallet : MonoBehaviour
{
    public int Money { get; private set; }

    public void AddMoney(int amount)
    {
        Money += Mathf.Max(0, amount);
    }
}