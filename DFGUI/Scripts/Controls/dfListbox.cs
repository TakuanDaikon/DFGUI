/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Allows the user to select from a list of options
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Allows the user to select from a list of options" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_listbox.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Listbox" )]
public class dfListbox : dfInteractiveBase, IDFMultiRender, IRendersText
{

	#region Public events

	/// <summary>
	/// Raised whenever the SelectedIndex property's value is changed
	/// </summary>
	public event PropertyChangedEventHandler<int> SelectedIndexChanged;

	/// <summary>
	/// Raised whenever the user clicks with the left mouse button on 
	/// any item in the list
	/// </summary>
	public event PropertyChangedEventHandler<int> ItemClicked;

	#endregion

	#region Serialized protected members

	[SerializeField]
	protected dfFontBase font;

	[SerializeField]
	protected RectOffset listPadding = new RectOffset();

	[SerializeField]
	protected int selectedIndex = -1;

	[SerializeField]
	protected Color32 itemTextColor = UnityEngine.Color.white;

	[SerializeField]
	protected float itemTextScale = 1f;

	[SerializeField]
	protected int itemHeight = 25;

	[SerializeField]
	protected RectOffset itemPadding = new RectOffset();

	[SerializeField]
	protected string[] items = new string[] { };

	[SerializeField]
	protected string itemHighlight = "";

	[SerializeField]
	protected string itemHover = "";

	[SerializeField]
	protected dfScrollbar scrollbar;

	[SerializeField]
	protected bool animateHover = false;

	[SerializeField]
	protected bool shadow = false;

	[SerializeField]
	protected dfTextScaleMode textScaleMode = dfTextScaleMode.None;

	[SerializeField]
	protected Color32 shadowColor = UnityEngine.Color.black;

	[SerializeField]
	protected Vector2 shadowOffset = new Vector2( 1, -1 );

	[SerializeField]
	protected TextAlignment itemAlignment = TextAlignment.Left;

	#endregion

	#region Private non-serialized variables

	private bool isFontCallbackAssigned = false;
	private bool eventsAttached = false;
	private float scrollPosition = 0f;
	private int hoverIndex = -1;
	private float hoverTweenLocation = 0f;
	private Vector2 touchStartPosition = Vector2.zero;
	private Vector2 startSize = Vector2.zero;

	#endregion

	#region Public properties

	/// <summary>
	/// Gets or sets the <see cref="dfFont"/> instance that will be used 
	/// to render list items
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
	/// Gets or sets the scroll position of the list
	/// </summary>
	public float ScrollPosition
	{
		get { return this.scrollPosition; }
		set
		{
			if( !Mathf.Approximately( value, scrollPosition ) )
			{
				scrollPosition = constrainScrollPosition( value );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the alignment method that will be used when 
	/// rendering item text
	/// </summary>
	public TextAlignment ItemAlignment
	{
		get { return this.itemAlignment; }
		set
		{
			if( value != itemAlignment )
			{
				this.itemAlignment = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the name of the sprite that will be used to render
	/// the background of the selected list item
	/// </summary>
	public string ItemHighlight
	{
		get { return this.itemHighlight; }
		set
		{
			if( value != this.itemHighlight )
			{
				this.itemHighlight = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the name of the sprite that will be used to render 
	/// the background of the list item under the mouse
	/// </summary>
	public string ItemHover
	{
		get { return this.itemHover; }
		set
		{
			if( value != this.itemHover )
			{
				this.itemHover = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Returns the actual text of the currently selected item. Returns
	/// NULL if no item is currently selected.
	/// </summary>
	public string SelectedItem
	{
		get
		{

			if( selectedIndex == -1 )
				return null;

			return items[ selectedIndex ];

		}
	}

	/// <summary>
	/// Gets or sets the value of the currently selected list item
	/// </summary>
	public string SelectedValue
	{
		get { return this.items[ this.selectedIndex ]; }
		set
		{
			this.selectedIndex = -1;
			for( int i = 0; i < this.items.Length; i++ )
			{
				if( items[ i ] == value )
				{
					this.selectedIndex = i;
					break;
				}
			}
		}
	}

	/// <summary>
	/// Gets or sets the index of the currently selected list item
	/// </summary>
	public int SelectedIndex
	{
		get { return this.selectedIndex; }
		set
		{
			value = Mathf.Max( -1, value );
			value = Mathf.Min( items.Length - 1, value );
			if( value != this.selectedIndex )
			{
				this.selectedIndex = value;
				EnsureVisible( value );
				OnSelectedIndexChanged();
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding that will be applied when 
	/// rendering each list item
	/// </summary>
	public RectOffset ItemPadding
	{
		get
		{
			if( itemPadding == null )
				itemPadding = new RectOffset();
			return this.itemPadding;
		}
		set
		{
			value = value.ConstrainPadding();
			if( !value.Equals( this.itemPadding ) )
			{
				this.itemPadding = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the color that will be used to render each list item
	/// </summary>
	public Color32 ItemTextColor
	{
		get { return this.itemTextColor; }
		set
		{
			if( !value.Equals( this.itemTextColor ) )
			{
				this.itemTextColor = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the size multiplier that will be used when rendering 
	/// each list item
	/// </summary>
	public float ItemTextScale
	{
		get { return this.itemTextScale; }
		set
		{
			value = Mathf.Max( 0.1f, value );
			if( !Mathf.Approximately( itemTextScale, value ) )
			{
				this.itemTextScale = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the height (in pixels) of each list item
	/// </summary>
	public int ItemHeight
	{
		get { return this.itemHeight; }
		set
		{
			scrollPosition = 0;
			value = Mathf.Max( 1, value );
			if( value != this.itemHeight )
			{
				this.itemHeight = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the collection of string values that will be displayed in the list
	/// </summary>
	public string[] Items
	{
		get
		{
			if( items == null )
			{
				items = new string[] { };
			}
			return items;
		}
		set
		{
			if( value != items )
			{
				scrollPosition = 0;
				if( value == null )
				{
					value = new string[] { };
				}
				items = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the scrollbar that will be used to scroll the list
	/// </summary>
	public dfScrollbar Scrollbar
	{
		get { return this.scrollbar; }
		set
		{
			scrollPosition = 0;
			if( value != this.scrollbar )
			{
				detachScrollbarEvents();
				this.scrollbar = value;
				attachScrollbarEvents();
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding that will be applied to the control's 
	/// borders when rendering the collection of list items
	/// </summary>
	public RectOffset ListPadding
	{
		get
		{
			if( listPadding == null )
				listPadding = new RectOffset();
			return this.listPadding;
		}
		set
		{
			value = value.ConstrainPadding();
			if( !RectOffset.Equals( value, this.listPadding ) )
			{
				this.listPadding = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether a shadow will be rendered for each list item
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
	/// Gets or sets the color of the shadow that will be rendered for each 
	/// list item if the <see cref="Shadow"/> property is set to TRUE
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
	/// Gets or sets the distance that the shadow will be offset for each list
	/// item if the <see cref="Shadow"/> property is set to TRUE
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
	/// Gets or sets whether the mouse hover indicator will be animated
	/// </summary>
	public bool AnimateHover
	{
		get { return this.animateHover; }
		set { this.animateHover = value; }
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

	#endregion

	#region Base class overrides

	public override void Awake()
	{
		base.Awake();
		startSize = this.Size;
	}

	public override void Update()
	{

		base.Update();

		if( size.magnitude == 0 )
		{
			size = new Vector2( 200, 150 );
		}

		if( animateHover && hoverIndex != -1 )
		{
			var hoverTargetPos = ( hoverIndex * itemHeight ) * PixelsToUnits();
			if( Mathf.Abs( hoverTweenLocation - hoverTargetPos ) < 1 )
			{
				Invalidate();
			}
		}

		if( isControlInvalidated )
		{
			synchronizeScrollbar();
		}

	}

	public override void LateUpdate()
	{

		base.LateUpdate();

		if( !Application.isPlaying )
			return;

		// HACK: Need to perform initialization after all dependant objects 
		attachScrollbarEvents();

	}

	public override void OnEnable()
	{
		base.OnEnable();
		bindTextureRebuildCallback();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		detachScrollbarEvents();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		unbindTextureRebuildCallback();
		detachScrollbarEvents();
	}

	protected internal override void OnLocalize()
	{

		base.OnLocalize();

		var valuesChanged = false;

		for( int i = 0; i < items.Length; i++ )
		{
			var localizedValue = getLocalizedValue( items[ i ] );
			if( localizedValue != items[ i ] )
			{
				valuesChanged = true;
				items[ i ] = localizedValue;
			}
		}

		if( valuesChanged )
		{
			Invalidate();
		}

	}

	protected internal virtual void OnSelectedIndexChanged()
	{

		SignalHierarchy( "OnSelectedIndexChanged", this, this.selectedIndex );

		if( SelectedIndexChanged != null )
		{
			SelectedIndexChanged( this, this.selectedIndex );
		}

	}

	protected internal virtual void OnItemClicked()
	{

		Signal( "OnItemClicked", this, this.selectedIndex );

		if( ItemClicked != null )
		{
			ItemClicked( this, this.selectedIndex );
		}

	}

	protected internal override void OnMouseMove( dfMouseEventArgs args )
	{

		base.OnMouseMove( args );

		if( args is dfTouchEventArgs )
		{

			if( Mathf.Abs( args.Position.y - touchStartPosition.y ) < itemHeight / 2 )
				return;

			ScrollPosition = Mathf.Max( 0, ScrollPosition + args.MoveDelta.y );
			synchronizeScrollbar();

			this.hoverIndex = -1;

			return;

		}

		updateItemHover( args );

	}

	protected internal override void OnMouseEnter( dfMouseEventArgs args )
	{
		base.OnMouseEnter( args );
		touchStartPosition = args.Position;
	}

	protected internal override void OnMouseLeave( dfMouseEventArgs args )
	{
		base.OnMouseLeave( args );
		hoverIndex = -1;
	}

	protected internal override void OnMouseWheel( dfMouseEventArgs args )
	{

		base.OnMouseWheel( args );

		ScrollPosition = Mathf.Max( 0, ScrollPosition - (int)args.WheelDelta * ItemHeight );
		synchronizeScrollbar();

		updateItemHover( args );

	}

	protected internal override void OnMouseUp( dfMouseEventArgs args )
	{

		hoverIndex = -1;
		base.OnMouseUp( args );

		if( args is dfTouchEventArgs )
		{
			if( Mathf.Abs( args.Position.y - touchStartPosition.y ) < itemHeight )
			{
				selectItemUnderMouse( args );
			}
		}

	}

	protected internal override void OnMouseDown( dfMouseEventArgs args )
	{

		base.OnMouseDown( args );

		if( args is dfTouchEventArgs )
		{
			touchStartPosition = args.Position;
			return;
		}

		selectItemUnderMouse( args );

	}

	protected internal override void OnKeyDown( dfKeyEventArgs args )
	{

		switch( args.KeyCode )
		{
			case KeyCode.PageDown:
				SelectedIndex += Mathf.FloorToInt( ( size.y - listPadding.vertical ) / itemHeight );
				break;
			case KeyCode.PageUp:
				var newIndex = SelectedIndex - Mathf.FloorToInt( ( size.y - listPadding.vertical ) / itemHeight );
				SelectedIndex = Mathf.Max( 0, newIndex );
				break;
			case KeyCode.UpArrow:
				SelectedIndex = Mathf.Max( 0, selectedIndex - 1 );
				break;
			case KeyCode.DownArrow:
				SelectedIndex += 1;
				break;
			case KeyCode.Home:
				SelectedIndex = 0;
				break;
			case KeyCode.End:
				SelectedIndex = items.Length;
				break;
		}

		base.OnKeyDown( args );

	}

	#endregion

	#region Public methods

	/// <summary>
	/// Adds a new value to the collection of list items
	/// </summary>
	/// <param name="item"></param>
	public void AddItem( string item )
	{

		var newList = new string[ items.Length + 1 ];

		Array.Copy( items, newList, items.Length );
		newList[ items.Length ] = item;

		items = newList;

		this.Invalidate();

	}

	/// <summary>
	/// Ensures that the list item at the specified index will be 
	/// visible to the user
	/// </summary>
	/// <param name="index">The index of the list item to make visible</param>
	public void EnsureVisible( int index )
	{

		var itemTop = index * ItemHeight;
		if( scrollPosition > itemTop )
			ScrollPosition = itemTop;

		var clientHeight = size.y - listPadding.vertical;
		if( scrollPosition + clientHeight < itemTop + itemHeight )
			ScrollPosition = itemTop - clientHeight + itemHeight;

	}

	#endregion

	#region Private utility methods

	private void selectItemUnderMouse( dfMouseEventArgs args )
	{

		var pivotOffset = pivot.TransformToUpperLeft( Size );
		var top = pivotOffset.y + ( -itemHeight * ( selectedIndex - scrollPosition ) - listPadding.top );
		var needed = ( selectedIndex - scrollPosition + 1 ) * itemHeight + listPadding.vertical;
		var overlap = needed - size.y;
		if( overlap > 0 )
			top += overlap;

		var mousepos = GetHitPosition( args ).y - listPadding.top;
		if( mousepos < 0 || mousepos > size.y - listPadding.bottom )
			return;

		this.SelectedIndex = (int)( ( scrollPosition + mousepos ) / itemHeight );

		OnItemClicked();

	}

	private void renderHover()
	{

		if( !Application.isPlaying )
			return;

		var hoverDisabled =
			Atlas == null ||
			!this.IsEnabled ||
			hoverIndex < 0 ||
			hoverIndex > items.Length - 1 ||
			string.IsNullOrEmpty( ItemHover );

		if( hoverDisabled )
			return;

		var spriteInfo = Atlas[ ItemHover ];
		if( spriteInfo == null )
		{
			return;
		}

		var pivotOffset = pivot.TransformToUpperLeft( Size );
		var offset = new Vector3(
			pivotOffset.x + listPadding.left,
			pivotOffset.y - listPadding.top + scrollPosition,
			0
		);

		var pixelSize = PixelsToUnits();

		var hoverTargetPos = ( hoverIndex * itemHeight );
		if( animateHover )
		{

			var tweenDistance = Mathf.Abs( hoverTweenLocation - hoverTargetPos );
			float maxDistance = ( size.y - listPadding.vertical ) * 0.5f;
			if( tweenDistance > maxDistance )
			{
				hoverTweenLocation = hoverTargetPos + Mathf.Sign( hoverTweenLocation - hoverTargetPos ) * maxDistance;
				tweenDistance = maxDistance;
			}

			var speed = Time.deltaTime / pixelSize * 2f;
			hoverTweenLocation = Mathf.MoveTowards( hoverTweenLocation, hoverTargetPos, speed );

		}
		else
		{
			hoverTweenLocation = hoverTargetPos;
		}

		offset.y -= hoverTweenLocation.Quantize( pixelSize );

		var color = ApplyOpacity( this.color );
		var options = new dfSprite.RenderOptions()
		{
			atlas = atlas,
			color = color,
			fillAmount = 1,
			flip = dfSpriteFlip.None,
			pixelsToUnits = PixelsToUnits(),
			size = new Vector3( this.size.x - listPadding.horizontal, itemHeight ),
			spriteInfo = spriteInfo,
			offset = offset
		};

		if( spriteInfo.border.horizontal > 0 || spriteInfo.border.vertical > 0 )
			dfSlicedSprite.renderSprite( renderData, options );
		else
			dfSprite.renderSprite( renderData, options );

		if( hoverTargetPos != hoverTweenLocation )
		{
			Invalidate();
		}

	}

	private void renderSelection()
	{

		if( Atlas == null || selectedIndex < 0 )
			return;

		var spriteInfo = Atlas[ ItemHighlight ];
		if( spriteInfo == null )
		{
			return;
		}

		var p2u = PixelsToUnits();

		var pivotOffset = pivot.TransformToUpperLeft( Size );
		var offset = new Vector3(
			pivotOffset.x + listPadding.left,
			pivotOffset.y - listPadding.top + scrollPosition,
			0
		);

		offset.y -= ( selectedIndex * itemHeight );

		var color = ApplyOpacity( this.color );
		var options = new dfSprite.RenderOptions()
		{
			atlas = atlas,
			color = color,
			fillAmount = 1,
			flip = dfSpriteFlip.None,
			pixelsToUnits = p2u,
			size = new Vector3( this.size.x - listPadding.horizontal, itemHeight ),
			spriteInfo = spriteInfo,
			offset = offset
		};

		if( spriteInfo.border.horizontal > 0 || spriteInfo.border.vertical > 0 )
			dfSlicedSprite.renderSprite( renderData, options );
		else
			dfSprite.renderSprite( renderData, options );

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

		// Return scale based on control size
		return Size.y / startSize.y;

	}

	private void renderItems( dfRenderData buffer )
	{

		if( font == null || items == null || items.Length == 0 )
			return;

		var p2u = PixelsToUnits();
		var maxSize = new Vector2( this.size.x - itemPadding.horizontal - listPadding.horizontal, itemHeight - itemPadding.vertical );

		var pivotOffset = pivot.TransformToUpperLeft( Size );
		var origin = new Vector3(
			pivotOffset.x + itemPadding.left + listPadding.left,
			pivotOffset.y - itemPadding.top - listPadding.top,
			0
		) * p2u;

		origin.y += scrollPosition * p2u;

		var renderColor = IsEnabled ? ItemTextColor : DisabledColor;

		// TODO: Figure out why the text renderer cannot be re-used for each list item

		var top = pivotOffset.y * p2u;
		var bottom = top - size.y * p2u;
		for( int i = 0; i < items.Length; i++ )
		{

			using( var textRenderer = font.ObtainRenderer() )
			{

				textRenderer.WordWrap = false;
				textRenderer.MaxSize = maxSize;
				textRenderer.PixelRatio = p2u;
				textRenderer.TextScale = ItemTextScale * getTextScaleMultiplier();
				textRenderer.VectorOffset = origin;
				textRenderer.MultiLine = false;
				textRenderer.TextAlign = this.ItemAlignment;
				textRenderer.ProcessMarkup = true;
				textRenderer.DefaultColor = renderColor;
				textRenderer.OverrideMarkupColors = false;
				textRenderer.Opacity = this.CalculateOpacity();
				textRenderer.Shadow = this.Shadow;
				textRenderer.ShadowColor = this.ShadowColor;
				textRenderer.ShadowOffset = this.ShadowOffset;

				var dynamicFontRenderer = textRenderer as dfDynamicFont.DynamicFontRenderer;
				if( dynamicFontRenderer != null )
				{
					dynamicFontRenderer.SpriteAtlas = this.Atlas;
					dynamicFontRenderer.SpriteBuffer = renderData;
				}

				if( origin.y - itemHeight * p2u <= top )
				{
					textRenderer.Render( items[ i ], buffer );
				}

				origin.y -= itemHeight * p2u;
				textRenderer.VectorOffset = origin;

				if( origin.y < bottom )
					break;

			}

		}

	}

	private void clipQuads( dfRenderData buffer, int startIndex )
	{

		// Performs a simplified version of triangle clipping, "clipping" vertices
		// to the vertical limits of the target render area. Simplified clipping 
		// does not split triangles, it simply moves vertices to the corresponding 
		// edge of the clip area and adjusts the UV coordinates to match, which 
		// means that it is faster than regular triangle clipping and does not
		// allocate any additional memory.

		var verts = buffer.Vertices;
		var uv = buffer.UV;

		var p2u = PixelsToUnits();
		var maxY = ( Pivot.TransformToUpperLeft( Size ).y - listPadding.top ) * p2u;
		var minY = maxY - ( size.y - listPadding.vertical ) * p2u;

		for( int i = startIndex; i < verts.Count; i += 4 )
		{

			var ul = verts[ i + 0 ];
			var ur = verts[ i + 1 ];
			var br = verts[ i + 2 ];
			var bl = verts[ i + 3 ];

			var h = ul.y - bl.y;

			// Bottom of clip rect
			if( bl.y < minY )
			{

				var clip = 1f - ( Mathf.Abs( -minY + ul.y ) / h );

				verts[ i + 0 ] = ul = new Vector3( ul.x, Mathf.Max( ul.y, minY ), ur.z );
				verts[ i + 1 ] = ur = new Vector3( ur.x, Mathf.Max( ur.y, minY ), ur.z );
				verts[ i + 2 ] = br = new Vector3( br.x, Mathf.Max( br.y, minY ), br.z );
				verts[ i + 3 ] = bl = new Vector3( bl.x, Mathf.Max( bl.y, minY ), bl.z );

				var uvy = Mathf.Lerp( uv[ i + 3 ].y, uv[ i ].y, clip );
				uv[ i + 3 ] = new Vector2( uv[ i + 3 ].x, uvy );
				uv[ i + 2 ] = new Vector2( uv[ i + 2 ].x, uvy );

				h = Mathf.Abs( bl.y - ul.y );

			}

			// Top of clip rect
			if( ul.y > maxY )
			{

				var clip = Mathf.Abs( maxY - ul.y ) / h;

				verts[ i + 0 ] = new Vector3( ul.x, Mathf.Min( maxY, ul.y ), ul.z );
				verts[ i + 1 ] = new Vector3( ur.x, Mathf.Min( maxY, ur.y ), ur.z );
				verts[ i + 2 ] = new Vector3( br.x, Mathf.Min( maxY, br.y ), br.z );
				verts[ i + 3 ] = new Vector3( bl.x, Mathf.Min( maxY, bl.y ), bl.z );

				var uvy = Mathf.Lerp( uv[ i ].y, uv[ i + 3 ].y, clip );
				uv[ i ] = new Vector2( uv[ i ].x, uvy );
				uv[ i + 1 ] = new Vector2( uv[ i + 1 ].x, uvy );

			}

		}

	}

	private void updateItemHover( dfMouseEventArgs args )
	{

		if( !Application.isPlaying )
			return;

		var ray = args.Ray;

		RaycastHit hitInfo;
		if( !collider.Raycast( ray, out hitInfo, 1000f ) )
		{
			hoverIndex = -1;
			hoverTweenLocation = 0f;
			return;
		}

		Vector2 hoverPos;
		GetHitPosition( ray, out hoverPos );

		var pivotOffset = Pivot.TransformToUpperLeft( Size );
		var top = pivotOffset.y + ( -itemHeight * ( selectedIndex - scrollPosition ) - listPadding.top );
		var needed = ( selectedIndex - scrollPosition + 1 ) * itemHeight + listPadding.vertical;
		var overlap = needed - size.y;
		if( overlap > 0 )
			top += overlap;

		var mousePos = hoverPos.y - listPadding.top;

		var index = (int)( scrollPosition + mousePos ) / itemHeight;
		if( index != hoverIndex )
		{
			hoverIndex = index;
			Invalidate();
		}

	}

	private float constrainScrollPosition( float value )
	{

		value = Mathf.Max( 0, value );

		var totalItemHeight = items.Length * itemHeight;
		var clientHeight = size.y - listPadding.vertical;
		if( totalItemHeight < clientHeight )
			return 0f;

		return Mathf.Min( value, totalItemHeight - clientHeight );

	}

	private void attachScrollbarEvents()
	{

		if( scrollbar == null || eventsAttached )
			return;

		eventsAttached = true;

		scrollbar.ValueChanged += scrollbar_ValueChanged;
		scrollbar.GotFocus += scrollbar_GotFocus;

	}

	private void detachScrollbarEvents()
	{

		if( scrollbar == null || !eventsAttached )
			return;

		eventsAttached = false;
		scrollbar.ValueChanged -= scrollbar_ValueChanged;
		scrollbar.GotFocus -= scrollbar_GotFocus;

	}

	void scrollbar_GotFocus( dfControl control, dfFocusEventArgs args )
	{
		// We don't want the Listbox to lose focus just because the user
		// clicks on the scrollbar. The scrollbar will function just fine
		// without retaining focus, whereas the Listbox will not respond
		// to keys unless it has focus.
		this.Focus();
	}

	private void scrollbar_ValueChanged( dfControl control, float value )
	{
		ScrollPosition = value;
	}

	private void synchronizeScrollbar()
	{

		if( scrollbar == null )
			return;

		var totalItemHeight = items.Length * itemHeight;
		var clientHeight = size.y - listPadding.vertical;

		scrollbar.IncrementAmount = itemHeight;
		scrollbar.MinValue = 0;
		scrollbar.MaxValue = totalItemHeight;
		scrollbar.ScrollSize = clientHeight;
		scrollbar.Value = scrollPosition;

	}

	#endregion

	#region IDFMultiRender Members

	private dfRenderData textRenderData = null;
	private dfList<dfRenderData> buffers = dfList<dfRenderData>.Obtain();

	public dfList<dfRenderData> RenderMultiple()
	{

		if( Atlas == null || Font == null )
			return null;

		if( !isVisible )
		{
			return null;
		}

		// Initialize render buffers if needed
		if( renderData == null )
		{

			renderData = dfRenderData.Obtain();
			textRenderData = dfRenderData.Obtain();

			isControlInvalidated = true;

		}

		var matrix = this.transform.localToWorldMatrix;

		// If control is not dirty, update the transforms on the 
		// render buffers (in case control moved) and return 
		// pre-rendered data
		if( !isControlInvalidated )
		{
			for( int i = 0; i < buffers.Count; i++ )
			{
				buffers[ i ].Transform = matrix;
			}
			return buffers;
		}

		#region Prepare render buffers

		buffers.Clear();

		renderData.Clear();
		renderData.Material = Atlas.Material;
		renderData.Transform = matrix;
		buffers.Add( renderData );

		textRenderData.Clear();
		textRenderData.Material = Atlas.Material;
		textRenderData.Transform = matrix;
		buffers.Add( textRenderData );

		#endregion

		// Render background before anything else, since we're going to 
		// want to keep track of where the background data ends and any
		// other data begins
		renderBackground();

		// We want to start clipping *after* the background is rendered, so 
		// grab the current number of vertices before rendering other elements
		var spriteClipStart = renderData.Vertices.Count;

		// Render other sprites
		renderHover();
		renderSelection();

		// Render text items
		renderItems( textRenderData );

		// Perform clipping
		clipQuads( renderData, spriteClipStart );
		clipQuads( textRenderData, 0 );

		// Control is no longer in need of rendering
		isControlInvalidated = false;

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

		if( this.items == null || this.items.Length == 0 )
			return;

		var effectiveTextScale = getTextScaleMultiplier();
		var effectiveFontSize = Mathf.CeilToInt( this.font.FontSize * effectiveTextScale );

		for( int i = 0; i < items.Length; i++ )
		{
			dynamicFont.AddCharacterRequest( items[ i ], effectiveFontSize, FontStyle.Normal );
		}

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
