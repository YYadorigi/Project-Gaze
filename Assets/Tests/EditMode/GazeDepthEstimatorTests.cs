using NUnit.Framework;
using ProjectGaze.Gaze.Depth;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class GazeDepthFeatureExtractorTests
    {
        [Test]
        public void TryBuildFeatureVector_EncodesStereoDisparityAndOptionalSignals()
        {
            var sample = new BinocularGazeSample(
                true,
                new Vector2(0.5f, 0.4f),
                new BinocularEyeObservation(true, new Vector2(0.6f, 0.4f), true, new Vector2(0.42f, 0.51f), true, new Vector3(1f, 2f, 3f), false, default),
                new BinocularEyeObservation(true, new Vector2(0.4f, 0.4f), true, new Vector2(0.58f, 0.49f), true, new Vector3(4f, 5f, 6f), false, default),
                1L);

            bool resolved = GazeDepthFeatureExtractor.TryBuildFeatureVector(sample, out var features);

            Assert.That(resolved, Is.True);
            Assert.That(features[GazeDepthFeatureExtractor.DisparityXIndex], Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(features[GazeDepthFeatureExtractor.DisparityMagnitudeIndex], Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(features[GazeDepthFeatureExtractor.PupilDisparityXIndex], Is.EqualTo(-0.16f).Within(0.0001f));
            Assert.That(features[GazeDepthFeatureExtractor.OriginDisparityZIndex], Is.EqualTo(-3f).Within(0.0001f));
        }
    }

    public sealed class DisparityDepthEstimatorTests
    {
        [Test]
        public void TrainAndPredict_FollowsInverseDisparityTrend()
        {
            var dataset = new GazeDepthCalibrationDataset
            {
                Records = new[]
                {
                    CreateRecord(0.50f, 2.0f, 0.5f),
                    CreateRecord(0.25f, 4.0f, 0.5f),
                    CreateRecord(0.125f, 8.0f, 0.5f)
                }
            };

            var model = DisparityDepthEstimator.Train(dataset);
            bool predicted = DisparityDepthEstimator.TryPredict(GazeDepthTestFeatureFactory.CreateFeatures(0.20f, 0.55f), model, out float depth);

            Assert.That(model.IsTrained, Is.True);
            Assert.That(predicted, Is.True);
            Assert.That(depth, Is.EqualTo(5.0f).Within(0.4f));
        }

        private static GazeDepthCalibrationRecord CreateRecord(float disparityMagnitude, float targetDepth, float recommendedX)
        {
            return new GazeDepthCalibrationRecord
            {
                Features = GazeDepthTestFeatureFactory.CreateFeatures(disparityMagnitude, recommendedX),
                TargetRayDistance = targetDepth,
                TargetViewportX = recommendedX,
                TargetViewportY = 0.5f
            };
        }
    }

    public sealed class LinearSvrDepthEstimatorTests
    {
        [Test]
        public void Train_PredictsDifferentResidualsForDifferentViewportPatterns()
        {
            var dataset = new GazeDepthCalibrationDataset
            {
                Records = new[]
                {
                    CreateResidualRecord(0.20f, 0.20f),
                    CreateResidualRecord(0.25f, 0.20f),
                    CreateResidualRecord(0.30f, 0.20f),
                    CreateResidualRecord(0.20f, 0.80f),
                    CreateResidualRecord(0.25f, 0.80f),
                    CreateResidualRecord(0.30f, 0.80f)
                }
            };

            var baselineModel = DisparityDepthEstimator.Train(dataset);
            var svrModel = LinearSvrDepthEstimator.Train(dataset, baselineModel);

            float lowResidual = LinearSvrDepthEstimator.PredictResidual(GazeDepthTestFeatureFactory.CreateFeatures(0.25f, 0.20f), svrModel);
            float highResidual = LinearSvrDepthEstimator.PredictResidual(GazeDepthTestFeatureFactory.CreateFeatures(0.25f, 0.80f), svrModel);

            Assert.That(svrModel.IsTrained, Is.True);
            Assert.That(highResidual, Is.GreaterThan(lowResidual + 0.4f));
        }

        private static GazeDepthCalibrationRecord CreateResidualRecord(float disparityMagnitude, float recommendedX)
        {
            float baselineDepth = 1f / disparityMagnitude;
            float residual = (recommendedX - 0.5f) * 2f;

            return new GazeDepthCalibrationRecord
            {
                Features = GazeDepthTestFeatureFactory.CreateFeatures(disparityMagnitude, recommendedX),
                TargetRayDistance = baselineDepth + residual,
                TargetViewportX = recommendedX,
                TargetViewportY = 0.5f
            };
        }
    }

    internal static class GazeDepthTestFeatureFactory
    {
        public static float[] CreateFeatures(float disparityMagnitude, float recommendedX)
        {
            float leftX = recommendedX + (disparityMagnitude * 0.5f);
            float rightX = recommendedX - (disparityMagnitude * 0.5f);
            var sample = new BinocularGazeSample(
                true,
                new Vector2(recommendedX, 0.5f),
                new BinocularEyeObservation(true, new Vector2(leftX, 0.5f), false, default, false, default, false, default),
                new BinocularEyeObservation(true, new Vector2(rightX, 0.5f), false, default, false, default, false, default),
                1L);

            Assert.That(GazeDepthFeatureExtractor.TryBuildFeatureVector(sample, out var features), Is.True);
            return features;
        }
    }
}
