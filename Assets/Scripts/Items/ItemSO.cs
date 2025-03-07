using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "ScriptableObjects/Items", order = 1)]
public class ItemSO : ScriptableObject
{
    public Item itemPrefab;
    public Sprite icon;
    public string itemName;
    public float weight;
    public int saleValue = 1;
}
