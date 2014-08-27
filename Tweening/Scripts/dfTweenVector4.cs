/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Animates between two different Vector4 values
/// </summary>
[AddComponentMenu( "Daikon Forge/Tweens/Vector4" )]
public class dfTweenVector4 : dfTweenComponent<Vector4>
{

	// @cond DOXY_IGNORE 

	public override Vector4 offset( Vector4 lhs, Vector4 rhs )
	{
		return lhs + rhs;
	}

	public override Vector4 evaluate( Vector4 startValue, Vector4 endValue, float time )
	{
		return new Vector4(
			Lerp( startValue.x, endValue.x, time ),
			Lerp( startValue.y, endValue.y, time ),
			Lerp( startValue.z, endValue.z, time ),
			Lerp( startValue.w, endValue.w, time ) );
	}

	// @endcond DOXY_IGNORE

}

