using UnityEngine;
using VContainer;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// The revolver's cylinder: six chambers that empty as they are fired and
    /// fill as they are reloaded, turning one chamber's worth each time.
    ///
    /// Which chamber is loaded is worked out from the ammo count rather than
    /// tracked as it changes, so the widget cannot drift out of step with the
    /// rules — a fight beginning with a full cylinder or a card that reloads
    /// three rounds at once both land correctly without a special case.
    ///
    /// Hidden entirely for a character who carries no such weapon, so one HUD
    /// serves everyone.
    /// </summary>
    public sealed class CylinderBinder : MonoBehaviour
    {
        [Tooltip("The whole widget. Switched off for a character with no weapon. " +
                 "Leave empty to use this object.")]
        [SerializeField] private GameObject root;

        [Tooltip("The part that turns — the cylinder body with the chambers on " +
                 "it. Everything that should rotate belongs under here.")]
        [SerializeField] private RectTransform cylinder;

        [Tooltip("One round per chamber, in order around the circle. Switched " +
                 "off when that chamber is spent, so put the round here and the " +
                 "empty hole in the body behind it.")]
        [SerializeField] private GameObject[] rounds;

        [Header("Motion")]
        [Tooltip("How quickly the cylinder settles after turning. 0 snaps.")]
        [SerializeField, Min(0f)] private float spinSmoothing = 12f;

        [Tooltip("Which way it turns as rounds are fired. Reloading turns it back.")]
        [SerializeField] private bool clockwiseWhenFiring = true;

        private CombatPresenter _presenter;
        private float _angle;
        private float _targetAngle;

        // The first draw of a fight should not spin in from wherever the last
        // one left off.
        private bool _placed;

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

        private void Refresh()
        {
            if (_presenter == null)
                return;

            PlayerView player = _presenter.Player;

            UiSlot.SetActive(root != null ? root : gameObject, player.HasWeapon);
            if (!player.HasWeapon)
                return;

            // Chambers are spent from the first one round, so the one under the
            // hammer moves along as the cylinder turns — the same order a real
            // one goes in.
            int spent = Mathf.Max(0, player.MaxAmmo - player.Ammo);

            if (rounds != null)
            {
                for (int i = 0; i < rounds.Length; i++)
                    UiSlot.SetActive(rounds[i], i >= spent);
            }

            // Not wrapped to a single turn on purpose: emptying the cylinder and
            // reloading it winds a full circle back, which is the flourish the
            // reload deserves.
            _targetAngle = spent * StepDegrees * (clockwiseWhenFiring ? -1f : 1f);

            if (!_placed)
            {
                _placed = true;
                _angle = _targetAngle;
                Apply();
            }
        }

        /// <summary>
        /// Worked out from the chambers actually placed, so a five- or
        /// eight-chamber weapon needs no second number kept in step by hand.
        /// </summary>
        private float StepDegrees =>
            rounds != null && rounds.Length > 0 ? 360f / rounds.Length : 60f;

        private void Update()
        {
            if (Mathf.Approximately(_angle, _targetAngle))
                return;

            _angle = spinSmoothing <= 0f
                ? _targetAngle
                : Mathf.Lerp(_angle, _targetAngle,
                    1f - Mathf.Exp(-spinSmoothing * Time.unscaledDeltaTime));

            Apply();
        }

        private void Apply()
        {
            if (cylinder != null)
                cylinder.localRotation = Quaternion.Euler(0f, 0f, _angle);
        }
    }
}
