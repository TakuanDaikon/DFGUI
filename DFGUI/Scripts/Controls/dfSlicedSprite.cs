/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Displays a sprite from a Texture Atlas using 9-slice scaling
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Displays a sprite from a Texture Atlas using 9-slice scaling" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_sliced_sprite.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Sprite/Sliced" )]
public class dfSlicedSprite : dfSprite
{

	#region Static constants

	private static int[] triangleIndices = new int[]
{
	0, 1, 2, 2, 3, 0,			// Top left quad
	4, 5, 6, 6, 7, 4,			// Top right quad
	8, 9, 10, 10, 11, 8,		// Bottom left quad
	12, 13, 14, 14, 15, 12,		// Bottom right quad
	1, 4, 7, 7, 2, 1,			// Top middle quad
	9, 12, 15, 15, 10, 9,		// Bottom middle quad
	3, 2, 9, 9, 8, 3,			// Left middle quad
	7, 6, 13, 13, 12, 7,		// Right middle quad
	2, 7, 12, 12, 9, 2			// Center quad
};

	private static int[][] horzFill = new int[][]
{
	new int[] { 0, 1, 4, 5 },
	new int[] { 3, 2, 7, 6 },
	new int[] { 8, 9, 12, 13 },
	new int[] { 11, 10, 15, 14 }
};

	private static int[][] vertFill = new int[][]
{
	new int[] { 11, 8, 3, 0 },
	new int[] { 10, 9, 2, 1 },
	new int[] { 15, 12, 7, 4 },
	new int[] { 14, 13, 6,  5 }
};

	private static int[][] fillIndices = new int[][]
{
	new int[] { 0, 0, 0, 0 },
	new int[] { 0, 0, 0, 0 },
	new int[] { 0, 0, 0, 0 },
	new int[] { 0, 0, 0, 0 }
};

	private static Vector3[] verts = new Vector3[ 16 ];
	private static Vector2[] uv = new Vector2[ 16 ];

	#endregion

	protected override void OnRebuildRenderData()
	{

		if( Atlas == null )
			return;

		var spriteInfo = SpriteInfo;
		if( spriteInfo == null )
		{
			return;
		}

		renderData.Material = Atlas.Material;

		if( spriteInfo.border.horizontal == 0 && spriteInfo.border.vertical == 0 )
		{
			base.OnRebuildRenderData();
			return;
		}

		var color = ApplyOpacity( IsEnabled ? this.color : this.disabledColor );
		var options = new RenderOptions()
		{
			atlas = atlas,
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

	internal new static void renderSprite( dfRenderData renderData, RenderOptions options )
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

		options.baseIndex = renderData.Vertices.Count;

		rebuildTriangles( renderData, options );
		rebuildVertices( renderData, options );
		rebuildUV( renderData, options );
		rebuildColors( renderData, options );

		if( options.fillAmount < 1f - float.Epsilon )
		{
			doFill( renderData, options );
		}

	}

	private static void rebuildTriangles( dfRenderData renderData, RenderOptions options )
	{

		var baseIndex = options.baseIndex;

		var triangles = renderData.Triangles;
		for( int i = 0; i < triangleIndices.Length; i++ )
		{
			triangles.Add( baseIndex + triangleIndices[ i ] );
		}

	}

	private static void doFill( dfRenderData renderData, RenderOptions options )
	{

		var baseIndex = options.baseIndex;
		var verts = renderData.Vertices;
		var uv = renderData.UV;

		var rows = getFillIndices( options.fillDirection, baseIndex );

		// HACK: The code performs as originally written vertical fills in 
		// the opposite direction as other dfSprite classes, and I didn't feel 
		// like refactoring the code so I'm just punting on the issue for now.
		var invertFill = options.invertFill;
		if( options.fillDirection == dfFillDirection.Vertical )
		{
			invertFill = !invertFill;
		}

		if( invertFill )
		{
			for( int i = 0; i < rows.Length; i++ )
			{
				Array.Reverse( rows[ i ] );
			}
		}

		var element = ( options.fillDirection == dfFillDirection.Horizontal ) ? 0 : 1;

		var vertStart = verts[ rows[ 0 ][ !invertFill ? 0 : 3 ] ][ element ];
		var vertEnd = verts[ rows[ 0 ][ !invertFill ? 3 : 0 ] ][ element ];
		var vertRange = Mathf.Abs( vertEnd - vertStart );
		var vertLimit = !invertFill
			? vertStart + options.fillAmount * vertRange
			: vertEnd - options.fillAmount * vertRange;

		for( int x = 0; x < rows.Length; x++ )
		{

			if( !invertFill )
			{

				for( int i = 3; i > 0; i-- )
				{

					var vert = verts[ rows[ x ][ i ] ][ element ];

					if( vert < vertLimit )
						continue;

					var vtemp = verts[ rows[ x ][ i ] ];
					vtemp[ element ] = vertLimit;
					verts[ rows[ x ][ i ] ] = vtemp;

					var prev = verts[ rows[ x ][ i - 1 ] ][ element ];
					if( prev > vertLimit )
						continue;

					var segRange = ( vert - prev );
					var segFill = ( vertLimit - prev ) / segRange;

					var uvEnd = uv[ rows[ x ][ i ] ][ element ];
					var uvStart = uv[ rows[ x ][ i - 1 ] ][ element ];

					var uvtemp = uv[ rows[ x ][ i ] ];
					uvtemp[ element ] = Mathf.Lerp( uvStart, uvEnd, segFill );
					uv[ rows[ x ][ i ] ] = uvtemp;

				}

			}
			else
			{

				for( int i = 1; i < 4; i++ )
				{

					var vert = verts[ rows[ x ][ i ] ][ element ];

					if( vert > vertLimit )
						continue;

					var vtemp = verts[ rows[ x ][ i ] ];
					vtemp[ element ] = vertLimit;
					verts[ rows[ x ][ i ] ] = vtemp;

					var prev = verts[ rows[ x ][ i - 1 ] ][ element ];
					if( prev < vertLimit )
						continue;

					var segRange = ( vert - prev );
					var segFill = ( vertLimit - prev ) / segRange;

					var uvEnd = uv[ rows[ x ][ i ] ][ element ];
					var uvStart = uv[ rows[ x ][ i - 1 ] ][ element ];

					var uvtemp = uv[ rows[ x ][ i ] ];
					uvtemp[ element ] = Mathf.Lerp( uvStart, uvEnd, segFill );
					uv[ rows[ x ][ i ] ] = uvtemp;

				}

			}

		}

	}

	private static int[][] getFillIndices( dfFillDirection fillDirection, int baseIndex )
	{

		var baseIndices = ( fillDirection == dfFillDirection.Horizontal ) ? horzFill : vertFill;

		for( int x = 0; x < 4; x++ )
		{
			for( int y = 0; y < 4; y++ )
			{
				fillIndices[ x ][ y ] = baseIndex + baseIndices[ x ][ y ];
			}
		}

		return fillIndices;

	}

	private static void rebuildVertices( dfRenderData renderData, RenderOptions options )
	{

		float meshLeft = 0;
		float meshTop = 0;
		float meshRight = Mathf.Ceil( options.size.x );
		float meshBottom = Mathf.Ceil( -options.size.y );

		#region Borders

		var spriteInfo = options.spriteInfo;
		float borderLeft = spriteInfo.border.left;
		float borderTop = spriteInfo.border.top;
		float borderRight = spriteInfo.border.right;
		float borderBottom = spriteInfo.border.bottom;

		if( options.flip.IsSet( dfSpriteFlip.FlipHorizontal ) )
		{
			float temp = borderRight;
			borderRight = borderLeft;
			borderLeft = temp;
		}

		if( options.flip.IsSet( dfSpriteFlip.FlipVertical ) )
		{
			float temp = borderBottom;
			borderBottom = borderTop;
			borderTop = temp;
		}

		#endregion

		// Top left corner
		verts[ 0 ] = new Vector3( meshLeft, meshTop, 0 ) + options.offset;
		verts[ 1 ] = verts[ 0 ] + new Vector3( borderLeft, 0, 0 );
		verts[ 2 ] = verts[ 0 ] + new Vector3( borderLeft, -borderTop, 0 );
		verts[ 3 ] = verts[ 0 ] + new Vector3( 0, -borderTop, 0 );

		// Top right corner
		verts[ 4 ] = new Vector3( meshRight - borderRight, meshTop, 0 ) + options.offset;
		verts[ 5 ] = verts[ 4 ] + new Vector3( borderRight, 0, 0 );
		verts[ 6 ] = verts[ 4 ] + new Vector3( borderRight, -borderTop, 0 );
		verts[ 7 ] = verts[ 4 ] + new Vector3( 0, -borderTop, 0 );

		// Bottom left corner
		verts[ 8 ] = new Vector3( meshLeft, meshBottom + borderBottom, 0 ) + options.offset;
		verts[ 9 ] = verts[ 8 ] + new Vector3( borderLeft, 0, 0 );
		verts[ 10 ] = verts[ 8 ] + new Vector3( borderLeft, -borderBottom, 0 );
		verts[ 11 ] = verts[ 8 ] + new Vector3( 0, -borderBottom, 0 );

		// Bottom right corner
		verts[ 12 ] = new Vector3( meshRight - borderRight, meshBottom + borderBottom, 0 ) + options.offset;
		verts[ 13 ] = verts[ 12 ] + new Vector3( borderRight, 0, 0 );
		verts[ 14 ] = verts[ 12 ] + new Vector3( borderRight, -borderBottom, 0 );
		verts[ 15 ] = verts[ 12 ] + new Vector3( 0, -borderBottom, 0 );

		for( int i = 0; i < verts.Length; i++ )
		{
			renderData.Vertices.Add( ( verts[ i ] * options.pixelsToUnits ).Quantize( options.pixelsToUnits ) );
		}

	}

	private static void rebuildUV( dfRenderData renderData, RenderOptions options )
	{

		var atlas = options.atlas;
		var textureSize = new Vector2( atlas.Texture.width, atlas.Texture.height );

		var spriteInfo = options.spriteInfo;
		float offsetTop = spriteInfo.border.top / textureSize.y;
		float offsetBottom = spriteInfo.border.bottom / textureSize.y;
		float offsetLeft = spriteInfo.border.left / textureSize.x;
		float offsetRight = spriteInfo.border.right / textureSize.x;

		var rect = spriteInfo.region;

		// Top left corner
		uv[ 0 ] = new Vector2( rect.x, rect.yMax );
		uv[ 1 ] = new Vector2( rect.x + offsetLeft, rect.yMax );
		uv[ 2 ] = new Vector2( rect.x + offsetLeft, rect.yMax - offsetTop );
		uv[ 3 ] = new Vector2( rect.x, rect.yMax - offsetTop );

		// Top right corner
		uv[ 4 ] = new Vector2( rect.xMax - offsetRight, rect.yMax );
		uv[ 5 ] = new Vector2( rect.xMax, rect.yMax );
		uv[ 6 ] = new Vector2( rect.xMax, rect.yMax - offsetTop );
		uv[ 7 ] = new Vector2( rect.xMax - offsetRight, rect.yMax - offsetTop );

		// Bottom left corner
		uv[ 8 ] = new Vector2( rect.x, rect.y + offsetBottom );
		uv[ 9 ] = new Vector2( rect.x + offsetLeft, rect.y + offsetBottom );
		uv[ 10 ] = new Vector2( rect.x + offsetLeft, rect.y );
		uv[ 11 ] = new Vector2( rect.x, rect.y );

		// Bottom right corner
		uv[ 12 ] = new Vector2( rect.xMax - offsetRight, rect.y + offsetBottom );
		uv[ 13 ] = new Vector2( rect.xMax, rect.y + offsetBottom );
		uv[ 14 ] = new Vector2( rect.xMax, rect.y );
		uv[ 15 ] = new Vector2( rect.xMax - offsetRight, rect.y );

		#region Flip UV if requested

		if( options.flip != dfSpriteFlip.None )
		{

			for( int i = 0; i < uv.Length; i += 4 )
			{

				Vector2 temp = Vector2.zero;

				if( options.flip.IsSet( dfSpriteFlip.FlipHorizontal ) )
				{
					temp = uv[ i + 0 ];
					uv[ i + 0 ] = uv[ i + 1 ];
					uv[ i + 1 ] = temp;
					temp = uv[ i + 2 ];
					uv[ i + 2 ] = uv[ i + 3 ];
					uv[ i + 3 ] = temp;
				}

				if( options.flip.IsSet( dfSpriteFlip.FlipVertical ) )
				{
					temp = uv[ i + 0 ];
					uv[ i + 0 ] = uv[ i + 3 ];
					uv[ i + 3 ] = temp;
					temp = uv[ i + 1 ];
					uv[ i + 1 ] = uv[ i + 2 ];
					uv[ i + 2 ] = temp;
				}

			}

			if( options.flip.IsSet( dfSpriteFlip.FlipHorizontal ) )
			{

				var th = new Vector2[ uv.Length ];
				Array.Copy( uv, th, uv.Length );

				// Swap top-left and top-right corners
				Array.Copy( uv, 0, uv, 4, 4 );
				Array.Copy( th, 4, uv, 0, 4 );

				// Swap bottom-left and bottom-right corners
				Array.Copy( uv, 8, uv, 12, 4 );
				Array.Copy( th, 12, uv, 8, 4 );

			}

			if( options.flip.IsSet( dfSpriteFlip.FlipVertical ) )
			{

				var tv = new Vector2[ uv.Length ];
				Array.Copy( uv, tv, uv.Length );

				// Swap top-left and bottom-left corners
				Array.Copy( uv, 0, uv, 8, 4 );
				Array.Copy( tv, 8, uv, 0, 4 );

				// Swap top-right and bottom-right corners
				Array.Copy( uv, 4, uv, 12, 4 );
				Array.Copy( tv, 12, uv, 4, 4 );

			}

		}

		#endregion

		for( int i = 0; i < uv.Length; i++ )
		{
			renderData.UV.Add( uv[ i ] );
		}

	}

	private static void rebuildColors( dfRenderData renderData, RenderOptions options )
	{
		for( int i = 0; i < 16; i++ )
		{
			renderData.Colors.Add( options.color );
		}
	}

}
