using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using STSlike.Presentation.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace STSlike.Presentation.View
{
    /// <summary>
    /// Aims a picked-up card by pointing at a model.
    ///
    /// Only active while a card is waiting for a target, so the models are inert
    /// the rest of the time and nothing on stage competes with the UI for
    /// clicks. Enemies need colliders for this to reach them — anywhere on the
    /// rig will do, since the figure is identified by walking up from whatever
    /// was hit.
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
        private IActorStage _stage;

        [Inject]
        public void Construct(CombatPresenter presenter, CardSelection selection,
            IActorStage stage)
        {
            _presenter = presenter;
            _selection = selection;
            _stage = stage;
        }

        private void Update()
        {
            // Nothing to point at for a card that acts on the player: the hand
            // commits that one on a click away from the UI. And never on the
            // click that picked the card up, which is still pressed this frame.
            if (_selection == null || !_selection.IsAiming || !_selection.NeedsTarget
                || _selection.JustSelected)
                return;

            // The hand, the tooltip, any panel: a click there belongs to the UI.
            // Without this, clicking the aimed card to put it back would also
            // throw it at whatever model happens to be behind it.
            if (OverUi())
            {
                _selection.Hover(ActorRef.None);
                return;
            }

            ActorRef hit = Pick();
            _selection.Hover(hit);

            // Only enemies: the cards that need aiming are the ones that hit
            // somebody else.
            if (!hit.IsValid || hit.IsPlayer || !ClickPressed())
                return;

            // Read the aim before clearing it — Clear would otherwise take the
            // hand index with it before the card could be played.
            int handIndex = _selection.HandIndex;
            _selection.Clear();
            _presenter.PlayCard(handIndex, hit.EnemyIndex);
        }

        private void OnDisable()
        {
            _selection?.Hover(ActorRef.None);
        }

        /// <summary>The figure under the pointer, or none.</summary>
        private ActorRef Pick()
        {
            Camera scene = worldCamera != null ? worldCamera : Camera.main;
            if (scene == null || _stage == null)
                return ActorRef.None;

            Ray ray = scene.ScreenPointToRay(PointerPosition());
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayers))
                return ActorRef.None;

            return _stage.TryIdentify(hit.transform, out ActorRef actor)
                ? actor
                : ActorRef.None;
        }

        private static bool OverUi()
        {
            EventSystem events = EventSystem.current;
            return events != null && events.IsPointerOverGameObject();
        }

        private static Vector2 PointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null)
                return Pointer.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        private static bool ClickPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.leftButton.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }
    }
}
