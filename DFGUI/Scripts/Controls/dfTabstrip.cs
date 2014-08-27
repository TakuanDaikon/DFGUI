/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Used in conjunction with the <see cref="dfTabContainer"/> class to implement
/// tabbed containers. This control maintains the tabs that are displayed for
/// the user to select, and the <see cref="dfTabContainer"/> class manages the
/// display of the tab pages themselves.
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Used in conjunction with the dfTabContainer class to implement tabbed containers. This control maintains the tabs that are displayed for the user to select, and the dfTabContainer class manages the display of the tab pages themselves." )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_tabstrip.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Containers/Tab Control/Tab Strip" )]
public class dfTabstrip : dfControl
{

	#region Public events

	/// <summary>
	/// Raised whenever the value of the <see cref="SelectedIndex"/> property has changed
	/// </summary>
	public event PropertyChangedEventHandler<int> SelectedIndexChanged;

	#endregion

	#region Serialized protected members

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected string backgroundSprite;

	[SerializeField]
	protected RectOffset layoutPadding = new RectOffset();

	[SerializeField]
	protected Vector2 scrollPosition = Vector2.zero;

	[SerializeField]
	protected int selectedIndex = 0;

	[SerializeField]
	protected dfTabContainer pageContainer;

	[SerializeField]
	protected bool allowKeyboardNavigation = true;

	#endregion

	#region Public properties

	/// <summary>
	/// Gets or sets the associated <see cref="dfTabContainer"/> control, 
	/// which will maintain a list of pages (one for each tab) that will 
	/// be displayed when the corresponding tab is activated
	/// </summary>
	public dfTabContainer TabPages
	{
		get { return this.pageContainer; }
		set
		{
			if( pageContainer != value )
			{

				pageContainer = value;

				if( value != null )
				{
					while( value.Controls.Count < this.controls.Count )
					{
						value.AddTabPage();
					}
				}

				pageContainer.SelectedIndex = this.SelectedIndex;
				Invalidate();

			}
		}
	}

	/// <summary>
	/// Gets/Sets the index of the currently selected Tab.
	/// </summary>
	public int SelectedIndex
	{
		get { return this.selectedIndex; }
		set
		{
			if( value != this.selectedIndex )
			{
				selectTabByIndex( value );
			}
		}
	}

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
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding that will be applied to each 
	/// tab control
	/// </summary>
	public RectOffset LayoutPadding
	{
		get
		{
			if( this.layoutPadding == null )
				this.layoutPadding = new RectOffset();
			return this.layoutPadding;
		}
		set
		{
			value = value.ConstrainPadding();
			if( !RectOffset.Equals( value, this.layoutPadding ) )
			{
				this.layoutPadding = value;
				arrangeTabs();
			}
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the arrow keys can be used
	/// to navigate between tabs
	/// </summary>
	public bool AllowKeyboardNavigation
	{
		get { return this.allowKeyboardNavigation; }
		set { this.allowKeyboardNavigation = value; }
	}

	#endregion

	#region Public methods

	/// <summary>
	/// Enable the tab at the specified index
	/// </summary>
	/// <param name="index">The index of the tab to be enabled</param>
	public void EnableTab( int index )
	{
		if( this.selectedIndex >= 0 && this.selectedIndex <= this.controls.Count - 1 )
		{
			this.controls[ index ].Enable();
		}
	}

	/// <summary>
	/// Disable the tab at the specified index
	/// </summary>
	/// <param name="index">The index of the tab to be disabled</param>
	public void DisableTab( int index )
	{
		if( this.selectedIndex >= 0 && this.selectedIndex <= this.controls.Count - 1 )
		{
			this.controls[ index ].Disable();
		}
	}

	/// <summary>
	/// Adds a new tab to the list. If the <see cref="TabPages"/> property contains
	/// a reference to a <see cref="dfTabContainer"/> control, it will also add a 
	/// new tab page to that container. If there are already tabs in the list, the
	/// new tab will be a shallow copy of the first tab except for the Text property. 
	/// </summary>
	/// <param name="Text">The text to be displayed in the tab</param>
	/// <returns>Returns a reference to the newly-created tab control</returns>
	public dfControl AddTab( string Text )
	{

		if( Text == null )
			Text = string.Empty;

		var template = controls.Where( i => i is dfButton ).FirstOrDefault() as dfButton;

		var tabName = "Tab " + ( this.controls.Count + 1 );
		if( string.IsNullOrEmpty( Text ) )
			Text = tabName;

		var tab = AddControl<dfButton>();
		tab.name = tabName;
		tab.Atlas = this.Atlas;
		tab.Text = Text;
		tab.ButtonGroup = this;

		if( template != null )
		{

			tab.Atlas = template.Atlas;
			tab.Font = template.Font;

			tab.AutoSize = template.AutoSize;
			tab.Size = template.Size;

			tab.BackgroundSprite = template.BackgroundSprite;
			tab.DisabledSprite = template.DisabledSprite;
			tab.FocusSprite = template.FocusSprite;
			tab.HoverSprite = template.HoverSprite;
			tab.PressedSprite = template.PressedSprite;

			tab.Shadow = template.Shadow;
			tab.ShadowColor = template.ShadowColor;
			tab.ShadowOffset = template.ShadowOffset;

			tab.TextColor = template.TextColor;
			tab.TextAlignment = template.TextAlignment;

			var padding = template.Padding;
			tab.Padding = new RectOffset( padding.left, padding.right, padding.top, padding.bottom );

		}

		if( pageContainer != null )
		{
			pageContainer.AddTabPage();
		}

		arrangeTabs();
		Invalidate();

		return tab;

	}

	#endregion

	#region Event handlers

	protected internal override void OnGotFocus( dfFocusEventArgs args )
	{

		if( controls.Contains( args.GotFocus ) )
			this.SelectedIndex = args.GotFocus.ZOrder;

		base.OnGotFocus( args );

	}

	protected internal override void OnLostFocus( dfFocusEventArgs args )
	{

		base.OnLostFocus( args );

		if( controls.Contains( args.LostFocus ) )
			showSelectedTab();

	}

	protected internal override void OnClick( dfMouseEventArgs args )
	{

		if( controls.Contains( args.Source ) )
		{
			this.SelectedIndex = args.Source.ZOrder;
		}

		base.OnClick( args );

	}

	private void OnClick( dfControl sender, dfMouseEventArgs args )
	{

		if( !controls.Contains( args.Source ) )
			return;

		this.SelectedIndex = args.Source.ZOrder;

	}

	protected internal override void OnKeyDown( dfKeyEventArgs args )
	{

		if( args.Used )
			return;

		if( allowKeyboardNavigation )
		{

			if( args.KeyCode == KeyCode.LeftArrow || ( args.KeyCode == KeyCode.Tab && args.Shift ) )
			{
				SelectedIndex = Mathf.Max( 0, SelectedIndex - 1 );
				args.Use();
				return;
			}
			else if( args.KeyCode == KeyCode.RightArrow || args.KeyCode == KeyCode.Tab )
			{
				SelectedIndex += 1;
				args.Use();
				return;
			}

		}

		base.OnKeyDown( args );

	}

	protected internal override void OnControlAdded( dfControl child )
	{
		base.OnControlAdded( child );
		attachEvents( child );
		arrangeTabs();
	}

	protected internal override void OnControlRemoved( dfControl child )
	{
		base.OnControlRemoved( child );
		detachEvents( child );
		arrangeTabs();
	}

	public override void OnEnable()
	{

		base.OnEnable();

		if( size.sqrMagnitude < float.Epsilon )
		{
			this.Size = new Vector2( 256, 26 );
		}

		if( Application.isPlaying )
		{
			selectTabByIndex( Mathf.Max( this.selectedIndex, 0 ) );
		}

	}

	public override void Update()
	{

		base.Update();

		if( isControlInvalidated )
		{
			arrangeTabs();
		}

		showSelectedTab();

	}

	protected internal virtual void OnSelectedIndexChanged()
	{

		SignalHierarchy( "OnSelectedIndexChanged", this, this.SelectedIndex );

		if( SelectedIndexChanged != null )
		{
			SelectedIndexChanged( this, this.SelectedIndex );
		}

	}

	#endregion

	#region Rendering

	protected override void OnRebuildRenderData()
	{

		if( Atlas == null || string.IsNullOrEmpty( backgroundSprite ) )
			return;

		var spriteInfo = Atlas[ backgroundSprite ];
		if( spriteInfo == null )
		{
			return;
		}

		renderData.Material = Atlas.Material;

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

	#endregion

	#region Private utility methods

	private void showSelectedTab()
	{

		if( this.selectedIndex >= 0 && this.selectedIndex <= this.controls.Count - 1 )
		{
			var tab = this.controls[ this.selectedIndex ] as dfButton;
			if( tab != null && !tab.ContainsMouse )
			{
				tab.State = dfButton.ButtonState.Focus;
			}
		}

	}

	private void selectTabByIndex( int value )
	{

		value = Mathf.Max( Mathf.Min( value, controls.Count - 1 ), -1 );
		if( value == this.selectedIndex )
			return;

		this.selectedIndex = value;

		for( int i = 0; i < controls.Count; i++ )
		{

			var tab = controls[ i ] as dfButton;
			if( tab == null )
				continue;

			if( i == value )
			{
				tab.State = dfButton.ButtonState.Focus;
			}
			else
			{
				tab.State = dfButton.ButtonState.Default;
			}

		}

		Invalidate();

		OnSelectedIndexChanged();

		if( pageContainer != null )
		{
			pageContainer.SelectedIndex = value;
		}

	}

	private void arrangeTabs()
	{

		SuspendLayout();
		try
		{

			layoutPadding = layoutPadding.ConstrainPadding();

			var x = (float)layoutPadding.left - scrollPosition.x;
			var y = (float)layoutPadding.top - scrollPosition.y;

			var maxWidth = 0f;
			var maxHeight = 0f;

			for( int i = 0; i < Controls.Count; i++ )
			{

				var child = controls[ i ];
				if( !child.IsVisible || !child.enabled || !child.gameObject.activeSelf )
					continue;

				var childPosition = new Vector2( x, y );
				child.RelativePosition = childPosition;

				var xofs = child.Width + layoutPadding.horizontal;
				var yofs = child.Height + layoutPadding.vertical;

				maxWidth = Mathf.Max( xofs, maxWidth );
				maxHeight = Mathf.Max( yofs, maxHeight );

				x += xofs;

			}

		}
		finally
		{
			ResumeLayout();
		}

	}

	private void attachEvents( dfControl control )
	{
		control.IsVisibleChanged += control_IsVisibleChanged;
		control.PositionChanged += childControlInvalidated;
		control.SizeChanged += childControlInvalidated;
		control.ZOrderChanged += childControlZOrderChanged;
	}

	private void detachEvents( dfControl control )
	{
		control.IsVisibleChanged -= control_IsVisibleChanged;
		control.PositionChanged -= childControlInvalidated;
		control.SizeChanged -= childControlInvalidated;
	}

	void childControlZOrderChanged( dfControl control, int value )
	{
		onChildControlInvalidatedLayout();
	}

	void control_IsVisibleChanged( dfControl control, bool value )
	{
		onChildControlInvalidatedLayout();
	}

	private void childControlInvalidated( dfControl control, Vector2 value )
	{
		onChildControlInvalidatedLayout();
	}

	private void onChildControlInvalidatedLayout()
	{

		if( IsLayoutSuspended )
			return;

		arrangeTabs();
		Invalidate();

	}

	#endregion

}
