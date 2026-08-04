using STSlike.Domain.Map;

namespace STSlike.Domain.Run
{
    /// <summary>
    /// What kind of fight the map asked for. The floor drives scaling and the
    /// node type separates a normal encounter from an elite or the boss, so the
    /// launcher can build the right opponent without knowing about the map.
    /// </summary>
    public readonly struct CombatRequest
    {
        /// <summary>1-based floor number shown to the player.</summary>
        public readonly int Floor;
        public readonly MapNodeType NodeType;

        public CombatRequest(int floor, MapNodeType nodeType)
        {
            Floor = floor;
            NodeType = nodeType;
        }

        public bool IsElite => NodeType == MapNodeType.Elite;
        public bool IsBoss => NodeType == MapNodeType.Boss;
    }
}
