using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    internal sealed class MainViewPageSwapper
    {
        private readonly struct PagePose
        {
            public PagePose(
                Vector3 localPosition,
                Vector3 localScale,
                float matchingRadius,
                string depthLayerId,
                float depthLayerRayDistance,
                float depthLayerTolerance)
            {
                LocalPosition = localPosition;
                LocalScale = localScale;
                MatchingRadius = matchingRadius;
                DepthLayerId = depthLayerId;
                DepthLayerRayDistance = depthLayerRayDistance;
                DepthLayerTolerance = depthLayerTolerance;
            }

            public Vector3 LocalPosition { get; }

            public Vector3 LocalScale { get; }

            public float MatchingRadius { get; }

            public string DepthLayerId { get; }

            public float DepthLayerRayDistance { get; }

            public float DepthLayerTolerance { get; }

            public static PagePose Capture(SpatialPage page)
            {
                return new PagePose(
                    page.transform.localPosition,
                    page.transform.localScale,
                    page.MatchingRegionRadiusWorld,
                    page.DepthLayerId,
                    page.DepthLayerRayDistance,
                    page.DepthLayerTolerance);
            }

            public void ApplyTo(SpatialPage page)
            {
                page.transform.localPosition = LocalPosition;
                page.transform.localScale = LocalScale;
                page.ConfigureMatchingRegion(MatchingRadius);
                page.ConfigureDepthLayer(DepthLayerId, DepthLayerRayDistance, DepthLayerTolerance);
            }
        }

        private readonly MonoBehaviour coroutineHost;
        private readonly IReadOnlyDictionary<string, SpatialPage> pagesById;
        private readonly float swapDurationSeconds;
        private readonly Dictionary<string, PagePose> defaultPagePoses = new();
        private readonly bool usesFixedMainViewPose;
        private readonly PagePose fixedMainViewPose;
        private Coroutine activeSwapCoroutine;
        private bool currentMainPageOccupiesMainView;

        public MainViewPageSwapper(MonoBehaviour coroutineHost, IReadOnlyDictionary<string, SpatialPage> pagesById)
            : this(coroutineHost, pagesById, LayeredPagesSceneDefaults.InitialMainPageId, LayeredPagesSceneDefaults.MainViewSwapDurationSeconds)
        {
        }

        public MainViewPageSwapper(
            MonoBehaviour coroutineHost,
            IReadOnlyDictionary<string, SpatialPage> pagesById,
            string initialMainPageId,
            float swapDurationSeconds)
        {
            this.coroutineHost = coroutineHost;
            this.pagesById = pagesById;
            this.swapDurationSeconds = Mathf.Max(0.01f, swapDurationSeconds);
            CurrentMainPageId = initialMainPageId;
            currentMainPageOccupiesMainView = true;
            CaptureDefaultPagePoses();
        }

        public MainViewPageSwapper(
            MonoBehaviour coroutineHost,
            IReadOnlyDictionary<string, SpatialPage> pagesById,
            string initialMainPageId,
            float swapDurationSeconds,
            Vector3 fixedMainViewLocalPosition,
            Vector3 fixedMainViewLocalScale,
            float fixedMainViewMatchingRadius,
            string fixedMainViewDepthLayerId,
            float fixedMainViewDepthLayerRayDistance,
            float fixedMainViewDepthLayerTolerance)
            : this(coroutineHost, pagesById, initialMainPageId, swapDurationSeconds)
        {
            usesFixedMainViewPose = true;
            currentMainPageOccupiesMainView = false;
            fixedMainViewPose = new PagePose(
                fixedMainViewLocalPosition,
                fixedMainViewLocalScale,
                fixedMainViewMatchingRadius,
                fixedMainViewDepthLayerId,
                fixedMainViewDepthLayerRayDistance,
                fixedMainViewDepthLayerTolerance);
        }

        public string CurrentMainPageId { get; private set; }

        public bool TrySwapTo(string nextMainPageId)
        {
            if (string.IsNullOrEmpty(nextMainPageId) || nextMainPageId == CurrentMainPageId)
            {
                return usesFixedMainViewPose && TryPromoteToFixedMainView(nextMainPageId);
            }

            if (usesFixedMainViewPose)
            {
                return TryPromoteToFixedMainView(nextMainPageId);
            }

            if (!pagesById.TryGetValue(CurrentMainPageId, out var currentMainPage) ||
                !pagesById.TryGetValue(nextMainPageId, out var nextMainPage))
            {
                return false;
            }

            Transform currentMainTransform = currentMainPage.transform;
            Transform nextMainTransform = nextMainPage.transform;

            Vector3 currentMainPosition = currentMainTransform.localPosition;
            Vector3 currentMainScale = currentMainTransform.localScale;
            Vector3 nextMainPosition = nextMainTransform.localPosition;
            Vector3 nextMainScale = nextMainTransform.localScale;

            if (activeSwapCoroutine != null)
            {
                coroutineHost.StopCoroutine(activeSwapCoroutine);
            }

            activeSwapCoroutine = coroutineHost.StartCoroutine(AnimateMainViewSwap(
                currentMainTransform,
                currentMainPosition,
                currentMainScale,
                nextMainPosition,
                nextMainScale,
                nextMainTransform,
                nextMainPosition,
                nextMainScale,
                currentMainPosition,
                currentMainScale));

            CurrentMainPageId = nextMainPageId;
            return true;
        }

        public bool TryRestoreCurrentMainPage()
        {
            if (!usesFixedMainViewPose ||
                !currentMainPageOccupiesMainView ||
                string.IsNullOrEmpty(CurrentMainPageId) ||
                !pagesById.TryGetValue(CurrentMainPageId, out var currentMainPage) ||
                !defaultPagePoses.TryGetValue(CurrentMainPageId, out var defaultPose))
            {
                return false;
            }

            if (activeSwapCoroutine != null)
            {
                coroutineHost.StopCoroutine(activeSwapCoroutine);
            }

            activeSwapCoroutine = coroutineHost.StartCoroutine(AnimateSinglePagePose(
                currentMainPage,
                PagePose.Capture(currentMainPage),
                defaultPose));
            currentMainPageOccupiesMainView = false;
            return true;
        }

        private bool TryPromoteToFixedMainView(string nextMainPageId)
        {
            if (string.IsNullOrEmpty(nextMainPageId) ||
                !pagesById.TryGetValue(nextMainPageId, out var nextMainPage))
            {
                return false;
            }

            if (currentMainPageOccupiesMainView &&
                string.Equals(nextMainPageId, CurrentMainPageId, System.StringComparison.Ordinal))
            {
                return false;
            }

            SpatialPage currentMainPage = null;
            PagePose currentMainTargetPose = default;
            bool shouldRestoreCurrentMainPage = currentMainPageOccupiesMainView &&
                                                pagesById.TryGetValue(CurrentMainPageId, out currentMainPage) &&
                                                defaultPagePoses.TryGetValue(CurrentMainPageId, out currentMainTargetPose);

            if (activeSwapCoroutine != null)
            {
                coroutineHost.StopCoroutine(activeSwapCoroutine);
            }

            activeSwapCoroutine = coroutineHost.StartCoroutine(AnimateFixedMainViewSwap(
                shouldRestoreCurrentMainPage ? currentMainPage : null,
                currentMainTargetPose,
                nextMainPage,
                PagePose.Capture(nextMainPage),
                fixedMainViewPose));

            CurrentMainPageId = nextMainPageId;
            currentMainPageOccupiesMainView = true;
            return true;
        }

        private void CaptureDefaultPagePoses()
        {
            defaultPagePoses.Clear();
            foreach (var pageEntry in pagesById)
            {
                if (pageEntry.Value != null)
                {
                    defaultPagePoses[pageEntry.Key] = PagePose.Capture(pageEntry.Value);
                }
            }
        }

        private IEnumerator AnimateMainViewSwap(
            Transform currentMainTransform,
            Vector3 currentMainStartPosition,
            Vector3 currentMainStartScale,
            Vector3 currentMainTargetPosition,
            Vector3 currentMainTargetScale,
            Transform nextMainTransform,
            Vector3 nextMainStartPosition,
            Vector3 nextMainStartScale,
            Vector3 nextMainTargetPosition,
            Vector3 nextMainTargetScale)
        {
            float elapsedSeconds = 0f;

            while (elapsedSeconds < swapDurationSeconds)
            {
                float t = elapsedSeconds / swapDurationSeconds;
                float easedT = t * t * (3f - (2f * t));

                currentMainTransform.localPosition = Vector3.Lerp(currentMainStartPosition, currentMainTargetPosition, easedT);
                currentMainTransform.localScale = Vector3.Lerp(currentMainStartScale, currentMainTargetScale, easedT);
                nextMainTransform.localPosition = Vector3.Lerp(nextMainStartPosition, nextMainTargetPosition, easedT);
                nextMainTransform.localScale = Vector3.Lerp(nextMainStartScale, nextMainTargetScale, easedT);

                elapsedSeconds += Time.deltaTime;
                yield return null;
            }

            currentMainTransform.localPosition = currentMainTargetPosition;
            currentMainTransform.localScale = currentMainTargetScale;
            nextMainTransform.localPosition = nextMainTargetPosition;
            nextMainTransform.localScale = nextMainTargetScale;
            activeSwapCoroutine = null;
        }

        private IEnumerator AnimateFixedMainViewSwap(
            SpatialPage pageToRestore,
            PagePose restoreTargetPose,
            SpatialPage pageToPromote,
            PagePose promoteStartPose,
            PagePose promoteTargetPose)
        {
            PagePose restoreStartPose = pageToRestore != null
                ? PagePose.Capture(pageToRestore)
                : default;

            float elapsedSeconds = 0f;

            while (elapsedSeconds < swapDurationSeconds)
            {
                float easedT = ResolveEasedProgress(elapsedSeconds);

                if (pageToRestore != null)
                {
                    ApplyInterpolatedTransform(pageToRestore.transform, restoreStartPose, restoreTargetPose, easedT);
                }

                ApplyInterpolatedTransform(pageToPromote.transform, promoteStartPose, promoteTargetPose, easedT);
                elapsedSeconds += Time.deltaTime;
                yield return null;
            }

            if (pageToRestore != null)
            {
                restoreTargetPose.ApplyTo(pageToRestore);
            }

            promoteTargetPose.ApplyTo(pageToPromote);
            activeSwapCoroutine = null;
        }

        private IEnumerator AnimateSinglePagePose(
            SpatialPage page,
            PagePose startPose,
            PagePose targetPose)
        {
            float elapsedSeconds = 0f;

            while (elapsedSeconds < swapDurationSeconds)
            {
                ApplyInterpolatedTransform(page.transform, startPose, targetPose, ResolveEasedProgress(elapsedSeconds));
                elapsedSeconds += Time.deltaTime;
                yield return null;
            }

            targetPose.ApplyTo(page);
            activeSwapCoroutine = null;
        }

        private float ResolveEasedProgress(float elapsedSeconds)
        {
            float t = elapsedSeconds / swapDurationSeconds;
            return t * t * (3f - (2f * t));
        }

        private static void ApplyInterpolatedTransform(
            Transform pageTransform,
            PagePose startPose,
            PagePose targetPose,
            float easedT)
        {
            pageTransform.localPosition = Vector3.Lerp(startPose.LocalPosition, targetPose.LocalPosition, easedT);
            pageTransform.localScale = Vector3.Lerp(startPose.LocalScale, targetPose.LocalScale, easedT);
        }
    }
}
