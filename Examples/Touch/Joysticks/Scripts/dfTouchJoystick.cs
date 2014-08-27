using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[Serializable]
public class dfTouchJoystick : MonoBehaviour
{

	#region Public enumerations

	/// <summary>
	/// Kind of virtual joystick
	/// </summary>
	public enum TouchJoystickType
	{
		/// <summary>
		/// Acts as a virtual controller stick
		/// </summary>
		Joystick,

		/// <summary>
		/// Acts as a laptop-style trackpad
		/// </summary>
		Trackpad
	}

	#endregion

	#region Static variables

	// joysticks in the scene
	private static Dictionary<string, dfTouchJoystick> joysticks = new Dictionary<string, dfTouchJoystick>();

	#endregion

	#region Public serialized fields

	// the ID of this joystick
	[SerializeField]
	public string JoystickID = "Joystick";

	[SerializeField]
	public TouchJoystickType JoystickType = TouchJoystickType.Joystick;

	// How far from center the thumb can move before being clamped
	[SerializeField]
	public int Radius = 80;

	// If position is within this radius it is ignored (returns 0,0)
	[SerializeField]
	public float DeadzoneRadius = 0.25f;

	// Whether to move the thumb to the touched position on touch start
	[SerializeField]
	public bool DynamicThumb = false;

	// Whether to hide the thumb when touch stops
	[SerializeField]
	public bool HideThumb = false;

	// The thumb control
	[SerializeField]
	public dfControl ThumbControl;

	// Visually represents the area inside of which the thumb control
	// can be mvoed.
	[SerializeField]
	public dfControl AreaControl;

	#endregion

	#region Private runtime variables

	private dfControl control;
	private Vector2 joystickPos = Vector2.zero;

	#endregion

	#region Public properties

	/// <summary>
	/// The position of this joystick, (0,0) when centered
	/// </summary>
	public Vector2 Position
	{
		get
		{
			return joystickPos;
		}
	}

	#endregion

	#region Static methods 

	/// <summary>
	/// Get the position of the given joystick
	/// </summary>
	public static Vector2 GetJoystickPosition( string joystickID )
	{

		if( !joysticks.ContainsKey( joystickID ) )
			throw new Exception( "Joystick not registered: " + joystickID );

		return joysticks[ joystickID ].Position;

	}

	/// <summary>
	/// Resets or recenters the joystick position for the indicated joystick
	/// </summary>
	public static void ResetJoystickPosition( string joystickID )
	{

		if( !joysticks.ContainsKey( joystickID ) )
			throw new Exception( "Joystick not registered: " + joystickID );

		var joystick = joysticks[ joystickID ];

		if( joystick.JoystickType == TouchJoystickType.Trackpad )
			joystick.joystickPos = Vector2.zero;
		else
			joystick.recenter();

	}

	#endregion

	#region Monobehavior events

	public void Start()
	{

		control = GetComponent<dfControl>();

		var isValidConfiguration =
			( JoystickType == TouchJoystickType.Trackpad && control != null ) ||
			( control != null && ThumbControl != null && AreaControl != null );

		if( !isValidConfiguration )
		{
			Debug.LogError( "Invalid virtual joystick configuration", this );
			this.enabled = false;
			return;
		}

		joysticks.Add( JoystickID, this );

		if( ThumbControl != null && HideThumb )
		{
			ThumbControl.Hide();
			if( DynamicThumb )
			{
				AreaControl.Hide();
			}
		}

		recenter();

	}

	public void OnDestroy()
	{
		joysticks.Remove( JoystickID );
	}

	#endregion

	#region dfControl events

	public void OnMouseDown( dfControl control, dfMouseEventArgs args )
	{

		if( JoystickType == TouchJoystickType.Trackpad )
			return;

		Vector2 touchPosition;
		control.GetHitPosition( args.Ray, out touchPosition, true );

		if( HideThumb )
		{
			ThumbControl.Show();
			AreaControl.Show();
		}

		if( DynamicThumb )
		{
			// Center thumb area around touch position
			AreaControl.RelativePosition = touchPosition - AreaControl.Size * 0.5f;
			centerThumbInArea();
		}
		else
		{
			// Center thumb area
			recenter();
		}

		processTouch( args );

	}

	public void OnMouseHover()
	{
		if( JoystickType == TouchJoystickType.Trackpad )
		{
			joystickPos = Vector2.zero;
		}
	}

	public void OnMouseMove( dfControl control, dfMouseEventArgs args )
	{

		if( JoystickType == TouchJoystickType.Trackpad && args.Buttons.IsSet( dfMouseButtons.Left ) )
		{
			joystickPos = args.MoveDelta * 0.25f;
			return;
		}

		if( args.Buttons.IsSet( dfMouseButtons.Left ) )
		{
			processTouch( args );
		}

	}

	public void OnMouseUp( dfControl control, dfMouseEventArgs args )
	{

		if( JoystickType == TouchJoystickType.Trackpad )
		{
			joystickPos = Vector2.zero;
			return;
		}

		// reset thumb position
		recenter();

		if( HideThumb )
		{
			ThumbControl.Hide();
			if( DynamicThumb )
			{
				AreaControl.Hide();
			}
		}

	}

	#endregion

	#region Private utility methods

	private void recenter()
	{

		if( JoystickType == TouchJoystickType.Trackpad )
			return;

		AreaControl.RelativePosition = ( control.Size - AreaControl.Size ) * 0.5f;

		var areaCenter = AreaControl.RelativePosition + (Vector3)AreaControl.Size * 0.5f;
		var thumbCenter = (Vector3)ThumbControl.Size * 0.5f;

		ThumbControl.RelativePosition = areaCenter - thumbCenter;

		joystickPos = Vector2.zero;

	}

	private void centerThumbInArea()
	{
		ThumbControl.RelativePosition = AreaControl.RelativePosition + (Vector3)( AreaControl.Size - ThumbControl.Size ) * 0.5f;
	}

	private void processTouch( dfMouseEventArgs evt )
	{

		var touchPosition = raycast( evt.Ray );

		// get touch pos local to thumb area center
		var areaCenter = AreaControl.RelativePosition + (Vector3)AreaControl.Size * 0.5f;
		Vector3 localTouchPos = (Vector3)touchPosition - areaCenter;

		// clamp to radius
		if( localTouchPos.magnitude > Radius )
		{
			localTouchPos = localTouchPos.normalized * Radius;
		}

		// set thumb position
		var thumbCenter = (Vector3)ThumbControl.Size * 0.5f;
		ThumbControl.RelativePosition = areaCenter - thumbCenter + localTouchPos;

		// Normalize the touch position 
		localTouchPos /= Radius;

		// inside deadzone
		if( localTouchPos.magnitude <= DeadzoneRadius )
		{
			joystickPos = Vector2.zero;
			return;
		}

		// Flip Y coordinate for easy processing as movement direction
		joystickPos = new Vector3( localTouchPos.x, -localTouchPos.y );

	}

	private Vector2 raycast( Ray ray )
	{

		var corners = control.GetCorners();
		var plane = new Plane( corners[0], corners[1], corners[3] );

		var distance = 0f;
		plane.Raycast( ray, out distance );

		var hitPoint = ray.GetPoint( distance );
		var relative = ( hitPoint - corners[ 0 ] ).Scale( 1, -1, 0 ) / control.GetManager().PixelsToUnits();

		return relative;

	}

	#endregion

}