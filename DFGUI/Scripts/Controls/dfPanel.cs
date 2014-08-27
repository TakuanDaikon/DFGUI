/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Basic container control to facilitate user interface layout
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Basic container control to facilitate user interface layout" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_panel.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Containers/Panel" )]
public class dfPanel : dfControl
{

	#region Serialized protected members

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected string backgroundSprite;

	[SerializeField]
	protected Color32 backgroundColor = UnityEngine.Color.white;

	[SerializeField]
	protected RectOffset padding = new RectOffset();

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
			value = getLocalizedValue( value );
			if( value != backgroundSprite )
			{
				backgroundSprite = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The color that will be used to render the background sprite, if any
	/// </summary>
	public Color32 BackgroundColor
	{
		get { return this.backgroundColor; }
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
	/// Gets/Sets the amount of padding to add to the client region. 
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
				Invalidate();
			}
		}
	}

	#endregion

	#region Overrides

	protected internal override void OnLocalize()
	{
		base.OnLocalize();
		this.BackgroundSprite = getLocalizedValue( this.backgroundSprite );
	}

	/// <summary>
	/// Returns the padding used when clipping is enabled and 
	/// the renderer is using shader-based clipping
	/// </summary>
	/// <returns></returns>
	protected internal override RectOffset GetClipPadding()
	{
		return this.padding ?? dfRectOffsetExtensions.Empty;
	}

	protected internal override Plane[] GetClippingPlanes()
	{

		if( !ClipChildren )
			return null;

		var corners = GetCorners();

		var right = transform.TransformDirection( Vector3.right );
		var left = transform.TransformDirection( Vector3.left );
		var up = transform.TransformDirection( Vector3.up );
		var down = transform.TransformDirection( Vector3.down );

		var p2u = PixelsToUnits();
		var padding = Padding;
		corners[ 0 ] += right * padding.left * p2u + down * padding.top * p2u;
		corners[ 1 ] += left * padding.right * p2u + down * padding.top * p2u;
		corners[ 2 ] += right * padding.left * p2u + up * padding.bottom * p2u;

		return new Plane[]
		{
			new Plane( right, corners[ 0 ] ),
			new Plane( left, corners[ 1 ] ),
			new Plane( up, corners[ 2 ] ),
			new Plane( down, corners[ 0 ] )
		};

	}

	public override void OnEnable()
	{

		base.OnEnable();

		if( this.size == Vector2.zero )
		{
			SuspendLayout();
			var camera = GetCamera();
			Size = new Vector3( camera.pixelWidth / 2, camera.pixelHeight / 2 );
			ResumeLayout();
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

	#region Public methods

	/// <summary>
	/// Resizes the panel to ensure that it encompasses all child controls
	/// </summary>
	public void FitToContents()
	{

		if( controls.Count == 0 )
			return;

		var max = Vector2.zero;
		for( int i = 0; i < controls.Count; i++ )
		{

			var child = controls[ i ];
			var childMax = (Vector2)child.RelativePosition + child.Size;

			max = Vector2.Max( max, childMax );

		}

		this.Size = max + new Vector2( padding.right, padding.bottom );

	}

	/// <summary>
	/// Centers all child controls within the bounds of the panel
	/// </summary>
	public void CenterChildControls()
	{

		if( controls.Count == 0 )
			return;

		var min = Vector2.one * float.MaxValue;
		var max = Vector2.one * float.MinValue;

		for( int i = 0; i < controls.Count; i++ )
		{

			var child = controls[ i ];
			var childMin = (Vector2)child.RelativePosition;
			var childMax = childMin + child.Size;

			min = Vector2.Min( min, childMin );
			max = Vector2.Max( max, childMax );

		}

		var contentSize = max - min;
		var contentOffset = ( this.Size - contentSize ) * 0.5f;

		for( int i = 0; i < controls.Count; i++ )
		{
			var child = controls[ i ];
			child.RelativePosition = (Vector2)child.RelativePosition - min + contentOffset;
		}

	}

	#endregion

}
