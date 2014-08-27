/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implements a Sprite that can be tiled horizontally and vertically
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Implements a Sprite that can be tiled horizontally and vertically" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_tiled_sprite.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Sprite/Tiled" )]
public class dfTiledSprite : dfSprite
{

	#region Static variables

	private static int[] quadTriangles = new int[] { 0, 1, 3, 3, 1, 2 };
	private static Vector2[] quadUV = new Vector2[ 4 ];

	#endregion

	#region Serialized protected members

	[SerializeField]
	protected Vector2 tileScale = Vector2.one;

	[SerializeField]
	protected Vector2 tileScroll = Vector2.zero;

	#endregion

	#region Public properties

	/// <summary>
	/// Gets or sets the scale multiplier that will be applied to the
	/// pattern when rendered
	/// </summary>
	public Vector2 TileScale
	{
		get { return this.tileScale; }
		set
		{
			if( Vector2.Distance( value, tileScale ) > float.Epsilon )
			{
				tileScale = Vector2.Max( Vector2.one * 0.1f, value );
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the offset that will be applied to the pattern 
	/// when rendered. The value of each component (x,y) should be 
	/// between 0 and 1, and values outside of this range will be 
	/// adjusted or "wrapped". 
	/// </summary>
	public Vector2 TileScroll
	{
		get { return this.tileScroll; }
		set
		{
			if( Vector2.Distance( value, tileScroll ) > float.Epsilon )
			{
				tileScroll = value; // new Vector2( Mathf.Max( 0, value.x ), Mathf.Max( 0, value.y ) );
				Invalidate();
			}
		}
	}

	#endregion

	protected override void OnRebuildRenderData()
	{

		if( Atlas == null )
			return;

		var spriteInfo = SpriteInfo;
		if( spriteInfo == null )
			return;

		renderData.Material = Atlas.Material;

		var verts = renderData.Vertices;
		var uv = renderData.UV;
		var colors = renderData.Colors;
		var triangles = renderData.Triangles;

		var quadUV = buildQuadUV();

		var tileSize = Vector2.Scale( spriteInfo.sizeInPixels, tileScale );
		var tileOffset = new Vector2( ( tileScroll.x % 1f ), ( tileScroll.y % 1f ) );

		var y = -Mathf.Abs( tileOffset.y * tileSize.y );
		while( y < size.y )
		{

			var x = -Mathf.Abs( tileOffset.x * tileSize.x );
			while( x < size.x )
			{

				var baseIndex = verts.Count;

				verts.Add( new Vector3( x, -y ) );
				verts.Add( new Vector3( x + tileSize.x, -y ) );
				verts.Add( new Vector3( x + tileSize.x, -y + -tileSize.y ) );
				verts.Add( new Vector3( x, -y + -tileSize.y ) );

				addQuadTriangles( triangles, baseIndex );
				addQuadUV( uv, quadUV );
				addQuadColors( colors );

				x += tileSize.x;

			}

			y += tileSize.y;

		}

		// Clip the quads *before* scaling, as it's easier to deal with untranslated
		// pixel coordinates
		clipQuads( verts, uv );

		var p2u = PixelsToUnits();
		var pivotOffset = pivot.TransformToUpperLeft( size );

		for( int i = 0; i < verts.Count; i++ )
		{
			verts[ i ] = ( verts[ i ] + (Vector3)pivotOffset ) * p2u;
		}

	}

	private void clipQuads( dfList<Vector3> verts, dfList<Vector2> uv )
	{

		var minX = 0f;
		var maxX = size.x;
		var minY = -size.y;
		var maxY = 0f;

		if( fillAmount < 1f )
		{
			if( fillDirection == dfFillDirection.Horizontal )
			{
				if( !invertFill )
					maxX = size.x * fillAmount;
				else
					minX = size.x - size.x * fillAmount;
			}
			else
			{
				if( !invertFill )
					minY = -size.y * fillAmount;
				else
					maxY = -size.y * ( 1f - fillAmount );
			}
		}

		for( int i = 0; i < verts.Count; i += 4 )
		{

			var ul = verts[ i + 0 ];
			var ur = verts[ i + 1 ];
			var br = verts[ i + 2 ];
			var bl = verts[ i + 3 ];

			var w = ur.x - ul.x;
			var h = ul.y - bl.y;

			if( ul.x < minX )
			{

				var clip = ( minX - ul.x ) / w;

				verts[ i + 0 ] = ul = new Vector3( Mathf.Max( minX, ul.x ), ul.y, ul.z );
				verts[ i + 1 ] = ur = new Vector3( Mathf.Max( minX, ur.x ), ur.y, ur.z );
				verts[ i + 2 ] = br = new Vector3( Mathf.Max( minX, br.x ), br.y, br.z );
				verts[ i + 3 ] = bl = new Vector3( Mathf.Max( minX, bl.x ), bl.y, bl.z );

				var uvx = Mathf.Lerp( uv[ i ].x, uv[ i + 1 ].x, clip );
				uv[ i + 0 ] = new Vector2( uvx, uv[ i ].y );
				uv[ i + 3 ] = new Vector2( uvx, uv[ i + 3 ].y );

				w = ur.x - ul.x;

			}

			if( ur.x > maxX )
			{

				var clip = 1f - ( ( maxX - ur.x + w ) / w );

				verts[ i + 0 ] = ul = new Vector3( Mathf.Min( ul.x, maxX ), ul.y, ul.z );
				verts[ i + 1 ] = ur = new Vector3( Mathf.Min( ur.x, maxX ), ur.y, ur.z );
				verts[ i + 2 ] = br = new Vector3( Mathf.Min( br.x, maxX ), br.y, br.z );
				verts[ i + 3 ] = bl = new Vector3( Mathf.Min( bl.x, maxX ), bl.y, bl.z );

				var uvx = Mathf.Lerp( uv[ i + 1 ].x, uv[ i ].x, clip );
				uv[ i + 1 ] = new Vector2( uvx, uv[ i + 1 ].y );
				uv[ i + 2 ] = new Vector2( uvx, uv[ i + 2 ].y );

				w = ur.x - ul.x;

			}

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

				verts[ i + 0 ] = ul = new Vector3( ul.x, Mathf.Min( maxY, ul.y ), ul.z );
				verts[ i + 1 ] = ur = new Vector3( ur.x, Mathf.Min( maxY, ur.y ), ur.z );
				verts[ i + 2 ] = br = new Vector3( br.x, Mathf.Min( maxY, br.y ), br.z );
				verts[ i + 3 ] = bl = new Vector3( bl.x, Mathf.Min( maxY, bl.y ), bl.z );

				var uvy = Mathf.Lerp( uv[ i ].y, uv[ i + 3 ].y, clip );
				uv[ i ] = new Vector2( uv[ i ].x, uvy );
				uv[ i + 1 ] = new Vector2( uv[ i + 1 ].x, uvy );

			}

		}

	}

	private void addQuadTriangles( dfList<int> triangles, int baseIndex )
	{
		for( int i = 0; i < quadTriangles.Length; i++ )
		{
			triangles.Add( quadTriangles[ i ] + baseIndex );
		}
	}

	private void addQuadColors( dfList<Color32> colors )
	{
		colors.EnsureCapacity( colors.Count + 4 );
		var color = ApplyOpacity( IsEnabled ? this.color : this.disabledColor );
		for( int i = 0; i < 4; i++ )
		{
			colors.Add( color );
		}
	}

	private void addQuadUV( dfList<Vector2> uv, Vector2[] spriteUV )
	{
		uv.AddRange( spriteUV );
	}

	private Vector2[] buildQuadUV()
	{

		var spriteInfo = SpriteInfo;
		var rect = spriteInfo.region;

		quadUV[ 0 ] = new Vector2( rect.x, rect.yMax );
		quadUV[ 1 ] = new Vector2( rect.xMax, rect.yMax );
		quadUV[ 2 ] = new Vector2( rect.xMax, rect.y );
		quadUV[ 3 ] = new Vector2( rect.x, rect.y );

		var temp = Vector2.zero;

		if( flip.IsSet( dfSpriteFlip.FlipHorizontal ) )
		{
			temp = quadUV[ 1 ];
			quadUV[ 1 ] = quadUV[ 0 ];
			quadUV[ 0 ] = temp;
			temp = quadUV[ 3 ];
			quadUV[ 3 ] = quadUV[ 2 ];
			quadUV[ 2 ] = temp;
		}

		if( flip.IsSet( dfSpriteFlip.FlipVertical ) )
		{
			temp = quadUV[ 0 ];
			quadUV[ 0 ] = quadUV[ 3 ];
			quadUV[ 3 ] = temp;
			temp = quadUV[ 1 ];
			quadUV[ 1 ] = quadUV[ 2 ];
			quadUV[ 2 ] = temp;
		}

		return quadUV;

	}

}
