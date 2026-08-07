using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Draws the act map: a node per stop, a line per route.
    ///
    /// Positions come from the node's row and column, which is why the domain
    /// puts nodes on a grid in the first place — the layout is arithmetic rather
    /// than something the generator has to solve. A small deterministic wobble
    /// is added so the result reads as a hand-drawn path instead of a table, and
    /// it stays put between redraws because it is derived from the node id.
    /// </summary>
    public sealed class MapViewBinder : MonoBehaviour
    {
        /// <summary>How a floor's nodes are placed across the width of the map.</summary>
        public enum RowAlignment
        {
            Lanes = 0,
            Centered = 1,
            Spread = 2,
        }

        [Header("Panel")]
        [Tooltip("Shown while the player is choosing a route. Leave empty to use this GameObject.")]
        [SerializeField] private GameObject root;

        [Header("Prefabs")]
        [SerializeField] private MapNodeBinder nodePrefab;
        [SerializeField] private RectTransform nodeContainer;

        [Tooltip("A thin Image used as a connecting line. Leave empty to skip lines.")]
        [SerializeField] private RectTransform linePrefab;
        [SerializeField] private RectTransform lineContainer;

        [Header("Visibility")]
        [Tooltip("Open the map automatically whenever the player is choosing a route. " +
                 "Turn this off to use it purely as a panel opened from a button.")]
        [SerializeField] private bool autoShowWhileChoosing = true;

        [Header("Layout")]
        [Tooltip("How each floor is arranged across the width.\n\n" +
                 "Lanes: keep the exact lane, so the same lane lines up vertically.\n" +
                 "Centered: keep lane gaps but centre each floor — tidier when a " +
                 "floor only uses part of the width.\n" +
                 "Spread: even gaps, centred. Neatest, but lanes no longer line up.")]
        [SerializeField] private RowAlignment rowAlignment = RowAlignment.Centered;

        [SerializeField] private Vector2 spacing = new Vector2(140f, 150f);

        [Tooltip("Random-looking offset per node. Starts at 0 for a clean grid; " +
                 "raise it slightly once the map reads well to soften the rows.")]
        [SerializeField] private float jitter;
        [SerializeField] private float lineThickness = 6f;

        [Tooltip("How far a route stops short of the nodes at each end. Set it " +
                 "to a little more than the node's radius so the icon sits in a " +
                 "clear gap instead of having lines run into it.")]
        [SerializeField, Min(0f)] private float nodeGap = 26f;

        [Tooltip("Grow the container to fit the whole map, so it can be scrolled.")]
        [SerializeField] private bool resizeContainer = true;

        [Tooltip("Room left below the first floor and above the last, so the end " +
                 "nodes are not flush against the edge and can be scrolled to.")]
        [SerializeField] private float edgePadding = 120f;

        [Header("Scrolling")]
        [Tooltip("The scroll view holding the map. Assigned so it can start at the bottom.")]
        [SerializeField] private ScrollRect mapScroll;

        [Tooltip("Open the map at the first floor and let the player scroll upward.")]
        [SerializeField] private bool startAtBottom = true;

        private readonly List<MapNodeBinder> _nodes = new List<MapNodeBinder>();
        private readonly List<RectTransform> _lines = new List<RectTransform>();
        private readonly Dictionary<int, Vector2> _positions = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, List<int>> _rowLanes = new Dictionary<int, List<int>>();

        private CombatPresenter _presenter;
        private PanelToggle _panel;
        private bool _manuallyOpen;

        // Created on demand rather than in Awake: VContainer injects from the
        // LifetimeScope's own Awake, which can run before this component's.
        private PanelToggle Panel => _panel ??= new PanelToggle(root, gameObject);

        [Inject]
        public void Construct(CombatPresenter presenter)
        {
            _presenter = presenter;
            _presenter.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
                _presenter.Changed -= Refresh;
        }

        // ---- hook these to buttons when opening the map by hand ----

        /// <summary>Opens the map as a panel (for the map button in the top bar).</summary>
        public void Show()
        {
            _manuallyOpen = true;
            Refresh();
        }

        public void Hide()
        {
            _manuallyOpen = false;
            Refresh();
        }

        public void Toggle()
        {
            _manuallyOpen = !_manuallyOpen;
            Refresh();
        }

        private void Refresh()
        {
            if (_presenter == null || nodePrefab == null)
                return;

            bool visible = _manuallyOpen || (autoShowWhileChoosing && _presenter.IsChoosingNode);
            bool justOpened = visible && !Panel.IsVisible;

            Panel.Set(visible);

            if (!visible)
                return;

            IReadOnlyList<MapNodeView> nodes = _presenter.MapNodes;
            LayOutPositions(nodes);
            DrawLines(nodes);
            DrawNodes(nodes);

            if (justOpened && startAtBottom)
                ScrollToBottom();
        }

        /// <summary>
        /// Starts the map at the first floor. The container was only resized a
        /// moment ago, so the layout is forced first — otherwise the scroll
        /// position would be computed from the previous, shorter map.
        /// </summary>
        private void ScrollToBottom()
        {
            if (mapScroll == null)
                return;

            Canvas.ForceUpdateCanvases();

            if (mapScroll is WheelOnlyScrollRect wheel)
                wheel.ScrollToEnd();
            else
                mapScroll.verticalNormalizedPosition = 0f;
        }

        private void LayOutPositions(IReadOnlyList<MapNodeView> nodes)
        {
            _positions.Clear();

            int columns = 1;
            int rows = 1;
            foreach (MapNodeView node in nodes)
            {
                columns = Mathf.Max(columns, node.Column + 1);
                rows = Mathf.Max(rows, node.Row + 1);
            }

            // Per-floor bookkeeping, so a floor can be balanced against its own
            // contents rather than against the full width of the grid.
            _rowLanes.Clear();
            foreach (MapNodeView node in nodes)
            {
                if (!_rowLanes.TryGetValue(node.Row, out List<int> lanes))
                {
                    lanes = new List<int>();
                    _rowLanes[node.Row] = lanes;
                }
                lanes.Add(node.Column);
            }
            foreach (List<int> lanes in _rowLanes.Values)
                lanes.Sort();

            float gridCenter = (columns - 1) * 0.5f;

            foreach (MapNodeView node in nodes)
            {
                // The first floor sits one padding above the bottom edge, so the
                // map grows upward from there and the lowest node is never
                // clipped against the container edge.
                var position = new Vector2(
                    ResolveX(node, gridCenter),
                    edgePadding + node.Row * spacing.y);

                _positions[node.Id] = position + Wobble(node.Id);
            }

            if (resizeContainer && nodeContainer != null)
            {
                // Padding at both ends, so the top floor can actually be
                // scrolled into view instead of sitting on the boundary.
                float height = (rows - 1) * spacing.y + edgePadding * 2f;
                nodeContainer.sizeDelta = new Vector2(nodeContainer.sizeDelta.x, height);
            }
        }

        /// <summary>
        /// Horizontal placement for a node.
        ///
        /// The generator works in absolute lanes, and a floor that only uses
        /// lanes 3 and 4 would sit hard against the right of the map while the
        /// floor above spans everything — which is what makes the column look
        /// ragged. Centring (or spreading) each floor against its own contents
        /// straightens that out, and since the nodes stay in lane order it never
        /// introduces a crossing.
        /// </summary>
        private float ResolveX(MapNodeView node, float gridCenter)
        {
            switch (rowAlignment)
            {
                case RowAlignment.Lanes:
                    return (node.Column - gridCenter) * spacing.x;

                case RowAlignment.Centered:
                {
                    List<int> lanes = _rowLanes[node.Row];
                    float rowCenter = (lanes[0] + lanes[lanes.Count - 1]) * 0.5f;
                    return (node.Column - rowCenter) * spacing.x;
                }

                default:
                {
                    List<int> lanes = _rowLanes[node.Row];
                    int index = lanes.IndexOf(node.Column);
                    float center = (lanes.Count - 1) * 0.5f;
                    return (index - center) * spacing.x;
                }
            }
        }

        /// <summary>
        /// A fixed offset per node. Derived from the id rather than random, so
        /// the map does not twitch every time it is redrawn.
        /// </summary>
        private Vector2 Wobble(int id)
        {
            if (jitter <= 0f)
                return Vector2.zero;

            float x = Mathf.Sin(id * 12.9898f) * 43758.5453f;
            float y = Mathf.Sin(id * 78.233f) * 12345.6789f;

            return new Vector2(
                (x - Mathf.Floor(x)) * 2f - 1f,
                (y - Mathf.Floor(y)) * 2f - 1f) * jitter;
        }

        private void DrawNodes(IReadOnlyList<MapNodeView> nodes)
        {
            Transform parent = nodeContainer != null ? nodeContainer : transform;

            while (_nodes.Count < nodes.Count)
                _nodes.Add(Instantiate(nodePrefab, parent));

            for (int i = 0; i < _nodes.Count; i++)
            {
                MapNodeBinder binder = _nodes[i];
                bool inUse = i < nodes.Count;
                binder.gameObject.SetActive(inUse);

                if (!inUse)
                    continue;

                MapNodeView view = nodes[i];
                int id = view.Id;

                var rect = (RectTransform)binder.transform;
                AnchorToBottomCenter(rect);
                rect.anchoredPosition = _positions[id];
                binder.Bind(view, () => _presenter.ChooseNode(id));

                // Nodes are pushed to the end of the sibling list so they always
                // draw over the lines — which matters when both share a
                // container, as the connections would otherwise cut across the
                // node art.
                binder.transform.SetAsLastSibling();
            }
        }

        private void DrawLines(IReadOnlyList<MapNodeView> nodes)
        {
            if (linePrefab == null)
                return;

            Transform parent = lineContainer != null ? lineContainer : nodeContainer;
            if (parent == null)
                parent = transform;

            int used = 0;
            foreach (MapNodeView node in nodes)
            {
                foreach (int nextId in node.Next)
                {
                    if (!_positions.TryGetValue(nextId, out Vector2 to))
                        continue;

                    RectTransform line = GetLine(used++, parent);
                    Stretch(line, _positions[node.Id], to);
                }
            }

            for (int i = used; i < _lines.Count; i++)
                _lines[i].gameObject.SetActive(false);
        }

        private RectTransform GetLine(int index, Transform parent)
        {
            while (_lines.Count <= index)
                _lines.Add(Instantiate(linePrefab, parent));

            RectTransform line = _lines[index];
            if (!line.gameObject.activeSelf)
                line.gameObject.SetActive(true);
            return line;
        }

        /// <summary>
        /// Pins a child so its position is measured from the bottom-centre of the
        /// container, which is what the row/column maths assumes. Without this
        /// the prefab's own anchors decide what the coordinates mean, and a
        /// centre-anchored prefab pushes the whole map half a screen upward.
        /// Doing it here also means the container's own pivot does not matter.
        /// </summary>
        private static void AnchorToBottomCenter(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
        }

        /// <summary>
        /// Positions a thin rect so it runs between two nodes, stopping short of
        /// both. Routes drawn centre to centre disappear under the icons and
        /// collide where several meet, so a route reads as ending at a node
        /// rather than skewering it.
        /// </summary>
        private void Stretch(RectTransform line, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;

            // Both ends pull in, so a very short route shrinks away rather than
            // turning inside out and drawing backwards.
            float trim = Mathf.Min(nodeGap, length * 0.45f);
            Vector2 step = length > 0f ? delta / length : Vector2.zero;

            Vector2 start = from + step * trim;
            Vector2 end = to - step * trim;
            Vector2 span = end - start;

            AnchorToBottomCenter(line);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = start + span * 0.5f;
            line.sizeDelta = new Vector2(span.magnitude, lineThickness);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }
}
