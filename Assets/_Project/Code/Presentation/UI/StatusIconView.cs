using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// One condition in the row above a figure: an icon and how many turns are
    /// left on it.
    ///
    /// Put this on a small prefab and drag its two pieces in. Both slots are
    /// optional, as everywhere else — an icon with no number still reads, and a
    /// number with no icon is still better than nothing while art is being made.
    /// </summary>
    public sealed class StatusIconView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text stacksText;

        public void Bind(Sprite sprite, int stacks)
        {
            // Kept rather than cleared when a condition has no art yet: these
            // are pooled, so blanking would show the previous condition's gap
            // rather than a placeholder.
            UiSlot.SetSprite(icon, sprite);
            UiSlot.SetText(stacksText, stacks.ToString());
        }
    }
}
