using UnityEngine;

namespace ProjectGaze.Gaze
{
    public static class GazeViewportPointUtility
    {
        public static Vector2 ClampViewportPoint(Vector2 viewportPoint)
        {
            return new Vector2(
                Mathf.Clamp01(viewportPoint.x),
                Mathf.Clamp01(viewportPoint.y));
        }

        public static Vector2 DisplayAreaToViewport(Vector2 displayAreaPoint)
        {
            return ClampViewportPoint(new Vector2(
                displayAreaPoint.x,
                1f - displayAreaPoint.y));
        }

        public static Vector2 DisplayAreaToViewport(Camera targetCamera, Vector2 displayAreaPoint)
        {
            return DisplayAreaToViewport(displayAreaPoint);
        }

        public static bool TryResolveViewportPointFromScreenRay(
            Camera targetCamera,
            Ray screenRay,
            out Vector2 viewportPoint)
        {
            viewportPoint = default;

            if (targetCamera == null ||
                !IsFiniteVector3(screenRay.origin) ||
                !IsFiniteVector3(screenRay.direction) ||
                screenRay.direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            if (TryResolveViewportPointFromWorldPoint(targetCamera, screenRay.origin, out viewportPoint))
            {
                return true;
            }

            var fallbackPoint = screenRay.GetPoint(Mathf.Max(targetCamera.nearClipPlane + 1f, 1f));
            return TryResolveViewportPointFromWorldPoint(targetCamera, fallbackPoint, out viewportPoint);
        }

        public static Ray BuildWorldRay(Camera targetCamera, Vector2 normalizedViewportPoint)
        {
            return TryBuildWorldRay(targetCamera, normalizedViewportPoint, out var worldRay)
                ? worldRay
                : default;
        }

        public static bool TryBuildWorldRay(Camera targetCamera, Vector2 normalizedViewportPoint, out Ray worldRay)
        {
            worldRay = default;

            if (targetCamera == null)
            {
                return false;
            }

            if (!IsFiniteVector3(targetCamera.transform.position) ||
                !IsFiniteQuaternion(targetCamera.transform.rotation) ||
                !IsFiniteMatrix(targetCamera.worldToCameraMatrix) ||
                !IsFiniteMatrix(targetCamera.projectionMatrix))
            {
                return false;
            }

            var clampedViewportPoint = ClampViewportPoint(normalizedViewportPoint);
            try
            {
                worldRay = targetCamera.ViewportPointToRay(new Vector3(
                    clampedViewportPoint.x,
                    clampedViewportPoint.y,
                    0f));
            }
            catch
            {
                worldRay = default;
                return false;
            }

            return IsFiniteVector3(worldRay.origin) &&
                   IsFiniteVector3(worldRay.direction) &&
                   worldRay.direction.sqrMagnitude > Mathf.Epsilon;
        }

        private static bool TryResolveViewportPointFromWorldPoint(
            Camera targetCamera,
            Vector3 worldPoint,
            out Vector2 viewportPoint)
        {
            viewportPoint = default;

            if (!IsFiniteVector3(worldPoint))
            {
                return false;
            }

            var rawViewportPoint = targetCamera.WorldToViewportPoint(worldPoint);
            if (!IsFiniteVector3(rawViewportPoint) || rawViewportPoint.z <= 0f)
            {
                return false;
            }

            viewportPoint = ClampViewportPoint(new Vector2(rawViewportPoint.x, rawViewportPoint.y));
            return true;
        }

        public static bool IsFiniteVector3(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFiniteMatrix(Matrix4x4 value)
        {
            for (int index = 0; index < 16; index += 1)
            {
                if (!IsFinite(value[index]))
                {
                    return false;
                }
            }

            float determinant = value.determinant;
            return IsFinite(determinant) && Mathf.Abs(determinant) > Mathf.Epsilon;
        }

        private static bool IsFiniteQuaternion(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }
    }
}
