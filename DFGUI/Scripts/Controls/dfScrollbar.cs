/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implements a common Scrollbar control
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Implements a common Scrollbar control" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_scrollbar.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Scrollbar" )]
public class dfScrollbar : dfControl
{

	#region Public events

	/// <summary>
	/// Raised when the value of the <see cref="Value"/> property has changed
	/// </summary>
	public event PropertyChangedEventHandler<float> ValueChanged;

	#endregion

	#region Protected serialized members

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected dfControlOrientation orientation = dfControlOrientation.Horizontal;

	[SerializeField]
	protected float rawValue = 1f;

	[SerializeField]
	protected float minValue = 0f;

	[SerializeField]
	protected float maxValue = 100f;

	[SerializeField]
	protected float stepSize = 1f;

	[SerializeField]
	protected float scrollSize = 1f;

	[SerializeField]
	protected float increment = 1f;

	[SerializeField]
	protected dfControl thumb = null;

	[SerializeField]
	protected dfControl track = null;

	[SerializeField]
	protected dfControl incButton = null;

	[SerializeField]
	protected dfControl decButton = null;

	[SerializeField]
	protected RectOffset thumbPadding = new RectOffset();

	[SerializeField]
	protected bool autoHide = false;

	#endregion

	#region Public properties

	/// <summary>
	/// The <see cref="dfAtlas">Texture Atlas</see> containing the images used by this control
	/// </summary>
	public dfAtlas Atlas
	{
		get
		{
			if( atlas == null )
			{
				var view = GetManager();
				if( view != null )
				{
					return atlas = view.DefaultAtlas;
				}
			}
			return this.atlas;
		}
		set
		{
			if( !dfAtlas.Equals( value, atlas ) )
			{
				this.atlas = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets/Sets the lower limit of the range of values this Scrollbar can return
	/// </summary>
	public float MinValue
	{
		get { return this.minValue; }
		set
		{
			if( value != this.minValue )
			{
				this.minValue = value;
				Value = Value; // Force update and validation
				Invalidate();
				doAutoHide();
			}
		}
	}

	/// <summary>
	/// Gets/Sets the upper limit of the range of values this Scrollbar can return
	/// </summary>
	public float MaxValue
	{
		get { return this.maxValue; }
		set
		{
			if( value != this.maxValue )
			{
				this.maxValue = value;
				Value = Value; // Force update and validation
				Invalidate();
				doAutoHide();
			}
		}
	}

	/// <summary>
	/// All values assigned to the Value property will be clamped to 
	/// a multiple of StepSize. For example, if StepSize is 0.25 then
	/// the Value property will always be a multiple of 0.25 such as
	/// 0.25, 0.5, 1.75, etc.
	/// </summary>
	public float StepSize
	{
		get { return this.stepSize; }
		set
		{
			value = Mathf.Max( 0, value );
			if( value != this.stepSize )
			{
				this.stepSize = value;
				Value = Value; // Force update and validation
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Represents viewable portion of the area being scrolled
	/// </summary>
	public float ScrollSize
	{
		get { return this.scrollSize; }
		set
		{
			value = Mathf.Max( 0, value );
			if( value != this.scrollSize )
			{
				this.scrollSize = value;
				Value = Value; // Force update and validation
				Invalidate();
				doAutoHide();
			}
		}
	}

	/// <summary>
	/// The amount added to or subtracted from the Value property when
	/// the user clicks the increment/decrement buttons or uses the
	/// mouse wheel
	/// </summary>
	public float IncrementAmount
	{
		get { return this.increment; }
		set
		{
			value = Mathf.Max( 0, value );
			if( !Mathf.Approximately( value, this.increment ) )
			{
				this.increment = value;
			}
		}
	}

	/// <summary>
	/// Gets or sets a value indicating the horizontal or 
	/// vertical orientation of the Scrollbar
	/// </summary>
	public dfControlOrientation Orientation
	{
		get { return this.orientation; }
		set
		{
			if( value != this.orientation )
			{
				this.orientation = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets a numeric value that represents the current position of 
	/// the scroll box on the track
	/// </summary>
	public float Value
	{
		get { return this.rawValue; }
		set
		{
			value = adjustValue( value );
			if( !Mathf.Approximately( value, rawValue ) )
			{
				rawValue = value;
				OnValueChanged();
			}
			updateThumb( rawValue );
			doAutoHide();
		}
	}

	/// <summary>
	/// Gets/Sets a reference to the dfControl used to display the Thumb button.
	/// This property should refer to a child control.
	/// </summary>
	public dfControl Thumb
	{
		get { return this.thumb; }
		set
		{
			if( value != this.thumb )
			{
				this.thumb = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets/Sets a reference to the dfControl that will be used to 
	/// properly position and size the Thumb icon.
	/// This property should refer to a child control.
	/// </summary>
	public dfControl Track
	{
		get { return this.track; }
		set
		{
			if( value != this.track )
			{
				this.track = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets/Sets a reference to the dfControl (if any) that can be clicked
	/// to increment the Value.
	/// This property should refer to a child control.
	/// </summary>
	public dfControl IncButton
	{
		get { return this.incButton; }
		set
		{
			if( value != this.incButton )
			{
				this.incButton = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets/Sets a reference to the dfControl (if any) that can be clicked
	/// to decrement the Value.
	/// This property should refer to a child control.
	/// </summary>
	public dfControl DecButton
	{
		get { return this.decButton; }
		set
		{
			if( value != this.decButton )
			{
				this.decButton = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The amount of padding to apply when positioning the Thumb control
	/// </summary>
	public RectOffset ThumbPadding
	{
		get
		{
			if( this.thumbPadding == null )
				this.thumbPadding = new RectOffset();
			return this.thumbPadding;
		}
		set
		{

			// Constrain value for control orientation 
			if( orientation == dfControlOrientation.Horizontal )
			{
				value.top = value.bottom = 0;
			}
			else
			{
				value.left = value.right = 0;
			}

			if( !RectOffset.Equals( value, this.thumbPadding ) )
			{
				this.thumbPadding = value;
				updateThumb( this.rawValue );
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the scrollbar will automatically hide when not needed
	/// </summary>
	public bool AutoHide
	{
		get { return this.autoHide; }
		set
		{
			if( value != this.autoHide )
			{
				this.autoHide = value;
				Invalidate();
				doAutoHide();
			}
		}
	}

	#endregion

	#region Private runtime variables

	private Vector3 thumbMouseOffset = Vector3.zero;

	#endregion

	#region Overrides

	public override Vector2 CalculateMinimumSize()
	{

		var min = new Vector2[ 3 ];

		if( decButton != null )
		{
			min[ 0 ] = decButton.CalculateMinimumSize();
		}

		if( incButton != null )
		{
			min[ 1 ] = incButton.CalculateMinimumSize();
		}

		if( thumb != null )
		{
			min[ 2 ] = thumb.CalculateMinimumSize();
		}

		var accum = Vector2.zero;
		if( orientation == dfControlOrientation.Horizontal )
		{
			accum.x = min[ 0 ].x + min[ 1 ].x + min[ 2 ].x;
			accum.y = Mathf.Max( min[ 0 ].y, min[ 1 ].y, min[ 2 ].y );
		}
		else
		{
			accum.x = Mathf.Max( min[ 0 ].x, min[ 1 ].x, min[ 2 ].x );
			accum.y = min[ 0 ].y + min[ 1 ].y + min[ 2 ].y;
		}

		return Vector2.Max( accum, base.CalculateMinimumSize() );

	}

	public override bool CanFocus
	{
		get
		{
			if( this.IsEnabled && this.IsVisible )
				return true;
			return base.CanFocus;
		}
	}

	protected override void OnRebuildRenderData()
	{

		// This control doesn't render anything by itself, but should 
		// make sure that child controls are properly updated before 
		// they are asked to render themselves.
		updateThumb( rawValue );

		base.OnRebuildRenderData();

	}

	public override void Start()
	{
		base.Start();
		attachEvents();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		detachEvents();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		detachEvents();
	}

	private void attachEvents()
	{

		if( !Application.isPlaying )
			return;

		if( IncButton != null )
		{
			IncButton.MouseDown += incrementPressed;
			IncButton.MouseHover += incrementPressed;
		}

		if( DecButton != null )
		{
			DecButton.MouseDown += decrementPressed;
			DecButton.MouseHover += decrementPressed;
		}

	}

	private void detachEvents()
	{

		if( !Application.isPlaying )
			return;

		if( IncButton != null )
		{
			IncButton.MouseDown -= incrementPressed;
			IncButton.MouseHover -= incrementPressed;
		}

		if( DecButton != null )
		{
			DecButton.MouseDown -= decrementPressed;
			DecButton.MouseHover -= decrementPressed;
		}

	}

	#endregion

	#region Event-handling and notification

	protected internal override void OnKeyDown( dfKeyEventArgs args )
	{

		if( Orientation == dfControlOrientation.Horizontal )
		{
			if( args.KeyCode == KeyCode.LeftArrow )
			{
				Value -= IncrementAmount;
				args.Use();
				return;
			}
			else if( args.KeyCode == KeyCode.RightArrow )
			{
				Value += IncrementAmount;
				args.Use();
				return;
			}
		}
		else
		{
			if( args.KeyCode == KeyCode.UpArrow )
			{
				Value -= IncrementAmount;
				args.Use();
				return;
			}
			else if( args.KeyCode == KeyCode.DownArrow )
			{
				Value += IncrementAmount;
				args.Use();
				return;
			}
		}

		base.OnKeyDown( args );

	}

	protected internal override void OnMouseWheel( dfMouseEventArgs args )
	{

		this.Value += IncrementAmount * -args.WheelDelta;

		args.Use();
		Signal( "OnMouseWheel", this, args );

	}

	protected internal override void OnMouseHover( dfMouseEventArgs args )
	{

		var ignoreEvent =
			args.Source == this.incButton ||
			args.Source == this.decButton ||
			args.Source == this.thumb;

		if( ignoreEvent )
			return;

		if( ( args.Source != track ) || !args.Buttons.IsSet( dfMouseButtons.Left ) )
		{
			base.OnMouseHover( args );
			return;
		}

		updateFromTrackClick( args );

		args.Use();
		Signal( "OnMouseHover", this, args );

	}

	protected internal override void OnMouseMove( dfMouseEventArgs args )
	{

		// Don't care about mouse movement over increment or 
		// decrement buttons
		if( args.Source == this.incButton || args.Source == this.decButton )
			return;

		if( ( args.Source != track && args.Source != thumb ) || !args.Buttons.IsSet( dfMouseButtons.Left ) )
		{
			base.OnMouseMove( args );
			return;
		}

		// Attempt to center the thumb on the mouse position
		this.Value = Mathf.Max( minValue, getValueFromMouseEvent( args ) - scrollSize * 0.5f );

		args.Use();
		Signal( "OnMouseMove", this, args );

	}

	protected internal override void OnMouseDown( dfMouseEventArgs args )
	{

		if( args.Buttons.IsSet( dfMouseButtons.Left ) )
			this.Focus();

		if( args.Source == incButton || args.Source == decButton )
			return;

		if( ( args.Source != track && args.Source != thumb ) || !args.Buttons.IsSet( dfMouseButtons.Left ) )
		{
			base.OnMouseDown( args );
			return;
		}

		if( args.Source == this.thumb )
		{

			// Find the point where the ray intersects the thumb
			RaycastHit hitInfo;
			thumb.collider.Raycast( args.Ray, out hitInfo, 1000f );

			// Calculate the thumb's center in global space
			var thumbCenter = thumb.transform.position + thumb.Pivot.TransformToCenter( thumb.Size * PixelsToUnits() );

			// Calculate the offset between the intersect point and the 
			// thumb's upper left corner so that the thumb can always
			// be positioned relative to the mouse while dragging
			this.thumbMouseOffset = ( thumbCenter - hitInfo.point );

		}
		else
		{
			updateFromTrackClick( args );
		}

		args.Use();
		Signal( "OnMouseDown", this, args );

	}

	protected internal virtual void OnValueChanged()
	{

		doAutoHide();
		Invalidate();

		SignalHierarchy( "OnValueChanged", this, this.Value );

		if( ValueChanged != null )
		{
			ValueChanged( this, this.Value );
		}

	}

	protected internal override void OnSizeChanged()
	{
		base.OnSizeChanged();
		updateThumb( this.rawValue );
	}

	#endregion

	#region Private utility methods

	private void doAutoHide()
	{

		if( !this.autoHide || !Application.isPlaying )
			return;

		if( Mathf.CeilToInt( ScrollSize ) >= Mathf.CeilToInt( maxValue - minValue ) )
		{
			this.Hide();
		}
		else
		{
			this.Show();
		}

	}

	private void incrementPressed( dfControl sender, dfMouseEventArgs args )
	{
		if( args.Buttons.IsSet( dfMouseButtons.Left ) )
		{
			Value += IncrementAmount;
			args.Use();
		}
	}

	private void decrementPressed( dfControl sender, dfMouseEventArgs args )
	{
		if( args.Buttons.IsSet( dfMouseButtons.Left ) )
		{
			Value -= IncrementAmount;
			args.Use();
		}
	}

	private void updateFromTrackClick( dfMouseEventArgs args )
	{

		var newValue = getValueFromMouseEvent( args );
		if( newValue > rawValue + scrollSize )
		{
			Value += scrollSize;
		}
		else if( newValue < rawValue )
		{
			Value -= scrollSize;
		}

	}

	private float adjustValue( float value )
	{
		var range = Mathf.Max( maxValue - minValue, 0 );
		var maxMinusScrollsize = Mathf.Max( range - scrollSize, 0 ) + minValue;
		var adjustedValue = Mathf.Max( Mathf.Min( maxMinusScrollsize, value ), minValue );
		return adjustedValue.Quantize( stepSize );
	}

	private void updateThumb( float rawValue )
	{

		if( controls.Count == 0 || thumb == null || track == null || !IsVisible )
			return;

		var valueRange = ( maxValue - minValue );
		if( valueRange <= 0 || valueRange <= scrollSize )
		{
			thumb.IsVisible = false;
			return;
		}

		thumb.IsVisible = true;

		var trackLength = ( orientation == dfControlOrientation.Horizontal )
			? track.Width
			: track.Height;

		var thumbLength = ( orientation == dfControlOrientation.Horizontal )
			? Mathf.Max( ( scrollSize / valueRange ) * trackLength, thumb.MinimumSize.x )
			: Mathf.Max( ( scrollSize / valueRange ) * trackLength, thumb.MinimumSize.y );

		var thumbSize = ( orientation == dfControlOrientation.Horizontal )
			? new Vector2( thumbLength, thumb.Height )
			: new Vector2( thumb.Width, thumbLength );

		if( Orientation == dfControlOrientation.Horizontal )
			thumbSize.x -= thumbPadding.horizontal;
		else
			thumbSize.y -= thumbPadding.vertical;

		thumb.Size = thumbSize;

		var lerp = ( rawValue - minValue ) / ( valueRange - scrollSize );
		var distance = lerp * ( trackLength - thumbLength );
		var thumbDirection = ( orientation == dfControlOrientation.Horizontal ) ? Vector3.right : Vector3.up;

		var centerOffset = ( Orientation == dfControlOrientation.Horizontal )
			? new Vector3( 0, ( track.Height - thumb.Height ) * 0.5f )
			: new Vector3( ( track.Width - thumb.Width ) * 0.5f, 0 );

		if( Orientation == dfControlOrientation.Horizontal )
			centerOffset.x = thumbPadding.left;
		else
			centerOffset.y = thumbPadding.top;

		if( thumb.Parent == this )
		{

			thumb.RelativePosition =
				track.RelativePosition +
				centerOffset +
				thumbDirection * distance;

		}
		else
		{
			thumb.RelativePosition = thumbDirection * distance + centerOffset;
		}

	}

	private float getValueFromMouseEvent( dfMouseEventArgs args )
	{

		var corners = track.GetCorners();
		var start = corners[ 0 ];
		var end = corners[ orientation == dfControlOrientation.Horizontal ? 1 : 2 ];

		var plane = new Plane( transform.TransformDirection( Vector3.back ), start );

		var ray = args.Ray;
		var distance = 0f;
		if( !plane.Raycast( ray, out distance ) )
			return this.rawValue;

		var hit = ray.origin + ray.direction * distance;

		if( args.Source == this.thumb )
		{
			hit += this.thumbMouseOffset;
		}

		var closest = closestPoint( start, end, hit, true );
		var lerp = ( closest - start ).magnitude / ( end - start ).magnitude;
		var rawValue = minValue + ( maxValue - minValue ) * lerp;

		return rawValue;

	}

	private static Vector3 closestPoint( Vector3 start, Vector3 end, Vector3 test, bool clamp )
	{

		// http://www.gamedev.net/community/forums/topic.asp?topic_id=198199&whichpage=1&#1250842

		Vector3 c = test - start;				// Vector from a to Point
		Vector3 v = ( end - start ).normalized;	// Unit Vector from a to b
		float d = ( end - start ).magnitude;	// Length of the line segment
		float t = Vector3.Dot( v, c );			// Intersection point Distance from a

		// Check to see if the point is on the line
		// if not then return the endpoint
		if( clamp )
		{
			if( t < 0 )
				return start;
			if( t > d )
				return end;
		}

		// get the distance to move from point a
		v *= t;

		// move from point a to the nearest point on the segment
		return start + v;

	}

	#endregion

}
