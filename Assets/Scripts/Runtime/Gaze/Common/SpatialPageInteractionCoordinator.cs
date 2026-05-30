using System;

namespace ProjectGaze.Gaze
{
    internal sealed class SpatialPageInteractionCoordinator : IDisposable
    {
        private readonly GazeInteractionController gazeController;
        private string lastObservedConfirmedPageId;
        private string pendingConfirmedPageId;

        public SpatialPageInteractionCoordinator(
            GazeInteractionController gazeController,
            string initialConfirmedPageId)
        {
            this.gazeController = gazeController;
            lastObservedConfirmedPageId = initialConfirmedPageId;

            if (this.gazeController != null)
            {
                this.gazeController.ConfirmedTargetChanged += HandleConfirmedTargetChanged;
            }
        }

        public void Tick(Action<string> onConfirmedPageChanged)
        {
            if (gazeController == null)
            {
                return;
            }

            string confirmedPageId = pendingConfirmedPageId;
            pendingConfirmedPageId = null;
            if (string.IsNullOrEmpty(confirmedPageId) ||
                string.Equals(confirmedPageId, lastObservedConfirmedPageId, StringComparison.Ordinal))
            {
                return;
            }

            lastObservedConfirmedPageId = confirmedPageId;
            onConfirmedPageChanged?.Invoke(confirmedPageId);
        }

        public void Dispose()
        {
            if (gazeController != null)
            {
                gazeController.ConfirmedTargetChanged -= HandleConfirmedTargetChanged;
            }
        }

        private void HandleConfirmedTargetChanged(string confirmedTargetId)
        {
            pendingConfirmedPageId = confirmedTargetId;
        }
    }
}
