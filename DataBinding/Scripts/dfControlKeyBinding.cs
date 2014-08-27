/* Copyright 2013-2014 Daikon Forge */

using System;
using UnityEngine;

/// <summary>
/// Provides Editor support for binding a keypress event on a
/// control to a compatible method on another Component
/// </summary>
[AddComponentMenu( "Daikon Forge/Data Binding/Key Binding" )]
[Serializable]
public class dfControlKeyBinding : MonoBehaviour, IDataBindingComponent
{

	#region Protected serialized members 

	[SerializeField]
	protected dfControl control;

	[SerializeField]
	protected KeyCode keyCode;

	[SerializeField]
	protected bool shiftPressed;

	[SerializeField]
	protected bool altPressed;

	[SerializeField]
	protected bool controlPressed;

	[SerializeField]
	protected dfComponentMemberInfo target;

	#endregion

	#region Public properties 

	/// <summary>
	/// The control whose KeyDown event will be bound
	/// </summary>
	public dfControl Control
	{
		get { return this.control; }
		set
		{
			if( isBound ) Unbind();
			this.control = value;
		}
	}

	/// <summary>
	/// They KeyCode value that will cause the target method to be invoked
	/// </summary>
	public KeyCode KeyCode
	{
		get { return this.keyCode; }
		set { this.keyCode = value; }
	}

	/// <summary>
	/// Set to TRUE if the ALT key must be pressed to trigger the event
	/// </summary>
	public bool AltPressed
	{
		get { return this.altPressed; }
		set { this.altPressed = value; }
	}

	/// <summary>
	/// Set to TRUE if the CONTROL key must be pressed to trigger the event
	/// </summary>
	public bool ControlPressed
	{
		get { return this.controlPressed; }
		set { this.controlPressed = value; }
	}

	/// <summary>
	/// Set to TRUE if the SHIFT key must be pressed to trigger the event
	/// </summary>
	public bool ShiftPressed
	{
		get { return this.shiftPressed; }
		set { this.shiftPressed = value; }
	}

	/// <summary>
	/// Information about the method that will be invoked 
	/// </summary>
	public dfComponentMemberInfo Target
	{
		get { return target; }
		set
		{
			if( isBound ) Unbind();
			this.target = value;
		}
	}

	/// <summary>
	/// Returns whether this component is currenly bound
	/// </summary>
	public bool IsBound { get { return this.isBound; } }

	#endregion

	#region Private instance variables

	private bool isBound = false;

	#endregion

	#region Unity events 

	public void Awake() { }
	public void OnEnable() { }

	public void Start() 
	{
		if( control != null && target.IsValid )
		{
			Bind();
		}
	} 

	#endregion

	#region Public methods

	/// <summary>
	/// Bind the key event to the target method
	/// </summary>
	public void Bind()
	{

		if( isBound )
		{
			Unbind();
		}

		if( control != null )
		{
			control.KeyDown += eventSource_KeyDown;
		}

		isBound = true;

	}

	/// <summary>
	/// Unbind the key event
	/// </summary>
	public void Unbind()
	{

		if( control != null )
		{
			control.KeyDown -= eventSource_KeyDown;
		}

		isBound = false;

	}

	#endregion

	#region Event handler 

	void eventSource_KeyDown( dfControl sourceControl, dfKeyEventArgs args )
	{

		// TODO: Check for args.Used?
		if( args.KeyCode != this.keyCode )
			return;

		var modifiersMatch =
			args.Shift == this.shiftPressed &&
			args.Control == this.controlPressed &&
			args.Alt == this.altPressed;

		if( !modifiersMatch )
			return;

		var method = target.GetMethod();
		method.Invoke( target.Component, null );

	}

	#endregion

}
