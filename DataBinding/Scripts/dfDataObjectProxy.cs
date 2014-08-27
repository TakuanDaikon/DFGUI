/* Copyright 2013-2014 Daikon Forge */

using UnityEngine;

using System;
using System.Linq;
using System.Reflection;

/// <summary>
/// Provides a data-bindable proxy object that works with the <see cref="dfProxyPropertyBinding"/>
/// component to allow design-time data binding for objects whose <i>Type</i> is known at
/// design time but whose <i>value</i> can only be provided at runtime.
/// </summary>
[Serializable]
[AddComponentMenu( "Daikon Forge/Data Binding/Proxy Data Object" )]
public class dfDataObjectProxy : MonoBehaviour, IDataBindingComponent
{

	#region Events

	/// <summary>
	/// Defines a method signature for handling the DataChanged event
	/// </summary>
	/// <param name="data">A reference to the data object</param>
	[dfEventCategory( "Data Changed" )]
	public delegate void DataObjectChangedHandler( object data );

	/// <summary>
	/// Raised whenever the Data property is changed
	/// </summary>
	public event DataObjectChangedHandler DataChanged;

	#endregion

	#region Serialized fields

	[SerializeField]
	protected string typeName;

	#endregion

	#region Public properties

	/// <summary>
	/// Returns whether this component is currenly bound
	/// </summary>
	public bool IsBound { get { return this.data != null; } }

	/// <summary>
	/// The name of the <see cref="Type"/> of data that will be referenced by this proxy
	/// </summary>
	public string TypeName
	{
		get { return this.typeName; }
		set
		{
			if( this.typeName != value )
			{
				this.typeName = value;
				this.Data = null;
			}
		}
	}

	/// <summary>
	/// The <see cref="System.Type"/> of data that will be referenced by this proxy
	/// </summary>
	public Type DataType
	{
		get { return getTypeFromName( this.typeName ); }
	}

	/// <summary>
	/// Gets or sets the actual data object referenced by this proxy
	/// </summary>
	public object Data
	{
		get { return this.data; }
		set
		{
			if( !object.ReferenceEquals( value, this.data ) )
			{
					
				this.data = value;
					
				if( value != null )
					this.typeName = value.GetType().Name;
					
				if( DataChanged != null )
				{
					DataChanged( value );
				}

			}
		}
	}

	#endregion

	#region Private runtime variables 

	private object data;

	#endregion

	#region Unity events 

	public void Start()
	{
		var type = this.DataType;
		if( type == null )
		{
			Debug.LogError( "Unable to retrieve System.Type reference for type: " + this.TypeName );
		}
	}

	#endregion

	#region Public methods 

	/// <summary>
	/// Returns the <see cref="System.Type"/> of the named property 
	/// </summary>
	/// <param name="propertyName">The name of a field or property that is expected to be available on the object referenced by <see cref="Data"/></param>
	public Type GetPropertyType( string propertyName )
	{

		// NOTE: There is a bug in Unity 4.3.3+ on Windows Phone that causes all reflection 
		// method overloads that take a BindingFlags parameter to throw a runtime exception.
		// This means that we cannot have 100% compatibility between Unity 4.3.3 and prior
		// versions on the Windows Phone platform, and that some functionality 
		// will unfortunately be lost.

		var type = this.DataType;
		if( type == null )
			return null;

#if UNITY_EDITOR || !UNITY_WP8
		var member = type.GetMember( propertyName, BindingFlags.Public | BindingFlags.Instance ).FirstOrDefault();
#else
		var member = type.GetMember( propertyName ).FirstOrDefault();
#endif
		if( member is FieldInfo )
		{
			return ( (FieldInfo)member ).FieldType;
		}
		else if( member is PropertyInfo )
		{
			return ( (PropertyInfo)member ).PropertyType;
		}

		return null;

	}

	/// <summary>
	/// Returns a dfObservableProperty wrapper for the named property 
	/// </summary>
	/// <param name="PropertyName">The name of a field or property that is expected to be available on the object referenced by <see cref="Data"/></param>
	public dfObservableProperty GetProperty( string PropertyName )
	{

		if( data == null )
			return null;

		return new dfObservableProperty( data, PropertyName );

	}

	#endregion

	#region Private utility methods

	/// <summary>
	/// Returns a Type whose Name property matches the value specified in 
	/// the <paramref name="nameOfType"/> parameter, if possible. Only looks
	/// in the current Assembly.
	/// </summary>
	/// <param name="nameOfType">The value corresponding to the desired Type.Name property</param>
	/// <returns></returns>
	private Type getTypeFromName( string nameOfType )
	{

		if( nameOfType == null )
			throw new ArgumentNullException( "nameOfType" );

		var definedTypes =
			this.GetType()
			.GetAssembly()
			.GetTypes();

		var result = definedTypes.FirstOrDefault(t => t.Name == nameOfType);
		return result;

	}

	/// <summary>
	/// Returns a Type whose AssemblyQualifiedName property matches the value specified in 
	/// the <paramref name="typeName"/> parameter, if possible. If no match
	/// can be found in the current Assembly, will attempt to load the source
	/// assembly by matching the Assembly in the qualified type name
	/// </summary>
	/// <param name="typeName">The value corresponding to the desired Type.AssemblyQualifiedName property</param>
	/// <returns></returns>
	private static Type getTypeFromQualifiedName( string typeName )
	{
		
		// Try Type.GetType() first. This will work with types defined
		// by the Mono runtime, etc.
		var type = Type.GetType( typeName );

		// If it worked, then we're done here
		if( type != null )
			return type;

		// See if type name is qualified
		if( typeName.IndexOf( '.' ) == -1 )
			return null;

		// Get the name of the assembly (Assumption is that we are using 
		// fully-qualified type names)
		var assemblyName = typeName.Substring( 0, typeName.IndexOf( '.' ) );

		// Attempt to load the indicated Assembly
		var assembly = Assembly.Load( new AssemblyName( assemblyName ) );
		if( assembly == null )
			return null;

		// Ask that assembly to return the proper Type
		return assembly.GetType( typeName );

	}

	#endregion


	#region IDataBindingComponent Members

	public void Bind()
	{
		// Stub - Nothing to bind to 
	}

	public void Unbind()
	{
		// Stub - Nothing to unbind
	}

	#endregion

}
