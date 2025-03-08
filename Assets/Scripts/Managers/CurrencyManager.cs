using System;
using UnityEngine;

public class CurrencyManager : Singleton<CurrencyManager>
{
    [SerializeField] private int totalMoney = 0;

    public event Action<int> OnMoneyChanged;

    public void AddMoney(int saleValue)
    {
        totalMoney += saleValue;
        OnMoneyChanged?.Invoke(totalMoney);
    }

    public void TakeMoney(int saleValue)
    {
        totalMoney -= saleValue;
        OnMoneyChanged?.Invoke(totalMoney);
    }

    public int GetTotalMoney()
    {
        return totalMoney;
    }
}