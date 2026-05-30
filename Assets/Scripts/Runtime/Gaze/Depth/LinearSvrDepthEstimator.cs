using System;
using UnityEngine;

namespace ProjectGaze.Gaze.Depth
{
    public static class LinearSvrDepthEstimator
    {
        private const int TrainingEpochs = 320;
        private const float InitialLearningRate = 0.03f;
        private const float LearningRateDecay = 0.012f;
        private const float Regularization = 0.0005f;
        private const float ResidualEpsilon = 0.025f;

        public static LinearSvrDepthModel Train(
            GazeDepthCalibrationDataset dataset,
            DisparityDepthModel baselineModel)
        {
            var model = new LinearSvrDepthModel
            {
                Epsilon = ResidualEpsilon
            };

            if (dataset?.Records == null || dataset.Records.Length < 4 || baselineModel == null || !baselineModel.IsTrained)
            {
                return model;
            }

            int sampleCount = dataset.Records.Length;
            int featureCount = GazeDepthFeatureExtractor.FeatureCount;
            var features = new float[sampleCount][];
            var residualTargets = new float[sampleCount];
            int usableCount = 0;

            for (int index = 0; index < dataset.Records.Length; index += 1)
            {
                var record = dataset.Records[index];
                if (record?.Features == null ||
                    record.Features.Length != featureCount ||
                    !DisparityDepthEstimator.TryPredict(record.Features, baselineModel, out float baselinePrediction))
                {
                    continue;
                }

                features[usableCount] = (float[])record.Features.Clone();
                residualTargets[usableCount] = record.TargetRayDistance - baselinePrediction;
                usableCount += 1;
            }

            if (usableCount < 4)
            {
                return model;
            }

            Array.Resize(ref features, usableCount);
            Array.Resize(ref residualTargets, usableCount);

            model.FeatureMeans = ComputeFeatureMeans(features, featureCount);
            model.FeatureScales = ComputeFeatureScales(features, model.FeatureMeans, featureCount);
            model.Weights = new float[featureCount];
            model.Bias = 0f;

            var normalizedFeatures = NormalizeFeatures(features, model.FeatureMeans, model.FeatureScales, featureCount);

            for (int epoch = 0; epoch < TrainingEpochs; epoch += 1)
            {
                float learningRate = InitialLearningRate / (1f + (epoch * LearningRateDecay));

                for (int sampleOffset = 0; sampleOffset < usableCount; sampleOffset += 1)
                {
                    int sampleIndex = (epoch + sampleOffset) % usableCount;
                    var sample = normalizedFeatures[sampleIndex];
                    float prediction = PredictNormalized(sample, model.Weights, model.Bias);
                    float error = prediction - residualTargets[sampleIndex];
                    int gradientSign = error > model.Epsilon
                        ? 1
                        : error < -model.Epsilon
                            ? -1
                            : 0;

                    for (int featureIndex = 0; featureIndex < featureCount; featureIndex += 1)
                    {
                        model.Weights[featureIndex] *= 1f - (learningRate * Regularization);
                    }

                    if (gradientSign == 0)
                    {
                        continue;
                    }

                    for (int featureIndex = 0; featureIndex < featureCount; featureIndex += 1)
                    {
                        model.Weights[featureIndex] -= learningRate * gradientSign * sample[featureIndex];
                    }

                    model.Bias -= learningRate * gradientSign;
                }
            }

            model.IsTrained = true;
            return model;
        }

        public static float PredictResidual(float[] features, LinearSvrDepthModel model)
        {
            if (features == null ||
                model == null ||
                !model.IsTrained ||
                model.Weights == null ||
                model.FeatureMeans == null ||
                model.FeatureScales == null ||
                features.Length != model.Weights.Length)
            {
                return 0f;
            }

            var normalizedFeatures = new float[features.Length];
            for (int index = 0; index < features.Length; index += 1)
            {
                normalizedFeatures[index] = (features[index] - model.FeatureMeans[index]) / model.FeatureScales[index];
            }

            return PredictNormalized(normalizedFeatures, model.Weights, model.Bias);
        }

        private static float PredictNormalized(float[] normalizedFeatures, float[] weights, float bias)
        {
            float sum = bias;
            for (int index = 0; index < normalizedFeatures.Length; index += 1)
            {
                sum += weights[index] * normalizedFeatures[index];
            }

            return sum;
        }

        private static float[] ComputeFeatureMeans(float[][] features, int featureCount)
        {
            var means = new float[featureCount];
            for (int sampleIndex = 0; sampleIndex < features.Length; sampleIndex += 1)
            {
                for (int featureIndex = 0; featureIndex < featureCount; featureIndex += 1)
                {
                    means[featureIndex] += features[sampleIndex][featureIndex];
                }
            }

            for (int featureIndex = 0; featureIndex < featureCount; featureIndex += 1)
            {
                means[featureIndex] /= features.Length;
            }

            return means;
        }

        private static float[] ComputeFeatureScales(float[][] features, float[] means, int featureCount)
        {
            var scales = new float[featureCount];
            for (int sampleIndex = 0; sampleIndex < features.Length; sampleIndex += 1)
            {
                for (int featureIndex = 0; featureIndex < featureCount; featureIndex += 1)
                {
                    float centered = features[sampleIndex][featureIndex] - means[featureIndex];
                    scales[featureIndex] += centered * centered;
                }
            }

            for (int featureIndex = 0; featureIndex < featureCount; featureIndex += 1)
            {
                float variance = scales[featureIndex] / Mathf.Max(1, features.Length);
                scales[featureIndex] = Mathf.Max(0.001f, Mathf.Sqrt(variance));
            }

            return scales;
        }

        private static float[][] NormalizeFeatures(float[][] features, float[] means, float[] scales, int featureCount)
        {
            var normalizedFeatures = new float[features.Length][];

            for (int sampleIndex = 0; sampleIndex < features.Length; sampleIndex += 1)
            {
                normalizedFeatures[sampleIndex] = new float[featureCount];
                for (int featureIndex = 0; featureIndex < featureCount; featureIndex += 1)
                {
                    normalizedFeatures[sampleIndex][featureIndex] =
                        (features[sampleIndex][featureIndex] - means[featureIndex]) / scales[featureIndex];
                }
            }

            return normalizedFeatures;
        }
    }
}
