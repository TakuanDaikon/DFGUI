using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Enables a collection of <see cref="dfTweenPlayableBase"/>
/// objects to be treated as a single tween animation.
/// </summary>
[Serializable]
[AddComponentMenu( "Daikon Forge/Tweens/Group" )]
public class dfTweenGroup : dfTweenPlayableBase
{

	#region Enumerations 

	/// <summary>
	/// Indicates whether the objects in the collection should be 
	/// started concurrently or in sequence
	/// </summary>
	public enum TweenGroupMode
	{
		/// <summary>
		/// All tween animations should be started at the same time
		/// </summary>
		Concurrent,
		/// <summary>
		/// Each tween animation should only be started when the previous
		/// item in the collection (if any) has completed
		/// </summary>
		Sequence
	}

	#endregion

	#region Events 

	#pragma warning disable 0067

	/// <summary>
	/// Raised when the tween animation has started playing 
	/// </summary>
	public event TweenNotification TweenStarted;

	/// <summary>
	/// Raised when the tween animation has stopped playing before completion
	/// </summary>
	public event TweenNotification TweenStopped;

	/// <summary>
	/// Raised when the tween animation has been reset
	/// </summary>
	public event TweenNotification TweenReset;

	/// <summary>
	/// Raised when the tween animation has successfully completed
	/// </summary>
	public event TweenNotification TweenCompleted;

	#pragma warning restore 0067

	#endregion

	#region Serialized protected members

	[SerializeField]
	protected string groupName = "";

	[SerializeField]
	protected bool autoStart = false;

	[SerializeField]
	protected float delayBeforeStarting = 0f;

	#endregion

	#region Public properties 

	/// <summary>
	/// The amount of time in seconds after Play() is called before the tween will start animating
	/// </summary>
	public float StartDelay
	{
		get { return this.delayBeforeStarting; }
		set { this.delayBeforeStarting = value; }
	}

	/// <summary>
	/// The collection of tween components
	/// </summary>
	public List<dfTweenPlayableBase> Tweens = new List<dfTweenPlayableBase>();

	/// <summary>
	/// Specifies whether the collection of tween components should be 
	/// started concurrently or in sequence
	/// </summary>
	public TweenGroupMode Mode;

	/// <summary>
	/// Gets or sets whether the Tween Group will automatically run at startup
	/// </summary>
	public bool AutoStart
	{
		get { return this.autoStart; }
		set { this.autoStart = value; }
	}

	/// <summary>
	/// Gets or sets the user-defined name of the Tween, which is 
	/// useful to the developer at design time when there are 
	/// multiple tweens on a single GameObject
	/// </summary>
	public override string TweenName
	{
		get { return this.groupName; }
		set { this.groupName = value; }
	}

	/// <summary>
	/// Returns TRUE if the any Tween in the collection is currently playing
	/// </summary>
	public override bool IsPlaying
	{
		get
		{
			for( int i = 0; i < Tweens.Count; i++ )
			{
				if( Tweens[ i ] == null || !Tweens[ i ].enabled ) continue;
				if( Tweens[ i ].IsPlaying )
					return true;
			}
			return false;
		}
	}

	#endregion

	#region Unity events 

	public void Start() 
	{
		if( AutoStart && !IsPlaying )
		{
			Play();
		}
	}

	#endregion

	#region Public methods

	/// <summary>
	/// Enable a tween by name
	/// </summary>
	/// <param name="TweenName">The name of the tween component to enable</param>
	public void EnableTween( string TweenName )
	{
		for( int i = 0; i < Tweens.Count; i++ )
		{
			if( Tweens[ i ] == null ) continue;
			if( Tweens[ i ].TweenName == TweenName )
			{
				Tweens[ i ].enabled = true;
				break;
			}
		}
	}

	/// <summary>
	/// Disable a tween by name
	/// </summary>
	/// <param name="TweenName">The name of the tween component to disable</param>
	public void DisableTween( string TweenName )
	{
		for( int i = 0; i < Tweens.Count; i++ )
		{
			if( Tweens[ i ] == null ) continue;
			if( Tweens[ i ].name == TweenName )
			{
				Tweens[ i ].enabled = false;
				break;
			}
		}
	}

	/// <summary>
	/// Begin playing the the tweens in the collection
	/// </summary>
	public override void Play()
	{

		if( IsPlaying )
		{
			Stop();
		}

		onStarted();

		if( Mode == TweenGroupMode.Concurrent )
		{
			StartCoroutine( runConcurrent() );
		}
		else
		{
			StartCoroutine( runSequence() );
		}

	}

	/// <summary>
	/// Stop playing all tweens in the collection
	/// </summary>
	public override void Stop()
	{

		if( !IsPlaying )
			return;

		StopAllCoroutines();
		for( int i = 0; i < Tweens.Count; i++ )
		{
			if( Tweens[ i ] == null ) continue;
			Tweens[ i ].Stop();
		}

		onStopped();

	}

	/// <summary>
	/// Reset all tweens in the collection back to the start
	/// </summary>
	public override void Reset()
	{

		if( !IsPlaying )
			return;

		StopAllCoroutines();
		for( int i = 0; i < Tweens.Count; i++ )
		{
			if( Tweens[ i ] == null ) continue;
			Tweens[ i ].Reset();
		}

		onReset();

	}

	#endregion

	#region Private utility methods

	[HideInInspector]
	private IEnumerator runSequence()
	{

		if( delayBeforeStarting > 0f )
		{
			var timeout = Time.realtimeSinceStartup + delayBeforeStarting;
			while( Time.realtimeSinceStartup < timeout )
			{
				yield return null;
			}
		}

		for( var i = 0; i < Tweens.Count; i += 1 )
		{

			if( Tweens[ i ] == null || !Tweens[ i ].enabled ) continue;

			var tween = Tweens[ i ];
			tween.Play();

			while( tween.IsPlaying )
				yield return null;

		}

		onCompleted();

	}

	[HideInInspector]
	private IEnumerator runConcurrent()
	{

		if( delayBeforeStarting > 0f )
		{
			var timeout = Time.realtimeSinceStartup + delayBeforeStarting;
			while( Time.realtimeSinceStartup < timeout )
			{
				yield return null;
			}
		}

		for( int i = 0; i < Tweens.Count; i++ )
		{
			if( Tweens[ i ] == null || !Tweens[ i ].enabled ) continue;
			Tweens[ i ].Play();
		}

		do
		{
			yield return null;
		} while( Tweens.Any( tween => tween != null && tween.IsPlaying ) );

		onCompleted();

	}

	#endregion

	#region Event signalers

	protected internal void onStarted()
	{
		SendMessage( "TweenStarted", this, SendMessageOptions.DontRequireReceiver );
		if( TweenStarted != null ) TweenStarted( this );
	}

	protected internal void onStopped()
	{
		SendMessage( "TweenStopped", this, SendMessageOptions.DontRequireReceiver );
		if( TweenStopped != null ) TweenStopped( this );
	}

	protected internal void onReset()
	{
		SendMessage( "TweenReset", this, SendMessageOptions.DontRequireReceiver );
		if( TweenReset != null ) TweenReset( this );
	}

	protected internal void onCompleted()
	{
		SendMessage( "TweenCompleted", this, SendMessageOptions.DontRequireReceiver );
		if( TweenCompleted != null ) TweenCompleted( this );
	}

	#endregion

}
