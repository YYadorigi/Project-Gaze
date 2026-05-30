#pragma warning disable 0649
using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System;
using System.Net.NetworkInformation;
using System.Globalization;
namespace AS3DPlugin
{
    /// <summary>
    /// EESVR Service connection Module
    /// </summary>
    public class EEServerThread : EyeTracking
    {
        Thread thread;
        private TcpClient client = null;
        private string recvString = null;
        private string recvStringStatus = null;
        private const int resolutionW = 640;
        private const int resolutionH = 480;
        private int[] ListenPorts = { 9647, 6143, 25938 };    //default ports
        private int ListenPortId = 0;
        private EyeTrackingRemote msgInfo;
        private uint[,] commandBuffer = new uint[(uint)SERVER_MSG.MaxSERVER_MSG, 2];// msg : [value : sending]
        private float faceWidth = 1;
        private float InfoLx, InfoLy, InfoLz, InfoRx, InfoRy, InfoRz;
        private uint ProtocalVersion = 3;
        private uint K3Enabled = 0; //0:unkown(unplug usb cable); 1: false(after reset eesvr or reboot); 2: true
        private PlatformID PID = PlatformID.Lenovo27;
        private EyeTrackerModule ETModuleID = EyeTrackerModule.WZOpenCV;
        private const string CameraModuleNameSamsung = "Web Camera";
        private int MultiViewSwitchDelayPatch = 0;  //<0 disable relative commands if switch failure happened
        private object timerLock = new object();
        private int shortTimer, midTimer, longTimer;
        private bool EnableSyncStatus = false;
        private System.Diagnostics.Stopwatch _debugSW;
        private enum ClientType
        {
            Shell,  //max count : 1
            VideoPlayer,    //10
            PicturePlayer,  //5
            Game,   //10
            Calibration,    //1
            EyeTrackingModule,  //1
            ServiceExtension,   //1
            NetworkMonitor = 253,
            Unknown,
            BroadCast
        }
        private ClientType clientType;
        private enum EyeTrackerModule
        {
            WZOpenCV,    //single camera
            Tobii,     //
            DualCamera,     //
            Himax,
            MaxEyeTrackerModele
        }
        private enum FILMTYPE
        {
            Colume = 1,
            Slant,  //should expand to switchable film
        }
        private enum VIEWNUMBER
        {
            TwoView = 1,    // two view
            MultiView = 2   // multiview, likely about 5 views
        }
        private enum K3SUPPORT
        {
            False,
            True
        }
        //default value set to 32 inches as3d display
        private bool k3ModeLicenseTB = true;    //top bottom to multiview convertion support
        private bool k3ModeLicenseSBS = true;   //side by side weaving
        private bool k3ModeLicense2DZ = true;   //side by side 2d+depth to multiview convertion support
        private bool k3ModeLicenseMono = true;  //bypass 2D, has nothing to do with film switchable feature
        private FILMTYPE filmType = FILMTYPE.Slant;
        private VIEWNUMBER viewNumber = VIEWNUMBER.MultiView;
        private K3SUPPORT k3Support = K3SUPPORT.True;
        private PlatformFeatures platformFeatures;
        private enum K3MODE
        {
            MultiviewTB,    //TopBottom to multiview    //might not be supported/restricted
            TwoViewSBS,
            Multiview2DZ,   //might not be supported/restricted
            Bypass,
            LeftViewOnly,  //So called 3D2, managed by EESVR and controlled by ELAC command
                                //FrameSequencial4K1K,    //dual 1920x1080 page fliping, not support!
        }
        private enum SERVER_MSG
        {
            AskClientTypePassive = 1,
            AnsClientTypeAPassive,
            SendClientType,
            AnsClientTypeActive,
            SendLoop,   //
            AnsLoop,
            GetInfo = 7,
            AnsInfo,
            AskPreview = 15,    //
            AnsPreview,
            AskSubmitChange = 19,   //CLIENT_UID STATUS_CHANGE_FLAG 
            AskEyeTrackingModuleStatus = 21,    //not work? {"CLIENT_TYPE":5,"SERVER_MSG":21,"SOURCE_SOCK":0,"SOURCE_TYPE":5}
            AnsEyeTrackingModuleStatus,
            AskEnableTracking = 27, //
            AnsEnableTracking,
            AskWeaving,  //
            AnsWeaving,
            AskDeviceInfo = 33,  //
            AnsDeviceInfo,
            SendTrackingSetting,    //
            AnsTrackingSetting,
            SendFaceWidth,  //37
            AnsFaceWidth,
            AskNotifyPassive,
            AnsNotifyPassive,
            SendInfo,
            AnsSendinfo,
            AskServerStatus,    //43    
            AnsServerStatus,    //removed ON_OFF_EYETRACK & ON_OFF_3DFILM in spec 3.0.2, moved to AnsPlatformStatus & NotifyPlatformStatus
            SetTobbi,
            AnsTobbi,
            SetExtend,
            AnsExtend,
            AskFaceWidthParam = 57,
            AnsFaceWidthParam,
            AskPlatformParam = 63,  //ask for list of name as: PlatformID
            AnsPlatformParam,
            AskEyeTrackerModule,    //ask for list of name as: EyeTrackerModule   
            AnsEyeTrackerModule,
            AskEyeTrackerModuleCamera,  //need to open one module first(enable eyetracking!!), might return empty if init fail! will store last selection in it's register table
            AnsEyeTrackerModuleCamera,
            AskSwitchPlatform,
            AnsSwitchPlatform,
            AskSwitchEyeTrackerModule,
            AnsSwitchEyeTrackerModule,
            AskSwitchEyeTrackerModuleCamera,    //if selected eyetracking not run, need to invoke this command automatically.
            AnsSwitchEyeTrackerModuleCamera,
            AskPlatformFeature = 79,        //spec 3.0.2
            AnsPlatformFeature,         
            AskPlatformStatus,  //K3 version of AskServerStatus
            AnsPlatformStatus,
            NotifyPlatformStatus, //notify all clients if k3 status has changed
            SetK3Mode = 85,
            AnsK3Mode,          //patch: if AnsK3Mode no response(might be k3 down), need to AskPlatformStatus to re-confirm and notify user to reboot?!
            AskLDCPath = 97,    //for software weaving
            AnsLDCPath,
            AskAuto2DStatus = 107,    //for Auto 2D/3D Switch: ELAC_SET = 1:ON 0:OFF
            ReleaseELACControl = 115,     //Release ELAC control to EESVR
            MaxSERVER_MSG
        }
        [System.Serializable]
        private class NET_MSG
        {
            public uint SERVER_MSG;
        }
        [System.Serializable]
        private class NET_MSG_BASE
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
        }
        [System.Serializable]
        private class NET_MSG_BASE_ANS
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint ACTIVE_STATUS;
        }
        [System.Serializable]
        private class NET_MSG_SWITCH
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint ON_OFF_OPTION;
            public uint CONFIG; //0
            public uint DELAY_TIME; //0
        }
        [System.Serializable]
        private class NET_MSG_SWITCH_ANS
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint ON_OFF_OPTION;
            public uint ACTION_STATUS;  //0: success; 1: failure because of other client confliction; 2: internal error
        }
        [System.Serializable]
        private class NET_MSG_SETTING
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint THRESHOLD_SPEED;
            public uint THRESHOLD_VARIANC;
            public uint CHECK_ENABLE;
        }
        [System.Serializable]
        private class NET_MSG_SETTING_FACE
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public float FACE_WIDTH;
        }
        [System.Serializable]
        private class NET_MSG_SERVER_STATUS
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint EESERVER_VER;
            public uint PROTOCOL_VER;       //2 3
            public uint ON_OFF_EYETRACK;    //2: closed 1: opened   //will be deprecated 
            public uint ON_OFF_3DFILM;      //2: closed 1: opened   //will be deprecated 
        }

        [System.Serializable]
        private class NET_MSG_PLATFORM_FEATURE
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint FEATURE_0;   //spec 3.0.2 page.30. bit[0,3]:FilmType 2(slant) bit[4,7]:ViewNumber 1(2-view) 2(multi-view) bit[12,15]:K3Support 1(support) bit[16,19]: K3Mode bit[n] = 1(support mode n)
        }
        [System.Serializable]
        private class NET_MSG_PLATFORM_STATUS
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint STATUS_0;   //spec 3.0.2 page.24. bit[0,3]:K3Enable=0(enable) bit[8,15]:K3Mode=0/1(L+R weaving) 2(2d+z) 3(bypass) bit[24,27]:Film=0(3D off) bit[28,31]: eyetracking=0(Off/ =5(On))
                                    //mapping Film+K3Mode to ON_OFF_3DFILM; mapping Eyetrack to ON_OFF_EYETRACK
            public uint STATUS_1;   //IDs, just ignore
        }
        [System.Serializable]
        private class NET_MSG_PLATFORM_STATUS_NOTIFY
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint STATUS_0;   //spec 3.0.2 page.24. bit[0,3]:K3Enable=0(enable) bit[8,15]:K3Mode=0/1(L+R weaving) 2(2d+z) 3(bypass) bit[24,27]:Film=0(3D off) bit[28,31]: eyetracking=0(Off/ =5(On))
                                    //mapping Film+K3Mode to ON_OFF_3DFILM; mapping Eyetrack to ON_OFF_EYETRACK
            public uint STATUS_1;   //IDs, just ignore
            public uint STATUS_CHANGE_FLAGS;    //bit[1] : 1: changed; 0: unchange
        }
        [System.Serializable]
        private class NET_MSG_K3MODE
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint OPTION;
            public uint MODE;
            public uint K3ENABLE;
            public uint FOCUS_DEPTH;
            public uint IS2D;   //0: normal K3Mode; 1:ForceOneView Rendering for all 3d modes, 
            public uint BYPASSFILMSWITCH;   //0: auto turn film on/off; 1:manuall switch film as usual using AskWeaving
        }
        [System.Serializable]
        private class NET_MSG_K3MODE_ANS
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint OPTION;
            public uint MODE;
            public uint K3ENABLE;
            public uint FOCUS_DEPTH; 
            public uint IS2D;
            public uint BYPASSFILMSWITCH;
            public uint STATUS;
        }
        [System.Serializable]
        private class EyeTrackingRemote
        {
            public int FACE_NUM;
            public string SLANT;
            public string PITCH;
            public string CTVIEW;
            public string CAMERAX;
            public string CAMERAY;
            public float LX;
            public float LY;
            public float LZ;
            public float RX;
            public float RY;
            public float RZ;
            public float FX;
            public float FY;
            public float FW;  //need to read and set
            public float FH;
            public int ON_OFF_3DFILM;   //1: on 2: off 0: not sure  //deprecated 
            public int SWITCH2D3D;  //xx（intValue，1: 2D(waving off) 2: 3D(waving on)    //deprecated
            public string HT;   //hand tracking
        }
        [System.Serializable]
        private class EyeTrackingLocal
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public int FACE_NUM;
            public float LX;
            public float LY;
            public float LZ;
            public float RX;
            public float RY;
            public float RZ;
            public float FX;
            public float FY;
            public float FW;
            public float FH;
        }
        [System.Serializable]
        private class NET_MSG_AskEyeTrackingModuleStatus
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint SOURCE_SOCK;
            public uint SOURCE_TYPE;
        }
        [System.Serializable]
        private class NET_MSG_AnsEyeTrackingModuleStatus
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint SOURCE_SOCK;
            public uint SOURCE_TYPE;
            public uint CLIENT_UID;
            public string CLIENT_NAME;
            public uint CURRENT_CAMERA_IDX;
            public string CURRENT_CAMERA_ID;    //camera name
            public uint CURRENT_CAMERA_STATUS;  //0未知默认1打开成功2打 开失败3在打开4在关闭5 关闭 
        }
        /*
        消息格式 {   “CLIENT_TYPE”     :  xxxuintValue,        
        “SERVER_MSG”     :  73uintValue，   
        “SOURCE_SOCK”    :  xxxuintValue， 
        “SOURCE_TYPE”    :  xxxuintValue， 
        “SELECTED_ITEM”  :  “xxxxxx” stringValue}
        消息段含义 “CLIENT_TYPE”客户端类型号，设置求客户端类型 
        “SERVER_MSG”消息号，设置 73 
        “SOURCE_SOCK”求端 Socket ID，服务端求时设置 0客户端求时设置 0，服务 端转发时修改求客户端 Socket ID 
        “SOURCE_TYPE”求端 Client type，服务端求时设置 254未知客户端求时设 置自客户端类型 
        SELECTED_ITEM 为切换摄头 ID。 
        */
        [System.Serializable]
        private class NET_MSG_AskSwitchEyeTrackerModuleCamera
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint SOURCE_SOCK;
            public uint SOURCE_TYPE;
            public string SELECTED_ITEM;
        }
        /*
          消息格式  {   “CLIENT_TYPE”    :  xxxuintValue,        
          “SERVER_MSG”     :  68uintValue， 
          “SOURCE_SOCK”    :  xxxuintValue， 
          “SOURCE_TYPE”    :  xxxuintValue， 
          “SELECTED_ITEM”  :  “xxxxxx” string， 
          “STATUS”      :  xxxuintValue } 
           消息段含义 “CLIENT_TYPE”客户端类型号，设置目标客户端类型 
           “SERVER_MSG”消息号，设置 74 
           “SOURCE_SOCK”求消息消息 73中“SOURCE_SOCK”拷贝 
           “SOURCE_TYPE”求消息消息 73中“SOURCE_ TYPE”拷贝 
           “SELECTED_ITEM”为切换摄头 ID。 
           “STATUS”换状态，0人眼跟踪模块未运行1成功2失败
         */
        [System.Serializable]
        private class NET_MSG_AnsSwitchEyeTrackerModuleCamera
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint SOURCE_SOCK;
            public uint SOURCE_TYPE;
            public string SELECTED_ITEM;
            public uint STATUS;
        }
        [System.Serializable]
        private class NET_MSG_ELAC
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
            public uint ELAC_SET;
        }
        [System.Serializable]
        private class EES_MSG_ELAC_SET
        {
            public uint CLIENT_TYPE;
            public uint SERVER_MSG;
        }

        public EEServerThread()
        {
            msgInfo = new EyeTrackingRemote();
            trackingWidth = resolutionW;
            trackingHeight = resolutionH;
        }
        /// <summary>
        /// ForcePort = 0 : auto(enum between 3 presets); otherwise: only selected one
        /// </summary>
        /// <param name="ForcePort"></param>
        private void EstablishConnection(int ForcePort)
        {
            if (client != null)
            {
                client.Close();
            }
            client = new TcpClient()
            {
                ReceiveTimeout = 200,
                SendTimeout = 200
            };
            int tryCnt = 3;
            while (clientStatus == ClentStatus.Running)
            {
                int selectedPort = ETConfig.port;
                try
                {
                    if (ForcePort == 0)
                    {
                        if (ListenPortId >= ListenPorts.Length)
                        {
                            ListenPortId = 0;
                        }
                        selectedPort = ListenPorts[ListenPortId];
                        ListenPortId++;
                    }
                    client.Connect(System.Net.IPAddress.Parse(ETConfig.ipAddress), selectedPort);
                    if (clientStatus == ClentStatus.Stopped)
                    {
                        Debug.Log("EESVR connection abort");
                        client.Close();
                        client = null;
                        return;
                    }
                    serverStatus = ServerStatus.Connected;
                    if (StereoCam.enableDebugging)
                        Debug.Log("Connect succeed on port: " + selectedPort);
                }
                catch (Exception)
                {
                    if(tryCnt < 0)
                    {
                        Debug.Log("EESVR connection error on port ：" + selectedPort);
                        try
                        {
                            Thread.Sleep(5000);
                        }
                        catch { }
                    }
                    else
                    {
                        tryCnt--;

                        try
                        {
                            Thread.Sleep(100);
                        }
                        catch { }
                    }
                    continue;
                }
                break;
            }
            if (serverStatus == ServerStatus.Connected)
            {
                NET_MSG_BASE msgBase = new NET_MSG_BASE()
                {
                    CLIENT_TYPE = (uint)clientType,
                    SERVER_MSG = (uint)SERVER_MSG.SendClientType
                };
                byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msgBase));
                StreamWrite(bytes_json);
                //CheckServerStatus();
                SyncPlatformStatus();
                AskPlatformFeature();
            }
        }
        private void ThreadRun()
        {
            int i = 0;
            int portNoneOverlap = 1;
            while (i < ListenPorts.Length)
            {
                if (ListenPorts[i] == ETConfig.port)
                {
                    ListenPortId = i;
                    portNoneOverlap = 0;
                    break;
                }
                i++;
            }
            serverStatus = ServerStatus.Starting;
            clientStatus = ClentStatus.Running;
            EstablishConnection(portNoneOverlap);
            shortTimer = midTimer = longTimer = 0;
            byte[] tempBuffer = new byte[4096];
            string recvJson = "";
            CultureInfo culture = new CultureInfo("en-US");
            NET_MSG msg = new NET_MSG();
            do
            {
                if (client == null)
                    break;
                if (serverStatus == ServerStatus.Restarting && longTimer % 30 == 0)   //480ms delay to redo handshake
                {
                    //in case of socket has been connected to another fake server
                    EstablishConnection(0);
                    if (client.Connected && serverStatus == ServerStatus.Connected)
                    {
                        NET_MSG_BASE msgBase = new NET_MSG_BASE()
                        {
                            CLIENT_TYPE = (uint)clientType,
                            SERVER_MSG = (uint)SERVER_MSG.SendClientType
                        };
                        byte[] bytes_json1 = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msgBase));
                        NetworkStream ns1 = client.GetStream();
                        ns1.Write(bytes_json1, 0, bytes_json1.Length);
                    }
                }
                if (serverStatus >= ServerStatus.Connected)
                {
                    if (client.Connected)
                    {
                        NetworkStream ns = client.GetStream();
                        if (ns.DataAvailable)
                        {
                            int bytesRead = ns.Read(tempBuffer, 0, 4096);
                            recvJson += System.Text.Encoding.ASCII.GetString(tempBuffer, 0, bytesRead);
                            int startIdx = -1;
                            int endIdx = -1;
                            while (true)
                            {
                                if (endIdx == recvJson.Length - 1)
                                {
                                    break;
                                }
                                recvJson = recvJson.Substring(endIdx + 1);
                                startIdx = recvJson.IndexOf("{");
                                endIdx = recvJson.IndexOf("}");
                                if (endIdx == -1)
                                {
                                    break;
                                }
                                try
                                {
                                    string subRecvJson = recvJson.Substring(startIdx, endIdx + 1);
                                    msg = JsonUtility.FromJson<NET_MSG>(subRecvJson);
                                    switch (msg.SERVER_MSG)
                                    {
                                        case (uint)SERVER_MSG.GetInfo:  //always broadcasting! 
                                                                        //Debug.Log("GetInfo");
                                            if (Foreground)
                                            {
                                                JsonUtility.FromJsonOverwrite(subRecvJson, msgInfo);
                                                recvString = "p = " + msgInfo.PITCH + " s = " + msgInfo.SLANT + " cv = " + msgInfo.CTVIEW + " CenterEyePos = [" + 
                                                    ((msgInfo.LX + msgInfo.RX)/2).ToString("F1") + ":" + ((msgInfo.LY + msgInfo.RY)/2).ToString("F1") + ":" + ((msgInfo.LZ + msgInfo.RZ)/2).ToString("F1")
                                                    + "]\nRawData = " + subRecvJson;
                                                WP.PITCH = Convert.ToSingle(msgInfo.PITCH, culture);
                                                WP.SLANT = Convert.ToSingle(msgInfo.SLANT, culture);
                                                WP.CTVIEW = Convert.ToSingle(msgInfo.CTVIEW, culture);
                                                WP.CAMERAX = Convert.ToInt32(msgInfo.CAMERAX, culture);
                                                WP.CAMERAY = Convert.ToInt32(msgInfo.CAMERAY, culture);
                                                switch (ETConfig.trackingDevice)
                                                {
                                                    case TrackingDevice.SingleCamera:
                                                        PushEye(new Vector2(msgInfo.RX, msgInfo.RY), new Vector2(msgInfo.LX, msgInfo.LY), -1, msgInfo.FACE_NUM); 
                                                        break;
                                                    default:
                                                        PushEye(new Vector3(msgInfo.LX, msgInfo.LY, msgInfo.LZ), new Vector3(msgInfo.RX, msgInfo.RY, msgInfo.RZ), msgInfo.FACE_NUM);
                                                        break;
                                                }
                                                serverStatus = ServerStatus.Tracking;
                                            }
                                            break;
                                        case (uint)SERVER_MSG.AnsClientTypeActive:
                                            NET_MSG_BASE_ANS baseAns = new NET_MSG_BASE_ANS();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, baseAns);
                                            if (baseAns.CLIENT_TYPE == (uint)clientType)
                                                JsonUtility.FromJsonOverwrite(subRecvJson, baseAns);
                                            switch ((int)baseAns.ACTIVE_STATUS)
                                            {
                                                case -3:
                                                    recvStringStatus = " Error: Maxim eye tracking number";
                                                    //serverStatus = ServerStatus.Stopped;
                                                    break;
                                                case -2:
                                                    recvStringStatus = " Error: client type error";
                                                    //serverStatus = ServerStatus.Stopped;
                                                    break;
                                                case -1:
                                                    recvStringStatus = " Error: socket ports are not availble";
                                                    //serverStatus = ServerStatus.Stopped;
                                                    break;
                                                case 0:
                                                case 1:
                                                    recvStringStatus = " connect successed";
                                                    serverStatus = ServerStatus.Started;
                                                    SwitchET(OnOff.ON);
                                                    break;
                                                default:
                                                    break;
                                            }
                                            break;
                                        case (uint)SERVER_MSG.AnsFaceWidthParam:
                                            NET_MSG_SETTING_FACE ansFWP = new NET_MSG_SETTING_FACE();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, ansFWP);
                                            if (ansFWP.CLIENT_TYPE == (int)clientType)
                                            {
                                                faceWidth = ansFWP.FACE_WIDTH;
                                                commandBuffer[(int)SERVER_MSG.AskFaceWidthParam - 1, 1] = 0;
                                            }
                                            break;
                                        case (uint)SERVER_MSG.SendLoop:
                                            NET_MSG_BASE ansLoop = new NET_MSG_BASE();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, ansLoop);
                                            if (ansLoop.CLIENT_TYPE == (int)clientType)
                                            {
                                                NET_MSG_BASE msgBase = new NET_MSG_BASE();
                                                byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msgBase));
                                                StreamWrite(bytes_json);
                                            }
                                            break;
                                        case (uint)SERVER_MSG.AnsFaceWidth:
                                            NET_MSG_SETTING_FACE ansFW = new NET_MSG_SETTING_FACE();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, ansFW);
                                            if (ansFW.CLIENT_TYPE == (int)clientType)
                                                commandBuffer[(int)SERVER_MSG.SendFaceWidth - 1, 1] = 0;
                                            break;
                                        case (uint)SERVER_MSG.AnsWeaving:
                                            NET_MSG_SWITCH_ANS asw = new NET_MSG_SWITCH_ANS();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, asw);
                                            if (asw.ACTION_STATUS == 0 || asw.ON_OFF_OPTION == commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 0])
                                            {
                                                //OnOff3D = asw.ON_OFF_OPTION;  //use k3 instead
                                                commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 1] = 0;
                                                //Debug.Log("AnsWeaving : " + asw.ACTION_STATUS + " on_off: " + asw.ON_OFF_OPTION + " time(ms) = " + _debugSW.ElapsedMilliseconds);
                                            }
                                            shortTimer = 0;
                                            break;
                                        case (uint)SERVER_MSG.AnsK3Mode:
                                            NET_MSG_K3MODE_ANS aswk3 = new NET_MSG_K3MODE_ANS();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, aswk3);
                                            if (aswk3.STATUS == 1 || aswk3.STATUS == 0)
                                            {
                                                OnOff3D = (aswk3.MODE >= (uint)K3MODE.Bypass) ? (uint)0 : 1;
                                                LeftViewOnly = (aswk3.MODE == (uint)K3MODE.LeftViewOnly);
                                                if (aswk3.MODE == (uint)K3MODE.MultiviewTB || aswk3.MODE == (uint)K3MODE.Multiview2DZ)
                                                {
                                                    MultiViewSwitchDelayPatch = 0;
                                                }
                                                commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 1] = 0;
                                            }
                                            else if (aswk3.STATUS == 2)
                                            {
                                                //patch: avoid thread hang when switching to not supported modes, assume OnOff3D always set succeed
                                                if (aswk3.MODE == (uint)K3MODE.MultiviewTB || aswk3.MODE == (uint)K3MODE.Multiview2DZ)
                                                {
                                                    commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 1] = 0;
                                                    MultiViewSwitchDelayPatch = -1;
                                                    OnOff3D = commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 0];
                                                }
                                                else
                                                {
                                                    OnOff3D = 1 - commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 0];
                                                }
                                            }

                                            if (StereoCam.enableDebugging)
                                            {
                                                Debug.Log(" AnsK3Mode K3Mode = " + (K3MODE)aswk3.MODE + " OnOff3D: " + OnOff3D + " aswk3.STATUS = " + aswk3.STATUS + " buffer = " + commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 1] + " MultiViewSwitchDelayPatch = " + MultiViewSwitchDelayPatch + " time(ms) = " + _debugSW.ElapsedMilliseconds);
                                                _debugSW.Stop();
                                            }
                                            shortTimer = 0;
                                            break;
                                        case (uint)SERVER_MSG.AnsEnableTracking:
                                            NET_MSG_SWITCH_ANS asw1 = new NET_MSG_SWITCH_ANS();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, asw1);
                                            if (asw1.ACTION_STATUS == 0)
                                            {
                                                OnOffET = commandBuffer[(int)SERVER_MSG.AskEnableTracking - 1, 0];
                                            }
                                            //Debug.Log("AnsEnableTracking : " + asw1.ACTION_STATUS + " OnOffET:" + OnOffET + " data = " + asw1.ON_OFF_OPTION);
                                            shortTimer = 0;
                                            break;
                                        case (uint)SERVER_MSG.AnsServerStatus:
                                            NET_MSG_SERVER_STATUS asw2 = new NET_MSG_SERVER_STATUS();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, asw2);
                                            ProtocalVersion = asw2.PROTOCOL_VER;
                                            if (ProtocalVersion < 3)
                                            {
                                                uint onOffET = (uint)(asw2.ON_OFF_EYETRACK == 2 ? 0 : 1);
                                                uint onOff3D = (uint)(asw2.ON_OFF_3DFILM == 2 ? 0 : 1);
                                                //Debug.Log("ON_OFF_EYETRACK: " + onOffET + " OnOffET : " + OnOffET + " ON_OFF_3DFILM : " + onOff3D + " OnOff3D :" + OnOff3D + " serverStatus : " + serverStatus + " " + midTimer);
                                                //Debug.LogError("ON_OFF_EYETRACK: " + onOffET + " OnOffET : " + OnOffET + " ON_OFF_3DFILM : " + onOff3D + " OnOff3D :" + OnOff3D + " serverStatus : "+ serverStatus +  " " + midTimer);
                                                if (Foreground)
                                                {
                                                    if (commandBuffer[(int)SERVER_MSG.AskEnableTracking - 1, 0] != onOffET)
                                                    {
                                                        //Debug.Log("AnsServerStatus " + onOffET + " ProtocalVersion = " + ProtocalVersion);
                                                        switchET(commandBuffer[(int)SERVER_MSG.AskEnableTracking - 1, 0]);
                                                    }
                                                    if (OnOff3D != onOff3D)
                                                    {
                                                        switch3D(onOff3D);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                SyncPlatformStatus();
                                            }
                                            //Debug.Log("ProtocalVersion " + ProtocalVersion + " " + asw2.EESERVER_VER);
                                            break;
                                        case (uint)SERVER_MSG.AnsPlatformFeature:
                                            NET_MSG_PLATFORM_FEATURE asw7 = new NET_MSG_PLATFORM_FEATURE();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, asw7);
                                            uint FilmType = asw7.FEATURE_0 & 0xF;   //2: slant
                                            uint ViewNumber = (asw7.FEATURE_0 & 0xF0) >> 4; //2: multiview with eye tracking
                                            uint K3Support = (asw7.FEATURE_0 & 0xF000) >> 12;   //1: with K3
                                            uint K3ModeByBit = (asw7.FEATURE_0 & 0xF0000) >> 16;    //bit0:tb to multiview; bit1: SBS; bit2: 2dz to multiview; bit3: mono
                                            filmType = (FILMTYPE)FilmType;
                                            viewNumber = (VIEWNUMBER)ViewNumber;
                                            k3Support = (K3SUPPORT)K3Support;
                                            platformFeatures.K3Valid = (k3Support == K3SUPPORT.True);
                                            platformFeatures.K3ModeLicenseTB = (K3ModeByBit & 1) > 0;
                                            platformFeatures.K3ModeLicenseSBS = (K3ModeByBit & 2) > 0;
                                            platformFeatures.K3ModeLicense2DZ = (K3ModeByBit & 4) > 0;
                                            platformFeatures.K3ModeLicenseMono = (K3ModeByBit & 8) > 0;
                                            if (ETConfig.PFNotify != null)
                                                ETConfig.PFNotify(platformFeatures);
                                            //Debug.Log("AnsPlatformFeature FilmType = " + FilmType + " ViewNumber =  " + ViewNumber + " K3Ready = " + k3Support + " K3ModeByBit = " + K3ModeByBit);
                                            break;
                                        case (uint)SERVER_MSG.AnsPlatformStatus:
                                            NET_MSG_PLATFORM_STATUS asw3 = new NET_MSG_PLATFORM_STATUS();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, asw3);
                                            if ((uint)clientType == asw3.CLIENT_TYPE)
                                            {
                                                K3Enabled = (asw3.STATUS_0 & 0xF) > 0 ? (uint)1 : 2;
                                                uint K3Mode = (asw3.STATUS_0 & 0xFF00) >> 8;
                                                if (K3Mode == 0xFF)
                                                    K3Enabled = 0;
                                                uint Eyetrack = (asw3.STATUS_0 & 0xF0000000) >> 28;
                                                OnOffET = (uint)(Eyetrack == 5 ? 1 : 0);
                                                uint Film = ((asw3.STATUS_0 & 0xF000000) >> 24) == 5 ? (uint)1 : 0;
                                                if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Slant ||
                                                    ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SlantWithLDC ||
                                                    ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Slant9Views ||
                                                    ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SlantWithXYZ || MultiViewSwitchDelayPatch < 0)
                                                {
                                                    OnOff3D = Film;
                                                }
                                                else
                                                {
                                                    OnOff3D = (K3Mode >= (uint)K3MODE.Bypass) ? (uint)0 : 1;
                                                    LeftViewOnly = (K3Mode == (uint)K3MODE.LeftViewOnly);
                                                }
                                                PID = (PlatformID)(asw3.STATUS_1 & 0xFF);
                                                ETModuleID = (EyeTrackerModule)((asw3.STATUS_1 & 0xFF00) >> 8);
                                                if (true && Foreground) // comment out Foreground: make sure everything is synced even if minimized or run in background
                                                {
                                                    //CheckEyeTrackingModuleStatus();
                                                    if (EnableSyncStatus && commandBuffer[(int)SERVER_MSG.AskEnableTracking - 1, 0] != OnOffET)
                                                    {
                                                        //Debug.Log("AnsPlatformStatus " + OnOffET);
                                                        switchET(commandBuffer[(int)SERVER_MSG.AskEnableTracking - 1, 0]);
                                                    }
                                                    if (EnableSyncStatus && commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 0] != Film)
                                                    {
                                                        switch3D(commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 0]);
                                                    }
                                                }

                                                if (StereoCam.enableDebugging)
                                                    Debug.Log("AnsPlatformStatus " + (K3MODE)K3Mode + " Film =  " + Film + " OnOff3D = " + OnOff3D + " OnOffET = " + OnOffET + " clientType = " + clientType + " K3Enabled = " + K3Enabled);
                                            }
                                            break;
                                        case (uint)SERVER_MSG.AnsSwitchEyeTrackerModuleCamera:
                                            NET_MSG_AnsSwitchEyeTrackerModuleCamera asw5 = new NET_MSG_AnsSwitchEyeTrackerModuleCamera();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, asw5);
                                            if (asw5.STATUS != 1)
                                            {
                                                ;
                                            }
                                            break;
                                        case (uint)SERVER_MSG.AnsEyeTrackingModuleStatus:
                                            NET_MSG_AnsEyeTrackingModuleStatus asw6 = new NET_MSG_AnsEyeTrackingModuleStatus();
                                            JsonUtility.FromJsonOverwrite(subRecvJson, asw6);
                                            //Debug.Log("AnsEyeTrackingModuleStatus" + asw6.CURRENT_CAMERA_ID);   //why no message received?
                                            if (asw6.CURRENT_CAMERA_ID != CameraModuleNameSamsung && (uint)clientType == asw6.CLIENT_TYPE)
                                            {
                                                //patch: manually switch to eyetracking camera
                                                if (PID == 0 && ETModuleID == EyeTrackerModule.WZOpenCV)
                                                {
                                                    switchETModule(CameraModuleNameSamsung);
                                                }
                                            }
                                            break;
                                        case (uint)SERVER_MSG.NotifyPlatformStatus:
                                            if (Foreground)
                                            {
                                                NET_MSG_PLATFORM_STATUS_NOTIFY asw4 = new NET_MSG_PLATFORM_STATUS_NOTIFY();
                                                JsonUtility.FromJsonOverwrite(subRecvJson, asw4);
                                                K3Enabled = (asw4.STATUS_0 & 0xF) > 0 ? (uint)1 : 2;
                                                uint K3Mode = (asw4.STATUS_0 & 0xFF00) >> 8;
                                                if (K3Mode == 0xFF)
                                                    K3Enabled = 0;
                                                uint Eyetrack = (asw4.STATUS_0 & 0xF0000000) >> 28;
                                                OnOffET = (uint)(Eyetrack == 5 ? 1 : 0);
                                                uint Film = ((asw4.STATUS_0 & 0xF000000) >> 24) == 5 ? (uint)1 : 0;
                                                if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Slant ||
                                                    ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SlantWithLDC ||
                                                    ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Slant9Views ||
                                                    ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SlantWithXYZ || MultiViewSwitchDelayPatch < 0)
                                                {
                                                    OnOff3D = Film;
                                                }
                                                else
                                                {
                                                    OnOff3D = (K3Mode >= (uint)K3MODE.Bypass) ? (uint)0 : 1;
                                                    LeftViewOnly = (K3Mode == (uint)K3MODE.LeftViewOnly);
                                                }
                                                recvStringStatus = "EEServer | Film:" + Film + " K3Enabled:" + K3Enabled + " Multiview Support:" + k3ModeLicense2DZ + " K3Mode:" + (K3MODE)K3Mode + " Eyetrack:" + Eyetrack;

                                                if (commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 0] != Film)
                                                {
                                                    switch3D(commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 0]);
                                                }

                                                if (StereoCam.enableDebugging)
                                                    Debug.Log(" NotifyPlatformStatus K3Mode = " + (K3MODE)K3Mode + " Film = " + Film + " OnOff3D = " + OnOff3D + " OnOffET = " + OnOffET + " K3Enabled = " + K3Enabled);
                                            }
                                            break;
                                        default:
                                            //Debug.Log(msg.SERVER_MSG);
                                            break;
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    Console.WriteLine("recvJson warning：" + ex.Message);
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(16);
                shortTimer++;
                longTimer++;
                if (serverStatus >= ServerStatus.Started)
                {
                    if (shortTimer > 15) //base 16x15=240ms
                    {
                        if (commandBuffer[(int)SERVER_MSG.AskEnableTracking - 1, 1] == 1)
                        {
                            switchET(commandBuffer[(int)SERVER_MSG.AskEnableTracking - 1, 0]);
                            commandBuffer[(int)SERVER_MSG.AskEnableTracking - 1, 1] = 0;    //one shot
                        }
                        if (commandBuffer[(int)SERVER_MSG.SendFaceWidth - 1, 1] == 1)
                        {
                            setFaceWidth(faceWidth);
                        }
                        if (commandBuffer[(int)SERVER_MSG.AskPreview - 1, 1] == 1)
                        {
                            switchPreview(1);
                            commandBuffer[(int)SERVER_MSG.AskPreview - 1, 1] = 0;   //no reply
                        }
                        if (commandBuffer[(int)SERVER_MSG.SendInfo - 1, 1] == 1)
                        {
                            eyeTrackingSendInfo();
                            commandBuffer[(int)SERVER_MSG.SendInfo - 1, 1] = 0;   //no reply
                        }
                        shortTimer = 0; 
                        lock (timerLock)
                        {
                            midTimer++;
                        }
                    }

                    if (midTimer > 4) //base 240x4=960ms
                    {
                        if (commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 1] == 1)
                        {
                            switch3D(commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 0]);
                            //Debug.Log("midTimer switch film = " + commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 0] + " time(ms) = " + _debugSW.ElapsedMilliseconds);
                        }
                        if ((K3Enabled > 0) && (commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 1] == 1) && MultiViewSwitchDelayPatch >= 0)
                        {
                            switchK3(commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 0]);
                            //Debug.Log("midTimer switch K3 = " + commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 0] + " time(ms) = " + _debugSW.ElapsedMilliseconds);
                        }
                        lock (timerLock)
                        {
                            midTimer = 0;
                        }
                    }
                }
                if (longTimer > 625 && Foreground)       //base 16x625=10s
                {
                    if (ProtocalVersion < 3)
                    {
                        CheckServerStatus();
                    }
                    else
                    {
                        SyncPlatformStatus();
                        if (K3Enabled < 2)
                            AskPlatformFeature();
                    }
                    longTimer = 0;
                }
            }
            while (clientStatus == ClentStatus.Running);
            //Debug.Log("exit");
            if(client != null)
            {
                client.Close();
            }
            client = null;
        }

        public override int InitET()
        {
            clientType = (ClientType)ETConfig.clientType;
            Array.Clear(commandBuffer, 0, commandBuffer.Length);
            platformFeatures = new PlatformFeatures();
            thread = new Thread(new ThreadStart(ThreadRun));
            _debugSW = new System.Diagnostics.Stopwatch();
            thread.Start();
            return 0;
        }
        public override ServerStatus GetServerStatus()
        {
            return serverStatus;
        }
        public override int SwitchET(OnOff onOff)
        {
            //Debug.Log("SwitchET start = " + onOff);
            int ret = -1;
            //if (serverStatus == EyeTracking.ServerStatus.Unkown)
            //    return ret;
            if (serverStatus >= ServerStatus.Started)
            {
                commandBuffer[(uint)SERVER_MSG.AskEnableTracking - 1, 0] = (uint)onOff;
                commandBuffer[(uint)SERVER_MSG.AskEnableTracking - 1, 1] = 1;
                EnableSyncStatus = true;
                ret = 0;
            }
            else
            {
                Debug.Log("SwitchET failure, serverStatus = " + serverStatus);
            }
            return ret;
        }
        private OnOff lastOnOffCommand = OnOff.OFF;
        private uint lastConfig = (uint)StereoCam.AutoStereoShader.Mono;
        /// <summary>
        /// Switch film and Hardware weaving
        /// </summary>
        /// <param name="onOff"></param>
        /// <returns> 1 : success with both film and hardware weaving; 0: success with only film; -1: waiting for last command complete; -2: feature not available </returns>
        public override int Switch3D(OnOff onOff)
        {
            int ret = -1;
            if (serverStatus >= ServerStatus.Started)
            {
                //filter out duplicated commands and leave it to auto sync
                if(lastOnOffCommand == onOff && lastConfig == ETConfig.preserved0 )
                {
                    if(commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 1] == 1)
                    {
                        return ret;
                    }else if(commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 1] == 1){

                        return ret;
                    }
                }
                else
                {
                    lastOnOffCommand = onOff;
                    lastConfig = ETConfig.preserved0;
                }
                commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 0] = (uint)onOff;
                commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 1] = 1;
                lock (timerLock)
                {
                    midTimer = 0;
                }

                if (StereoCam.enableDebugging)
                {
                    _debugSW.Reset();
                    _debugSW.Start();
                    Debug.Log("Switch3D " + onOff + " k3Support = " + k3Support + " time(ms) = " + _debugSW.ElapsedMilliseconds);
                }
                switch3D((uint)onOff);
                //Debug.Log("AskWeaving commandBuffer added " + onOff);
                ret++;
                if (K3Enabled > 0)
                {
                    commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 0] = (uint)onOff;
                    commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 1] = 1;
                    if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Row ||
                        ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Column ||
                        ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Checkerboard ||
                        ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Mono ||
                        ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.MultiView)
                    {
                        commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 0] = 0;
                    }
                    else
                    {
                        MultiViewSwitchDelayPatch = 0;
                    }
                    lock (timerLock)
                    {
                        midTimer = 0;
                    }
                    int result = switchK3(commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 0]);
                    if(result < 0)
                    {
                        ret = result;
                        commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 0] = 0;
                    }
                    else
                    {
                        ret++;
                    }
                    //Debug.Log("switch k3 done " + commandBuffer[(int)SERVER_MSG.SetK3Mode - 1, 0] + " time(ms) = " + _debugSW.ElapsedMilliseconds);
                }
            }
            else
            {
                Debug.Log("Switch3D failure, serverStatus = " + serverStatus);
            }
            return ret;
        }
        public override int SetFaceWidth(float FW)
        {
            if (K3Enabled > 0)
            {
                faceWidth = FW;
                commandBuffer[(uint)SERVER_MSG.SendFaceWidth - 1, 1] = 1;
                return 0;
            }
            else
            {
                return -1;
            }
        }
        public override int SwitchPreview(int onOff)
        {
            commandBuffer[(uint)SERVER_MSG.AskPreview - 1, 1] = 1;
            return 0;
        }
        public override int SendInfo(float Lx, float Ly, float Lz, float Rx, float Ry, float Rz)
        {
            InfoLx = Lx;
            InfoLy = Ly;
            InfoLz = Lz;
            InfoRx = Rx;
            InfoRy = Ry;
            InfoRz = Rz;
            commandBuffer[(uint)SERVER_MSG.SendInfo - 1, 1] = 1;
            return 0;
        }
        public override float GetFaceWidth()
        {
            if (K3Enabled > 0)
            {
                askFaceWidth();
                while (commandBuffer[(int)SERVER_MSG.SendFaceWidth - 1, 1] == 0)
                {
                    Thread.Sleep(100);
                }
            }
            return faceWidth;
        }
        private void CheckServerStatus()
        {
            NET_MSG_BASE msgBase = new NET_MSG_BASE()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.AskServerStatus
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msgBase));
            StreamWrite(bytes_json);
        }
        private void CheckEyeTrackingModuleStatus()
        {
            NET_MSG_AskEyeTrackingModuleStatus msgBase = new NET_MSG_AskEyeTrackingModuleStatus()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.AskEyeTrackingModuleStatus,
                SOURCE_SOCK = 0,
                SOURCE_TYPE = (uint)clientType
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msgBase));
            StreamWrite(bytes_json);
            //Debug.Log("CheckEyeTrackingModuleStatus sent");
        }

        /// <summary>
        /// THIS FUNCTION WILL CHECK IF CLIENT IS STILL CONNECTED WITH SERVER.
        /// </summary>
        /// <returns>FALSE IF NOT CONNECTED ELSE TRUE</returns>
        public bool isClientConnected()
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation c in tcpConnections)
            {
                TcpState stateOfConnection = c.State;
                if (c.LocalEndPoint.Equals(client.Client.LocalEndPoint) && c.RemoteEndPoint.Equals(client.Client.RemoteEndPoint))
                {
                    if (stateOfConnection == TcpState.Established)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }
        private void AskPlatformFeature()
        {
            NET_MSG_BASE msgBase = new NET_MSG_BASE()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.AskPlatformFeature
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msgBase));
            StreamWrite(bytes_json);
        }
        private void SyncPlatformStatus()
        {
            NET_MSG_BASE msgBase = new NET_MSG_BASE()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.AskPlatformStatus
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msgBase));
            StreamWrite(bytes_json);
        }
        private void StreamWrite(byte[] data)
        {
            if(client.Connected)
            {
                try
                {
                    NetworkStream ns = client.GetStream();
                    ns.Write(data, 0, data.Length);
                }
                catch (System.Exception ex)
                {
                    Debug.Log("StreamWrite failure, restarting... : " + ex.Message);
                    serverStatus = ServerStatus.Restarting;
                }
            }
        }
        private int switchET(uint onOff)
        {
            NET_MSG_SWITCH msg = new NET_MSG_SWITCH()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.AskEnableTracking,
                ON_OFF_OPTION = onOff
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
            StreamWrite(bytes_json);
            if (StereoCam.enableDebugging)
                Debug.Log(" switchET sent = " + onOff + " time(ms) = " + _debugSW.ElapsedMilliseconds);
            return 0;
        }
        private int switchETModule(string name)
        {
            NET_MSG_AskSwitchEyeTrackerModuleCamera msg = new NET_MSG_AskSwitchEyeTrackerModuleCamera()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.AskSwitchEyeTrackerModuleCamera,
                SOURCE_SOCK = 0,
                SOURCE_TYPE = (uint)clientType,
                SELECTED_ITEM = name
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
            StreamWrite(bytes_json);
            return 0;
        }
        /// <summary>
        /// switch K3 hardware
        /// </summary>
        /// <param name="onOff"></param>
        /// <returns>-2: feature not supported, -1: error happened, 0: success </returns>
        private int switchK3(uint onOff)
        {
            uint mode = (uint)K3MODE.Bypass;
            uint option = 2;
            uint is2d = 0;
            if (onOff > 0)
            {
                if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SideBySide)
                {
                    if (!k3ModeLicenseSBS)
                        return -2;
                    MultiViewSwitchDelayPatch = 0;
                    mode = (uint)K3MODE.TwoViewSBS;
                }
                else if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.TopBottom)
                {
                    if (!k3ModeLicenseTB)
                        return -2;
                    //need time to response
                    mode = (uint)K3MODE.MultiviewTB;
                    option = 6;
                }
                else if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SideBySideDepth || ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SideBySideDepthBH)
                {
                    if (!k3ModeLicense2DZ)
                        return -2;
                    //need time to response
                    mode = (uint)K3MODE.Multiview2DZ;
                    option = 6;
                }
                else
                {
                    MultiViewSwitchDelayPatch = -2;
                }
                //can use is2d to do screen space 2d(left view) switch in 3d mode,however, the resolution is half in width.
                //is2d = 1; option |= 1 << 3;
            }
            if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Slant || ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SlantWithLDC ||
                ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.Slant9Views || ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SlantWithXYZ)
            {
                if (!k3ModeLicenseMono)
                    return -2;
                MultiViewSwitchDelayPatch = 0;
                mode = (uint)K3MODE.Bypass;
                option = 2;
            }
            if (MultiViewSwitchDelayPatch >= 0 || mode == (uint)K3MODE.Bypass)
            {
                NET_MSG_K3MODE msg = new NET_MSG_K3MODE()
                {
                    CLIENT_TYPE = (uint)clientType,
                    SERVER_MSG = (uint)SERVER_MSG.SetK3Mode,
                    OPTION = option,
                    MODE = mode,
                    K3ENABLE = 0,   //0: enable
                    FOCUS_DEPTH = ETConfig.preserved1,    //0~0x3f adjust through menu
                    IS2D = is2d,
                    BYPASSFILMSWITCH = 1
                };
                byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
                StreamWrite(bytes_json);
                //patch for AnsK3Mode wait too long to receive, make it one shot
                if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.TopBottom || ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SideBySideDepth || ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SideBySideDepthBH)
                {
                    MultiViewSwitchDelayPatch = -1;
                }
                if (StereoCam.enableDebugging)
                    Debug.Log(DateTime.Now.ToString(" yyyy-MM-dd HH:mm:ss:fff") + " switch mode = " + (K3MODE)mode + " option = " + option + " is2d = " + is2d + " time(ms) = " + _debugSW.ElapsedMilliseconds);
                return 0;
            }
            return -1;
        }
        private int switch3D(uint onOff)
        {
            int FeatureSupport = 0;
            if(ETConfig.preserved0 != (uint)StereoCam.AutoStereoShader.Anaglyph && ETConfig.preserved0 != (uint)StereoCam.AutoStereoShader.Column && 
                ETConfig.preserved0 != (uint)StereoCam.AutoStereoShader.Row && ETConfig.preserved0 != (uint)StereoCam.AutoStereoShader.Checkerboard)
            {
                NET_MSG_SWITCH msg = new NET_MSG_SWITCH()
                {
                    CLIENT_TYPE = (uint)clientType,
                    SERVER_MSG = (uint)SERVER_MSG.AskWeaving,
                    ON_OFF_OPTION = onOff,
                    CONFIG = 0,
                    DELAY_TIME = 0
                };
                byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
                StreamWrite(bytes_json);
                if(StereoCam.enableDebugging)
                    Debug.Log(" switch film = " + onOff + " time(ms) = " + _debugSW.ElapsedMilliseconds);
            }
            else
            {
                FeatureSupport = -1;
            }
            if (FeatureSupport < 0)
            {
                commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 1] = 0;       //just ignore checking
                OnOff3D = commandBuffer[(int)SERVER_MSG.AskWeaving - 1, 0];
            }
            return FeatureSupport;
        }
        private int switchPreview(uint onOff)
        {
            NET_MSG_BASE msg = new NET_MSG_BASE()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.AskPreview
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
            StreamWrite(bytes_json);
            return 0;
        }
        private int eyeTrackingSendInfo()
        {
            EyeTrackingLocal msg = new EyeTrackingLocal()
            {
                CLIENT_TYPE = (uint)ClientType.EyeTrackingModule,
                SERVER_MSG = (uint)SERVER_MSG.SendInfo,
                FACE_NUM = 1,
                LX = InfoLx,
                LY = InfoLy,
                LZ = InfoLz,
                RX = InfoRx,
                RY = InfoRy,
                RZ = InfoRz,
                FW = faceWidth
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
            StreamWrite(bytes_json);
            //Debug.Log("send: " + InfoLx + " " + InfoLy);
            return 0;
        }
        private int setFaceWidth(float width)
        {
            NET_MSG_SETTING_FACE msg = new NET_MSG_SETTING_FACE()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.SendFaceWidth,
                FACE_WIDTH = width,
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
            StreamWrite(bytes_json);
            return 0;
        }
        private int askFaceWidth()
        {
            commandBuffer[(int)SERVER_MSG.AskFaceWidthParam - 1, 1] = 1;
            NET_MSG_BASE msg = new NET_MSG_BASE()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.AskFaceWidthParam,
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
            StreamWrite(bytes_json); 
            return 0;
        }
        public override string GetInfo()
        {
            return recvStringStatus + " " +  recvString;
        }
        public override int ShutdownET(bool BypassSwitch2D)
        {
            //force close screen
            if (serverStatus >= ServerStatus.Started)
            {
                if(!BypassSwitch2D)
                {
                    if (OnOffET == 1)
                        switchET(0);
                    if (OnOff3D == 1)
                    {
                        switch3D(0);
                        if (K3Enabled > 0)
                            switchK3(0);
                    }
                }
                clientStatus = ClentStatus.Stopped;
            }
            else
            {
                clientStatus = ClentStatus.Stopped;
                thread.Interrupt();
                if (StereoCam.enableDebugging)
                    Debug.Log("Force Abort eesvr reconnect ");
            }
            return 0;
        }
        public override int GetFaceNum()
        {
            return msgInfo.FACE_NUM;
        }

        public override void SetK3Depth()
        {
            uint option = 6;
            uint mode;
            if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.TopBottom)
            {
                mode = 0;
            }else if (ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SideBySideDepth || ETConfig.preserved0 == (uint)StereoCam.AutoStereoShader.SideBySideDepthBH)
            {
                mode = 2;
            }
            else
            {
                return;
            }
            NET_MSG_K3MODE msg = new NET_MSG_K3MODE()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.SetK3Mode,
                OPTION = option,
                MODE = mode,
                K3ENABLE = 0,
                FOCUS_DEPTH = ETConfig.preserved1,
                IS2D = 0,
                BYPASSFILMSWITCH = 1
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
            StreamWrite(bytes_json);
        }

        public override uint IsK3Enabled()
        {
            return K3Enabled;
        }
        public override PlatformID GetPID()
        {
            return PID;
        }
        public override void SetAuto2D(bool param)
        {
            NET_MSG_ELAC msg = new NET_MSG_ELAC()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.AskAuto2DStatus,
                ELAC_SET = (param == true) ? (uint)1 : 0,
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
            StreamWrite(bytes_json);
            if (StereoCam.enableDebugging)
                Debug.Log("ELAC_SET = " + param);
        }
        public override void ReleaseELACControl()
        {
            EES_MSG_ELAC_SET msg = new EES_MSG_ELAC_SET()
            {
                CLIENT_TYPE = (uint)clientType,
                SERVER_MSG = (uint)SERVER_MSG.ReleaseELACControl,
            };
            byte[] bytes_json = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
            StreamWrite(bytes_json);

            if (StereoCam.enableDebugging)
                Debug.Log("ReleaseELACControl");
        }

    }
}
