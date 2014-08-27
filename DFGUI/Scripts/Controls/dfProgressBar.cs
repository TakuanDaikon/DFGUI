/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implements a progress bar that can be used to display
/// the progress from a start value toward an end value, such as the
/// amount of work completed or a player's progress toward some goal, etc.
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Implements a progress bar that can be used to display the progress from a start value toward an end value, such as the amount of work completed or a player's progress toward some goal, etc." )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_progress_bar.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Progress Bar" )]
public class dfProgressBar : dfControl
{

	#region Public events

	/// <summary>
	/// Raised whenever the value of the <see cref="Value"/> property has changed
	/// </summary>
	public event PropertyChangedEventHandler<float> ValueChanged;

	#endregion

	#region Protected serialized fields

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected string backgroundSprite;

	[SerializeField]
	protected string progressSprite;

	[SerializeField]
	protected Color32 progressColor = UnityEngine.Color.white;

	[SerializeField]
	protected float rawValue = 0.25f;

	[SerializeField]
	protected float minValue = 0f;

	[SerializeField]
	protected float maxValue = 1f;

	[SerializeField]
	protected dfProgressFillMode fillMode = dfProgressFillMode.Stretch;

	[SerializeField]
	protected RectOffset padding = new RectOffset();

	[SerializeField]
	protected bool actAsSlider = false;

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
	/// render the background of this control
	/// </summary>
	public string BackgroundSprite
	{
		get { return backgroundSprite; }
		set
		{
			if( value != backgroundSprite )
			{
				backgroundSprite = value;
				setDefaultSize( value );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The name of the image in the <see cref="Atlas"/> that will be used to 
	/// render the progress indicator of this control
	/// </summary>
	public string ProgressSprite
	{
		get { return progressSprite; }
		set
		{
			if( value != progressSprite )
			{
				progressSprite = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the color used when rendering the progress indicator sprite
	/// </summary>
	public Color32 ProgressColor
	{
		get { return this.progressColor; }
		set
		{
			if( !Color32.Equals( value, progressColor ) )
			{
				this.progressColor = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the lower limit of the range of values this progress bar can return
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
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the upper limit of the range of values this progress bar can return
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
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets a numeric value that represents the current progress value
	/// </summary>
	public float Value
	{
		get { return this.rawValue; }
		set
		{
			value = Mathf.Max( minValue, Mathf.Min( maxValue, value ) );
			if( !Mathf.Approximately( value, rawValue ) )
			{
				rawValue = value;
				OnValueChanged();
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
	/// Gets or sets the amount of padding that will be applied to when
	/// rendering the progress indicator
	/// </summary>
	public RectOffset Padding
	{
		get
		{
			if( padding == null )
				padding = new RectOffset();
			return this.padding;
		}
		set
		{
			if( !RectOffset.Equals( value, this.padding ) )
			{
				this.padding = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// If set to TRUE, this <see cref="ProgressBar"/> will act as a <see cref="dfSlider"/>
	/// control, responding to user input with the mouse and keyboard. This allows the 
	/// developer to create a basic slider control with minimal setup compared to the 
	/// <see cref="dfSlider"/> class.
	/// </summary>
	public bool ActAsSlider
	{
		get { return this.actAsSlider; }
		set { this.actAsSlider = value; }
	}

	#endregion

	#region Event-handling and notification

	protected internal override void OnMouseWheel( dfMouseEventArgs args )
	{

		try
		{

			if( !actAsSlider )
				return;

			var scrollSize = ( this.maxValue - this.minValue ) * 0.1f;
			this.Value += ( scrollSize * Mathf.RoundToInt( -args.WheelDelta ) );

			args.Use();

		}
		finally
		{
			base.OnMouseWheel( args );
		}

	}

	protected internal override void OnMouseMove( dfMouseEventArgs args )
	{

		try
		{

			if( !actAsSlider )
				return;

			if( !args.Buttons.IsSet( dfMouseButtons.Left ) )
			{
				return;
			}

			this.Value = getValueFromMouseEvent( args );

			args.Use();

		}
		finally
		{
			base.OnMouseMove( args );
		}

	}

	protected internal override void OnMouseDown( dfMouseEventArgs args )
	{

		try
		{

			if( !actAsSlider )
				return;

			if( !args.Buttons.IsSet( dfMouseButtons.Left ) )
			{
				return;
			}

			this.Focus();

			this.Value = getValueFromMouseEvent( args );

			args.Use();

		}
		finally
		{
			base.OnMouseDown( args );
		}

	}

	protected internal override void OnKeyDown( dfKeyEventArgs args )
	{

		try
		{

			if( !actAsSlider )
				return;

			var scrollSize = ( this.maxValue - this.minValue ) * 0.1f;

			if( args.KeyCode == KeyCode.LeftArrow )
			{
				this.Value -= scrollSize;
				args.Use();
				return;
			}
			else if( args.KeyCode == KeyCode.RightArrow )
			{
				this.Value += scrollSize;
				args.Use();
				return;
			}

		}
		finally
		{
			base.OnKeyDown( args );
		}

	}

	protected internal virtual void OnValueChanged()
	{

		Invalidate();

		SignalHierarchy( "OnValueChanged", this, this.Value );

		if( ValueChanged != null )
		{
			ValueChanged( this, this.Value );
		}

	}

	#endregion

	#region Rendering

	protected override void OnRebuildRenderData()
	{

		if( Atlas == null )
			return;

		renderData.Material = Atlas.Material;

		renderBackground();
		renderProgressFill();

	}

	private void renderProgressFill()
	{

		if( Atlas == null )
			return;

		var spriteInfo = Atlas[ progressSprite ];
		if( spriteInfo == null )
		{
			return;
		}

		var paddingOffset = new Vector3( padding.left, -padding.top );

		var fillSize = new Vector2( size.x - padding.horizontal, size.y - padding.vertical );
		var progressFill = 1f;
		var valueRange = maxValue - minValue;
		var lerp = ( rawValue - minValue ) / valueRange;

		// There is a minimum size that sliced sprites can be stretched to, which is the 
		// sum of their left and right borders. If the fill amount is less than that, we
		// automatically switch the FillMode to Fill instead of Stretch
		var activeMode = fillMode;
		if( activeMode == dfProgressFillMode.Stretch && fillSize.x * lerp < spriteInfo.border.horizontal )
		{

			// TODO: Make this an option... Doesn't look right on sprites that have transparent padding in the image
			//activeMode = ProgressFillMode.Fill;

			// TODO: Switching to fill should resize the image instead of stretching to fill 
			// the entire control then performing fill operation. 

		}

		if( activeMode == dfProgressFillMode.Fill )
		{
			progressFill = lerp;
		}
		else
		{
			fillSize.x = Mathf.Max( spriteInfo.border.horizontal, fillSize.x * lerp );
		}

		var color = ApplyOpacity( IsEnabled ? this.ProgressColor : this.DisabledColor );
		var options = new dfSprite.RenderOptions()
		{
			atlas = atlas,
			color = color,
			fillAmount = progressFill,
			flip = dfSpriteFlip.None,
			offset = pivot.TransformToUpperLeft( Size ) + paddingOffset,
			pixelsToUnits = PixelsToUnits(),
			size = fillSize,
			spriteInfo = spriteInfo
		};

		if( spriteInfo.border.horizontal == 0 && spriteInfo.border.vertical == 0 )
			dfSprite.renderSprite( renderData, options );
		else
			dfSlicedSprite.renderSprite( renderData, options );

	}

	private void renderBackground()
	{

		if( Atlas == null )
			return;

		var spriteInfo = Atlas[ backgroundSprite ];
		if( spriteInfo == null )
		{
			return;
		}

		var color = ApplyOpacity( IsEnabled ? this.Color : this.DisabledColor );
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

	#endregion

	#region Private utility methods

	private float getValueFromMouseEvent( dfMouseEventArgs args )
	{

		var endPoints = getEndPoints( true );
		var start = endPoints[ 0 ];
		var end = endPoints[ 1 ];

		var plane = new Plane( transform.TransformDirection( Vector3.back ), start );

		var ray = args.Ray;
		var distance = 0f;
		if( !plane.Raycast( ray, out distance ) )
		{
			return this.rawValue;
		}

		var hit = ray.origin + ray.direction * distance;

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

		var start = new Vector3( offset.x + padding.left, offset.y - size.y * 0.5f );
		var end = start + new Vector3( size.x - padding.right, 0 );

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

	private void setDefaultSize( string spriteName )
	{

		if( Atlas == null )
			return;

		var spriteInfo = Atlas[ spriteName ];
		if( size == Vector2.zero && spriteInfo != null )
		{
			Size = spriteInfo.sizeInPixels;
		}

	}

	#endregion

}
