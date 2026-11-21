using System.Collections.Generic;
using UnityEngine;
using STSlike.Domain.Actors;
using STSlike.Domain.Content;

namespace STSlike.Data
{
    /// <summary>
    /// Authoring asset for one room: which enemies stand in it and where in the
    /// act it belongs.
    ///
    /// Enemies are dragged in as assets because that is a pleasant way to build
    /// a room, but what leaves this object is a list of ids — so the rules never
    /// hold a reference to an asset, and a room described from outside the build
    /// looks identical to one authored here.
    ///
    /// Adding a room is data: create one of these, drop enemies in, rescan.
    /// </summary>
    [CreateAssetMenu(fileName = "Encounter_New", menuName = "STSlike/Encounter", order = 3)]
    public sealed class EncounterData : ScriptableObject, IEncounterDefinition
    {
        [Header("Identity")]
        [SerializeField] private string id = "encounter";

        [Tooltip("Which kind of node this room can fill. A Boss room is only " +
                 "ever drawn for the boss.")]
        [SerializeField] private EnemyTier tier = EnemyTier.Normal;

        [Header("The Room")]
        [Tooltip("Who stands here, left to right. More of them is a different " +
                 "fight from one bigger enemy, which is the point of authoring " +
                 "rooms rather than picking an enemy at random.")]
        [SerializeField] private List<EnemyData> enemies = new List<EnemyData>();

        [Header("Where It Belongs")]
        [Tooltip("First floor this room may appear on, counting from 1.")]
        [SerializeField, Min(1)] private int minFloor = 1;

        [Tooltip("Last floor it may appear on. 0 means it never stops being " +
                 "appropriate.")]
        [SerializeField, Min(0)] private int maxFloor;

        public string Id => id;
        public EnemyTier Tier => tier;
        public int MinFloor => minFloor;
        public int MaxFloor => maxFloor;

        public IReadOnlyList<string> EnemyIds => BuildIds();

        /// <summary>Falls back to the id so an unnamed room still reads in a list.</summary>
        public override string ToString() => id;

        private List<string> BuildIds()
        {
            var ids = new List<string>(enemies.Count);
            foreach (EnemyData enemy in enemies)
            {
                if (enemy != null)
                    ids.Add(enemy.Id);
            }
            return ids;
        }
    }
}
