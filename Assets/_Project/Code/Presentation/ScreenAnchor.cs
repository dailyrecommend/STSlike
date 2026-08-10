using UnityEngine;

namespace STSlike.Presentation
{
    /// <summary>
    /// Puts a point in the world onto a screen-space canvas.
    ///
    /// Anything pinned to a figure — the panel over its head, a number rising
    /// off it — needs the same projection, and the same two things wrong with
    /// it: an overlay canvas is drawn without a camera, and a point behind the
    /// camera projects to a mirrored position on the wrong side of the screen.
    /// Both belong in one place.
    /// </summary>
    internal static class ScreenAnchor
    {
        /// <summary>
        /// False when there is nothing sensible to draw — no camera, or the
        /// point is behind it.
        /// </summary>
        public static bool TryProject(RectTransform container, Camera scene, Vector3 world,
            out Vector2 local)
        {
            local = default;

            if (container == null || scene == null)
                return false;

            Vector3 screen = scene.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                return false;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, screen, UiCameraFor(container), out local);
        }

        /// <summary>
        /// An overlay canvas is drawn without a camera; any other mode needs the
        /// one it was given, or the projection lands somewhere else entirely.
        /// </summary>
        public static Camera UiCameraFor(RectTransform container)
        {
            Canvas canvas = container != null ? container.GetComponentInParent<Canvas>() : null;

            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }
    }
}
