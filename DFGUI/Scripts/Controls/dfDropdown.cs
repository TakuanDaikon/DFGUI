/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implements a drop-down list control
/// </summary>
[Serializable]
[ExecuteInEditMode]
[dfCategory( "Basic Controls" )]
[dfTooltip( "Implements a drop-down list control" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_dropdown.html" )]
[AddComponentMenu( "Daikon Forge/User Interface/Dropdown List" )]
public class dfDropdown : dfInteractiveBase, IDFMultiRender, IRendersText
{

	#region Public enumerations

	/// <summary>
	/// Specifies whether the pop-up list will appear under or over the
	/// <see cref="SimpleDropdown" /> or whether the position wil be
	/// determined automatically
	/// </summary>
	public enum PopupListPosition : int
	{
		/// <summary>
		/// The pop-up list will appear below the <see cref="SimpleDropdown" /> control
		/// </summary>
		Below,
		/// <summary>
		/// The pop-up list will appear above the <see cref="SimpleDropdown" /> control
		/// </summary>
		Above,
		/// <summary>
		/// The position of the pop-up list will be automatically determined
		/// </summary>
		Automatic
	}

	#endregion

	#region Public events

	/// <summary>
	/// Specifies the method signature required to process the DropdownOpen and DropdownClose events
	/// </summary>
	/// <param name="dropdown">A reference the the dfDropdown instance sending the event</param>
	/// <param name="popup">A reference to the dfListbox instance that will be displayed</param>
	/// <param name="overridden">Can be set to TRUE by the event subscriber to override the standard show/hide functionality</param>
	[dfEventCategory( "Popup" )]
	public delegate void PopupEventHandler( dfDropdown dropdown, dfListbox popup, ref bool overridden );

	/// <summary>
	/// Occurs when the drop-down list is displayed
	/// </summary>
	public event PopupEventHandler DropdownOpen;

	/// <summary>
	/// Occurs when the drop-down list is closed
	/// </summary>
	public event PopupEventHandler DropdownClose;

	/// <summary>
	/// Occurs when the value of the SelectedIndex property changes
	/// </summary>
	public event PropertyChangedEventHandler<int> SelectedIndexChanged;

	#endregion

	#region Serialized protected members

	[SerializeField]
	protected dfFontBase font;

	[SerializeField]
	protected int selectedIndex = -1;

	[SerializeField]
	protected dfControl triggerButton;

	[SerializeField]
	protected Color32 disabledTextColor = UnityEngine.Color.gray;

	[SerializeField]
	protected Color32 textColor = UnityEngine.Color.white;

	[SerializeField]
	protected float textScale = 1f;

	[SerializeField]
	protected RectOffset textFieldPadding = new RectOffset();

	[SerializeField]
	protected PopupListPosition listPosition = PopupListPosition.Below;

	[SerializeField]
	protected int listWidth = 0;

	[SerializeField]
	protected int listHeight = 200;

	[SerializeField]
	protected RectOffset listPadding = new RectOffset();

	[SerializeField]
	protected dfScrollbar listScrollbar = null;

	[SerializeField]
	protected int itemHeight = 25;

	[SerializeField]
	protected string itemHighlight = "";

	[SerializeField]
	protected string itemHover = "";

	[SerializeField]
	protected string listBackground = "";

	[SerializeField]
	protected Vector2 listOffset = Vector2.zero;

	[SerializeField]
	protected string[] items = new string[] { };

	[SerializeField]
	protected bool shadow = false;

	[SerializeField]
	protected Color32 shadowColor = UnityEngine.Color.black;

	[SerializeField]
	protected Vector2 shadowOffset = new Vector2( 1, -1 );

	[SerializeField]
	protected bool openOnMouseDown = true;

	[SerializeField]
	protected bool clickWhenSpacePressed = true;

	#endregion

	#region Private non-serialized variables

	private bool eventsAttached = false;
	private bool isFontCallbackAssigned = false;

	private dfListbox popup = null;

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
	/// Gets or sets the <see cref="dfFont"/> instance that will be used 
	/// to render the list items
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
				ClosePopup();
				unbindTextureRebuildCallback();
				this.font = value;
				bindTextureRebuildCallback();
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets a reference to a <see cref="Scrollbar"/> prefab that will
	/// be attached to the popup list
	/// </summary>
	public dfScrollbar ListScrollbar
	{
		get { return this.listScrollbar; }
		set
		{
			if( value != this.listScrollbar )
			{
				this.listScrollbar = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of space in pixels that the list will be 
	/// offset from the default display position
	/// </summary>
	public Vector2 ListOffset
	{
		get { return this.listOffset; }
		set
		{
			if( Vector2.Distance( listOffset, value ) > 1f )
			{
				listOffset = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the name of the sprite that will be used to 
	/// render the background image of the control
	/// </summary>
	public string ListBackground
	{
		get { return this.listBackground; }
		set
		{
			if( value != listBackground )
			{
				ClosePopup();
				listBackground = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the name of the sprite that will be used to render the 
	/// background of the list item that the mouse is hovering over
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
	/// Gets or sets the name of the sprite that will be used to render the 
	/// background of the list item that is currently selected
	/// </summary>
	public string ItemHighlight
	{
		get { return this.itemHighlight; }
		set
		{
			if( value != this.itemHighlight )
			{
				ClosePopup();
				this.itemHighlight = value;
				Invalidate();
			}
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

			// Look for the value in the current items. If found,
			// set SelectedIndex to match the value's index.
			for( int i = 0; i < this.items.Length; i++ )
			{
				if( items[ i ] == value )
				{
					this.selectedIndex = i;
					break;
				}
			}

			Invalidate();

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
				if( popup != null )
					popup.SelectedIndex = value;
				this.selectedIndex = value;
				OnSelectedIndexChanged();
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding to use when rendering the 
	/// text in the text field
	/// </summary>
	public RectOffset TextFieldPadding
	{
		get
		{
			if( textFieldPadding == null )
				textFieldPadding = new RectOffset();
			return this.textFieldPadding;
		}
		set
		{
			value = value.ConstrainPadding();
			if( !RectOffset.Equals( value, this.textFieldPadding ) )
			{
				this.textFieldPadding = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the color that will be used to render any text items
	/// </summary>
	public Color32 TextColor
	{
		get { return this.textColor; }
		set
		{
			ClosePopup();
			this.textColor = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the color that will be used to render the selected
	/// item's text when the control is disabled
	/// </summary>
	public Color32 DisabledTextColor
	{
		get { return this.disabledTextColor; }
		set
		{
			ClosePopup();
			this.disabledTextColor = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the size multiplier that will be used when rendering
	/// any text items
	/// </summary>
	public float TextScale
	{
		get { return this.textScale; }
		set
		{
			value = Mathf.Max( 0.1f, value );
			if( !Mathf.Approximately( textScale, value ) )
			{
				ClosePopup();
				dfFontManager.Invalidate( this.Font );
				this.textScale = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the height in pixels of a list item
	/// </summary>
	public int ItemHeight
	{
		get { return this.itemHeight; }
		set
		{
			value = Mathf.Max( 1, value );
			if( value != this.itemHeight )
			{
				ClosePopup();
				this.itemHeight = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the collection of values that will be displayed in the list
	/// </summary>
	public string[] Items
	{
		get
		{
			if( items == null )
				items = new string[] { };
			return items;
		}
		set
		{
			ClosePopup();
			if( value == null )
				value = new string[] { };
			items = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding that will be used when 
	/// rendering the popup list
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
	/// A <see cref="PopupListPosition"/> value indicating whether the pop-up
	/// list will appear below or above this control
	/// </summary>
	public PopupListPosition ListPosition
	{
		get { return this.listPosition; }
		set
		{
			if( value != this.ListPosition )
			{
				ClosePopup();
				this.listPosition = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the maximum width of the popup list
	/// </summary>
	public int MaxListWidth
	{
		get { return this.listWidth; }
		set { this.listWidth = value; }
	}

	/// <summary>
	/// Gets or sets the maximum height of the popup list. If the list 
	/// contains more items than can be displayed within this size, it
	/// will be scrollable.
	/// </summary>
	public int MaxListHeight
	{
		get { return this.listHeight; }
		set { this.listHeight = value; Invalidate(); }
	}

	/// <summary>
	/// Gets or sets a reference to a control that, when clicked by the user,
	/// will cause the popup list to be displayed
	/// </summary>
	public dfControl TriggerButton
	{
		get { return this.triggerButton; }
		set
		{
			if( value != this.triggerButton )
			{
				detachChildEvents();
				this.triggerButton = value;
				attachChildEvents();
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the dropdown will open the popup menu
	/// on a MouseDown action.
	/// </summary>
	public bool OpenOnMouseDown
	{
		get { return this.openOnMouseDown; }
		set { this.openOnMouseDown = value; }
	}

	/// <summary>
	/// Gets or sets whether text items will be rendered with a shadow
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
	/// Gets or sets the color that will be used when rendering the shadow
	/// for text if the <see cref="Shadow"/> property is set to TRUE
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
	/// Gets or sets the distance in pixels that a text item's shadow will 
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

	#region Event handlers

	protected internal override void OnMouseWheel( dfMouseEventArgs args )
	{

		SelectedIndex = Mathf.Max( 0, SelectedIndex - Mathf.RoundToInt( args.WheelDelta ) );

		args.Use();

		base.OnMouseWheel( args );

	}

	protected internal override void OnMouseDown( dfMouseEventArgs args )
	{

		if( openOnMouseDown && !args.Used && args.Buttons == dfMouseButtons.Left && args.Source == this )
		{

			args.Use();
			base.OnMouseDown( args );

			if( this.popup == null )
				OpenPopup();
			else
				ClosePopup();

		}
		else
		{
			base.OnMouseDown( args );
		}

	}

	protected internal override void OnKeyDown( dfKeyEventArgs args )
	{

		switch( args.KeyCode )
		{
			case KeyCode.UpArrow:
				SelectedIndex = Mathf.Max( 0, selectedIndex - 1 );
				break;
			case KeyCode.DownArrow:
				SelectedIndex = Mathf.Min( items.Length - 1, selectedIndex + 1 );
				break;
			case KeyCode.Home:
				SelectedIndex = 0;
				break;
			case KeyCode.End:
				SelectedIndex = items.Length - 1;
				break;
			case KeyCode.Space:
			case KeyCode.Return:
				if( this.ClickWhenSpacePressed && IsInteractive )
				{
					OpenPopup();
				}
				break;
		}

		base.OnKeyDown( args );

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
		ClosePopup( false );
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		ClosePopup( false );
		detachChildEvents();
	}

	public override void Update()
	{
		base.Update();
		checkForPopupClose();
	}

	private void checkForPopupClose()
	{

		// HACK: Since clicking on nothing doesn't remove a control's input focus,
		// we don't necessarily get any sort of automatic notification that the user 
		// has clicked outside of all focusable controls. We have to poll for such 
		// a condition manually.

		if( popup == null || !Input.GetMouseButtonDown( 0 ) )
			return;

		var camera = GetCamera();
		var ray = camera.ScreenPointToRay( Input.mousePosition );

		RaycastHit hitInfo;

		if( triggerButton != null && triggerButton.collider.Raycast( ray, out hitInfo, camera.farClipPlane ) )
			return;

		if( popup.collider.Raycast( ray, out hitInfo, camera.farClipPlane ) )
			return;

		if( popup.Scrollbar != null && popup.Scrollbar.collider.Raycast( ray, out hitInfo, camera.farClipPlane ) )
			return;

		if( this.collider.Raycast( ray, out hitInfo, camera.farClipPlane ) )
			return;

		ClosePopup();

	}

	public override void LateUpdate()
	{

		base.LateUpdate();

		if( !Application.isPlaying )
			return;

		// HACK: Need to perform initialization after all dependant objects 
		if( !eventsAttached )
		{
			attachChildEvents();
		}

		if( popup != null && !popup.ContainsFocus )
		{
			ClosePopup();
		}

	}

	private void attachChildEvents()
	{
		if( triggerButton != null && !eventsAttached )
		{
			eventsAttached = true;
			triggerButton.Click += trigger_Click;
		}
	}

	private void detachChildEvents()
	{
		if( triggerButton != null && eventsAttached )
		{
			triggerButton.Click -= trigger_Click;
			eventsAttached = false;
		}
	}

	void trigger_Click( dfControl control, dfMouseEventArgs mouseEvent )
	{

		if( mouseEvent.Source == triggerButton && !mouseEvent.Used )
		{

			mouseEvent.Use();

			if( popup == null )
			{
				OpenPopup();
			}
			else
			{
				ClosePopup();
			}

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

	#endregion

	#region Rendering

	private void renderText( dfRenderData buffer )
	{

		if( selectedIndex < 0 || selectedIndex >= items.Length )
			return;

		var selectedItem = items[ selectedIndex ];

		var p2u = PixelsToUnits();
		var maxSize = new Vector2( size.x - textFieldPadding.horizontal, this.size.y - textFieldPadding.vertical );

		var pivotOffset = pivot.TransformToUpperLeft( Size );
		var origin = new Vector3(
			pivotOffset.x + textFieldPadding.left,
			pivotOffset.y - textFieldPadding.top,
			0
		) * p2u;

		var renderColor = IsEnabled ? TextColor : DisabledTextColor;

		using( var textRenderer = font.ObtainRenderer() )
		{

			textRenderer.WordWrap = false;
			textRenderer.MaxSize = maxSize;
			textRenderer.PixelRatio = p2u;
			textRenderer.TextScale = TextScale;
			textRenderer.VectorOffset = origin;
			textRenderer.MultiLine = false;
			textRenderer.TextAlign = TextAlignment.Left;
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
				dynamicFontRenderer.SpriteBuffer = buffer;
			}

			textRenderer.Render( selectedItem, buffer );

		}

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

	}

	public void OpenPopup()
	{

		if( popup != null || items.Length == 0 )
			return;

		var popupSize = calculatePopupSize();

		// Create the popup list and set all necessary properties
		popup = GetManager().AddControl<dfListbox>();
		popup.name = this.name + " - Dropdown List";
		popup.gameObject.hideFlags = HideFlags.DontSave;
		popup.Atlas = this.Atlas;
		popup.Anchor = dfAnchorStyle.Left | dfAnchorStyle.Top;
		popup.Color = this.Color;
		popup.Font = this.Font;
		popup.Pivot = dfPivotPoint.TopLeft;
		popup.Size = popupSize;
		popup.Font = this.Font;
		popup.ItemHeight = this.ItemHeight;
		popup.ItemHighlight = this.ItemHighlight;
		popup.ItemHover = this.ItemHover;
		popup.ItemPadding = this.TextFieldPadding;
		popup.ItemTextColor = this.TextColor;
		popup.ItemTextScale = this.TextScale;
		popup.Items = this.Items;
		popup.ListPadding = this.ListPadding;
		popup.BackgroundSprite = this.ListBackground;
		popup.Shadow = this.Shadow;
		popup.ShadowColor = this.ShadowColor;
		popup.ShadowOffset = this.ShadowOffset;
		popup.BringToFront();

		// If there is a modal control/window up that displays this dropdown, 
		// need to ensure that it is able to receive input
		if( dfGUIManager.GetModalControl() != null )
		{
			dfGUIManager.PushModal( popup );
		}

		if( popupSize.y >= MaxListHeight && listScrollbar != null )
		{

			var scrollGO = GameObject.Instantiate( listScrollbar.gameObject ) as GameObject;
			var activeScrollbar = scrollGO.GetComponent<dfScrollbar>();

			var p2u = PixelsToUnits();
			var right = popup.transform.TransformDirection( Vector3.right );
			var scrollbarPosition = popup.transform.position + right * ( popupSize.x - activeScrollbar.Width ) * p2u;

			popup.AddControl( activeScrollbar );
			popup.Width -= activeScrollbar.Width;
			popup.Scrollbar = activeScrollbar;

			popup.SizeChanged += ( control, size ) =>
			{
				activeScrollbar.Height = control.Height;
			};

			activeScrollbar.transform.parent = popup.transform;
			activeScrollbar.transform.position = scrollbarPosition;

			activeScrollbar.Anchor = dfAnchorStyle.Top | dfAnchorStyle.Bottom;
			activeScrollbar.Height = popup.Height;

		}

		// Position the dropdown and set its rotation to the same world rotation 
		// as this control
		var popupPosition = calculatePopupPosition( (int)popup.Size.y );
		popup.transform.position = popupPosition;
		popup.transform.rotation = this.transform.rotation;

		// Attach important events
		popup.SelectedIndexChanged += popup_SelectedIndexChanged;
		popup.LeaveFocus += popup_LostFocus;
		popup.ItemClicked += popup_ItemClicked;
		popup.KeyDown += popup_KeyDown;

		// Make sure that the selected item is visible
		popup.SelectedIndex = Mathf.Max( 0, this.SelectedIndex );
		popup.EnsureVisible( popup.SelectedIndex );

		// Make sure that the popup has input focus
		popup.Focus();

		// Notify any listeners that the popup has been displayed
		if( DropdownOpen != null )
		{
			bool overridden = false;
			DropdownOpen( this, popup, ref overridden );
		}
		Signal( "OnDropdownOpen", this, popup );

	}

	public void ClosePopup()
	{
		ClosePopup( true );
	}

	public void ClosePopup( bool allowOverride )
	{

		if( popup == null )
			return;

		if( dfGUIManager.GetModalControl() == popup )
		{
			dfGUIManager.PopModal();
		}

		popup.LostFocus -= popup_LostFocus;
		popup.SelectedIndexChanged -= popup_SelectedIndexChanged;
		popup.ItemClicked -= popup_ItemClicked;
		popup.KeyDown -= popup_KeyDown;

		if( !allowOverride )
		{
			Destroy( popup.gameObject );
			popup = null;
			return;
		}

		bool overridden = false;
		if( DropdownClose != null )
		{
			DropdownClose( this, popup, ref overridden );
		}

		if( !overridden )
		{
			Signal( "OnDropdownClose", this, popup );
		}

		if( !overridden )
		{
			Destroy( popup.gameObject );
		}

		popup = null;

	}

	#endregion

	#region Private utility methods

	private Vector3 calculatePopupPosition( int height )
	{

		var p2u = PixelsToUnits();
		var controlPosition = this.transform.position + pivot.TransformToUpperLeft( size ) * p2u;
		var down = getScaledDirection( Vector3.down );
		var offset = transformOffset( listOffset );
		var positionBelow = controlPosition + ( offset + ( down * Size.y ) ) * p2u;
		var positionAbove = controlPosition + ( offset - ( down * popup.Size.y ) ) * p2u;

		if( listPosition == PopupListPosition.Above )
			return positionAbove;
		else if( listPosition == PopupListPosition.Below )
			return positionBelow;

		var screenSize = GetManager().GetScreenSize();

		var listBottom = GetAbsolutePosition().y + Height + height;
		if( listBottom >= screenSize.y )
			return positionAbove;

		return positionBelow;

	}

	private Vector2 calculatePopupSize()
	{

		var width = this.MaxListWidth > 0 ? this.MaxListWidth : this.size.x;

		var heightNeeded = items.Length * itemHeight + listPadding.vertical;
		if( items.Length == 0 )
		{
			// Need a way to visually represent that the list is empty
			heightNeeded = itemHeight / 2 + listPadding.vertical;
		}

		return new Vector2( width, Mathf.Min( this.MaxListHeight, heightNeeded ) );

	}

	private void popup_KeyDown( dfControl control, dfKeyEventArgs args )
	{
		if( args.KeyCode == KeyCode.Escape || args.KeyCode == KeyCode.Return )
		{
			ClosePopup();
			this.Focus();
		}
	}

	private void popup_ItemClicked( dfControl control, int selectedIndex )
	{
		// By the time this event fires the SelectedIndex value has already been 
		// synchronized, so just close the popup
		this.Focus();
	}

	private void popup_LostFocus( dfControl control, dfFocusEventArgs args )
	{

		// TODO: Revisit popup_LostFocus() when non-mouse control of focus is enabled

		if( popup != null && !popup.ContainsFocus )
		{
			ClosePopup();
		}

	}

	private void popup_SelectedIndexChanged( dfControl control, int selectedIndex )
	{
		this.SelectedIndex = selectedIndex;
		Invalidate();
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

		// If control is not dirty, update the transforms on the 
		// render buffers (in case control moved) and return 
		// pre-rendered data
		if( !isControlInvalidated )
		{
			for( int i = 0; i < buffers.Count; i++ )
			{
				buffers[ i ].Transform = transform.localToWorldMatrix;
			}
			return buffers;
		}

		#region Prepare render buffers

		buffers.Clear();

		renderData.Clear();
		renderData.Material = Atlas.Material;
		renderData.Transform = this.transform.localToWorldMatrix;
		buffers.Add( renderData );

		textRenderData.Clear();
		textRenderData.Material = Atlas.Material;
		textRenderData.Transform = this.transform.localToWorldMatrix;
		buffers.Add( textRenderData );

		#endregion

		// Render background before anything else, since we're going to 
		// want to keep track of where the background data ends and any
		// other data begins
		renderBackground();

		// Render text item
		renderText( textRenderData );

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

		var text = this.SelectedValue;
		if( string.IsNullOrEmpty( text ) )
			return;

		var effectiveTextScale = TextScale; // * getTextScaleMultiplier();
		var effectiveFontSize = Mathf.CeilToInt( this.font.FontSize * effectiveTextScale );

		dynamicFont.AddCharacterRequest( text, effectiveFontSize, FontStyle.Normal );

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
