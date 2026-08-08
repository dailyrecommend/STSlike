using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using STSlike.Data;
using STSlike.Domain.Actors;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// The little panel that floats over one figure: health, block, and — for an
    /// enemy — what it is about to do.
    ///
    /// One prefab serves the player and every opponent. Nothing here knows where
    /// on screen it belongs; <see cref="ActorOverlayBinder"/> places it. As with
    /// the other binders, every slot is optional: leave one empty and that piece
    /// is simply not drawn, so the panel can be built up a bit at a time.
    ///
    /// A row of status effect icons belongs here too, once the rules have any.
    /// Leave space for it in the prefab now and it will be a matter of filling a
    /// container, not of rebuilding the panel.
    /// </summary>
    public sealed class ActorOverlayView : MonoBehaviour
    {
        [Header("Name (shown only under the pointer)")]
        [SerializeField] private TMP_Text nameText;

        [Tooltip("Object holding the name — a plate, a backing, whatever it sits " +
                 "on. Falls back to the text's own object. Hidden unless the " +
                 "pointer is on this figure, so a crowded room is not a wall of " +
                 "labels.")]
        [SerializeField] private GameObject nameRoot;

        [Header("Health")]
        [SerializeField] private Image hpFill;
        [SerializeField] private TMP_Text hpText;

        [Tooltip("How quickly the bar catches up. Keep it in step with the HUD's " +
                 "own setting, or the same blow will drain two bars at two speeds.")]
        [SerializeField, Min(0f)] private float barSmoothing = 20f;

        [Header("Block (hidden at zero)")]
        [SerializeField] private GameObject blockRoot;
        [SerializeField] private TMP_Text blockText;

        [Header("Targeting")]
        [Tooltip("Shown on every enemy while a card is looking for a target: " +
                 "'this one can be hit'. The click itself lands on the model, " +
                 "not here — nothing in this panel is meant to be clickable.")]
        [SerializeField] private GameObject targetableHighlight;

        [Tooltip("Shown on the one the pointer is actually over: 'this is where " +
                 "the card would land'. Make it read louder than the one above.")]
        [SerializeField] private GameObject targetedHighlight;

        [Header("Conditions")]
        [Tooltip("Row the condition icons are spawned into. A Horizontal Layout " +
                 "Group on it is fine — nothing here positions them.")]
        [SerializeField] private RectTransform statusContainer;

        [SerializeField] private StatusIconView statusIconPrefab;

        [Tooltip("Shared asset describing every condition. One reference rather " +
                 "than a table here: a list per prefab drifts as conditions are " +
                 "added, and every panel that shows one would need its own copy.")]
        [SerializeField] private StatusCatalog statusCatalog;

        [Header("Intent (switched off for the player)")]
        [SerializeField] private GameObject intentRoot;
        [SerializeField] private TMP_Text intentText;
        [SerializeField] private Image intentIcon;

        [Tooltip("Optional art per intent. A kind with no entry keeps whatever " +
                 "sprite the prefab already has.")]
        [SerializeField] private ArtEntry<IntentType>[] intentIcons;

        private RectTransform _rect;
        private float _fill = -1f;
        private float _targetFill;
        private readonly List<StatusIconView> _statusIcons = new List<StatusIconView>();

        /// <summary>Cached because the binder moves this every frame.</summary>
        public RectTransform Rect => _rect != null ? _rect : _rect = (RectTransform)transform;

        /// <summary>
        /// Says whether this figure is a candidate for the card being aimed, and
        /// whether it is the one under the pointer.
        /// </summary>
        public void SetTargeting(bool targetable, bool targeted)
        {
            UiSlot.SetActive(targetableHighlight, targetable);
            UiSlot.SetActive(targetedHighlight, targeted);
        }

        private GameObject NameObject => nameText != null ? nameText.gameObject : null;

        /// <summary>
        /// Draws one figure. Health comes in already resolved to what the player
        /// is allowed to see, so the bar over a head and the bar in the corner
        /// tell the same story through a flurry.
        /// </summary>
        public void Bind(ActorHudView actor, int shownHp, int shownBlock, bool named)
        {
            UiSlot.SetText(nameText, actor.Name);
            UiSlot.SetActive(nameRoot != null ? nameRoot : NameObject, named);

            UiSlot.SetText(hpText, $"{shownHp}/{actor.MaxHp}");
            _targetFill = UiSlot.Fraction(shownHp, actor.MaxHp);

            UiSlot.SetActive(blockRoot, shownBlock > 0);
            UiSlot.SetText(blockText, shownBlock.ToString());

            BindStatuses(actor.Statuses);
            BindIntent(actor);
        }

        /// <summary>
        /// Draws the row of conditions. Icons are pooled and switched off rather
        /// than destroyed: conditions come and go every turn, and rebuilding the
        /// row each time would fight any animation on the prefab.
        /// </summary>
        private void BindStatuses(IReadOnlyList<StatusView> statuses)
        {
            if (statusContainer == null || statusIconPrefab == null)
                return;

            while (_statusIcons.Count < statuses.Count)
                _statusIcons.Add(Instantiate(statusIconPrefab, statusContainer));

            for (int i = 0; i < _statusIcons.Count; i++)
            {
                bool inUse = i < statuses.Count;
                UiSlot.SetActive(_statusIcons[i].gameObject, inUse);

                if (!inUse)
                    continue;

                StatusView status = statuses[i];

                // No catalogue, or nothing describing this one yet: the icon
                // keeps its own sprite and the count still shows.
                Sprite icon = statusCatalog != null && statusCatalog.TryGet(status.Id, out StatusArt art)
                    ? art.icon
                    : null;

                _statusIcons[i].Bind(icon, status.Stacks);
            }
        }

        private void BindIntent(ActorHudView actor)
        {
            bool show = actor.IsEnemy && actor.Intent != IntentType.Unknown;
            UiSlot.SetActive(intentRoot, show);

            if (!show)
                return;

            UiSlot.SetText(intentText, actor.IntentAmount > 0
                ? actor.IntentText
                : actor.Intent.ToString());

            ArtTable.Apply(intentIcons, actor.Intent, intentIcon);
        }

        // Eased here rather than in Bind so the bar keeps gliding between the
        // events that change it.
        private void Update()
        {
            _fill = UiSlot.Approach(_fill, _targetFill, barSmoothing);
            UiSlot.SetFill(hpFill, _fill);
        }

        /// <summary>Drops the easing, so a reused panel does not sweep in from
        /// the last figure's health.</summary>
        public void ResetBar()
        {
            _fill = -1f;
        }
    }
}
