using System.Collections.Generic;

namespace STSlike.Domain.Map
{
    /// <summary>
    /// One stop on the act map.
    ///
    /// Nodes sit on a row/column grid rather than at free positions: the row is
    /// the floor, the column is the lane. Keeping them aligned is what makes the
    /// map readable — the UI only has to add a little jitter to stop it looking
    /// like a spreadsheet, and connections between neighbouring lanes never
    /// cross awkwardly.
    /// </summary>
    public sealed class MapNode
    {
        private readonly List<int> _next = new List<int>();

        public int Id { get; }

        /// <summary>Floor, counting from 0 at the bottom.</summary>
        public int Row { get; }

        /// <summary>Lane, counting from 0 on the left.</summary>
        public int Column { get; }

        public MapNodeType Type { get; internal set; }

        /// <summary>Ids of the nodes on the next floor this one leads to.</summary>
        public IReadOnlyList<int> Next => _next;

        internal MapNode(int id, int row, int column, MapNodeType type)
        {
            Id = id;
            Row = row;
            Column = column;
            Type = type;
        }

        internal void Connect(int nodeId)
        {
            if (!_next.Contains(nodeId))
                _next.Add(nodeId);
        }
    }
}
