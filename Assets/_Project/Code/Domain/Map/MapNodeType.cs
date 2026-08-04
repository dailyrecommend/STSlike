namespace STSlike.Domain.Map
{
    /// <summary>What happens when the player arrives at a node.</summary>
    public enum MapNodeType
    {
        Combat = 0,
        Elite = 1,
        Shop = 2,
        Rest = 3,
        Event = 4,
        Treasure = 5,
        Boss = 6,
    }
}
