using System;
using System.Collections.Generic;

namespace STSlike.Domain.Map
{
    /// <summary>
    /// A generated act map: nodes arranged in floors, connected upward.
    ///
    /// Held as a plain graph so the run flow can walk it and the UI can draw it
    /// without either needing to know how it was produced.
    /// </summary>
    public sealed class RunMap
    {
        private readonly List<MapNode> _nodes;
        private readonly List<List<MapNode>> _rows;

        public int RowCount => _rows.Count;
        public IReadOnlyList<MapNode> Nodes => _nodes;

        /// <summary>Where a run can begin: every node on the bottom floor.</summary>
        public IReadOnlyList<MapNode> StartNodes => _rows[0];

        /// <summary>The single node on the top floor.</summary>
        public MapNode Boss => _rows[_rows.Count - 1][0];

        internal RunMap(List<MapNode> nodes, List<List<MapNode>> rows)
        {
            if (nodes == null || nodes.Count == 0)
                throw new ArgumentException("A map needs at least one node.", nameof(nodes));
            if (rows == null || rows.Count == 0)
                throw new ArgumentException("A map needs at least one row.", nameof(rows));

            _nodes = nodes;
            _rows = rows;
        }

        public IReadOnlyList<MapNode> GetRow(int row)
        {
            if (row < 0 || row >= _rows.Count)
                throw new ArgumentOutOfRangeException(nameof(row));
            return _rows[row];
        }

        public MapNode GetNode(int id)
        {
            foreach (MapNode node in _nodes)
            {
                if (node.Id == id)
                    return node;
            }
            throw new ArgumentOutOfRangeException(nameof(id), $"No node with id {id}.");
        }

        /// <summary>The nodes reachable from this one, i.e. the player's choices.</summary>
        public IReadOnlyList<MapNode> NextOf(MapNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var next = new List<MapNode>(node.Next.Count);
            foreach (int id in node.Next)
                next.Add(GetNode(id));
            return next;
        }
    }
}
