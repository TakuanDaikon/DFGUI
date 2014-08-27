// NOTE: This class is highly experimental, and is not part of the official
// Daikon Forge GUI Library package. It is provided for your use should you
// choose to attempt to make use of it. As we don't currently have the 
// hardware available to fully test this, we do not promise that it works
// correctly in all cases, or even at all. We hope that it works well, and
// that it will provide a solid starting point for anyone who wishes to 
// extend the functionality, but we make absolutely no warranties.

#if USE_WIN7_TOUCH

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// Processes Windows 7 touch events.
/// Known issues:
/// <list type="bullet">
///     <item>Works in Windows Standalone ONLY</item>
/// </list>
/// </summary>
[AddComponentMenu( "TouchScript/Input Sources/Windows 7 Touch Input" )]
public class dfWin7TouchInputSource : IDFTouchInputSource
{

	#region Delegate definitions

	private delegate int WndProcDelegate( IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam );

	#endregion

	#region Private fields

	private IntPtr hMainWindow;
	private IntPtr oldWndProcPtr;
	private IntPtr newWndProcPtr;

	private WndProcDelegate newWndProc;

	private Dictionary<int, int> winToInternalId = new Dictionary<int, int>();
	private bool isInitialized = false;
	private int newTouchID = 0;

	private dfList<dfTouchTrackingInfo> tracking = new dfList<dfTouchTrackingInfo>();
	private dfList<dfTouchInfo> activeTouches = new dfList<dfTouchInfo>();

	#endregion

	#region Public Methods

	public bool Initialize()
	{

		try
		{

			if( Application.platform != RuntimePlatform.WindowsPlayer )
				return false;

			touchInputSize = Marshal.SizeOf( typeof( TOUCHINPUT ) );

			hMainWindow = GetForegroundWindow();
			if( !RegisterTouchWindow( hMainWindow, 0 ) )
			{
				Debug.LogWarning( "Failed to register Windows 7 touch window" );
				return false;
			}

			newWndProc = new WndProcDelegate( wndProc );
			newWndProcPtr = Marshal.GetFunctionPointerForDelegate( newWndProc );
			oldWndProcPtr = SetWindowLong( hMainWindow, -4, newWndProcPtr );

			isInitialized = true;

			return true;

		}
		catch( Exception err )
		{
			Debug.LogError( err.Message );
			return false;
		}

	}

	public void Cleanup()
	{

		if( isInitialized )
		{

			SetWindowLong( hMainWindow, -4, oldWndProcPtr );
			UnregisterTouchWindow( hMainWindow );

			hMainWindow = IntPtr.Zero;
			oldWndProcPtr = IntPtr.Zero;
			newWndProcPtr = IntPtr.Zero;

			newWndProc = null;

		}
		
	}

	#endregion

	#region IDFTouchInputSource Members

	public int TouchCount
	{
		get { return activeTouches.Count; }
	}

	public dfTouchInfo GetTouch( int index )
	{

		if( index < 0 || index > activeTouches.Count )
			throw new IndexOutOfRangeException();

		return activeTouches[ index ];

	}

	public IList<dfTouchInfo> Touches
	{
		get { return activeTouches; }
	}

	public void Update()
	{

		lock( tracking )
		{

			activeTouches.Clear();

			for( int i = 0; i < tracking.Count; i++ )
			{
				activeTouches.Add( tracking[ i ] );
			}

			postProcessTouches();

		}

	}

	#endregion

	#region Private functions

	private int wndProc( IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam )
	{

		switch( msg )
		{
			case WM_TOUCH:
				decodeTouches( wParam, lParam );
				break;
			case WM_CLOSE:
				UnregisterTouchWindow( hWnd );
				SetWindowLong( hWnd, -4, oldWndProcPtr );
				SendMessage( hWnd, WM_CLOSE, 0, 0 );
				return 0;
		}

		return CallWindowProc( oldWndProcPtr, hWnd, msg, wParam, lParam );

	}

	private void decodeTouches( IntPtr wParam, IntPtr lParam )
	{

		lock( tracking )
		{

			int inputCount = LOWORD( wParam.ToInt32() );
			TOUCHINPUT[] inputs = new TOUCHINPUT[ inputCount ];

			if( !GetTouchInputInfo( lParam, inputCount, inputs, touchInputSize ) )
			{
				return;
			}

			for( int i = 0; i < inputCount; i++ )
			{
				TOUCHINPUT touch = inputs[ i ];

				if( ( touch.dwFlags & (int)TouchEvent.TOUCHEVENTF_DOWN ) != 0 )
				{
					POINT p = new POINT();
					p.X = touch.x / 100;
					p.Y = touch.y / 100;
					ScreenToClient( hMainWindow, ref p );

					winToInternalId.Add( touch.dwID, beginTouch( new Vector2( p.X, Screen.height - p.Y ) ) );
				}
				else if( ( touch.dwFlags & (int)TouchEvent.TOUCHEVENTF_UP ) != 0 )
				{
					int existingId;
					if( winToInternalId.TryGetValue( touch.dwID, out existingId ) )
					{
						winToInternalId.Remove( touch.dwID );
						endTouch( existingId );
					}
				}
				else if( ( touch.dwFlags & (int)TouchEvent.TOUCHEVENTF_MOVE ) != 0 )
				{
					int existingId;
					if( winToInternalId.TryGetValue( touch.dwID, out existingId ) )
					{
						POINT p = new POINT();
						p.X = touch.x / 100;
						p.Y = touch.y / 100;
						ScreenToClient( hMainWindow, ref p );

						moveTouch( existingId, new Vector2( p.X, Screen.height - p.Y ) );
					}
				}
			}

			CloseTouchInputHandle( lParam );
		
		}

	}

	private dfTouchTrackingInfo findTouch( int existingID )
	{

		for( int i = 0; i < tracking.Count; i++ )
		{
			if( tracking[ i ].FingerID == existingID )
				return tracking[ i ];
		}

		return null;

	}

	private void moveTouch( int existingId, Vector2 position )
	{

		var touch = findTouch( existingId );
		if( touch == null || touch.Phase == TouchPhase.Ended || touch.Phase == TouchPhase.Began )
			return;

		touch.Phase = Vector2.Distance( position, touch.Position ) <= float.Epsilon ? TouchPhase.Stationary : TouchPhase.Moved;
		touch.Position = position;

	}

	private void endTouch( int existingId )
	{

		var touch = findTouch( existingId );
		if( touch == null )
			return;

		touch.Phase = TouchPhase.Ended;

	}

	private int beginTouch( Vector2 position )
	{

		var newID = ( newTouchID++ ) % 10;

		var touch = new dfTouchTrackingInfo()
		{
			FingerID = newID,
			Phase = TouchPhase.Began,
			Position = position
		};

		tracking.Add( touch );

		return newID;

	}

	private void postProcessTouches()
	{

		var index = 0;
		while( index < tracking.Count )
		{

			if( tracking[ index ].Phase == TouchPhase.Ended )
			{
				tracking.RemoveAt( index );
				continue;
			}

			tracking[ index ].Phase = TouchPhase.Stationary;

			index += 1;

		}

	}

	#endregion

	#region p/invoke

	// Touch event window message constants [winuser.h]
	private const int WM_TOUCH = 0x0240;
	private const int WM_CLOSE = 0x0010;

	internal enum TouchEvent : int
	{
		TOUCHEVENTF_MOVE = 0x0001,
		TOUCHEVENTF_DOWN = 0x0002,
		TOUCHEVENTF_UP = 0x0004,
		TOUCHEVENTF_INRANGE = 0x0008,
		TOUCHEVENTF_PRIMARY = 0x0010,
		TOUCHEVENTF_NOCOALESCE = 0x0020,
		TOUCHEVENTF_PEN = 0x0040
	}

	// Touch API defined structures [winuser.h]
	[StructLayout( LayoutKind.Sequential )]
	private struct TOUCHINPUT
	{
		public int x;
		public int y;
		public IntPtr hSource;
		public int dwID;
		public int dwFlags;
		public int dwMask;
		public int dwTime;
		public IntPtr dwExtraInfo;
		public int cxContact;
		public int cyContact;
	}

	[StructLayout( LayoutKind.Sequential )]
	private struct POINT
	{
		public int X;
		public int Y;
	}

	[DllImport( "user32.dll" )]
	private static extern IntPtr GetForegroundWindow();

	[DllImport( "user32.dll" )]
	private static extern IntPtr SetWindowLong( IntPtr hWnd, int nIndex, IntPtr dwNewLong );

	[DllImport( "user32.dll" )]
	private static extern int CallWindowProc( IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam );

	[DllImport( "user32.dll" )]
	[return: MarshalAs( UnmanagedType.Bool )]
	private static extern bool RegisterTouchWindow( IntPtr hWnd, uint ulFlags );

	[DllImport( "user32.dll" )]
	[return: MarshalAs( UnmanagedType.Bool )]
	private static extern bool UnregisterTouchWindow( IntPtr hWnd );

	[DllImport( "user32.dll" )]
	[return: MarshalAs( UnmanagedType.Bool )]
	private static extern bool GetTouchInputInfo( IntPtr hTouchInput, int cInputs, [Out] TOUCHINPUT[] pInputs, int cbSize );

	[DllImport( "user32.dll" )]
	[return: MarshalAs( UnmanagedType.Bool )]
	private static extern void CloseTouchInputHandle( IntPtr lParam );

	[DllImport( "user32.dll" )]
	private static extern bool ScreenToClient( IntPtr hWnd, ref POINT lpPoint );

	[DllImport( "coredll.dll", EntryPoint = "SendMessage", SetLastError = true )]
	private static extern int SendMessage( IntPtr hWnd, uint uMsg, int wParam, int lParam );

	private int touchInputSize;

	private int HIWORD( int value )
	{
		return (int)( value >> 16 );
	}

	private int LOWORD( int value )
	{
		return (int)( value & 0xffff );
	}

	#endregion

}

#endif