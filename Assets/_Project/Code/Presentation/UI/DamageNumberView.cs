using TMPro;
using UnityEngine;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// One number rising off a figure.
    ///
    /// Put this on a small prefab with a text on it. How it moves and fades is
    /// yours — set the curve and the colours here, or drive the whole thing from
    /// an Animator on the prefab and leave the motion fields at zero.
    /// </summary>
    public sealed class DamageNumberView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [Tooltip("Optional. Shown instead of the text when a blow was stopped " +
                 "outright — a shield burst, a spark. Leave empty to just print " +
                 "the word.")]
        [SerializeField] private GameObject blockedMark;

        [Header("Colour")]
        [SerializeField] private Color hurtColor = Color.white;
        [SerializeField] private Color blockedColor = new Color(0.65f, 0.75f, 1f);

        [Header("Motion")]
        [Tooltip("How far it drifts up over its life, in canvas units.")]
        [SerializeField] private float rise = 60f;

        [Tooltip("Sideways spread, so two numbers in the same spot do not stack " +
                 "exactly on top of each other.")]
        [SerializeField] private float scatter = 24f;

        [Tooltip("Seconds before it disappears. 0 leaves it to an Animator.")]
        [SerializeField, Min(0f)] private float lifetime = 0.8f;

        [SerializeField] private AnimationCurve fade =
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _from;
        private Vector2 _drift;
        private float _age;

        public RectTransform Rect => _rect != null ? _rect : _rect = (RectTransform)transform;

        /// <summary>
        /// Shows a blow. A cost of zero means a guard ate the whole thing, which
        /// is worth saying rather than printing "0".
        /// </summary>
        public void Show(int hpLost, Vector2 at)
        {
            bool blocked = hpLost <= 0;

            if (label != null)
            {
                label.text = blocked ? "BLOCKED" : hpLost.ToString();
                label.color = blocked ? blockedColor : hurtColor;
            }

            if (blockedMark != null && blockedMark.activeSelf != blocked)
                blockedMark.SetActive(blocked);

            _from = at;
            _drift = new Vector2(Random.Range(-scatter, scatter), rise);
            _age = 0f;

            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
            Rect.anchoredPosition = at;

            if (_group == null && !TryGetComponent(out _group))
                _group = gameObject.AddComponent<CanvasGroup>();

            _group.alpha = 1f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (lifetime <= 0f)
                return;

            _age += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(_age / lifetime);
            Rect.anchoredPosition = _from + _drift * t;

            if (_group != null)
                _group.alpha = fade.Evaluate(t);

            if (t >= 1f)
                gameObject.SetActive(false);
        }
    }
}
