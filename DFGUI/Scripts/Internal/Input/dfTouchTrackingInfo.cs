// @cond DOXY_IGNORE
using System;
using UnityEngine;

internal class dfTouchTrackingInfo
{

	#region Private instance variables

	private TouchPhase phase = TouchPhase.Began;

	private Vector2 position = Vector2.one * float.MinValue;

	private Vector2 deltaPosition = Vector2.zero;
	private float deltaTime = 0f;

	private float lastUpdateTime = Time.realtimeSinceStartup;

	#endregion

	#region Public properties

	public bool IsActive = false;

	public int FingerID { get; set; }

	public TouchPhase Phase
	{
		get { return phase; }
		set 
		{ 

			IsActive = true; 
			phase = value;

			if( value == TouchPhase.Stationary )
			{
				deltaTime = float.Epsilon;
				deltaPosition = Vector2.zero;
				lastUpdateTime = Time.realtimeSinceStartup;
			}

		}
	}

	public Vector2 Position
	{
		get { return this.position; }
		set
		{

			IsActive = true;

			if( Phase == TouchPhase.Began )
				deltaPosition = Vector2.zero;
			else
				deltaPosition = value - position;

			position = value;

			var realTime = Time.realtimeSinceStartup;
			deltaTime = realTime - lastUpdateTime;
			lastUpdateTime = realTime;

		}
	}

	#endregion

	#region Type Conversion

	public static implicit operator dfTouchInfo( dfTouchTrackingInfo info )
	{

		var touch = new dfTouchInfo(
			info.FingerID,
			info.phase,
			info.phase == TouchPhase.Began ? 1 : 0,
			info.position,
			info.deltaPosition,
			info.deltaTime
		);

		return touch;

	}

	#endregion

}
// @endcond DOXY_IGNORE
