using NUnit.Framework;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Depth;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectGaze.Tests
{
    public sealed class DepthGatedAgentPanelCoordinatorTests
    {
        [Test]
        public void TickConfirmedPage_ShowsPanelForAgentLogoAndHidesForOrdinaryPage()
        {
            var coordinator = new DepthGatedAgentPanelCoordinator();

            var showTransition = coordinator.TickConfirmedPage(DepthGatedAgentPanelCoordinator.AgentLogoPageId);
            var repeatTransition = coordinator.TickConfirmedPage(DepthGatedAgentPanelCoordinator.AgentLogoPageId);
            var hideTransition = coordinator.TickConfirmedPage("Web_A");

            Assert.That(showTransition, Is.EqualTo(DepthGatedAgentPanelTransition.Show));
            Assert.That(repeatTransition, Is.EqualTo(DepthGatedAgentPanelTransition.None));
            Assert.That(hideTransition, Is.EqualTo(DepthGatedAgentPanelTransition.Hide));
            Assert.That(coordinator.IsAgentPanelActive, Is.False);
        }

        [Test]
        public void AgentPageMorphPresenter_SwitchesOneSpatialPageBetweenLogoAndPanelPoses()
        {
            var pageObject = new GameObject("Agent_Logo");
            var logoObject = new GameObject("LogoGroup");
            var panelObject = new GameObject("PanelGroup");

            try
            {
                logoObject.transform.SetParent(pageObject.transform, false);
                panelObject.transform.SetParent(pageObject.transform, false);
                var page = pageObject.AddComponent<SpatialPage>();
                page.Initialize(DepthGatedAgentPanelCoordinator.AgentLogoPageId, null, null);
                var logoGroup = logoObject.AddComponent<CanvasGroup>();
                var panelGroup = panelObject.AddComponent<CanvasGroup>();
                var zeroLayer = GazeDepthLayerProfile.GetLayer(3);
                var logoSpec = DepthGatedAgentLayoutProvider.GetAgentLogoSpec();
                var logoPose = new DepthGatedAgentMorphPose(
                    logoSpec.LocalPosition,
                    Vector3.one * logoSpec.SlotScale,
                    logoSpec.MatchingRadius,
                    logoSpec.DepthLayerId,
                    logoSpec.DepthLayerRayDistance,
                    logoSpec.DepthLayerTolerance,
                    1f,
                    0f);
                var panelPose = new DepthGatedAgentMorphPose(
                    DepthGatedAgentLayoutProvider.GetAgentPanelActivePosition(),
                    Vector3.one * DepthGatedAgentLayoutProvider.GetAgentPanelActiveScale(),
                    DepthGatedAgentLayoutProvider.GetAgentPanelActiveMatchingRadius(),
                    zeroLayer.LayerId,
                    zeroLayer.RayDistance,
                    zeroLayer.Tolerance,
                    0f,
                    1f);
                var presenter = new DepthGatedAgentPageMorphPresenter(
                    null,
                    page,
                    logoGroup,
                    panelGroup,
                    logoPose,
                    panelPose,
                    0.01f);

                presenter.HideImmediate();

                Assert.That(page.transform.localPosition, Is.EqualTo(logoPose.LocalPosition));
                Assert.That(page.transform.localScale, Is.EqualTo(logoPose.LocalScale));
                Assert.That(page.MatchingRegionRadiusWorld, Is.EqualTo(logoPose.MatchingRadius).Within(0.0001f));
                Assert.That(page.DepthLayerId, Is.EqualTo(GazeDepthLayerProfile.Far3LayerId));
                Assert.That(logoGroup.alpha, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(panelGroup.alpha, Is.EqualTo(0f).Within(0.0001f));

                presenter.ShowImmediate();

                Assert.That(presenter.IsPanelVisible, Is.True);
                Assert.That(page.transform.localPosition, Is.EqualTo(panelPose.LocalPosition));
                Assert.That(page.transform.localScale, Is.EqualTo(panelPose.LocalScale));
                Assert.That(page.MatchingRegionRadiusWorld, Is.EqualTo(panelPose.MatchingRadius).Within(0.0001f));
                Assert.That(page.DepthLayerId, Is.EqualTo(zeroLayer.LayerId));
                Assert.That(logoGroup.alpha, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(panelGroup.alpha, Is.EqualTo(1f).Within(0.0001f));

                presenter.HideImmediate();

                Assert.That(presenter.IsPanelVisible, Is.False);
                Assert.That(page.transform.localPosition, Is.EqualTo(logoPose.LocalPosition));
                Assert.That(page.DepthLayerId, Is.EqualTo(GazeDepthLayerProfile.Far3LayerId));
                Assert.That(logoGroup.alpha, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(panelGroup.alpha, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(pageObject);
            }
        }

        [Test]
        public void AgentRoundTripState_BuildsCompletedTaskRecordAndClearsPendingState()
        {
            var state = new DepthGatedAgentRoundTripState();

            state.Begin(null, "2026-05-29T00:00:00.0000000Z");
            bool completed = state.TryBuildCompletedRecord(
                "Web_A",
                null,
                out var record,
                "2026-05-29T00:00:02.0000000Z");

            Assert.That(completed, Is.True);
            Assert.That(state.HasPending, Is.True);
            Assert.That(record.TaskType, Is.EqualTo(GazeTaskInteractionTypes.AgentPanelRoundTrip));
            Assert.That(record.StartedAtUtc, Is.EqualTo("2026-05-29T00:00:00.0000000Z"));
            Assert.That(record.CompletedAtUtc, Is.EqualTo("2026-05-29T00:00:02.0000000Z"));
            Assert.That(record.LogoPageId, Is.EqualTo(DepthGatedAgentPanelCoordinator.AgentLogoPageId));
            Assert.That(record.ClosedByPageId, Is.EqualTo("Web_A"));
            Assert.That(record.HasOpenPredictedDepth, Is.False);

            state.Clear();

            Assert.That(state.HasPending, Is.False);
        }
    }
}
