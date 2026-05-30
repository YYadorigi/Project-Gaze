using NUnit.Framework;
using ProjectGaze.Gaze;

namespace ProjectGaze.Tests
{
    public sealed class SpatialPageVisualStateResolverTests
    {
        [Test]
        public void NoConfirmedPage_PreviewPageIsPreview_OthersDormant()
        {
            var snapshot = new GazeInteractionSnapshot(GazeInteractionMode.Previewing, "Page_B", null);

            Assert.That(SpatialPageVisualStateResolver.Resolve("Page_B", snapshot), Is.EqualTo(SpatialPageVisualState.Preview));
            Assert.That(SpatialPageVisualStateResolver.Resolve("Page_A", snapshot), Is.EqualTo(SpatialPageVisualState.Dormant));
        }

        [Test]
        public void ConfirmedPage_TakesPriority()
        {
            var snapshot = new GazeInteractionSnapshot(GazeInteractionMode.Confirmed, null, "Page_A");

            Assert.That(SpatialPageVisualStateResolver.Resolve("Page_A", snapshot), Is.EqualTo(SpatialPageVisualState.Confirmed));
            Assert.That(SpatialPageVisualStateResolver.Resolve("Page_B", snapshot), Is.EqualTo(SpatialPageVisualState.Suppressed));
        }

        [Test]
        public void SwitchPreview_PreviewPageRemainsPreview_WhileOtherPagesSuppressed()
        {
            var snapshot = new GazeInteractionSnapshot(GazeInteractionMode.SwitchPreview, "Page_B", "Page_A");

            Assert.That(SpatialPageVisualStateResolver.Resolve("Page_A", snapshot), Is.EqualTo(SpatialPageVisualState.Confirmed));
            Assert.That(SpatialPageVisualStateResolver.Resolve("Page_B", snapshot), Is.EqualTo(SpatialPageVisualState.Preview));
            Assert.That(SpatialPageVisualStateResolver.Resolve("Page_C", snapshot), Is.EqualTo(SpatialPageVisualState.Suppressed));
        }
    }
}
