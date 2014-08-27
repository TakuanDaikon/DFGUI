using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Gestures/Resize" )]
public class dfResizeGesture : dfGestureBase
{

	#region Public events 

	public event dfGestureEventHandler<dfResizeGesture> ResizeGestureStart;
	public event dfGestureEventHandler<dfResizeGesture> ResizeGestureUpdate;
	public event dfGestureEventHandler<dfResizeGesture> ResizeGestureEnd;

	#endregion 

	#region Public properties 

	/// <summary>
	/// Returns the amount of change during the last gesture action
	/// </summary>
	public float SizeDelta { get; protected set; }

	#endregion 

	#region Private runtime variables 

	private float lastDistance = 0;

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
		}
		else if( State == dfGestureState.Possible )
		{
			if( isResizeMovement( args.Touches ) )
			{
				
				State = dfGestureState.Began;
				
				StartPosition = CurrentPosition = getCenter( touches );

				lastDistance = Vector2.Distance( touches[ 0 ].position, touches[ 1 ].position );
				SizeDelta = 0;

				if( ResizeGestureStart != null ) ResizeGestureStart( this );
				gameObject.Signal( "OnResizeGestureStart", this );

			}
		}
		else if( State == dfGestureState.Began || State == dfGestureState.Changed )
		{
			if( isResizeMovement( touches ) )
			{

				State = dfGestureState.Changed;

				CurrentPosition = getCenter( touches );

				var distance = Vector2.Distance( touches[ 0 ].position, touches[ 1 ].position );
				SizeDelta = distance - lastDistance;
				lastDistance = distance;

				if( ResizeGestureUpdate != null ) ResizeGestureUpdate( this );
				gameObject.Signal( "OnResizeGestureUpdate", this );

			}
		}

	}

	#endregion 

	#region Private utility methods 

	private Vector2 getCenter( List<dfTouchInfo> list )
	{

		var accum = Vector2.zero;

		for( int i = 0; i < list.Count; i++ )
		{
			accum += list[ i ].position;
		}

		return accum / list.Count;

	}

	private bool isResizeMovement( List<dfTouchInfo> list )
	{

		if( list.Count < 2 )
			return false;

		var first = list[ 0 ];
		var firstDir = ( first.deltaPosition * ( Time.deltaTime / first.deltaTime ) ).normalized;

		var second = list[ 1 ];
		var secondDir = ( second.deltaPosition * ( Time.deltaTime / second.deltaTime ) ).normalized;

		var angle1 = Vector2.Dot( firstDir, ( first.position - second.position ).normalized );
		var angle2 = Vector2.Dot( secondDir, ( second.position - first.position ).normalized );

		const float threshold = 1f - Mathf.Deg2Rad * 45f;

		return Mathf.Abs( angle1 ) >= threshold || Mathf.Abs( angle2 ) >= threshold;

	}

	private void endGesture()
	{

		if( State == dfGestureState.Began || State == dfGestureState.Changed )
		{

			if( State == dfGestureState.Began )
				State = dfGestureState.Cancelled;
			else
				State = dfGestureState.Ended;

			lastDistance = SizeDelta = 0f;

			if( ResizeGestureEnd != null ) ResizeGestureEnd( this );
			gameObject.Signal( "OnResizeGestureEnd", this );

		}
		else
		{
			State = dfGestureState.None;
		}

	}

	#endregion 

}
