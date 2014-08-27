using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Gestures/Double Tap" )]
public class dfDoubleTapGesture : dfGestureBase
{
	#region Events

	public event dfGestureEventHandler<dfDoubleTapGesture> DoubleTapGesture;

	#endregion

	#region Serialized protected variables

	[SerializeField]
	private float timeout = 0.5f;

	[SerializeField]
	private float maxDistance = 35;

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

		if( State == dfGestureState.Possible )
		{
		
			var elapsed = Time.realtimeSinceStartup - StartTime;
			if( elapsed <= timeout && Vector2.Distance( args.Position, StartPosition ) <= maxDistance )
			{

				StartPosition = CurrentPosition = args.Position;
				
				State = dfGestureState.Began;

				if( DoubleTapGesture != null ) DoubleTapGesture( this );
				gameObject.Signal( "OnDoubleTapGesture", this );

				endGesture();

				return;

			}

		}

		StartPosition = CurrentPosition = args.Position;
		State = dfGestureState.Possible;
		StartTime = Time.realtimeSinceStartup;

	}

	public void OnMouseLeave()
	{
		endGesture();
	}

	public void OnMultiTouchEnd()
	{
		endGesture();
	}

	public void OnMultiTouch()
	{
		endGesture();
	}

	#endregion

	#region Private utility methods 
	private void endGesture()
	{

		if( State == dfGestureState.Began || State == dfGestureState.Changed )
			State = dfGestureState.Ended;
		else if( State == dfGestureState.Possible )
			State = dfGestureState.Cancelled;
		else
			State = dfGestureState.None;

	}

	#endregion 

}
