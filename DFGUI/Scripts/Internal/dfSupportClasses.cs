// @cond DOXY_IGNORE
/* Copyright 2013-2014 Daikon Forge */

using UnityEngine;

using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Describes the minimum required interface for poolable objects
/// </summary>
public interface IPoolable
{

	/// <summary>
	/// Release the object back to the object pool, freeing it for re-use
	/// </summary>
	void Release();

}

[Serializable]
public class dfDesignGuide
{
	public dfControlOrientation orientation;
	public int position;
}

/// <summary>
/// This is a "marker" exception that indicates that DFGUI should abort
/// the current rendering pass
/// </summary>
public class dfAbortRenderingException : System.Exception
{
}

/// <summary>
/// Implements clipboard copy/paste functionality in standalone and web player
/// deployment targets. <b>NOTE:</b> Because Unity does not provide access to
/// this functionality, this class uses reflection to obtain access to private
/// members of the GUIUtility class, and may not continue to work in future 
/// version of Unity.
/// </summary>
public class dfClipboardHelper
{

	private static PropertyInfo m_systemCopyBufferProperty = null;

	private static PropertyInfo GetSystemCopyBufferProperty()
	{

		if( m_systemCopyBufferProperty == null )
		{
			Type temp = typeof( GUIUtility );
			m_systemCopyBufferProperty = temp.GetProperty( "systemCopyBuffer", BindingFlags.Static | BindingFlags.NonPublic );
			if( m_systemCopyBufferProperty == null )
				throw new Exception( "Can't access internal member 'GUIUtility.systemCopyBuffer' it may have been removed / renamed" );
		}

		return m_systemCopyBufferProperty;

	}

	public static string clipBoard
	{
		get
		{
			try
			{
				PropertyInfo P = GetSystemCopyBufferProperty();
				return (string)P.GetValue( null, null );
			}
			catch
			{
				return "";
			}
		}
		set
		{
			try
			{
				PropertyInfo P = GetSystemCopyBufferProperty();
				P.SetValue( null, value, null );
			}
			catch { }
		}
	}

}

public static class dfStringExtensions
{

	/// <summary>
	/// Makes a file path relative to the Unity project's path
	/// </summary>
	public static string MakeRelativePath( this string path )
	{

		if( string.IsNullOrEmpty( path ) )
		{
			return "";
		}

		return path.Substring( path.IndexOf( "Assets/", StringComparison.OrdinalIgnoreCase ) );

	}

	/// <summary>
	/// Returns a value indicating whether the specified string pattern occurs
	/// within this string.
	/// </summary>
	/// <param name="pattern"></param>
	/// <param name="caseInsensitive"></param>
	/// <returns></returns>
	public static bool Contains( this string value, string pattern, bool caseInsensitive )
	{

		if( caseInsensitive )
		{
			return value.IndexOf( pattern, StringComparison.OrdinalIgnoreCase ) != -1;
		}

		return value.IndexOf( pattern ) != -1;

	}

}

public static class dfNumberExtensions
{

	/// <summary>
	/// Restricts the value to a discrete multiple of the value in the <paramref name="stepSize"/> parameter
	/// </summary>
	public static int Quantize( this int value, int stepSize )
	{

		if( stepSize <= 0 )
			return value;

		return ( value / stepSize ) * stepSize;

	}

	/// <summary>
	/// Restricts the value to a discrete multiple of the value in the <paramref name="stepSize"/> parameter
	/// </summary>
	public static float Quantize( this float value, float stepSize )
	{

		if( stepSize <= 0 ) 
			return value;

		return Mathf.Floor( value / stepSize ) * stepSize;

	}

	/// <summary>
	/// Restricts the value to a discrete multiple of the value in the <value>stepSize</value> parameter
	/// </summary>
	public static int RoundToNearest( this int value, int stepSize )
	{

		if( stepSize <= 0 ) 
			return value;

		var result = ( value / stepSize ) * stepSize;

		var remainder = value % stepSize;
		if( remainder >= stepSize / 2 )
			return result + stepSize;

		return result;

	}

	/// <summary>
	/// Restricts the value to a discrete multiple of the value in the <value>stepSize</value> parameter
	/// </summary>
	public static float RoundToNearest( this float value, float stepSize )
	{

		if( stepSize <= 0 )
			return value;

		var result = (float)Mathf.Floor( value / stepSize ) * stepSize;

		var remainder = value - ( stepSize * Mathf.Floor( value / stepSize ) );
		if( remainder >= stepSize * 0.5f - float.Epsilon )
			return result + stepSize;

		return result;

	}

}

public static class dfVectorExtensions
{

	/// <summary>
	/// Returns TRUE if any field in the Vector3 structure is float.NaN
	/// </summary>
	public static bool IsNaN( this Vector3 vector )
	{
		return
			float.IsNaN( vector.x ) ||
			float.IsNaN( vector.y ) ||
			float.IsNaN( vector.z );
	}

	/// <summary>
	/// Clamps an Euler rotation vector to 0-360 in all axes
	/// </summary>
	public static Vector3 ClampRotation( this Vector3 euler )
	{

		if( euler.x < 0 ) euler.x += 360; if( euler.x >= 360 ) euler.x -= 360;
		if( euler.y < 0 ) euler.y += 360; if( euler.y >= 360 ) euler.y -= 360;
		if( euler.z < 0 ) euler.z += 360; if( euler.z >= 360 ) euler.z -= 360;

		return euler;

	}

	public static Vector2 Scale( this Vector2 vector, float x, float y )
	{
		return new Vector2( vector.x * x, vector.y * y );
	}

	public static Vector3 Scale( this Vector3 vector, float x, float y, float z )
	{
		return new Vector3( vector.x * x, vector.y * y, vector.z * z );
	}

	public static Vector3 FloorToInt( this Vector3 vector )
	{
		return new Vector3(
			Mathf.FloorToInt( vector.x ),
			Mathf.FloorToInt( vector.y ),
			Mathf.FloorToInt( vector.z )
		);
	}

	public static Vector3 CeilToInt( this Vector3 vector )
	{
		return new Vector3(
			Mathf.CeilToInt( vector.x ),
			Mathf.CeilToInt( vector.y ),
			Mathf.CeilToInt( vector.z )
		);
	}

	public static Vector2 FloorToInt( this Vector2 vector )
	{
		return new Vector2(
			Mathf.FloorToInt( vector.x ),
			Mathf.FloorToInt( vector.y )
		);
	}

	public static Vector2 CeilToInt( this Vector2 vector )
	{
		return new Vector2(
			Mathf.CeilToInt( vector.x ),
			Mathf.CeilToInt( vector.y )
		);
	}

	public static Vector3 RoundToInt( this Vector3 vector )
	{
		return new Vector3( 
			Mathf.RoundToInt( vector.x ),
			Mathf.RoundToInt( vector.y ),
			Mathf.RoundToInt( vector.z )
		);
	}

	public static Vector2 RoundToInt( this Vector2 vector )
	{
		return new Vector2(
			Mathf.RoundToInt( vector.x ),
			Mathf.RoundToInt( vector.y )
		);
	}

	/// <summary>
	/// Restricts the values in the Vector2 to a discrete multiple of 
	/// the value in the <paramref name="discreteValue"/> parameter. 
	/// </summary>
	public static Vector2 Quantize( this Vector2 vector, float discreteValue )
	{
		vector.x = Mathf.RoundToInt( vector.x / discreteValue ) * discreteValue;
		vector.y = Mathf.RoundToInt( vector.y / discreteValue ) * discreteValue;
		return vector;
	}

	/// <summary>
	/// Restricts the values in the Vector2 to a discrete multiple of 
	/// the value in the <paramref name="discreteValue"/> parameter. 
	/// </summary>
	public static Vector3 Quantize( this Vector3 vector, float discreteValue )
	{
		vector.x = Mathf.RoundToInt( vector.x / discreteValue ) * discreteValue;
		vector.y = Mathf.RoundToInt( vector.y / discreteValue ) * discreteValue;
		vector.z = Mathf.RoundToInt( vector.z / discreteValue ) * discreteValue;
		return vector;
	}

}

public static class dfRectOffsetExtensions
{
	public static readonly RectOffset Empty = new RectOffset();
}

public static class dfRectExtensions
{

	public static RectOffset ConstrainPadding( this RectOffset borders )
	{

		if( borders == null )
			return new RectOffset();

		borders.left = Mathf.Max( 0, borders.left );
		borders.right = Mathf.Max( 0, borders.right );
		borders.top = Mathf.Max( 0, borders.top );
		borders.bottom = Mathf.Max( 0, borders.bottom );

		return borders;

	}

	/// <summary>
	/// Returns a value indicating whether a Rect is empty (has no volume)
	/// </summary>
	public static bool IsEmpty( this Rect rect )
	{
		return ( rect.xMin == rect.xMax ) || ( rect.yMin == rect.yMax );
	}

	/// <summary>
	/// Returns the intersection of two Rect objects
	/// </summary>
	public static Rect Intersection( this Rect a, Rect b )
	{

		if( !a.Intersects( b ) )
			return new Rect();

		float xmin = Mathf.Max( a.xMin, b.xMin );
		float xmax = Mathf.Min( a.xMax, b.xMax );
		float ymin = Mathf.Max( a.yMin, b.yMin );
		float ymax = Mathf.Min( a.yMax, b.yMax );

		return Rect.MinMaxRect( xmin, ymin, xmax, ymax );

	}

	/// <summary>
	/// Returns the Union of two Rects
	/// </summary>
	public static Rect Union( this Rect a, Rect b )
	{

		float xmin = Mathf.Min( a.xMin, b.xMin );
		float xmax = Mathf.Max( a.xMax, b.xMax );
		float ymin = Mathf.Min( a.yMin, b.yMin );
		float ymax = Mathf.Max( a.yMax, b.yMax );

		return Rect.MinMaxRect( xmin, ymin, xmax, ymax );

	}

	/// <summary>
	/// Returns a value indicating whether the Rect defined by <paramref name="other"/>
	/// is fully contained within the source Rect.
	/// </summary>
	public static bool Contains( this Rect rect, Rect other )
	{

		var left = rect.x <= other.x;
		var right = rect.x + rect.width >= other.x + other.width;
		var top = rect.yMin <= other.yMin;
		var bottom = rect.y + rect.height >= other.y + other.height;

		return left && right && top && bottom;

	}

	/// <summary>
	/// Returns a value indicating whether two Rect objects are overlapping
	/// </summary>
	public static bool Intersects( this Rect rect, Rect other )
	{

		var outside =
			rect.xMax < other.xMin ||
			rect.yMax < other.yMin ||
			rect.xMin > other.xMax ||
			rect.yMin > other.yMax;

		return !outside;

	}

	public static Rect RoundToInt( this Rect rect )
	{
		return new Rect(
			Mathf.RoundToInt( rect.x ),
			Mathf.RoundToInt( rect.y ),
			Mathf.RoundToInt( rect.width ),
			Mathf.RoundToInt( rect.height )
		);
	}

	public static string Debug( this Rect rect )
	{
		return string.Format( "[{0},{1},{2},{3}]", rect.xMin, rect.yMin, rect.xMax, rect.yMax );
	}

}

public static class dfReflectionExtensions
{

	public static Type[] EmptyTypes = new Type[ 0 ];

	public static MemberTypes GetMemberType( this MemberInfo member )
	{
#if !UNITY_EDITOR && UNITY_METRO
		if( member is FieldInfo )
			return MemberTypes.Field;
		if( member is ConstructorInfo )
			return MemberTypes.Constructor;
		if( member is PropertyInfo )
			return MemberTypes.Property;
		if( member is EventInfo )
			return MemberTypes.Event;
		if( member is MethodInfo )
			return MemberTypes.Method;

		var typeInfo = member as TypeInfo;
		if( typeInfo.IsNested )
			return MemberTypes.NestedType;

		return MemberTypes.TypeInfo; 
#else
		return member.MemberType;
#endif
	}

	public static Type GetBaseType( this Type type )
	{
#if !UNITY_EDITOR && UNITY_METRO
		return type.GetTypeInfo().BaseType;
#else
		return type.BaseType;
#endif
	}

	public static Assembly GetAssembly( this Type type )
	{
#if !UNITY_EDITOR && UNITY_METRO
			return type.GetTypeInfo().Assembly;
#else
		return type.Assembly;
#endif
	}

	/// <summary>
	/// Performs a SendMessage()-like event notification by searching the GameObject
	/// for components which have a method with the same name as the <paramref name="messageName"/>
	/// parameter and which have a signature that matches the types in the 
	/// <paramref name="args"/> array. Will walk up the GameObject hierarchy tree
	/// until the event is handled.
	/// </summary>
	/// <param name="target">The GameObject on which to raise the event</param>
	/// <param name="messageName">The name of the method to invoke</param>
	/// <param name="args">The parameters that will be passed to the method</param>
	/// <returns>Returns TRUE if a matching event handler was found and invoked</returns>
	[HideInInspector]
	internal static bool SignalHierarchy( this GameObject target, string messageName, params object[] args )
	{

		while( target != null )
		{

			if( Signal( target, messageName, args ) )
				return true;

			if( target.transform.parent == null )
				break;

			target = target.transform.parent.gameObject;

		}

		return false;

	}

	/// <summary>
	/// Performs a SendMessage()-like event notification by searching the GameObject
	/// for components which have a method with the same name as the <paramref name="messageName"/>
	/// parameter and which have a signature that matches the types in the 
	/// <paramref name="args"/> array. 
	/// </summary>
	/// <param name="target">The GameObject on which to raise the event</param>
	/// <param name="messageName">The name of the method to invoke</param>
	/// <param name="args">The parameters that will be passed to the method</param>
	/// <returns>Returns TRUE if a matching event handler was found and invoked</returns>
	[HideInInspector]
	internal static bool Signal( this GameObject target, string messageName, params object[] args )
	{

		// Retrieve the list of MonoBehaviour instances on the target object
		var components = target.GetComponents( typeof( MonoBehaviour ) );

		// Compile a list of Type definitions that defines the desired method signature
		var paramTypes = new Type[ args.Length ];
		for( int i = 0; i < paramTypes.Length; i++ )
		{
			if( args[ i ] == null )
			{
				paramTypes[ i ] = typeof( object );
			}
			else
			{
				paramTypes[ i ] = args[ i ].GetType();
			}
		}

		bool wasHandled = false;

		for( int i = 0; i < components.Length; i++ )
		{

			var component = components[ i ];

			// Should never happen, but seems to happen occasionally during a 
			// long recompile in the Editor. Unity bug?
			if( component == null || component.GetType() == null )
				continue;

			if( component is MonoBehaviour && !( (MonoBehaviour)component ).enabled )
				continue;

			#region First try to find a MethodInfo with the exact signature

			var handlerWithParams = getMethod( component.GetType(), messageName, paramTypes );

			IEnumerator coroutine = null;

			if( handlerWithParams != null )
			{

				coroutine = handlerWithParams.Invoke( component, args ) as IEnumerator;

				// If the target event handler returned an IEnumerator object,
				// assume that it should be run as a coroutine.
				if( coroutine != null )
				{
					( (MonoBehaviour)component ).StartCoroutine( coroutine );
				}

				wasHandled = true;

				continue;

			}

			#endregion

			if( args.Length == 0 )
				continue;

			#region Look for a parameterless method with the given name

			var handlerWithoutParams = getMethod( component.GetType(), messageName, dfReflectionExtensions.EmptyTypes );

			if( handlerWithoutParams != null )
			{

				coroutine = handlerWithoutParams.Invoke( component, null ) as IEnumerator;

				// If the target event handler returned an IEnumerator object,
				// assume that it should be run as a coroutine.
				if( coroutine != null )
				{
					( (MonoBehaviour)component ).StartCoroutine( coroutine );
				}

				wasHandled = true;

			}

			#endregion

		}

		return wasHandled;

	}

	private static MethodInfo getMethod( System.Type type, string name, System.Type[] paramTypes )
	{

		// NOTE: There is a bug in Unity 4.3.3+ on Windows Phone that causes all reflection 
		// method overloads that take a BindingFlags parameter to throw a runtime exception.
		// This means that we cannot have 100% compatibility between Unity 4.3.3 and prior
		// versions on the Windows Phone platform, and that some functionality 
		// will unfortunately be lost.

#if UNITY_EDITOR || !UNITY_WP8

		var method = type.GetMethod(
			name,
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
			null,
			paramTypes,
			null
		);

		return method;

#else

			var methods = type.GetMethods();
			for( int i = 0; i < methods.Length; i++ )
			{

				var info = methods[ i ];
				if( info.IsStatic || info.Name != name )
					continue;

				if( matchesParameterTypes( info, paramTypes ) )
					return info;

			}

			return null;

#endif

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

	/// <summary>
	/// Returns all instance fields on an object, including inherited fields
	/// </summary>
	internal static FieldInfo[] GetAllFields( this Type type )
	{

		// http://stackoverflow.com/a/1155549/154165

		if( type == null )
			return new FieldInfo[ 0 ];

#if UNITY_EDITOR || !UNITY_WP8

		BindingFlags flags = 
			BindingFlags.Public | 
			BindingFlags.NonPublic | 
			BindingFlags.Instance | 
			BindingFlags.DeclaredOnly;

		return
			type.GetFields( flags )
			.Concat( GetAllFields( type.GetBaseType() ) )
			.Where( f => !f.IsDefined( typeof( HideInInspector ), true ) )
			.ToArray();

#else

		// NOTE: There is a bug in Unity 4.3.3+ on Windows Phone that causes all reflection 
		// method overloads that take a BindingFlags parameter to throw a runtime exception.
		// This means that we cannot have 100% compatibility between Unity 4.3.3 and prior
		// versions of Unity on the Windows Phone platform, and that some functionality 
		// will unfortunately be lost.

		return
			type.GetFields()
			.Concat( GetAllFields( type.GetBaseType() ) )
			.Where( f => !f.IsDefined( typeof( HideInInspector ), true ) )
			.ToArray();

#endif

	}

}

// @endcond DOXY_IGNORE
