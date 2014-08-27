/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Abstract class that defines the core API of a tweening component
/// </summary>
[Serializable]
public abstract class dfTweenComponentBase : dfTweenPlayableBase
{

	#region Protected serialized members

	[SerializeField]
	protected string tweenName = "";

	[SerializeField]
	protected dfComponentMemberInfo target;

	[SerializeField]
	protected dfEasingType easingType = dfEasingType.Linear;

	[SerializeField]
	protected AnimationCurve animCurve = new AnimationCurve( new Keyframe( 0f, 0f, 0f, 1f ), new Keyframe( 1f, 1f, 1f, 0f ) );

	[SerializeField]
	protected float length = 1f;

	[SerializeField]
	protected bool syncStartWhenRun = false;

	[SerializeField]
	protected bool startValueIsOffset = false;

	[SerializeField]
	protected bool syncEndWhenRun = false;

	[SerializeField]
	protected bool endValueIsOffset = false;

	[SerializeField]
	protected dfTweenLoopType loopType = dfTweenLoopType.Once;

	[SerializeField]
	protected bool autoRun = false;

	[SerializeField]
	protected bool skipToEndOnStop = false;

	[SerializeField]
	protected float delayBeforeStarting = 0f;

	#endregion

	#region Private instance variables

	protected dfTweenState state = dfTweenState.Stopped;
	protected dfEasingFunctions.EasingFunction easingFunction;
	protected dfObservableProperty boundProperty;
	protected bool wasAutoStarted = false;

	#endregion

	#region Public properties

	/// <summary>
	/// Gets or sets the user-defined name of the Tween, which is 
	/// useful to the developer at design time when there are 
	/// multiple tweens on a single GameObject
	/// </summary>
	public override string TweenName
	{
		get
		{
			if( tweenName == null ) tweenName = base.ToString();
			return tweenName;
		}
		set { this.tweenName = value; }
	}

	/// <summary>
	/// Provides information about the target property being tweened
	/// </summary>
	public dfComponentMemberInfo Target
	{
		get { return this.target; }
		set
		{
			this.target = value;
		}
	}

	/// <summary>
	/// A user-editable AnimationCurve that will be applied to the Tween operation
	/// </summary>
	public AnimationCurve AnimationCurve
	{
		get { return this.animCurve; }
		set { this.animCurve = value; }
	}

	/// <summary>
	/// The amount of time in seconds for the tween to operate
	/// </summary>
	public float Length
	{
		get { return this.length; }
		set
		{
			this.length = Mathf.Max( 0f, value ); ;
		}
	}

	/// <summary>
	/// The amount of time in seconds after Play() is called before the tween will start animating
	/// </summary>
	public float StartDelay
	{
		get { return this.delayBeforeStarting; }
		set { this.delayBeforeStarting = value; }
	}

	/// <summary>
	/// The type of easing function that is to be used
	/// </summary>
	public dfEasingType Function
	{
		get { return this.easingType; }
		set
		{
			this.easingType = value;
			if( state != dfTweenState.Stopped )
			{
				Stop();
				Play();
			}
		}
	}

	/// <summary>
	/// Gets or sets a value that controls how the Tween operation is looped
	/// </summary>
	public dfTweenLoopType LoopType
	{
		get { return this.loopType; }
		set
		{
			this.loopType = value;
			if( state != dfTweenState.Stopped )
			{
				Stop();
				Play();
			}
		}
	}

	/// <summary>
	/// If set to TRUE, will synchronize the value of the <see cref="StartValue"/> 
	/// property when the tween is run
	/// </summary>
	public bool SyncStartValueWhenRun
	{
		get { return this.syncStartWhenRun; }
		set { this.syncStartWhenRun = value; }
	}

	/// <summary>
	/// If set to TRUE, the value of <see cref="StartValue"/> will be treated as 
	/// an offset rather than the absolute value
	/// </summary>
	public bool StartValueIsOffset
	{
		get { return this.startValueIsOffset; }
		set { this.startValueIsOffset = value; }
	}

	/// <summary>
	/// If set to TRUE, will synchronize the value of the <see cref="EndValue"/> 
	/// property when the tween is run
	/// </summary>
	public bool SyncEndValueWhenRun
	{
		get { return this.syncEndWhenRun; }
		set { this.syncEndWhenRun = value; }
	}

	/// <summary>
	/// If set to TRUE, the value of <see cref="EndValue"/> will be treated as 
	/// an offset rather than the absolute value
	/// </summary>
	public bool EndValueIsOffset
	{
		get { return this.endValueIsOffset; }
		set { this.endValueIsOffset = value; }
	}

	/// <summary>
	/// If set to TRUE, the tween will automatically run on startup
	/// </summary>
	public bool AutoRun
	{
		get { return this.autoRun; }
		set { this.autoRun = value; }
	}

	/// <summary>
	/// Returns TRUE if the Tween is currently playing
	/// </summary>
	public override bool IsPlaying
	{
		get { return this.enabled && state != dfTweenState.Stopped; }
	}

	/// <summary>
	/// Indicates whether the tween is paused
	/// </summary>
	public bool IsPaused
	{
		get { return state == dfTweenState.Paused; }
		set
		{
			var isPaused = ( this.state == dfTweenState.Paused );
			if( value != isPaused && state != dfTweenState.Stopped )
			{

				state = value ? dfTweenState.Paused : dfTweenState.Playing;

				if( value )
				{
					onPaused();
				}
				else
				{
					onResumed();
				}

			}
		}
	}

	#endregion

	#region Abstract methods 

	protected internal abstract void onPaused();
	protected internal abstract void onResumed();
	protected internal abstract void onStarted();
	protected internal abstract void onStopped();
	protected internal abstract void onReset();
	protected internal abstract void onCompleted();
	
	#endregion

	#region Unity Events

	public void Start()
	{

		if( autoRun && !wasAutoStarted )
		{
			wasAutoStarted = true;
			Play();
		}

	}

	public void OnDisable()
	{
		Stop();
		wasAutoStarted = false;
	}

	#endregion

	#region System.Object overrides 

	/// <summary>
	/// Returns a formatted string summarizing this object's state
	/// </summary>
	public override string ToString()
	{
		if( Target != null && Target.IsValid )
		{
			var targetType = target.Component.name;
			return string.Format( "{0} ({1}.{2})", TweenName, targetType, target.MemberName );
		}
		return this.TweenName;
	}

	#endregion

}
