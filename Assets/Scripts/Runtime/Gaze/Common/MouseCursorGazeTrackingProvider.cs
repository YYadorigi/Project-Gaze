using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectGaze.Gaze.Providers
{
    public sealed class MouseCursorGazeTrackingProvider : MonoBehaviour, IGazeTrackingProvider
    {
        private Camera targetCamera;

        public string ProviderName => "Mouse Cursor";

        public bool IsAvailable => Mouse.current != null;

        public void ConfigureTargetCamera(Camera targetCamera)
        {
            this.targetCamera = targetCamera;
        }

        public bool TryGetSample(out GazeTrackingSample sample)
        {
            sample = default;

            if (Mouse.current == null)
            {
                return false;
            }

            Vector2 screenPoint = Mouse.current.position.ReadValue();
            Vector3 viewportPoint3 = targetCamera != null
                ? targetCamera.ScreenToViewportPoint(screenPoint)
                : new Vector3(
                    Screen.width > 0 ? screenPoint.x / Screen.width : 0.5f,
                    Screen.height > 0 ? screenPoint.y / Screen.height : 0.5f,
                    0f);
            Vector2 viewportPoint = GazeViewportPointUtility.ClampViewportPoint(new Vector2(viewportPoint3.x, viewportPoint3.y));

            sample = new GazeTrackingSample(
                true,
                viewportPoint,
                DateTime.UtcNow.Ticks,
                leftEyeValid: true,
                rightEyeValid: true);
            return true;
        }
    }
}
