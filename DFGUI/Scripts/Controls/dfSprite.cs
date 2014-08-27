/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Used to render a sprite from a Texture Atlas on the screen
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Used to render a sprite from a Texture Atlas on the screen" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_sprite.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Sprite/Basic" )]
public class dfSprite : dfControl
{

	#region Static variables

	private static int[] TRIANGLE_INDICES = new int[] { 0, 1, 3, 3, 1, 2 };

	#endregion

	#region Events

	/// <summary>
	/// Raised when the value of the <see cref="SpriteName"/> property has changed
	/// </summary>
	public event PropertyChangedEventHandler<string> SpriteNameChanged;

	#endregion

	#region Serialized data members

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected string spriteName;

	[SerializeField]
	protected dfSpriteFlip flip = dfSpriteFlip.None;

	[SerializeField]
	protected dfFillDirection fillDirection = dfFillDirection.Horizontal;

	[SerializeField]
	protected float fillAmount = 1f;

	[SerializeField]
	protected bool invertFill = false;

	#endregion

	#region Public properties

	/// <summary>
	/// The <see cref="dfAtlas">Texture Atlas</see> containing the images used by this control
	/// </summary>
	public dfAtlas Atlas
	{
		get
		{
			if( atlas == null )
			{
				var view = GetManager();
				if( view != null )
				{
					return atlas = view.DefaultAtlas;
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
	/// The name of a sprite in the <see cref="Atlas"/> that will be rendered
	/// </summary>
	public string SpriteName
	{
		get { return spriteName; }
		set
		{
			value = getLocalizedValue( value );
			if( value != spriteName )
			{

				spriteName = value;

				var spriteInfo = SpriteInfo;
				if( size == Vector2.zero && spriteInfo != null )
				{
					size = spriteInfo.sizeInPixels;
					updateCollider();
				}

				Invalidate();
				OnSpriteNameChanged( value );

			}
		}
	}

	/// <summary>
	/// Returns the ItemInfo structure containing the sprite information 
	/// for the sprite indicated by the <see cref="SpriteName"/> property
	/// </summary>
	public dfAtlas.ItemInfo SpriteInfo
	{
		get
		{

			var result = (dfAtlas.ItemInfo)null;

			if( Atlas == null )
				return null;

			result = Atlas[ spriteName ];

			return result;

		}
	}

	/// <summary>
	/// Gets or sets which axes will be flipped when rendering the sprite
	/// </summary>
	public dfSpriteFlip Flip
	{
		get { return flip; }
		set
		{
			if( value != flip )
			{
				flip = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets which direction will be used for a fill operation 
	/// during rendering
	/// </summary>
	public dfFillDirection FillDirection
	{
		get { return this.fillDirection; }
		set
		{
			if( value != this.fillDirection )
			{
				this.fillDirection = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The amount (from 0.0 to 1.0) of the sprite's surface to be filled
	/// </summary>
	public float FillAmount
	{
		get { return this.fillAmount; }
		set
		{
			if( !Mathf.Approximately( value, this.fillAmount ) )
			{
				this.fillAmount = Mathf.Max( 0, Mathf.Min( 1, value ) );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// If set to TRUE, will invert the fill direction
	/// </summary>
	public bool InvertFill
	{
		get { return invertFill; }
		set
		{
			if( value != invertFill )
			{
				invertFill = value;
				Invalidate();
			}
		}
	}

	#endregion

	protected internal override void OnLocalize()
	{
		base.OnLocalize();
		this.SpriteName = getLocalizedValue( this.spriteName );
	}

	protected internal virtual void OnSpriteNameChanged( string value )
	{

		Signal( "OnSpriteNameChanged", this, value );

		if( SpriteNameChanged != null )
		{
			SpriteNameChanged( this, value );
		}

	}

	public override Vector2 CalculateMinimumSize()
	{

		var spriteInfo = SpriteInfo;
		if( spriteInfo == null )
			return Vector2.zero;

		var borders = spriteInfo.border;
		if( borders != null && borders.horizontal > 0 && borders.vertical > 0 )
		{
			return Vector2.Max( base.CalculateMinimumSize(), new Vector2( borders.horizontal, borders.vertical ) );
		}

		return base.CalculateMinimumSize();

	}

	protected override void OnRebuildRenderData()
	{

		var isValidConfig =
			Atlas != null &&
			Atlas.Material != null;

		if( !isValidConfig )
		{
			return;
		}

		if( SpriteInfo == null )
		{
			return;
		}

		renderData.Material = Atlas.Material;

		var color = ApplyOpacity( IsEnabled ? this.color : this.disabledColor );
		var options = new RenderOptions()
		{
			atlas = Atlas,
			color = color,
			fillAmount = fillAmount,
			fillDirection = fillDirection,
			flip = flip,
			invertFill = invertFill,
			offset = pivot.TransformToUpperLeft( Size ),
			pixelsToUnits = PixelsToUnits(),
			size = Size,
			spriteInfo = SpriteInfo
		};

		renderSprite( renderData, options );

	}

	internal static void renderSprite( dfRenderData data, RenderOptions options )
	{

		if( options.fillAmount <= float.Epsilon )
			return;

#if UNITY_EDITOR

		var atlas = options.atlas;
		if( atlas == null )
			throw new NullReferenceException( "The Texture Atlas cannot be null" );

		if( atlas.Texture == null )
			throw new NullReferenceException( "The Texture Altas has no texture assigned or the texture was deleted" );

		if( options.spriteInfo == null )
			throw new ArgumentNullException( "The Sprite cannot be null" );

#endif

		options.baseIndex = data.Vertices.Count;

		rebuildTriangles( data, options );
		rebuildVertices( data, options );
		rebuildUV( data, options );
		rebuildColors( data, options );

		if( options.fillAmount < 1f - float.Epsilon )
		{
			doFill( data, options );
		}

	}

	private static void rebuildTriangles( dfRenderData renderData, RenderOptions options )
	{

		var baseIndex = options.baseIndex;

		var triangles = renderData.Triangles;
		triangles.EnsureCapacity( triangles.Count + TRIANGLE_INDICES.Length );

		for( int i = 0; i < TRIANGLE_INDICES.Length; i++ )
		{
			triangles.Add( baseIndex + TRIANGLE_INDICES[ i ] );
		}

	}

	private static void rebuildVertices( dfRenderData renderData, RenderOptions options )
	{

		var verts = renderData.Vertices;
		var baseIndex = options.baseIndex;

		float meshLeft = 0;
		float meshTop = 0;
		float meshRight = Mathf.Ceil( options.size.x );
		float meshBottom = Mathf.Ceil( -options.size.y );

		verts.Add( new Vector3( meshLeft, meshTop, 0 ) * options.pixelsToUnits );
		verts.Add( new Vector3( meshRight, meshTop, 0 ) * options.pixelsToUnits );
		verts.Add( new Vector3( meshRight, meshBottom, 0 ) * options.pixelsToUnits );
		verts.Add( new Vector3( meshLeft, meshBottom, 0 ) * options.pixelsToUnits );

		var offset = options.offset.RoundToInt() * options.pixelsToUnits;
		var rawVerts = verts.Items;
		for( int i = baseIndex; i < baseIndex + 4; i++ )
		{
			rawVerts[ i ] = ( rawVerts[ i ] + offset ).Quantize( options.pixelsToUnits );
		}

	}

	private static void rebuildColors( dfRenderData renderData, RenderOptions options )
	{
		var colors = renderData.Colors;
		colors.Add( options.color );
		colors.Add( options.color );
		colors.Add( options.color );
		colors.Add( options.color );
	}

	private static void rebuildUV( dfRenderData renderData, RenderOptions options )
	{

		var rect = options.spriteInfo.region;
		var result = renderData.UV;

		result.Add( new Vector2( rect.x, rect.yMax ) );
		result.Add( new Vector2( rect.xMax, rect.yMax ) );
		result.Add( new Vector2( rect.xMax, rect.y ) );
		result.Add( new Vector2( rect.x, rect.y ) );

		var temp = Vector2.zero;

		if( options.flip.IsSet( dfSpriteFlip.FlipHorizontal ) )
		{
			temp = result[ 1 ];
			result[ 1 ] = result[ 0 ];
			result[ 0 ] = temp;
			temp = result[ 3 ];
			result[ 3 ] = result[ 2 ];
			result[ 2 ] = temp;
		}

		if( options.flip.IsSet( dfSpriteFlip.FlipVertical ) )
		{
			temp = result[ 0 ];
			result[ 0 ] = result[ 3 ];
			result[ 3 ] = temp;
			temp = result[ 1 ];
			result[ 1 ] = result[ 2 ];
			result[ 2 ] = temp;
		}

	}

	private static void doFill( dfRenderData renderData, RenderOptions options )
	{

		var baseIndex = options.baseIndex;
		var verts = renderData.Vertices;
		var uv = renderData.UV;

		var ul = baseIndex + 0;
		var ur = baseIndex + 1;
		var bl = baseIndex + 3;
		var br = baseIndex + 2;

		if( options.invertFill )
		{
			if( options.fillDirection == dfFillDirection.Horizontal )
			{
				ul = baseIndex + 1;
				ur = baseIndex + 0;
				bl = baseIndex + 2;
				br = baseIndex + 3;
			}
			else
			{
				ul = baseIndex + 3;
				ur = baseIndex + 2;
				bl = baseIndex + 0;
				br = baseIndex + 1;
			}
		}

		if( options.fillDirection == dfFillDirection.Horizontal )
		{
			verts[ ur ] = Vector3.Lerp( verts[ ur ], verts[ ul ], 1f - options.fillAmount );
			verts[ br ] = Vector3.Lerp( verts[ br ], verts[ bl ], 1f - options.fillAmount );
			uv[ ur ] = Vector2.Lerp( uv[ ur ], uv[ ul ], 1f - options.fillAmount );
			uv[ br ] = Vector2.Lerp( uv[ br ], uv[ bl ], 1f - options.fillAmount );
		}
		else
		{
			verts[ bl ] = Vector3.Lerp( verts[ bl ], verts[ ul ], 1f - options.fillAmount );
			verts[ br ] = Vector3.Lerp( verts[ br ], verts[ ur ], 1f - options.fillAmount );
			uv[ bl ] = Vector2.Lerp( uv[ bl ], uv[ ul ], 1f - options.fillAmount );
			uv[ br ] = Vector2.Lerp( uv[ br ], uv[ ur ], 1f - options.fillAmount );
		}

	}

	/// <summary>
	/// Returns a formatted string summarizing this object's state
	/// </summary>
	public override string ToString()
	{
		if( !string.IsNullOrEmpty( spriteName ) )
			return string.Format( "{0} ({1})", name, spriteName );
		return base.ToString();
	}

	/// <summary>
	/// Specifies the list of options used when rendering a Sprite
	/// </summary>
	internal struct RenderOptions
	{
		/// <summary>
		/// The Atlas containing the sprite to render
		/// </summary>
		public dfAtlas atlas;
		/// <summary>
		/// A reference to the <see cref="dfAtlas.SpriteInfo"/> corresponding
		/// to the sprite to be rendered
		/// </summary>
		public dfAtlas.ItemInfo spriteInfo;
		/// <summary>
		/// The <see cref="UnityEngine.Color"/> used to tint the sprite
		/// </summary>
		public Color32 color;
		/// <summary>
		/// Used to convert "pixel" values to world units
		/// </summary>
		public float pixelsToUnits;
		/// <summary>
		/// The desired size of the sprite
		/// </summary>
		public Vector2 size;
		/// <summary>
		/// Specifies how (or whether) the sprite should be flipped 
		/// </summary>
		public dfSpriteFlip flip;
		/// <summary>
		/// Indicates whether any current fill should be inverted
		/// </summary>
		public bool invertFill;
		/// <summary>
		/// Specifies the direction of sprite fill
		/// </summary>
		public dfFillDirection fillDirection;
		/// <summary>
		/// Specifies the amount of sprite fill
		/// </summary>
		public float fillAmount;
		/// <summary>
		/// Specifies the offset (in "pixels") to apply to all sprite vertices
		/// </summary>
		public Vector3 offset;
		/// <summary>
		/// Used when rendering into a <see cref="RenderData"/> buffer that 
		/// already contains geometry. This value is set internally and does 
		/// not need to be specified by the caller.
		/// </summary>
		public int baseIndex;
	}

}
