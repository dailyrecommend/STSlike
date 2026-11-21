using System.Collections.Generic;
using UnityEngine;
using VContainer;
using STSlike.Data;
using STSlike.Domain.Content;
using STSlike.Domain.Relics;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// The row of relics the player is carrying.
    ///
    /// Nothing here lays them out — put a Horizontal Layout Group on the
    /// container and the row arranges itself. This only decides how many icons
    /// exist and which sprite each one wears.
    ///
    /// The presenter hands over ids; the sprite is looked up in the catalogue,
    /// which is why a relic that arrives from a mod needs no change here.
    /// </summary>
    public sealed class RelicRowBinder : MonoBehaviour
    {
        [Tooltip("Object the icons are spawned into. Empty means no row is " +
                 "drawn, which is a fine state while the HUD is being built.")]
        [SerializeField] private RectTransform container;

        [SerializeField] private RelicIconView iconPrefab;

        [Tooltip("Hidden while nothing is carried, so an empty row does not " +
                 "leave a frame sitting on the HUD. Falls back to the container.")]
        [SerializeField] private GameObject root;

        private CombatPresenter _presenter;
        private IContentRegistry _content;

        private readonly List<RelicIconView> _icons = new List<RelicIconView>();

        [Inject]
        public void Construct(CombatPresenter presenter, IContentRegistry content)
        {
            _presenter = presenter;
            _content = content;
            _presenter.Changed += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
                _presenter.Changed -= Refresh;
        }

        /// <summary>
        /// Rebuilds the row. Icons are pooled and switched off rather than
        /// destroyed — relics are picked up rarely, but the HUD redraws
        /// constantly, and destroying a row every redraw would fight any
        /// animation on the prefab.
        /// </summary>
        private void Refresh()
        {
            if (_presenter == null || container == null || iconPrefab == null)
                return;

            IReadOnlyList<string> relics = _presenter.Relics;
            UiSlot.SetActive(root != null ? root : container.gameObject, relics.Count > 0);

            while (_icons.Count < relics.Count)
                _icons.Add(Instantiate(iconPrefab, container));

            for (int i = 0; i < _icons.Count; i++)
            {
                bool inUse = i < relics.Count;
                UiSlot.SetActive(_icons[i].gameObject, inUse);

                if (inUse)
                    _icons[i].Bind(IconOf(relics[i]));
            }
        }

        // The domain knows the id; the asset knows what it looks like. Cast
        // rather than an interface member, because a Sprite has no business
        // being in the rules.
        private Sprite IconOf(string id)
        {
            IRelicDefinition definition = _content?.Relic(id);
            return definition is RelicData data ? data.Icon : null;
        }
    }
}
