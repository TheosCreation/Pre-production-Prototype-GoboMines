public enum OreType
{
    Copper,
    Iron,
    Silver,
    Gold,
    Platinum,
    Diamond
}

public class Ore : ItemSO
{
    public OreType oreType;

    public Ore(OreType type, int quantity) : base(type.ToString(), quantity)
    {
        oreType = type;
    }
}
