namespace ProjectGaze.Gaze
{
    public static class SpatialPageVisualStateResolver
    {
        public static SpatialPageVisualState Resolve(string pageId, in GazeInteractionSnapshot snapshot)
        {
            if (!string.IsNullOrEmpty(snapshot.ConfirmedPageId))
            {
                if (pageId == snapshot.ConfirmedPageId)
                {
                    return SpatialPageVisualState.Confirmed;
                }

                if (!string.IsNullOrEmpty(snapshot.PreviewPageId) && pageId == snapshot.PreviewPageId)
                {
                    return SpatialPageVisualState.Preview;
                }

                return SpatialPageVisualState.Suppressed;
            }

            if (!string.IsNullOrEmpty(snapshot.PreviewPageId) && pageId == snapshot.PreviewPageId)
            {
                return SpatialPageVisualState.Preview;
            }

            return SpatialPageVisualState.Dormant;
        }
    }
}
