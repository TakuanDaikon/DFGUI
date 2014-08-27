/* Copyright 2013-2014 Daikon Forge */

using System;
using UnityEngine;

/// <summary>
/// Provides a basic Button implementation that allows the developer
/// to specify individual sprite images to be used to represent common 
/// button states.
/// </summary>
[Serializable]
[ExecuteInEditMode]
[dfCategory( "Basic Controls" )]
[dfTooltip( "Provides a basic Button implementation that allows the developer to specify individual sprite images to be used to represent common button states." )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_button.html" )]
[AddComponentMenu( "Daikon Forge/User Interface/Button" )]
public class dfButton : dfInteractiveBase, IDFMultiRender, IRendersText
{

	#region Public enums

	/// <summary>
	/// Represents the state of a Button
	/// </summary>
	public enum ButtonState : int
	{
		/// <summary>
		/// The default state of the button
		/// </summary>
		Default,
		/// <summary>
		/// Indicates that the button is current, i.e. has input focus or is 
		/// the selected button in a group
		/// </summary>
		Focus,
		/// <summary>
		/// Indicates that the mouse is hovering over the control
		/// </summary>
		Hover,
		/// <summary>
		/// Indicates that the user has pressed the button
		/// </summary>
		Pressed,
		/// <summary>
		/// Indicates that the control is disabled and cannot respond to
		/// user events
		/// </summary>
		Disabled
	}

	#endregion

	#region Public events

	/// <summary>
	/// Raised whenever the button's State property changes
	/// </summary>
	public event PropertyChangedEventHandler<ButtonState> ButtonStateChanged;

	#endregion

	#region Protected serialized members

	[SerializeField]
	protected dfFontBase font;

	[SerializeField]
	protected string pressedSprite;

	[SerializeField]
	protected ButtonState state;

	[SerializeField]
	protected dfControl group = null;

	[SerializeField]
	protected string text = "";

	[SerializeField]
	protected TextAlignment textAlign = TextAlignment.Center;

	[SerializeField]
	protected dfVerticalAlignment vertAlign = dfVerticalAlignment.Middle;

	[SerializeField]
	protected Color32 normalColor = UnityEngine.Color.white;

	[SerializeField]
	protected Color32 textColor = UnityEngine.Color.white;

	[SerializeField]
	protected Color32 hoverText = UnityEngine.Color.white;

	[SerializeField]
	protected Color32 pressedText = UnityEngine.Color.white;

	[SerializeField]
	protected Color32 focusText = UnityEngine.Color.white;

	[SerializeField]
	protected Color32 disabledText = UnityEngine.Color.white;

	[SerializeField]
	protected Color32 hoverColor = UnityEngine.Color.white;

	[SerializeField]
	protected Color32 pressedColor = UnityEngine.Color.white;

	[SerializeField]
	protected Color32 focusColor = UnityEngine.Color.white;

	[SerializeField]
	protected float textScale = 1f;

	[SerializeField]
	protected dfTextScaleMode textScaleMode = dfTextScaleMode.None;

	[SerializeField]
	protected bool wordWrap = false;

	[SerializeField]
	protected RectOffset padding = new RectOffset();

	[SerializeField]
	protected bool textShadow = false;

	[SerializeField]
	protected Color32 shadowColor = UnityEngine.Color.black;

	[SerializeField]
	protected Vector2 shadowOffset = new Vector2( 1, -1 );

	[SerializeField]
	protected bool autoSize = false;

	[SerializeField]
	protected bool clickWhenSpacePressed = true;

	#endregion

	#region Public properties

	/// <summary>
	/// Gets or sets whether a Click event will be generated when this control
	/// has input focus and the Spacebar key is pressed
	/// </summary>
	public bool ClickWhenSpacePressed
	{
		get { return this.clickWhenSpacePressed; }
		set { this.clickWhenSpacePressed = value; }
	}

	/// <summary>
	/// Gets or sets the button's current ButtonState value
	/// </summary>
	public ButtonState State
	{
		get { return this.state; }
		set
		{
			if( value != this.state )
			{
				OnButtonStateChanged( value );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the name of the sprite that will be displayed when 
	/// the button date is set to <see cref="ButtonState.Pressed"/>
	/// </summary>
	public string PressedSprite
	{
		get { return pressedSprite; }
		set
		{
			if( value != pressedSprite )
			{
				pressedSprite = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// If set, only one Button attached to the indicated dfControl
	/// can have its State property set to <see cref="ButtonState.Pressed"/> 
	/// at a time. This is used to emulate Toolbar and TabStrip 
	/// functionality.
	/// </summary>
	public dfControl ButtonGroup
	{
		get { return this.group; }
		set
		{
			if( value != group )
			{
				group = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// If set to TRUE, the <see cref="dfButton"/> will be automatically sized to 
	/// fit the <see cref="Text"/>
	/// </summary>
	public bool AutoSize
	{
		get { return this.autoSize; }
		set
		{
			if( value != this.autoSize )
			{
				this.autoSize = value;
				if( value )
					this.textAlign = TextAlignment.Left;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the text alignment that will be used to render the 
	/// button's caption: Left, Right, or Centered
	/// </summary>
	public TextAlignment TextAlignment
	{
		get
		{
			if( this.autoSize )
				return TextAlignment.Left;
			return this.textAlign;
		}
		set
		{
			if( value != textAlign )
			{
				textAlign = value;
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
	/// Gets or sets the amount of padding that will be used when rendering 
	/// the button's caption when the <see cref="AutoSize"/> property is set
	/// to TRUE.
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
	/// Gets or sets the <see cref="dfFont"/> instance that will be used 
	/// to render the button's caption
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
			}
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the button's caption text
	/// </summary>
	public string Text
	{
		get { return this.text; }
		set
		{
			if( value != this.text )
			{
				dfFontManager.Invalidate( this.Font );
				this.localizationKey = value;
				this.text = getLocalizedValue( value );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the Color that will be used when rendering the 
	/// button's caption in its Normal state
	/// </summary>
	public Color32 TextColor
	{
		get { return this.textColor; }
		set
		{
			this.textColor = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the Color that will be used when rendering the 
	/// button's caption when in the Hover state
	/// </summary>
	public Color32 HoverTextColor
	{
		get { return this.hoverText; }
		set
		{
			this.hoverText = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the color that will be used to render the button's
	/// background sprite in the Default state
	/// </summary>
	public Color32 NormalBackgroundColor
	{
		get { return this.normalColor; }
		set
		{
			this.normalColor = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the Color that will be used when rendering the 
	/// button's sprite when in the Hover state
	/// </summary>
	public Color32 HoverBackgroundColor
	{
		get { return this.hoverColor; }
		set
		{
			this.hoverColor = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the Color that will be used when rendering the 
	/// button's caption in its Pressed state
	/// </summary>
	public Color32 PressedTextColor
	{
		get { return this.pressedText; }
		set
		{
			this.pressedText = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the Color that will be used when rendering the 
	/// button's sprite when in the Pressed state
	/// </summary>
	public Color32 PressedBackgroundColor
	{
		get { return this.pressedColor; }
		set
		{
			this.pressedColor = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the Color that will be used when rendering the 
	/// button's caption in its Focused state
	/// </summary>
	public Color32 FocusTextColor
	{
		get { return this.focusText; }
		set
		{
			this.focusText = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the Color that will be used when rendering the 
	/// button's sprite when in the Focus state
	/// </summary>
	public Color32 FocusBackgroundColor
	{
		get { return this.focusColor; }
		set
		{
			this.focusColor = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the Color that will be used when rendering the 
	/// button's caption in its Disabled state
	/// </summary>
	public Color32 DisabledTextColor
	{
		get { return this.disabledText; }
		set
		{
			this.disabledText = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the size multiplier that will be used when rendering
	/// the button's caption
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
	/// Gets or sets whether the button's caption will be word-wrapped
	/// when too long to fit as a single line of text
	/// </summary>
	public bool WordWrap
	{
		get { return this.wordWrap; }
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
	/// Gets or sets whether the button's caption will be rendered with 
	/// a shadow
	/// </summary>
	public bool Shadow
	{
		get { return this.textShadow; }
		set
		{
			if( value != textShadow )
			{
				textShadow = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the color that will be used to render the caption's shadow
	/// if the <see cref="Shadow"/> property is set to TRUE
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
	/// Gets or sets the distance in pixels that the caption's shadow will 
	/// be offset if the <see cref="Shadow"/> property is set to TRUE
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

	#endregion

	#region Private runtime variables

	private Vector2 startSize = Vector2.zero;
	private bool isFontCallbackAssigned = false;

	#endregion

	#region Overrides and event handling

	protected internal override void OnLocalize()
	{
		base.OnLocalize();
		this.Text = getLocalizedValue( this.localizationKey ?? this.text );
	}

	[HideInInspector]
	public override void Invalidate()
	{

		base.Invalidate();

		if( this.AutoSize )
		{
			this.autoSizeToText();
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

		#region Ensure that this control always has a valid font, if possible

		var validFont =
			Font != null &&
			Font.IsValid;

		if( Application.isPlaying && !validFont )
		{
			Font = GetManager().DefaultFont;
		}

		#endregion

		bindTextureRebuildCallback();

	}

	public override void OnDisable()
	{
		base.OnDisable();
		unbindTextureRebuildCallback();
	}

	public override void Awake()
	{
		base.Awake();
		startSize = Size;
	}

	protected internal override void OnEnterFocus( dfFocusEventArgs args )
	{

		if( this.State != ButtonState.Pressed )
		{
			this.State = ButtonState.Focus;
		}

		base.OnEnterFocus( args );

	}

	protected internal override void OnLeaveFocus( dfFocusEventArgs args )
	{
		this.State = ButtonState.Default;
		base.OnLeaveFocus( args );
	}

	protected internal override void OnKeyPress( dfKeyEventArgs args )
	{

		if( this.ClickWhenSpacePressed && this.IsInteractive && args.KeyCode == KeyCode.Space )
		{
			OnClick( new dfMouseEventArgs( this, dfMouseButtons.Left, 1, new Ray(), Vector2.zero, 0 ) );
			return;
		}

		base.OnKeyPress( args );

	}

	protected internal override void OnClick( dfMouseEventArgs args )
	{

		if( group != null )
		{

			var list = transform.parent.GetComponentsInChildren<dfButton>();
			for( int i = 0; i < list.Length; i++ )
			{
				var control = list[ i ];
				if( control != this && control.ButtonGroup == this.ButtonGroup )
				{
					if( control != this )
					{
						control.State = ButtonState.Default;
					}
				}
			}

			// Need to manually signal the Group object if it's not part 
			// of this dfControl's hierarchy
			if( !transform.IsChildOf( group.transform ) )
			{
				Signal( group.gameObject, "OnClick", args );
			}

		}

		base.OnClick( args );

	}

	protected internal override void OnMouseDown( dfMouseEventArgs args )
	{

		// Active tabs do not display pressed state
		if( !( this.parent is dfTabstrip ) || this.State != ButtonState.Focus )
		{
			this.State = ButtonState.Pressed;
		}

		base.OnMouseDown( args );

	}

	protected internal override void OnMouseUp( dfMouseEventArgs args )
	{

		if( !IsEnabled )
		{
			State = ButtonState.Disabled;
			base.OnMouseUp( args );
			return;
		}

#if !UNITY_IPHONE && !UNITY_ANDROID
		if( isMouseHovering )
		{

			// Active tabs do not display hover state
			if( this.parent is dfTabstrip && this.ContainsFocus )
			{
				this.State = ButtonState.Focus;
			}
			else
			{
				this.State = ButtonState.Hover;
			}

		}
		else
#endif
			if( HasFocus )
			{
				this.State = ButtonState.Focus;
			}
			else
			{
				this.State = ButtonState.Default;
			}

		base.OnMouseUp( args );

	}

	protected internal override void OnMouseEnter( dfMouseEventArgs args )
	{

#if !UNITY_IPHONE && !UNITY_ANDROID

		// Active tabs do not display hover state
		if( !( this.parent is dfTabstrip ) || this.State != ButtonState.Focus )
		{
			this.State = ButtonState.Hover;
		}

#endif
		base.OnMouseEnter( args );

	}

	protected internal override void OnMouseLeave( dfMouseEventArgs args )
	{

		if( this.ContainsFocus )
			this.State = ButtonState.Focus;
		else
			this.State = ButtonState.Default;

		base.OnMouseLeave( args );

	}

	protected internal override void OnIsEnabledChanged()
	{

		if( !this.IsEnabled )
		{
			this.State = ButtonState.Disabled;
		}
		else
		{
			this.State = ButtonState.Default;
		}

		base.OnIsEnabledChanged();

	}

	protected virtual void OnButtonStateChanged( ButtonState value )
	{

		// Cannot change button state when button is disabled
		if( value != ButtonState.Disabled && !this.IsEnabled )
		{
			value = ButtonState.Disabled;
		}

		this.state = value;

		Signal( "OnButtonStateChanged", this, value );

		if( ButtonStateChanged != null )
		{
			ButtonStateChanged( this, value );
		}

		Invalidate();

	}

	//protected override void OnRebuildRenderData()
	//{

	//    if( Atlas == null )
	//        return;

	//    renderData.Material = Atlas.Material;

	//    renderBackground();
	//    renderText();

	//}

	#endregion

	#region Private utility methods

	protected override Color32 getActiveColor()
	{
		switch( this.State )
		{
			case ButtonState.Focus:
				return this.FocusBackgroundColor;
			case ButtonState.Hover:
				return this.HoverBackgroundColor;
			case ButtonState.Pressed:
				return this.PressedBackgroundColor;
			case ButtonState.Disabled:
				return this.DisabledColor;
			default:
				return this.NormalBackgroundColor;
		}
	}

	private void autoSizeToText()
	{

		if( Font == null || !Font.IsValid || string.IsNullOrEmpty( Text ) )
			return;

		using( var textRenderer = obtainTextRenderer() )
		{

			var textSize = textRenderer.MeasureString( this.Text );
			var newSize = new Vector2( textSize.x + padding.horizontal, textSize.y + padding.vertical );

			if( this.size != newSize )
			{
				this.SuspendLayout();
				this.Size = newSize;
				this.ResumeLayout();
			}

		}

	}

	private dfRenderData renderText()
	{

		if( Font == null || !Font.IsValid || string.IsNullOrEmpty( Text ) )
			return null;

		var buffer = renderData;
		if( font is dfDynamicFont )
		{

			var dynamicFont = (dfDynamicFont)font;

			buffer = textRenderData;
			buffer.Clear();
			buffer.Material = dynamicFont.Material;

		}

		using( var textRenderer = obtainTextRenderer() )
		{
			textRenderer.Render( text, buffer );
		}

		return buffer;

	}

	private dfFontRendererBase obtainTextRenderer()
	{

		var clientSize = this.Size - new Vector2( padding.horizontal, padding.vertical );

		var effectiveSize = this.autoSize ? Vector2.one * int.MaxValue : clientSize;

		var p2u = PixelsToUnits();
		var origin = ( pivot.TransformToUpperLeft( Size ) + new Vector3( padding.left, -padding.top ) ) * p2u;

		var effectiveTextScale = TextScale * getTextScaleMultiplier();
		var renderColor = ApplyOpacity( getTextColorForState() );

		var textRenderer = Font.ObtainRenderer();
		textRenderer.WordWrap = this.WordWrap;
		textRenderer.MultiLine = this.WordWrap;
		textRenderer.MaxSize = effectiveSize;
		textRenderer.PixelRatio = p2u;
		textRenderer.TextScale = effectiveTextScale;
		textRenderer.CharacterSpacing = 0;
		textRenderer.VectorOffset = origin.Quantize( p2u );
		textRenderer.TabSize = 0;
		textRenderer.TextAlign = autoSize ? TextAlignment.Left : this.TextAlignment;
		textRenderer.ProcessMarkup = true;
		textRenderer.DefaultColor = renderColor;
		textRenderer.OverrideMarkupColors = false;
		textRenderer.Opacity = this.CalculateOpacity();
		textRenderer.Shadow = Shadow;
		textRenderer.ShadowColor = ShadowColor;
		textRenderer.ShadowOffset = ShadowOffset;

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
			return (float)Screen.height / (float)cachedManager.FixedHeight;
		}

		// Cannot scale by control size if AutoSize is enabled
		if( autoSize )
		{
			return 1f;
		}

		return Size.y / startSize.y;

	}

	private Color32 getTextColorForState()
	{

		if( !IsEnabled )
			return this.DisabledTextColor;

		switch( this.state )
		{
			case ButtonState.Default:
				return this.TextColor;
			case ButtonState.Focus:
				return this.FocusTextColor;
			case ButtonState.Hover:
				return this.HoverTextColor;
			case ButtonState.Pressed:
				return this.PressedTextColor;
			case ButtonState.Disabled:
				return this.DisabledTextColor;
		}

		return UnityEngine.Color.white;

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

	protected internal override dfAtlas.ItemInfo getBackgroundSprite()
	{

		if( Atlas == null )
			return null;

		var result = (dfAtlas.ItemInfo)null;

		switch( this.state )
		{

			case ButtonState.Default:
				result = atlas[ backgroundSprite ];
				break;

			case ButtonState.Focus:
				result = atlas[ focusSprite ];
				break;

			case ButtonState.Hover:
				result = atlas[ hoverSprite ];
				break;

			case ButtonState.Pressed:
				result = atlas[ pressedSprite ];
				break;

			case ButtonState.Disabled:
				result = atlas[ disabledSprite ];
				break;

		}

		// TODO: Implement some sort of logic-based "fallback" logic when indicated sprite is not available?

		if( result == null )
			result = atlas[ backgroundSprite ];

		return result;

	}

	#endregion

	#region IDFMultiRender Members

	private dfRenderData textRenderData = null;
	private dfList<dfRenderData> buffers = dfList<dfRenderData>.Obtain();

	public dfList<dfRenderData> RenderMultiple()
	{

		if( renderData == null )
		{
			renderData = dfRenderData.Obtain();
			textRenderData = dfRenderData.Obtain();
			isControlInvalidated = true;
		}

		var matrix = transform.localToWorldMatrix;

		if( !isControlInvalidated )
		{

			for( int i = 0; i < buffers.Count; i++ )
			{
				buffers[ i ].Transform = matrix;
			}

			return buffers;

		}

		isControlInvalidated = false;

		buffers.Clear();
		renderData.Clear();

		if( Atlas != null )
		{

			renderData.Material = Atlas.Material;
			renderData.Transform = matrix;

			renderBackground();
			buffers.Add( renderData );

		}

		var textBuffer = renderText();
		if( textBuffer != null && textBuffer != renderData )
		{
			textBuffer.Transform = matrix;
			buffers.Add( textBuffer );
		}

		// Make sure that the collider size always matches the control
		updateCollider();

		return buffers;

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
