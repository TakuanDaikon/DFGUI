using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Gestures/Pan" )]
public class dfPanGesture : dfGestureBase
{

	#region Events

	public event dfGestureEventHandler<dfPanGesture> PanGestureStart;
	public event dfGestureEventHandler<dfPanGesture> PanGestureMove;
	public event dfGestureEventHandler<dfPanGesture> PanGestureEnd;

	#endregion

	#region Serialized protected variables

	[SerializeField]
	protected float minDistance = 25;

	#endregion

	#region Private runtime variables 

	private bool multiTouchMode = false;

	#endregion 

	#region Public properties

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
	/// Returns the change in position since the last frame
	/// </summary>
	public Vector2 Delta { get; protected set; }

	#endregion 

	#region Unity messsags

	protected void Start()
	{
		// Only included to allow the user to enable/disable this component in the inspector
	}

	#endregion

	#region Input events

	public void OnMouseDown( dfControl source, dfMouseEventArgs args )
	{
		StartPosition = CurrentPosition = args.Position;
		State = dfGestureState.Possible;
		StartTime = Time.realtimeSinceStartup;
		Delta = Vector2.zero;
	}

	public void OnMouseMove( dfControl source, dfMouseEventArgs args )
	{
		
		if( State == dfGestureState.Possible )
		{
			if( Vector2.Distance( args.Position, StartPosition ) >= minDistance )
			{

				State = dfGestureState.Began;

				CurrentPosition = args.Position;
				Delta = args.Position - StartPosition;
				
				if( PanGestureStart != null ) PanGestureStart( this );
				gameObject.Signal( "OnPanGestureStart", this );

			}
		}
		else if( State == dfGestureState.Began || State == dfGestureState.Changed )
		{
			
			State = dfGestureState.Changed;
			
			Delta = args.Position - CurrentPosition;
			CurrentPosition = args.Position;
			
			if( PanGestureMove != null ) PanGestureMove( this );
			gameObject.Signal( "OnPanGestureMove", this );

		}

	}

	public void OnMouseUp( dfControl source, dfMouseEventArgs args )
	{
		endPanGesture();
	}

	public void OnMultiTouchEnd()
	{
		endPanGesture();
		multiTouchMode = false;
	}

	public void OnMultiTouch( dfControl source, dfTouchEventArgs args )
	{

		var center = getCenter( args.Touches );

		if( !multiTouchMode )
		{

			endPanGesture();

			multiTouchMode = true;
			State = dfGestureState.Possible;
			StartPosition = center;

		}
		else if( State == dfGestureState.Possible )
		{
			if( Vector2.Distance( center, StartPosition ) >= minDistance )
			{

				State = dfGestureState.Began;

				CurrentPosition = center;
				Delta = CurrentPosition - StartPosition;

				if( PanGestureStart != null ) PanGestureStart( this );
				gameObject.Signal( "OnPanGestureStart", this );

			}
		}
		else if( State == dfGestureState.Began || State == dfGestureState.Changed )
		{

			State = dfGestureState.Changed;

			Delta = center - CurrentPosition;
			CurrentPosition = center;

			if( PanGestureMove != null ) PanGestureMove( this );
			gameObject.Signal( "OnPanGestureMove", this );

		}

	}

	#endregion

	#region Private utillity methods 

	private Vector2 getCenter( List<dfTouchInfo> list )
	{

		var accum = Vector2.zero;
		for( int i = 0; i < list.Count; i++ )
		{
			accum += list[ i ].position;
		}

		return accum / list.Count;

	}

	private void endPanGesture()
	{

		Delta = Vector2.zero;
		StartPosition = Vector2.one * float.MinValue;

		if( State == dfGestureState.Began || State == dfGestureState.Changed )
		{
			State = dfGestureState.Ended;
			if( PanGestureEnd != null ) PanGestureEnd( this );
			gameObject.Signal( "OnPanGestureEnd", this );
		}
		else if( State == dfGestureState.Possible )
		{
			State = dfGestureState.Cancelled;
		}

	}

	#endregion 

}
