/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Allows easy animation (tweening) of float values
/// </summary>
public class dfAnimatedFloat : dfAnimatedValue<float>
{

	// @cond DOXY_IGNORE 

	public dfAnimatedFloat( float StartValue, float EndValue, float Time )
		: base( StartValue, EndValue, Time )
	{
	}

	protected override float Lerp( float startValue, float endValue, float time )
	{
		return Mathf.Lerp( startValue, endValue, time );
	}

	public static implicit operator dfAnimatedFloat( float value )
	{
		return new dfAnimatedFloat( value, value, 0f );
	}

	// @endcond

}

/// <summary>
/// Allows easy animation (tweening) of integer values
/// </summary>
public class dfAnimatedInt : dfAnimatedValue<int>
{

	// @cond DOXY_IGNORE 

	public dfAnimatedInt( int StartValue, int EndValue, float Time )
		: base( StartValue, EndValue, Time )
	{
	}

	protected override int Lerp( int startValue, int endValue, float time )
	{
		return Mathf.RoundToInt( Mathf.Lerp( startValue, endValue, time ) );
	}

	public static implicit operator dfAnimatedInt( int value )
	{
		return new dfAnimatedInt( value, value, 0f );
	}

	// @endcond

}

/// <summary>
/// Allows easy animation (tweening) of Vector3 values
/// </summary>
public class dfAnimatedVector3 : dfAnimatedValue<Vector3>
{

	// @cond DOXY_IGNORE 

	public dfAnimatedVector3( Vector3 StartValue, Vector3 EndValue, float Time )
		: base( StartValue, EndValue, Time )
	{
	}

	protected override Vector3 Lerp( Vector3 startValue, Vector3 endValue, float time )
	{
		return Vector3.Lerp( startValue, endValue, time );
	}

	public static implicit operator dfAnimatedVector3( Vector3 value )
	{
		return new dfAnimatedVector3( value, value, 0f );
	}

	// @endcond

}

/// <summary>
/// Allows easy animation (tweening) of Vector4 values
/// </summary>
public class dfAnimatedVector4 : dfAnimatedValue<Vector4>
{

	// @cond DOXY_IGNORE 

	public dfAnimatedVector4( Vector4 StartValue, Vector4 EndValue, float Time )
		: base( StartValue, EndValue, Time )
	{
	}

	protected override Vector4 Lerp( Vector4 startValue, Vector4 endValue, float time )
	{
		return Vector4.Lerp( startValue, endValue, time );
	}

	public static implicit operator dfAnimatedVector4( Vector4 value )
	{
		return new dfAnimatedVector4( value, value, 0f );
	}

	// @endcond

}

/// <summary>
/// Allows easy animation (tweening) of Vector2 values
/// </summary>
public class dfAnimatedVector2 : dfAnimatedValue<Vector2>
{

	// @cond DOXY_IGNORE 

	public dfAnimatedVector2( Vector2 StartValue, Vector2 EndValue, float Time )
		: base( StartValue, EndValue, Time )
	{
	}

	protected override Vector2 Lerp( Vector2 startValue, Vector2 endValue, float time )
	{
		return Vector2.Lerp( startValue, endValue, time );
	}

	public static implicit operator dfAnimatedVector2( Vector2 value )
	{
		return new dfAnimatedVector2( value, value, 0f );
	}

	// @endcond

}

/// <summary>
/// Allows easy animation (tweening) of Quaternion values
/// </summary>
public class dfAnimatedQuaternion : dfAnimatedValue<Quaternion>
{

	// @cond DOXY_IGNORE 

	public dfAnimatedQuaternion( Quaternion StartValue, Quaternion EndValue, float Time )
		: base( StartValue, EndValue, Time )
	{
	}

	protected override Quaternion Lerp( Quaternion startValue, Quaternion endValue, float time )
	{
		return Quaternion.Lerp( startValue, endValue, time );
	}

	public static implicit operator dfAnimatedQuaternion( Quaternion value )
	{
		return new dfAnimatedQuaternion( value, value, 0f );
	}

	// @endcond

}

/// <summary>
/// Allows easy animation (tweening) of Color values
/// </summary>
public class dfAnimatedColor : dfAnimatedValue<Color>
{

	// @cond DOXY_IGNORE 

	public dfAnimatedColor( Color StartValue, Color EndValue, float Time )
		: base( StartValue, EndValue, Time )
	{
	}

	protected override Color Lerp( Color startValue, Color endValue, float time )
	{
		return Color.Lerp( startValue, endValue, time );
	}

	public static implicit operator dfAnimatedColor( Color value )
	{
		return new dfAnimatedColor( value, value, 0f );
	}

	// @endcond

}

/// <summary>
/// Allows easy animation (tweening) of Color32 values
/// </summary>
public class dfAnimatedColor32 : dfAnimatedValue<Color32>
{

	// @cond DOXY_IGNORE 

	public dfAnimatedColor32( Color32 StartValue, Color32 EndValue, float Time )
		: base( StartValue, EndValue, Time )
	{
	}

	protected override Color32 Lerp( Color32 startValue, Color32 endValue, float time )
	{
		return Color.Lerp( startValue, endValue, time );
	}

	public static implicit operator dfAnimatedColor32( Color32 value )
	{
		return new dfAnimatedColor32( value, value, 0f );
	}

	// @endcond

}

/// <summary>
/// Allows simple and easy animation (tweening) of value types.
/// This class allows you to specify an <a href="http://www.robertpenner.com/easing/" target="_blank">easing function</a>
/// that will be applied to the value over time to control the shape of the animation curve.
/// </summary>
public abstract class dfAnimatedValue<T> where T: struct
{

	#region Private variables 

	private T startValue;
	private T endValue;

	private float animLength = 1f;
	private float startTime;
	private bool isDone = false;

	private dfEasingType easingType = dfEasingType.Linear;
	private dfEasingFunctions.EasingFunction easingFunction;

	#endregion

	#region Constructor 

	protected internal dfAnimatedValue( T StartValue, T EndValue, float Time )
		: this()
	{
		this.startValue = StartValue;
		this.endValue = EndValue;
		this.animLength = Time;
	}

	protected internal dfAnimatedValue()
	{
		this.startTime = Time.realtimeSinceStartup;
		this.easingFunction = dfEasingFunctions.GetFunction( this.easingType );
	}

	#endregion

	#region Public properties

	/// <summary>
	/// Returns a value indicating whether the animation has completed
	/// </summary>
	public bool IsDone
	{
		get { return isDone; }
	}

	/// <summary>
	/// The length of time (in seconds) the animation should take
	/// </summary>
	public float Length
	{
		get { return this.animLength; }
		set
		{
			this.animLength = value;
			startTime = Time.realtimeSinceStartup;
			isDone = false;
		}
	}

	/// <summary>
	/// The starting value
	/// </summary>
	public T StartValue
	{
		get { return this.startValue; }
		set
		{
			this.startValue = value;
			startTime = Time.realtimeSinceStartup;
			isDone = false;
		}
	}

	/// <summary>
	/// The ending value
	/// </summary>
	public T EndValue
	{
		get { return this.endValue; }
		set
		{
			this.endValue = value;
			startTime = Time.realtimeSinceStartup;
			isDone = false;
		}
	}

	/// <summary>
	/// Retrieves the current value, which will be the automatically
	/// interpolated value between <see cref="StartValue"/> and <see cref="EndValue"/>
	/// according to the time when this property is queried after starting 
	/// the animation
	/// </summary>
	public T Value
	{
		get
		{

			// Determine how much time has elapsed
			var elapsed = Time.realtimeSinceStartup - startTime;
			if( elapsed >= animLength )
			{
				isDone = true;
				return endValue;
			}

			// Normalize time elapsed to 0..1 range
			var time = Mathf.Clamp01( elapsed / animLength );
			
			// Allow easing function to modify time
			time = easingFunction( 0, 1, time );

			// Return interpolated  value
			return Lerp( startValue, endValue, time );

		}
	}

	/// <summary>
	/// The type of <a href="http://easings.net/" target="_blank">easing function</a> that
	/// will be applied to the value over time
	/// </summary>
	public dfEasingType EasingType
	{
		get { return this.easingType; }
		set
		{
			this.easingType = value;
			this.easingFunction = dfEasingFunctions.GetFunction( this.easingType );
		}
	}

	#endregion

	#region Abstract functions 

	/// <summary>
	/// Performs a linear interpolation of the base value.
	/// </summary>
	/// <param name="start">The start value</param>
	/// <param name="end">The end value</param>
	/// <param name="time">The time (between 0 and 1) to interpolate</param>
	/// <returns></returns>
	protected abstract T Lerp( T start, T end, float time );

	#endregion

	#region Type conversion 

	/// <summary>
	/// Converts the animated value to the base value type
	/// </summary>
	/// <param name="animated">The animated value to convert</param>
	/// <returns>Returns the current value as the base value type</returns>
	public static implicit operator T( dfAnimatedValue<T> animated )
	{
		return animated.Value;
	}

	#endregion

}

