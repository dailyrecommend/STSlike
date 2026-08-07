using UnityEngine;
using STSlike.Presentation.UI;

namespace STSlike.Presentation.View
{
    /// <summary>
    /// Where a figure's floating UI should sit.
    ///
    /// Kept apart from whatever spawns the models so the overlay never has to
    /// know how they got there, and asked again every frame rather than cached:
    /// models belong to one fight, and a reference held across an encounter
    /// would be pointing at something destroyed.
    /// </summary>
    public interface IActorAnchors
    {
        /// <summary>Null when that figure has no model on stage right now.</summary>
        Transform AnchorFor(ActorRef actor);
    }
}
