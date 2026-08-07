using UnityEngine;
using STSlike.Presentation.UI;

namespace STSlike.Presentation.View
{
    /// <summary>
    /// The stage's directory of figures: where each one is, and who a given
    /// piece of geometry belongs to.
    ///
    /// Kept apart from whatever spawns the models so the UI never has to know
    /// how they got there, and asked fresh each time rather than cached: models
    /// belong to one fight, and a reference held across an encounter would be
    /// pointing at something destroyed.
    /// </summary>
    public interface IActorStage
    {
        /// <summary>
        /// Where this figure's floating UI should sit. Null when it has no model
        /// on stage right now.
        /// </summary>
        Transform AnchorFor(ActorRef actor);

        /// <summary>
        /// Which figure a transform belongs to — a collider hit by a click, say.
        /// Walks up from the transform, so any part of a model identifies it.
        /// </summary>
        bool TryIdentify(Transform hit, out ActorRef actor);
    }
}
