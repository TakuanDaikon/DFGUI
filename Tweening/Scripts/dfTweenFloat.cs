/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Animates between two different float values
/// </summary>
[AddComponentMenu( "Daikon Forge/Tweens/Float" )]
public class dfTweenFloat : dfTweenComponent<float>
{

	// @cond DOXY_IGNORE 

	public override float offset( float lhs, float rhs )
	{
		return lhs + rhs;
	}

	public override float evaluate( float startValue, float endValue, float time )
	{
		var result = startValue + ( endValue - startValue ) * time;
		return result;
	}

	// @endcond DOXY_IGNORE

}
