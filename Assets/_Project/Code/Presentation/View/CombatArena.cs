using System.Collections.Generic;
using UnityEngine;

namespace STSlike.Presentation.View
{
    /// <summary>
    /// Where the figures stand. Put empty objects in the scene, point them the
    /// way the model should face, and drag them in here.
    ///
    /// Keeping the arrangement in the scene rather than in code means the fight
    /// can be staged by eye — camera angle, spacing and facing are exactly what
    /// was set up, and a wider room is more slots rather than a code change.
    /// </summary>
    public sealed class CombatArena : MonoBehaviour
    {
        [SerializeField] private Transform playerSlot;

        [Tooltip("One per enemy. A fight with more enemies than slots reuses the " +
                 "last one, so add enough for the biggest room you plan.")]
        [SerializeField] private List<Transform> enemySlots = new List<Transform>();

        public Transform PlayerSlot => playerSlot != null ? playerSlot : transform;

        public int EnemySlotCount => enemySlots.Count;

        public Transform EnemySlot(int index)
        {
            if (enemySlots.Count == 0)
                return transform;

            int clamped = Mathf.Clamp(index, 0, enemySlots.Count - 1);
            return enemySlots[clamped] != null ? enemySlots[clamped] : transform;
        }
    }
}
