using UnityEngine;

public class ItemSO : ScriptableObject
{
    public Item itemPrefab;
    public Sprite icon;
    public string itemName;
    public float weight;
    public int saleValue = 1;
}
