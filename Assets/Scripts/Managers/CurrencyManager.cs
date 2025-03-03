using System;
using UnityEngine;

public class CurrencyManager : Singleton<CurrencyManager>
{
    private float totalMoney = 0f;

    public event Action<float> OnMoneyChanged;

    public void AddMoney(float saleValue)
    {
        totalMoney += saleValue;
        OnMoneyChanged?.Invoke(totalMoney);
    }

    public float GetTotalMoney()
    {
        return totalMoney;
    }
}