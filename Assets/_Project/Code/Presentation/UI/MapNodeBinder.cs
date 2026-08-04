using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using STSlike.Domain.Map;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// One stop on the map. Put this on your node prefab and drag its pieces in;
    /// it fills the icon, reflects whether the node can be travelled to, and
    /// reports clicks. Every slot is optional.
    /// </summary>
    public sealed class MapNodeBinder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>Artwork per node type, so a shop reads differently from an elite.</summary>
        [Serializable]
        private struct TypeIcon
        {
            public MapNodeType type;
            public Sprite sprite;
            public Color tint;
        }

        [Header("Content")]
        [SerializeField] private Image icon;
        [SerializeField] private Button button;
        [SerializeField] private TypeIcon[] typeIcons;

        [Header("State")]
        [Tooltip("Shown on the node the player is standing on.")]
        [SerializeField] private GameObject currentMarker;

        [Tooltip("Highlights the nodes that can be picked right now.")]
        [SerializeField] private GameObject reachableMarker;

        [Tooltip("Fades nodes that are done with, or cannot be reached from here.")]
        [SerializeField] private CanvasGroup fadeGroup;

        [Tooltip("Nodes that cannot be travelled to from where the player stands.")]
        [SerializeField, Range(0f, 1f)] private float unreachableAlpha = 0.4f;

        [Header("Cleared")]
        [Tooltip("Nodes already travelled through on this run.")]
        [SerializeField, Range(0f, 1f)] private float clearedAlpha = 0.5f;
        [SerializeField] private Color clearedTint = Color.black;

        [Header("Hover")]
        [Tooltip("How much the node grows under the pointer. 1 = no growth.")]
        [SerializeField] private float hoverScale = 1.15f;

        [Tooltip("How quickly it grows and shrinks back. 0 = snap.")]
        [SerializeField] private float hoverSmoothing = 14f;

        [Tooltip("Only react on nodes that can actually be travelled to, so the " +
                 "growth reads as 'you can pick this'.")]
        [SerializeField] private bool hoverOnlyWhenReachable = true;

        private Action _onClick;
        private Color _defaultIconColor = Color.white;
        private Vector3 _baseScale = Vector3.one;
        private bool _hovered;

        public MapNodeView View { get; private set; }

        private void Awake()
        {
            if (icon != null)
                _defaultIconColor = icon.color;

            _baseScale = transform.localScale;

            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

        public void OnPointerExit(PointerEventData eventData) => _hovered = false;

        private void OnDisable()
        {
            // Pooled instances are switched off while the pointer may still be
            // over them, so no exit event ever arrives.
            _hovered = false;
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            bool grow = _hovered && (!hoverOnlyWhenReachable || View.IsReachable);
            Vector3 target = grow ? _baseScale * hoverScale : _baseScale;

            // Scaling from the centre keeps the pointer inside the node, so
            // unlike a lift this cannot bounce between hovered and not.
            transform.localScale = hoverSmoothing <= 0f
                ? target
                : Vector3.Lerp(transform.localScale, target,
                    1f - Mathf.Exp(-hoverSmoothing * Time.unscaledDeltaTime));
        }

        public void Bind(MapNodeView view, Action onClick)
        {
            View = view;
            _onClick = onClick;

            // A node the player has left behind, as opposed to the one they are
            // standing on right now.
            bool cleared = view.IsVisited && !view.IsCurrent;

            Color baseColor = ApplyIcon(view.Type);
            if (icon != null)
                icon.color = cleared ? clearedTint : baseColor;

            if (button != null)
                button.interactable = view.IsReachable;

            if (fadeGroup != null)
                fadeGroup.alpha = ResolveAlpha(view, cleared);

            ToggleMarker(currentMarker, view.IsCurrent);
            ToggleMarker(reachableMarker, view.IsReachable);
        }

        private float ResolveAlpha(MapNodeView view, bool cleared)
        {
            if (cleared)
                return clearedAlpha;
            if (view.IsCurrent || view.IsReachable)
                return 1f;
            return unreachableAlpha;   // not on the route from here
        }

        /// <summary>Applies the type's art and returns the colour it should use.</summary>
        private Color ApplyIcon(MapNodeType type)
        {
            if (icon == null || typeIcons == null)
                return _defaultIconColor;

            foreach (TypeIcon entry in typeIcons)
            {
                if (entry.type != type)
                    continue;

                // Only override what was actually configured. Blanking the image
                // for a type with no art yet would hide the node entirely, which
                // looks like the map failed to build.
                if (entry.sprite != null)
                    icon.sprite = entry.sprite;

                return entry.tint.a > 0f ? entry.tint : _defaultIconColor;
            }

            return _defaultIconColor;
        }

        /// <summary>
        /// Turns a small decoration on or off — never the node itself.
        ///
        /// These slots are meant for a highlight ring or a "you are here" pip.
        /// Assigning the node's own object (or a parent) by mistake would switch
        /// the whole node off for every state it is not in, so the map would
        /// simply lose most of its nodes. State is carried by colour and alpha
        /// instead, which cannot make anything disappear.
        /// </summary>
        private void ToggleMarker(GameObject marker, bool active)
        {
            if (marker == null)
                return;

            // IsChildOf is true for the transform itself, so this covers both
            // "the node" and "a parent of the node".
            if (transform.IsChildOf(marker.transform))
            {
                Debug.LogWarning(
                    $"[MapNodeBinder] '{marker.name}' is the node itself or one of its parents; " +
                    "marker slots take a child decoration. Ignoring it.", this);
                return;
            }

            if (marker.activeSelf != active)
                marker.SetActive(active);
        }
    }
}
