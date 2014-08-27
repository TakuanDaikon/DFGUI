/* Copyright 2013-2014 Daikon Forge */

/****************************************************************************
 * PLEASE NOTE: The code in this file is under extremely active development
 * and is likely to change quite frequently. It is not recommended to modify
 * the code in this file, as your changes are likely to be overwritten by
 * the next product update when it is published.
 * **************************************************************************/

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using UnityColor = UnityEngine.Color;
using UnityMaterial = UnityEngine.Material;

/// <summary>
/// Used to display pseudo-HTML "rich text" 
/// </summary>
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Rich Text Label" )]
public class dfRichTextLabel : dfControl, IDFMultiRender, IRendersText
{

	#region Public events

	/// <summary>
	/// Raised whenever the value of the <see cref="Text"/> property changes
	/// </summary>
	public event PropertyChangedEventHandler<string> TextChanged;

	/// <summary>
	/// Raised when the value of the <see cref="ScrollPosition"/> property has changed
	/// </summary>
	public event PropertyChangedEventHandler<Vector2> ScrollPositionChanged;

	/// <summary>
	/// Defines the signature for methods which handle user clicks on anchor tags
	/// </summary>
	/// <param name="sender">The dfRichTextLabel control which raised the event</param>
	/// <param name="tag">Reference to the dfMarkupTag instance that was clicked</param>
	[dfEventCategory( "Markup" )]
	public delegate void LinkClickEventHandler( dfRichTextLabel sender, dfMarkupTagAnchor tag );

	/// <summary>
	/// Raised when the user clicks on a link 
	/// </summary>
	public event LinkClickEventHandler LinkClicked;

	#endregion

	#region Protected serialized fields

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected dfDynamicFont font;

	[SerializeField]
	protected string text = "Rich Text Label";

	[SerializeField]
	protected int fontSize = 16;

	[SerializeField]
	protected int lineheight = 16;

	[SerializeField]
	protected dfTextScaleMode textScaleMode = dfTextScaleMode.None;

	[SerializeField]
	protected FontStyle fontStyle = FontStyle.Normal;

	[SerializeField]
	protected bool preserveWhitespace = false;

	[SerializeField]
	protected string blankTextureSprite;

	[SerializeField]
	protected dfMarkupTextAlign align;

	[SerializeField]
	protected bool allowScrolling = false;

	[SerializeField]
	protected dfScrollbar horzScrollbar;

	[SerializeField]
	protected dfScrollbar vertScrollbar;

	[SerializeField]
	protected bool useScrollMomentum = false;

	[SerializeField]
	protected bool autoHeight = false;

	#endregion

	#region Private variables 

	private static dfRenderData clipBuffer = new dfRenderData();

	private dfList<dfRenderData> buffers = new dfList<dfRenderData>();
	private dfList<dfMarkupElement> elements = null;
	private dfMarkupBox viewportBox = null;

	private dfMarkupTag mouseDownTag = null;
	private Vector2 mouseDownScrollPosition = Vector2.zero;

	private Vector2 scrollPosition = Vector2.zero;
	private bool initialized = false;
	private bool isMouseDown = false;
	private Vector2 touchStartPosition = Vector2.zero;
	private Vector2 scrollMomentum = Vector2.zero;
	private bool isMarkupInvalidated = true;
	private Vector2 startSize = Vector2.zero;
	private bool isFontCallbackAssigned = false;

	#endregion

	#region Public properties

	/// <summary>
	/// Gets or sets whether the label will be automatically
	/// resized vertically to contain the rendered text.
	/// </summary>
	public bool AutoHeight
	{
		get { return this.autoHeight; }
		set
		{
			if( this.autoHeight != value )
			{
				this.autoHeight = value;
				scrollPosition = Vector2.zero;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The <see cref="dfAtlas">Texture Atlas</see> containing the images used by 
	/// the <see cref="dfRichTextLabel"/>
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
	/// Gets or sets the default TrueType or OpenType baseFont that will 
	/// be used to render the label text
	/// </summary>
	public dfDynamicFont Font
	{
		get { return this.font; }
		set
		{
			if( value != this.font )
			{

				unbindTextureRebuildCallback();
				this.font = value;
				bindTextureRebuildCallback();

				this.LineHeight = value.FontSize;

				dfFontManager.Invalidate( this.Font );
				Invalidate();

			}
		}
	}

	/// <summary>
	/// The name of the image in the <see cref="Atlas"/> that will be used to 
	/// render the selection, background, and cursor of this label
	/// </summary>
	public string BlankTextureSprite
	{
		get { return blankTextureSprite; }
		set
		{
			if( value != blankTextureSprite )
			{
				blankTextureSprite = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the text that will be displayed
	/// </summary>
	public string Text
	{
		get { return this.text; }
		set
		{
			value = this.getLocalizedValue( value );
			if( !string.Equals( this.text, value ) )
			{
				dfFontManager.Invalidate( this.Font );
				this.text = value;
				scrollPosition = Vector2.zero;
				Invalidate();
				OnTextChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the default size (in pixels) of the rendered text. Refers to the 
	/// maximum pixel height of each character.
	/// </summary>
	public int FontSize
	{
		get { return this.fontSize; }
		set
		{
			value = Mathf.Max( 6, value );
			if( value != this.fontSize )
			{
				dfFontManager.Invalidate( this.Font );
				this.fontSize = value;
				Invalidate();
			}
			LineHeight = value;
		}
	}

	/// <summary>
	/// Gets or sets the default height (in pixels) of a line of rendered text.
	/// </summary>
	public int LineHeight
	{
		get { return this.lineheight; }
		set
		{
			value = Mathf.Max( FontSize, value );
			if( value != this.lineheight )
			{
				this.lineheight = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the method by which text will be automatically scaled
	/// </summary>
	public dfTextScaleMode TextScaleMode
	{
		get { return this.textScaleMode; }
		set { this.textScaleMode = value; Invalidate(); }
	}

	/// <summary>
	/// Determines whether whitespace will be preserved by default.
	/// </summary>
	public bool PreserveWhitespace
	{
		get { return this.preserveWhitespace; }
		set
		{
			if( value != this.preserveWhitespace )
			{
				this.preserveWhitespace = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the default font style that will be used to render label text
	/// </summary>
	public FontStyle FontStyle
	{
		get { return this.fontStyle; }
		set
		{
			if( value != this.fontStyle )
			{
				this.fontStyle = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the type of text alignment to use when rendering the text
	/// </summary>
	public dfMarkupTextAlign TextAlignment
	{
		get { return this.align; }
		set
		{
			if( value != align )
			{
				align = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether this control allows scrolling
	/// </summary>
	public bool AllowScrolling
	{
		get { return this.allowScrolling; }
		set 
		{ 
			this.allowScrolling = value;
			if( !value )
			{
				ScrollPosition = Vector2.zero;
			}
		}
	}

	/// <summary>
	/// Gets or sets the upper-left position of the viewport relative
	/// to the entire scrollable area
	/// </summary>
	public Vector2 ScrollPosition
	{
		get { return this.scrollPosition; }
		set
		{

			if( !allowScrolling || autoHeight )
				value = Vector2.zero;

			if( isMarkupInvalidated )
				processMarkup();

			var maxPosition = ContentSize - Size;

			value = Vector2.Min( maxPosition, value );
			value = Vector2.Max( Vector2.zero, value );
			value = value.RoundToInt();

			if( ( value - this.scrollPosition ).sqrMagnitude > float.Epsilon )
			{
				this.scrollPosition = value;
				updateScrollbars();
				OnScrollPositionChanged();
			}

		}
	}

	/// <summary>
	/// Gets or sets a reference the the <see cref="dfScrollBar"/> instance
	/// that is used to scroll this control horizontally
	/// </summary>
	public dfScrollbar HorizontalScrollbar
	{
		get { return this.horzScrollbar; }
		set
		{
			this.horzScrollbar = value;
			updateScrollbars();
		}
	}

	/// <summary>
	/// Gets or sets a reference the the <see cref="dfScrollBar"/> instance
	/// that is used to scroll this control vertically
	/// </summary>
	public dfScrollbar VerticalScrollbar 
	{ 
		get { return this.vertScrollbar; }
		set
		{
			this.vertScrollbar = value;
			updateScrollbars();
		}
	}

	/// <summary>
	/// Returns the size (in pixels) of the rendered rich text context
	/// </summary>
	public Vector2 ContentSize
	{

		get
		{

			if( this.viewportBox != null )
				return viewportBox.Size;

			return this.Size;

		}

	}

	/// <summary>
	/// Gets or sets whether scrolling with the mousewheel or touch swipe will
	/// add a momentum effect to scrolling
	/// </summary>
	public bool UseScrollMomentum
	{
		get { return this.useScrollMomentum; }
		set { this.useScrollMomentum = value; scrollMomentum = Vector2.zero; }
	}

	#endregion

	#region dfControl overrides 

	protected internal override void OnLocalize()
	{
		base.OnLocalize();
		this.Text = getLocalizedValue( this.text );
	}

	[HideInInspector]
	public override void Invalidate()
	{
		base.Invalidate();
		dfFontManager.Invalidate( this.Font );
		isMarkupInvalidated = true;
	}

	public override void Awake()
	{
		base.Awake();
		startSize = this.Size;
	}

	public override void OnEnable()
	{
		
		base.OnEnable();

		bindTextureRebuildCallback();

		if( this.size.sqrMagnitude <= float.Epsilon )
		{
			this.Size = new Vector2( 320, 200 );
			this.FontSize = this.LineHeight = 16;
		}

	}

	public override void OnDisable()
	{
		base.OnDisable();
		unbindTextureRebuildCallback();
	}

	public override void Update()
	{

		base.Update();

		if( useScrollMomentum && !isMouseDown && scrollMomentum.magnitude > 0.5f )
		{
			ScrollPosition += scrollMomentum;
			scrollMomentum *= ( 0.95f - Time.deltaTime );
		}

	}

	public override void LateUpdate()
	{

		base.LateUpdate();

		// HACK: Need to perform initialization after all dependant objects 
		initialize();

	}

	#endregion

	#region Event handlers

	protected internal void OnTextChanged()
	{

		Invalidate();

		Signal( "OnTextChanged", this, this.text );

		if( TextChanged != null )
		{
			TextChanged( this, this.text );
		}

	}

	protected internal void OnScrollPositionChanged()
	{

		// NOTE: Not using this.Invalidate() because the markup is still valid,
		// we just need to signal that a re-render is necessary at the new 
		// scroll position
		base.Invalidate();

		SignalHierarchy( "OnScrollPositionChanged", this, this.ScrollPosition );

		if( ScrollPositionChanged != null )
		{
			ScrollPositionChanged( this, this.ScrollPosition );
		}

	}

	protected internal override void OnKeyDown( dfKeyEventArgs args )
	{

		if( args.Used )
		{
			base.OnKeyDown( args );
			return;
		}

		var horzAmount = FontSize; // horzScrollbar != null ? horzScrollbar.IncrementAmount : FontSize;
		var vertAmount = FontSize; // vertScrollbar != null ? vertScrollbar.IncrementAmount : FontSize;

		switch( args.KeyCode )
		{
			case KeyCode.LeftArrow:
				ScrollPosition += new Vector2( -horzAmount, 0 );
				args.Use();
				break;
			case KeyCode.RightArrow:
				ScrollPosition += new Vector2( horzAmount, 0 );
				args.Use();
				break;
			case KeyCode.UpArrow:
				ScrollPosition += new Vector2( 0, -vertAmount );
				args.Use();
				break;
			case KeyCode.DownArrow:
				ScrollPosition += new Vector2( 0, vertAmount );
				args.Use();
				break;
			case KeyCode.Home:
				ScrollToTop();
				args.Use();
				break;
			case KeyCode.End:
				ScrollToBottom();
				args.Use();
				break;
		}

		base.OnKeyDown( args );

	}

	internal override void OnDragEnd( dfDragEventArgs args )
	{
		base.OnDragEnd( args );
		isMouseDown = false;
	}

	protected internal override void OnMouseEnter( dfMouseEventArgs args )
	{
		base.OnMouseEnter( args );
		touchStartPosition = args.Position;
	}

	protected internal override void OnMouseDown( dfMouseEventArgs args )
	{

		base.OnMouseDown( args );

		this.mouseDownTag = hitTestTag( args );
		this.mouseDownScrollPosition = scrollPosition;

		scrollMomentum = Vector2.zero;
		touchStartPosition = args.Position;
		isMouseDown = true;

	}

	protected internal override void OnMouseUp( dfMouseEventArgs args )
	{

		base.OnMouseUp( args );

		isMouseDown = false;

		if( Vector2.Distance( scrollPosition, mouseDownScrollPosition ) <= 2 )
		{

			if( hitTestTag( args ) == mouseDownTag )
			{

				var linkTag = mouseDownTag;
				while( linkTag != null && !( linkTag is dfMarkupTagAnchor ) )
				{
					linkTag = linkTag.Parent as dfMarkupTag;
				}

				if( linkTag is dfMarkupTagAnchor )
				{

					Signal( "OnLinkClicked", this, linkTag );

					if( this.LinkClicked != null )
					{
						this.LinkClicked( this, linkTag as dfMarkupTagAnchor );
					}

				}

			}

		}

		mouseDownTag = null;
		mouseDownScrollPosition = scrollPosition;

	}

	protected internal override void OnMouseMove( dfMouseEventArgs args )
	{

		base.OnMouseMove( args );

		if( !allowScrolling || autoHeight )
			return;

		var scrollWithDrag =
			args is dfTouchEventArgs ||
			isMouseDown;

		if( scrollWithDrag )
		{

			if( ( args.Position - touchStartPosition ).magnitude > 5 )
			{
				
				var delta = args.MoveDelta.Scale( -1, 1 );

				// Calculate the effective screen size
				var manager = GetManager();
				var screenSize = manager.GetScreenSize();

				// Obtain a reference to the main camera
				var mainCamera = Camera.main ?? GetCamera();

				// Scale the movement amount by the difference between the "virtual" 
				// screen size and the real screen size
				delta.x = screenSize.x * ( delta.x / mainCamera.pixelWidth );
				delta.y = screenSize.y * ( delta.y / mainCamera.pixelHeight );

				// Set the new scroll position and momentum
				ScrollPosition += delta;
				scrollMomentum = ( scrollMomentum + delta ) * 0.5f;

			}

		}

	}

	protected internal override void OnMouseWheel( dfMouseEventArgs args )
	{

		try
		{

			if( args.Used || !allowScrolling || autoHeight )
				return;

			var wheelAmount = this.UseScrollMomentum ? 1 : 3;
			var amount = vertScrollbar != null ? vertScrollbar.IncrementAmount : FontSize * wheelAmount;

			ScrollPosition = new Vector2( scrollPosition.x, scrollPosition.y - amount * args.WheelDelta );
			scrollMomentum = new Vector2( 0, -amount * args.WheelDelta );

			args.Use();
			Signal( "OnMouseWheel", this, args );

		}
		finally
		{
			base.OnMouseWheel( args );
		}

	}

	#endregion 

	#region Public methods 

	/// <summary>
	/// Sets the scrollposition to the top
	/// </summary>
	public void ScrollToTop()
	{
		this.ScrollPosition = new Vector2( this.scrollPosition.x, 0 );
	}

	/// <summary>
	/// Sets the scrollposition to the top
	/// </summary>
	public void ScrollToBottom()
	{
		this.ScrollPosition = new Vector2( this.scrollPosition.x, int.MaxValue );
	}

	/// <summary>
	/// Sets the scrollposition to the top
	/// </summary>
	public void ScrollToLeft()
	{
		this.ScrollPosition = new Vector2( 0, this.scrollPosition.y );
	}

	/// <summary>
	/// Sets the scrollposition to the top
	/// </summary>
	public void ScrollToRight()
	{
		this.ScrollPosition = new Vector2( int.MaxValue, this.scrollPosition.y );
	}

	#endregion

	#region IDFMultiRender Members

	public dfList<dfRenderData> RenderMultiple()
	{

		if( !this.isVisible || this.Font == null )
			return null;

		var matrix = this.transform.localToWorldMatrix;

		if( !this.isControlInvalidated && viewportBox != null )
		{

			for( int i = 0; i < buffers.Count; i++ )
			{
				buffers[ i ].Transform = matrix;
			}

			return this.buffers;

		}

		try
		{

			// Clear the 'dirty' flag first, because some events (like font texture rebuild)
			// should be able to set the control to 'dirty' again.
			this.isControlInvalidated = false;

			// Parse the markup and perform document layout
			if( isMarkupInvalidated )
			{
				isMarkupInvalidated = false;
				processMarkup();
			}

			// Ensure that our viewport box has been properly resized to 
			// fully encompass all nodes, because the resulting size of 
			// the viewport box will be used to update scrollbars and 
			// determine max scroll position.
			viewportBox.FitToContents();

			// Perform auto-sizing if indicated
			if( autoHeight )
			{
				this.Height = viewportBox.Height;
			}

			// Update scrollbars to match rendered height 
			updateScrollbars();

			//@Profiler.BeginSample( "Gather markup render buffers" );
			buffers.Clear();
			gatherRenderBuffers( viewportBox, this.buffers );

			return this.buffers;

		}
		finally
		{
			updateCollider();
		}

	}

	#endregion 

	#region Private utility methods 

	private dfMarkupTag hitTestTag( dfMouseEventArgs args )
	{

		var relativeMousePosition = this.GetHitPosition( args ) + scrollPosition;
		var hitBox = viewportBox.HitTest( relativeMousePosition );
		if( hitBox != null )
		{

			var tag = hitBox.Element;
			while( tag != null && !( tag is dfMarkupTag ) )
			{
				tag = tag.Parent;
			}

			return tag as dfMarkupTag;

		}

		return null;

	}

	private void processMarkup()
	{

		releaseMarkupReferences();

		this.elements = dfMarkupParser.Parse( this, this.text );

		var scaleMultiplier = getTextScaleMultiplier();

		var scaledFontSize = Mathf.CeilToInt( this.FontSize * scaleMultiplier );
		var scaledLineHeight = Mathf.CeilToInt( this.LineHeight * scaleMultiplier );

		var style = new dfMarkupStyle()
		{
			Host = this,
			Atlas = this.Atlas,
			Font = this.Font,
			FontSize = scaledFontSize,
			FontStyle = this.FontStyle,
			LineHeight = scaledLineHeight,
			Color = ApplyOpacity( this.Color ),
			Opacity = this.CalculateOpacity(),
			Align = this.TextAlignment,
			PreserveWhitespace = this.preserveWhitespace
		};

		viewportBox = new dfMarkupBox( null, dfMarkupDisplayType.block, style )
		{
			Size = this.Size
		};

		for( int i = 0; i < elements.Count; i++ )
		{
			var child = elements[ i ];
			if( child != null )
			{
				child.PerformLayout( viewportBox, style );
			}
		}

	}

	private float getTextScaleMultiplier()
	{

		if( textScaleMode == dfTextScaleMode.None || !Application.isPlaying )
			return 1f;

		// Return the difference between design resolution and current resolution
		if( textScaleMode == dfTextScaleMode.ScreenResolution )
		{
			return (float)Screen.height / (float)cachedManager.FixedHeight;
		}

		return Size.y / startSize.y;

	}

	private void releaseMarkupReferences()
	{

		this.mouseDownTag = null;

		if( viewportBox != null )
		{
			viewportBox.Release();
		}

		if( elements != null )
		{

			for( int i = 0; i < elements.Count; i++ )
			{
				elements[ i ].Release();
			}

			elements.Release();

		}

	}

	[HideInInspector]
	private void initialize()
	{

		if( initialized )
			return;

		initialized = true;

		if( Application.isPlaying )
		{

			if( horzScrollbar != null )
			{
				horzScrollbar.ValueChanged += horzScroll_ValueChanged;
			}

			if( vertScrollbar != null )
			{
				vertScrollbar.ValueChanged += vertScroll_ValueChanged;
			}

		}

		Invalidate();
		ScrollPosition = Vector2.zero;

		updateScrollbars();

	}

	private void vertScroll_ValueChanged( dfControl control, float value )
	{
		ScrollPosition = new Vector2( scrollPosition.x, value );
	}

	private void horzScroll_ValueChanged( dfControl control, float value )
	{
		ScrollPosition = new Vector2( value, ScrollPosition.y );
	}

	private void updateScrollbars()
	{

		if( horzScrollbar != null )
		{
			horzScrollbar.MinValue = 0;
			horzScrollbar.MaxValue = ContentSize.x;
			horzScrollbar.ScrollSize = Size.x;
			horzScrollbar.Value = ScrollPosition.x;
		}

		if( vertScrollbar != null )
		{
			vertScrollbar.MinValue = 0;
			vertScrollbar.MaxValue = ContentSize.y;
			vertScrollbar.ScrollSize = Size.y;
			vertScrollbar.Value = ScrollPosition.y;
		}

	}

	private void gatherRenderBuffers( dfMarkupBox box, dfList<dfRenderData> buffers )
	{

		var intersectionType = getViewportIntersection( box );
		if( intersectionType == dfIntersectionType.None )
		{
			return;
		}

		var buffer = box.Render();
		if( buffer != null )
		{

			if( buffer.Material == null )
			{
				if( this.atlas != null )
				{
					buffer.Material = atlas.Material;
				}
			}

			var p2u = PixelsToUnits();
			var scroll = -scrollPosition.Scale( 1, -1 ).RoundToInt();
			var offset = (Vector3)( scroll + box.GetOffset().Scale( 1, -1 ) ) + pivot.TransformToUpperLeft( Size );
			
			var vertices = buffer.Vertices;
			for( int i = 0; i < buffer.Vertices.Count; i++ )
			{
				vertices[ i ] = ( offset + vertices[ i ] ) * p2u;
			}

			if( intersectionType == dfIntersectionType.Intersecting )
			{
				clipToViewport( buffer );
			}

			buffer.Transform = transform.localToWorldMatrix;
			buffers.Add( buffer );

		}

		for( int i = 0; i < box.Children.Count; i++ )
		{
			gatherRenderBuffers( box.Children[ i ], buffers );
		}

	}

	private dfIntersectionType getViewportIntersection( dfMarkupBox box )
	{

		if( box.Display == dfMarkupDisplayType.none )
			return dfIntersectionType.None;

		var viewSize = this.Size;
		var min = box.GetOffset() - scrollPosition;
		var max = min + box.Size;

		if( max.x <= 0 || max.y <= 0 )
			return dfIntersectionType.None;

		if( min.x >= viewSize.x || min.y >= viewSize.y )
			return dfIntersectionType.None;

		if( min.x < 0 || min.y < 0 || max.x > viewSize.x || max.y > viewSize.y )
			return dfIntersectionType.Intersecting;

		return dfIntersectionType.Inside;

	}

	private void clipToViewport( dfRenderData renderData )
	{

		var planes = getViewportClippingPlanes();

		var material = renderData.Material;
		var matrix = renderData.Transform;

		clipBuffer.Clear();
		dfClippingUtil.Clip( planes, renderData, clipBuffer );

		renderData.Clear();
		renderData.Merge( clipBuffer, false );
		renderData.Material = material;
		renderData.Transform = matrix;

	}

	private Plane[] getViewportClippingPlanes()
	{

		// Translate corners back into local space
		var corners = GetCorners();
		var matrix = transform.worldToLocalMatrix;
		for( int i = 0; i < corners.Length; i++ )
		{
			corners[ i ] = matrix.MultiplyPoint( corners[ i ] );
		}

		cachedClippingPlanes[ 0 ] = new Plane( Vector3.right, corners[ 0 ] );
		cachedClippingPlanes[ 1 ] = new Plane( Vector3.left, corners[ 1 ] );
		cachedClippingPlanes[ 2 ] = new Plane( Vector3.up, corners[ 2 ] );
		cachedClippingPlanes[ 3 ] = new Plane( Vector3.down, corners[ 0 ] );

		return cachedClippingPlanes;

	}

	#endregion

	#region IRendersText Members

	public void UpdateFontInfo()
	{

		if( !dfFontManager.IsDirty( this.Font ) )
			return;

		if( string.IsNullOrEmpty( this.text ) )
			return;

		updateFontInfo( viewportBox );

	}

	private void updateFontInfo( dfMarkupBox box )
	{

		if( box == null )
			return;

		var intersectionType = ( box == viewportBox ) ? dfIntersectionType.Inside : getViewportIntersection( box );
		if( intersectionType == dfIntersectionType.None )
		{
			return;
		}

		var textBox = box as dfMarkupBoxText;
		if( textBox != null )
		{
			Profiler.BeginSample( "Adding character request" );
			font.AddCharacterRequest( textBox.Text, textBox.Style.FontSize, textBox.Style.FontStyle );
			Profiler.EndSample();
		}

		for( int i = 0; i < box.Children.Count; i++ )
		{
			updateFontInfo( box.Children[ i ] );
		}

	}

	private void onFontTextureRebuilt()
	{
		Invalidate();
		updateFontInfo( viewportBox );
	}

	private void bindTextureRebuildCallback()
	{

		if( isFontCallbackAssigned || Font == null )
			return;

		var baseFont = Font.BaseFont;
		baseFont.textureRebuildCallback = (UnityEngine.Font.FontTextureRebuildCallback)Delegate.Combine( baseFont.textureRebuildCallback, (Font.FontTextureRebuildCallback)this.onFontTextureRebuilt );

		isFontCallbackAssigned = true;

	}

	private void unbindTextureRebuildCallback()
	{

		if( !isFontCallbackAssigned || Font == null)
			return;

		var baseFont = Font.BaseFont;
		baseFont.textureRebuildCallback = (UnityEngine.Font.FontTextureRebuildCallback)Delegate.Remove( baseFont.textureRebuildCallback, (UnityEngine.Font.FontTextureRebuildCallback)this.onFontTextureRebuilt );

		isFontCallbackAssigned = false;

	}

	#endregion

}
