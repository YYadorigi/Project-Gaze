namespace ProjectGaze.Gaze.Providers
{
    public interface IBlinkDetectionProvider
    {
        string ProviderName { get; }

        bool IsAvailable { get; }

        bool ConsumeBlinkConfirmation();
    }
}
