/* Copyright 2013-2014 Daikon Forge */

using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Provides Editor support for binding any event on a source Component
/// to a compatible event handler on a target Component
/// </summary>
[AddComponentMenu( "Daikon Forge/Data Binding/Event Binding" )]
[Serializable]
public class dfEventBinding : MonoBehaviour, IDataBindingComponent
{

	#region Public fields

	/// <summary>
	/// Specifies which event on the source component to bind to
	/// </summary>
	public dfComponentMemberInfo DataSource;

	/// <summary>
	/// Specifies which method on the target component to invoke when 
	/// the source event is triggered
	/// </summary>
	public dfComponentMemberInfo DataTarget;

	/// <summary>
	/// If set to TRUE (default), this component will attempt to bind the 
	/// event handler when the component is Enabled.
	/// </summary>
	public bool AutoBind = true;

	/// <summary>
	/// If set to TRUE (default), this component will unbind the event
	/// handler (if bound) when the component is Disabled.
	/// </summary>
	public bool AutoUnbind = true;

	#endregion

	#region Public properties

	/// <summary>
	/// Returns whether this component is currenly bound
	/// </summary>
	public bool IsBound { get { return this.isBound; } }

	#endregion

	#region Private fields

	private bool isBound;

	private Component sourceComponent;
	private Component targetComponent;

	private EventInfo eventInfo;
	private FieldInfo eventField;
	private Delegate eventDelegate;
	private MethodInfo handlerProxy;
	private ParameterInfo[] handlerParameters;

	#endregion

	#region Unity events

	public void OnEnable()
	{
		if( AutoBind && DataSource != null && !isBound && DataSource.IsValid && DataTarget.IsValid )
		{
			Bind();
		}
	}

	public void Start()
	{
		if( AutoBind && DataSource != null && !isBound && DataSource.IsValid && DataTarget.IsValid )
		{
			Bind();
		}
	}

	public void OnDisable()
	{
		if( AutoUnbind )
		{
			Unbind();
		}
	}

	public void OnDestroy()
	{
		Unbind();
	}

	#endregion

	#region Public methods 

	/// <summary>
	/// Bind the source event to the target event handler
	/// </summary>
	public void Bind()
	{

		if( isBound || DataSource == null )
			return;

		if( !DataSource.IsValid || !DataTarget.IsValid )
		{
			Debug.LogError( string.Format( "Invalid event binding configuration - Source:{0}, Target:{1}", DataSource, DataTarget ) );
			return;
		}

		sourceComponent = DataSource.Component;
		targetComponent = DataTarget.Component;

		var eventHandler = DataTarget.GetMethod();
		if( eventHandler == null )
		{
			Debug.LogError( "Event handler not found: " + targetComponent.GetType().Name + "." + DataTarget.MemberName );
			return;
		}

		if( bindToEventProperty( eventHandler ) )
		{
			isBound = true;
			return;
		}

		if( bindToEventField( eventHandler ) )
		{
			isBound = true;
			return;
		}

	}

	/// <summary>
	/// Unbind the source event and target event handler
	/// </summary>
	public void Unbind()
	{

		if( !isBound )
			return;

		isBound = false;

		if( eventField != null )
		{
			var currentDelegate = (Delegate)eventField.GetValue( sourceComponent );
			var newDelegate = Delegate.Remove( currentDelegate, eventDelegate );
			eventField.SetValue( sourceComponent, newDelegate );
		}
		else if( eventInfo != null )
		{
			var removeMethod = eventInfo.GetRemoveMethod();
			removeMethod.Invoke( sourceComponent, new object[] { eventDelegate } );
		}

		eventInfo = null;
		eventField = null; 
		eventDelegate = null;
		handlerProxy = null;

		sourceComponent = null;
		targetComponent = null;

	}

	#endregion

	#region System.Object overrides 

	/// <summary>
	/// Returns a formatted string summarizing this object's state
	/// </summary>
	public override string ToString()
	{

		string sourceType = DataSource != null && DataSource.Component != null ? DataSource.Component.GetType().Name : "[null]";
		string sourceMember = DataSource != null && !string.IsNullOrEmpty( DataSource.MemberName ) ? DataSource.MemberName : "[null]";

		string targetType = DataTarget != null && DataTarget.Component != null ? DataTarget.Component.GetType().Name : "[null]";
		string targetMember = DataTarget != null && !string.IsNullOrEmpty( DataTarget.MemberName ) ? DataTarget.MemberName : "[null]";

		return string.Format( "Bind {0}.{1} -> {2}.{3}", sourceType, sourceMember, targetType, targetMember );

	}

	#endregion

	#region Proxy event handlers

	[HideInInspector]
	[dfEventProxy]
	public void NotificationEventProxy()
	{
		callProxyEventHandler();
	}

	[HideInInspector]
	[dfEventProxy]
	public void GenericCallbackProxy( object sender )
	{
		callProxyEventHandler( sender );
	}

	/// <summary>
	///  definition for tween events
	/// </summary>
	/// <param name="tween">The <see cref="dfTweenPlayableBase"/> instance which is raising the event</param>
	[HideInInspector]
	[dfEventProxy]
	public void AnimationEventProxy( dfTweenPlayableBase tween )
	{
		callProxyEventHandler( tween );
	}

	/// <summary>
	///  definition for control mouse events
	/// </summary>
	/// <param name="control">The <see cref="dfControl"/> instance which is currently notified of the event</param>
	/// <param name="mouseEvent">Contains information about the user mouse operation that triggered the event</param>
	[HideInInspector]
	[dfEventProxy]
	public void MouseEventProxy( dfControl control, dfMouseEventArgs mouseEvent )
	{
		callProxyEventHandler( control, mouseEvent );
	}

	/// <summary>
	///  definition for control keyboard events
	/// </summary>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="keyEvent">Contains information about the user keyboard operation that triggered the event</param>
	[HideInInspector]
	[dfEventProxy]
	public void KeyEventProxy( dfControl control, dfKeyEventArgs keyEvent )
	{
		callProxyEventHandler( control, keyEvent );
	}

	/// <summary>
	///  definition for control drag and drop events
	/// </summary>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="keyEvent">Contains information about the drag and drop operation that triggered the event</param>
	[HideInInspector]
	[dfEventProxy]
	public void DragEventProxy( dfControl control, dfDragEventArgs dragEvent )
	{
		callProxyEventHandler( control, dragEvent );
	}

	/// <summary>
	///  definition for control hierarchy change events
	/// </summary>
	/// <param name="container">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="child">A reference to the child control that was added to or removed from the container</param>
	[HideInInspector]
	[dfEventProxy]
	public void ChildControlEventProxy( dfControl container, dfControl child )
	{
		callProxyEventHandler( container, child );
	}

	/// <summary>
	///  definition for control focus events
	/// </summary>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="args">Contains information about the focus change event, including a reference to which control
	/// (if any) lost focus and which control (if any) obtained input focus</param>
	[HideInInspector]
	[dfEventProxy]
	public void FocusEventProxy( dfControl control, dfFocusEventArgs args )
	{
		callProxyEventHandler( control, args );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, int value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
		/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, float value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, bool value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, string value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, Vector2 value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, Vector3 value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, Vector4 value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, Quaternion value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, dfButton.ButtonState value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, dfPivotPoint value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, Texture value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, Texture2D value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for control property change events
	/// </summary>
	/// <typeparam name="T">The data type of the property that has changed</typeparam>
	/// <param name="control">The <see cref="dfControl"/> instance for which the event was generated</param>
	/// <param name="value">The new value of the associated property</param>
	[HideInInspector]
	[dfEventProxy]
	public void PropertyChangedProxy( dfControl control, Material value )
	{
		callProxyEventHandler( control, value );
	}

	/// <summary>
	/// Delegate definition for standard .NET event handlers
	/// </summary>
	/// <param name="sender">Should contain a reference to the object which raised the event</param>
	/// <param name="args">The EventArgs (or descendent type) that contains information about the specific event</param>
	[HideInInspector]
	[dfEventProxy]
	public void SystemEventHandlerProxy( object sender, EventArgs args )
	{
		callProxyEventHandler( sender, args );
	}

	#endregion

	#region Private utility methods

	private bool bindToEventField( MethodInfo eventHandler )
	{

		eventField = getField( sourceComponent, DataSource.MemberName );
		if( eventField == null )
		{
			return false;
		}

		try
		{

			var eventMethod = eventField.FieldType.GetMethod( "Invoke" );
			var eventParams = eventMethod.GetParameters();
			var handlerParams = eventHandler.GetParameters();

			var needProxyDelegate =
				( eventParams.Length != handlerParams.Length ) ||
				( eventMethod.ReturnType != eventHandler.ReturnType );

			if( !needProxyDelegate )
			{
#if !UNITY_EDITOR && UNITY_METRO
				eventDelegate = eventHandler.CreateDelegate( eventField.FieldType, targetComponent );
#else
				eventDelegate = Delegate.CreateDelegate( eventField.FieldType, targetComponent, eventHandler, true );
#endif
			}
			else
			{
				eventDelegate = createEventProxyDelegate( targetComponent, eventField.FieldType, eventParams, eventHandler );
			}

			var combinedDelegate = Delegate.Combine( eventDelegate, (Delegate)eventField.GetValue( sourceComponent ) );
			eventField.SetValue( sourceComponent, combinedDelegate );

		}
		catch( Exception err )
		{
			this.enabled = false;
			var errMessage = string.Format( "Event binding failed - Failed to create event handler for {0} ({1}) - {2}", DataSource, eventHandler, err.ToString() );
			Debug.LogError( errMessage, this );
			return false;
		}

		return true;

	}

	private bool bindToEventProperty( MethodInfo eventHandler )
	{

		eventInfo = sourceComponent.GetType().GetEvent( DataSource.MemberName );
		if( eventInfo == null )
			return false;

		try
		{

			var eventDelegateType = eventInfo.EventHandlerType;
			var addMethod = eventInfo.GetAddMethod();

			var eventMethod = eventDelegateType.GetMethod( "Invoke" );
			var eventParams = eventMethod.GetParameters();
			var handlerParams = eventHandler.GetParameters();

			var needProxyDelegate =
				( eventParams.Length != handlerParams.Length ) ||
				( eventMethod.ReturnType != eventHandler.ReturnType );

			if( !needProxyDelegate )
			{
#if !UNITY_EDITOR && UNITY_METRO
				eventDelegate = eventHandler.CreateDelegate( eventDelegateType, targetComponent );
#else
				eventDelegate = Delegate.CreateDelegate( eventDelegateType, targetComponent, eventHandler, true );
#endif

			}
			else
			{
				eventDelegate = createEventProxyDelegate( targetComponent, eventDelegateType, eventParams, eventHandler );
			}

			addMethod.Invoke( DataSource.Component, new object[] { eventDelegate } );

		}
		catch( Exception err )
		{
			this.enabled = false;
			var errMessage = string.Format( "Event binding failed - Failed to create event handler for {0} ({1}) - {2}", DataSource, eventHandler, err.ToString() );
			Debug.LogError( errMessage, this );
			return false;
		}

		return true;

	}

	private void callProxyEventHandler( params object[] arguments )
	{
		
		if( handlerProxy == null )
			return;

		if( handlerParameters.Length == 0 )
			arguments = null;

		var result = handlerProxy.Invoke( targetComponent, arguments );

		if( !( result is IEnumerator ) )
			return;

		if( targetComponent is MonoBehaviour )
		{
			( (MonoBehaviour)targetComponent ).StartCoroutine( (IEnumerator)result );
		}

	}

	private static FieldInfo getField( Component component, string fieldName )
	{

		if( component == null )
			throw new ArgumentNullException( "component" );

		return
			component.GetType()
			.GetAllFields()
			.FirstOrDefault(f => f.Name == fieldName);

	}

	/// <summary>
	/// Creates a Delegate wrapper that allows a parameterless method to be used as 
	/// an event handler for an event that defines parameters. This enables the use
	/// of "notification" event handlers - Methods which either cannot make use of
	/// or don't care about event parameters. 
	/// </summary>
	private Delegate createEventProxyDelegate( object target, Type delegateType, ParameterInfo[] eventParams, MethodInfo eventHandler )
	{

		var proxyMethod = typeof( dfEventBinding )
#if UNITY_EDITOR || !UNITY_WP8
			.GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )
#else
			.GetMethods()
#endif
			.Where( m =>
				m.IsDefined( typeof( dfEventProxyAttribute ), true ) &&
				signatureIsCompatible( eventParams, m.GetParameters() )
			)
			.FirstOrDefault();

		if( proxyMethod == null )
		{
			return null;
		}

		this.handlerProxy = eventHandler;
		this.handlerParameters = eventHandler.GetParameters();

		Delegate createdDelegate;
#if !UNITY_EDITOR && UNITY_METRO
		createdDelegate = proxyMethod.CreateDelegate( delegateType, this );
#else
		createdDelegate = Delegate.CreateDelegate( delegateType, this, proxyMethod, true );
#endif

		return createdDelegate;

	}

	private bool signatureIsCompatible( ParameterInfo[] lhs, ParameterInfo[] rhs )
	{

		if( lhs == null || rhs == null )
			return false;

		if( lhs.Length != rhs.Length )
			return false;

		for( var i = 0; i < lhs.Length; i++ )
		{
			if( !areTypesCompatible( lhs[i], rhs[i] ) )
				return false;
		}

		return true;

	}

	private bool areTypesCompatible( ParameterInfo lhs, ParameterInfo rhs )
	{

		if( lhs.ParameterType.Equals( rhs.ParameterType ) )
			return true;

		if( lhs.ParameterType.IsAssignableFrom( rhs.ParameterType ) )
			return true;

		return false;

	}

	#endregion

}
