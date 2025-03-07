using System;
using UnityEngine;

public class CurrencyManager : Singleton<CurrencyManager>
{
    private int totalMoney = 0;

    public event Action<int> OnMoneyChanged;

    public void AddMoney(int saleValue)
    {
        totalMoney += saleValue;
        OnMoneyChanged?.Invoke(totalMoney);
    }

    public int GetTotalMoney()
    {
        return totalMoney;
    }
}