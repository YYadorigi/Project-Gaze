using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    public interface ITargetMatcher
    {
        bool TryMatch(
            Camera targetCamera,
            IReadOnlyList<ISpatialTarget> targets,
            in GazeTrackingSample sample,
            out GazeHitResult hitResult);
    }

    public sealed class ViewportOnlyTargetMatcher : ITargetMatcher
    {
        public bool TryMatch(
            Camera targetCamera,
            IReadOnlyList<ISpatialTarget> targets,
            in GazeTrackingSample sample,
            out GazeHitResult hitResult)
        {
            return SpatialTargetMatchingUtility.TryProjectMatchingRegionHit(
                targetCamera,
                targets,
                sample.NormalizedViewportPoint,
                out hitResult,
                null,
                GazeDepthMatchingMode.ViewportOnly);
        }
    }

    public sealed class ContinuousWorldPointTargetMatcher : ITargetMatcher
    {
        public bool TryMatch(
            Camera targetCamera,
            IReadOnlyList<ISpatialTarget> targets,
            in GazeTrackingSample sample,
            out GazeHitResult hitResult)
        {
            if (!sample.HasPredictedWorldPoint)
            {
                hitResult = default;
                return false;
            }

            return SpatialTargetMatchingUtility.TryProjectPredictedWorldHit(
                targets,
                sample.PredictedWorldPoint,
                out hitResult,
                GazeDepthMatchingMode.ContinuousWorldPoint);
        }
    }

    public sealed class DiscreteDepthLayerTargetMatcher : ITargetMatcher
    {
        public bool TryMatch(
            Camera targetCamera,
            IReadOnlyList<ISpatialTarget> targets,
            in GazeTrackingSample sample,
            out GazeHitResult hitResult)
        {
            hitResult = default;
            if (sample.PredictedRayDistance <= 0f || targets == null)
            {
                return false;
            }

            string bestLayerId = null;
            float bestLayerRayDistance = 0f;
            float bestLayerDistanceError = float.PositiveInfinity;
            float bestLayerTolerance = 0f;

            foreach (var target in targets)
            {
                if (target == null || !target.HasDepthLayer)
                {
                    continue;
                }

                float distanceError = Mathf.Abs(sample.PredictedRayDistance - target.DepthLayerRayDistance);
                if (distanceError >= bestLayerDistanceError)
                {
                    continue;
                }

                bestLayerId = target.DepthLayerId;
                bestLayerRayDistance = target.DepthLayerRayDistance;
                bestLayerDistanceError = distanceError;
                bestLayerTolerance = target.DepthLayerTolerance;
            }

            if (string.IsNullOrWhiteSpace(bestLayerId) || bestLayerDistanceError > bestLayerTolerance)
            {
                return false;
            }

            return SpatialTargetMatchingUtility.TryProjectMatchingRegionHit(
                targetCamera,
                targets,
                sample.NormalizedViewportPoint,
                out hitResult,
                target => target != null && string.Equals(target.DepthLayerId, bestLayerId, StringComparison.Ordinal),
                GazeDepthMatchingMode.DiscreteDepthLayer,
                bestLayerId,
                bestLayerRayDistance);
        }
    }

    public sealed class TargetMatcherPipeline
    {
        private readonly ITargetMatcher viewportMatcher = new ViewportOnlyTargetMatcher();
        private readonly ITargetMatcher continuousMatcher = new ContinuousWorldPointTargetMatcher();
        private readonly ITargetMatcher discreteMatcher = new DiscreteDepthLayerTargetMatcher();

        public bool TryMatch(
            Camera targetCamera,
            IReadOnlyList<ISpatialTarget> targets,
            in GazeTrackingSample sample,
            GazeDepthMatchingMode matchingMode,
            out GazeHitResult hitResult)
        {
            switch (matchingMode)
            {
                case GazeDepthMatchingMode.ViewportOnly:
                    return viewportMatcher.TryMatch(targetCamera, targets, sample, out hitResult);

                case GazeDepthMatchingMode.ContinuousWorldPoint:
                    if (continuousMatcher.TryMatch(targetCamera, targets, sample, out hitResult))
                    {
                        return true;
                    }

                    return viewportMatcher.TryMatch(targetCamera, targets, sample, out hitResult);

                case GazeDepthMatchingMode.DiscreteDepthLayer:
                default:
                    if (discreteMatcher.TryMatch(targetCamera, targets, sample, out hitResult))
                    {
                        return true;
                    }

                    if (continuousMatcher.TryMatch(targetCamera, targets, sample, out hitResult))
                    {
                        return true;
                    }

                    return viewportMatcher.TryMatch(targetCamera, targets, sample, out hitResult);
            }
        }
    }

    public static class SpatialTargetMatchingUtility
    {
        public static bool TryProjectPredictedWorldHit(
            IReadOnlyList<ISpatialTarget> targets,
            Vector3 predictedWorldPoint,
            out GazeHitResult hitResult,
            GazeDepthMatchingMode matchingMode)
        {
            hitResult = default;
            if (targets == null)
            {
                return false;
            }

            ISpatialTarget bestTarget = null;
            float bestDistanceToCenter = float.PositiveInfinity;

            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                float distanceToCenter = Vector3.Distance(predictedWorldPoint, target.MatchingRegionCenterWorld);
                if (distanceToCenter > target.MatchingRegionRadiusWorld || distanceToCenter >= bestDistanceToCenter)
                {
                    continue;
                }

                bestTarget = target;
                bestDistanceToCenter = distanceToCenter;
            }

            if (bestTarget == null)
            {
                return false;
            }

            hitResult = BuildHitResult(bestTarget, matchingMode);
            return true;
        }

        public static bool TryProjectMatchingRegionHit(
            Camera targetCamera,
            IReadOnlyList<ISpatialTarget> targets,
            Vector2 gazeViewportPoint,
            out GazeHitResult hitResult,
            Predicate<ISpatialTarget> targetFilter = null,
            GazeDepthMatchingMode matchingMode = GazeDepthMatchingMode.ViewportOnly,
            string depthLayerId = null,
            float depthLayerRayDistance = 0f)
        {
            hitResult = default;
            if (targetCamera == null || targets == null)
            {
                return false;
            }

            ISpatialTarget bestTarget = null;
            float bestDistanceToCenter = float.PositiveInfinity;
            float bestDepth = float.PositiveInfinity;

            foreach (var target in targets)
            {
                if (target == null ||
                    (targetFilter != null && !targetFilter(target)) ||
                    !TryResolveMatchingRegionHit(target, targetCamera, gazeViewportPoint, out float distanceToCenter, out float depth))
                {
                    continue;
                }

                if (distanceToCenter > bestDistanceToCenter + 0.0001f)
                {
                    continue;
                }

                if (Mathf.Abs(distanceToCenter - bestDistanceToCenter) <= 0.0001f && depth >= bestDepth)
                {
                    continue;
                }

                bestTarget = target;
                bestDistanceToCenter = distanceToCenter;
                bestDepth = depth;
            }

            if (bestTarget == null)
            {
                return false;
            }

            hitResult = BuildHitResult(
                bestTarget,
                matchingMode,
                string.IsNullOrWhiteSpace(depthLayerId) ? bestTarget.DepthLayerId : depthLayerId,
                depthLayerRayDistance > 0f ? depthLayerRayDistance : bestTarget.DepthLayerRayDistance);
            return true;
        }

        private static GazeHitResult BuildHitResult(
            ISpatialTarget target,
            GazeDepthMatchingMode matchingMode,
            string depthLayerId = null,
            float depthLayerRayDistance = 0f)
        {
            return new GazeHitResult(
                true,
                true,
                target,
                target.TargetId,
                matchingMode,
                string.IsNullOrWhiteSpace(depthLayerId) ? target.DepthLayerId : depthLayerId,
                depthLayerRayDistance > 0f ? depthLayerRayDistance : target.DepthLayerRayDistance);
        }

        private static bool TryResolveMatchingRegionHit(
            ISpatialTarget target,
            Camera targetCamera,
            Vector2 gazeViewportPoint,
            out float distanceToCenter,
            out float depth)
        {
            distanceToCenter = float.PositiveInfinity;
            depth = float.PositiveInfinity;

            Vector3 centerViewport = targetCamera.WorldToViewportPoint(target.MatchingRegionCenterWorld);
            if (centerViewport.z <= 0f)
            {
                return false;
            }

            float viewportRadius = ResolveViewportRadius(targetCamera, target.MatchingRegionCenterWorld, target.MatchingRegionRadiusWorld);
            if (viewportRadius <= Mathf.Epsilon)
            {
                return false;
            }

            distanceToCenter = Vector2.Distance(
                gazeViewportPoint,
                new Vector2(centerViewport.x, centerViewport.y));
            if (distanceToCenter > viewportRadius)
            {
                return false;
            }

            depth = centerViewport.z;
            return true;
        }

        private static float ResolveViewportRadius(Camera targetCamera, Vector3 worldCenter, float worldRadius)
        {
            Vector3 centerViewport = targetCamera.WorldToViewportPoint(worldCenter);
            Vector3 rightViewport = targetCamera.WorldToViewportPoint(worldCenter + (targetCamera.transform.right * worldRadius));
            Vector3 upViewport = targetCamera.WorldToViewportPoint(worldCenter + (targetCamera.transform.up * worldRadius));
            Vector2 centerViewport2D = new(centerViewport.x, centerViewport.y);

            float horizontalRadius = Vector2.Distance(centerViewport2D, new Vector2(rightViewport.x, rightViewport.y));
            float verticalRadius = Vector2.Distance(centerViewport2D, new Vector2(upViewport.x, upViewport.y));
            return Mathf.Max(horizontalRadius, verticalRadius);
        }
    }
}
