using System.Linq;
using NUnit.Framework;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Depth;

namespace ProjectGaze.Tests
{
    public sealed class LayeredPagesLayoutTests
    {
        [Test]
        public void GetPageSpecs_KeepsExpandedPagesAndPageAAsInitialMainPage()
        {
            var specs = LayeredPagesLayoutProvider.GetPageSpecs();

            Assert.That(specs.Count, Is.EqualTo(15));
            Assert.That(specs[0].Name, Is.EqualTo(LayeredPagesSceneDefaults.InitialMainPageId));
            Assert.That(specs.Select(spec => spec.Name), Does.Contain("Page_O"));
        }

        [Test]
        public void GetPageSpecs_AssignsSevenSymmetricDepthLayers()
        {
            var specs = LayeredPagesLayoutProvider.GetPageSpecs();

            var layerIds = specs.Select(spec => spec.DepthLayerId).Distinct().ToArray();

            Assert.That(layerIds, Is.EquivalentTo(new[]
            {
                GazeDepthLayerProfile.Near3LayerId,
                GazeDepthLayerProfile.Near2LayerId,
                GazeDepthLayerProfile.Near1LayerId,
                GazeDepthLayerProfile.ZeroLayerId,
                GazeDepthLayerProfile.Far1LayerId,
                GazeDepthLayerProfile.Far2LayerId,
                GazeDepthLayerProfile.Far3LayerId
            }));
            Assert.That(specs.Single(spec => spec.Name == "Page_B").DepthLayerRayDistance, Is.EqualTo(GazeDepthLayerProfile.Near3RayDistance).Within(0.0001f));
            Assert.That(specs.Single(spec => spec.Name == "Page_A").DepthLayerRayDistance, Is.EqualTo(GazeDepthLayerProfile.ZeroRayDistance).Within(0.0001f));
            Assert.That(specs.Single(spec => spec.Name == "Page_O").DepthLayerRayDistance, Is.EqualTo(GazeDepthLayerProfile.Far3RayDistance).Within(0.0001f));
        }

        [Test]
        public void BuildDepthTintProfile_UsesExpandedDepthRange()
        {
            var specs = LayeredPagesLayoutProvider.GetPageSpecs();

            var profile = LayeredPagesLayoutProvider.BuildDepthTintProfile(specs);

            Assert.That(profile.ZeroPlaneLocalZ, Is.EqualTo(GazeDepthLayerProfile.ZeroRayDistance).Within(0.0001f));
            Assert.That(profile.NearDepthRange, Is.EqualTo(9.0f).Within(0.0001f));
            Assert.That(profile.FarDepthRange, Is.EqualTo(9.0f).Within(0.0001f));
        }

        [Test]
        public void DepthGatedAgentLayoutProvider_MapsWebPagesToLayeredPageSlots()
        {
            var agentSpecs = DepthGatedAgentLayoutProvider.GetWebPageSpecs();
            var layeredPageSpecs = LayeredPagesLayoutProvider.GetPageSpecs();

            var webA = agentSpecs.Single(spec => spec.PageId == "Web_A");
            var pageE = layeredPageSpecs.Single(spec => spec.Name == "Page_E");
            var webB = agentSpecs.Single(spec => spec.PageId == "Web_B");
            var pageD = layeredPageSpecs.Single(spec => spec.Name == "Page_D");

            Assert.That(webA.LocalPosition, Is.EqualTo(pageE.Position));
            Assert.That(webA.SlotScale, Is.EqualTo(pageE.SlotScale).Within(0.0001f));
            Assert.That(webA.DepthLayerId, Is.EqualTo(GazeDepthLayerProfile.Near2LayerId));
            Assert.That(webA.MatchingRadius, Is.EqualTo(pageE.MatchingRegionRadiusWorld).Within(0.0001f));
            Assert.That(webB.LocalPosition, Is.EqualTo(pageD.Position));
            Assert.That(webB.DepthLayerId, Is.EqualTo(GazeDepthLayerProfile.Near2LayerId));
        }

        [Test]
        public void DepthGatedAgentLayoutProvider_KeepsOrdinaryPagesAwayFromAgentPanelSlot()
        {
            var agentSpecs = DepthGatedAgentLayoutProvider.GetWebPageSpecs();
            var panelPosition = DepthGatedAgentLayoutProvider.GetAgentPanelActivePosition();

            Assert.That(agentSpecs.Select(spec => spec.LocalPosition), Has.None.EqualTo(panelPosition));
            Assert.That(agentSpecs.Select(spec => spec.LocalPosition), Has.None.EqualTo(LayeredPagesLayoutProvider.GetPageSpec(LayeredPagesSceneDefaults.InitialMainPageId).Position));
        }

        [Test]
        public void DepthGatedAgentLayoutProvider_KeepsOrdinaryPagesBetweenNear2AndFar1()
        {
            var agentSpecs = DepthGatedAgentLayoutProvider.GetWebPageSpecs();

            Assert.That(agentSpecs.Select(spec => spec.DepthLayerId), Is.SubsetOf(new[]
            {
                GazeDepthLayerProfile.Near2LayerId,
                GazeDepthLayerProfile.Near1LayerId,
                GazeDepthLayerProfile.ZeroLayerId,
                GazeDepthLayerProfile.Far1LayerId
            }));
            Assert.That(agentSpecs.Min(spec => spec.DepthLayerRayDistance), Is.GreaterThanOrEqualTo(GazeDepthLayerProfile.Near2RayDistance));
            Assert.That(agentSpecs.Max(spec => spec.DepthLayerRayDistance), Is.LessThanOrEqualTo(GazeDepthLayerProfile.Far1RayDistance));
        }

        [Test]
        public void DepthGatedAgentLayoutProvider_MapsAgentLogoToFar3LayeredSlot()
        {
            var logoSpec = DepthGatedAgentLayoutProvider.GetAgentLogoSpec();
            var pageO = LayeredPagesLayoutProvider.GetPageSpecs().Single(spec => spec.Name == "Page_O");

            Assert.That(logoSpec.PageId, Is.EqualTo(DepthGatedAgentPanelCoordinator.AgentLogoPageId));
            Assert.That(logoSpec.LocalPosition, Is.EqualTo(pageO.Position));
            Assert.That(logoSpec.SlotScale, Is.EqualTo(pageO.SlotScale).Within(0.0001f));
            Assert.That(logoSpec.DepthLayerId, Is.EqualTo(GazeDepthLayerProfile.Far3LayerId));
            Assert.That(logoSpec.DepthLayerRayDistance, Is.EqualTo(GazeDepthLayerProfile.Far3RayDistance).Within(0.0001f));
            Assert.That(logoSpec.DepthLayerTolerance, Is.EqualTo(pageO.DepthLayerTolerance).Within(0.0001f));
            Assert.That(logoSpec.MatchingRadius, Is.EqualTo(pageO.MatchingRegionRadiusWorld).Within(0.0001f));
        }

        [Test]
        public void DepthGatedAgentLayoutProvider_ExpandsPanelAtMainLayeredSlotWithVisualOffset()
        {
            var pageA = LayeredPagesLayoutProvider.GetPageSpecs().Single(spec => spec.Name == LayeredPagesSceneDefaults.InitialMainPageId);

            var panelPosition = DepthGatedAgentLayoutProvider.GetAgentPanelActivePosition();

            Assert.That(panelPosition.x, Is.EqualTo(pageA.Position.x).Within(0.0001f));
            Assert.That(panelPosition.y, Is.EqualTo(pageA.Position.y).Within(0.0001f));
            Assert.That(panelPosition.z, Is.EqualTo(pageA.Position.z + DepthGatedAgentLayoutProvider.AgentPanelVisualDepthOffset).Within(0.0001f));
            Assert.That(DepthGatedAgentLayoutProvider.GetAgentPanelActiveScale(), Is.EqualTo(pageA.SlotScale).Within(0.0001f));
            Assert.That(DepthGatedAgentLayoutProvider.GetAgentPanelActiveMatchingRadius(), Is.EqualTo(pageA.MatchingRegionRadiusWorld).Within(0.0001f));
        }

        [Test]
        public void GazeDepthLayerProfile_IsSymmetricAroundZeroPlane()
        {
            Assert.That(GazeDepthLayerProfile.LayerCount, Is.EqualTo(7));

            Assert.That(
                GazeDepthLayerProfile.ZeroRayDistance - GazeDepthLayerProfile.Near3RayDistance,
                Is.EqualTo(GazeDepthLayerProfile.Far3RayDistance - GazeDepthLayerProfile.ZeroRayDistance).Within(0.0001f));
            Assert.That(
                GazeDepthLayerProfile.ZeroRayDistance - GazeDepthLayerProfile.Near2RayDistance,
                Is.EqualTo(GazeDepthLayerProfile.Far2RayDistance - GazeDepthLayerProfile.ZeroRayDistance).Within(0.0001f));
            Assert.That(
                GazeDepthLayerProfile.ZeroRayDistance - GazeDepthLayerProfile.Near1RayDistance,
                Is.EqualTo(GazeDepthLayerProfile.Far1RayDistance - GazeDepthLayerProfile.ZeroRayDistance).Within(0.0001f));

            var profile = GazeDepthLayerProfile.CreateSymmetricSevenV1Profile();
            Assert.That(profile.ProfileVersion, Is.EqualTo(GazeDepthLayerProfile.SymmetricSevenV1ProfileVersion));
            Assert.That(profile.Layers.Count, Is.EqualTo(7));
            Assert.That(profile.ZeroRayDistance, Is.EqualTo(GazeDepthLayerProfile.ZeroRayDistance).Within(0.0001f));
        }

        [Test]
        public void LayeredPagesSceneDefaults_AlignsCameraAndWorldRootForLayerDistances()
        {
            Assert.That(LayeredPagesSceneDefaults.CameraPosition.z, Is.EqualTo(LayeredPagesSceneDefaults.WorldOffset.z).Within(0.0001f));
        }

        [Test]
        public void MockContentProvider_FallsBackToInitialMainPageContent()
        {
            var provider = new MockLayeredPageContentProvider();

            var fallbackContent = provider.GetContent("Missing_Page");

            Assert.That(fallbackContent.Query, Is.EqualTo("ThinkVision 27 3D Unity SDK"));
        }
    }
}
