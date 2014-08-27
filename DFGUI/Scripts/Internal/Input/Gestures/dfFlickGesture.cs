using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Gestures/Flick" )]
public class dfFlickGesture : dfGestureBase
{

	#region Events

	public event dfGestureEventHandler<dfFlickGesture> FlickGesture;

	#endregion

	#region Serialized protected variables

	[SerializeField]
	private float timeout = 0.25f;

	[SerializeField]
	private float minDistance = 25;

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
	/// Gets or sets the minimum distance the use must move the mouse
	/// or touch before the gesture is recognized
	/// </summary>
	public float MinimumDistance
	{
		get { return this.minDistance; }
		set { this.minDistance = value; }
	}

	/// <summary>
	/// Returns the amount of time that was taken for the gesture to be completed
	/// </summary>
	public float DeltaTime
	{
		get;
		protected set;
	}

	#endregion

	#region Private runtime variables 

	private float hoverTime = 0f;

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
		hoverTime = Time.realtimeSinceStartup;
	}

	public void OnMouseHover( dfControl source, dfMouseEventArgs args )
	{
		if( State == dfGestureState.Possible && Time.realtimeSinceStartup - hoverTime >= timeout )
		{
			StartPosition = CurrentPosition = args.Position;
			StartTime = Time.realtimeSinceStartup;
		}
	}

	public void OnMouseMove( dfControl source, dfMouseEventArgs args )
	{

		hoverTime = Time.realtimeSinceStartup;

		if( State == dfGestureState.Possible || State == dfGestureState.Began )
		{
			State = dfGestureState.Began;
			CurrentPosition = args.Position;
		}

	}

	public void OnMouseUp( dfControl source, dfMouseEventArgs args )
	{

		if( State == dfGestureState.Began )
		{
			
			CurrentPosition = args.Position;
			
			if( Time.realtimeSinceStartup - StartTime <= timeout )
			{

				var distance = Vector2.Distance( CurrentPosition, StartPosition );
				if( distance >= minDistance )
				{

					State = dfGestureState.Ended;
					DeltaTime = Time.realtimeSinceStartup - StartTime;

					if( FlickGesture != null ) FlickGesture( this );
					gameObject.Signal( "OnFlickGesture", this );

				}
				else
				{
					State = dfGestureState.Failed;
				}

			}
			else
			{
				State = dfGestureState.Failed;
			}
		}

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
		State = dfGestureState.None;
	}

	#endregion 

}
