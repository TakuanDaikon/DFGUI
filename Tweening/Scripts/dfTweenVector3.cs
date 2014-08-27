/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Animates between two different Vector3 values
/// </summary>
[AddComponentMenu( "Daikon Forge/Tweens/Vector3" )]
public class dfTweenVector3 : dfTweenComponent<Vector3>
{

	// @cond DOXY_IGNORE 

	public override Vector3 offset( Vector3 lhs, Vector3 rhs )
	{
		return lhs + rhs;
	}

	public override Vector3 evaluate( Vector3 startValue, Vector3 endValue, float time )
	{
		return new Vector3(
			Lerp( startValue.x, endValue.x, time ),
			Lerp( startValue.y, endValue.y, time ),
			Lerp( startValue.z, endValue.z, time ) );
	}

	// @endcond DOXY_IGNORE

}

