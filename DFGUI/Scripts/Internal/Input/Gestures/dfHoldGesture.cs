using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Gestures/Hold" )]
public class dfHoldGesture : dfGestureBase
{

	#region Public events 

	public event dfGestureEventHandler<dfHoldGesture> HoldGestureStart;
	public event dfGestureEventHandler<dfHoldGesture> HoldGestureEnd;

	#endregion 

	#region Serialized protected variables

	[SerializeField]
	private float minTime = 0.75f;

	[SerializeField]
	private float maxDistance = 25;

	#endregion

	#region Public properties

	/// <summary>
	/// Gets or sets the minimum amount of time (in seconds) from the
	/// initial mouse down or touch press for the gesture to be recognized.
	/// </summary>
	public float MinimumTime
	{
		get { return this.minTime; }
		set { this.minTime = value; }
	}

	/// <summary>
	/// Gets or sets the maximum distance the user can move the mouse
	/// or touch when tapping. Moving more than this distance means
	/// that the gesture will not be recognized.
	/// </summary>
	public float MaximumDistance
	{
		get { return this.maxDistance; }
		set { this.maxDistance = value; }
	}

	/// <summary>
	/// Returns the amount of time (in seconds) that the touch or
	/// press has been held.
	/// </summary>
	public float HoldLength
	{
		get
		{
			if( State == dfGestureState.Began )
				return Time.realtimeSinceStartup - StartTime;
			else
				return 0f;
		}
	}

	#endregion

	#region Unity messsags

	protected void Start()
	{
		// Only included to allows the user to enable/disable this component in the inspector
	}

	protected void Update()
	{

		if( State == dfGestureState.Possible )
		{
			if( Time.realtimeSinceStartup - StartTime >= minTime )
			{

				State = dfGestureState.Began;

				if( HoldGestureStart != null ) HoldGestureStart( this );

				gameObject.Signal( "OnHoldGestureStart", this );

			}
		}

	}

	#endregion

	#region Input events

	public void OnMouseDown( dfControl source, dfMouseEventArgs args )
	{
		State = dfGestureState.Possible;
		StartPosition = CurrentPosition = args.Position;
		StartTime = Time.realtimeSinceStartup;
	}

	public void OnMouseMove( dfControl source, dfMouseEventArgs args )
	{

		if( State != dfGestureState.Possible && State != dfGestureState.Began )
			return;

		CurrentPosition = args.Position;

		if( Vector2.Distance( args.Position, StartPosition ) > maxDistance )
		{
			if( State == dfGestureState.Possible )
			{
				State = dfGestureState.Failed;
			}
			else if( State == dfGestureState.Began )
			{
				State = dfGestureState.Cancelled;
				if( HoldGestureEnd != null ) HoldGestureEnd( this );
				gameObject.Signal( "OnHoldGestureEnd", this );
			}
		}

	}

	public void OnMouseUp( dfControl source, dfMouseEventArgs args )
	{

		if( State == dfGestureState.Began )
		{
			CurrentPosition = args.Position;
			State = dfGestureState.Ended;
			if( HoldGestureEnd != null ) HoldGestureEnd( this );
			gameObject.Signal( "OnHoldGestureEnd", this );
		}

		State = dfGestureState.None;

	}

	public void OnMultiTouch( dfControl source, dfTouchEventArgs args )
	{

		if( State == dfGestureState.Began )
		{
			State = dfGestureState.Cancelled;
			if( HoldGestureEnd != null ) HoldGestureEnd( this );
			gameObject.Signal( "OnHoldGestureEnd", this );
		}
		else
		{
			State = dfGestureState.Failed;
		}

	}

	#endregion

}
