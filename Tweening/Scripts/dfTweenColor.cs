/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Animates between two different Color values
/// </summary>
[AddComponentMenu( "Daikon Forge/Tweens/Color" )]
public class dfTweenColor : dfTweenComponent<Color>
{

	// @cond DOXY_IGNORE 

	public override Color offset( Color lhs, Color rhs )
	{
		return lhs + rhs;
	}

	public override Color evaluate( Color startValue, Color endValue, float time )
	{

		var lhs = (Vector4)startValue;
		var rhs = (Vector4)endValue;

		var colorAsVector = new Vector4(
			Lerp( lhs.x, rhs.x, time ),
			Lerp( lhs.y, rhs.y, time ),
			Lerp( lhs.z, rhs.z, time ),
			Lerp( lhs.w, rhs.w, time )
		);

		return (Color)colorAsVector;

	}

	// @endcond DOXY_IGNORE

}

