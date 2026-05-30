namespace ProjectGaze.Gaze.Providers
{
    public interface IGazeTrackingProvider
    {
        string ProviderName { get; }

        bool IsAvailable { get; }

        bool TryGetSample(out GazeTrackingSample sample);
    }
}
