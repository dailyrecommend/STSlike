using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace STSlike.Presentation
{
    /// <summary>
    /// The one place that knows which input backend the project is built with.
    ///
    /// Unity ships two, and either or both can be enabled, so every read needs
    /// the same three-way dance: try the new system, fall back to the old one,
    /// and answer harmlessly when neither is compiled in. Written out at each
    /// call site that dance gets copied per binder, and adding a gamepad later
    /// means finding all of them.
    ///
    /// Only the reads the game actually performs are exposed. A general
    /// key-by-name lookup would need a mapping table between the two backends'
    /// key enums, which is a lot of surface for keys nobody presses.
    /// </summary>
    internal static class GameInput
    {
        /// <summary>
        /// Where the pointer is, in screen space. The fallback is returned only
        /// when no backend is compiled in — useful for callers that already have
        /// a last-known position from an event.
        /// </summary>
        public static Vector2 Position(Vector2 fallback = default)
        {
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null)
                return Pointer.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return fallback;
#endif
        }

        /// <summary>Left button, pressed this frame.</summary>
        public static bool ClickedPrimary
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                    return Mouse.current.leftButton.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(0);
#else
                return false;
#endif
            }
        }

        /// <summary>Right button, pressed this frame.</summary>
        public static bool ClickedSecondary
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                    return Mouse.current.rightButton.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(1);
#else
                return false;
#endif
            }
        }

        public static bool EscapePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                    return Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Escape);
#else
                return false;
#endif
            }
        }

        /// <summary>F10 — the development skip.</summary>
        public static bool DebugSkipPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                    return Keyboard.current.f10Key.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.F10);
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// True when the pointer is over any UI. Not a backend question but a
        /// pointer one, and asked wherever the 3D scene competes with the
        /// interface for a click.
        /// </summary>
        public static bool OverUi
        {
            get
            {
                EventSystem events = EventSystem.current;
                return events != null && events.IsPointerOverGameObject();
            }
        }
    }
}
