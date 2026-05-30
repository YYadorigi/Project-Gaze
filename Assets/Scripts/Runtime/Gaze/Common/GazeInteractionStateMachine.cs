namespace ProjectGaze.Gaze
{
    public sealed class GazeInteractionStateMachine
    {
        private readonly GazeInteractionSettings settings;
        private float switchCooldownRemaining;
        private GazeInteractionSnapshot snapshot = new(GazeInteractionMode.Idle, null, null);

        public GazeInteractionStateMachine(GazeInteractionSettings settings)
        {
            this.settings = settings;
        }

        public GazeInteractionSnapshot CurrentSnapshot => snapshot;

        public void SeedConfirmedPage(string confirmedPageId)
        {
            switchCooldownRemaining = 0f;
            snapshot = string.IsNullOrEmpty(confirmedPageId)
                ? new GazeInteractionSnapshot(GazeInteractionMode.Idle, null, null)
                : new GazeInteractionSnapshot(GazeInteractionMode.Confirmed, null, confirmedPageId);
        }

        public void Reset()
        {
            switchCooldownRemaining = 0f;
            snapshot = new GazeInteractionSnapshot(GazeInteractionMode.Idle, null, null);
        }

        public GazeInteractionSnapshot Tick(in GazeInteractionInput input)
        {
            switchCooldownRemaining = UnityEngine.Mathf.Max(0f, switchCooldownRemaining - input.DeltaTime);

            switch (snapshot.Mode)
            {
                case GazeInteractionMode.Idle:
                    HandleIdle(input);
                    break;

                case GazeInteractionMode.Previewing:
                    HandlePreviewing(input);
                    break;

                case GazeInteractionMode.Confirmed:
                    HandleConfirmed(input);
                    break;

                case GazeInteractionMode.SwitchPreview:
                    HandleSwitchPreview(input);
                    break;
            }

            return snapshot;
        }

        private void HandleIdle(in GazeInteractionInput input)
        {
            if (string.IsNullOrEmpty(input.StableHitPageId))
            {
                return;
            }

            snapshot = new GazeInteractionSnapshot(
                GazeInteractionMode.Previewing,
                input.StableHitPageId,
                null);
        }

        private void HandlePreviewing(in GazeInteractionInput input)
        {
            if (string.IsNullOrEmpty(input.StableHitPageId))
            {
                snapshot = new GazeInteractionSnapshot(GazeInteractionMode.Idle, null, snapshot.ConfirmedPageId);
                return;
            }

            if (input.StableHitPageId != snapshot.PreviewPageId)
            {
                snapshot = new GazeInteractionSnapshot(
                    GazeInteractionMode.Previewing,
                    input.StableHitPageId,
                    snapshot.ConfirmedPageId);
            }

            if (input.BlinkConfirmed && switchCooldownRemaining <= 0f && !string.IsNullOrEmpty(snapshot.PreviewPageId))
            {
                switchCooldownRemaining = settings.SwitchCooldownSeconds + settings.BlinkConfirmCooldownSeconds;
                snapshot = new GazeInteractionSnapshot(
                    GazeInteractionMode.Confirmed,
                    null,
                    snapshot.PreviewPageId);
            }
        }

        private void HandleConfirmed(in GazeInteractionInput input)
        {
            if (string.IsNullOrEmpty(input.StableHitPageId) || input.StableHitPageId == snapshot.ConfirmedPageId)
            {
                return;
            }

            if (switchCooldownRemaining > 0f)
            {
                return;
            }

            snapshot = new GazeInteractionSnapshot(
                GazeInteractionMode.SwitchPreview,
                input.StableHitPageId,
                snapshot.ConfirmedPageId);
        }

        private void HandleSwitchPreview(in GazeInteractionInput input)
        {
            if (string.IsNullOrEmpty(input.StableHitPageId))
            {
                snapshot = new GazeInteractionSnapshot(
                    GazeInteractionMode.Confirmed,
                    null,
                    snapshot.ConfirmedPageId);
                return;
            }

            if (input.StableHitPageId == snapshot.ConfirmedPageId)
            {
                snapshot = new GazeInteractionSnapshot(
                    GazeInteractionMode.Confirmed,
                    null,
                    snapshot.ConfirmedPageId);
                return;
            }

            if (input.StableHitPageId != snapshot.PreviewPageId)
            {
                snapshot = new GazeInteractionSnapshot(
                    GazeInteractionMode.SwitchPreview,
                    input.StableHitPageId,
                    snapshot.ConfirmedPageId);
            }

            if (input.BlinkConfirmed && switchCooldownRemaining <= 0f && !string.IsNullOrEmpty(snapshot.PreviewPageId))
            {
                switchCooldownRemaining = settings.SwitchCooldownSeconds + settings.BlinkConfirmCooldownSeconds;
                snapshot = new GazeInteractionSnapshot(
                    GazeInteractionMode.Confirmed,
                    null,
                    snapshot.PreviewPageId);
            }
        }
    }
}
