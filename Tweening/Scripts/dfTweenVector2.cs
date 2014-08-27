/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Animates between two different Vector2 values
/// </summary>
[AddComponentMenu( "Daikon Forge/Tweens/Vector2" )]
public class dfTweenVector2 : dfTweenComponent<Vector2>
{

	// @cond DOXY_IGNORE 

	public override Vector2 offset( Vector2 lhs, Vector2 rhs )
	{
		return lhs + rhs;
	}

	public override Vector2 evaluate( Vector2 startValue, Vector2 endValue, float time )
	{
		return new Vector2(
			Lerp( startValue.x, endValue.x, time ),
			Lerp( startValue.y, endValue.y, time ) );
	}

	// @endcond DOXY_IGNORE

}

