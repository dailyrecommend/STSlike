using System;
using System.Collections.Generic;
using STSlike.Domain.Randomness;

namespace STSlike.Domain.Map
{
    /// <summary>
    /// Builds an act map by walking several routes from the bottom floor to the
    /// top and letting them share nodes where they meet.
    ///
    /// Growing the map from paths rather than scattering nodes and connecting
    /// them afterwards guarantees the two properties that matter: every node is
    /// reachable from the start, and every node leads somewhere. A map that
    /// merely looks plausible can still strand the player on a dead end.
    ///
    /// Each step moves at most one lane sideways, which is what keeps the result
    /// tidy — connections only ever join neighbouring lanes, so nothing crosses
    /// at an awkward angle.
    /// </summary>
    public sealed class MapGenerator : IMapGenerator
    {
        public RunMap Generate(IRng rng, MapGenerationSettings settings)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            settings.Validate();

            int bossRow = settings.RowCount - 1;
            var rows = NewRows(settings.RowCount);
            var nodes = new List<MapNode>();

            // Edges already drawn between each pair of floors, so later routes
            // can avoid crossing them.
            var edges = new List<List<Edge>>(Math.Max(0, bossRow));
            for (int i = 0; i < bossRow; i++)
                edges.Add(new List<Edge>());

            // Walk the routes. Floors below the boss are built here; the boss
            // floor is added afterwards so every route funnels into it.
            for (int path = 0; path < settings.PathCount; path++)
            {
                int column = rng.Next(settings.Columns);
                MapNode current = GetOrCreate(rows, nodes, row: 0, column: column);

                for (int row = 1; row < bossRow; row++)
                {
                    int nextColumn = ChooseNextColumn(rng, column, settings.Columns, edges[row - 1]);
                    MapNode next = GetOrCreate(rows, nodes, row, nextColumn);

                    current.Connect(next.Id);
                    edges[row - 1].Add(new Edge(column, nextColumn));

                    column = nextColumn;
                    current = next;
                }
            }

            MapNode boss = GetOrCreate(rows, nodes, bossRow, settings.Columns / 2);
            foreach (MapNode node in rows[bossRow - 1])
                node.Connect(boss.Id);

            AssignTypes(rng, settings, rows, bossRow);

            return new RunMap(nodes, rows);
        }

        /// <summary>A drawn connection, kept as lanes so crossings can be spotted.</summary>
        private readonly struct Edge
        {
            public readonly int From;
            public readonly int To;

            public Edge(int from, int to)
            {
                From = from;
                To = to;
            }
        }

        /// <summary>
        /// Picks the next lane: one to the left, straight on, or one to the
        /// right — but never a move that would cross a connection already drawn
        /// between these two floors.
        ///
        /// Two routes swapping lanes is what produces the tangle of X shapes on
        /// screen. Rejecting those moves at generation time is the only place it
        /// can be fixed; no amount of layout work untangles edges that genuinely
        /// cross.
        /// </summary>
        private static int ChooseNextColumn(IRng rng, int column, int columns, List<Edge> drawn)
        {
            Span<int> candidates = stackalloc int[3];
            int count = 0;

            for (int offset = -1; offset <= 1; offset++)
            {
                int candidate = column + offset;
                if (candidate < 0 || candidate >= columns)
                    continue;
                if (Crosses(column, candidate, drawn))
                    continue;

                candidates[count++] = candidate;
            }

            if (count == 0)
                return column;   // boxed in; going straight is the tidiest option

            return candidates[rng.Next(count)];
        }

        /// <summary>
        /// Two connections cross when one starts left of the other and ends up
        /// right of it (or the mirror image).
        /// </summary>
        private static bool Crosses(int from, int to, List<Edge> drawn)
        {
            foreach (Edge edge in drawn)
            {
                if (from < edge.From && to > edge.To)
                    return true;
                if (from > edge.From && to < edge.To)
                    return true;
            }
            return false;
        }

        private static MapNode GetOrCreate(List<List<MapNode>> rows, List<MapNode> nodes, int row, int column)
        {
            foreach (MapNode existing in rows[row])
            {
                if (existing.Column == column)
                    return existing;   // routes that meet share the node
            }

            var node = new MapNode(nodes.Count, row, column, MapNodeType.Combat);
            nodes.Add(node);
            rows[row].Add(node);
            rows[row].Sort((a, b) => a.Column.CompareTo(b.Column));
            return node;
        }

        private static void AssignTypes(IRng rng, MapGenerationSettings settings,
            List<List<MapNode>> rows, int bossRow)
        {
            for (int row = 0; row <= bossRow; row++)
            {
                foreach (MapNode node in rows[row])
                    node.Type = ResolveType(rng, settings, row, bossRow);
            }
        }

        private static MapNodeType ResolveType(IRng rng, MapGenerationSettings settings, int row, int bossRow)
        {
            // Fixed floors first: an opening fight the player cannot avoid, a
            // guaranteed treasure, a rest before the boss, and the boss itself.
            if (row == bossRow)
                return MapNodeType.Boss;
            if (row == 0)
                return MapNodeType.Combat;
            if (settings.TreasureRow >= 0 && row == settings.TreasureRow)
                return MapNodeType.Treasure;
            if (settings.RestBeforeBoss && row == bossRow - 1)
                return MapNodeType.Rest;

            return RollType(rng, settings, row);
        }

        private static MapNodeType RollType(IRng rng, MapGenerationSettings settings, int row)
        {
            bool elitesAllowed = row >= settings.FirstEliteRow;

            int combat = Math.Max(0, settings.CombatWeight);
            int @event = Math.Max(0, settings.EventWeight);
            int elite = elitesAllowed ? Math.Max(0, settings.EliteWeight) : 0;
            int shop = Math.Max(0, settings.ShopWeight);
            int rest = Math.Max(0, settings.RestWeight);

            int total = combat + @event + elite + shop + rest;
            if (total <= 0)
                return MapNodeType.Combat;

            int roll = rng.Next(total);

            if ((roll -= combat) < 0) return MapNodeType.Combat;
            if ((roll -= @event) < 0) return MapNodeType.Event;
            if ((roll -= elite) < 0) return MapNodeType.Elite;
            if ((roll -= shop) < 0) return MapNodeType.Shop;
            return MapNodeType.Rest;
        }

        private static List<List<MapNode>> NewRows(int rowCount)
        {
            var rows = new List<List<MapNode>>(rowCount);
            for (int i = 0; i < rowCount; i++)
                rows.Add(new List<MapNode>());
            return rows;
        }
    }
}
