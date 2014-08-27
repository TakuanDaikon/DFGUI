// @cond DOXY_IGNORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

/// <summary>
/// Uses the mouse to simulate Touch input. This is used primarily to test
/// the Touch processing code from within the Unity Editor, and probably 
/// has very little utility otherwise.
/// </summary>
public class dfMouseTouchInputSource : IDFTouchInputSource
{

	#region Public properties 

	public bool MirrorAlt { get; set; }
	public bool ParallelAlt { get; set; }

	#endregion

	#region Private runtime variables

	private List<dfTouchInfo> activeTouches = new List<dfTouchInfo>();
	private dfTouchTrackingInfo touch;
	private dfTouchTrackingInfo altTouch;

	#endregion 

	#region IDFTouchInputSource Members

	public int TouchCount
	{
		get
		{
			var count = 0;
			if( touch != null ) count += 1;
			if( altTouch != null ) count += 1;
			return count;
		}
	}

	public IList<dfTouchInfo> Touches
	{
		get
		{
			
			activeTouches.Clear();
			
			if( touch != null )
				activeTouches.Add( touch );

			if( altTouch != null )
				activeTouches.Add( altTouch );

			return activeTouches;

		}
	}

	public void Update()
	{

		if( Input.GetKey( KeyCode.LeftAlt ) && Input.GetMouseButtonDown( 1 ) )
		{

			if( altTouch != null )
			{
				altTouch.Phase = TouchPhase.Ended;
			}
			else
			{

				altTouch = new dfTouchTrackingInfo()
				{
					Phase = TouchPhase.Began,
					FingerID = 1,
					Position = Input.mousePosition
				};

			}
			
			return;

		}
		else if( Input.GetKeyUp( KeyCode.LeftAlt ) )
		{
			if( altTouch != null )
			{
				altTouch.Phase = TouchPhase.Ended;
				return;
			}
		}
		else if( altTouch != null )
		{
			
			if( altTouch.Phase == TouchPhase.Ended )
			{
				altTouch = null;
			}
			else if( altTouch.Phase == TouchPhase.Began || altTouch.Phase == TouchPhase.Moved )
			{
				altTouch.Phase = TouchPhase.Stationary;
			}

		}

		if( touch != null ) touch.IsActive = false;

		if( touch != null && Input.GetKeyDown( KeyCode.Escape ) )
		{
			touch.Phase = TouchPhase.Canceled;
		}
		else if( touch == null || touch.Phase != TouchPhase.Canceled )
		{

			if( Input.GetMouseButtonUp( 0 ) )
			{
				if( touch != null )
				{
					touch.Phase = TouchPhase.Ended;
				}
			}
			else if( Input.GetMouseButtonDown( 0 ) )
			{
				touch = new dfTouchTrackingInfo() 
				{ 
					FingerID = 0,
					Phase = TouchPhase.Began, 
					Position = Input.mousePosition 
				};
			}
			else if( touch != null && touch.Phase != TouchPhase.Ended )
			{

				var delta = (Vector2)Input.mousePosition - touch.Position;
				
				var moved = Vector2.Distance( Input.mousePosition, touch.Position ) > float.Epsilon;
				touch.Position = Input.mousePosition;
				touch.Phase = moved ? TouchPhase.Moved : TouchPhase.Stationary;

				if( moved && altTouch != null && ( MirrorAlt || ParallelAlt ) )
				{

					if( MirrorAlt )
						altTouch.Position -= delta;
					else
						altTouch.Position += delta;
					
					altTouch.Phase = TouchPhase.Moved;

				}

			}

		}

		if( touch != null && !touch.IsActive )
		{
			touch = null;
		}

	}

	public dfTouchInfo GetTouch( int index )
	{
		return Touches[ index ];
	}

	#endregion

}
