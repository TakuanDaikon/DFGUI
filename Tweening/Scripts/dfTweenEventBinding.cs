/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Provides Editor support for binding the events of a Component to 
/// the StartTween, StopTween, and Reset methods of a Tween component
/// without having to have a seperate EventBinding for each method
/// </summary>
[Serializable]
[AddComponentMenu( "Daikon Forge/Tweens/Tween Event Binding" )]
public class dfTweenEventBinding : MonoBehaviour
{

	#region Public fields 

	/// <summary>
	/// The Tween being controlled
	/// </summary>
	public Component Tween;

	/// <summary>
	/// The component whose events will be used
	/// </summary>
	public Component EventSource;

	/// <summary>
	/// The name of the event fired by <see cref="EventSource"/> that will 
	/// cause the StartTween method to be called
	/// </summary>
	public string StartEvent;

	/// <summary>
	/// The name of the event fired by <see cref="EventSource"/> that will 
	/// cause the StopTween method to be called
	/// </summary>
	public string StopEvent;

	/// <summary>
	/// The name of the event fired by <see cref="EventSource"/> that will 
	/// cause the Reset method to be called
	/// </summary>
	public string ResetEvent;

	#endregion

	#region Private variables 

	private bool isBound = false;

	private dfEventBinding startEventBinding;
	private dfEventBinding stopEventBinding;
	private dfEventBinding resetEventBinding;

	#endregion

	#region Unity events

	void OnEnable()
	{
		if( isValid() )
		{
			Bind();
		}
	}

	void Start()
	{
		if( isValid() )
		{
			Bind();
		}
	}

	void OnDisable()
	{
		Unbind();
	}

	#endregion

	#region Public methods 

	/// <summary>
	/// Binds the source events to the corresponding tween methods
	/// </summary>
	public void Bind()
	{

		if( isBound && !isValid() )
			return;

		isBound = true;

		if( !string.IsNullOrEmpty( StartEvent ) )
		{
			startEventBinding = bindEvent( StartEvent, "Play" );
		}

		if( !string.IsNullOrEmpty( StopEvent ) )
		{
			stopEventBinding = bindEvent( StopEvent, "Stop" );
		}

		if( !string.IsNullOrEmpty( ResetEvent ) )
		{
			resetEventBinding = bindEvent( ResetEvent, "Reset" );
		}

	}

	/// <summary>
	/// Unbinds all source component events
	/// </summary>
	public void Unbind()
	{

		if( !isBound )
			return;

		isBound = false;

		if( startEventBinding != null )
		{
			startEventBinding.Unbind();
			startEventBinding = null;
		}

		if( stopEventBinding != null )
		{
			stopEventBinding.Unbind();
			stopEventBinding = null;
		}

		if( resetEventBinding != null )
		{
			resetEventBinding.Unbind();
			resetEventBinding = null;
		}

	}

	#endregion

	#region Private utility methods

	private bool isValid()
	{

		if( Tween == null || !( Tween is dfTweenComponentBase ) )
			return false;

		if( EventSource == null )
			return false;

		var noEvents =
			string.IsNullOrEmpty( StartEvent ) &&
			string.IsNullOrEmpty( StopEvent ) &&
			string.IsNullOrEmpty( ResetEvent );

		if( noEvents )
			return false;

		var sourceType = EventSource.GetType();

		if( !string.IsNullOrEmpty( StartEvent ) && getField( sourceType, StartEvent ) == null )
			return false;

		if( !string.IsNullOrEmpty( StopEvent ) && getField( sourceType, StopEvent ) == null )
			return false;

		if( !string.IsNullOrEmpty( ResetEvent ) && getField( sourceType, ResetEvent ) == null )
			return false;

		return true;

	}

	private FieldInfo getField( Type type, string fieldName )
	{

		return
			type.GetAllFields()
			.Where( f => f.Name == fieldName )
			.FirstOrDefault();

	}

	private void unbindEvent( FieldInfo eventField, Delegate eventDelegate )
	{
		var currentDelegate = (Delegate)eventField.GetValue( EventSource );
		var newDelegate = Delegate.Remove( currentDelegate, eventDelegate );
		eventField.SetValue( EventSource, newDelegate );
	}

	private dfEventBinding bindEvent( string eventName, string handlerName )
	{

		var method = Tween.GetType().GetMethod( handlerName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
		if( method == null )
		{
			throw new MissingMemberException( "Method not found: " + handlerName );
		}

		var eventBinding = gameObject.AddComponent<dfEventBinding>();
		eventBinding.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;

		eventBinding.DataSource = new dfComponentMemberInfo()
		{
			Component = EventSource,
			MemberName = eventName
		};

		eventBinding.DataTarget = new dfComponentMemberInfo()
		{
			Component = Tween,
			MemberName = handlerName
		};

		eventBinding.Bind();

		return eventBinding;


	}

	#endregion

}
