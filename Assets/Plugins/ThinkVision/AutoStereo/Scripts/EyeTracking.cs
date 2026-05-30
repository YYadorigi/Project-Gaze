using UnityEngine;

/// <summary>
/// Screen with 3DScreen Eyetracking control abstract class
/// </summary>
namespace AS3DPlugin
{
    public abstract class EyeTracking
    {
        /// <summary>
        /// server side status
        /// OnOffET: if eyetracking is on
        /// OnOff3D: if both k3 and film(if under control) are on.
        /// LeftViewOnly: about 3 seconds later when viewer left, the screen will be set to this mode to show only Left Eye screen, the resolution will be halved
        /// </summary>
        public uint OnOffET = (uint)OnOff.OFF;
        public uint OnOff3D = (uint)OnOff.OFF;
        public bool LeftViewOnly = false;
        /// <summary>
        /// Forground: set if main app is in the foreground
        /// </summary>
        public bool Foreground = false; 
        /// <summary>
        /// Not accurate, please use EDID infor instead
        /// </summary>
        public enum PlatformID
        {
            Lenovo27 = 8,   //Lenovo 27 3D
            MaxPlatformID
        }
        public enum OnOff
        {
            OFF,
            ON
        }
        public enum TrackingMethod
        {
            None,
            EEServerThread
        }
        public enum TrackingDevice
        {
            SingleCamera,
            StereoCamera,
            Tobii,
            Himax
        }
        public enum ClentStatus
        {
            Stopped,
            Running
        }
        public enum ServerStatus
        {
            Unkown,
            Starting,   //connecting server socket
            Restarting,    //socket disconnected and reconnecting
            Connected,   // server socket connected
            Started,    //correct ack received and connected
            Tracking
        }
        public enum TrackingStatus
        {
            Lost,
            Normal,
            LeftBorder,
            RightBorder,
            TopBorder,
            BottomBorder,
            TooClose,
            TooFar
        }
        public struct TrackingParam
        {
            public bool updateFlag;
            public TrackingStatus status;
            public Vector3 leftRaw;    //mm in physical space
            public Vector3 rightRaw;
            public Vector3 weaving; //cm
            public float distance;  //mm in physical space
            public float roll;  //in degrees
            public float yaw;   //in degrees
            public bool yawBig; //flag indicate yaw larger than 45
            public float rollFitted;    //in degrees
            public float yawFitted;    //in degrees
            public Vector3 left;    //m in virtual space
            public Vector3 right;
            public Vector3 centor;
        }
        public struct WavingParam
        {
            public float SLANT;
            public float PITCH;
            public float CTVIEW;
            public int CAMERAX;
            public int CAMERAY;
        }
        public struct PlatformFeatures
        {
            public bool K3Valid;            //has k3 installed, refresh each time after restart eesvr
            public bool SwithcableFilm;
            public bool K3ModeLicenseTB;    //top bottom to multiview convertion support
            public bool K3ModeLicenseSBS;   //side by side weaving
            public bool K3ModeLicense2DZ;   //side by side 2d+depth to multiview convertion support
            public bool K3ModeLicenseMono;  //bypass 2D, has nothing to do with film switchable feature
        }
        /// <summary>
        /// EESVR will report ASIC information after connected
        /// </summary>
        /// <param name="features"></param>
        public delegate void PlatformFeaturesNotify(PlatformFeatures features);
        public struct ETConfig
        {
            public static string ipAddress;
            public static int port;   //9647,6143,25938;6768
            public static uint clientType;
            public static bool supportYawCalib; //For Tobii tilt follow patch used in media
            public static uint preserved0;  //see enum AutoStereoShader
            public static uint preserved1;  //0-0x3f FOCUS_DEPTH
            public static TrackingDevice trackingDevice = TrackingDevice.SingleCamera;
            public static PlatformFeaturesNotify PFNotify;
            public ETConfig(string ip, int p, uint ct, bool yawCalib,TrackingDevice device, float Near, float Far, uint shader, uint depth, PlatformFeaturesNotify pfNotifyCB = null)
            {
                ipAddress = ip;
                port = p;
                clientType = ct;
                supportYawCalib = yawCalib;
                trackingDevice = device;
                trackingNear = Near;
                trackingFar = Far;
                preserved0 = shader;
                preserved1 = depth;
                PFNotify = pfNotifyCB;
                InitRegion(StereoCam.EyeTrackFOV[0], StereoCam.EyeTrackFOV[1], StereoCam.EyeTrackFOVBias);
            }
        }
        public TrackingParam TP;
        public WavingParam WP;
        public ClentStatus clientStatus = ClentStatus.Stopped;
        public ServerStatus serverStatus = ServerStatus.Unkown;
        public const float IPDMargin = 0.8f;
        public const float IPDIPDMaxDefault = 67.8f;
        public float Yo = 0;    //Yaw offset for user calibration
        public float Ro = 0;    //Roll offset
        public float Po = 0;    //Position X offset
        public float IPDMax = IPDIPDMaxDefault - IPDMargin;    //IPD for tobii
        private static float trackingNear = 0.4f;//0.3m for 15.6,32inch; 0.4m for 65inch
        private static float trackingFar = 1.4f;//1.4m for 15.6,32inch; 3m for 65inch
        private const float trackingMargin = 10f;//in degrees
        protected float trackingWidth;
        protected float trackingHeight;
        private float yOffset = StereoCam.height_m / 2;   //in case the camera is installed on top center of the display
        private float rollValid = 0;
        private const float DRFitMin = 0.7887f;
        private const float DRFitMax1 = 0.1944f;
        private const float DRFitMax2 = 4.219f;
        private const float DMin = 7.1f;
        private const float DMax = 21.5f;
        /// <summary>
        /// define the tracking erea based on center of physical screen, right hand coordinate
        /// </summary>
        private struct trackingFrustum
        {
            public Vector3 position;
            public FrustumPlanes frustum;
            public Vector4 biasDegree;
            public Plane planeL;
            public Plane planeR;
            public Plane planeT;
            public Plane planeB;
            //private string deb = "";
            //private static int cntPrint = 0;
            public trackingFrustum(Vector3 p, float fovH, float fovV, Vector4 bias)
            {
                position = p;
                biasDegree = bias;
                frustum.left = Mathf.Cos(Mathf.Deg2Rad * fovH / 2);
                frustum.right = -frustum.left;
                frustum.top = -Mathf.Cos(Mathf.Deg2Rad * fovV / 2);
                frustum.bottom = -frustum.top;
                frustum.zNear = trackingNear;
                frustum.zFar = trackingFar;
                Matrix4x4 matrix = Matrix4x4.identity;
                planeL = new Plane(matrix.MultiplyPoint(new Vector3(frustum.left, 0, Mathf.Sin(Mathf.Deg2Rad * fovH / 2))), p);
                planeR = new Plane(matrix.MultiplyPoint(new Vector3(frustum.right, 0, Mathf.Sin(Mathf.Deg2Rad * fovH / 2))), p);
                planeT = new Plane(matrix.MultiplyPoint(new Vector3(0, frustum.top, Mathf.Sin(Mathf.Deg2Rad * fovV / 2))), p);
                planeB = new Plane(matrix.MultiplyPoint(new Vector3(0, frustum.bottom, Mathf.Sin(Mathf.Deg2Rad * fovV / 2))), p);
            }
        }
        private static trackingFrustum region;
        private static void InitRegion(float fovH, float fovV, Vector4 fovBias)
        {
            //ignore region test for tobii
            if (TrackingDevice.Tobii != ETConfig.trackingDevice)
            {
                region = new trackingFrustum(new Vector3(0, StereoCam.height_m / 2, 0), fovH, fovV, fovBias);
            }
            //todo: visualize the plane grids and sweetspot in 3d
        }
        //filter eye position in meters
        private TrackingStatus RegionFilter(Vector3 EyeLeft, Vector3 EyeRight)
        {
            if ((float.IsNaN(EyeLeft.x)) || (float.IsNaN(EyeRight.x)) || (float.IsInfinity(EyeLeft.x)) || (float.IsInfinity(EyeRight.x)))
            {
                return TrackingStatus.Lost;
            }
            else if (TrackingDevice.Tobii != ETConfig.trackingDevice)
            {
                Vector3 vt = EyeLeft.y > EyeRight.y ? EyeLeft : EyeRight;
                Vector3 vb = EyeLeft.y < EyeRight.y ? EyeLeft : EyeRight;
                Vector3 vl = EyeLeft.x < EyeRight.x ? EyeLeft : EyeRight;
                Vector3 vr = EyeLeft.x > EyeRight.x ? EyeLeft : EyeRight;
                //todo: remove biasDegree and use calibration value
                float pl = Vector3.Angle(vr.normalized, Vector3.ProjectOnPlane(vr, region.planeL.normal)) + region.biasDegree.x;//viewer's right on frustum's left
                float pr = Vector3.Angle(vl.normalized, Vector3.ProjectOnPlane(vl, region.planeR.normal)) + region.biasDegree.y;
                float pt = Vector3.Angle(vt.normalized, Vector3.ProjectOnPlane(vt, region.planeT.normal)) + region.biasDegree.z;
                float pb = Vector3.Angle(vb.normalized, Vector3.ProjectOnPlane(vb, region.planeB.normal)) + region.biasDegree.w;
                TrackingStatus status;
                if (pl <= trackingMargin)
                    status = TrackingStatus.LeftBorder;
                else if (pr <= trackingMargin)
                    status = TrackingStatus.RightBorder;
                else if ((EyeLeft.z + EyeRight.z) / 2 > region.frustum.zFar)
                {
                    status = TrackingStatus.TooFar;
                }
                else if ((EyeLeft.z + EyeRight.z) / 2 < region.frustum.zNear)
                    status = TrackingStatus.TooClose;
                else if (pt <= trackingMargin / 2)
                    status = TrackingStatus.TopBorder;
                else if (pb <= trackingMargin / 2)
                    status = TrackingStatus.BottomBorder;
                else if (pl > trackingMargin && pr > trackingMargin)
                    status = TrackingStatus.Normal;
                else
                    status = TrackingStatus.Lost;
        //        if (cntPrint > 30)
        //        {
        //            cntPrint = 0;
        ////            Debug.Log(" vt = " + vt + " left = " + pl.ToString("F1") + " right = " + pr.ToString("F1") + " top = " + pt.ToString("F1") + " bottom = " + pb.ToString("F1") +
        ////" Margin = " + trackingMargin + " Near = " + region.frustum.zNear + " Far = " + region.frustum.zFar);
        //            Debug.Log("degrees to border LRTB: " + pl.ToString("##") + " " + pr.ToString("##") + " " + pt.ToString("##") + " " + pb.ToString("##") + " status " + status);
        //        }
        //        else
        //        {
        //            cntPrint++;
        //        }
                return status;
            }
            else
            {
                return TrackingStatus.Normal;
            }

        }
        protected void PushEye(Vector3 EyeLeft, Vector3 EyeRight, Vector3 EyeWeaving)   //in mm
        {
            TP.status = RegionFilter(EyeLeft / 1000, EyeRight / 1000);
            if (EyeLeft.x == EyeRight.x)
            {
                //warning: missing direction data, has to generate a fake one
                EyeLeft -= new Vector3(31, 0, 0);
                EyeRight += new Vector3(31, 0, 0);
            }
            TP.weaving = EyeWeaving;
            if (TP.status >= TrackingStatus.Normal)
            {
                TP.leftRaw = EyeLeft;
                TP.rightRaw = EyeRight;
                TP.distance = Vector3.Distance(TP.leftRaw, TP.rightRaw);
                Vector3 centorRaw = (TP.leftRaw + TP.rightRaw) / 2;
                TP.roll = Mathf.Atan((EyeRight.y - EyeLeft.y) / (EyeRight.x - EyeLeft.x)) * 180 / Mathf.PI;
                TP.yaw = Mathf.Atan((EyeRight.z - EyeLeft.z) / (EyeRight.x - EyeLeft.x)) * 180 / Mathf.PI;
                TP.left = EyeLeft * StereoCam.scaleFactor / 1000;
                TP.right = EyeRight * StereoCam.scaleFactor / 1000;
                TP.centor = centorRaw * StereoCam.scaleFactor / 1000;
                TP.yawFitted = TP.yaw - Yo;
                TP.rollFitted = TP.roll - Ro;
            }            
            TP.updateFlag = true;
        }
        protected void PushEye(Vector3 EyeLeft, Vector3 EyeRight, int faceNum)   //in mm
        {
            if (faceNum == 0)
            {
                TP.status = TrackingStatus.Lost;
            }
            else
            {
                if (EyeLeft.x == EyeRight.x)
                {
                    //warning: missing direction data, has to generate a fake one
                    EyeLeft -= new Vector3(31, 0, 0);
                    EyeRight += new Vector3(31, 0, 0);
                }
                TP.status = RegionFilter(EyeLeft / 1000, EyeRight / 1000);
                if (TP.status >= TrackingStatus.Normal)
                {
                    TP.leftRaw = EyeLeft;
                    TP.rightRaw = EyeRight;
                    TP.distance = Vector3.Distance(TP.leftRaw, TP.rightRaw);
                    TP.roll = Mathf.Atan((EyeRight.y - EyeLeft.y) / (EyeRight.x - EyeLeft.x)) * 180 / Mathf.PI;
                    TP.yaw = Mathf.Atan((EyeRight.z - EyeLeft.z) / (EyeRight.x - EyeLeft.x)) * 180 / Mathf.PI;
                    TP.yawFitted = TP.yaw - Yo;
                    //fiter out incorrect head tilt, refer to https://developer.tobii.com/community/forums/topic/head-tracking-data/page/2/
                    if (EyeTracking.ETConfig.trackingDevice == EyeTracking.TrackingDevice.Tobii && EyeTracking.ETConfig.supportYawCalib)    //only for media
                    {
                        float[] DRFit = { DRFitMin, 0};
                        float D = IPDMax - TP.distance;
                        if (D <= DMax)
                        {
                            float RF;
                            float Sy;
                            float rollFitMultiplier = 0.4f;
                            if (D < 0)
                            { 
                                RF = 0;
                            }
                            else
                            {
                                if (D > DMin)
                                {
                                    DRFit[0] = DRFitMax1;
                                    DRFit[1] = DRFitMax2;
                                }
                                Sy = Mathf.Sign(TP.yawFitted);
                                RF = (DRFit[0] * D + DRFit[1]) * Sy;
                            }
                            rollValid = TP.rollFitted = (TP.roll + RF - Ro) * rollFitMultiplier;
                            TP.yawBig = false;
                        }
                        else
                        {
                            //ignore current roll and use last valid data, this means even unsync head rotation too
                            TP.yawBig = true;
                            TP.rollFitted = rollValid - Ro;
                        }
                        if (Ro == 0)
                            TP.rollFitted = TP.roll;
                        float rollFittedDegree = TP.rollFitted * Mathf.PI / 180;
                        Vector3 centorRaw = (TP.leftRaw + TP.rightRaw) / 2;
                        TP.left = new Vector3(centorRaw.x - Mathf.Cos(rollFittedDegree) * TP.distance / 2 - Po,
                            centorRaw.y - Mathf.Sin(rollFittedDegree) * TP.distance / 2, TP.leftRaw.z) * StereoCam.scaleFactor / 1000;
                        TP.right = new Vector3(centorRaw.x + Mathf.Cos(rollFittedDegree) * TP.distance / 2 - Po,
                            centorRaw.y + Mathf.Sin(rollFittedDegree) * TP.distance / 2, TP.rightRaw.z) * StereoCam.scaleFactor / 1000;
                        EyeLeft.x -= Po;
                        EyeRight.x -= Po;
                    }
                    else
                    {
                        TP.rollFitted = TP.roll - Ro;
                        EyeLeft.x -= Po;
                        EyeRight.x -= Po;
                        TP.left = EyeLeft * StereoCam.scaleFactor / 1000;
                        TP.right = EyeRight * StereoCam.scaleFactor / 1000;
                    }

                    TP.centor = (EyeLeft + EyeRight) / 2 * StereoCam.scaleFactor / 1000;
                }
            }
            TP.updateFlag = true;
        }
        protected void PushEye(Vector2 EyeLeft, Vector2 EyeRight, int flipX, int faceNum)
        {
            if (faceNum == 0)
            {
                TP.status = TrackingStatus.Lost;
            }
            else
            {
                float A = Mathf.Abs((StereoCam.stereoBasis / StereoCam.scaleFactor / 2) /
                    (Mathf.Tan(StereoCam.EyeTrackFOV[0] / 2 * Mathf.Deg2Rad) * StereoCam.focalDistance - StereoCam.stereoBasis / StereoCam.scaleFactor / 2));
                float B = Mathf.Abs(Vector2.Distance(EyeLeft, EyeRight)) / (trackingWidth - Mathf.Abs(Vector2.Distance(EyeLeft, EyeRight)));
                float Pz = (1 + B) * A / ((1 + A) * B) * StereoCam.focalDistance;
                float PxL = (2 * EyeLeft.x / trackingWidth - 1) * Mathf.Tan(StereoCam.EyeTrackFOV[0] / 2 * Mathf.Deg2Rad) * Pz * flipX;
                float PyL = (1 - 2 * EyeLeft.y / trackingHeight) * Mathf.Tan(StereoCam.EyeTrackFOV[1] / 2 * Mathf.Deg2Rad) * Pz;
                float PxR = (2 * EyeRight.x / trackingWidth - 1) * Mathf.Tan(StereoCam.EyeTrackFOV[0] / 2 * Mathf.Deg2Rad) * Pz * flipX;
                float PyR = (1 - 2 * EyeRight.y / trackingHeight) * Mathf.Tan(StereoCam.EyeTrackFOV[1] / 2 * Mathf.Deg2Rad) * Pz;
                //Debug.Log("Pz " + Pz + " ipd = " + StereoCam.stereoBasis / StereoCam.scaleFactor);
                Vector3 PL = new Vector3(PxL, PyL, Pz);
                Vector3 PR = new Vector3(PxR, PyR, Pz);
                TP.status = RegionFilter(PL, PR);
                Matrix4x4 extrincs = Matrix4x4.identity;
                extrincs.SetColumn(3, new Vector4(0, yOffset / StereoCam.scaleFactor)); 
                PL = extrincs.MultiplyPoint3x4(PL);
                PR = extrincs.MultiplyPoint3x4(PR);
                if (TP.status >= TrackingStatus.Normal)
                {
                    TP.leftRaw = PL * 1000;
                    TP.rightRaw = PR * 1000;
                    TP.distance = Vector3.Distance(TP.leftRaw, TP.rightRaw);
                    Vector3 centorRaw = (TP.leftRaw + TP.rightRaw) / 2;
                    TP.roll = Mathf.Atan((TP.rightRaw.y - TP.leftRaw.y) / (TP.rightRaw.x - TP.leftRaw.x)) * 180 / Mathf.PI;
                    TP.yaw = Mathf.Atan((TP.rightRaw.z - TP.leftRaw.z) / (TP.rightRaw.x - TP.leftRaw.x)) * 180 / Mathf.PI;
                    TP.left = PL * StereoCam.scaleFactor;
                    TP.right = PR * StereoCam.scaleFactor;
                    TP.centor = centorRaw * StereoCam.scaleFactor / 1000;
                    TP.yawFitted = TP.yaw - Yo;
                    TP.rollFitted = TP.roll - Ro;
                }
            }
            TP.updateFlag = true;
        }
        /// <summary>
        /// Init EESVR connection, set ETConfig before calling
        /// </summary>
        /// <returns>0</returns>
        public abstract int InitET();   //init service
        /// <summary>
        /// Report EESVR Server Status
        /// </summary>
        /// <returns></returns>
        public abstract ServerStatus GetServerStatus();
        /// <summary>
        /// swith camera on/off, in most cases, you don't need to call it explicitly.
        /// </summary>
        /// <param name="onOff"></param>
        /// <returns></returns>
        public abstract int SwitchET(OnOff onOff);
        /// <summary>
        /// get eyetracking module info string, for debugging purpose
        /// </summary>
        /// <returns></returns>
        public abstract string GetInfo();
        /// <summary>
        /// destroy all instance
        /// </summary>
        /// <param name="bypassSwitch2D"> set true if closing scene and opening another in 3d</param>
        /// <returns></returns>
        public abstract int ShutdownET(bool bypassSwitch2D);
        /// <summary>
        /// get face numbers detected
        /// </summary>
        /// <returns>0/1</returns>
        public virtual int GetFaceNum()
        {
            return 0;
        }
        /// <summary>
        /// swith screen 3d status on/off
        /// </summary>
        /// <param name="onOff">On / Off</param>
        /// <returns></returns>
        public virtual int Switch3D(OnOff onOff) 
        {
            return 0;
        }
        /// <summary>
        /// user's face width for single tracking camera usage, obsolated.
        /// </summary>
        /// <param name="FW">0 - 1</param>
        /// <returns></returns>
        public virtual int SetFaceWidth(float FW) 
        {
            return 0;
        }
        /// <summary>
        /// get current user's face width for single tracking camera usage, obsolated.
        /// </summary>
        /// <returns>0 - 1</returns>
        public virtual float GetFaceWidth()
        {
            return 0;
        }
        /// <summary>
        /// Switch Tracking Camera preview window, obsolated
        /// </summary>
        /// <param name="onOff">On / Off</param>
        /// <returns></returns>
        public virtual int SwitchPreview(int onOff)
        {
            return 0;
        }
        /// <summary>
        /// Send back tracking data
        /// </summary>
        /// <param name="Lx"></param>
        /// <param name="Ly"></param>
        /// <param name="Lz"></param>
        /// <param name="Rx"></param>
        /// <param name="Ry"></param>
        /// <param name="Rz"></param>
        /// <returns></returns>
        public virtual int SendInfo(float Lx, float Ly ,float Lz, float Rx, float Ry, float Rz)
        {
            return 0;
        }
        /// <summary>
        /// Set Asic 2D to 3D conversion strengh, for 2DZ mode & TB mode
        /// </summary>
        public virtual void SetK3Depth()
        {
            return;
        }
        /// <summary>
        /// Querry Asic Status
        /// </summary>
        /// <returns>0: error; 1: false; 2: true;</returns>
        public virtual uint IsK3Enabled()
        {
            return 0;
        }
        /// <summary>
        ///  Not accurate, Please use EDID instead 
        /// </summary>
        /// <returns>current platform id</returns>
        public virtual PlatformID GetPID()
        {
            return PlatformID.Lenovo27;
        }
        /// <summary>
        /// Enable Screen auto switch to Half width 2D or not, better to set true when building a media player
        /// </summary>
        /// <param name="param">true: Enable Screen auto switch to Half width 2D(smoothly); false: Disable that feature </param>
        public virtual void SetAuto2D(bool param)
        {
            return;
        }
        /// <summary>
        /// Release Auto2D Control, should be called if SetAuto2D(False) when minimize or quit
        /// </summary>
        public virtual void ReleaseELACControl()
        {
            return;
        }
    }
}