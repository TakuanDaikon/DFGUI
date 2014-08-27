using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Gestures/Tap" )]
public class dfTapGesture : dfGestureBase
{

	#region Events

	public event dfGestureEventHandler<dfTapGesture> TapGesture;

	#endregion 

	#region Serialized protected variables

	[SerializeField]
	private float timeout = 0.25f;

	[SerializeField]
	private float maxDistance = 25;

	#endregion 
	
	#region Public properties

	/// <summary>
	/// Gets or sets the maximum amount of time (in seconds) for the 
	/// gesture to be recognized, from the start of the touch to the
	/// end of the touch.
	/// </summary>
	public float Timeout
	{
		get { return this.timeout; }
		set { this.timeout = value; }
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

	#endregion

	#region Unity messsags 

	protected void Start()
	{
		// Only included to allows the user to enable/disable this component in the inspector
	}

	#endregion 

	#region Input events

	public void OnMouseDown( dfControl source, dfMouseEventArgs args )
	{
		StartPosition = CurrentPosition = args.Position;
		State = dfGestureState.Possible;
		StartTime = Time.realtimeSinceStartup;
	}

	public void OnMouseMove( dfControl source, dfMouseEventArgs args )
	{
		if( State == dfGestureState.Possible || State == dfGestureState.Began )
		{
			CurrentPosition = args.Position;
			if( Vector2.Distance( args.Position, StartPosition ) > maxDistance )
			{
				State = dfGestureState.Failed;
			}
		}
	}

	public void OnMouseUp( dfControl source, dfMouseEventArgs args )
	{

		if( State == dfGestureState.Possible )
		{
			if( Time.realtimeSinceStartup - StartTime <= timeout )
			{
				CurrentPosition = args.Position;
				State = dfGestureState.Ended;
				if( TapGesture != null ) TapGesture( this );
				gameObject.Signal( "OnTapGesture", this );
			}
			else
			{
				State = dfGestureState.Failed;
			}
		}
		else
		{
			State = dfGestureState.None;
		}

	}

	public void OnMultiTouch( dfControl source, dfTouchEventArgs args )
	{
		State = dfGestureState.Failed;
	}

	#endregion 

}
