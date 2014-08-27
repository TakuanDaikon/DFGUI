/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for controls which can be selected or have input focus and 
/// which can be configured to display a different Sprite for each state
/// </summary>
[Serializable]
[ExecuteInEditMode]
public class dfInteractiveBase : dfControl
{

	#region Protected serialized fields

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected string backgroundSprite;

	[SerializeField]
	protected string hoverSprite;

	[SerializeField]
	protected string disabledSprite;

	[SerializeField]
	protected string focusSprite;

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
				setDefaultSize( value );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The name of the image in the <see cref="Atlas"/> that will be used
	/// to render the background of this control in its Disabled state
	/// </summary>
	public string DisabledSprite
	{
		get { return disabledSprite; }
		set
		{
			if( value != disabledSprite )
			{
				disabledSprite = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The name of the image in the <see cref="Atlas"/> that will be used
	/// to render the background of this control in its Focused state
	/// </summary>
	public string FocusSprite
	{
		get { return focusSprite; }
		set
		{
			if( value != focusSprite )
			{
				focusSprite = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The name of the image in the <see cref="Atlas"/> that will be used
	/// to render the background of this control in its Hover state
	/// </summary>
	public string HoverSprite
	{
		get { return hoverSprite; }
		set
		{
			if( value != hoverSprite )
			{
				hoverSprite = value;
				Invalidate();
			}
		}
	}

	#endregion

	#region Overrides and event handling

	/// <summary>
	/// Gets a value indicating whether the control can receive focus.
	/// </summary>
	public override bool CanFocus
	{
		get
		{
			if( this.IsEnabled && this.IsVisible )
				return true;
			return base.CanFocus;
		}
	}

	protected internal override void OnGotFocus( dfFocusEventArgs args )
	{

		base.OnGotFocus( args );

		// This control needs to redraw itself when it obtains focus
		Invalidate();

	}

	protected internal override void OnLostFocus( dfFocusEventArgs args )
	{

		base.OnLostFocus( args );

		// This control needs to redraw itself when it loses focus
		Invalidate();

	}

	protected internal override void OnMouseEnter( dfMouseEventArgs args )
	{

		base.OnMouseEnter( args );

		// This control needs to redraw itself when the mouse is hovering
		Invalidate();

	}

	protected internal override void OnMouseLeave( dfMouseEventArgs args )
	{

		base.OnMouseLeave( args );

		// This control needs to redraw itself when the mouse is no longer hovering
		Invalidate();

	}

	public override Vector2 CalculateMinimumSize()
	{

		var spriteInfo = getBackgroundSprite();
		if( spriteInfo == null )
			return base.CalculateMinimumSize();

		var borders = spriteInfo.border;

		if( borders.horizontal > 0 || borders.vertical > 0 )
		{
			return Vector2.Max( base.CalculateMinimumSize(), new Vector2( borders.horizontal, borders.vertical ) );
		}

		return base.CalculateMinimumSize();

	}

	#endregion

	#region Private utility methods

	protected internal virtual void renderBackground()
	{

		if( Atlas == null )
			return;

		var spriteInfo = getBackgroundSprite();
		if( spriteInfo == null )
		{
			return;
		}

		var color = ApplyOpacity( getActiveColor() );
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

	protected virtual Color32 getActiveColor()
	{

		if( IsEnabled )
			return this.color;

		if( !string.IsNullOrEmpty( this.disabledSprite ) )
		{
			if( Atlas != null && Atlas[ DisabledSprite ] != null )
				return this.color;
		}

		return this.disabledColor;

	}

	protected internal virtual dfAtlas.ItemInfo getBackgroundSprite()
	{

		if( Atlas == null )
			return null;

		if( !IsEnabled )
		{

			var disabled = atlas[ DisabledSprite ];
			if( disabled != null )
			{
				return disabled;
			}

			return atlas[ BackgroundSprite ];

		}

		if( HasFocus )
		{
			var focus = atlas[ FocusSprite ];
			if( focus != null )
				return focus;
			else
				return atlas[ BackgroundSprite ];
		}

#if !UNITY_IPHONE && !UNITY_ANDROID
		if( isMouseHovering )
		{
			var hover = atlas[ HoverSprite ];
			if( hover != null )
				return hover;
		}
#endif

		return Atlas[ BackgroundSprite ];

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
