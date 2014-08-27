/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

#region Documentation
/// <summary>
/// Used to display text information on the screen. The Label control can optionally 
/// use embedded markup to specify colors and embedded sprites.
/// <h3>The Color Tag</h3>
/// <para>The <b>color</b> tag is used to render a section of text in a different
/// color than the default, and is used to surround the text that should be 
/// rendered in that color. Note that the section to be colorized must be followed
/// by a closing tag ([/color]). For instance, <b>[color red]This is red[/color]</b> will 
/// result in the words <span style="color: red">This is red</span> being displayed in red.</para>
/// <para>You can use hexidecimal formatting for the color value, which is defined
/// by using a pound sign followed by the six-digit hexidecimal value of the color, 
/// where the components are specified in Red, Green, Blue order.
/// For instance, the value #FF0000 results in red, #00FF00 results in green, and 
/// #0000FF results in blue.</para>
/// <para>You can also use a few pre-defined color names: aqua, black, blue, cyan,
/// fuchsia, gray, green, lime, magenta, maroon, navy, olive, orange, purple, red,
/// silver, teal, white, and yellow.</para>
/// <h3>The Sprite Tag</h3>
/// <para>The <b>Sprite</b> tag is used to display a sprite inline with the text.
/// It takes a single quoted parameter which corresponds to the name of a sprite 
/// in the same Texture Atlas as the label. It does not require an end tag.</para>
/// <para>To embed a sprite named "smiley face" you would use the format
/// <b>[sprite "smiley face"].</b></para>
/// </summary>
#endregion
[Serializable]
[ExecuteInEditMode]
[dfCategory( "Basic Controls" )]
[dfTooltip( "Displays text information, optionally allowing the use of markup to specify colors and embedded sprites" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_label.html" )]
[AddComponentMenu( "Daikon Forge/User Interface/Label" )]
public class dfLabel : dfControl, IDFMultiRender, IRendersText
{

	#region Public events

	/// <summary>
	/// Raised whenever the value of the <see cref="Text"/> property changes
	/// </summary>
	public event PropertyChangedEventHandler<string> TextChanged;

	#endregion

	#region Serialized data members

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected dfFontBase font;

	[SerializeField]
	protected string backgroundSprite;

	[SerializeField]
	protected Color32 backgroundColor = UnityEngine.Color.white;

	[SerializeField]
	protected bool autoSize = false;

	[SerializeField]
	protected bool autoHeight = false;

	[SerializeField]
	protected bool wordWrap = false;

	[SerializeField]
	protected string text = "Label";

	[SerializeField]
	protected Color32 bottomColor = new Color32( 255, 255, 255, 255 );

	[SerializeField]
	protected TextAlignment align;

	[SerializeField]
	protected dfVerticalAlignment vertAlign = dfVerticalAlignment.Top;

	[SerializeField]
	protected float textScale = 1f;

	[SerializeField]
	protected dfTextScaleMode textScaleMode = dfTextScaleMode.None;

	[SerializeField]
	protected int charSpacing = 0;

	[SerializeField]
	protected bool colorizeSymbols = false;

	[SerializeField]
	protected bool processMarkup = false;

	[SerializeField]
	protected bool outline = false;

	[SerializeField]
	protected int outlineWidth = 1;

	[SerializeField]
	protected bool enableGradient = false;

	[SerializeField]
	protected Color32 outlineColor = UnityEngine.Color.black;

	[SerializeField]
	protected bool shadow = false;

	[SerializeField]
	protected Color32 shadowColor = UnityEngine.Color.black;

	[SerializeField]
	protected Vector2 shadowOffset = new Vector2( 1, -1 );

	[SerializeField]
	protected RectOffset padding = new RectOffset();

	[SerializeField]
	protected int tabSize = 48;

	// TODO: Consider implementing "elastic tabstops" - http://nickgravgaard.com/elastictabstops/
	[SerializeField]
	protected List<int> tabStops = new List<int>();

	#endregion

	#region Public properties

	/// <summary>
	/// The <see cref="dfAtlas">Texture Atlas</see> containing the images used by 
	/// the <see cref="Font"/>
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
	/// Gets or sets the <see cref="dfFont"/> instance that will be used 
	/// to render the text
	/// </summary>
	public dfFontBase Font
	{
		get
		{
			if( this.font == null )
			{
				var view = this.GetManager();
				if( view != null )
				{
					this.font = view.DefaultFont;
				}
			}
			return this.font;
		}
		set
		{
			if( value != this.font )
			{
				unbindTextureRebuildCallback();
				this.font = value;
				bindTextureRebuildCallback();
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The name of the image in the <see cref="Atlas"/> that will be used to 
	/// render the background of this label
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
	/// Gets or set the color that will be applied to the background sprite
	/// </summary>
	public Color32 BackgroundColor
	{
		get { return backgroundColor; }
		set
		{
			if( !Color32.Equals( value, backgroundColor ) )
			{
				backgroundColor = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the size multiplier that will be used to render text
	/// </summary>
	public float TextScale
	{
		get { return this.textScale; }
		set
		{
			value = Mathf.Max( 0.1f, value );
			if( !Mathf.Approximately( textScale, value ) )
			{
				dfFontManager.Invalidate( this.Font );
				this.textScale = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the TextScale property will be automatically 
	/// adjusted to match runtime screen resolution
	/// </summary>
	public dfTextScaleMode TextScaleMode
	{
		get { return this.textScaleMode; }
		set { this.textScaleMode = value; Invalidate(); }
	}

	/// <summary>
	/// Gets or sets the amount of additional spacing (in pixels) that will 
	/// be applied when rendering the text
	/// </summary>
	public int CharacterSpacing
	{
		get { return this.charSpacing; }
		set
		{
			value = Mathf.Max( 0, value );
			if( value != this.charSpacing )
			{
				this.charSpacing = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets/Sets a value indicating whether symbols (sprites embedded in the 
	/// text) should be colorized
	/// </summary>
	public bool ColorizeSymbols
	{
		get { return this.colorizeSymbols; }
		set
		{
			if( value != colorizeSymbols )
			{
				colorizeSymbols = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets/Sets a value indicating whether embedded markup codes are processed
	/// </summary>
	public bool ProcessMarkup
	{
		get { return this.processMarkup; }
		set
		{
			if( value != processMarkup )
			{
				processMarkup = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the label is drawn with a vertical gradient, using
	/// the Color property as the top of the gradient and the BottomColor property
	/// to specify the bottom of the gradient.
	/// </summary>
	public bool ShowGradient
	{
		get { return this.enableGradient; }
		set
		{
			if( value != this.enableGradient )
			{
				this.enableGradient = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the color for the bottom of the gradient
	/// </summary>
	public Color32 BottomColor
	{
		get { return this.bottomColor; }
		set
		{
			if( !bottomColor.Equals( value ) )
			{
				bottomColor = value;
				OnColorChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the value of the text that will be rendered
	/// </summary>
	public string Text
	{
		get { return this.text; }
		set
		{

			if( value == null )
				value = string.Empty;
			else
				value = value.Replace( "\\t", "\t" ).Replace( "\\n", "\n" );

			if( !string.Equals( value, this.text ) )
			{
				dfFontManager.Invalidate( this.Font );
				this.localizationKey = value;
				this.text = this.getLocalizedValue( value );
				OnTextChanged();
			}

		}
	}

	/// <summary>
	/// Gets or sets whether the <see cref="dfLabel"/> label will be automatically
	/// resized to contain the rendered text.
	/// </summary>
	public bool AutoSize
	{
		get { return autoSize; }
		set
		{
			if( value != autoSize )
			{
				if( value )
					autoHeight = false;
				autoSize = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the label will be automatically
	/// resized vertically to contain the rendered text.
	/// </summary>
	public bool AutoHeight
	{
		get { return autoHeight && !autoSize; }
		set
		{
			if( value != autoHeight )
			{
				if( value )
					autoSize = false;
				autoHeight = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether Word Wrap should be used when rendering the text.
	/// </summary>
	public bool WordWrap
	{
		get { return wordWrap; }
		set
		{
			if( value != wordWrap )
			{
				wordWrap = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the type of text alignment to use when rendering the text
	/// </summary>
	public TextAlignment TextAlignment
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
	/// Gets or sets the vertical alignment to use when rendering the text
	/// </summary>
	public dfVerticalAlignment VerticalAlignment
	{
		get { return this.vertAlign; }
		set
		{
			if( value != this.vertAlign )
			{
				this.vertAlign = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the text should be rendered with an outline
	/// </summary>
	public bool Outline
	{
		get { return this.outline; }
		set
		{
			if( value != outline )
			{
				outline = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the width of the outline effect
	/// </summary>
	public int OutlineSize
	{
		get { return this.outlineWidth; }
		set
		{
			value = Mathf.Max( 0, value );
			if( value != this.outlineWidth )
			{
				this.outlineWidth = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the color of the outline that will be rendered if the <see cref="Outline"/>
	/// property is set to TRUE
	/// </summary>
	public Color32 OutlineColor
	{
		get { return this.outlineColor; }
		set
		{
			if( !value.Equals( outlineColor ) )
			{
				outlineColor = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the text should be rendered with a shadow effect
	/// </summary>
	public bool Shadow
	{
		get { return this.shadow; }
		set
		{
			if( value != shadow )
			{
				shadow = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the color of the shadow that will be rendered if the <see cref="Shadow"/>
	/// property is set to TRUE
	/// </summary>
	public Color32 ShadowColor
	{
		get { return this.shadowColor; }
		set
		{
			if( !value.Equals( shadowColor ) )
			{
				shadowColor = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the distance that the shadow that will be offset if the <see cref="Shadow"/>
	/// property is set to TRUE
	/// </summary>
	public Vector2 ShadowOffset
	{
		get { return this.shadowOffset; }
		set
		{
			if( value != shadowOffset )
			{
				shadowOffset = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding that will be added to the label's borders 
	/// when rendering the text
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
			value = value.ConstrainPadding();
			if( !RectOffset.Equals( value, this.padding ) )
			{
				this.padding = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The width (in pixels) of a tab character embedded in the <see cref="Text"/>
	/// </summary>
	public int TabSize
	{
		get { return this.tabSize; }
		set
		{
			value = Mathf.Max( 0, value );
			if( value != this.tabSize )
			{
				this.tabSize = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Returns the list of tab stop positions
	/// </summary>
	public List<int> TabStops
	{
		get { return this.tabStops; }
	}

	#endregion

	#region Private runtime variables

	private Vector2 startSize = Vector2.zero;
	private bool isFontCallbackAssigned = false;

	#endregion

	#region Base class overrides

	protected internal override void OnLocalize()
	{
		base.OnLocalize();
		this.Text = getLocalizedValue( this.localizationKey ?? this.text );
	}

	protected internal void OnTextChanged()
	{

		Invalidate();

		Signal( "OnTextChanged", this, this.text );

		if( TextChanged != null )
		{
			TextChanged( this, this.text );
		}

	}

	public override void Start()
	{
		base.Start();
		this.localizationKey = this.Text;
	}

	public override void OnEnable()
	{

		base.OnEnable();

		#region Ensure that this label always has a valid font, if possible

		var validFont =
			Font != null &&
			Font.IsValid;

		if( Application.isPlaying && !validFont )
		{
			Font = GetManager().DefaultFont;
		}

		bindTextureRebuildCallback();

		#endregion

		// Default size for newly-created dfLabel controls
		if( size.sqrMagnitude <= float.Epsilon )
		{
			this.Size = new Vector2( 150, 25 );
		}

	}

	public override void OnDisable()
	{
		base.OnDisable();
		unbindTextureRebuildCallback();
	}

	public override void Update()
	{

		// Autosize overrides autoheight (may only be an issue during dev, where this
		// value is being set on a protected field via the default Inspector rather 
		// than through the exposed property).
		if( autoSize )
			autoHeight = false;

		// Make sure that there is always a font assigned, if possible
		if( this.Font == null )
		{
			this.Font = GetManager().DefaultFont;
		}

		// IMPORTANT: Must call base class Update() method to ensure proper operation
		base.Update();

	}

	public override void Awake()
	{
		base.Awake();
		startSize = Application.isPlaying ? this.Size : Vector2.zero;
	}

	#endregion

	#region Private utility methods

	public override Vector2 CalculateMinimumSize()
	{

		if( this.Font != null )
		{
			var fontSize = Font.FontSize * TextScale * 0.75f;
			return Vector2.Max( base.CalculateMinimumSize(), new Vector2( fontSize, fontSize ) );
		}

		return base.CalculateMinimumSize();

	}

	[HideInInspector]
	public override void Invalidate()
	{

		base.Invalidate();

		if( this.Font == null || !this.Font.IsValid || GetManager() == null )
			return;

		// We want to calculate the dfLabel's size *before* rendering or 
		// raising any public events.

		var sizeIsUninitialized = ( size.sqrMagnitude <= float.Epsilon );

		if( !autoSize && !autoHeight && !sizeIsUninitialized )
			return;

		if( string.IsNullOrEmpty( this.Text ) )
		{

			var lastSize = this.size;
			var newSize = lastSize;

			if( sizeIsUninitialized )
				newSize = new Vector2( 150, 24 );

			if( this.AutoSize || this.AutoHeight )
				newSize.y = Mathf.CeilToInt( Font.LineHeight * TextScale );

			if( lastSize != newSize )
			{
				this.SuspendLayout();
				this.Size = newSize;
				this.ResumeLayout();
			}

			return;

		}

		var previousSize = this.size;

		// TODO: There should be no need to do this when the text, font, and textscale have not changed
		using( var textRenderer = obtainRenderer() )
		{

			var renderSize = textRenderer.MeasureString( this.text ).RoundToInt();

			// NOTE: Assignment to private field 'size' rather than the 'Size' property
			// below is intentional. Not only do we not need the full host of actions
			// that would be caused by assigning to the property, but doing so actually
			// causes issues: http://daikonforge.com/issues/view.php?id=37

			// NOTE: The call to raiseSizeChangedEvent() was added to address a user-reported 
			// issue where an AutoSize or AutoHeight label that was contained in a ScrollPanel 
			// did not notify the parent ScrollPanel that it had to redo the layout and clipping 
			// operations. raiseSizeChangedEvent() performs that notification.

			if( AutoSize || sizeIsUninitialized )
			{
				this.size = renderSize + new Vector2( padding.horizontal, padding.vertical );
			}
			else if( AutoHeight )
			{
				this.size = new Vector2( size.x, renderSize.y + padding.vertical );
			}

		}

		if( ( this.size - previousSize ).sqrMagnitude >= 1f )
		{
			raiseSizeChangedEvent();
		}

	}

	private dfFontRendererBase obtainRenderer()
	{

		var sizeIsUninitialized = ( Size.sqrMagnitude <= float.Epsilon );

		var clientSize = this.Size - new Vector2( padding.horizontal, padding.vertical );

		var effectiveSize = ( this.autoSize || sizeIsUninitialized ) ? getAutoSizeDefault() : clientSize;
		if( autoHeight )
			effectiveSize = new Vector2( clientSize.x, int.MaxValue );

		var p2u = PixelsToUnits();
		var origin = ( pivot.TransformToUpperLeft( Size ) + new Vector3( padding.left, -padding.top ) ) * p2u;

		var effectiveTextScale = TextScale * getTextScaleMultiplier();

		var textRenderer = Font.ObtainRenderer();
		textRenderer.WordWrap = this.WordWrap;
		textRenderer.MaxSize = effectiveSize;
		textRenderer.PixelRatio = p2u;
		textRenderer.TextScale = effectiveTextScale;
		textRenderer.CharacterSpacing = CharacterSpacing;
		textRenderer.VectorOffset = origin.Quantize( p2u );
		textRenderer.MultiLine = true;
		textRenderer.TabSize = this.TabSize;
		textRenderer.TabStops = this.TabStops;
		textRenderer.TextAlign = autoSize ? TextAlignment.Left : this.TextAlignment;
		textRenderer.ColorizeSymbols = this.ColorizeSymbols;
		textRenderer.ProcessMarkup = this.ProcessMarkup;
		textRenderer.DefaultColor = IsEnabled ? this.Color : this.DisabledColor;
		textRenderer.BottomColor = enableGradient ? BottomColor : (Color32?)null;
		textRenderer.OverrideMarkupColors = !IsEnabled;
		textRenderer.Opacity = this.CalculateOpacity();
		textRenderer.Outline = this.Outline;
		textRenderer.OutlineSize = this.OutlineSize;
		textRenderer.OutlineColor = this.OutlineColor;
		textRenderer.Shadow = this.Shadow;
		textRenderer.ShadowColor = this.ShadowColor;
		textRenderer.ShadowOffset = this.ShadowOffset;

		var dynamicFontRenderer = textRenderer as dfDynamicFont.DynamicFontRenderer;
		if( dynamicFontRenderer != null )
		{
			dynamicFontRenderer.SpriteAtlas = this.Atlas;
			dynamicFontRenderer.SpriteBuffer = renderData;
		}

		if( this.vertAlign != dfVerticalAlignment.Top )
		{
			textRenderer.VectorOffset = getVertAlignOffset( textRenderer );
		}

		return textRenderer;

	}

	private float getTextScaleMultiplier()
	{

		if( textScaleMode == dfTextScaleMode.None || !Application.isPlaying )
			return 1f;

		// Return the difference between design resolution and current resolution
		if( textScaleMode == dfTextScaleMode.ScreenResolution )
		{
			return (float)Screen.height / (float)GetManager().FixedHeight;
		}

		// Cannot scale by control size if AutoSize is enabled
		if( autoSize )
		{
			return 1f;
		}

		return Size.y / startSize.y;

	}

	private Vector2 getAutoSizeDefault()
	{

		var maxWidth = this.maxSize.x > float.Epsilon ? this.maxSize.x : int.MaxValue;
		var maxHeight = this.maxSize.y > float.Epsilon ? this.maxSize.y : int.MaxValue;

		var maxSize = new Vector2( maxWidth, maxHeight );

		return maxSize;

	}

	private Vector3 getVertAlignOffset( dfFontRendererBase textRenderer )
	{

		var p2u = PixelsToUnits();
		var renderedSize = textRenderer.MeasureString( this.text ) * p2u;
		var origin = textRenderer.VectorOffset;
		var clientHeight = ( Height - padding.vertical ) * p2u;

		if( renderedSize.y >= clientHeight )
			return origin;

		switch( this.vertAlign )
		{
			case dfVerticalAlignment.Middle:
				origin.y -= ( clientHeight - renderedSize.y ) * 0.5f;
				break;
			case dfVerticalAlignment.Bottom:
				origin.y -= clientHeight - renderedSize.y;
				break;
		}

		return origin;

	}

	protected internal virtual void renderBackground()
	{

		if( Atlas == null )
			return;

		var spriteInfo = Atlas[ backgroundSprite ];
		if( spriteInfo == null )
		{
			return;
		}

		var color = ApplyOpacity( BackgroundColor );
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

	#region IDFMultiRender Members

	private dfRenderData textRenderData = null;
	private dfList<dfRenderData> renderDataBuffers = dfList<dfRenderData>.Obtain();

	public dfList<dfRenderData> RenderMultiple()
	{

		try
		{

#if UNITY_EDITOR
			//@Profiler.BeginSample( "Rendering " + GetType().Name );
#endif

			// If control is not dirty, update the transforms on the 
			// render buffers (in case control moved) and return 
			// pre-rendered data
			if( !isControlInvalidated && ( textRenderData != null || renderData != null ) )
			{

				//@Profiler.BeginSample( "Re-using existing render buffers" );

				var matrix = transform.localToWorldMatrix;

				for( int i = 0; i < renderDataBuffers.Count; i++ )
				{
					renderDataBuffers[ i ].Transform = matrix;
				}

				//@Profiler.EndSample();

				return renderDataBuffers;

			}

			if( Atlas == null || Font == null || !isVisible )
				return null;

			// Initialize render buffers if needed
			if( renderData == null )
			{

				renderData = dfRenderData.Obtain();
				textRenderData = dfRenderData.Obtain();

				isControlInvalidated = true;

			}

			// Clear the render buffers
			resetRenderBuffers();

			// Render the background sprite, if there is one
			renderBackground();

			if( string.IsNullOrEmpty( this.Text ) )
			{

				if( this.AutoSize || this.AutoHeight )
					Height = Mathf.CeilToInt( Font.LineHeight * TextScale );

				return renderDataBuffers;

			}

			var sizeIsUninitialized = ( size.sqrMagnitude <= float.Epsilon );

			using( var textRenderer = obtainRenderer() )
			{

				textRenderer.Render( text, textRenderData );

				if( AutoSize || sizeIsUninitialized )
				{
					Size = ( textRenderer.RenderedSize + new Vector2( padding.horizontal, padding.vertical ) ).CeilToInt();
				}
				else if( AutoHeight )
				{
					Size = new Vector2( size.x, textRenderer.RenderedSize.y + padding.vertical ).CeilToInt();
				}

			}

			// Make sure that the collider size always matches the control
			updateCollider();

			return renderDataBuffers;

		}
		finally
		{

			this.isControlInvalidated = false;

#if UNITY_EDITOR
			//@Profiler.EndSample();
#endif

		}

	}

	private void resetRenderBuffers()
	{

		renderDataBuffers.Clear();

		var matrix = transform.localToWorldMatrix;

		if( renderData != null )
		{

			renderData.Clear();
			renderData.Material = Atlas.Material;
			renderData.Transform = matrix;

			renderDataBuffers.Add( renderData );

		}

		if( textRenderData != null )
		{

			textRenderData.Clear();
			textRenderData.Material = Atlas.Material;
			textRenderData.Transform = matrix;

			renderDataBuffers.Add( textRenderData );

		}

	}

	#endregion

	#region Dynamic font management 

	private void bindTextureRebuildCallback()
	{

		if( isFontCallbackAssigned || Font == null )
			return;

		if( Font is dfDynamicFont )
		{

			Font font = ( Font as dfDynamicFont ).BaseFont;
			font.textureRebuildCallback = (UnityEngine.Font.FontTextureRebuildCallback)Delegate.Combine( font.textureRebuildCallback, (Font.FontTextureRebuildCallback)this.onFontTextureRebuilt );

			isFontCallbackAssigned = true;

		}

	}

	private void unbindTextureRebuildCallback()
	{

		if( !isFontCallbackAssigned || Font == null )
			return;

		if( Font is dfDynamicFont )
		{

			Font font = ( Font as dfDynamicFont ).BaseFont;
			font.textureRebuildCallback = (UnityEngine.Font.FontTextureRebuildCallback)Delegate.Remove( font.textureRebuildCallback, (UnityEngine.Font.FontTextureRebuildCallback)this.onFontTextureRebuilt );
		}

		isFontCallbackAssigned = false;

	}

	private void requestCharacterInfo()
	{

		var dynamicFont = this.Font as dfDynamicFont;
		if( dynamicFont == null )
			return;

		if( !dfFontManager.IsDirty( this.Font ) )
			return;

		if( string.IsNullOrEmpty( this.text ) )
			return;

		var effectiveTextScale = TextScale * getTextScaleMultiplier();
		var effectiveFontSize = Mathf.CeilToInt( this.font.FontSize * effectiveTextScale );

		dynamicFont.AddCharacterRequest( this.text, effectiveFontSize, FontStyle.Normal );

	}

	private void onFontTextureRebuilt()
	{
		requestCharacterInfo();
		Invalidate();
	}

	public void UpdateFontInfo()
	{
		requestCharacterInfo();
	}

	#endregion

}
