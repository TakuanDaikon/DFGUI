/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Animates between two different Color32 values
/// </summary>
[AddComponentMenu( "Daikon Forge/Tweens/Color32" )]
public class dfTweenColor32 : dfTweenComponent<Color32>
{

	// @cond DOXY_IGNORE 

	public override Color32 offset( Color32 lhs, Color32 rhs )
	{
		return (Color)lhs + (Color)rhs;
	}

	public override Color32 evaluate( Color32 startValue, Color32 endValue, float time )
	{

		var lhs = (Vector4)(Color)startValue;
		var rhs = (Vector4)(Color)endValue;

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

