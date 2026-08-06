using UnityEngine;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// The character's art, split out from <see cref="STSlike.Data.CharacterData"/>
    /// so the UI depends on a tiny value object instead of the whole asset — and
    /// so it is always registered, even before a character has been authored.
    /// Every sprite may be null; binders skip what they do not get.
    /// </summary>
    public sealed class CharacterPresentation
    {
        public string Id { get; }
        public Sprite Icon { get; }
        public Sprite EnergyIcon { get; }

        /// <summary>The 3D figure spawned in the arena.</summary>
        public GameObject ModelPrefab { get; }

        public CharacterPresentation(string id, Sprite icon, Sprite energyIcon, GameObject modelPrefab)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "default" : id;
            Icon = icon;
            EnergyIcon = energyIcon;
            ModelPrefab = modelPrefab;
        }
    }
}
