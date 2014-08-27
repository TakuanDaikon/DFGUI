// @cond DOXY_IGNORE
/* Copyright 2013-2014 Daikon Forge */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;

#if ( !UNITY_EDITOR && UNITY_METRO )

#region MemberTypes enumeration

[Flags]
public enum MemberTypes
{
	Constructor = 1,
	Event = 2,
	Field = 4,
	Method = 8,
	Property = 16,
	TypeInfo = 32,
	Custom = 64,
	NestedType = 128,
	All = 191,
}

#endregion

#region BindingFlags enumeration 

[Flags]
public enum BindingFlags
{
	Default = 0,
	IgnoreCase = 1,
	DeclaredOnly = 2,
	Instance = 4,
	Static = 8,
	Public = 16,
	NonPublic = 32,
	FlattenHierarchy = 64,
	InvokeMethod = 256,
	CreateInstance = 512,
	GetField = 1024,
	SetField = 2048,
	GetProperty = 4096,
	SetProperty = 8192,
	PutDispProperty = 16384,
	PutRefDispProperty = 32768,
	ExactBinding = 65536,
	SuppressChangeType = 131072,
	OptionalParamBinding = 262144,
	IgnoreReturn = 16777216,
}

#endregion 

#region Reflection methods to compensate for missing functionality on UNITY_METRO 

/// <summary>
/// Compensates for lack of common reflection functionality on Windows Metro
/// platform by creating extension methods that match the original .NET functions
/// in call-level signatures and functionality.
/// </summary>
public static class MetroReflectionExtensions
{

	public static Type[] EmptyTypes = new Type[ 0 ];

	#region GetEvent and EventInfo extension methods 

	public static EventInfo GetEvent( this System.Type type, string eventName )
	{
		return type.GetTypeInfo().DeclaredEvents.Where( x => x.Name == eventName ).FirstOrDefault();
	}

	public static MethodInfo GetAddMethod( this EventInfo eventInfo )
	{
		return eventInfo.AddMethod;
	}

	public static MethodInfo GetRemoveMethod( this EventInfo eventInfo )
	{
		return eventInfo.RemoveMethod;
	}

	#endregion 

	#region GetCustomAttributes() extension methods

	public static System.Attribute[] GetCustomAttributes( this Type type )
	{
		return GetCustomAttributes( type, true );
	}

	public static System.Attribute[] GetCustomAttributes( this Type type, bool inherited )
	{
		var typeInfo = type.GetTypeInfo();
		return typeInfo.GetCustomAttributes( inherited ).ToArray();
	}

	public static System.Attribute[] GetCustomAttributes( this Type type, Type attributeType, bool inherited )
	{
		var typeInfo = type.GetTypeInfo();
		return typeInfo.GetCustomAttributes( attributeType, inherited ).ToArray();
	}

#endregion 

#region System.Type extension methods

	public static bool IsAssignableFrom( this Type type, Type other )
	{
		return type.GetTypeInfo().IsAssignableFrom( other.GetTypeInfo() );
	}

	public static Type[] GetTypes( this Assembly assembly )
	{
		return assembly.ExportedTypes.ToArray();
	}

#endregion 

#region GetGetMethod and GetSetMethod replacements

	public static MethodInfo GetGetMethod( this PropertyInfo property )
	{
		return property.GetMethod;
	}

	public static MethodInfo GetSetMethod( this PropertyInfo property )
	{
		return property.SetMethod;
	}

#endregion

#region GetMember overloads

	public static MemberInfo[] GetMember( this Type type, string name )
	{
		return GetMember( type, name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public );
	}

	public static MemberInfo[] GetMember( this Type type, string name, BindingFlags flags )
	{
		return GetMembers( type, flags ).Where( x => x.Name == name ).ToArray();
	}

	public static MemberInfo[] GetMembers( this Type type )
	{
		return GetMembers( type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public );
	}

	public static MemberInfo[] GetMembers( this Type type, BindingFlags flags )
	{
		var list = new List<MemberInfo>();
		GetMembers( type, flags, list );
		return list.ToArray();
	}

#endregion 

#region GetField overloads

	public static FieldInfo[] GetFields( this Type type )
	{
		return GetFields( type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public );
	}

	public static FieldInfo[] GetFields( this Type type, BindingFlags flags )
	{

		var result = new List<FieldInfo>();

		GetFields( type, flags, result );

		return result.ToArray();

	}

	public static FieldInfo GetField( this Type type, string name )
	{
		return GetField( type, name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public );
	}

	public static FieldInfo GetField( this Type type, string name, BindingFlags flags )
	{

		var typeInfo = type.GetTypeInfo();

		while( typeInfo != null )
		{

			var field = typeInfo.GetDeclaredField( name );
			if( field != null )
			{
				if( !matchesBindingFlags( field, flags ) )
					return null;
				else
					return field;
			}

			if( typeInfo.BaseType == null || ( ( flags & BindingFlags.DeclaredOnly ) == BindingFlags.DeclaredOnly ) )
				return null;

			typeInfo = typeInfo.BaseType.GetTypeInfo();

		}

		return null;

	}

#endregion

#region GetProperty overloads

	public static PropertyInfo[] GetProperties( this Type type )
	{
		return GetProperties( type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public );
	}

	public static PropertyInfo[] GetProperties( this Type type, BindingFlags flags )
	{

		var result = new List<PropertyInfo>();

		GetProperties( type, flags, result );

		return result.ToArray();

	}

	public static PropertyInfo GetProperty( this Type type, string name )
	{
		return GetProperty( type, name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public );
	}

	public static PropertyInfo GetProperty( this Type type, string name, BindingFlags flags )
	{

		var typeInfo = type.GetTypeInfo();

		while( typeInfo != null )
		{

			var property = typeInfo.GetDeclaredProperty( name );
			if( property != null )
			{
				if( !matchesBindingFlags( property, flags ) )
					return null;
				else
					return property;
			}

			if( typeInfo.BaseType == null || ( ( flags & BindingFlags.DeclaredOnly ) == BindingFlags.DeclaredOnly ) )
				return null;

			typeInfo = typeInfo.BaseType.GetTypeInfo();

		}

		return null;

	}

#endregion 

#region GetMethod overloads

	public static MethodInfo GetMethod( this Type type, string name )
	{
		return GetMethod( type, name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public );
	}

	public static MethodInfo GetMethod( this Type type, string name, BindingFlags flags )
	{
		return GetMethod( type, name, flags, null, null, null );
	}

	public static MethodInfo GetMethod( this Type type, string name, BindingFlags flags, object binder, Type[] types, object parameterModifiers )
	{

		var typeInfo = type.GetTypeInfo();

		while( typeInfo != null )
		{

			var methods = typeInfo.DeclaredMethods;
			foreach( var method in methods )
			{

				if( method.Name != name )
					continue;

				if( !matchesBindingFlags( method, flags ) )
					continue;

				if( types == null )
					return method;

				if( matchesParameterTypes( method, types ) )
					return method;

			}

			if( ( flags & BindingFlags.DeclaredOnly ) == BindingFlags.DeclaredOnly )
				return null;

			var baseType = typeInfo.BaseType;
			if( baseType == null )
				return null;

			typeInfo = baseType.GetTypeInfo();

		}

		return null;

	}

	public static MethodInfo[] GetMethods( this Type type )
	{
		return GetMethods( type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public );
	}

	public static MethodInfo[] GetMethods( this Type type, BindingFlags flags )
	{
		var list = new List<MethodInfo>();
		GetMethods( type, flags, list );
		return list.ToArray();
	}

#endregion 

#region Private utility methods 

	private static void GetMembers( Type type, BindingFlags flags, List<MemberInfo> list )
	{

		var typeInfo = type.GetTypeInfo();
			
		do
		{

			var members = typeInfo.DeclaredMembers;
			foreach( var member in members )
			{

				if( member is MethodBase )
				{
					if( alreadyContainsMethod( list, (MethodBase)member ) )
						continue;
				}

				if( matchesBindingFlags( member, flags ) )
					list.Add( member );

			}

			if( typeInfo.BaseType != null )
			{

				if( typeInfo.BaseType == typeof( System.Object ) )
					flags = ( flags & ~BindingFlags.Static );

				typeInfo = typeInfo.BaseType.GetTypeInfo();

			}
			else
			{
				break;
			}


		} while( typeInfo != null && ( ( flags & BindingFlags.DeclaredOnly ) == 0 ) );

	}

	private static bool alreadyContainsMethod( List<MethodInfo> list, MethodBase method )
	{

		for( int i = 0; i < list.Count; i++ )
		{
			if( methodSignaturesMatch( list[ i ], method ) )
				return true;
		}

		return false;

	}

	private static bool alreadyContainsMethod( List<MemberInfo> list, MethodBase method )
	{
			
		for( int i = 0; i < list.Count; i++ )
		{
				
			var listMethod = list[ i ] as MethodBase;
			if( listMethod == null || listMethod.GetType() != method.GetType() )
				continue;

			if( methodSignaturesMatch( listMethod, method ) )
				return true;

		}

		return false;

	}

	private static bool methodSignaturesMatch( MethodBase lhs, MethodBase rhs )
	{

		if( lhs.Name != rhs.Name )
			return false;

		var lhsParams = lhs.GetParameters();
		var rhsParams = rhs.GetParameters();

		if( lhsParams.Length != rhsParams.Length )
			return false;

		for( int i = 0; i < lhsParams.Length; i++ )
		{
			if( lhsParams[ i ].ParameterType != rhsParams[ i ].ParameterType )
				return false;
		}

		return true;

	}

	private static void GetMethods( Type type, BindingFlags flags, List<MethodInfo> list )
	{

		var typeInfo = type.GetTypeInfo();

		do
		{

			var methods = typeInfo.DeclaredMethods;
			foreach( var method in methods )
			{

				if( alreadyContainsMethod( list, method ) )
					continue;

				if( matchesBindingFlags( method, flags ) )
					list.Add( method );

			}

			if( typeInfo.BaseType != null )
			{
				if( typeInfo.BaseType == typeof( System.Object ) )
					flags = ( flags & ~BindingFlags.Static );
				typeInfo = typeInfo.BaseType.GetTypeInfo();
			}
			else
			{
				break;
			}


		} while( typeInfo != null && ( ( flags & BindingFlags.DeclaredOnly ) == 0 ) );

	}

	private static void GetFields( Type type, BindingFlags flags, List<FieldInfo> list )
	{

		var typeInfo = type.GetTypeInfo();
		var canReturnPrivateFields = true;

		do
		{

			var fields = typeInfo.DeclaredFields;
			foreach( var field in fields )
			{

				// Standard .NET doesn't return private fields that are not directly
				// declared on the given type.
				if( !canReturnPrivateFields && field.IsPrivate )
					continue;

				if( matchesBindingFlags( field, flags ) )
				{
					list.Add( field );
				}

			}

			if( typeInfo.BaseType != null )
				typeInfo = typeInfo.BaseType.GetTypeInfo();
			else
				break;

			canReturnPrivateFields = false;

		} while( typeInfo != null && ( ( flags & BindingFlags.DeclaredOnly ) == 0 ) );

	}

	private static void GetProperties( Type type, BindingFlags flags, List<PropertyInfo> list )
	{

		var typeInfo = type.GetTypeInfo();

		do
		{

			var properties = typeInfo.DeclaredProperties;
			foreach( var property in properties )
			{
				if( matchesBindingFlags( property, flags ) )
					list.Add( property );
			}

			if( typeInfo.BaseType != null )
				typeInfo = typeInfo.BaseType.GetTypeInfo();
			else
				break;


		} while( typeInfo != null && ( ( flags & BindingFlags.DeclaredOnly ) == 0 ) );

	}

	private static bool matchesParameterTypes( MethodInfo method, Type[] types )
	{

		var parameters = method.GetParameters();
		if( parameters.Length != types.Length )
			return false;

		for( int i = 0; i < types.Length; i++ )
		{
			if( !parameters[ i ].ParameterType.IsAssignableFrom( types[ i ] ) )
				return false;
		}

		return true;

	}

	private static bool matchesBindingFlags( BindingFlags flags, bool isPublic, bool isStatic )
	{

		var checkInstance = ( flags & BindingFlags.Instance ) == BindingFlags.Instance;
		var checkStatic = ( flags & BindingFlags.Static ) == BindingFlags.Static;

		if( !checkInstance || !checkStatic )
		{

			if( !checkInstance && !checkStatic )
				return false;

			if( checkInstance )
			{
				if( isStatic )
					return false;
			}
			else if( !isStatic )
			{
				return false;
			}

		}

		bool checkPublic = ( flags & BindingFlags.Public ) == BindingFlags.Public;
		bool checkPrivate = ( flags & BindingFlags.NonPublic ) == BindingFlags.NonPublic;

		if( checkPublic && checkPrivate )
			return true;

		if( checkPublic ) return isPublic;
		if( checkPrivate ) return !isPublic;

		return true;

	}

	private static bool matchesBindingFlags( MemberInfo member, BindingFlags flags )
	{

		if( member is FieldInfo )
		{
			return matchesBindingFlags( (FieldInfo)member, flags );
		}
		else if( member is PropertyInfo )
		{
			return matchesBindingFlags( (PropertyInfo)member, flags );
		}
		else if( member is EventInfo )
		{
			return matchesBindingFlags( (EventInfo)member, flags );
		}
		else if( member is MethodBase )
		{
			return matchesBindingFlags( (MethodBase)member, flags );
		}
		else if( member is TypeInfo )
		{
			var typeInfo = member as TypeInfo;
			return matchesBindingFlags( flags, typeInfo.IsPublic, false );
		}
		else
		{
			throw new Exception( "Unhandled member type: " + member );
		}

	}

	private static bool matchesBindingFlags( EventInfo eventInfo, BindingFlags flags )
	{
		var eventMethod = eventInfo.AddMethod ?? eventInfo.RemoveMethod;
		return matchesBindingFlags( eventMethod, flags );
	}

	private static bool matchesBindingFlags( FieldInfo field, BindingFlags flags )
	{
		return matchesBindingFlags( flags, field.IsPublic, field.IsStatic );
	}

	private static bool matchesBindingFlags( PropertyInfo property, BindingFlags flags )
	{
		return matchesBindingFlags( property.GetMethod ?? property.SetMethod, flags );
	}

	private static bool matchesBindingFlags( MethodBase method, BindingFlags flags )
	{
		return matchesBindingFlags( flags, method.IsPublic, method.IsStatic );
	}

#endregion

}

#endregion

#endif

// @endcond DOXY_IGNORE

