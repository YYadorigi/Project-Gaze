using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectGaze.Gaze.Providers
{
    public sealed class MouseClickConfirmProvider : MonoBehaviour, IBlinkDetectionProvider
    {
        public string ProviderName => "Mouse Click";

        public bool IsAvailable => Mouse.current != null;

        public bool ConsumeBlinkConfirmation()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }
    }
}
