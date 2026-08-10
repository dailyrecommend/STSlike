using System.Collections.Generic;
using UnityEngine;
using VContainer;
using STSlike.Presentation.View;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Throws a number off a figure each time a blow becomes visible.
    ///
    /// Driven from the moment the damage is *released* rather than from the
    /// moment the rules settled it. The rules settle a six-round burst in one
    /// go; releasing is what spreads it over the animation, so hanging the
    /// numbers there is what makes them arrive one at a time, in step with the
    /// health bar and the flinch. Nothing here has to know about pacing.
    /// </summary>
    public sealed class DamageNumberBinder : MonoBehaviour
    {
        [Tooltip("Parent for the numbers. A full-stretch child of the canvas, " +
                 "the same shape as the actor overlay's container.")]
        [SerializeField] private RectTransform container;

        [SerializeField] private DamageNumberView numberPrefab;

        [Tooltip("Camera the fight is seen through. Falls back to the main camera.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Where the number starts relative to the figure's anchor.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 20f);

        [Tooltip("Numbers kept alive at once. Past this the oldest is reused, " +
                 "which only shows during something already excessive.")]
        [SerializeField, Min(1)] private int poolSize = 16;

        private CombatDisplayState _display;
        private IActorStage _stage;

        private readonly List<DamageNumberView> _pool = new List<DamageNumberView>();
        private int _next;

        [Inject]
        public void Construct(CombatDisplayState display, IActorStage stage)
        {
            _display = display;
            _stage = stage;
            _display.Revealed += OnRevealed;
        }

        private void OnDestroy()
        {
            if (_display != null)
                _display.Revealed -= OnRevealed;
        }

        private void OnRevealed(ActorRef actor, int hpLost, int blockLost)
        {
            // Nothing moved at all — a heal or a re-sync rather than a blow.
            if (hpLost <= 0 && blockLost <= 0)
                return;

            if (container == null || numberPrefab == null)
                return;

            Transform anchor = _stage?.AnchorFor(actor);
            if (anchor == null)
                return;

            Camera scene = worldCamera != null ? worldCamera : Camera.main;
            if (!ScreenAnchor.TryProject(container, scene, anchor.position, out Vector2 local))
                return;

            Take().Show(hpLost, local + offset);
        }

        /// <summary>
        /// Next free number, growing the pool until it is full and then reusing
        /// in order. Reuse cuts a number short rather than dropping it, which is
        /// the better failure when the screen is already crowded.
        /// </summary>
        private DamageNumberView Take()
        {
            foreach (DamageNumberView number in _pool)
            {
                if (!number.gameObject.activeSelf)
                    return number;
            }

            if (_pool.Count < poolSize)
            {
                DamageNumberView made = Instantiate(numberPrefab, container);
                _pool.Add(made);
                return made;
            }

            _next = (_next + 1) % _pool.Count;
            return _pool[_next];
        }
    }
}
