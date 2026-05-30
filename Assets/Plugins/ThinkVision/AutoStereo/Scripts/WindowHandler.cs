using AOT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Windows apis
/// All Rights reserved
/// </summary>
public class WindowHandler 
{
#region Enums
	enum WindowStyle : uint
	{
		BORDER			= 0x00800000,
		CAPTION			= 0x00C00000,
		CHILD			= 0x40000000,
		CHILDWINDOW		= 0x40000000,
		CLIPCHILDREN	= 0x02000000,
		CLIPSIBLINGS	= 0x04000000,
		DISABLED		= 0x08000000,
		DLGFRAME		= 0x00400000,
		GROUP			= 0x00020000,
		HSCROLL			= 0x00100000,
		ICONIC			= 0x20000000,
		MAXIMIZE		= 0x01000000,
		MAXIMIZEBOX		= 0x00010000,
		MINIMIZE		= 0x20000000,
		MINIMIZEBOX		= 0x00020000,
		OVERLAPPED		= 0x00000000,
		POPUP			= 0x80000000,
		SIZEBOX			= 0x00040000,
		SYSMENU			= 0x00080000,
		TABSTOP			= 0x00010000,
		THICKFRAME		= 0x00040000,
		TILED			= 0x00000000,
		VISIBLE			= 0x10000000,
		VSCROLL			= 0x00200000
	}
	enum ExtendedWindowStyle : uint
	{
		ACCEPTFILES			= 0x00000010,
		APPWINDOW			= 0x00040000,
		CLIENTEDGE			= 0x00000200,
		COMPOSITED			= 0x02000000,
		CONTEXTHELP			= 0x00000400,
		CONTROLPARENT		= 0x00010000,
		DLGMODALFRAME		= 0x00000001,
		LAYERED				= 0x00080000,
		LAYOUTRTL			= 0x00400000,
		LEFT				= 0x00000000,
		LEFTSCROLLBAR		= 0x00004000,
		LTRREADING			= 0x00000000,
		MDICHILD			= 0x00000040,
		NOACTIVATE			= 0x08000000,
		NOINHERITLAYOUT		= 0x00100000,
		NOPARENTNOTIFY		= 0x00000004,
		NOREDIRECTIONBITMAP	= 0x00200000,
		RIGHT				= 0x00001000,
		RIGHTSCROLLBAR		= 0x00000000,
		RTLREADING			= 0x00002000,
		STATICEDGE			= 0x00020000,
		TOOLWINDOW			= 0x00000080,
		TOPMOST				= 0x00000008,
		TRANSPARENT			= 0x00000020,
		WINDOWEDGE			= 0x00000100
	}

	public enum ZOrder
	{
		/// <summary>
		/// 让窗口变为当时的最顶层，相当于给窗口设置了一个"置顶"标志，
		/// 与其他有这个标志的窗口竞争最顶层的位置（鼠标点击可切换哪个窗口成为当时的最顶层），
		/// 所有带这个标志的窗口处在所有不带这个标志的窗口的上面，离用户更近。
		/// </summary>
		TopMost = -1,

		/// <summary>
		/// 取消窗口的"置顶"标志，于是这个窗口就变成了普通窗口，置顶窗口们就不和它一起玩了，它之后便和其他普通窗口一桌竞争了。
		/// 这个设置只对本来就是置顶窗口的窗口有用，对普通窗口没效果。
		/// </summary>
		NoTopMost = -2,

		/// <summary>
		/// 将窗口移动到普通窗口的顶部，依然处在置顶窗口们的下面，依然是普通窗口，不会一直待在顶部，会在以后鼠标点来点去的时候跑到其他窗口下面。
		/// </summary>
		Top = 0,

		/// <summary>
		/// 将窗口移动到普通窗口的底部。其他与Top同理。
		/// </summary>
		Bottom = 1,
	}
	enum SwpFlag : uint
	{
		ASYNCWINDOWPOS	= 0x4000,
		DEFERERASE		= 0x2000,
		DRAWFRAME		= 0x0020,
		FRAMECHANGED	= 0x0020,
		HIDEWINDOW		= 0x0080,
		NOACTIVATE		= 0x0010,
		NOCOPYBITS		= 0x0100,
		NOMOVE			= 0x0002,
		NOOWNERZORDER	= 0x0200,
		NOREDRAW		= 0x0008,
		NOREPOSITION	= 0x0200,
		NOSENDCHANGING	= 0x0400,
		NOSIZE			= 0x0001,
		NOZORDER		= 0x0004,
		SHOWWINDOW		= 0x0040
	}
    /// <summary>
    /// Win32 API Constants for ShowWindowAsync()
    /// see https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-showwindow
    /// </summary>
    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int SW_SHOW = 5;
    public const int SW_MINIMIZE = 6;
    public const int SW_SHOWMINNOACTIVE = 7;
    public const int SW_SHOWNA = 8;
    private const int SW_RESTORE = 9;
    private const int SW_SHOWDEFAULT = 10;
    private const int SW_FORCEMINIMIZE = 11;
	#endregion
#region fields
	/// <summary>
	/// The window's position.
	/// </summary>
	/// <param name="X">The x position of the window.</param>
	/// <param name="Y">The y position of the window.</param>
	public static Vector2 position;
	/// <summary>
		/// The window's width.
		/// </summary>
	public static int width;
	/// <summary>
	/// The window's height.
	/// </summary>
	public static int height;
	/// <summary>
	/// The color of pixels to cull if trancparency is enabled.
	/// </summary>
	public static int colorkey = 0x00000000;
	static int windowOpacity = 100;
	static bool stayOnTop = true;
	static bool showTaskbarIcon = true;
	static bool canClickThrough = false;
	static bool transparentWindow = false;
    static bool noBorder = false;
    static int windowHandle;
    static uint swpFlags;
    static long lWsStyle, lExWsStyle;
	static DisplayInfoCollection DIC;
	struct point
	{
		public int x;
		public int y;
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct windowRect
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}
	public class DisplayInfo
	{
		public string Availability { get; set; }
		public string ScreenHeight { get; set; }
		public string ScreenWidth { get; set; }
		public string displayName { get; set; }
		public int displayID { get; set; }
		public windowRect MonitorArea { get; set; }
		public windowRect WorkArea { get; set; }
	}
	public class MyMonitor
	{
		public int targetX;
		public int targetY;
		public int monitorNumber;
		public int height;
		public int width;
		public int id;
		public string name;

		public MyMonitor(int targetX, int targetY, int monitorNumber, int height, int width, int id, string displayName)
		{
			this.targetX = targetX;
			this.targetY = targetY;
			this.monitorNumber = monitorNumber;
			this.height = height;
			this.width = width;
			this.id = id;
			this.name = displayName;
		}
	}
	public class DisplayInfoCollection : List<DisplayInfo>
	{
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
	public class MONITORINFOEX
	{
		public int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
		public windowRect rcMonitor = new windowRect();
		public windowRect rcWork = new windowRect();
		public int dwFlags = 0;
		[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 32)]
		public char[] szDevice = new char[32];
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct POINTSTRUCT
	{
		public int x;
		public int y;
		public POINTSTRUCT(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
	}
	[StructLayout(LayoutKind.Sequential)]
    public struct WINDOWINFO
    {
        public uint cbSize;
        public windowRect rcWindow;
        public windowRect rcClient;
        public uint dwStyle;
        public uint dwExStyle;
        public uint dwWindowStatus;
        public uint cxWindowBorders;
        public uint cyWindowBorders;
        public ushort atomWindowType;
        public ushort wCreatorVersion;

        public WINDOWINFO(bool? filler)
        : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
        {
            cbSize = (uint)(Marshal.SizeOf(typeof(WINDOWINFO)));
        }

    }
	public static class SystemNotification
	{
		private const uint SPI_SETMESSAGEDURATION = 0x2017; private const int SPIF_SENDCHANGE = 0x2;
		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
		public static void DisableSystemNotification()
		{
			Console.WriteLine("DisableSystemNotification = " + SystemParametersInfo(SPI_SETMESSAGEDURATION, 0, new IntPtr(0), SPIF_SENDCHANGE));
		}
		public static void EnableSystemNotification()
		{
			Console.WriteLine("EnableSystemNotification = " + SystemParametersInfo(SPI_SETMESSAGEDURATION, 0, new IntPtr(15), SPIF_SENDCHANGE));
		}
	}
	// Use this for initialization
	static WindowHandler()
	{
		//SystemNotification.DisableSystemNotification(); //not always work
		// Grab window handle
#if UNITY_EDITOR
		windowHandle = GetActiveWindow();
#else
        windowHandle = FindWindow("UnityWndClass", Application.productName);
#endif
		//Process prc = Process.GetCurrentProcess();
		//windowHandle = (int)prc.MainWindowHandle;

		// Grab windows styles
		//lWsStyle = GetWindowLong( windowHandle, -16 );

		// Grab windows styles
		//lExWsStyle  = 0;

		GetWindowInfo1();
	}
    public static IntPtr GetWindowHandle()
    {
        IntPtr value = (IntPtr)windowHandle;
        return value;
    }
    public static IntPtr GetWindowHandleFocus()
    {
#if !UNITY_EDITOR
        IntPtr value = (IntPtr)GetActiveWindow();
		return value;
#else
		return IntPtr.Zero;
#endif
	}
    /// <summary>
    /// Grabs all the window data and stores it in variables for the WindowHandler to use
    /// </summary>
    public static void GetWindowInfo1()
	{
		windowRect windowRect = new windowRect();
		GetWindowRect( windowHandle, ref windowRect );

		position.x	= windowRect.left;
		position.y	= windowRect.top;
		width		= windowRect.right - windowRect.left;
		height		= windowRect.bottom - windowRect.top;
	}
    /// <summary>
    /// Grabs all the window data and stores it in variables for the WindowHandler to use
    /// </summary>
    public static void GetWindowInfo2()
    {
        WINDOWINFO windowInfo = new WINDOWINFO();
        GetWindowInfo(windowHandle, ref windowInfo);
		Vector2 position;
		position.x = windowInfo.rcClient.left;
        position.y = windowInfo.rcClient.top;
        int width = windowInfo.rcClient.right - windowInfo.rcClient.left;
		int height = windowInfo.rcClient.bottom - windowInfo.rcClient.top;

		windowRect windowRect = new windowRect();
		GetWindowRect(windowHandle, ref windowRect);
		position.x = windowRect.left;
		position.y = windowRect.top;
		width = windowRect.right - windowRect.left;
		height = windowRect.bottom - windowRect.top;
	}
#endregion
#region SwitchScreen
	/// <summary>
	/// find all active displays using win32 api
	/// </summary>
	/// <returns>dictionary for all active displays</returns>
	public static DisplayInfoCollection GetDisplays()
	{
		DIC = new DisplayInfoCollection();
		EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
			MonitorEnumCallBackFunc
			, IntPtr.Zero);
		return DIC;
	}
	public static void SwitchScreen(int newHeight, int newWidth, int targetX, int targetY)
	{
		//watch the task bar, and filter out editor
		MoveWindow((IntPtr)windowHandle, targetX, targetY, newWidth, newHeight, true);
	}
#endregion
#region SetWindowsPosition
	/// <summary>
	/// Sets the position of the window
	/// </summary>
	public static void SetWindowPosition(int windowPosX, int windowPosY)
	{
		swpFlags = (uint)SwpFlag.NOZORDER | (uint)SwpFlag.SHOWWINDOW | (uint)SwpFlag.NOSIZE;
		SetWindowPos(windowHandle, new IntPtr(), windowPosX, windowPosY, 0, 0, swpFlags);
	}

#endregion
#region SetWindowSize
	/// <summary>
	/// Sets the size of the window
	/// </summary>
	/// <param name="Width">The width to change the window to.</param>
	/// <param name="Height">The height to change the window to.</param>
	public static void SetWindowSize( int windowWidth, int windowHeight )
	{
		swpFlags = (uint)SwpFlag.NOZORDER | (uint)SwpFlag.SHOWWINDOW | (uint)SwpFlag.NOREPOSITION;
		SetWindowPos(windowHandle, new IntPtr(), 0, 0, windowWidth, windowHeight, swpFlags);
	}
#endregion
#region Mouse Functions
	/// <summary>
	/// Returns the position of the mouse in monitor space.
	/// </summary>
	/// <returns>Returns a Vector2 of the x and y positions of the mouse in monitor space.</returns>
	public static Vector2 GetMousePosition()
	{
		point tempPoint = new point();
		Vector2 mousepos;
		GetPhysicalCursorPos( ref tempPoint );
		mousepos.x = tempPoint.x;
		mousepos.y = tempPoint.y;

		return mousepos;
	}
	/// <summary>
		/// Returns the position of the mouse in window space.
		/// </summary>
		/// <returns>Returns a Vector2 of the x and y positions of the mouse in window space.</returns>
	public static Vector2 GetMousePositionInWindow()
	{
		Vector2 mouseInWinPos = new Vector2();
		Vector2 mousePos = GetMousePosition();
		GetWindowInfo1();
		mouseInWinPos.x = mousePos.x - position.x;
		mouseInWinPos.y = mousePos.y - position.y;

		return mouseInWinPos;
	}
	/*
	static Color GetPixelColor( int x, int y )
	{
		Color temp = Color.black;
		uint colorData = GetPixel( windowHandle, x, y );
		Debug.Log( "Full val: " + colorData );
		Debug.Log( "B val: " + ((colorData>>16) & 0xFF) );
		Debug.Log( "G val: " + ((colorData>>8) & 0xFF) );
		Debug.Log( "R val: " + ((colorData) & 0xFF) );
		return temp;
	}
	*/
#endregion
#region Style Modifiers
	/// <summary>
	/// Pins the window to display over the top of all other windows.
	/// </summary>
	/// <param name="bool pinToTop">The value of if the window should be pinned to the top.</param>
	public static void SetWindowTopmost( bool pinToTop )
	{
		//if (IsWindowVisible(windowHandle))
		{

#if !UNITY_EDITOR
			stayOnTop = pinToTop;
#else
            stayOnTop = false ;
#endif
            swpFlags = (uint)SwpFlag.NOMOVE | (uint)SwpFlag.NOSIZE;
			if (stayOnTop)
				SetWindowPos(windowHandle, new IntPtr((int)ZOrder.TopMost), 0, 0, 0, 0, swpFlags);// || (uint)SwpFlag.SHOWWINDOW);
			else
				SetWindowPos(windowHandle, new IntPtr((int)ZOrder.NoTopMost), 0, 0, 0, 0, swpFlags);
		}
	}

    /// <summary>
    /// Sets the window to be transparent. All pixels with a color matching the WindowHandler.colorKey value will be transparent and able to be clicked through.
    /// </summary>
    /// <param name="bool is noBorder">The value of if the window should be no border.</param>
    public static void SetWindowNoBorder(bool param)
    {
        noBorder = param;
        if (noBorder)
        {
            lExWsStyle &= ~(long)(ExtendedWindowStyle.DLGMODALFRAME | ExtendedWindowStyle.CLIENTEDGE | ExtendedWindowStyle.STATICEDGE);
        }
		PushExStyles();
	}
    /// <summary>
    /// Sets the window to be transparent. All pixels with a color matching the WindowHandler.colorKey value will be transparent and able to be clicked through.
    /// </summary>
    /// <param name="bool isTransparent">The value of if the window should be transparent.</param>
    public static void SetWindowTransparency( bool isTransparent )
	{
		transparentWindow = isTransparent;
		PushExStyles();
	}

	/// <summary>
		/// Sets the whole window to ignore clicks.
		/// </summary>
		/// <param name="bool clickThrough">The value of if the window should ignore clicks.</param>
	public static void SetWindowIgnoreClicks( bool clickThrough )
	{
		canClickThrough = clickThrough;
		PushExStyles();
	}

	/// <summary>
		/// Sets the window's taskbar icon.
		/// </summary>
		/// <param name="bool showIcon">The value of if the taskbar icon should appear in the taskbar or not.</param>
	public static void SetShowTaskbarIcon( bool showIcon )
	{
		showTaskbarIcon = showIcon;
		PushExStyles();
	}

	/// <summary>
		/// Sets the entire window's opacity.
		/// </summary>
		/// <param name="Percentage">Between 0% and 100% of the windows opacity</param>
	public static void SetWindowOpacity( int percentage )
	{
		windowOpacity = percentage;
		windowOpacity = Mathf.Clamp( windowOpacity, 0, 100 );

		PushExStyles();
	}
	/// <summary>
		/// Sets the entire window's opacity.
		/// </summary>
		/// <param name="Percentage">Between 0% and 100% of the windows opacity</param>
	public static void SetWindowOpacity( float percentage )
	{
		SetWindowOpacity( (int)percentage );
	}

	/* Push the Extended window styles */
	static void PushExStyles()
	{
		lExWsStyle = 0x0000;
		int typeFlag = 0x0002;

		if( transparentWindow )
		{
			lExWsStyle |= (long)ExtendedWindowStyle.LAYERED;
			typeFlag = 0x0001;
		}
		if( canClickThrough )
		{
			lExWsStyle |= (long)ExtendedWindowStyle.LAYERED;
			lExWsStyle |= (long)ExtendedWindowStyle.TRANSPARENT;
		}
		if( !showTaskbarIcon )
		{
			lExWsStyle |= (long)ExtendedWindowStyle.TOOLWINDOW;
		}

		// Set the windows extended style
		SetWindowLong( windowHandle, -20, lExWsStyle );

		SetLayeredWindowAttributes( windowHandle, colorkey, (byte)((255 * windowOpacity) /100), typeFlag );

		//UpdateWindowPosition();
	}
    public static void ShowWindowMinimized()
    {
        //ShowWindowAsync(GetActiveWindow(), SW_MINIMIZE);
        ShowWindowAsync(windowHandle, SW_MINIMIZE);
	}
	public static void ShowWindowNormal()
	{
		ShowWindow(windowHandle, SW_SHOW);
	}
	public static void SwitchWindow(bool Show)
	{
#if !UNITY_EDITOR
		if (Show)
        {
			ShowWindowAsync(windowHandle, SW_SHOWMAXIMIZED);
		}
        else
        {
			ShowWindowAsync(windowHandle, SW_HIDE);
		}
#endif
    }

    public static void SetForegroundWindow()
	{
#if !UNITY_EDITOR
		if (IsWindowVisible(windowHandle))
			SetForegroundWindow(windowHandle);
#endif
	}
	#endregion
#region EXTERNAL FUNCTIONS
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern int FindWindow(string lpClassName, string lpWindowName);
    [DllImport( "user32.dll", EntryPoint = "SetWindowLongA", CharSet = CharSet.Auto)]
	private static extern int SetWindowLong( int hwnd, int nIndex, long dwNewLong );
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
	private static extern int SetWindowLongPtr(int hwnd, int nIndex, ref long dwNewLongPtr);
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool ShowWindowAsync( int hWnd, int nCmdShow );
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool ShowWindow(int hWnd, int nCmdShow);
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool IsWindowVisible(int hWnd);
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr GetForegroundWindow();
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool SwitchToThisWindow(int hWnd, bool fUnknown);
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AllowSetForegroundWindow(int hWnd);
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool SetForegroundWindow(int hWnd);
    [DllImport( "user32.dll", CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern int SetLayeredWindowAttributes( int hwnd, int crKey, byte bAlpha, int dwFlags );
    [DllImport( "user32.dll", EntryPoint = "GetActiveWindow", CharSet = CharSet.Auto)]
	private static extern int GetActiveWindow();
    [DllImport( "user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
	private static extern long GetWindowLong( int hwnd, int nIndex );
    [DllImport( "user32.dll", EntryPoint = "SetWindowPos", CharSet = CharSet.Auto)]
	private static extern int SetWindowPos( int hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint uFlags );
	[DllImport( "user32.dll", EntryPoint = "GetCursorPos", CharSet = CharSet.Auto)]
	private static extern bool GetPhysicalCursorPos( ref point refPoint );
	[DllImport( "user32.dll", EntryPoint = "GetWindowRect", CharSet = CharSet.Auto)]   //use GetWindowInfo for more
	private static extern bool GetWindowRect( int hwnd, ref windowRect windowRect );
	[DllImport( "Gdi32.dll", EntryPoint = "GetPixel", CharSet = CharSet.Auto)]
	private static extern uint GetPixel( int hwnd, int xPos, int yPos );
    [return: MarshalAs(UnmanagedType.Bool)]
	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	static extern IntPtr PowerCreateRequest(ref POWER_REQUEST_CONTEXT Context);
	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	static extern bool PowerSetRequest(IntPtr PowerRequestHandle, PowerRequestType RequestType);
	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	static extern bool PowerClearRequest(IntPtr PowerRequestHandle, PowerRequestType RequestType);
	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
	internal static extern int CloseHandle(IntPtr hObject);
	[DllImport("user32.dll",CharSet = CharSet.Auto)]
	private static extern bool GetWindowInfo(int hwnd, ref WINDOWINFO pwi);
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	internal static extern void MoveWindow(IntPtr hwnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);
	[DllImport("User32.dll", CharSet = CharSet.Unicode)]
	public static extern bool GetMonitorInfo(IntPtr hmonitor, [In, Out] MONITORINFOEX info);
	[DllImport("User32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
	public static extern IntPtr MonitorFromPoint(POINTSTRUCT pt, int flags);
	delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref windowRect lprcMonitor, IntPtr dwData);
	[MonoPInvokeCallback(typeof(MonitorEnumDelegate))]
	static bool MonitorEnumCallBackFunc(IntPtr hMonitor, IntPtr hdcMonitor, ref windowRect lprcMonitor, IntPtr dwData)
	{
		MONITORINFOEX mi = new MONITORINFOEX();
		mi.cbSize = (int)Marshal.SizeOf(mi);
		bool success = GetMonitorInfo(hMonitor, mi);
		if (success)
		{
			DisplayInfo di = new DisplayInfo();
			di.ScreenWidth = (mi.rcMonitor.right - mi.rcMonitor.left).ToString();
			di.ScreenHeight = (mi.rcMonitor.bottom - mi.rcMonitor.top).ToString();
			di.MonitorArea = mi.rcMonitor;
			di.WorkArea = mi.rcMonitor;    //rcWork: erea without taskbar
			di.Availability = mi.dwFlags.ToString();
			string displayName = new string(mi.szDevice).Replace("\0","");
			string[] nameArray = displayName.Split(new string[] { "DISPLAY" }, StringSplitOptions.RemoveEmptyEntries);
			try
			{
				int curIdx = Convert.ToInt32(nameArray.Last());
				di.displayID = curIdx;
				di.displayName = "DISPLAY" + curIdx.ToString();
				DIC.Add(di);
            }
            catch
            {
				Debug.LogError("DISPLAY NAME not valid: " + displayName );
				foreach(char c in displayName)
                {
					Debug.Log(c);
				}
            }
		}
		return true;
	}
#endregion
#region prevent screensaver, display dimming and automatically sleeping
	static POWER_REQUEST_CONTEXT _PowerRequestContext;
	static IntPtr _PowerRequest; //HANDLE
	static bool _activeFlag = false;

	// Availablity Request Enumerations and Constants
	enum PowerRequestType
	{
		PowerRequestDisplayRequired = 0,
		PowerRequestSystemRequired,
		PowerRequestAwayModeRequired,
		PowerRequestMaximum
	}

	const int POWER_REQUEST_CONTEXT_VERSION = 0;
	const int POWER_REQUEST_CONTEXT_SIMPLE_STRING = 0x1;
	const int POWER_REQUEST_CONTEXT_DETAILED_STRING = 0x2;

	// Availablity Request Structures
	// Note:  Windows defines the POWER_REQUEST_CONTEXT structure with an
	// internal union of SimpleReasonString and Detailed information.
	// To avoid runtime interop issues, this version of 
	// POWER_REQUEST_CONTEXT only supports SimpleReasonString.  
	// To use the detailed information,
	// define the PowerCreateRequest function with the first 
	// parameter of type POWER_REQUEST_CONTEXT_DETAILED.
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct POWER_REQUEST_CONTEXT
	{
		public UInt32 Version;
		public UInt32 Flags;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string
			SimpleReasonString;
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct PowerRequestContextDetailedInformation
	{
		public IntPtr LocalizedReasonModule;
		public UInt32 LocalizedReasonId;
		public UInt32 ReasonStringCount;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string[] ReasonStrings;
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct POWER_REQUEST_CONTEXT_DETAILED
	{
		public UInt32 Version;
		public UInt32 Flags;
		public PowerRequestContextDetailedInformation DetailedInformation;
	}
	/// <summary>
	/// Prevent screensaver, display dimming and power saving. This function wraps PInvokes on Win32 API. 
	/// </summary>
	/// <param name="enableConstantDisplayAndPower">True to get a constant display and power - False to clear the settings</param>
	public static void EnableConstantDisplayAndPower(bool enableConstantDisplayAndPower)
	{
		if (enableConstantDisplayAndPower)
		{
            if (!_activeFlag)
            {
				// Set up the diagnostic string
				_PowerRequestContext.Version = POWER_REQUEST_CONTEXT_VERSION;
				_PowerRequestContext.Flags = POWER_REQUEST_CONTEXT_SIMPLE_STRING;
				_PowerRequestContext.SimpleReasonString = "3D Master Playing";

				// Create the request, get a handle
				_PowerRequest = PowerCreateRequest(ref _PowerRequestContext);

				// Set the request
				PowerSetRequest(_PowerRequest, PowerRequestType.PowerRequestSystemRequired);
				PowerSetRequest(_PowerRequest, PowerRequestType.PowerRequestDisplayRequired);
				_activeFlag = true;
			}
		}
		else
		{
            if (_activeFlag)
            {
				// Clear the request
				PowerClearRequest(_PowerRequest, PowerRequestType.PowerRequestSystemRequired);
				PowerClearRequest(_PowerRequest, PowerRequestType.PowerRequestDisplayRequired);

				CloseHandle(_PowerRequest);
				_activeFlag = false;
			}
		}
	}
#endregion
}
