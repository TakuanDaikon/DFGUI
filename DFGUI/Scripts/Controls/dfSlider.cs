/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Allows the user to select any value from a specified range by 
/// moving an indicator along a horizontal or vertical track
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Allows the user to select any value from a specified range by moving an indicator along a horizontal or vertical track" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_slider.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Slider" )]
public class dfSlider : dfControl
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
	protected string backgroundSprite;

	[SerializeField]
	protected dfControlOrientation orientation = dfControlOrientation.Horizontal;

	[SerializeField]
	protected float rawValue = 10f;

	[SerializeField]
	protected float minValue = 0f;

	[SerializeField]
	protected float maxValue = 100f;

	[SerializeField]
	protected float stepSize = 1f;

	[SerializeField]
	protected float scrollSize = 1f;

	[SerializeField]
	protected dfControl thumb = null;

	[SerializeField]
	protected dfControl fillIndicator = null;

	[SerializeField]
	protected dfProgressFillMode fillMode = dfProgressFillMode.Fill;

	[SerializeField]
	protected RectOffset fillPadding = new RectOffset();

	[SerializeField]
	protected Vector2 thumbOffset = Vector2.zero;

	[SerializeField]
	protected bool rightToLeft = false;

	#endregion

	#region Public properties

	/// <summary>
	/// The <see cref="dfAtlas">Texture Atlas</see> containing the images used by this control
	/// </summary>
	public dfAtlas Atlas
	{
		get
		{
			if( this.atlas == null )
			{
				var view = GetManager();
				if( view != null )
				{
					return this.atlas = view.DefaultAtlas;
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
	/// The name of the image in the <see cref="Atlas"/> that will be used to 
	/// render the background of this control in its Default state
	/// </summary>
	public string BackgroundSprite
	{
		get { return backgroundSprite; }
		set
		{
			if( value != backgroundSprite )
			{
				backgroundSprite = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets/Sets the lower limit of the range of values this Slider can return
	/// </summary>
	public float MinValue
	{
		get { return this.minValue; }
		set
		{
			if( value != this.minValue )
			{
				this.minValue = value;
				if( rawValue < value )
					Value = value;
				updateValueIndicators( rawValue );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets/Sets the upper limit of the range of values this Slider can return
	/// </summary>
	public float MaxValue
	{
		get { return this.maxValue; }
		set
		{
			if( value != this.maxValue )
			{
				this.maxValue = value;
				if( rawValue > value )
					Value = value;
				updateValueIndicators( rawValue );
				Invalidate();
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
				this.Value = this.rawValue.Quantize( value );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the value to be added to or subtracted from the Value 
	/// property when user scrolls the mouse wheel over this control
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
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets a value indicating the horizontal or vertical 
	/// orientation of the Slider
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
				updateValueIndicators( this.rawValue );
			}
		}
	}

	/// <summary>
	/// Gets or sets a numeric value that represents the current position of 
	/// the scroll box on the slider
	/// </summary>
	public float Value
	{
		get { return this.rawValue; }
		set
		{
			value = Mathf.Max( minValue, Mathf.Min( maxValue, value.RoundToNearest( stepSize ) ) );
			if( !Mathf.Approximately( value, rawValue ) )
			{
				rawValue = value;
				OnValueChanged();
			}
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
				updateValueIndicators( this.rawValue );
			}
		}
	}

	/// <summary>
	/// Gets/Sets a reference to the dfControl used to display the value as a 
	/// progressively filled dfSprite.
	/// This property should refer to a child control.
	/// </summary>
	public dfControl Progress
	{
		get { return this.fillIndicator; }
		set
		{
			if( value != this.fillIndicator )
			{
				this.fillIndicator = value;
				Invalidate();
				updateValueIndicators( this.rawValue );
			}
		}
	}

	/// <summary>
	/// Indicates whether the progress indicator will be rendered via a stretched 
	/// sprite or a filled sprite
	/// </summary>
	public dfProgressFillMode FillMode
	{
		get { return this.fillMode; }
		set
		{
			if( value != this.fillMode )
			{
				this.fillMode = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding that will be applied when rendering
	/// the progress indicator
	/// </summary>
	public RectOffset FillPadding
	{
		get
		{
			if( fillPadding == null )
				fillPadding = new RectOffset();
			return this.fillPadding;
		}
		set
		{
			if( !RectOffset.Equals( value, this.fillPadding ) )
			{
				this.fillPadding = value;
				updateValueIndicators( this.rawValue );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The amount of padding to apply when positioning the Thumb control
	/// </summary>
	public Vector2 ThumbOffset
	{
		get
		{
			return this.thumbOffset;
		}
		set
		{
			if( Vector2.Distance( value, this.thumbOffset ) > float.Epsilon )
			{
				this.thumbOffset = value;
				updateValueIndicators( this.rawValue );
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the thumb position will be aligned
	/// to the right of the control when the Value is set to the
	/// minimum, or aligned to the left (default)
	/// </summary>
	public bool RightToLeft
	{
		get { return this.rightToLeft; }
		set
		{
			if( value != this.rightToLeft )
			{
				this.rightToLeft = value;
				updateValueIndicators( this.rawValue );
			}
		}
	}

	#endregion

	#region Event-handling and notification

	protected internal override void OnKeyDown( dfKeyEventArgs args )
	{

		if( args.Used )
		{
			return;
		}

		if( Orientation == dfControlOrientation.Horizontal )
		{
			if( args.KeyCode == KeyCode.LeftArrow )
			{
				this.Value -= ( this.rightToLeft ) ? -scrollSize : scrollSize;
				args.Use();
				return;
			}
			else if( args.KeyCode == KeyCode.RightArrow )
			{
				this.Value += ( this.rightToLeft ) ? -scrollSize : scrollSize;
				args.Use();
				return;
			}
		}
		else
		{
			if( args.KeyCode == KeyCode.UpArrow )
			{
				this.Value += ScrollSize;
				args.Use();
				return;
			}
			else if( args.KeyCode == KeyCode.DownArrow )
			{
				this.Value -= ScrollSize;
				args.Use();
				return;
			}
		}

		base.OnKeyDown( args );

	}

	public override void Start()
	{
		base.Start();
		updateValueIndicators( this.rawValue );
	}

	public override void OnEnable()
	{

		if( size.magnitude < float.Epsilon )
		{
			size = new Vector2( 100, 25 );
		}

		base.OnEnable();

		updateValueIndicators( this.rawValue );

	}

	protected internal override void OnMouseWheel( dfMouseEventArgs args )
	{

		var orientationDir = ( orientation == dfControlOrientation.Horizontal ) ? -1 : 1;

		this.Value += ( scrollSize * args.WheelDelta ) * orientationDir;
		args.Use();

		Signal( "OnMouseWheel", args );
		raiseMouseWheelEvent( args );

	}

	protected internal override void OnMouseMove( dfMouseEventArgs args )
	{

		if( !args.Buttons.IsSet( dfMouseButtons.Left ) )
		{
			base.OnMouseMove( args );
			return;
		}

		this.Value = getValueFromMouseEvent( args );
		args.Use();

		Signal( "OnMouseMove", this, args );
		raiseMouseMoveEvent( args );

	}

	protected internal override void OnMouseDown( dfMouseEventArgs args )
	{

		if( !args.Buttons.IsSet( dfMouseButtons.Left ) )
		{
			base.OnMouseMove( args );
			return;
		}

		this.Focus();

		this.Value = getValueFromMouseEvent( args );
		args.Use();

		Signal( "OnMouseDown", this, args );
		raiseMouseDownEvent( args );

	}

	protected internal override void OnSizeChanged()
	{
		base.OnSizeChanged();
		updateValueIndicators( this.rawValue );
	}

	protected internal virtual void OnValueChanged()
	{

		Invalidate();
		updateValueIndicators( rawValue );

		SignalHierarchy( "OnValueChanged", this, this.Value );

		if( ValueChanged != null )
		{
			ValueChanged( this, this.Value );
		}

	}

	#endregion

	#region Overrides

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

		if( Atlas == null )
			return;

		renderData.Material = Atlas.Material;

		renderBackground();

	}

	#endregion

	#region Private utility methods

	protected internal virtual void renderBackground()
	{

		if( Atlas == null )
			return;

		var spriteInfo = Atlas[ backgroundSprite ];
		if( spriteInfo == null )
		{
			return;
		}

		var color = ApplyOpacity( IsEnabled ? this.color : this.disabledColor );
		var options = new dfSprite.RenderOptions()
		{
			atlas = atlas,
			color = color,
			fillAmount = 1,
			flip = dfSpriteFlip.None,
			offset = pivot.TransformToUpperLeft( Size ),
			pixelsToUnits = PixelsToUnits(),
			size = Size,
			spriteInfo = spriteInfo
		};

		if( spriteInfo.border.horizontal == 0 && spriteInfo.border.vertical == 0 )
			dfSprite.renderSprite( renderData, options );
		else
			dfSlicedSprite.renderSprite( renderData, options );

	}

	private void updateValueIndicators( float rawValue )
	{

		if( Mathf.Approximately( this.MinValue, this.MaxValue ) )
		{

			// Having the same Min and Max values is not a valid condition, and will result in 
			// float.NaN values

			if( Application.isEditor )
			{
				Debug.LogWarning( "Slider Min and Max values cannot be the same", this );
			}

			if( thumb != null )
				thumb.IsVisible = false;

			if( fillIndicator != null )
				fillIndicator.IsVisible = false;

			return;

		}
		else
		{

			if( thumb != null )
				thumb.IsVisible = true;

			if( fillIndicator != null )
				fillIndicator.IsVisible = true;

		}

		if( thumb != null )
		{

			var endPoints = getEndPoints( true );
			var dir = ( endPoints[ 1 ] - endPoints[ 0 ] );
			var valueRange = maxValue - minValue;
			var distance = ( ( rawValue - minValue ) / valueRange ) * dir.magnitude;

			var offset = (Vector3)thumbOffset * PixelsToUnits();

			var thumbPos = endPoints[ 0 ] + dir.normalized * distance + offset;
			if( orientation == dfControlOrientation.Vertical || rightToLeft )
			{
				// Vertical sliders start at bottom
				thumbPos = endPoints[ 1 ] + -dir.normalized * distance + offset;
			}

			thumb.Pivot = dfPivotPoint.MiddleCenter;
			thumb.transform.position = thumbPos;

		}

		if( fillIndicator == null )
			return;

		var padding = this.FillPadding;
		var lerp = ( rawValue - minValue ) / ( maxValue - minValue );

		var indicatorPosition = new Vector3( padding.left, padding.top );
		var indicatorSize = this.size - new Vector2( padding.horizontal, padding.vertical );

		var indicator = fillIndicator as dfSprite;
		if( indicator != null && fillMode == dfProgressFillMode.Fill )
		{
			indicator.FillAmount = lerp;
			indicator.FillDirection = orientation == dfControlOrientation.Horizontal ? dfFillDirection.Horizontal : dfFillDirection.Vertical;
			indicator.InvertFill = rightToLeft || orientation == dfControlOrientation.Vertical;
		}
		else
		{

			if( orientation == dfControlOrientation.Horizontal )
			{
				indicatorSize.x = Width * lerp - padding.horizontal;
			}
			else
			{
				indicatorSize.y = Height * lerp - padding.vertical;
				indicatorPosition.y = Height - indicatorSize.y;
			}

		}

		fillIndicator.Size = indicatorSize;
		fillIndicator.RelativePosition = indicatorPosition;

	}

	private float getValueFromMouseEvent( dfMouseEventArgs args )
	{

		var endPoints = getEndPoints( true );
		var start = endPoints[ 0 ];
		var end = endPoints[ 1 ];

		if( orientation == dfControlOrientation.Vertical || rightToLeft )
		{
			start = endPoints[ 1 ];
			end = endPoints[ 0 ];
		}

		var plane = new Plane( transform.TransformDirection( Vector3.back ), start );

		var ray = args.Ray;
		var distance = 0f;
		if( !plane.Raycast( ray, out distance ) )
		{
			return this.rawValue;
		}

		var hit = ray.GetPoint( distance );

		var closest = closestPoint( start, end, hit, true );
		var lerp = ( closest - start ).magnitude / ( end - start ).magnitude;
		var rawValue = minValue + ( maxValue - minValue ) * lerp;


		return rawValue;

	}

	private Vector3[] getEndPoints()
	{
		return getEndPoints( false );
	}

	private Vector3[] getEndPoints( bool convertToWorld )
	{

		var offset = pivot.TransformToUpperLeft( Size );

		var start = new Vector3( offset.x, offset.y - size.y * 0.5f );
		var end = start + new Vector3( size.x, 0 );

		if( orientation == dfControlOrientation.Vertical )
		{
			start = new Vector3( offset.x + size.x * 0.5f, offset.y );
			end = start - new Vector3( 0, size.y );
		}

		if( convertToWorld )
		{
			var p2u = PixelsToUnits();
			var matrix = transform.localToWorldMatrix;
			start = matrix.MultiplyPoint( start * p2u );
			end = matrix.MultiplyPoint( end * p2u );
		}

		return new Vector3[] { start, end };

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
