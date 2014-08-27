/* Copyright 2013-2014 Daikon Forge */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

/// <summary>
/// Defines the core API for tweening components
/// </summary>
public abstract class dfTweenPlayableBase : MonoBehaviour
{

	/// <summary>
	/// Gets or sets the user-defined name of the Tween, which is 
	/// useful to the developer at design time when there are 
	/// multiple tweens on a single GameObject
	/// </summary>
	public abstract string TweenName { get; set; }

	/// <summary>
	/// Returns TRUE if the tween animation is currently playing 
	/// </summary>
	public abstract bool IsPlaying { get; }

	/// <summary>
	/// Starts the tween animation
	/// </summary>
	public abstract void Play();

	/// <summary>
	/// Stops the tween animation
	/// </summary>
	public abstract void Stop();

	/// <summary>
	/// Resets the tween animation to the beginning
	/// </summary>
	public abstract void Reset();

	#region Public methods 

	/// <summary>
	/// Enables the tween animation 
	/// </summary>
	public void Enable()
	{
		this.enabled = true;
	}

	/// <summary>
	/// Disables the tween animation 
	/// </summary>
	public void Disable()
	{
		this.enabled = false;
	}

	#endregion

	#region System.Object overrides 

	/// <summary>
	/// Returns a formatted string summarizing this object's state
	/// </summary>
	public override string ToString()
	{
		return this.TweenName + " - " + base.ToString();
	}

	#endregion

}

/// <summary>
/// Indicates the current state of a Tween
/// </summary>
public enum dfTweenState
{
	/// <summary>The tween is not currently running</summary>
	Stopped,
	/// <summary>The tween is currently paused</summary>
	Paused,
	/// <summary>The tween is currently running</summary>
	Playing,
	/// <summary>The tween was started, but is waiting for the StartDelay to expire</summary>
	Started
}

