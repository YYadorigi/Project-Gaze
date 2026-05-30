using UnityEngine;
using UnityEngine.InputSystem;
using static AS3DPlugin.StereoCam;

namespace AS3DPlugin
{
    [RequireComponent(typeof(StereoCam))]
    /// <summary>
    /// Use 2D Mouse for the 3D UI
    /// </summary>
    public class StereoUI : MonoBehaviour
    {
        #region Public properties
        [Tooltip("3D Cursor Model(no collider attached) in current Scene")]
        public GameObject Cursor3D;
        [Tooltip("3D Canvas (In world space) in current Scene")]
        public Canvas Canvas3D;
        #endregion
        #region Static variables
        public static Ray ray2DMouse;
        public static RaycastHit hitController = new RaycastHit();
        private static Camera rayCamFixed;
        #endregion
        #region Other variables
        [HideInInspector]
        public Vector3 ViewportPoint;
        private StereoCam cam;
        private int mask3DCursor;   //layers to put 3d cursor on
        private int modifiedHalf;
        private AutoStereoShader displayTypeChange = AutoStereoShader.Mono;
        private bool eventCamOtherHalf = false;
        private bool lastEventCamOtherHalf = false;
        #endregion
        #region Methodes
        /// <summary>
        /// reusing the normal UI ray casting system
        /// </summary>
        /// <param name="VP">cursor position on screen as view point</param>
        /// <param name="modified"> bit0: not modified; bit1: modified x; bit2: modified y</param>
        /// <returns></returns>
        public static Ray MousePosToRay(ref Vector3 VP, ref int modified)
        {
            modified = 0;
            var mousePosition = GetMousePosition();
            var screenPosition = new Vector3(mousePosition.x, mousePosition.y, 0f);
            VP = rayCamFixed.ScreenToViewportPoint(screenPosition);
            if (VP.x > 1)
            {
                VP.x -= 1;
                modified |= 1;
            }
            if (VP.y > 1)
            {
                VP.y -= 1;
                modified |= 2;
            }
            Ray ray = rayCamFixed.ViewportPointToRay(VP);
            return ray;
        }

        public void refreshEventCam(AutoStereoShader displayType, bool init, int whichSide = 0)
        {
            if (displayTypeChange != displayType)
            {
                displayTypeChange = displayType;
                init = true;
            }
            switch (displayType)
            {
                case AutoStereoShader.SideBySide:
                    eventCamOtherHalf = (whichSide & 1) > 0;
                    if (lastEventCamOtherHalf != eventCamOtherHalf || init)
                    {
                        if (init)
                            rayCamFixed = cam.subCams[0];
                        lastEventCamOtherHalf = eventCamOtherHalf;
                        if (lastEventCamOtherHalf)
                        {
                            Canvas3D.worldCamera = cam.subCams[1];
                        }
                        else
                        {
                            Canvas3D.worldCamera = cam.subCams[0];
                        }
                    }
                    break;
                case AutoStereoShader.Mono:
                    if (init)
                    {
                        rayCamFixed = cam.thisCam;
                        Canvas3D.worldCamera = cam.subCams[0];
                    }
                    break;
                default:
                    if (init)
                    {
                        rayCamFixed = cam.thisCam;
                        Canvas3D.worldCamera = cam.thisCam;
                    }
                    break;
            }
        }

        public RaycastHit RayCastInteraction(Ray ray)
        {
            if (Physics.Raycast(ray, out hitController, Mathf.Infinity, mask3DCursor))
            {
                Cursor3D.transform.position = hitController.point - hitController.normal * 0.001f * scaleFactor;
                Cursor3D.transform.rotation = Quaternion.LookRotation(-hitController.normal, Vector3.forward);
            }
            return hitController;
        }
        #endregion
        #region Unity Functions
        private void Start()
        {
            cam = GetComponent<StereoCam>();
            rayCamFixed = cam.thisCam;
            if (Canvas3D != null)
                Canvas3D.worldCamera = cam.thisCam;
            mask3DCursor = LayerMask.GetMask("Default", "UI", "RayModel", "CursorPlane");
            Physics.queriesHitBackfaces = true;
        }

        void Update()
        {
            if (Cursor3D != null && Canvas3D != null)
            {
                if (!mockHMDDetected)
                {
                    ray2DMouse = MousePosToRay(ref ViewportPoint, ref modifiedHalf);
                    refreshEventCam(cam.usedShader, false, modifiedHalf);
                }
                else
                {
                    var mousePosition = GetMousePosition();
                    ViewportPoint = rayCamFixed.ScreenToViewportPoint(new Vector3(mousePosition.x, mousePosition.y, 0f));
                    ray2DMouse = rayCamFixed.ViewportPointToRay(ViewportPoint);
                }
                RayCastInteraction(ray2DMouse);
            }
        }

        private static Vector2 GetMousePosition()
        {
            var mouse = Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
        }
        #endregion
    }
}
