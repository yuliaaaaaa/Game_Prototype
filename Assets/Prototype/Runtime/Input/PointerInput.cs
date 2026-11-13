using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ClearPath
{
    public interface IChargeInput
    {
        bool Pressed { get; }
        bool Released { get; }
        bool Held { get; }
        bool IsOverUi { get; }
    }
    public sealed class PointerInput : IChargeInput
    {
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        ButtonControl Button => Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed
            ? Touchscreen.current.primaryTouch.press : (ButtonControl)Mouse.current?.leftButton;
        public bool Pressed => (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false) || (Mouse.current?.leftButton.wasPressedThisFrame ?? false);
        public bool Released => (Touchscreen.current?.primaryTouch.press.wasReleasedThisFrame ?? false) || (Mouse.current?.leftButton.wasReleasedThisFrame ?? false);
        public bool Held => Button?.isPressed ?? false;
        public bool IsOverUi
        {
            get
            {
                var events = EventSystem.current;
                if (!events) return false;
                var touch = Touchscreen.current?.primaryTouch;
                Vector2 position = touch != null && touch.press.wasPressedThisFrame
                    ? touch.position.ReadValue() : Mouse.current?.position.ReadValue() ?? Vector2.zero;
                events.RaycastAll(new PointerEventData(events) { position = position }, uiHits);
                bool overUi = uiHits.Exists(hit => hit.module is UnityEngine.UI.GraphicRaycaster);
                uiHits.Clear();
                return overUi;
            }
        }
    }
}
