/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Used in conjunction with the <see cref="dfTabstrip"/> class to implement
/// tabbed containers. This control maintains the pages that are used to host
/// the controls, while <see cref="dfTabstrip"/> manages the tabs themselves.
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Used in conjunction with the dfTabContainer class to implement tabbed containers. This control maintains the tabs that are displayed for the user to select, and the dfTabContainer class manages the display of the tab pages themselves." )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_tab_container.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Containers/Tab Control/Tab Page Container" )]
public class dfTabContainer : dfControl
{

	#region Public events

	/// <summary>
	/// Raised whenever the value of the <see cref="SelectedIndex"/> property has changed
	/// </summary>
	public event PropertyChangedEventHandler<int> SelectedIndexChanged;

	#endregion

	#region Protected serialized members

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected string backgroundSprite;

	[SerializeField]
	protected RectOffset padding = new RectOffset();

	[SerializeField]
	protected int selectedIndex = 0;

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
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding that will be applied when 
	/// layout out the tab pages
	/// </summary>
	public RectOffset Padding
	{
		get
		{
			if( this.padding == null )
				this.padding = new RectOffset();
			return this.padding;
		}
		set
		{
			value = value.ConstrainPadding();
			if( !RectOffset.Equals( value, this.padding ) )
			{
				this.padding = value;
				arrangeTabPages();
			}
		}
	}

	/// <summary>
	/// Gets or sets the index of the currently visible tab page
	/// </summary>
	public int SelectedIndex
	{
		get { return this.selectedIndex; }
		set
		{
			if( value != this.selectedIndex )
			{
				selectPageByIndex( value );
			}
		}
	}

	#endregion

	#region Public methods

	/// <summary>
	/// Adds a new tab page to the list
	/// </summary>
	/// <returns>Returns a reference to the newly-created tab page</returns>
	public dfControl AddTabPage()
	{

		var template = controls.Where( i => i is dfPanel ).FirstOrDefault() as dfPanel;
		var pageName = "Tab Page " + ( this.controls.Count + 1 );

		var page = AddControl<dfPanel>();
		page.name = pageName;
		page.Atlas = this.Atlas;
		page.Anchor = dfAnchorStyle.All;
		page.ClipChildren = true;

		if( template != null )
		{

			page.Atlas = template.Atlas;
			page.BackgroundSprite = template.BackgroundSprite;

		}

		arrangeTabPages();
		Invalidate();

		return page;

	}

	#endregion

	#region Events

	public override void OnEnable()
	{

		base.OnEnable();

		if( size.sqrMagnitude < float.Epsilon )
		{
			this.Size = new Vector2( 256, 256 );
		}

	}

	protected internal override void OnControlAdded( dfControl child )
	{
		base.OnControlAdded( child );
		attachEvents( child );
		arrangeTabPages();
	}

	protected internal override void OnControlRemoved( dfControl child )
	{
		base.OnControlRemoved( child );
		detachEvents( child );
		arrangeTabPages();
	}

	protected internal virtual void OnSelectedIndexChanged( int Index )
	{

		SignalHierarchy( "OnSelectedIndexChanged", this, Index );

		if( SelectedIndexChanged != null )
		{
			SelectedIndexChanged( this, Index );
		}

	}

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

	private void selectPageByIndex( int value )
	{

		value = Mathf.Max( Mathf.Min( value, controls.Count - 1 ), -1 );
		if( value == this.selectedIndex )
			return;

		this.selectedIndex = value;

		for( int i = 0; i < controls.Count; i++ )
		{

			var page = controls[ i ];
			if( page == null )
				continue;

			page.IsVisible = ( i == value );

		}

		arrangeTabPages();
		Invalidate();

		OnSelectedIndexChanged( value );

	}

	private void arrangeTabPages()
	{

		if( padding == null )
			padding = new RectOffset( 0, 0, 0, 0 );

		var pagePosition = new Vector3( padding.left, padding.top );
		var pageSize = new Vector2( size.x - padding.horizontal, size.y - padding.vertical );

		for( int i = 0; i < controls.Count; i++ )
		{

			// A TabContainer may contain controls other than Tab Pages,
			// for instance when a dropdown list displays its popup
			// menu. Do not auto-arrange anything but dfPanel instances
			var child = controls[ i ] as dfPanel;
			if( child != null )
			{
				child.Size = pageSize;
				child.RelativePosition = pagePosition;
			}

		}

	}

	private void attachEvents( dfControl control )
	{
		control.IsVisibleChanged += control_IsVisibleChanged;
		control.PositionChanged += childControlInvalidated;
		control.SizeChanged += childControlInvalidated;
	}

	private void detachEvents( dfControl control )
	{
		control.IsVisibleChanged -= control_IsVisibleChanged;
		control.PositionChanged -= childControlInvalidated;
		control.SizeChanged -= childControlInvalidated;
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

		arrangeTabPages();
		Invalidate();

	}

	#endregion

}
