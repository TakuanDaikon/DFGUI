// @cond DOXY_IGNORE
using System;
using System.Reflection;
using System.Runtime.InteropServices;

using UnityEngine;

[System.Serializable]
[StructLayout( LayoutKind.Sequential, Pack = 1 )]
public struct dfTouchInfo
{

	#region Private fields

	private int m_FingerId;
	private Vector2 m_Position;
#if !UNITY_4_2
	private Vector2 m_RawPosition;
#endif
	private Vector2 m_PositionDelta;
	private float m_TimeDelta;
	private int m_TapCount;
	private TouchPhase m_Phase;

	#endregion

	#region Public properties

	public int fingerId
	{
		get { return this.m_FingerId; }
	}

	public Vector2 position
	{
		get { return this.m_Position; }
	}

#if !UNITY_4_2
	public Vector2 rawPosition
	{
	  get { return this.m_RawPosition; }
	}
#endif

	public Vector2 deltaPosition
	{
		get { return this.m_PositionDelta; }
	}

	public float deltaTime
	{
		get { return this.m_TimeDelta; }
	}

	public int tapCount
	{
		get { return this.m_TapCount; }
	}

	public TouchPhase phase
	{
		get { return this.m_Phase; }
	}

	#endregion

	#region Constructor

	public dfTouchInfo( int fingerID, TouchPhase phase, int tapCount, Vector2 position, Vector2 positionDelta, float timeDelta )
	{

		this.m_FingerId = fingerID;
		this.m_Phase = phase;
		this.m_Position = position;
		this.m_PositionDelta = positionDelta;
		this.m_TapCount = tapCount;
		this.m_TimeDelta = timeDelta;

#if !UNITY_4_2
		this.m_RawPosition = position;
#endif

	}

	#endregion

	#region Implicit type conversion

	public static implicit operator dfTouchInfo( UnityEngine.Touch touch )
	{

		var info = new dfTouchInfo()
		{
			m_PositionDelta = touch.deltaPosition,
			m_TimeDelta = touch.deltaTime,
			m_FingerId = touch.fingerId,
			m_Phase = touch.phase,
			m_Position = touch.position,
			m_TapCount = touch.tapCount
		};

		return info;

	}

#if FALSE

	public static implicit operator UnityEngine.Touch( dfTouchInfo info )
	{

		UnityEngine.Touch uT = default( UnityEngine.Touch );

		IntPtr pnt = Marshal.AllocHGlobal( Marshal.SizeOf( typeof( UnityEngine.Touch ) ) );

		try
		{
			Marshal.StructureToPtr( info, pnt, false );
			uT = (UnityEngine.Touch)Marshal.PtrToStructure( pnt, typeof( UnityEngine.Touch ) );
		}
		finally
		{
			Marshal.FreeHGlobal( pnt );
		}

#if DEBUG
		var isExactCopy =
			uT.deltaPosition == info.deltaPosition &&
			uT.deltaTime == info.deltaTime &&
			uT.fingerId == info.fingerId &&
			uT.phase == info.phase &&
			uT.position == info.position &&
			uT.tapCount == info.tapCount;

		if( !isExactCopy )
		{
			Debug.LogError( "Failed to cast dfTouchInfo structure to UnityEngine.Touch" );
			return new UnityEngine.Touch();
		}
#endif

		return uT;

	}

#endif

	#endregion

}
// @endcond DOXY_IGNORE
