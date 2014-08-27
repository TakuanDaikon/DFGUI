using System;
using System.Reflection;
using System.Collections;

using UnityEngine;

/// <summary>
/// Allows the animation of any component which renders sprites,
/// such as dfSprite, dfTextureSprite, dfButton, dfPanel, etc.
/// </summary>
[Serializable]
[AddComponentMenu( "Daikon Forge/Tweens/Sprite Animator" )]
public class dfSpriteAnimation : dfTweenPlayableBase
{

	#region Events

#pragma warning disable 0067

	/// <summary>
	/// Raised when the tween animation has started playing 
	/// </summary>
	public event TweenNotification AnimationStarted;

	/// <summary>
	/// Raised when the tween animation has stopped playing before completion
	/// </summary>
	public event TweenNotification AnimationStopped;

	/// <summary>
	/// Raised when the tween animation has been paused
	/// </summary>
	public event TweenNotification AnimationPaused;

	/// <summary>
	/// Raised when the tween animation has been resumed after having been paused
	/// </summary>
	public event TweenNotification AnimationResumed;

	/// <summary>
	/// Raised when the tween animation has been reset
	/// </summary>
	public event TweenNotification AnimationReset;

	/// <summary>
	/// Raised when the tween animation has successfully completed
	/// </summary>
	public event TweenNotification AnimationCompleted;

#pragma warning restore 0067

	#endregion

	#region Private serialized fields

	[SerializeField]
	private string animationName = "ANIMATION";

	[SerializeField]
	private dfAnimationClip clip;

	[SerializeField]
	private dfComponentMemberInfo memberInfo = new dfComponentMemberInfo();

	[SerializeField]
	private dfTweenLoopType loopType = dfTweenLoopType.Loop;

	[SerializeField]
	private float length = 1f;

	[SerializeField]
	private bool autoStart = false;

	[SerializeField]
	private bool skipToEndOnStop = false;

	[SerializeField]
	private dfPlayDirection playDirection = dfPlayDirection.Forward;

	#endregion

	#region Private runtime variables

	private bool autoRunStarted = false;
	private bool isRunning = false;
	private bool isPaused = false;
	private dfObservableProperty target = null;

	#endregion

	#region Public properties

	public dfAnimationClip Clip
	{
		get { return this.clip; }
		set
		{
			this.clip = value;
		}
	}

	public dfComponentMemberInfo Target
	{
		get { return this.memberInfo; }
		set
		{
			this.memberInfo = value;
		}
	}

	public bool AutoRun
	{
		get { return this.autoStart; }
		set { this.autoStart = value; }
	}

	public float Length
	{
		get { return this.length; }
		set { this.length = Mathf.Max( value, 0.03f ); }
	}

	public dfTweenLoopType LoopType
	{
		get { return this.loopType; }
		set { this.loopType = value; }
	}

	public dfPlayDirection Direction
	{
		get { return this.playDirection; }
		set { this.playDirection = value; if( this.IsPlaying ) this.Play(); }
	}

	public bool IsPaused
	{
		get { return this.isRunning && this.isPaused; }
		set
		{
			if( value != this.IsPaused )
			{
				if( value )
					Pause();
				else
					Resume();
			}
		}
	}

	#endregion

	#region Unity events

	public void Awake() { }
	public void Start() { }

	public void LateUpdate()
	{

		if( this.AutoRun && !this.IsPlaying && !this.autoRunStarted )
		{
			this.autoRunStarted = true;
			this.Play();
		}

	}

	#endregion

	#region Public methods

	/// <summary>
	/// Event-bindable wrapper around Direction and Play members to 
	/// start playing the animation in the forward direction
	/// </summary>
	public void PlayForward()
	{
		this.playDirection = dfPlayDirection.Forward;
		this.Play();
	}

	/// <summary>
	/// Event-bindable wrapper around Direction and Play members to 
	/// start playing the animation in the reverse direction
	/// </summary>
	public void PlayReverse()
	{
		this.playDirection = dfPlayDirection.Reverse;
		this.Play();
	}

	/// <summary>
	/// Pauses the running animation
	/// </summary>
	public void Pause()
	{
		if( this.isRunning )
		{
			this.isPaused = true;
			onPaused();
		}
	}

	/// <summary>
	/// Resumes a paused animation
	/// </summary>
	public void Resume()
	{
		if( this.isRunning && this.isPaused )
		{
			this.isPaused = false;
			onResumed();
		}
	}

	#endregion

	#region dfTweenPlayableBase implementation

	public override bool IsPlaying
	{
		get { return this.isRunning; }
	}

	public override void Play()
	{

		if( this.IsPlaying )
		{
			this.Stop();
		}

		if( !enabled || !gameObject.activeSelf || !gameObject.activeInHierarchy )
			return;

		if( this.memberInfo == null )
			throw new NullReferenceException( "Animation target is NULL" );

		if( !this.memberInfo.IsValid )
			throw new InvalidOperationException( "Invalid property binding configuration on " + getPath( gameObject.transform ) + " - " + target );

		this.target = this.memberInfo.GetProperty();
		StartCoroutine( Execute() );

	}

	public override void Reset()
	{

		var sprites = ( clip != null ) ? clip.Sprites : null;
		if( memberInfo.IsValid && sprites != null && sprites.Count > 0 )
		{
			SetProperty( memberInfo.Component, memberInfo.MemberName, sprites[ 0 ] );
		}

		if( !isRunning )
			return;

		StopAllCoroutines();
		isRunning = false;
		isPaused = false;

		onReset();

		this.target = null;

	}

	public override void Stop()
	{

		if( !isRunning )
			return;

		var sprites = ( clip != null ) ? clip.Sprites : null;
		if( skipToEndOnStop && sprites != null )
		{
			setFrame( Mathf.Max( sprites.Count - 1, 0 ) );
		}

		StopAllCoroutines();
		isRunning = false;
		isPaused = false;

		onStopped();

		this.target = null;

	}

	public override string TweenName
	{
		get { return this.animationName; }
		set { this.animationName = value; }
	}

	#endregion

	#region Event signalers

	protected void onPaused()
	{
		SendMessage( "AnimationPaused", this, SendMessageOptions.DontRequireReceiver );
		if( AnimationPaused != null ) AnimationPaused( this );
	}

	protected void onResumed()
	{
		SendMessage( "AnimationResumed", this, SendMessageOptions.DontRequireReceiver );
		if( AnimationResumed != null ) AnimationResumed( this );
	}

	protected void onStarted()
	{
		SendMessage( "AnimationStarted", this, SendMessageOptions.DontRequireReceiver );
		if( AnimationStarted != null ) AnimationStarted( this );
	}

	protected void onStopped()
	{
		SendMessage( "AnimationStopped", this, SendMessageOptions.DontRequireReceiver );
		if( AnimationStopped != null ) AnimationStopped( this );
	}

	protected void onReset()
	{
		SendMessage( "AnimationReset", this, SendMessageOptions.DontRequireReceiver );
		if( AnimationReset != null ) AnimationReset( this );
	}

	protected void onCompleted()
	{
		SendMessage( "AnimationCompleted", this, SendMessageOptions.DontRequireReceiver );
		if( AnimationCompleted != null ) AnimationCompleted( this );
	}

	#endregion

	#region Private utility methods

	internal static void SetProperty( object target, string property, object value )
	{

		if( target == null )
			throw new NullReferenceException( "Target is null" );

		// NOTE: There is a bug in Unity 4.3.3+ on Windows Phone that causes all reflection 
		// method overloads that take a BindingFlags parameter to throw a runtime exception.
		// This means that we cannot have 100% compatibility between Unity 4.3.3 and prior
		// versions on the Windows Phone platform, and that some functionality 
		// will unfortunately be lost.

#if UNITY_EDITOR || !UNITY_WP8
		var members = target.GetType().GetMember( property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
#else
		var members = target.GetType().GetMember( property );
#endif
		if( members == null || members.Length == 0 )
			throw new IndexOutOfRangeException( "Property not found: " + property );

		var member = members[ 0 ];

		if( member is FieldInfo )
		{
			( (FieldInfo)member ).SetValue( target, value );
			return;
		}

		if( member is PropertyInfo )
		{
			( (PropertyInfo)member ).SetValue( target, value, null );
			return;
		}

		throw new InvalidOperationException( "Member type not supported: " + member.GetMemberType() );

	}

	private IEnumerator Execute()
	{

		if( clip == null || clip.Sprites == null || clip.Sprites.Count == 0 )
			yield break;

		this.isRunning = true;
		this.isPaused = false;

		onStarted();

		var startTime = Time.realtimeSinceStartup;
		var direction = ( this.playDirection == dfPlayDirection.Forward ) ? 1 : -1;
		var lastFrameIndex = ( direction == 1 ) ? 0 : clip.Sprites.Count - 1;

		setFrame( lastFrameIndex );

		while( true )
		{

			yield return null;

			// Do nothing if the animation is paused
			if( IsPaused )
				continue;

			// Rereference these values each frame in case base AnimationClip
			// has changed (should probably on happen at design time in editor)
			var sprites = clip.Sprites;
			var maxFrameIndex = sprites.Count - 1;

			// Calculate the amount of time that has passed since the animation
			// started, looped, or reversed
			var timeNow = Time.realtimeSinceStartup;
			var elapsed = timeNow - startTime;

			// Determine the index of the current animation frame
			var frameIndex = Mathf.RoundToInt( Mathf.Clamp01( elapsed / this.length ) * maxFrameIndex );

			// Determine what to do if the animation has reached the 
			// last frame.
			if( elapsed >= this.length )
			{

				switch( this.loopType )
				{
					case dfTweenLoopType.Once:
						isRunning = false;
						onCompleted();
						yield break;
					case dfTweenLoopType.Loop:
						startTime = timeNow;
						frameIndex = 0;
						break;
					case dfTweenLoopType.PingPong:
						startTime = timeNow;
						direction *= -1;
						frameIndex = 0;
						break;
				}

			}

			if( direction == -1 )
			{
				frameIndex = maxFrameIndex - frameIndex;
			}

			// Set the current animation frame on the sprite
			if( lastFrameIndex != frameIndex )
			{
				lastFrameIndex = frameIndex;
				setFrame( frameIndex );
			}

		}

	}

	private string getPath( Transform obj )
	{

		var path = new System.Text.StringBuilder();

		while( obj != null )
		{
			if( path.Length > 0 )
			{
				path.Insert( 0, "\\" );
				path.Insert( 0, obj.name );
			}
			else
			{
				path.Append( obj.name );
			}
			obj = obj.parent;
		}

		return path.ToString();

	}

	private void setFrame( int frameIndex )
	{

		var sprites = clip.Sprites;
		if( sprites.Count == 0 )
			return;

		// Clamp the frame index
		frameIndex = Mathf.Max( 0, Mathf.Min( frameIndex, sprites.Count - 1 ) );

		if( this.target != null )
		{
			// Sprites and other DFGUI controls will re-render themselves when
			// the property associated with their background image is changed.
			this.target.Value = sprites[ frameIndex ];
		}

	}

	#endregion

}
