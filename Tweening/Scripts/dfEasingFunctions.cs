/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Defines common easing functions 
/// </summary>
public class dfEasingFunctions
{

	/// <summary>
	/// Defines the method signature implemented by easing functions
	/// </summary>
	/// <param name="start">The start value</param>
	/// <param name="end">The end value</param>
	/// <param name="time">The time factor to use for interpolation</param>
	public delegate float EasingFunction( float start, float end, float time );

	//
	// NOTES: Based on Robert Penner's open source easing equations (http://www.robertpenner.com/easing_terms_of_use.html).
	//
	// EASING EQUATIONS TERMS OF USE 
	// 
	// Open source under the BSD License. 
	// 
	// Copyright © 2001 Robert Penner
	// All rights reserved.
	// 
	// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
	// 
	// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
	// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. 
	// Neither the name of the author nor the names of contributors may be used to endorse or promote products derived from this software without specific prior written permission. 
	// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
	//

	/// <summary>
	/// Returns an <see cref="EasignFunction"/> delegate that implements the 
	/// easing equation defined by the <paramref name="easeType"/> argument.
	/// </summary>
	/// <param name="easeType">The easing equation to be used</param>
	public static EasingFunction GetFunction( dfEasingType easeType )
	{

		switch( easeType )
		{
			case dfEasingType.BackEaseIn: return easeInBack;
			case dfEasingType.BackEaseInOut: return easeInOutBack;
			case dfEasingType.BackEaseOut: return easeOutBack;
			case dfEasingType.Bounce: return bounce;
			case dfEasingType.CircEaseIn: return easeInCirc;
			case dfEasingType.CircEaseInOut: return easeInOutCirc;
			case dfEasingType.CircEaseOut: return easeOutCirc;
			case dfEasingType.CubicEaseIn: return easeInCubic;
			case dfEasingType.CubicEaseInOut: return easeInOutCubic;
			case dfEasingType.CubicEaseOut: return easeOutCubic;
			case dfEasingType.ExpoEaseIn: return easeInExpo;
			case dfEasingType.ExpoEaseInOut: return easeInOutExpo;
			case dfEasingType.ExpoEaseOut: return easeOutExpo;
			case dfEasingType.Linear: return linear;
			case dfEasingType.QuadEaseIn: return easeInQuad;
			case dfEasingType.QuadEaseInOut: return easeInOutQuad;
			case dfEasingType.QuadEaseOut: return easeOutQuad;
			case dfEasingType.QuartEaseIn: return easeInQuart;
			case dfEasingType.QuartEaseInOut: return easeInOutQuart;
			case dfEasingType.QuartEaseOut: return easeOutQuart;
			case dfEasingType.QuintEaseIn: return easeInQuint;
			case dfEasingType.QuintEaseInOut: return easeInOutQuint;
			case dfEasingType.QuintEaseOut: return easeOutQuint;
			case dfEasingType.SineEaseIn: return easeInSine;
			case dfEasingType.SineEaseInOut: return easeInOutSine;
			case dfEasingType.SineEaseOut: return easeOutSine;
			case dfEasingType.Spring: return spring;
		}

		throw new NotImplementedException();

	}

	#region Easing functions

	private static float linear( float start, float end, float time )
	{
		return Mathf.Lerp( start, end, time );
	}

	private static float clerp( float start, float end, float time )
	{
		float min = 0.0f;
		float max = 360.0f;
		float half = Mathf.Abs( ( max - min ) / 2.0f );
		float retval = 0.0f;
		float diff = 0.0f;
		if( ( end - start ) < -half )
		{
			diff = ( ( max - start ) + end ) * time;
			retval = start + diff;
		}
		else if( ( end - start ) > half )
		{
			diff = -( ( max - end ) + start ) * time;
			retval = start + diff;
		}
		else retval = start + ( end - start ) * time;
		return retval;
	}

	private static float spring( float start, float end, float time )
	{
		time = Mathf.Clamp01( time );
		time = ( Mathf.Sin( time * Mathf.PI * ( 0.2f + 2.5f * time * time * time ) ) * Mathf.Pow( 1f - time, 2.2f ) + time ) * ( 1f + ( 1.2f * ( 1f - time ) ) );
		return start + ( end - start ) * time;
	}

	private static float easeInQuad( float start, float end, float time )
	{
		end -= start;
		return end * time * time + start;
	}

	private static float easeOutQuad( float start, float end, float time )
	{
		end -= start;
		return -end * time * ( time - 2 ) + start;
	}

	private static float easeInOutQuad( float start, float end, float time )
	{
		time /= .5f;
		end -= start;
		if( time < 1 ) return end / 2 * time * time + start;
		time--;
		return -end / 2 * ( time * ( time - 2 ) - 1 ) + start;
	}

	private static float easeInCubic( float start, float end, float time )
	{
		end -= start;
		return end * time * time * time + start;
	}

	private static float easeOutCubic( float start, float end, float time )
	{
		time--;
		end -= start;
		return end * ( time * time * time + 1 ) + start;
	}

	private static float easeInOutCubic( float start, float end, float time )
	{
		time /= .5f;
		end -= start;
		if( time < 1 ) return end / 2 * time * time * time + start;
		time -= 2;
		return end / 2 * ( time * time * time + 2 ) + start;
	}

	private static float easeInQuart( float start, float end, float time )
	{
		end -= start;
		return end * time * time * time * time + start;
	}

	private static float easeOutQuart( float start, float end, float time )
	{
		time--;
		end -= start;
		return -end * ( time * time * time * time - 1 ) + start;
	}

	private static float easeInOutQuart( float start, float end, float time )
	{
		time /= .5f;
		end -= start;
		if( time < 1 ) return end / 2 * time * time * time * time + start;
		time -= 2;
		return -end / 2 * ( time * time * time * time - 2 ) + start;
	}

	private static float easeInQuint( float start, float end, float time )
	{
		end -= start;
		return end * time * time * time * time * time + start;
	}

	private static float easeOutQuint( float start, float end, float time )
	{
		time--;
		end -= start;
		return end * ( time * time * time * time * time + 1 ) + start;
	}

	private static float easeInOutQuint( float start, float end, float time )
	{
		time /= .5f;
		end -= start;
		if( time < 1 ) return end / 2 * time * time * time * time * time + start;
		time -= 2;
		return end / 2 * ( time * time * time * time * time + 2 ) + start;
	}

	private static float easeInSine( float start, float end, float time )
	{
		end -= start;
		return -end * Mathf.Cos( time / 1 * ( Mathf.PI / 2 ) ) + end + start;
	}

	private static float easeOutSine( float start, float end, float time )
	{
		end -= start;
		return end * Mathf.Sin( time / 1 * ( Mathf.PI / 2 ) ) + start;
	}

	private static float easeInOutSine( float start, float end, float time )
	{
		end -= start;
		return -end / 2 * ( Mathf.Cos( Mathf.PI * time / 1 ) - 1 ) + start;
	}

	private static float easeInExpo( float start, float end, float time )
	{
		end -= start;
		return end * Mathf.Pow( 2, 10 * ( time / 1 - 1 ) ) + start;
	}

	private static float easeOutExpo( float start, float end, float time )
	{
		end -= start;
		return end * ( -Mathf.Pow( 2, -10 * time / 1 ) + 1 ) + start;
	}

	private static float easeInOutExpo( float start, float end, float time )
	{
		time /= .5f;
		end -= start;
		if( time < 1 ) return end / 2 * Mathf.Pow( 2, 10 * ( time - 1 ) ) + start;
		time--;
		return end / 2 * ( -Mathf.Pow( 2, -10 * time ) + 2 ) + start;
	}

	private static float easeInCirc( float start, float end, float time )
	{
		end -= start;
		return -end * ( Mathf.Sqrt( 1 - time * time ) - 1 ) + start;
	}

	private static float easeOutCirc( float start, float end, float time )
	{
		time--;
		end -= start;
		return end * Mathf.Sqrt( 1 - time * time ) + start;
	}

	private static float easeInOutCirc( float start, float end, float time )
	{
		time /= .5f;
		end -= start;
		if( time < 1 ) return -end / 2 * ( Mathf.Sqrt( 1 - time * time ) - 1 ) + start;
		time -= 2;
		return end / 2 * ( Mathf.Sqrt( 1 - time * time ) + 1 ) + start;
	}

	private static float bounce( float start, float end, float time )
	{
		time /= 1f;
		end -= start;
		if( time < ( 1 / 2.75f ) )
		{
			return end * ( 7.5625f * time * time ) + start;
		}
		else if( time < ( 2 / 2.75f ) )
		{
			time -= ( 1.5f / 2.75f );
			return end * ( 7.5625f * ( time ) * time + .75f ) + start;
		}
		else if( time < ( 2.5 / 2.75 ) )
		{
			time -= ( 2.25f / 2.75f );
			return end * ( 7.5625f * ( time ) * time + .9375f ) + start;
		}
		else
		{
			time -= ( 2.625f / 2.75f );
			return end * ( 7.5625f * ( time ) * time + .984375f ) + start;
		}
	}

	private static float easeInBack( float start, float end, float time )
	{
		end -= start;
		time /= 1;
		float s = 1.70158f;
		return end * ( time ) * time * ( ( s + 1 ) * time - s ) + start;
	}

	private static float easeOutBack( float start, float end, float time )
	{
		float s = 1.70158f;
		end -= start;
		time = ( time / 1 ) - 1;
		return end * ( ( time ) * time * ( ( s + 1 ) * time + s ) + 1 ) + start;
	}

	private static float easeInOutBack( float start, float end, float time )
	{
		float s = 1.70158f;
		end -= start;
		time /= .5f;
		if( ( time ) < 1 )
		{
			s *= ( 1.525f );
			return end / 2 * ( time * time * ( ( ( s ) + 1 ) * time - s ) ) + start;
		}
		time -= 2;
		s *= ( 1.525f );
		return end / 2 * ( ( time ) * time * ( ( ( s ) + 1 ) * time + s ) + 2 ) + start;
	}

	private static float punch( float amplitude, float time )
	{
		float s = 9;
		if( time == 0 )
		{
			return 0;
		}
		if( time == 1 )
		{
			return 0;
		}
		float period = 1 * 0.3f;
		s = period / ( 2 * Mathf.PI ) * Mathf.Asin( 0 );
		return ( amplitude * Mathf.Pow( 2, -10 * time ) * Mathf.Sin( ( time * 1 - s ) * ( 2 * Mathf.PI ) / period ) );
	}

	#endregion

}
