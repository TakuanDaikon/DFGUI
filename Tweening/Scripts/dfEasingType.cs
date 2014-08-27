using System;

/// <summary>
/// The type of <a href="http://easings.net/" target="_blank">easing function</a> to use for tweening.
/// </summary>
public enum dfEasingType
{

	/// <summary>
	/// Easing equation function for a simple linear tweening, with no easing.
	/// </summary>
	Linear,

	/// <summary>
	/// Easing equation function for a bounce (exponentially decaying parabolic bounce) easing out: 
	/// decelerating from zero velocity.
	/// </summary>
	Bounce,

	/// <summary>
	/// Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing in: 
	/// accelerating from zero velocity.
	/// </summary>
	BackEaseIn,
	/// <summary>
	/// Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing out: 
	/// decelerating from zero velocity.
	/// </summary>
	BackEaseOut,
	/// <summary>
	/// Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing in/out: 
	/// acceleration until halfway, then deceleration.
	/// </summary>
	BackEaseInOut,

	/// <summary>
	/// Easing equation function for a circular (sqrt(1-t^2)) easing in: 
	/// accelerating from zero velocity.
	/// </summary>
	CircEaseIn,
	/// <summary>
	/// Easing equation function for a circular (sqrt(1-t^2)) easing out: 
	/// decelerating from zero velocity.
	/// </summary>
	CircEaseOut,
	/// <summary>
	/// Easing equation function for a circular (sqrt(1-t^2)) easing in/out: 
	/// acceleration until halfway, then deceleration.
	/// </summary>
	CircEaseInOut,

	/// <summary>
	/// Easing equation function for a cubic (t^3) easing in: 
	/// accelerating from zero velocity.
	/// </summary>
	CubicEaseIn,
	/// <summary>
	/// Easing equation function for a cubic (t^3) easing out: 
	/// decelerating from zero velocity.
	/// </summary>
	CubicEaseOut,
	/// <summary>
	/// Easing equation function for a cubic (t^3) easing in/out: 
	/// acceleration until halfway, then deceleration.
	/// </summary>
	CubicEaseInOut,

	/// <summary>
	/// Easing equation function for an exponential (2^t) easing in: 
	/// accelerating from zero velocity.
	/// </summary>
	ExpoEaseIn,
	/// <summary>
	/// Easing equation function for an exponential (2^t) easing out: 
	/// decelerating from zero velocity.
	/// </summary>
	ExpoEaseOut,
	/// <summary>
	/// Easing equation function for an exponential (2^t) easing in/out: 
	/// acceleration until halfway, then deceleration.
	/// </summary>
	ExpoEaseInOut,

	/// <summary>
	/// Easing equation function for a quadratic (t^2) easing in: 
	/// accelerating from zero velocity.
	/// </summary>
	QuadEaseIn,
	/// <summary>
	/// Easing equation function for a quadratic (t^2) easing out: 
	/// decelerating from zero velocity.
	/// </summary>
	QuadEaseOut,
	/// <summary>
	/// Easing equation function for a quadratic (t^2) easing in/out: 
	/// acceleration until halfway, then deceleration.
	/// </summary>
	QuadEaseInOut,

	/// <summary>
	/// Easing equation function for a quartic (t^4) easing in: 
	/// accelerating from zero velocity.
	/// </summary>
	QuartEaseIn,
	/// <summary>
	/// Easing equation function for a quartic (t^4) easing out: 
	/// decelerating from zero velocity.
	/// </summary>
	QuartEaseOut,
	/// <summary>
	/// Easing equation function for a quartic (t^4) easing in/out: 
	/// acceleration until halfway, then deceleration.
	/// </summary>
	QuartEaseInOut,

	/// <summary>
	/// Easing equation function for a quintic (t^5) easing in: 
	/// accelerating from zero velocity.
	/// </summary>
	QuintEaseIn,
	/// <summary>
	/// Easing equation function for a quintic (t^5) easing out: 
	/// decelerating from zero velocity.
	/// </summary>
	QuintEaseOut,
	/// <summary>
	/// Easing equation function for a quintic (t^5) easing in/out: 
	/// acceleration until halfway, then deceleration.
	/// </summary>
	QuintEaseInOut,

	/// <summary>
	/// Easing equation function for a sinusoidal (sin(t)) easing in: 
	/// accelerating from zero velocity.
	/// </summary>
	SineEaseIn,
	/// <summary>
	/// Easing equation function for a sinusoidal (sin(t)) easing out: 
	/// decelerating from zero velocity.
	/// </summary>
	SineEaseOut,
	/// <summary>
	/// Easing equation function for a sinusoidal (sin(t)) easing in/out: 
	/// acceleration until halfway, then deceleration.
	/// </summary>
	SineEaseInOut,

	Spring

}

