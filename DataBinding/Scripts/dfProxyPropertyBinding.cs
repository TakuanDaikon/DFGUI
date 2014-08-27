/* Copyright 2013-2014 Daikon Forge */

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Used in conjuction with the <see cref="dfDataObjectProxy"/> component to allow
/// data binding for objects whose <i>Type</i> is known at design time but whose 
/// <i>value</i> will only be assigned at runtime.
/// </summary>
[Serializable]
[AddComponentMenu( "Daikon Forge/Data Binding/Proxy Property Binding" )]
public class dfProxyPropertyBinding : MonoBehaviour, IDataBindingComponent
{

	#region Public fields

	/// <summary>
	/// Specifies which field or property to bind to on the source proxy
	/// </summary>
	public dfComponentMemberInfo DataSource;

	/// <summary>
	/// Specifies which field or property to bind to on the target component
	/// </summary>
	public dfComponentMemberInfo DataTarget;

	/// <summary>
	/// If set to TRUE, the property will be synchronized 
	/// between DataSource and DataTarget. ie: When either 
	/// property changes, the other will be set to match.
	/// If set to FALSE, only changes to DataSource will 
	/// be mirrored to DataTarget.
	/// </summary>
	public bool TwoWay = false;

	#endregion

	#region Public properties

	/// <summary>
	/// Returns whether this component is currenly bound
	/// </summary>
	public bool IsBound { get { return this.isBound; } }

	#endregion

	#region Private fields

	private dfObservableProperty sourceProperty;
	private dfObservableProperty targetProperty;

	private bool isBound = false;
	private bool eventsAttached = false;

	#endregion

	#region Unity events

	public void Awake() { }

	public void OnEnable()
	{
		if( !isBound && IsDataSourceValid() && DataTarget.IsValid )
		{
			Bind();
		}
	}

	public void Start()
	{
		if( !isBound && IsDataSourceValid() && DataTarget.IsValid )
		{
			Bind();
		}
	}

	public void OnDisable()
	{
		Unbind();
	}

	public void Update()
	{

		if( sourceProperty == null || targetProperty == null )
			return;

		if( sourceProperty.HasChanged )
		{
			targetProperty.Value = sourceProperty.Value;
			sourceProperty.ClearChangedFlag();
		}
		else if( TwoWay && targetProperty.HasChanged )
		{
			sourceProperty.Value = targetProperty.Value;
			targetProperty.ClearChangedFlag();
		}

	}

	#endregion

	#region Public methods

	/// <summary>
	/// Bind the source and target properties 
	/// </summary>
	public void Bind()
	{

		if( isBound )
			return;

		if( !IsDataSourceValid() )
		{
			Debug.LogError( string.Format( "Invalid data binding configuration - Source:{0}, Target:{1}", DataSource, DataTarget ) );
			return;
		}

		if( !DataTarget.IsValid )
		{
			Debug.LogError( string.Format( "Invalid data binding configuration - Source:{0}, Target:{1}", DataSource, DataTarget ) );
			return;
		}

		var proxy = DataSource.Component as dfDataObjectProxy;

		sourceProperty = proxy.GetProperty( DataSource.MemberName );
		targetProperty = DataTarget.GetProperty();

		isBound = ( sourceProperty != null ) && ( targetProperty != null );

		if( isBound )
		{
			// Ensure that both properties are synced at start
			targetProperty.Value = sourceProperty.Value;
		}

		attachEvent();

	}

	/// <summary>
	/// Unbind the source and target properties 
	/// </summary>
	public void Unbind()
	{

		if( !isBound )
			return;

		detachEvent();

		sourceProperty = null;
		targetProperty = null;

		isBound = false;

	}

	#endregion

	#region Private utility methods 

	private bool IsDataSourceValid()
	{

		var sourceIsValid =
			DataSource != null ||
			DataSource.Component != null ||
			!string.IsNullOrEmpty( DataSource.MemberName ) ||
			( DataSource.Component as dfDataObjectProxy ).Data != null;

		return sourceIsValid;

	}

	private void attachEvent()
	{

		if( eventsAttached )
			return;

		eventsAttached = true;

		var proxy = DataSource.Component as dfDataObjectProxy;
		if( proxy != null )
		{
			proxy.DataChanged += handle_DataChanged;
		}

	}

	private void detachEvent()
	{

		if( !eventsAttached )
			return;

		eventsAttached = false;

		var proxy = DataSource.Component as dfDataObjectProxy;
		if( proxy != null )
		{
			proxy.DataChanged -= handle_DataChanged;
		}

	}

	private void handle_DataChanged( object data )
	{
		Unbind();
		if( IsDataSourceValid() )
		{
			Bind();
		}
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

}
