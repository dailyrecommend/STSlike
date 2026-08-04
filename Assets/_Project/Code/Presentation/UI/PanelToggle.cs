using UnityEngine;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Shows and hides a panel without ever switching off the object that is
    /// driving it.
    ///
    /// Deactivating the panel a binder lives on is a trap: the binder goes with
    /// it, and a button wired to that binder's Open method then does nothing,
    /// because Unity does not invoke handlers on inactive objects. So when the
    /// panel *is* the binder's own object it is hidden with a CanvasGroup —
    /// invisible and click-through, but still running.
    /// </summary>
    internal sealed class PanelToggle
    {
        private readonly GameObject _target;
        private readonly bool _useCanvasGroup;
        private CanvasGroup _group;

        public PanelToggle(GameObject root, GameObject self)
        {
            _target = root != null ? root : self;
            _useCanvasGroup = _target == self;

            if (!_useCanvasGroup)
                return;

            if (!_target.TryGetComponent(out _group))
                _group = _target.AddComponent<CanvasGroup>();
        }

        public bool IsVisible { get; private set; } = true;

        public void Set(bool visible)
        {
            IsVisible = visible;

            if (_useCanvasGroup)
            {
                _group.alpha = visible ? 1f : 0f;
                _group.blocksRaycasts = visible;
                _group.interactable = visible;
                return;
            }

            if (_target.activeSelf != visible)
                _target.SetActive(visible);
        }
    }
}
