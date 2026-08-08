using UnityEngine;
using VContainer;
using STSlike.Presentation.UI;

namespace STSlike.Presentation.View
{
    /// <summary>
    /// Tracks which figure the pointer is on, and aims a picked-up card at it.
    ///
    /// Hovering is followed all the time so names can appear on the figure under
    /// the pointer; only the click is gated on a card actually waiting for a
    /// target, so nothing on stage competes with the UI for clicks otherwise.
    /// Models need colliders for this to reach them — anywhere on the rig will
    /// do, since the figure is identified by walking up from whatever was hit.
    /// </summary>
    public sealed class ActorTargetingInput : MonoBehaviour
    {
        [Tooltip("Camera the fight is seen through. Falls back to the main camera.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Which layers the pick can hit. Narrow this to the layer the " +
                 "models are on if scenery starts swallowing clicks.")]
        [SerializeField] private LayerMask hitLayers = ~0;

        [SerializeField, Min(1f)] private float maxDistance = 500f;

        private CombatPresenter _presenter;
        private CardSelection _selection;
        private ActorHoverChannel _hover;
        private IActorStage _stage;

        [Inject]
        public void Construct(CombatPresenter presenter, CardSelection selection,
            ActorHoverChannel hover, IActorStage stage)
        {
            _presenter = presenter;
            _selection = selection;
            _hover = hover;
            _stage = stage;
        }

        private void Update()
        {
            // Pointing is tracked all the time, not only while a card waits for
            // a target: a figure's name appears whenever the pointer finds it.
            // The hand, the tooltip, any panel — a pointer there belongs to the
            // UI, and nothing on stage is under it.
            ActorRef hit = GameInput.OverUi ? ActorRef.None : Pick();
            _hover.Set(hit);

            // Nothing to point at for a card that acts on the player: the hand
            // commits that one on a click away from the UI. And never on the
            // click that picked the card up, which is still pressed this frame.
            if (_selection == null || !_selection.IsAiming || !_selection.NeedsTarget
                || _selection.JustSelected)
                return;

            // Only enemies: the cards that need aiming are the ones that hit
            // somebody else.
            if (!hit.IsValid || hit.IsPlayer || !GameInput.ClickedPrimary)
                return;

            // Read the aim before clearing it — Clear would otherwise take the
            // hand index with it before the card could be played.
            int handIndex = _selection.HandIndex;
            _selection.Clear();
            _presenter.PlayCard(handIndex, hit.EnemyIndex);
        }

        private void OnDisable()
        {
            _hover?.Clear();
        }

        /// <summary>The figure under the pointer, or none.</summary>
        private ActorRef Pick()
        {
            Camera scene = worldCamera != null ? worldCamera : Camera.main;
            if (scene == null || _stage == null)
                return ActorRef.None;

            Ray ray = scene.ScreenPointToRay(GameInput.Position());
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayers))
                return ActorRef.None;

            return _stage.TryIdentify(hit.transform, out ActorRef actor)
                ? actor
                : ActorRef.None;
        }

    }
}
