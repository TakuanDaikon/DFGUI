/* Copyright 2013-2014 Daikon Forge */

using System;
using System.Linq;
using System.Reflection;

using UnityEngine;

/// <summary>
/// Encapsulates a <see cref="Component"/> reference and the name a member 
/// on that component that can be retrieved for data binding or event binding.
/// </summary>
[Serializable]
public class dfComponentMemberInfo
{

	#region Public fields 

	/// <summary>
	/// The <see cref="Component"/> instance to be bound
	/// </summary>
	public Component Component;

	/// <summary>
	/// The name of the member to be bound
	/// </summary>
	public string MemberName;

	#endregion

	#region Public properties 

	/// <summary>
	/// Returns TRUE if the configuration is valid, FALSE otherwise
	/// </summary>
	public bool IsValid
	{
		get
		{
				
			var propertiesSet =
				Component != null &&
				!string.IsNullOrEmpty( MemberName );

			if( !propertiesSet )
				return false;

			var member = Component.GetType().GetMember( MemberName ).FirstOrDefault();
			if( member == null )
				return false;

			return true;

		}
	}

	#endregion

	#region Public methods 

	/// <summary>
	/// Returns the <see cref="Type"/> of the configured member
	/// </summary>
	/// <returns>Returns the <see cref="Type"/> of the configured member</returns>
	public Type GetMemberType()
	{

		var componentType = Component.GetType();

		var member = componentType.GetMember( MemberName ).FirstOrDefault();
		if( member == null )
			throw new MissingMemberException( "Member not found: " + componentType.Name + "." + MemberName );

		if( member is FieldInfo )
			return ( (FieldInfo)member ).FieldType;

		if( member is PropertyInfo )
			return ( (PropertyInfo)member ).PropertyType;

		if( member is MethodInfo )
			return ( (MethodInfo)member ).ReturnType;

		if( member is EventInfo )
			return ( (EventInfo)member ).EventHandlerType;

		throw new InvalidCastException( "Invalid member type: " + member.GetMemberType() );

	}

	/// <summary>
	/// If the configured member is a method, returns the <see cref="MethodInfo"/>
	/// returned via Reflection
	/// </summary>
	/// <returns>A <see cref="MethodInfo"/> instance that can be used to invoke the configured member</returns>
	public MethodInfo GetMethod()
	{

		// NOTE: There is a bug in Unity 4.3.3+ on Windows Phone that causes all reflection 
		// method overloads that take a BindingFlags parameter to throw a runtime exception.
		// This means that we cannot have 100% compatibility between Unity 4.3.3 and prior
		// versions of Unity on the Windows Phone platform, and that some functionality 
		// will unfortunately be lost.

		var member = Component
			.GetType()
#if UNITY_EDITOR || !UNITY_WP8
			.GetMember( MemberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )
#else
			.GetMember( MemberName )
#endif
			.FirstOrDefault() as MethodInfo;

		return member;

	}

	/// <summary>
	/// If the configured member is a field or property, will return a 
	/// <see cref="dfObservableProperty"/> instance that can be used to
	/// query or assign the member's value
	/// </summary>
	/// <returns>a <see cref="dfObservableProperty"/> instance that can be used to
	/// query or assign the member's value</returns>
	public dfObservableProperty GetProperty()
	{

		var componentType = Component.GetType();

		var member = Component.GetType().GetMember( MemberName ).FirstOrDefault();
		if( member == null )
			throw new MissingMemberException( "Member not found: " + componentType.Name + "." + MemberName );

		if( !( member is FieldInfo ) && !( member is PropertyInfo ) )
			throw new InvalidCastException( "Member " + MemberName + " is not an observable field or property" );

		return new dfObservableProperty( Component, member );

	}

	#endregion

	#region System.Object overrides 

	/// <summary>
	/// Returns a formatted string summarizing this object's state
	/// </summary>
	public override string ToString()
	{
		string type = Component != null ? Component.GetType().Name : "[Missing ComponentType]";
		string member = !string.IsNullOrEmpty( MemberName ) ? MemberName : "[Missing MemberName]";
		return string.Format( "{0}.{1}", type, member );
	}

	#endregion

}
