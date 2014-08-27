using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Gestures/Rotate" )]
public class dfRotateGesture : dfGestureBase
{

	#region Public events

	public event dfGestureEventHandler<dfRotateGesture> RotateGestureStart;
	public event dfGestureEventHandler<dfRotateGesture> RotateGestureUpdate;
	public event dfGestureEventHandler<dfRotateGesture> RotateGestureEnd;

	#endregion

	#region Serialized fields 

	[SerializeField]
	protected float thresholdAngle = 10f;

	#endregion 

	#region Public properties

	/// <summary>
	/// Returns the amount of change during the last gesture action
	/// </summary>
	public float AngleDelta { get; protected set; }

	#endregion

	#region Private runtime variables 

	private float accumulatedDelta = 0f;

	#endregion

	#region Unity messsags

	protected void Start()
	{
		// Only included to allows the user to enable/disable this component in the inspector
	}

	#endregion

	#region Input notifications

	public void OnMultiTouchEnd()
	{
		endGesture();
	}

	public void OnMultiTouch( dfControl sender, dfTouchEventArgs args )
	{

		var touches = args.Touches;

		if( State == dfGestureState.None || State == dfGestureState.Cancelled || State == dfGestureState.Ended )
		{
			State = dfGestureState.Possible;
			accumulatedDelta = 0f;
		}
		else if( State == dfGestureState.Possible )
		{
			if( isRotateMovement( args.Touches ) )
			{

				var angleDelta = getAngleDelta( touches ) + accumulatedDelta;
				if( Mathf.Abs( angleDelta ) < thresholdAngle )
				{
					accumulatedDelta = angleDelta;
					return;
				}

				State = dfGestureState.Began;
				StartPosition = CurrentPosition = getCenter( touches );
				AngleDelta = angleDelta;

				if( RotateGestureStart != null ) RotateGestureStart( this );
				gameObject.Signal( "OnRotateGestureStart", this );

			}
		}
		else if( State == dfGestureState.Began || State == dfGestureState.Changed )
		{

			var angleDelta = getAngleDelta( touches );
			if( Mathf.Abs( angleDelta ) <= float.Epsilon || Mathf.Abs( angleDelta ) > 22.5f )
				return;

			State = dfGestureState.Changed;
			AngleDelta = angleDelta;
			CurrentPosition = getCenter( touches );

			if( RotateGestureUpdate != null ) RotateGestureUpdate( this );
			gameObject.Signal( "OnRotateGestureUpdate", this );

		}

	}

	#endregion

	#region Private utility methods

	private float getAngleDelta( List<dfTouchInfo> touches )
	{

		if( touches.Count < 2 )
			return 0f;

		var touch1 = touches[ 0 ];
		var touch2 = touches[ 1 ];

		if( Vector2.Distance( touch1.deltaPosition, touch2.deltaPosition ) <= float.Epsilon )
			return 0f;

		var delta1 = touch1.deltaPosition * ( Time.deltaTime / touch1.deltaTime );
		var delta2 = touch2.deltaPosition * ( Time.deltaTime / touch2.deltaTime );

		var lastDir = ( touch1.position - delta1 ) - ( touch2.position - delta2 );
		var currDir = touch1.position - touch2.position;

		var result = deltaAngle( lastDir.normalized, currDir.normalized );
		if( float.IsNaN( result ) )
			return 0f;

		if( touch1.phase == TouchPhase.Stationary || touch2.phase == TouchPhase.Stationary )
			result *= 0.5f;

		return result;

	}

	private float deltaAngle( Vector2 start, Vector2 end )
	{
		var cross = ( start.x * end.y ) - ( start.y * end.x );
		return Mathf.Rad2Deg * Mathf.Atan2( cross, Vector2.Dot( start, end ) );
	}

	private Vector2 getCenter( List<dfTouchInfo> list )
	{

		var accum = Vector2.zero;

		for( int i = 0; i < list.Count; i++ )
		{
			accum += list[ i ].position;
		}

		return accum / list.Count;

	}

	private bool isRotateMovement( List<dfTouchInfo> list )
	{
		return Mathf.Abs( getAngleDelta( list ) ) >= 0.1f;
	}

	private void endGesture()
	{

		AngleDelta = 0f;
		accumulatedDelta = 0f;

		if( State == dfGestureState.Began || State == dfGestureState.Changed )
		{

			State = dfGestureState.Ended;

			if( RotateGestureEnd != null ) RotateGestureEnd( this );
			gameObject.Signal( "OnRotateGestureEnd", this );

		}
		else if( State == dfGestureState.Possible )
		{
			State = dfGestureState.Cancelled;
		}
		else
		{
			State = dfGestureState.None;
		}

	}

	#endregion

}
