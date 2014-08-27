/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Animates between two different Quaternion values
/// </summary>
[AddComponentMenu( "Daikon Forge/Tweens/Rotation" )]
public class dfTweenRotation : dfTweenComponent<Quaternion>
{

	// @cond DOXY_IGNORE 

	public override Quaternion offset( Quaternion lhs, Quaternion rhs )
	{
		return lhs * rhs;
	}

	public override Quaternion evaluate( Quaternion startValue, Quaternion endValue, float time )
	{
		Vector3 euler1 = startValue.eulerAngles;
		Vector3 euler2 = endValue.eulerAngles;
		return Quaternion.Euler( LerpEuler( euler1, euler2, time ) );
	}

	private static Vector3 LerpEuler( Vector3 startValue, Vector3 endValue, float time )
	{
		return new Vector3(
			LerpAngle( startValue.x, endValue.x, time ),
			LerpAngle( startValue.y, endValue.y, time ),
			LerpAngle( startValue.z, endValue.z, time ) );
	}

	private static float LerpAngle( float startValue, float endValue, float time )
	{
		float num = Mathf.Repeat( endValue - startValue, 360f );
		if( num > 180f )
		{
			num -= 360f;
		}
		return startValue + num * time;
	}

	// @endcond DOXY_IGNORE

}

