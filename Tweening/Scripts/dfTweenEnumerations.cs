using System;

/// <summary>
/// Defines the method signature used for TweenPlayableBase notifications
/// </summary>
[dfEventCategory( "Animation Events" )]
public delegate void TweenNotification( dfTweenPlayableBase sender );

/// <summary>
/// Specifies action a Tween component should take when the tween animation
/// has completed
/// </summary>
/// @class dfTweenLoopType
public enum dfTweenLoopType
{
	/// <summary>
	/// Executes once
	/// </summary>
	Once,
	/// <summary>
	/// Executes in a repeating loop, restarting at the beginning each time
	/// </summary>
	Loop,
	/// <summary>
	/// Executes in a repeating loop, switching direction each time (forward, backward, forward, etc)
	/// </summary>
	PingPong,
}

