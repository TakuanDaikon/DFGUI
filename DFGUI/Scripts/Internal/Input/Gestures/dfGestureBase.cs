using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public delegate void dfGestureEventHandler<T>( T gesture ) where T : dfGestureBase;

public abstract class dfGestureBase : MonoBehaviour
{

	#region Private runtime variables

	private dfControl control;

	#endregion

	#region Public properties

	/// <summary>
	/// Returns the current dfGestureState state of the gesture
	/// </summary>
	public dfGestureState State { get; protected set; }

	/// <summary>
	/// Returns the start position (in screen coordinates) of the gesture.
	/// For gestures that work on a single touch, this value will be the 
	/// first position that was touched. For gestures that work with multiple 
	/// touches simultaneously, this value will represent the center of the 
	/// initial group of touches.
	/// </summary>
	public Vector2 StartPosition { get; protected set; }

	/// <summary>
	/// Returns the current position (in screen coordinates) of the gesture.
	/// For gestures that work on a single touch, this value will be the 
	/// current touch position. For gestures that work with multiple 
	/// touches simultaneously, this value will represent the center of all
	/// active touches.
	/// </summary>
	public Vector2 CurrentPosition { get; protected set; }

	/// <summary>
	/// Returns the time (from Time.realtimeSinceStartup) that the gesture 
	/// became active
	/// </summary>
	public float StartTime { get; protected set; }

	/// <summary>
	/// Returns a reference to the control that this Gesture is attached to
	/// </summary>
	public dfControl Control
	{
		get
		{
			if( control == null ) control = GetComponent<dfControl>();
			return control;
		}
	}

	#endregion 

}
