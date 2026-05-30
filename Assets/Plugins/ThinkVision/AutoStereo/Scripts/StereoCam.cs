//#define UNITY_HDRP    //need to #Define UNITY_HDRP if HDRP pipeline is used
//#define USEMOCKHMD  //tested with unity 2022.3.13 SRP Backend and MOCKHMD v1.3.1 with single instance pass instanced 3D enabled & d3d12/vulkan enabled in the player settings, also be aware the fps can only be capped by setting the maxium fps in GPU panel
//#define POSTPROCESSING    //need to #Define POSTPROCESSING if postprocessing is used, you should enable post processing if MOCKHMD is enabled for FXAA. Don't use TAA.
//#define DISABLEAUTOPLAY   //enable to disable system autoplay pop out if in 3D, need to switch to .net4
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Xml;
using WindowsDisplayAPI.DisplayConfig;
using WindowsDisplayAPI.Native.DisplayConfig;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.XR;
using UnityEngine.InputSystem;
#if USEMOCKHMD
using UnityEngine.XR.Management;
#endif
using static WindowHandler;
using UnityEngine.Events;
using System.Runtime.InteropServices;
#if POSTPROCESSING
using UnityEngine.Rendering.PostProcessing;
#endif
#if DISABLEAUTOPLAY
using Microsoft.Win32;
#endif

namespace AS3DPlugin
{
    /// <summary>
    /// Attach this script to the targeting Main Camera in the scene, and it will be converted to 3D Camera at the runtime, set VirtualScreenWidth as the avatar of 
    /// the physical screen, mapping it's scale in the virtual world
    /// 
    /// Release Ver: 2.4
    /// </summary>
    public class StereoCam : MonoBehaviour
    {
#region Public properties for init   
        [Tooltip("It's the virtual avatar size for the 3D screen, Refer to user manual or the sample scenes to set it")]
        public float VirtualScreenWidth = 1;    // in unity3d meters
        [Tooltip("Enable frustum sync when start, this can benefit better 3D view experience and immersivity")]
        public bool FrustumSyncEnable = true;
        [Tooltip("Adjust the near plane cutoff position to your eyes, in order to get rid of the unconfortable view experience when game object might poping out too much")]
        public float NearClamp = 0.1f;  // in unity3d meters
        [Tooltip("Bypass switch 3d screen off when exit, set when switching between 3d scenes to avoid flick")]
        public bool ExitBypass2DSwitch = false;
        [Tooltip("Add Gray screen while switch between 3D/2D")]
        public bool ClearScreenWhileSwitch = false;
        [Tooltip("Gray sreen color")]
        public Color GrayScreenColor = Color.gray;
#if USEMOCKHMD
        [Tooltip("When MockHMD are enabled, notify related scripts if needed")]
        public UnityEvent _mockHMDEvent = new UnityEvent();
#endif
        [Tooltip("Select which scripts to copy(most time the post effects) to the new created stereo cameras at runtime")]
        public string[] ScriptsToCopy;
#if POSTPROCESSING
        [Tooltip("Assign PostProcessResources.asset here, it's under postprocessing package path")]
        public PostProcessResources postProcessResources;
#endif
#endregion
#region Enums
        public enum AutoStereoShader
        {
            Row,
            Column,
            Checkerboard,   //share shader pass 1
            Slant,          //xy weaving no LDC
            SlantWithLDC,   //xy weaving with LDC correction, typically work on big screens
            Slant9Views,     //xy weaving with LDC and duplite views
            SlantWithXYZ,   //xyz weaving with LDC
            Anaglyph,   //live switch from mono to Anaglyph rendering once eye tracking is success
            SideBySideDepth,   //Side by side with Depth, for multiview
            SideBySideDepthBH,   //Side by side with Depth, for multiview & tiles
            SideBySide,    //Side by side, support auto-switch to SideBySideDepth for model rendering / to TopBottom for video playing
            TopBottom,  //Top Bottom, for multiview
            D3DSTEREO,    //to do
            MultiView,  //5 Views to 45 views without eyetracking
            Mono    //force one camera rendering
        }
#endregion
#region Public properties for programming     
        public static bool enableDebugging = false;  //enable it to print selected debugging messages, toggle by pressing: Alt+D
        public static bool enableDebugMenu = false;  //while in debugging mode, toggle on screen debug infor, toggle by pressing: D  
        public static bool mockHMDDetected = false;
        public static PlatformInformation platformInformation;
        public static bool allow3DScreenDetection = true; //switch player window to the as3d screen if multiple screens are using, registered EDID friendly name to supportedEDID is required
        public static bool allowExpandScreenSleep = false; //longer preview time: about 10s
        public static float screenSizeInInch;   //physical screen diagno size
        public static float width_m;    //physical screen width in meters
        public static float height_m;   //physical screen width in meters
        public static Dictionary<string, Dictionary<string, string>> sysConfig;
        public static float focalDistance;    //meter 0.4m for 16inch 0.6m for 27inch 0.8m for 32 inch 1.6m for 65inch
        public static float[] EyeTrackFOV = { 68f, 41.5f };   //-15,+10 for tobii
        public static Vector4 EyeTrackFOVBias = Vector4.zero;
        public static string detectedEDID = ""; //only run on registered EDID 3D screen can use the auto switch feature
        public static float screenAspectRatio = 16.0f / 9;
        public static float scaleFactor;  //virtual objects are always larger, might need constantly update inside one scene
        public static float stereoBasis = 0.065f;   //m
        public static bool USESTEREOBASIS = true;  //use user setted value for IPD
        public static bool IGNOREHEADROTATION = false;  //ignore head yaw to draw frustum
        public static float stereoScaleFactor = 1; //can be used to simulate a relatively larger IPD when view longer distance object
        public static float fovScaleFactor = 1; //to temporally adjust fov of the stereo cameras for some zoom in/out effect.
        public static float safeDisparity; // in mm, just for reference for scene arrangement
        public static int doubleWidthFlag = 1;    //screen with side by side full width resolution, eg.5120/1440
        public static bool IgnoreDuplicate2DSwitch = false; //set true when minimizing to avoid duplicate switch 2D
        [HideInInspector]
        public float curHeadMoveCompensationParam;
        [HideInInspector]
        public Camera[] subCams = new Camera[2];
        [HideInInspector]
        public GameObject StereoRigs;
        [HideInInspector]
        public bool HoldBeforeSwitch3DOn = false;//disable 3d screen switch in screen sync
        [HideInInspector]
        public int screenCapType = 0;   //n>0: capture with scale 1/n
        [HideInInspector]
        public Camera thisCam;
        [HideInInspector]
        public EyeTracking et;
        [HideInInspector]
        public int _antiAliasing = 1;
        [HideInInspector]
        public float SmoothedHeadTilt = 0;  //use for playing media as tilt follow
        [HideInInspector]
        public float NoneSmoothedHeadTilt = 0;
        [HideInInspector]
        public EyeTracking.TrackingMethod eyeTrackingMethod = EyeTracking.TrackingMethod.EEServerThread;
        [HideInInspector]
        public AutoStereoShader usedShader = AutoStereoShader.Mono;
        [HideInInspector]
        public EyeTracking.TrackingDevice trackingDevice = EyeTracking.TrackingDevice.Tobii;
        [HideInInspector]
        public string IpAddress { get; set; } = "127.0.0.1"; //where eye tracking server located
        [HideInInspector]
        public int Port { get { return port; } set { port = Mathf.Clamp(value, 0, 65535); } }//and it's socket,eg.9647,6143,25938
#endregion
#region Getters&Setters
        [HideInInspector]
        public float HeadMoveCompensationParam
        {
            set
            {
                headMoveCompensationParam = Mathf.Clamp(value, 0, 5);
            }
            get { return headMoveCompensationParam; }
        }
        /// <summary>
        /// Stereobasis dynamically adjusted all the time
        /// </summary>
        public float StereoStrength
        {
            set
            {
                stereoStrength = Mathf.Clamp(value, 0, 1);
                if (et != null)
                {
                    if (FrustumSyncEnable)
                        RefreshEye(et.TP.left, et.TP.right);
                    else
                        RefreshEye(EyeLeftDefault, EyeRightDefault);
                }
            }
            get { return stereoStrength; }
        }
        /// <summary>
        /// A convenient way to adjust depth at the runtime
        /// </summary>
        public float ZOffset
        {
            set
            {
                zOffset = Mathf.Clamp(value, -0.2f, 0.2f);
                if (StereoRigs != null)
                    StereoRigs.transform.localPosition = new Vector3(0, 0, zOffset * focalDistance);
            }
            get { return zOffset; }
        }
        public float NearClipPlaneAdj
        {
            set
            {
                float nearClipPlaneAdjTemp = Mathf.Clamp(value, -100f, 100f);
                float res = thisCam.nearClipPlane + nearClipPlaneAdjTemp;

                if (res > (NearClamp / 10) && res < (3 * NearClamp) && (thisCam.transform.localPosition.z + res < (-0.1f)))  //keep osd
                {
                    thisCam.nearClipPlane = res;
                    if (!mockHMDDetected)
                    {
                        subCams[0].nearClipPlane = thisCam.nearClipPlane;
                        subCams[1].nearClipPlane = thisCam.nearClipPlane;
                    }
                    nearClipPlaneAdj = nearClipPlaneAdjTemp;
                    if (et == null || (et != null && et.OnOffET == 1 && !FrustumSyncEnable))
                    {
                        RefreshEye(EyeLeftDefault, EyeRightDefault);
                    }
                }
            }
            get { return nearClipPlaneAdj; }
        }
#endregion
#region Private Variables
        private static bool ExitBypass2DSwitchResume = false;    //Enable ExitBypass2DSwitch will set this true in order to "remember" to start new scene in Stereo 3D other in 2D
        private static Vector3 viewDefault;
        private static string[] supportedEDID = { "LUPUS", "65SRZ2D", "27 3D", "BOE DEMO", "DIM7791", "LQ156D1JX01B", "HDMI(2.0)" };
        private static float pixelsize_m;
        private static bool screenWeaving = false;
        private GameObject DebugPanelPref;   //Use a debug infor panel on UGUI
        private List<WindowHandler.DisplayInfo> allDisplays = new List<WindowHandler.DisplayInfo>();
        private List<MyMonitor> allScreens = new List<MyMonitor>();
        private bool EyeTrackingEnable = true;
        private float stereoStrengthAni = 0.01f;     //0.01-1
        private float CompensationStep = 2.4f;
#if !UNITY_EDITOR
        private int unityDisplayIdx = 0;    //active unity display index in multiscreen mode
#endif
        private int winDisplayIdx = 0;  //active windows display index for 3D screen
        private bool isScreenSideBySide = false;
        private Transform DebugPanel;
        private Text debugFPS;
        private Text debugStatus;
        private Text debugEESVRRaw;
        private StringBuilder debugString = new StringBuilder(100);
        private uint K3DepthControl = 16;
        private bool LRInvertFlag = false;
        private int port;   //9647,6143,25938 for eeserver;
        private float stereoStrength = 1f;
        private int stereoDirection = 0;
        private int stereoDirectionForceNeg = 0;
        private float durationCounter = 0;
        private Vector3 lastPos;
        private float zOffset = 0;
        private float nearClipPlaneAdj = 0;
        private float nearClipPlaneDefault = 0.1f;
        private float headMoveCompensationParam = 0.5f; //0-5  fastest eyetracking module: 0;
        private float trackingNear = 0.3f;
        private float trackingFar = 1.4f;
        private bool ForceMono = false;
        private GameObject DebugIcon;
        private uint clientType = 2;    //type for general
        private GameObject[] cameras = new GameObject[2];
        private Vector3 EyeLeftDefault;
        private Vector3 EyeRightDefault;
        private Vector3 targetLeftEyePosition;
        private Vector3 targetRightEyePosition;
        private Vector3 targetCenterEyePosition;
        private Vector3 curLeftEyePosition; //in mm unity world
        private Vector3 curRightEyePosition;
        private CameraClearFlags thisCamCF;
        private Color thisCamBGColor;
        private int thisCamCM;
#if UNITY_HDRP
        private UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode thiCamCCM;
        private int thisCamLM;
#endif
        private int FPS { get; set; }
        private bool EyeTrackingSwitch = false;
#if USEMOCKHMD
        private XRGeneralSettings xrSettings;
        private float XRResolutionScaleFactor = 1.4f;   // scale from 1512:1680 (oculus rift s) to 1920x2160 2160/1680 = 1.2857 however, 1.4 is used to fit the viewport size
        private void _mockHMDNotifier(){_mockHMDEvent.Invoke();}
#endif
#if DISABLEAUTOPLAY
        private const string regPathUSBAutoPlay = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers";
        private const string regNameUSBAutoPlay = "DisableAutoplay";
#endif
#endregion
#region Methods
        /// <summary>
        /// As long as eesvr can not report acurate switch time, the timeout can be vary
        /// around 300ms-2s for callback, but should be much earlier has the effect done.
        /// <param name="status">3D On /Off status</param>
        /// <param name="hardWait">block the thread to wait</param>
        /// <param name="callback">register other process</param>
        /// <param name="ClearScreenWhileSwitch">Use a Gray Screen to avoid flickering during 3d/2d switching</param>
        /// <returns></returns>
        public IEnumerator syncWaitK3Switch(uint status, bool hardWait = false, Action callback = null, bool ClearScreenWhileSwitch = true, int externalDelay = 2)
        {
            //link curHeadMoveCompensationParam to TIMEOUT
            //Compen : TIMEOUT2D / TIMEOUT3D
            //0.5 : 4/5
            //0: bypass
            //5: 22/25  //normal pc
            //5: 130/140    //N100 pc
            const float CLEARSCREENTHRESHOLD = 0.5f;
            int TIMEOUT2D = Mathf.FloorToInt(28 * curHeadMoveCompensationParam - 10);
            int TIMEOUT3D = Mathf.FloorToInt(30 * curHeadMoveCompensationParam - 10);
            if (TIMEOUT2D < 4)
                TIMEOUT2D = 4;
            if (TIMEOUT3D < 5)
                TIMEOUT3D = 5;
            if (ClearScreenWhileSwitch && curHeadMoveCompensationParam >= CLEARSCREENTHRESHOLD && !et.LeftViewOnly)
            {
                ClearMainScreenUsingOldCamStart();
                for (int i = 0; i < externalDelay; i++)
                {
                    yield return new WaitForEndOfFrame();   //todo: move this to syncWaitK3Switch and use gray screen
                }
            }
            if (status == (uint)EyeTracking.OnOff.OFF)   //to 2d
            {
                int timeout = TIMEOUT2D;    //40 , about 300ms on general pc/1400ms on N100 cpu, however, real effect happen on about 40ms,about 1/8 of the duration.
                while (et != null && et.OnOff3D != status && timeout > 0)
                {
                    timeout--;
                    if (hardWait)
                        Thread.Sleep(10);
                    else
                        yield return new WaitForSeconds(0.01f);

                }
                if (enableDebugging)
                    Debug.Log("...wait switch 2D done. Delay:" + (TIMEOUT2D - timeout) * 10 + "ms");
            }
            else //to 3d
            {
                int timeout = TIMEOUT3D;   //50, about 330ms on general pc/1800ms on N100 cpu, however, real effect happen on about 50ms
                while (et != null && et.OnOff3D != status && timeout > 0)
                {
                    timeout--;
                    if (hardWait)
                        Thread.Sleep(10);
                    else
                        yield return new WaitForSeconds(0.01f);
                }
                if (enableDebugging)
                    Debug.Log("...wait switch 3D done. Delay:" + (TIMEOUT3D - timeout) * 10 + "ms");
            }
            if (ClearScreenWhileSwitch && curHeadMoveCompensationParam >= CLEARSCREENTHRESHOLD && !et.LeftViewOnly)
            {
                ClearMainScreenUsingOldCamEnd();
            }
            if (callback != null)
                callback();
        }
        public void SetScreenSBSImmediate()
        {
            SetScreenSBS();
            if (!mockHMDDetected)
            {
                SafeRenderCamera(subCams[0]);
                SafeRenderCamera(subCams[1]);
            }
        }
        public void SetScreenMonoImmediate()
        {
            SetScreenMono();
            if (!mockHMDDetected)
                SafeRenderCamera(subCams[0]);
        }
        public void SetScreenSBS()
        {
#if DISABLEAUTOPLAY
            Registry.SetValue(regPathUSBAutoPlay, regNameUSBAutoPlay, 1);   //usb autoplay
#endif
            if (mockHMDDetected)
            {
                XRSettings.gameViewRenderMode = GameViewRenderMode.BothEyes;
            }
            else
            {
                for (int i = 0; i < cameras.Length; i++)
                {
                    subCams[i].targetTexture = null;
                }
                subCams[0].rect = new Rect(0, 0, 0.5f, 1);
                subCams[1].rect = new Rect(0.5f, 0, 0.5f, 1);
                subCams[1].cullingMask = -1;
                subCams[0].enabled = true;
                subCams[1].enabled = true;
                thisCam.enabled = false;
                thisCam.cullingMask = 0;
                thisCam.clearFlags = CameraClearFlags.Nothing;
                thisCam.depthTextureMode = DepthTextureMode.None;
                thisCam.tag = "Untagged";
                cameras[0].tag = "MainCamera";
                cameras[1].tag = "Untagged";
            }
            RefreshEye(curLeftEyePosition, curRightEyePosition);
            isScreenSideBySide = true;
            if (enableDebugging)
                print("SetScreenSBS");
        }
        public void SetScreenMono()
        {
#if DISABLEAUTOPLAY
            Registry.SetValue(regPathUSBAutoPlay, regNameUSBAutoPlay, 0);   //usb autoplay
#endif
            if (mockHMDDetected)
            {
                XRSettings.gameViewRenderMode = GameViewRenderMode.LeftEye; //the image is scaled in single eye mode
            }
            else
            {
                subCams[0].targetTexture = null;
                subCams[0].rect = new Rect(0, 0, 1, 1);
                subCams[0].enabled = true;
                subCams[1].enabled = false;
                thisCam.enabled = false;
                thisCam.cullingMask = 0;
                thisCam.clearFlags = CameraClearFlags.Nothing;
                thisCam.depthTextureMode = DepthTextureMode.None;
                thisCam.tag = "Untagged";
                cameras[0].tag = "MainCamera";
                cameras[1].tag = "Untagged";
            }
            RefreshEye(curLeftEyePosition, curRightEyePosition);
            isScreenSideBySide = false;
            if (enableDebugging)
                print("SetScreenMono");
        }
        public void SetScreen()
        {
            switch (usedShader)
            {
                case AutoStereoShader.SideBySide:
                    SetScreenSBS();
                    break;
                case AutoStereoShader.Mono:
                    SetScreenMono();
                    break;
                default:
                    break;
            }
        }
        public void ClearMainScreen(bool recover)
        {
            Color bkColor = subCams[0].backgroundColor;
            int mask = subCams[0].cullingMask;
            subCams[0].backgroundColor = GrayScreenColor;
            subCams[0].cullingMask = 0;
            subCams[1].backgroundColor = GrayScreenColor;
            subCams[1].cullingMask = 0;
            SafeRenderCamera(subCams[0]);
            SafeRenderCamera(subCams[1]);
            if (recover)
            {
                subCams[0].backgroundColor = bkColor;
                subCams[0].cullingMask = mask;
                subCams[1].backgroundColor = bkColor;
                subCams[1].cullingMask = mask;
            }
        }
        public void ClearMainScreenUsingOldCamStart()
        {
#if USEMOCKHMD
#if UNITY_HDRP
            thisCamCCM = thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().clearColorMode;
            thisCamBGColor = thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().backgroundColorHDR;
            thisCamLM = thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().volumeLayerMask;
            thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
            thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().backgroundColorHDR = GrayScreenColor;
            thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().volumeLayerMask = 0;
#else
            thisCamCF = thisCam.clearFlags;
            thisCamBGColor = thisCam.backgroundColor;
            thisCamCM = thisCam.cullingMask;
            thisCam.clearFlags = CameraClearFlags.SolidColor;
            thisCam.backgroundColor = GrayScreenColor;
            thisCam.cullingMask = 0;
#endif
#else
#if UNITY_HDRP
            thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
            thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().backgroundColorHDR = GrayScreenColor;
            thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().volumeLayerMask = 0;
#else
            thisCam.clearFlags = CameraClearFlags.SolidColor;
            thisCam.backgroundColor = GrayScreenColor;
#endif
            thisCam.enabled = true;
#endif
        }
        public void ClearMainScreenUsingOldCamEnd()
        {
#if USEMOCKHMD
#if UNITY_HDRP
            thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().clearColorMode = thisCamCCM;
            thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().backgroundColorHDR = thisCamBGColor;
            thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().volumeLayerMask = thisCamLM;
#else
            thisCam.clearFlags = thisCamCF;
            thisCam.backgroundColor = thisCamBGColor;
            thisCam.cullingMask = thisCamCM;
#endif
#else
            thisCam.enabled = false;
            thisCam.clearFlags = CameraClearFlags.Nothing;
#endif
        }
        public void NearClipPlaneReset()
        {
            if (thisCam)
            {
                thisCam.nearClipPlane = nearClipPlaneDefault;
                if (!mockHMDDetected)
                {
                    subCams[0].nearClipPlane = nearClipPlaneDefault;
                    subCams[1].nearClipPlane = nearClipPlaneDefault;
                }
            }
            if (et != null)
            {
                if (!FrustumSyncEnable)
                    RefreshEye(EyeLeftDefault, EyeRightDefault);
            }
        }
        private void InitDebugging()
        {
            if (!GetComponent<StereoUI>())
            {
                print("Need to attach StereUI component first");
                enableDebugging = false;
                return;
            }
            if (GetComponent<StereoUI>().Canvas3D == null)
            {
                var CanvasDebug = Resources.Load("Prefabs/Canvas3D") as GameObject;
                GetComponent<StereoUI>().Canvas3D = Instantiate(CanvasDebug, transform.parent).GetComponent<Canvas>();
            }
            if (enableDebugging)
            {
                if (DebugPanel == null)
                {
                    DebugPanelPref = Resources.Load("Prefabs/DebugPanel") as GameObject;
                    if (DebugPanelPref)
                    {
                        DebugPanel = Instantiate(DebugPanelPref, GetComponent<StereoUI>().Canvas3D.transform).transform;
                        DebugPanel.gameObject.SetActive(false);
                    }
                }
                debugFPS = DebugPanel.transform.Find("TextFPS").GetComponent<Text>();
                debugStatus = DebugPanel.transform.Find("TextStatus").GetComponent<Text>();
                debugEESVRRaw = DebugPanel.transform.Find("TextEESVRRaw").GetComponent<Text>();
                if (DebugIcon == null)
                {
                    var DebugIconPref = Resources.Load("Prefabs/DebugIcon") as GameObject;
                    if (DebugIconPref)
                        DebugIcon = Instantiate(DebugIconPref, GetComponent<StereoUI>().Canvas3D.transform);
                }
            }
            else
            {
                try
                {
                    Destroy(DebugIcon);
                    if (DebugPanel)
                        Destroy(DebugPanel.gameObject);
                }
                catch
                {
                    ;
                }
            }
        }
        private void SwitchDebugging()
        {
            enableDebugging = !enableDebugging;
            InitDebugging();
        }
        /// <summary>
        /// find connected 3D screen from edid, activate it as extend screen if has not been activated yet.
        /// </summary>
        /// <returns>winDisplayIdx : display id from win32 api</returns>
        private int ScreenDetection(bool forceActiveAll)
        {
            int winDisplayIdx = 0;
            int tryCnt = 1;
            while (tryCnt >= 0)
            {
                PathDisplayTarget[] pdt = PathDisplayTarget.GetDisplayTargets();
                if (enableDebugging)
                    Debug.Log("pdt length = " + pdt.Length);
                if (pdt.Length > 1)
                {
                    int target3DScreenIdx = -1;
                    bool targetIsValid = false;
                    bool targetIsAvailable = false;
                    var IDs = new int[pdt.Length];
                    try
                    {
                        int i = 0;
                        foreach (var pdtI in pdt)
                        {
                            var device = pdtI.ToDisplayDevice();
                            if (enableDebugging)
                                Debug.Log("displays friendly names = " + pdtI.FriendlyName + " ScreenName = " + device.ScreenName + " IsAvailable = " + device.IsAvailable + " IsValid = " + device.IsValid + " unity display size = " + Display.displays.Length);
                            string[] nameArray = Regex.Split(device.ScreenName, "DISPLAY", RegexOptions.IgnoreCase);
                            int curIdx = Convert.ToInt32(nameArray.Last());
                            if (enableDebugging)
                                Debug.Log("displays curIdx= " + curIdx);
                            //not always the first! not continous! eg 10(vorpx virtual display),12(3DScreen),13(2d); 4(3DScreen), 2(2d), 1(vorpx)
                            //set display screen to the as3d screen, match with unity display to see if need an inverse.
                            //occationally the EDID imformation can be lost, and friendlyName = "", if the display edid read failure by the op system
                            IDs[i] = curIdx;
                            if (supportedEDID.Contains(pdtI.FriendlyName))
                            {
                                detectedEDID = pdtI.FriendlyName;
                                target3DScreenIdx = curIdx;
                                if (enableDebugging)
                                    Debug.Log("target3DScreenIdx = " + target3DScreenIdx);
                                targetIsValid = device.IsValid;
                                targetIsAvailable = device.IsAvailable;
                            }
                            i++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("display infor get failure, EDID and monior index enum failure if monitor forbided: " + ex);
                    }
                    Array.Sort(IDs);
                    winDisplayIdx = Array.IndexOf(IDs, target3DScreenIdx);
                    if (winDisplayIdx < 0)
                    {
                        Debug.Log("3DScreen not found.");
                        winDisplayIdx = 0;
                    }
                    if (tryCnt == 0)
                        break;
                    if ((forceActiveAll || (targetIsValid && !targetIsAvailable)) && detectedEDID != "")
                    {
                        if (enableDebugging)
                            Debug.Log("force activate 3DScreen " + winDisplayIdx + "target3DScreenIdx = " + target3DScreenIdx); //Display not refreshed by unity                        
                        PathInfo.ApplyTopology(DisplayConfigTopologyId.Extend, true);
                        Thread.Sleep(5000);
                        tryCnt--;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (pdt.Length == 1)
                        detectedEDID = pdt[0].FriendlyName;
                    break;
                }
            }
            return winDisplayIdx;
        }
        public int CheckMultiScreen()
        {
            winDisplayIdx = ScreenDetection(false);
            allDisplays = GetDisplays();
            allScreens.Clear();
            for (int i = 0; i < allDisplays.Count; i++)
            {
                allScreens.Add(new MyMonitor(allDisplays[i].WorkArea.left, allDisplays[i].WorkArea.top, i, Convert.ToInt32(allDisplays[i].ScreenHeight), Convert.ToInt32(allDisplays[i].ScreenWidth), allDisplays[i].displayID, allDisplays[i].displayName));
            }
            allScreens.Sort((x, y) => x.id.CompareTo(y.id));

            if (enableDebugging)
            {
                foreach (var screen in allScreens)
                {
                    print("active " + screen.name + " width = " + screen.width + " targetX = " + screen.targetX);
                }
            }
            return allDisplays.Count;
        }
        public void SwitchScreen3D(int screenIdx)
        {
            if (screenIdx >= allScreens.Count)
            {
                Debug.LogWarning("screen count truncated, disabled some monitor? " + screenIdx + " >= " + allScreens.Count);
                screenIdx = allScreens.Count - 1;
            }
            if (enableDebugging)
                Debug.Log("SwitchScreen to " + allScreens[screenIdx].name);
            SwitchScreen(allScreens[screenIdx].height, allScreens[screenIdx].width, allScreens[screenIdx].targetX, allScreens[screenIdx].targetY);
        }
        private void ScreenSync()
        {
            if (ForceMono)
            {
                if (et.OnOff3D == (uint)EyeTracking.OnOff.ON)
                {
                    et.Switch3D(EyeTracking.OnOff.OFF);
                }
                return;
            }
            if (stereoStrengthAni > 0.1f)
            {
                if (!HoldBeforeSwitch3DOn)
                {
                    if (et != null && (et.OnOff3D == (uint)EyeTracking.OnOff.OFF))
                    {
                        if (et.Switch3D(EyeTracking.OnOff.ON) >= 0 && !ForceMono)
                        {
                            StartCoroutine(syncWaitK3Switch((uint)EyeTracking.OnOff.ON, false, SetScreenSBS, ClearScreenWhileSwitch));
                        }
                    }
                }
            }
        }
        private IEnumerator MainThreadPFNotify(EyeTracking.PlatformFeatures features)
        {
            if (et != null)
            {
                et.SwitchET(EyeTracking.OnOff.ON);
                if (et.IsK3Enabled() == 2)
                {
                    DelaySwitchTo3D();
                }
            }
            yield return null;
        }
        public void DelaySwitchTo3D()
        {
            if (enableDebugging)
                print("Init switch 3d Start " + Time.realtimeSinceStartup + " usedShader = " + usedShader);
            if (usedShader == AutoStereoShader.SideBySide)
            {
                et.Switch3D(EyeTracking.OnOff.ON);
                EyeTracking.ETConfig.preserved0 = (uint)usedShader;
                StartCoroutine(syncWaitK3Switch((uint)EyeTracking.OnOff.ON, false, SetScreenSBS, ClearScreenWhileSwitch));
            }
        }
        public void PFNotify(EyeTracking.PlatformFeatures features)
        {
            if (enableDebugging)
                Debug.LogFormat("PlatformFeatures received: K3Valid = {0} 2DZSupport = {1}", features.K3Valid, features.K3ModeLicense2DZ);
            UnityMainThreadDispatcher.Instance().Enqueue(MainThreadPFNotify(features));
            UnityMainThreadDispatcher.Instance().EnableDispather = true;
        }
        private Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
        {
            float x = 2.0F * near / (right - left);
            float y = 2.0F * near / (top - bottom);
            float a = (right + left) / (right - left);
            float b = (top + bottom) / (top - bottom);
            float c = -(far + near) / (far - near);
            float d = -(2.0F * far * near) / (far - near);
            float e = -1.0F;
            Matrix4x4 m = new Matrix4x4();
            m[0, 0] = x; m[0, 1] = 0f; m[0, 2] = a; m[0, 3] = 0f;
            m[1, 0] = 0f; m[1, 1] = y; m[1, 2] = b; m[1, 3] = 0f;
            m[2, 0] = 0f; m[2, 1] = 0f; m[2, 2] = c; m[2, 3] = d;
            m[3, 0] = 0f; m[3, 1] = 0f; m[3, 2] = e; m[3, 3] = 0f;
            return m;
        }
        private void UpdateEyeProjection(Camera.MonoOrStereoscopicEye eye, Vector3 EyePos, Vector3 pa, Vector3 pb, Vector3 pc, float NearClipPlane, float FarClipPlane)
        {
            Matrix4x4 projectionM;
            float left, right, bottom, top, eyedistance;
            Vector3 va, vb, vc;

            float ScalePatch; //weired patch that the frustum is not like what has been set for botheye rendering
            float halfWidth;
            if (mockHMDDetected)
            {
                if (XRSettings.gameViewRenderMode != GameViewRenderMode.BothEyes)
                {
                    halfWidth = 0.5f;
                    ScalePatch = XRSettings.eyeTextureResolutionScale > 0 ? XRSettings.eyeTextureResolutionScale : 1;
                }
                else
                {
                    if (eye != Camera.MonoOrStereoscopicEye.Mono)
                    {
                        ScalePatch = XRSettings.eyeTextureResolutionScale > 0 ? XRSettings.eyeTextureResolutionScale : 1;
                    }
                    else
                    {
                        ScalePatch = 1;
                    }
                    halfWidth = 1;
                }
            }
            else
            {
                halfWidth = 1;
                ScalePatch = 1;
            }
            Vector3 paPatched = new Vector3(pa.x, pa.y / halfWidth, 0) * ScalePatch;
            Vector3 pbPatched = new Vector3(pb.x, pb.y / halfWidth, 0) * ScalePatch;
            Vector3 pcPatched = new Vector3(pc.x, pc.y / halfWidth, 0) * ScalePatch;
            va = paPatched - EyePos;
            vb = pbPatched - EyePos;
            vc = pcPatched - EyePos;
            eyedistance = -(Vector3.Dot(va, Vector3.forward));
            left = (Vector3.Dot(Vector3.right, va) * NearClipPlane) / eyedistance;
            right = (Vector3.Dot(Vector3.right, vb) * NearClipPlane) / eyedistance;
            bottom = (Vector3.Dot(Vector3.up, va) * NearClipPlane) / eyedistance;
            top = (Vector3.Dot(Vector3.up, vc) * NearClipPlane) / eyedistance;
            projectionM = PerspectiveOffCenter(left, right, bottom, top, NearClipPlane, FarClipPlane);
            if (!mockHMDDetected)
            {
                if (eye == Camera.MonoOrStereoscopicEye.Left)
                    subCams[0].projectionMatrix = projectionM;
                else if (eye == Camera.MonoOrStereoscopicEye.Right)
                    subCams[1].projectionMatrix = projectionM;
                else //Mono
                    thisCam.projectionMatrix = projectionM;
            }
            else
            {
                if (eye == Camera.MonoOrStereoscopicEye.Left)
                    thisCam.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, projectionM);
                else if (eye == Camera.MonoOrStereoscopicEye.Right)
                    thisCam.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, projectionM);
                else //Mono
                    thisCam.projectionMatrix = projectionM;
            }
        }
        private void RefreshEye(Vector3 EyeLeft, Vector3 EyeRight)
        {
            if (IGNOREHEADROTATION)
            {
                EyeLeft.z = EyeRight.z = (EyeLeft.z + EyeRight.z) / 2;  //ignore variant on z
            }
            Vector3 facePos = (EyeLeft + EyeRight) / 2;
            float dist = (EyeLeft - EyeRight).magnitude;

            if (USESTEREOBASIS)
            {
                EyeLeft = (EyeLeft - facePos) / dist * stereoBasis * stereoScaleFactor * stereoStrength * stereoStrengthAni + facePos;
                EyeRight = (EyeRight - facePos) / dist * stereoBasis * stereoScaleFactor * stereoStrength * stereoStrengthAni + facePos;
            }
            else
            {
                EyeLeft = (EyeLeft - facePos) * stereoScaleFactor * stereoStrength * stereoStrengthAni + facePos;
                EyeRight = (EyeRight - facePos) * stereoScaleFactor * stereoStrength * stereoStrengthAni + facePos;
            }
            dist = (EyeLeft - EyeRight).magnitude;
            float NearClipPlane = thisCam.nearClipPlane;
            float FarClipPlane = thisCam.farClipPlane;

            float marging = Mathf.Abs(thisCam.transform.localPosition.z) - 0.1f;
            if (marging < thisCam.nearClipPlane)
            {
                thisCam.nearClipPlane = NearClipPlane = marging;
                if (!mockHMDDetected)
                {
                    subCams[0].nearClipPlane = NearClipPlane;
                    subCams[1].nearClipPlane = NearClipPlane;
                }
            }
            Vector3 pa = new Vector3(-width_m / 2.0f, -height_m / 2.0f, 0) * fovScaleFactor;
            Vector3 pb = new Vector3(width_m / 2.0f, -height_m / 2.0f, 0) * fovScaleFactor;
            Vector3 pc = new Vector3(-width_m / 2.0f, height_m / 2.0f, 0) * fovScaleFactor;
            UpdateEyeProjection(Camera.MonoOrStereoscopicEye.Left, EyeLeft, pa, pb, pc, NearClipPlane, FarClipPlane);
            UpdateEyeProjection(Camera.MonoOrStereoscopicEye.Right, EyeRight, pa, pb, pc, NearClipPlane, FarClipPlane);
            UpdateEyeProjection(Camera.MonoOrStereoscopicEye.Mono, facePos, pa, pb, pc, NearClipPlane, FarClipPlane);
            //to left hand coordinate system
            thisCam.stereoConvergence = facePos.z;
            thisCam.stereoSeparation = dist;
            facePos.z = -facePos.z;
            EyeLeft.z = -EyeLeft.z;
            EyeRight.z = -EyeRight.z;
            transform.localPosition = new Vector3(facePos.x, facePos.y, facePos.z);
            if (mockHMDDetected)
            {
                Vector3 transL = facePos - EyeLeft;
                Vector3 transR = facePos - EyeRight;
                Matrix4x4 originalView = thisCam.worldToCameraMatrix;
                Matrix4x4 eyeTranslationLeft = Matrix4x4.Translate(transL);
                Matrix4x4 eyeTranslationRight = Matrix4x4.Translate(transR);
                Matrix4x4 leftView = eyeTranslationLeft * originalView;
                Matrix4x4 rightView = eyeTranslationRight * originalView;
                thisCam.SetStereoViewMatrix(Camera.StereoscopicEye.Left, leftView);
                thisCam.SetStereoViewMatrix(Camera.StereoscopicEye.Right, rightView);
            }
            else
            {
                cameras[0].GetComponent<Transform>().localPosition = EyeLeft - facePos;
                cameras[1].GetComponent<Transform>().localPosition = EyeRight - facePos;
            }
        }
        public void CheckAndRefreshScreens()
        {
            RefreshEye(curLeftEyePosition, curRightEyePosition);
        }
        private static Vector3 GetNearPlane(float stereoBasis, float focalDis, float fov, float aspect, float windowWidth)
        {
            float D_ = -safeDisparity;//safe disparity range on screen in mm
            float W_ = windowWidth;//screen width in mm
            float w = ((Mathf.Tan((fov / 2) * Mathf.Deg2Rad) * focalDis)) * aspect;
            //z=ef/(e-dis); dis=D'/W'2tan(fov/2)*f*aspect; z=f/(1-2*D'/W'*f/e*tan(fov/2)*aspect)
            float z = focalDis / (1 - 2 * D_ / W_ * w / stereoBasis);
            //z=-k2x+d; where k2=f/(e+w) d=fw/(e+w); set the axis base at the center of zero parallax plane
            float n = (focalDis - z) / focalDis * 10;   //measured with 1/10f
            float width = ((10 - n) * w / 10 - stereoBasis / 2 * n / 10) * 2;
            float height = width / aspect;
            return new Vector3(width, height, z);
        }
        private static Vector3 GetFarPlane(float stereoBasis, float focalDis, float fov, float aspect, float windowWidth)
        {
            float D_ = safeDisparity;
            float W_ = windowWidth;
            float w = ((Mathf.Tan((fov / 2) * Mathf.Deg2Rad) * focalDis)) * aspect;
            float temp = 1 - 2 * D_ / W_ * w / stereoBasis;
            float z = focalDis / (temp < 0 ? 0.001f : temp);
            float n = (focalDis - z) / focalDis * 10;
            float width = ((10 - n) * w / 10 - stereoBasis / 2 * n / 10) * 2;
            float height = width / aspect;
            return new Vector3(width, height, z);
        }
        private T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }
        /// <summary>
        /// screen capture to SBS image(half/full width) in 8k
        /// </summary>
        private int screenCapture()
        {
            const int SCREENCAPSIZEW = 1280 * 6;
            const int SCREENCAPSIZEH = 720 * 6;
            int result = -2;
            Texture2D outputTex;
            RenderTexture RenderTextureL, RenderTextureR;
            if (!mockHMDDetected)
            {
                Rect cameraRectBkL = subCams[0].rect;
                Rect cameraRectBkR = subCams[1].rect;
                if (usedShader == AutoStereoShader.Mono)
                {
                    RenderTextureL = new RenderTexture(SCREENCAPSIZEW / 2, SCREENCAPSIZEH / 2, 0, RenderTextureFormat.Default);
                    RenderTextureL.autoGenerateMips = false;
                    RenderTextureL.filterMode = FilterMode.Bilinear;
                    RenderTextureL.antiAliasing = _antiAliasing;
                    subCams[0].targetTexture = RenderTextureL;
                    SafeRenderCamera(subCams[0]);
                    RenderTexture.active = RenderTextureL;
                    outputTex = new Texture2D(SCREENCAPSIZEW / 2, SCREENCAPSIZEH / 2, TextureFormat.RGB24, false, false);
                    outputTex.ReadPixels(new Rect(0, 0, SCREENCAPSIZEW / 2, SCREENCAPSIZEH / 2), 0, 0, false);
                    outputTex.Apply();
                }
                else if (usedShader != AutoStereoShader.SideBySideDepth && usedShader != AutoStereoShader.SideBySideDepthBH)
                {
                    RenderTextureL = new RenderTexture(SCREENCAPSIZEW / 2 / screenCapType, SCREENCAPSIZEH / screenCapType, 0, RenderTextureFormat.Default);
                    RenderTextureR = new RenderTexture(SCREENCAPSIZEW / 2 / screenCapType, SCREENCAPSIZEH / screenCapType, 0, RenderTextureFormat.Default);
                    RenderTextureL.autoGenerateMips = false;
                    RenderTextureL.filterMode = FilterMode.Bilinear;
                    RenderTextureL.antiAliasing = _antiAliasing;
                    RenderTextureR.autoGenerateMips = false;
                    RenderTextureR.filterMode = FilterMode.Bilinear;
                    RenderTextureR.antiAliasing = _antiAliasing;
                    subCams[0].targetTexture = RenderTextureL;
                    subCams[0].rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
                    subCams[1].targetTexture = RenderTextureR;
                    subCams[1].rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
                    SafeRenderCamera(subCams[0]);
                    if (FrustumSyncEnable)
                    {
                        curRightEyePosition.y = curLeftEyePosition.y;
                        RefreshEye(curLeftEyePosition, curRightEyePosition);
                    }
                    SafeRenderCamera(subCams[1]);
                    RenderTexture CombinedTextureTemp = new RenderTexture(SCREENCAPSIZEW / screenCapType, SCREENCAPSIZEH / screenCapType, 0, RenderTextureFormat.Default);
                    CombinedTextureTemp.autoGenerateMips = false;
                    CombinedTextureTemp.filterMode = FilterMode.Bilinear;
                    CombinedTextureTemp.antiAliasing = _antiAliasing;
                    RenderTexture currentActiveRTTemp = RenderTexture.active;
                    Graphics.CopyTexture(RenderTextureL, 0, 0, 0, 0, SCREENCAPSIZEW / 2 / screenCapType, SCREENCAPSIZEH / screenCapType, CombinedTextureTemp, 0, 0, 0, 0);
                    Graphics.CopyTexture(RenderTextureR, 0, 0, 0, 0, SCREENCAPSIZEW / 2 / screenCapType, SCREENCAPSIZEH / screenCapType, CombinedTextureTemp, 0, 0, SCREENCAPSIZEW / 2 / screenCapType, 0);
                    RenderTexture.active = CombinedTextureTemp;
                    outputTex = new Texture2D(SCREENCAPSIZEW / screenCapType, SCREENCAPSIZEH / screenCapType, TextureFormat.RGB24, false, false);
                    outputTex.ReadPixels(new Rect(0, 0, SCREENCAPSIZEW / screenCapType, SCREENCAPSIZEH / screenCapType), 0, 0, false);
                    outputTex.Apply();
                    RenderTexture.active = currentActiveRTTemp;
                    RenderTextureL.Release();
                    RenderTextureR.Release();
                    CombinedTextureTemp.Release();
                }
                else
                {
                    outputTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false, false);
                    outputTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                    outputTex.Apply();
                }

                if (usedShader != AutoStereoShader.SideBySideDepth && usedShader != AutoStereoShader.SideBySideDepthBH)
                {
                    subCams[0].rect = cameraRectBkL;
                    subCams[1].rect = cameraRectBkR;
                    if (!(usedShader != AutoStereoShader.Mono && usedShader != AutoStereoShader.SideBySide && usedShader != AutoStereoShader.TopBottom))
                    {
                        subCams[0].targetTexture = null;
                        subCams[1].targetTexture = null;
                    }
                }

                byte[] imagebytes = outputTex.EncodeToJPG();
                if (imagebytes.Length > 0)
                {
                    try
                    {
                        string savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                        File.WriteAllBytes(Path.Combine(savePath, "Exported.jpg"), imagebytes);
                        result = 0;
                    }
                    catch
                    {
                        try
                        {
                            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Exported.jpg");
                            File.WriteAllBytes(savePath, imagebytes);
                            result = -1;
                        }
                        catch (Exception e)
                        {
                            Debug.Log("file Write denied " + e);
                        }
                    }
                    imagebytes = null;
                }
            }
            else
            {
                string savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                ScreenCapture.CaptureScreenshot(Path.Combine(savePath, "Exported.jpg"), ScreenCapture.StereoScreenCaptureMode.BothEyes);
                result = 0;
            }
            return result;
        }
        public void initScreen()
        {
            CultureInfo culture = new CultureInfo("en-US");
            try
            {
                AutoStereoShader lastShader = usedShader;
                usedShader = (AutoStereoShader)Enum.Parse(typeof(AutoStereoShader), sysConfig["SCREEN"]["StereoMode"]);
                platformInformation = new PlatformInformation();
                platformInformation.InitPlatformInformation(usedShader, float.Parse(sysConfig["SCREEN"]["SIZEInch"], culture));
                screenSizeInInch = platformInformation.screenSizeInInch;
                screenAspectRatio = platformInformation.screenAspectRatio;
                doubleWidthFlag = platformInformation.doubleWidthFlag;
                eyeTrackingMethod = (EyeTracking.TrackingMethod)Enum.Parse(typeof(EyeTracking.TrackingMethod), sysConfig["ET"]["TrackingMethod"]);
                trackingDevice = (EyeTracking.TrackingDevice)Enum.Parse(typeof(EyeTracking.TrackingDevice), sysConfig["ET"]["TrackingDevice"]);
                scaleFactor = VirtualScreenWidth / (Mathf.Sin(Mathf.Atan(screenAspectRatio / doubleWidthFlag)) * screenSizeInInch * 0.0254f);
                IpAddress = sysConfig["ET"]["IP"];
                Port = int.Parse(sysConfig["ET"]["PORT"], culture);
                EyeTrackFOV[0] = float.Parse(sysConfig["ET"]["EyeTrackFOV"].Split(',')[0], culture);
                EyeTrackFOV[1] = float.Parse(sysConfig["ET"]["EyeTrackFOV"].Split(',')[1], culture);
                curHeadMoveCompensationParam = HeadMoveCompensationParam = float.Parse(sysConfig["ET"]["CompensationParam"], culture);
                USESTEREOBASIS = Convert.ToBoolean(int.Parse(sysConfig["INTERNAL"]["USESTEREOBASIS"]));
                if (USESTEREOBASIS)
                {
                    stereoBasis = scaleFactor * float.Parse(sysConfig["INTERNAL"]["STEREOBASISS"], culture);
                }
                IGNOREHEADROTATION = Convert.ToBoolean(int.Parse(sysConfig["INTERNAL"]["IGNOREHEADROTATION"]));
                viewDefault = new Vector3(float.Parse(sysConfig["SCREEN"]["ViewPosition"].Split(',')[0], culture),
                    float.Parse(sysConfig["SCREEN"]["ViewPosition"].Split(',')[1], culture),
                    float.Parse(sysConfig["SCREEN"]["ViewPosition"].Split(',')[2])) * scaleFactor;
                focalDistance = viewDefault.z;
                trackingNear = float.Parse(sysConfig["ET"]["trackingNear"], culture);
                trackingFar = float.Parse(sysConfig["ET"]["trackingFar"], culture);
                if (usedShader == AutoStereoShader.Mono)
                {
                    ForceMono = true;
                }
                else
                {
                    ForceMono = false;
                }
            }
            catch
            {
                usedShader = AutoStereoShader.Mono;
                eyeTrackingMethod = EyeTracking.TrackingMethod.None;
                trackingDevice = EyeTracking.TrackingDevice.Tobii;
                Debug.Log("warning: config.xml fields error");
            }
            pixelsize_m = platformInformation.pixelsizeScreen_mm * scaleFactor / 1000;
            int w1, h1;
#if UNITY_EDITOR
            w1 = Screen.width;
            h1 = Screen.height;
#else
            w1 = Screen.currentResolution.width;
            h1 = Screen.currentResolution.height;
#endif
            width_m = w1 / doubleWidthFlag * pixelsize_m;
            height_m = h1 * pixelsize_m;
        }
        public static Dictionary<string, Dictionary<string, string>> InitSysconfig()
        {
            sysConfig = new Dictionary<string, Dictionary<string, string>>();
            XmlDocument xmlDoc = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(Application.streamingAssetsPath + "/Config.xml"))
            {
                xmlDoc.Load(reader);
            }
            string[] tags = { "SCREEN", "ET", "INTERNAL" };
            for (int i = 0; i < tags.Length; i++)
            {
                sysConfig.Add(tags[i], new Dictionary<string, string>());
                XmlNodeList configs = xmlDoc["Data"].GetElementsByTagName(tags[i]);
                for (int j = 0; j < configs.Count; j++)
                {
                    sysConfig[tags[i]].Add(configs[j].Attributes["Key"].Value, configs[j].Attributes["Word"].Value);
                }
            }
            return sysConfig;
        }
        private void EyeDataExtractor()
        {
            if (FrustumSyncEnable && !HoldBeforeSwitch3DOn && eyeTrackingMethod != 0 && et != null && et.OnOffET == 1)
            {
                bool remindCondition = false;
                if (et.TP.status == EyeTracking.TrackingStatus.Normal && et.TP.updateFlag)
                {
                    float dt = Time.deltaTime;
                    targetLeftEyePosition = et.TP.left;
                    targetRightEyePosition = et.TP.right;
                    NoneSmoothedHeadTilt = et.TP.rollFitted;
                    SmoothedHeadTilt = Mathf.Lerp(SmoothedHeadTilt, NoneSmoothedHeadTilt, dt);
                    if (curHeadMoveCompensationParam == 0)
                    {
                        curLeftEyePosition = targetLeftEyePosition;
                        curRightEyePosition = targetRightEyePosition;
                    }
                    else
                    {
                        float d = Mathf.Abs(Vector3.Distance((curLeftEyePosition + curRightEyePosition) / 2, (targetLeftEyePosition + targetRightEyePosition) / 2));
                        float margin = scaleFactor * (0.004f + curHeadMoveCompensationParam / 500); //0.004f : resolution = 4mm
                        if (d > margin)
                        {
                            //dynamic step according to delta distance
                            float movingStep = dt * (6 - curHeadMoveCompensationParam) * 4 / CompensationStep;
                            curLeftEyePosition.x = Mathf.Lerp(curLeftEyePosition.x, targetLeftEyePosition.x, movingStep);
                            curRightEyePosition.x = Mathf.Lerp(curRightEyePosition.x, targetRightEyePosition.x, movingStep);
                            curLeftEyePosition.y = Mathf.Lerp(curLeftEyePosition.y, targetLeftEyePosition.y, movingStep);
                            curRightEyePosition.y = Mathf.Lerp(curRightEyePosition.y, targetRightEyePosition.y, movingStep);
                            curLeftEyePosition.z = Mathf.Lerp(curLeftEyePosition.z, targetLeftEyePosition.z, movingStep);
                            curRightEyePosition.z = Mathf.Lerp(curRightEyePosition.z, targetRightEyePosition.z, movingStep);
                            //print("d = " + d + " > " + margin + " movingStep = " + movingStep + " curHeadMoveCompensationParam =" + curHeadMoveCompensationParam);
                        }
                    }
                    //head roll need near instant sensitivity
                    //curRightEyePosition.y = curLeftEyePosition.y + Mathf.Tan(et.TP.rollFitted * Mathf.PI / 180) * (curRightEyePosition.x - curLeftEyePosition.x);
                    durationCounter += dt;
                    if (durationCounter >= 0.05f)
                    {
                        targetCenterEyePosition = (targetLeftEyePosition + targetRightEyePosition) / 2;
                        float dx = targetCenterEyePosition.x - lastPos.x;
                        float dy = targetCenterEyePosition.y - lastPos.y;
                        float dz = targetCenterEyePosition.z - lastPos.z;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy * 0.01f + dz * dz * 0.001f);
                        if (dist > scaleFactor * 0.006f)
                        {
                            stereoDirection = -1;
                        }
                        else
                        {
                            stereoDirection = +1;
                        }
                        durationCounter = 0;
                        lastPos = (targetLeftEyePosition + targetRightEyePosition) / 2;
                    }
                    et.TP.updateFlag = false;
                }
                else if (et.TP.status == EyeTracking.TrackingStatus.Lost || et.TP.status == EyeTracking.TrackingStatus.TooClose)
                {
                    stereoDirection = -1;
                    remindCondition = true;
                }
                if (stereoDirection < 0 || stereoDirectionForceNeg > 0)
                {
                    if (remindCondition)
                    {
                        stereoStrengthAni -= 0.01f;
                    }
                    else
                    {
                        stereoStrengthAni -= 0.005f * curHeadMoveCompensationParam;
                    }
                }
                else
                {
                    stereoStrengthAni += 0.01f * (6 - curHeadMoveCompensationParam);
                }
                stereoStrengthAni = Mathf.Clamp(stereoStrengthAni, 0.001f, 1);
            }
        }
        public void SwitchFrustumSyncEnable()
        {
            FrustumSyncEnable = !FrustumSyncEnable;
            if (!FrustumSyncEnable)
            {
                stereoStrengthAni = 1;
                StereoStrength = 1;
                CultureInfo culture = new CultureInfo("en-US");
                stereoBasis = scaleFactor * float.Parse(sysConfig["INTERNAL"]["STEREOBASISS"], culture);
                EyeLeftDefault = new Vector3((viewDefault.x - stereoBasis / 2) * stereoScaleFactor, viewDefault.y, viewDefault.z);
                EyeRightDefault = new Vector3((viewDefault.x + stereoBasis / 2) * stereoScaleFactor, viewDefault.y, viewDefault.z);
                curLeftEyePosition = EyeLeftDefault;
                curRightEyePosition = EyeRightDefault;
                RefreshEye(EyeLeftDefault, EyeRightDefault);
            }
        }
        public void AdjustFOVScale(float FovScale)
        {
            fovScaleFactor = FovScale;
            if (!FrustumSyncEnable)
            {
                EyeLeftDefault = new Vector3((viewDefault.x - stereoBasis / 2) * stereoScaleFactor, viewDefault.y, viewDefault.z);
                EyeRightDefault = new Vector3((viewDefault.x + stereoBasis / 2) * stereoScaleFactor, viewDefault.y, viewDefault.z);
                curLeftEyePosition = EyeLeftDefault;
                curRightEyePosition = EyeRightDefault;
                RefreshEye(EyeLeftDefault, EyeRightDefault);
            }
        }
        public bool SetEyeTrackingEnable(bool param)
        {
            bool ret = false;
            if (et != null)
            {
                et.SwitchET(param ? EyeTracking.OnOff.ON : EyeTracking.OnOff.OFF);
                EyeTrackingEnable = param;
                ret = true;
            }
            return ret;
        }
        public void ReSetVirtualScreenWidth(float width)
        {
            VirtualScreenWidth = width;
            scaleFactor = VirtualScreenWidth / (Mathf.Sin(Mathf.Atan(screenAspectRatio / doubleWidthFlag)) * screenSizeInInch * 0.0254f);
            initScreen();
            RefreshEye(curLeftEyePosition, curRightEyePosition);
        }
        private void ForceSwitchScreenWeaving()
        {
            screenWeaving = !screenWeaving;
            if (et != null)
            {
                et.Switch3D(screenWeaving ? EyeTracking.OnOff.ON : EyeTracking.OnOff.OFF);
            }
        }
        public void LRInterleaveSwitch()
        {
            LRInvertFlag = !LRInvertFlag;
            if (LRInvertFlag)
            {
                if (usedShader == AutoStereoShader.SideBySide)
                {
                    subCams[0].rect = new Rect(0.5f, 0, 0.5f, 1);
                    subCams[1].rect = new Rect(0, 0, 0.5f, 1);
                }
            }
            else
            {
                if (usedShader == AutoStereoShader.SideBySide)
                {
                    subCams[0].rect = new Rect(0, 0, 0.5f, 1);
                    subCams[1].rect = new Rect(0.5f, 0, 0.5f, 1);
                }
            }
        }
#endregion
#region UnityFunctions
        private void Awake()
        {
            mockHMDDetected = false;
#if USEMOCKHMD
            xrSettings = XRGeneralSettings.Instance;
            if (xrSettings != null)
            {
                XRSettings.eyeTextureResolutionScale = XRResolutionScaleFactor;
                //XRSettings.renderViewportScale = 1;  //0-1
                if (!xrSettings.InitManagerOnStart)
                {
                    xrSettings.Manager.InitializeLoaderSync();
                    xrSettings.Manager.StartSubsystems();
                    print("You'd better to check InitManagerOnStart in the panel instead, or the resolution can be lower and a lot of warnings when play");
                }
                XRLoader loader = xrSettings.Manager.activeLoader;
                if (loader != null && loader.GetType().Name == "MockHMDLoader")
                {
#if UNITY_EDITOR
                    xrSettings.InitManagerOnStart = true;
                    var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.xr.mock-hmd");
                    Debug.Log("mockHMD support is an experimental feature requires SRP(Not URP/HDRP yet) && directx12 or vulkan(some times not work with directx11), version required is v1.3.1, installed is: " + packageInfo.version);
#endif
                    mockHMDDetected = true;
                    _mockHMDNotifier();
                }
            }
            //print("mockHMDDetected enabled = " + mockHMDDetected + " eye texture size = " + XRSettings.eyeTextureDesc.width + ":" + XRSettings.eyeTextureDesc.height);  
#endif
        }
        void Start()
        {
            Application.targetFrameRate = 60;
            if (enableDebugging)
                InitDebugging();
            string[] CommandLineArgs = Environment.GetCommandLineArgs();
            if (sysConfig == null)
            {
                InitSysconfig();
            }
#if !UNITY_EDITOR
            if(allow3DScreenDetection)
            {
                int displayCnt = CheckMultiScreen();
                if (enableDebugging)
                    print("displayCnt = " + displayCnt + " Display.displays.Length = " + Display.displays.Length);
                if (displayCnt > 1)
                {
                    //dual screen render mode
                    Application.runInBackground = true;
                    //single screen render mode
                    if(detectedEDID != "")
                        SwitchScreen3D(winDisplayIdx);
                }
                if (enableDebugging)
                    print("winDisplayIdx = " + winDisplayIdx + " unityDisplayIdx = " + unityDisplayIdx);
            }
#endif
            initScreen();
            safeDisparity = 20.0f * scaleFactor;
            if (eyeTrackingMethod == EyeTracking.TrackingMethod.None)
            {
                EyeTrackingEnable = false;
                FrustumSyncEnable = false;
            }
            if (!EyeTrackingEnable || !FrustumSyncEnable)
                stereoStrengthAni = 1;
            if (eyeTrackingMethod != 0)
            {
                gameObject.AddComponent<UnityMainThreadDispatcher>();
                EyeTracking.ETConfig conf = new EyeTracking.ETConfig(IpAddress, port, clientType, false, trackingDevice, 
                    trackingNear, trackingFar, (uint)usedShader, K3DepthControl, PFNotify);
                if (eyeTrackingMethod == EyeTracking.TrackingMethod.EEServerThread)
                {
                    et = new EEServerThread();
                    et.Foreground = true;
                    LoadCalibResult();
                    et.InitET();
                }
            }
            thisCam = GetComponent<Camera>();
            nearClipPlaneDefault = thisCam.nearClipPlane;

            if (!mockHMDDetected)
            {
                thisCam.depth = 2;
                // Init New Cameras
                StereoRigs = new GameObject("StereoRigs");
                StereoRigs.transform.parent = transform;
                StereoRigs.transform.localPosition = Vector3.zero;
                StereoRigs.transform.localRotation = Quaternion.identity;
                StereoRigs.transform.localScale = Vector3.one;
                for (int i = 0; i < cameras.Length; i++)
                {
                    cameras[i] = new GameObject("Camera_" + (i + 1), typeof(Camera));
                    cameras[i].GetComponent<Camera>().CopyFrom(thisCam);
                    subCams[i] = cameras[i].GetComponent<Camera>();
                    subCams[i].depthTextureMode = DepthTextureMode.None;
                    subCams[i].depth = i;
                    cameras[i].transform.parent = StereoRigs.transform;
                }
            }
#if UNITY_HDRP
            thisCamCCM = thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().clearColorMode;
            thisCamBGColor = thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().backgroundColorHDR;
            thisCamLM = thisCam.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().volumeLayerMask;
#else
            thisCamCF = thisCam.clearFlags;
            thisCamBGColor = thisCam.backgroundColor;
            thisCamCM = thisCam.cullingMask;
#endif
            EyeLeftDefault = new Vector3((viewDefault.x - stereoBasis / 2) * stereoScaleFactor, viewDefault.y, viewDefault.z);
            EyeRightDefault = new Vector3((viewDefault.x + stereoBasis / 2) * stereoScaleFactor, viewDefault.y, viewDefault.z);
            curLeftEyePosition = EyeLeftDefault;
            curRightEyePosition = EyeRightDefault;
            if (ExitBypass2DSwitchResume)
            {
                ExitBypass2DSwitchResume = false;
                SetScreenSBS();
            }
            else
            {
                SetScreenMono();
            }
            Cursor.visible = false;
            if (!FrustumSyncEnable)
            {
                StereoStrength = 1;
            }
            if (!mockHMDDetected)
            {
                // Copy scripts to slave cameras
                foreach (Component c in thisCam.GetComponents<MonoBehaviour>())
                {
                    MonoBehaviour m = (MonoBehaviour)c;
                    if (enableDebugging)
                        Debug.Log(m.GetType().ToString() + " need copy to sub cameras?");
                    foreach (string s in ScriptsToCopy)
                    {
                        if (m.GetType().ToString().Substring(m.GetType().ToString().LastIndexOf('.') + 1) == s)
                        {
                            if (m.enabled)
                            {
                                if (enableDebugging)
                                    Debug.Log(s + " Script found and will be copied to slave cameras...");

                                for (int j = 0; j < cameras.Length; j++)
                                {
                                    //debug post processing copy error!
                                    if (s == "PostProcessLayer")
                                    {
#if POSTPROCESSING
                                        if (postProcessResources != null)
                                        {
                                            PostProcessLayer postProcessLayer = cameras[j].AddComponent<PostProcessLayer>();
                                            postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                                            postProcessLayer.volumeLayer = LayerMask.GetMask("PostProcessing");
                                            postProcessLayer.Init(postProcessResources);
                                            postProcessLayer.volumeTrigger = Camera.main.transform;
                                        }
#else
                                        Debug.Log(s + " uncomment //#define POSTPROCESSING to enable");
#endif
                                    }
                                    else
                                    {
                                        CopyComponent<MonoBehaviour>(m, cameras[j]);
                                    }
                                }
                                m.enabled = false;
                            }
                            else
                            {
                                if (enableDebugging)
                                    Debug.Log(s + " Script found disabled. Will not be copied...");
                            }
                        }
                    }
                }
            }
            else
            {
                //todo: add / remind user to add FXAA post processing module

            }
        }
        private void FixedUpdate()
        {
            EyeDataExtractor();
        }
        void Update()
        {
            if (eyeTrackingMethod != 0 && et != null && et.OnOffET == 1)
            {
                ScreenSync();
                if (FrustumSyncEnable)
                {
                    RefreshEye(curLeftEyePosition, curRightEyePosition);
                }
                if (et.TP.status == EyeTracking.TrackingStatus.Normal)
                {
                    if (Cursor.visible == true)
                    {
                        Cursor.visible = false;
                    }
                }
            }
            if (IsKeyPressed(Key.NumpadPlus))
            {
                NearClipPlaneAdj = 0.05f * NearClamp / 10;
            }
            if (IsKeyPressed(Key.NumpadMinus))
            {
                NearClipPlaneAdj = -0.05f * NearClamp / 10;
            }
            if (AreBothMouseButtonsPressed())
            {
                float v = GetMouseDeltaY() / NearClamp;
                NearClipPlaneAdj = Mathf.Clamp(v, -0.05f * NearClamp, 0.05f * NearClamp);
            }
            if (WasKeyPressedThisFrame(Key.P))
            {
                screenCapType = 1;
                SafeRenderCamera(thisCam);
            }
            if (!FrustumSyncEnable)
            {
                if (IsKeyPressed(Key.Numpad4))
                {
                    curLeftEyePosition = new Vector3(curLeftEyePosition.x - scaleFactor * screenSizeInInch / 10000, curLeftEyePosition.y, curLeftEyePosition.z);
                    curRightEyePosition = new Vector3(curRightEyePosition.x - scaleFactor * screenSizeInInch / 10000, curRightEyePosition.y, curRightEyePosition.z);
                    RefreshEye(curLeftEyePosition, curRightEyePosition);
                }
                else if (IsKeyPressed(Key.Numpad6))
                {
                    curLeftEyePosition = new Vector3(curLeftEyePosition.x + scaleFactor * screenSizeInInch / 10000, curLeftEyePosition.y, curLeftEyePosition.z);
                    curRightEyePosition = new Vector3(curRightEyePosition.x + scaleFactor * screenSizeInInch / 10000, curRightEyePosition.y, curRightEyePosition.z);
                    RefreshEye(curLeftEyePosition, curRightEyePosition);
                }
                else if (IsKeyPressed(Key.Numpad8))
                {
                    curLeftEyePosition = new Vector3(curLeftEyePosition.x, curLeftEyePosition.y, curLeftEyePosition.z - scaleFactor * screenSizeInInch / 10000);
                    curRightEyePosition = new Vector3(curRightEyePosition.x, curRightEyePosition.y, curRightEyePosition.z - scaleFactor * screenSizeInInch / 10000);
                    RefreshEye(curLeftEyePosition, curRightEyePosition);
                }
                else if (IsKeyPressed(Key.Numpad7))
                {
                    curLeftEyePosition = new Vector3(curLeftEyePosition.x, curLeftEyePosition.y + scaleFactor * screenSizeInInch / 10000, curLeftEyePosition.z);
                    curRightEyePosition = new Vector3(curRightEyePosition.x, curRightEyePosition.y + scaleFactor * screenSizeInInch / 10000, curRightEyePosition.z);
                    RefreshEye(curLeftEyePosition, curRightEyePosition);
                }
            }

            if (enableDebugging)
            {
                if (WasKeyPressedThisFrame(Key.Z))
                {
                    EyeTrackingSwitch = !EyeTrackingSwitch;
                    SetEyeTrackingEnable(EyeTrackingSwitch);
                }
                if (WasKeyPressedThisFrame(Key.T))
                {
                    ForceSwitchScreenWeaving();
                }
                if (WasKeyPressedThisFrame(Key.S) && IsKeyPressed(Key.LeftCtrl))
                {
                    LRInterleaveSwitch();
                }
            }

            if (WasKeyPressedThisFrame(Key.D) && IsKeyPressed(Key.LeftAlt))
            {
                SwitchDebugging();
            }
            if (enableDebugging)
            {
                if (WasKeyPressedThisFrame(Key.D))
                {
                    enableDebugMenu = !enableDebugMenu;
                    if (!enableDebugMenu)
                    {
                        debugStatus.text = debugEESVRRaw.text = "";
                        DebugPanel.gameObject.SetActive(false);
                    }
                    else
                    {
                        DebugPanel.gameObject.SetActive(true);
                    }
                }
                if (enableDebugMenu && et != null)
                {
                    FPS = (int)(1f / Time.smoothDeltaTime);
                    debugFPS.text = Mathf.Clamp(FPS, 0, 500).ToString() + "/" + Application.targetFrameRate;
                    debugString.Clear();
                    var mousePosition = GetMousePosition();
                    debugString.AppendFormat("TrackingStatus:{0} fullscreen:{1} resolution:{2} EyeTracking:{3} Screen:{4} Display:{5} VP:[{6}] mouse:[{7}]",
                        et.TP.status, Screen.fullScreen, Screen.width + ":" + Screen.height, et.OnOffET, et.OnOff3D, winDisplayIdx, GetComponent<StereoUI>().ViewportPoint, mousePosition);
                    debugString.AppendFormat(" Roll:{0}<={1} Yaw:{2}<={3} EESVR_IPD:{4}", et.TP.rollFitted.ToString("F0"), et.TP.roll.ToString("F0"),
                        et.TP.yawFitted.ToString("F0"), et.TP.yaw.ToString("F0"), et.TP.distance.ToString("F0"));
                    debugStatus.text = debugString.ToString();
                    debugString.Clear();
                    debugString.AppendFormat("{0} {1} Data: {2}", EyeTracking.ETConfig.trackingDevice, et.serverStatus, et.GetInfo());
                    debugEESVRRaw.text = debugString.ToString();
                    //print(_debugSW.ElapsedTicks);
                }
            }
        }

        private static bool IsKeyPressed(Key key)
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].isPressed;
        }

        private static bool WasKeyPressedThisFrame(Key key)
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }

        private static bool AreBothMouseButtonsPressed()
        {
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.isPressed && mouse.rightButton.isPressed;
        }

        private static float GetMouseDeltaY()
        {
            var mouse = Mouse.current;
            return mouse != null ? mouse.delta.ReadValue().y : 0f;
        }

        private static Vector2 GetMousePosition()
        {
            var mouse = Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
        }

        private static bool ShouldSkipImmediateRenderInEditor()
        {
            return Application.isEditor;
        }

        private static void SafeRenderCamera(Camera camera)
        {
            if (camera == null || ShouldSkipImmediateRenderInEditor())
            {
                return;
            }

            camera.Render();
        }
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (screenCapType > 0)
            {
                screenCapType = screenCapture();
            }
            Graphics.Blit(source, destination);
        }
        void OnDrawGizmosSelected()
        {
            if (thisCam != null)
            {
                Matrix4x4 tmp = Gizmos.matrix;
                Vector3 nearPlaneData = GetNearPlane(thisCam.stereoSeparation, thisCam.stereoConvergence, thisCam.fieldOfView, thisCam.aspect, width_m * 1000);//fieldOfView should be updated

                Vector3 npCenter = thisCam.transform.position + (thisCam.transform.TransformDirection(Vector3.forward) * nearPlaneData.z);
                Gizmos.matrix = Matrix4x4.TRS(npCenter, thisCam.transform.rotation, Vector3.one);
                Gizmos.color = new Color(1, 0, 0, 0.2F); // red
                Gizmos.DrawCube(Vector3.zero, new Vector3(nearPlaneData.x, nearPlaneData.y, 0.01f));

                Vector3 farPlaneData = GetFarPlane(thisCam.stereoSeparation, thisCam.stereoConvergence, thisCam.fieldOfView, thisCam.aspect, width_m * 1000);
                npCenter = thisCam.transform.position + (thisCam.transform.TransformDirection(Vector3.forward) * farPlaneData.z);
                Gizmos.matrix = Matrix4x4.TRS(npCenter, thisCam.transform.rotation, Vector3.one);
                Gizmos.color = new Color(0, 0, 1, 0.2F); // blue
                Gizmos.DrawCube(Vector3.zero, new Vector3(farPlaneData.x, farPlaneData.y, 0.01f));

                Gizmos.matrix = tmp;
            }
        }
        //string regPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings";
        //string regName = "ToastEnabled";
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        void OnApplicationFocus(bool focus)
        {
            if (et != null)
            {
                //debug reported occationally lost focus issue:
                if (enableDebugging)
                    Debug.Log("Focus: " + focus + " active window: " + WindowHandler.GetWindowHandleFocus() +
                        " current window: " + WindowHandler.GetWindowHandle() +
                        " HoldBeforeSwitch3DOn: " + HoldBeforeSwitch3DOn +
                        " ForceMono = " + ForceMono +
                        " usedShader = " + usedShader +
                        " Application.runInBackground = " + Application.runInBackground +
                        " et.OnOff3D = " + et.OnOff3D +
                        " Time: " + Mathf.RoundToInt(Time.realtimeSinceStartup / 60) + " minute");
                //make sure it's the main window on as3d display
                if (focus)
                {
#if !UNITY_EDITOR
                    if (WindowHandler.GetWindowHandleFocus() == WindowHandler.GetWindowHandle())
#endif
                    {
#if !UNITY_EDITOR
                        WindowHandler.SetWindowTopmost(true);
#endif
                        et.Foreground = true;
                        HoldBeforeSwitch3DOn = false;
                        IgnoreDuplicate2DSwitch = false;
                        if (EyeTrackingEnable)
                            et.SwitchET(EyeTracking.OnOff.ON);
                        if (usedShader == AutoStereoShader.SideBySide)
                        {
                            //Registry.SetValue(regPath, regName, 0);   //no effect even relogin or restart explorer
                            //const uint SPI_SETMESSAGEDURATION = 0x2017;
                            //const uint SPIF_UPDATEINIFILE = 0x01;
                            //const uint SPIF_SENDCHANGE = 0x02;
                            //SystemParametersInfo(SPI_SETMESSAGEDURATION, 10, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);    //no effect on toast and usb pop out
                            if (!ForceMono)
                            {
                                if (!Application.runInBackground || (Application.runInBackground && et.OnOff3D != 1))
                                {
                                    et.Switch3D(EyeTracking.OnOff.ON);
                                    StartCoroutine(syncWaitK3Switch((uint)EyeTracking.OnOff.ON, false, SetScreenSBSImmediate, ClearScreenWhileSwitch));
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (!Application.runInBackground)
                    {
#if !UNITY_EDITOR
                        WindowHandler.SetWindowTopmost(false);
#endif
                        if (FrustumSyncEnable)
                        {
                            stereoStrengthAni = 0.01f;
                        }
                        HoldBeforeSwitch3DOn = true;
                        if (isScreenSideBySide)
                        {
                            if(!IgnoreDuplicate2DSwitch)
                                et.Switch3D(EyeTracking.OnOff.OFF);
                            //StartCoroutine(syncWaitK3Switch((uint)EyeTracking.OnOff.OFF, true, null, ClearScreenWhileSwitch));       //can not delay it when switching focus by press win/alt+tab
                            //force 2D Snapshot Render
                            if (!mockHMDDetected)
                            {
                                subCams[0].rect = new Rect(0, 0, 1, 1);
                                SafeRenderCamera(subCams[0]);
                                subCams[1].enabled = false;
                            }
                            else
                            {
                                XRSettings.gameViewRenderMode = GameViewRenderMode.LeftEye;
                                RefreshEye(curLeftEyePosition, curRightEyePosition);
                            }
                        }
                        et.Foreground = false;
                        Cursor.visible = true;
                        if (!IgnoreDuplicate2DSwitch && EyeTrackingEnable)
                        {
                            et.SwitchET(EyeTracking.OnOff.OFF);
                        }
                    }
                }
            }
        }
        void OnDestroy()
        {
            if (et != null)
            {
                //et.ReleaseELACControl();
                et.ShutdownET(ExitBypass2DSwitch);
                et = null;
                if (ExitBypass2DSwitch)
                    ExitBypass2DSwitchResume = true;
            }
#if USEMOCKHMD
            if(xrSettings!= null && !xrSettings.InitManagerOnStart && mockHMDDetected)
                xrSettings.Manager.DeinitializeLoader();
#endif
#if !UNITY_EDITOR
            if(allow3DScreenDetection && allowExpandScreenSleep)
            {
                if(allDisplays.Count > 1)
                {
                    var pathInfos = PathInfo.GetActivePaths();
                    PathInfo.ApplyPathInfos(new[] { pathInfos.First(info => info.IsGDIPrimary) });
                    if (enableDebugging)
                        print("do deactivate subscreen");
                }
            }
#endif
#if DISABLEAUTOPLAY
            Registry.SetValue(regPathUSBAutoPlay, regNameUSBAutoPlay, 0);   //usb autoplay
#endif
        }
        #endregion
#region Calibration
        /// <summary>
        /// re-Calibration Rotation and Yaw benefit bigger parallax scenaries when looking far and near
        /// </summary>
        /// <param name="text remind"></param>
        public void CalibRecenter(Text value)
        {
            et.Yo = et.TP.yaw;
            et.Ro = et.TP.roll;
            et.Po = (et.TP.leftRaw.x + et.TP.rightRaw.x) / 2;
            et.IPDMax = et.TP.distance;
            if (value)
                value.text = "Yo = " + et.Yo.ToString("F1") + " Ro = " + et.Ro.ToString("F1") + " Po = " + et.Po.ToString("F1");
            PlayerPrefs.SetFloat("Yo", et.Yo);
            PlayerPrefs.SetFloat("Ro", et.Ro);
            PlayerPrefs.SetFloat("Po", et.Po);
            PlayerPrefs.SetFloat("IPDMax", et.IPDMax);
            print("Calib Done " + "Yo = " + et.Yo.ToString("F1") + " Ro = " + et.Ro.ToString("F1") + " Po = " + et.Po.ToString("F1"));
        }
        public void CalibRecenterClear(Text value)
        {
            et.Yo = et.Ro = et.Po = 0;
            if (value)
                value.text = "Yo = 0 Ro = 0 Po = 0";
            et.IPDMax = EyeTracking.IPDIPDMaxDefault - EyeTracking.IPDMargin;
            PlayerPrefs.SetFloat("Yo", et.Yo);
            PlayerPrefs.SetFloat("Ro", et.Ro);
            PlayerPrefs.SetFloat("Po", et.Po);
            PlayerPrefs.SetFloat("IPDMax", et.IPDMax);
            print("Calib Cleared ");
        }
        private void LoadCalibResult()
        {
            if (PlayerPrefs.HasKey("Yo"))
            {
                et.Yo = PlayerPrefs.GetFloat("Yo");
                et.Ro = PlayerPrefs.GetFloat("Ro");
                et.Po = PlayerPrefs.GetFloat("Po");
                et.IPDMax = PlayerPrefs.GetFloat("IPDMax");
            }
        }
        public void CalibResultRefresh(Text value)
        {
            value.text = "Yo = " + et.Yo.ToString("F1") + " Ro = " + et.Ro.ToString("F1") + " Po = " + et.Po.ToString("F1");
        }
        public void CalibHelpSwitch(GameObject helpMenu)
        {
            helpMenu.SetActive(!helpMenu.activeSelf);
        }
#endregion
#region ExitHelper
        private IEnumerator ExitPrepare(bool clearScreen, Action callback, int DelayCount)
        {
            if (et != null)
            {
                et.Switch3D(EyeTracking.OnOff.OFF);
                if (clearScreen)
                    StartCoroutine(syncWaitK3Switch((uint)EyeTracking.OnOff.OFF, true, SetScreenMonoImmediate, ClearScreenWhileSwitch, DelayCount));
                et.SwitchET(EyeTracking.OnOff.OFF);
                HoldBeforeSwitch3DOn = true;
                NearClipPlaneReset();
                while (isScreenSideBySide)
                {
                    yield return new WaitForEndOfFrame();
                }
                IgnoreDuplicate2DSwitch = true;
                callback(); 
            }
        }
        public void Quit()
        {
            StartCoroutine(ExitPrepare(true, Application.Quit, 2));
        }
        public void MinimizeWindow()
        {
            StartCoroutine(ExitPrepare(true, WindowHandler.ShowWindowMinimized, 2));
        }
        public void ExitPrepareElse(Action action)
        {
            StartCoroutine(ExitPrepare(true, action, 2));
        }
        public void ExitPrepareMoreDelay(Action action)
        {
            StartCoroutine(ExitPrepare(true, action, 30));  //30 for fast switch firmware, 70 for slow switch firmware
        }
        #endregion
    }
}
