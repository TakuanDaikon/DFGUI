/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Generic callback handler with reference to the control that raised the event
/// </summary>
/// <param name="control"></param>
[dfEventCategory( "General" )]
public delegate void ControlCallbackHandler( dfControl control );

/// <summary>
/// Delegate definition for control multi-touch events
/// </summary>
/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
/// <param name="touchData"></param>
[dfEventCategory( "Mouse Input" )]
public delegate void ControlMultiTouchEventHandler( dfControl control, dfTouchEventArgs touchData );

/// <summary>
/// Delegate definition for control mouse events
/// </summary>
/// <param name="control">The <see cref="dfControl"/> instance which is currently notified of the event</param>
/// <param name="mouseEvent">Contains information about the user mouse operation that triggered the event</param>
[dfEventCategory( "Mouse Input" )]
public delegate void MouseEventHandler( dfControl control, dfMouseEventArgs mouseEvent );

/// <summary>
/// Delegate definition for control keyboard events
/// </summary>
/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
/// <param name="keyEvent">Contains information about the user keyboard operation that triggered the event</param>
[dfEventCategory( "Keyboard Input" )]
public delegate void KeyPressHandler( dfControl control, dfKeyEventArgs keyEvent );

/// <summary>
/// Delegate definition for control drag and drop events
/// </summary>
/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
/// <param name="keyEvent">Contains information about the drag and drop operation that triggered the event</param>
[dfEventCategory( "Drag and Drop" )]
public delegate void DragEventHandler( dfControl control, dfDragEventArgs dragEvent );

/// <summary>
/// Delegate definition for control property change events
/// </summary>
/// <typeparam name="T">The data type of the property that has changed</typeparam>
/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
/// <param name="value">The new value of the associated property</param>
[dfEventCategory( "Properties" )]
public delegate void PropertyChangedEventHandler<T>( dfControl control, T value );

/// <summary>
/// Delegate definition for generic property change events
/// </summary>
/// <typeparam name="T">The data type of the property that has changed</typeparam>
/// <param name="sender">The object instance for which the event was generated</param>
/// <param name="value">The new value of the associated property</param>
[dfEventCategory( "Properties" )]
public delegate void ValueChangedEventHandler<T>( object sender, T value );

/// <summary>
/// Delegate definition for control hierarchy change events
/// </summary>
/// <param name="container">The <see cref="dfControl"/> instance for which the event was generated</param>
/// <param name="child">A reference to the child control that was added to or removed from the container</param>
[dfEventCategory( "Child Controls" )]
public delegate void ChildControlEventHandler( dfControl container, dfControl child );

/// <summary>
/// Delegate definition for control focus events
/// </summary>
/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
/// <param name="args">Contains information about the focus change event, including a reference to which control
/// (if any) lost focus and which control (if any) obtained input focus</param>
[dfEventCategory( "Focus" )]
public delegate void FocusEventHandler( dfControl control, dfFocusEventArgs args );

/// <summary>
/// Used by the dfScriptWizard class to display events in grouped categories
/// </summary>
[AttributeUsage( AttributeTargets.Delegate, Inherited = true, AllowMultiple = false )]
public class dfEventCategoryAttribute : System.Attribute
{

	public string Category { get; private set; }

	public dfEventCategoryAttribute( string category )
	{
		this.Category = category;
	}

}

/// <summary>
/// Base class for all dfControl events
/// </summary>
public class dfControlEventArgs
{

	#region Public properties

	/// <summary> The dfControl instance that this event was 
	/// originally generated for </summary>
	public dfControl Source { get; internal set; }

	/// <summary> Indicates whether this event has already been 
	/// processed by an event subscriber </summary>
	public bool Used { get; private set; }

	#endregion

	#region Constructor

	internal dfControlEventArgs( dfControl Target )
	{
		this.Source = Target;
	}

	#endregion

	#region Public methods

	/// <summary> 
	/// Set by an event subscriber to indicate that the mouse event has been processed. 
	/// If not called by an event subscriber, then the event will be "bubbled" up to the 
	/// parent for additional processing.
	/// </summary>
	public void Use()
	{
		this.Used = true;
	}

	#endregion

}

/// <summary>
/// Encapsulates information about a focus change event
/// </summary>
public class dfFocusEventArgs : dfControlEventArgs
{

	#region Public properties

	/// <summary>
	/// The control which received input focus
	/// </summary>
	public dfControl GotFocus { get { return Source; } }

	/// <summary>
	/// The control which lost input focus
	/// </summary>
	public dfControl LostFocus { get; private set; }

	#endregion

	#region Constructor

	internal dfFocusEventArgs( dfControl GotFocus, dfControl LostFocus )
		: base( GotFocus )
	{
		this.LostFocus = LostFocus;
	}

	#endregion

}

/// <summary>
/// Encapsulates information about a drag and drop operation
/// </summary>
public class dfDragEventArgs : dfControlEventArgs
{

	#region Public properties

	/// <summary>
	/// Represents the state of the drag and drop operation
	/// </summary>
	public dfDragDropState State { get; set; }

	/// <summary>
	/// User-defined data specified by the component being dragged
	/// </summary>
	public object Data { get; set; }

	/// <summary>
	/// The screen position (in pixels) of the current drag operation
	/// </summary>
	public Vector2 Position { get; set; }

	/// <summary>
	/// When a drag and drop operation has resulted in a successful drop,
	/// this property will be set to a reference to the drop target during
	/// the OnDragEnd message to the drag source
	/// </summary>
	public dfControl Target { get; set; }

	/// <summary>
	/// Returns the Ray that was used to raycast during the mouse event
	/// </summary>
	public Ray Ray { get; set; }

	#endregion

	#region Constructors

	internal dfDragEventArgs( dfControl source )
		: base( source )
	{
		State = dfDragDropState.None;
	}

	internal dfDragEventArgs( dfControl source, dfDragDropState state, object data, Ray ray, Vector2 position )
		: base( source )
	{
		this.Data = data;
		this.State = state;
		this.Position = position;
		this.Ray = ray;
	}

	#endregion

}

/// <summary>
/// Encapsulates information about a user key event
/// </summary>
public class dfKeyEventArgs : dfControlEventArgs
{

	#region Public properties

	/// <summary>
	/// The KeyCode that triggered the event
	/// </summary>
	public KeyCode KeyCode { get; set; }

	/// <summary>
	/// If KeyCode represents a printable character, this property will 
	/// contain the char representation of that character
	/// </summary>
	public char Character { get; set; }

	/// <summary>
	/// Indicates whether the CONTROL key was pressed when this event was triggered
	/// </summary>
	public bool Control { get; set; }

	/// <summary>
	/// Indicates whether the SHIFT key was pressed when this event was triggered
	/// </summary>
	public bool Shift { get; set; }

	/// <summary>
	/// Indicates whether the ALT key was pressed when this event was triggered
	/// </summary>
	public bool Alt { get; set; }

	#endregion

	#region Constructor

	internal dfKeyEventArgs( dfControl source, KeyCode Key, bool Control, bool Shift, bool Alt )
		: base( source )
	{
		this.KeyCode = Key;
		this.Control = Control;
		this.Shift = Shift;
		this.Alt = Alt;
	}

	#endregion

	#region System.Object overrides

	/// <summary>
	/// Returns a formatted string summarizing this object's state
	/// </summary>
	public override string ToString()
	{
		return string.Format( "Key: {0}, Control: {1}, Shift: {2}, Alt: {3}", KeyCode, Control, Shift, Alt );
	}

	#endregion

}

/// <summary>
/// Encapsulates data for the <see cref="MouseUp" />, 
/// <see cref="MouseDown" />, 
/// and <see cref="MouseMove" /> events.
/// </summary>
public class dfMouseEventArgs : dfControlEventArgs
{

	#region Public properties

	/// <summary>Gets which mouse button was pressed.</summary>
	public dfMouseButtons Buttons { get; private set; }

	/// <summary>Gets the number of times the mouse button was pressed and released.</summary>
	public int Clicks { get; private set; }

	/// <summary>Gets a signed currentIndex of the number of detents the mouse wheel has rotated. A detent is one notch of the mouse wheel.</summary>
	public float WheelDelta { get; private set; }

	/// <summary>Returns how much the mouse was moved since the last time the mouse was polled</summary>
	public Vector2 MoveDelta { get; set; }

	/// <summary>Gets the location of the mouse during the generating mouse event.</summary>
	/// <returns>A <see cref="Vector2" /> containing the x- and y- coordinate of the mouse, in pixels, relative to the top-left corner of the screen</returns>
	public Vector2 Position { get; set; }

	/// <summary>
	/// Returns the Ray that was used to raycast during the mouse event
	/// </summary>
	public Ray Ray { get; set; }

	#endregion

	#region Constructor

	/// <summary>Initializes a new instance of the <see cref="dfMouseEventArgs" /> class.</summary>
	/// <param name="Source">The <see cref="dfControl"/> that originally received this event notification</param>
	/// <param name="button">One of the <see cref="dfMouseButtons" /> values indicating which mouse button was pressed. </param>
	/// <param name="clicks">The number of times a mouse button was pressed. </param>
	/// <param name="ray">The <see cref="Ray"/> from the screen mouse location through the <paramref name="Source"/> control</param>
	/// <param name="location">The screen coordinates of a mouse click, in pixels. </param>
	/// <param name="wheel">A signed currentIndex of the number of detents the wheel has rotated. </param>
	public dfMouseEventArgs( dfControl Source, dfMouseButtons button, int clicks, Ray ray, Vector2 location, float wheel )
		: base( Source )
	{
		this.Buttons = button;
		this.Clicks = clicks;
		this.Position = location;
		this.WheelDelta = wheel;
		this.Ray = ray;
	}

	public dfMouseEventArgs( dfControl Source )
		: base( Source )
	{
		this.Buttons = dfMouseButtons.None;
		this.Clicks = 0;
		this.Position = Vector2.zero;
		this.WheelDelta = 0;
	}

	#endregion

}

/// <summary>
/// Encapsulates data for the <see cref="MouseUp" />, <see cref="MouseDown" />, 
/// and <see cref="MouseMove" /> events along with Touch-specific information.
/// </summary>
public class dfTouchEventArgs : dfMouseEventArgs
{

	#region Public properties

	/// <summary>
	/// The Touch event data
	/// </summary>
	public dfTouchInfo Touch { get; private set; }

	/// <summary>
	/// If the event is a multi-touch event, contains a Touch record for each 
	/// touch currently acting on the control
	/// </summary>
	public List<dfTouchInfo> Touches { get; private set; }

	/// <summary>
	/// Indicates whether the current event is a multi-touch event
	/// </summary>
	public bool IsMultiTouch { get { return this.Touches.Count > 1; } }

	#endregion

	#region Constructor

	/// <summary>Initializes a new instance of the <see cref="dfTouchEventArgs" /> class.</summary>
	/// <param name="Source">The <see cref="dfControl"/> that originally received this event notification</param>
	/// <param name="touch">A <see cref="Touch" /> record encapsulating the data describing the touch event. </param>
	/// <param name="ray">The <see cref="Ray"/> from the screen mouse location through the <paramref name="Source"/> control</param>
	public dfTouchEventArgs( dfControl Source, dfTouchInfo touch, Ray ray )
		: base( Source, dfMouseButtons.Left, touch.tapCount, ray, touch.position, 0f )
	{

		this.Touch = touch;
		this.Touches = new List<dfTouchInfo>() { touch };

		var deltaTime = Time.deltaTime;
		if( touch.deltaTime > float.Epsilon && deltaTime > float.Epsilon )
		{
			this.MoveDelta = touch.deltaPosition * ( deltaTime / touch.deltaTime );
		}
		else
		{
			this.MoveDelta = touch.deltaPosition;
		}

	}

	/// <summary>Initializes a new instance of the <see cref="dfTouchEventArgs" /> class.</summary>
	/// <param name="source">The <see cref="dfControl"/> that originally received this event notification</param>
	/// <param name="touches">A List of Touch events active for the Source control. </param>
	/// <param name="ray">The <see cref="Ray"/> from the first touch location through the <paramref name="Source"/> control</param>
	public dfTouchEventArgs( dfControl source, List<dfTouchInfo> touches, Ray ray )
		: this( source, touches.First(), ray )
	{
		this.Touches = touches;
	}

	public dfTouchEventArgs( dfControl Source )
		: base( Source )
	{
		this.Position = Vector2.zero;
	}

	#endregion

}
