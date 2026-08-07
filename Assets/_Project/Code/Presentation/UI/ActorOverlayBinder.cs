using System.Collections.Generic;
using UnityEngine;
using VContainer;
using STSlike.Presentation.View;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Floats a panel over every figure on stage.
    ///
    /// The panels live on a screen-space canvas rather than on a canvas per
    /// model: text stays crisp at any distance, they all batch together, and
    /// they are always drawn over the 3D rather than fighting it for sort order.
    /// The world anchor is projected to a screen point each frame instead.
    ///
    /// Which figures exist comes from the presenter, where they are, from the
    /// stage. Asking both every frame means neither has to be set up first.
    /// </summary>
    public sealed class ActorOverlayBinder : MonoBehaviour
    {
        [Tooltip("Parent for the panels. Should be a full-stretch child of the " +
                 "canvas — the panels are positioned relative to its centre.")]
        [SerializeField] private RectTransform container;

        [SerializeField] private ActorOverlayView panelPrefab;

        [Tooltip("Camera the fight is seen through. Falls back to the main camera.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Nudge from the anchor, in canvas units. Lift the panel clear " +
                 "of the model here rather than moving the anchor itself.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 40f);

        private CombatPresenter _presenter;
        private CombatDisplayState _display;
        private IActorAnchors _anchors;

        private readonly List<ActorOverlayView> _panels = new List<ActorOverlayView>();
        private Canvas _canvas;

        // Values only change on an event, but positions change every frame, so
        // the two are refreshed at different rates.
        private bool _dirty = true;

        [Inject]
        public void Construct(CombatPresenter presenter, CombatDisplayState display,
            IActorAnchors anchors)
        {
            _presenter = presenter;
            _display = display;
            _anchors = anchors;

            _presenter.Changed += MarkDirty;
            _display.Changed += MarkDirty;
        }

        private void OnDestroy()
        {
            if (_presenter != null)
                _presenter.Changed -= MarkDirty;
            if (_display != null)
                _display.Changed -= MarkDirty;
        }

        private void MarkDirty() => _dirty = true;

        // Late, so the panels follow models that moved during this frame rather
        // than trailing them by one.
        private void LateUpdate()
        {
            if (_presenter == null || panelPrefab == null || container == null)
                return;

            IReadOnlyList<ActorHudView> actors = _presenter.Actors;
            Fit(actors.Count);

            for (int i = 0; i < actors.Count; i++)
            {
                ActorHudView actor = actors[i];
                ActorOverlayView panel = _panels[i];

                CombatDisplayState.Vitals shown = _display.Of(actor.Actor, actor.Hp, actor.Block);
                Transform anchor = _anchors?.AnchorFor(actor.Actor);

                // A figure with no model yet, one that has been struck down, or
                // a run that has left combat entirely: nothing to float over.
                bool visible = anchor != null && shown.Hp > 0 && _presenter.IsInCombat
                               && Place(panel, anchor);

                bool wasVisible = panel.gameObject.activeSelf;
                if (wasVisible != visible)
                    panel.gameObject.SetActive(visible);

                if (!visible)
                    continue;

                // A panel that has just appeared has never been drawn, whatever
                // the dirty flag says — and it is probably above a different
                // figure than last time, so its bar starts fresh rather than
                // draining from whoever held that slot in the last fight.
                if (!wasVisible)
                    panel.ResetBar();

                if (_dirty || !wasVisible)
                    panel.Bind(actor, shown.Hp, shown.Block);
            }

            _dirty = false;
        }

        /// <summary>
        /// Puts the panel over the anchor. False when the anchor is behind the
        /// camera, where a projection would otherwise place it — mirrored — on
        /// the wrong side of the screen.
        /// </summary>
        private bool Place(ActorOverlayView panel, Transform anchor)
        {
            Camera scene = worldCamera != null ? worldCamera : Camera.main;
            if (scene == null)
                return false;

            Vector3 screen = scene.WorldToScreenPoint(anchor.position);
            if (screen.z <= 0f)
                return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    container, screen, UiCamera, out Vector2 local))
                return false;

            panel.Rect.anchoredPosition = local + offset;
            return true;
        }

        // An overlay canvas is drawn without a camera; any other mode needs the
        // one the canvas was given, or the projection lands somewhere else.
        private Camera UiCamera
        {
            get
            {
                if (_canvas == null)
                    _canvas = container.GetComponentInParent<Canvas>();

                return _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? _canvas.worldCamera
                    : null;
            }
        }

        /// <summary>Grows the pool to cover the room; spares are kept switched off.</summary>
        private void Fit(int count)
        {
            while (_panels.Count < count)
            {
                ActorOverlayView panel = Instantiate(panelPrefab, container);

                // Centre the anchors so the position set each frame is measured
                // from the middle of the container, whatever the prefab was
                // authored with. Getting this wrong offsets every panel by half
                // a screen, which is a miserable thing to debug in the inspector.
                RectTransform rect = panel.Rect;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);

                panel.gameObject.SetActive(false);
                _panels.Add(panel);
            }

            for (int i = count; i < _panels.Count; i++)
            {
                if (_panels[i].gameObject.activeSelf)
                    _panels[i].gameObject.SetActive(false);
            }
        }
    }
}
