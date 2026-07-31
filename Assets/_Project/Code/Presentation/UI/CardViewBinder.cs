using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Put this on your card prefab and drag its pieces into the slots. It only
    /// fills in data and reports clicks — the look, size and animation are
    /// entirely yours.
    /// </summary>
    public sealed class CardViewBinder : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Image artwork;
        [SerializeField] private Button button;

        [Header("Unaffordable")]
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField, Range(0f, 1f)] private float unaffordableAlpha = 0.45f;

        private Action _onClick;

        public CardView View { get; private set; }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        /// <summary>Called by the hand spawner every time the hand changes.</summary>
        public void Bind(CardView view, Sprite art, Action onClick)
        {
            View = view;
            _onClick = onClick;

            if (nameText != null)
                nameText.text = view.Id;
            if (costText != null)
                costText.text = view.Cost.ToString();

            if (artwork != null && art != null)
                artwork.sprite = art;

            if (button != null)
                button.interactable = view.IsPlayable;
            if (fadeGroup != null)
                fadeGroup.alpha = view.IsPlayable ? 1f : unaffordableAlpha;
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}
