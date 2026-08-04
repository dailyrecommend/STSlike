using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STSlike.Domain.Map;
using STSlike.Domain.Randomness;

namespace STSlike.Tests.EditMode
{
    public class MapGeneratorTests
    {
        private static MapGenerationSettings Settings(int rows = 12, int columns = 6, int paths = 5) =>
            new MapGenerationSettings { RowCount = rows, Columns = columns, PathCount = paths };

        private static RunMap Generate(int seed = 1, MapGenerationSettings settings = null) =>
            new MapGenerator().Generate(new SystemRng(seed), settings ?? Settings());

        [Test]
        public void Map_HasTheRequestedNumberOfFloors()
        {
            RunMap map = Generate(settings: Settings(rows: 10));

            Assert.AreEqual(10, map.RowCount);
            Assert.IsNotEmpty(map.StartNodes);
        }

        [Test]
        public void TopFloor_IsASingleBoss()
        {
            RunMap map = Generate();

            IReadOnlyList<MapNode> top = map.GetRow(map.RowCount - 1);
            Assert.AreEqual(1, top.Count);
            Assert.AreEqual(MapNodeType.Boss, top[0].Type);
            Assert.AreSame(top[0], map.Boss);
        }

        [Test]
        public void BottomFloor_IsAlwaysCombat()
        {
            RunMap map = Generate();

            foreach (MapNode node in map.StartNodes)
                Assert.AreEqual(MapNodeType.Combat, node.Type);
        }

        // The two properties a generated map must never violate.
        [Test]
        public void EveryNode_IsReachableFromTheStart()
        {
            RunMap map = Generate(seed: 99);

            var visited = new HashSet<int>();
            var queue = new Queue<MapNode>(map.StartNodes);
            foreach (MapNode start in map.StartNodes)
                visited.Add(start.Id);

            while (queue.Count > 0)
            {
                MapNode node = queue.Dequeue();
                foreach (MapNode next in map.NextOf(node))
                {
                    if (visited.Add(next.Id))
                        queue.Enqueue(next);
                }
            }

            Assert.AreEqual(map.Nodes.Count, visited.Count,
                "Some nodes cannot be reached by walking up from the bottom floor.");
        }

        [Test]
        public void NoNode_IsADeadEnd()
        {
            RunMap map = Generate(seed: 7);

            foreach (MapNode node in map.Nodes)
            {
                if (node.Row == map.RowCount - 1)
                    continue;   // the boss is meant to be the end

                Assert.IsNotEmpty(node.Next, $"Node {node.Id} on row {node.Row} leads nowhere.");
            }
        }

        [Test]
        public void Connections_OnlyGoUpOneFloor()
        {
            RunMap map = Generate(seed: 12);

            foreach (MapNode node in map.Nodes)
            {
                foreach (MapNode next in map.NextOf(node))
                    Assert.AreEqual(node.Row + 1, next.Row, "A connection skipped or went down a floor.");
            }
        }

        // Neighbouring lanes only: this is what keeps the map tidy on screen.
        [Test]
        public void Connections_StayWithinOneLane()
        {
            RunMap map = Generate(seed: 3, settings: Settings(rows: 14, columns: 6, paths: 6));

            foreach (MapNode node in map.Nodes)
            {
                if (node.Row == map.RowCount - 2)
                    continue;   // the funnel into the boss may span lanes

                foreach (MapNode next in map.NextOf(node))
                    Assert.LessOrEqual(Math.Abs(next.Column - node.Column), 1);
            }
        }

        // Crossed routes are the difference between a readable map and a tangle
        // of X shapes; no layout work can untangle edges that genuinely cross.
        [Test]
        public void Routes_NeverCrossEachOther()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                RunMap map = Generate(seed, Settings(rows: 14, columns: 6, paths: 7));

                for (int row = 0; row < map.RowCount - 2; row++)
                {
                    var edges = new List<(int from, int to)>();
                    foreach (MapNode node in map.GetRow(row))
                    {
                        foreach (MapNode next in map.NextOf(node))
                            edges.Add((node.Column, next.Column));
                    }

                    for (int i = 0; i < edges.Count; i++)
                    {
                        for (int j = i + 1; j < edges.Count; j++)
                        {
                            bool crosses =
                                (edges[i].from < edges[j].from && edges[i].to > edges[j].to) ||
                                (edges[i].from > edges[j].from && edges[i].to < edges[j].to);

                            Assert.IsFalse(crosses,
                                $"seed {seed}, row {row}: {edges[i]} crosses {edges[j]}");
                        }
                    }
                }
            }
        }

        [Test]
        public void Columns_StayInsideTheGrid()
        {
            RunMap map = Generate(settings: Settings(columns: 4));

            foreach (MapNode node in map.Nodes)
            {
                Assert.GreaterOrEqual(node.Column, 0);
                Assert.Less(node.Column, 4);
            }
        }

        [Test]
        public void OneNodePerLanePerFloor()
        {
            RunMap map = Generate(seed: 21);

            for (int row = 0; row < map.RowCount; row++)
            {
                var columns = map.GetRow(row).Select(n => n.Column).ToList();
                CollectionAssert.AllItemsAreUnique(columns);
            }
        }

        [Test]
        public void Elites_DoNotAppearOnTheOpeningFloors()
        {
            var settings = Settings(rows: 14, paths: 8);
            settings.FirstEliteRow = 5;
            settings.EliteWeight = 200;   // force them wherever they are allowed

            RunMap map = Generate(seed: 5, settings: settings);

            foreach (MapNode node in map.Nodes)
            {
                if (node.Type == MapNodeType.Elite)
                    Assert.GreaterOrEqual(node.Row, 5);
            }
        }

        [Test]
        public void FixedFloors_AreHonoured()
        {
            var settings = Settings(rows: 12);
            settings.TreasureRow = 5;
            settings.RestBeforeBoss = true;

            RunMap map = Generate(seed: 2, settings: settings);

            foreach (MapNode node in map.GetRow(5))
                Assert.AreEqual(MapNodeType.Treasure, node.Type);

            foreach (MapNode node in map.GetRow(map.RowCount - 2))
                Assert.AreEqual(MapNodeType.Rest, node.Type);
        }

        // Same seed, same map — the run's seed already reproduces shuffles and
        // rewards, and the map has to follow the same rule.
        [Test]
        public void SameSeed_ProducesTheSameMap()
        {
            RunMap first = Generate(seed: 4242);
            RunMap second = Generate(seed: 4242);

            Assert.AreEqual(first.Nodes.Count, second.Nodes.Count);

            for (int i = 0; i < first.Nodes.Count; i++)
            {
                MapNode a = first.Nodes[i];
                MapNode b = second.Nodes[i];

                Assert.AreEqual(a.Row, b.Row);
                Assert.AreEqual(a.Column, b.Column);
                Assert.AreEqual(a.Type, b.Type);
                CollectionAssert.AreEqual(a.Next, b.Next);
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentMaps()
        {
            RunMap first = Generate(seed: 1);
            RunMap second = Generate(seed: 2);

            bool identical = first.Nodes.Count == second.Nodes.Count;
            if (identical)
            {
                for (int i = 0; i < first.Nodes.Count; i++)
                {
                    if (first.Nodes[i].Column != second.Nodes[i].Column ||
                        first.Nodes[i].Type != second.Nodes[i].Type)
                    {
                        identical = false;
                        break;
                    }
                }
            }

            Assert.IsFalse(identical, "Two different seeds produced the same map.");
        }

        [Test]
        public void Settings_RejectImpossibleMaps()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Generate(settings: new MapGenerationSettings { RowCount = 1 }));
        }
    }
}
