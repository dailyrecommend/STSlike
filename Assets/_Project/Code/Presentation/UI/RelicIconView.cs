using UnityEngine;
using UnityEngine.UI;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// One relic in the row along the top: its icon, and nothing else.
    ///
    /// Put this on a small prefab and drag the image in. The slot is optional
    /// like everywhere else — an entry with no art still takes its place in the
    /// row, so a relic that has not been drawn yet is visible as a gap rather
    /// than missing entirely.
    /// </summary>
    public sealed class RelicIconView : MonoBehaviour
    {
        [SerializeField] private Image icon;

        public void Bind(Sprite sprite)
        {
            // Assigned even when null: these are pooled, so keeping the old
            // sprite would show the relic that used to be in this slot.
            UiSlot.SetSpriteOrClear(icon, sprite);
        }
    }
}
