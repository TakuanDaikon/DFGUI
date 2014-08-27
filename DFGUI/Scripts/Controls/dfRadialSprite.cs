/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implements a <see cref="dfSprite"/> class that displays an image
/// from a Texture Atlas on the screen, but can be filled in a radial 
/// fashion instead of strictly horizontally or vertically like other
/// sprite classes. Useful for spell cooldown effects, map effects, etc.
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Implements a sprite that can be filled in a radial fashion instead of strictly horizontally or vertically like other sprite classes. Useful for spell cooldown effects, map effects, etc." )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_radial_sprite.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Sprite/Radial" )]
public class dfRadialSprite : dfSprite
{

	#region Static variables

	private static Vector3[] baseVerts = new Vector3[]
{
	new Vector3(  0.0f,  0.5f, 0.0f ),	// 0 Top center
	new Vector3(  0.5f,  0.5f, 0.0f ),	// 1 Top right
	new Vector3(  0.5f,  0.0f, 0.0f ),	// 2 Middle right
	new Vector3(  0.5f, -0.5f, 0.0f ),	// 3 Bottom right
	new Vector3(  0.0f, -0.5f, 0.0f ),	// 4 Bottom center
	new Vector3( -0.5f, -0.5f, 0.0f ),	// 5 Bottom left
	new Vector3( -0.5f,  0.0f, 0.0f ),	// 6 Middle left
	new Vector3( -0.5f,  0.5f, 0.0f ),	// 7 Top left
};

	#endregion

	#region Serialized private fields

	[SerializeField]
	protected dfPivotPoint fillOrigin = dfPivotPoint.MiddleCenter;

	#endregion

	/// <summary>
	/// Specifies the anchor point from which the radial fill will originate
	/// </summary>
	public dfPivotPoint FillOrigin
	{
		get { return this.fillOrigin; }
		set
		{
			if( value != this.fillOrigin )
			{
				this.fillOrigin = value;
				this.Invalidate();
			}
		}
	}

	protected override void OnRebuildRenderData()
	{

		if( Atlas == null )
			return;

		var spriteInfo = SpriteInfo;
		if( spriteInfo == null )
			return;

		renderData.Material = Atlas.Material;

		List<Vector3> verts = null;
		List<int> triangles = null;
		List<Vector2> uv = null;

		buildMeshData( ref verts, ref triangles, ref uv );

		var colors = buildColors( verts.Count );

		renderData.Vertices.AddRange( verts );
		renderData.Triangles.AddRange( triangles );
		renderData.UV.AddRange( uv );
		renderData.Colors.AddRange( colors );

	}

	private void buildMeshData( ref List<Vector3> verts, ref List<int> indices, ref List<Vector2> uv )
	{

		var v = verts = new List<Vector3>();
		verts.AddRange( baseVerts );

		#region Assign start vert to pivot point and eliminate unused verts

		var numTriangles = 8;
		var startVert = -1;
		switch( fillOrigin )
		{
			case dfPivotPoint.TopLeft:
				numTriangles = 4;
				startVert = 5;
				v.RemoveAt( 6 );
				v.RemoveAt( 0 );
				break;
			case dfPivotPoint.TopCenter:
				numTriangles = 6;
				startVert = 0;
				break;
			case dfPivotPoint.TopRight:
				numTriangles = 4;
				startVert = 0;
				v.RemoveAt( 2 );
				v.RemoveAt( 0 );
				break;
			case dfPivotPoint.MiddleLeft:
				numTriangles = 6;
				startVert = 6;
				break;
			case dfPivotPoint.MiddleRight:
				numTriangles = 6;
				startVert = 2;
				break;
			case dfPivotPoint.BottomLeft:
				numTriangles = 4;
				startVert = 4;
				v.RemoveAt( 6 );
				v.RemoveAt( 4 );
				break;
			case dfPivotPoint.BottomCenter:
				numTriangles = 6;
				startVert = 4;
				break;
			case dfPivotPoint.BottomRight:
				numTriangles = 4;
				startVert = 2;
				v.RemoveAt( 4 );
				v.RemoveAt( 2 );
				break;
			case dfPivotPoint.MiddleCenter:
				numTriangles = 8;
				v.Add( v[ 0 ] ); // Necessary to complete the border
				v.Insert( 0, Vector3.zero );
				startVert = 0;
				break;
			default:
				throw new NotImplementedException();
		}

		#endregion

		// Make sure that the vertex list starts with the point closest to the pivot
		makeFirst( v, startVert );

		// Build list of triangle indices
		var t = indices = buildTriangles( v );

		var stepSize = 1f / numTriangles;
		var lastStep = fillAmount.Quantize( stepSize );
		var numSteps = Mathf.CeilToInt( lastStep / stepSize ) + 1;

		// Remove entire triangles that will not be needed
		var removeSteps = numSteps;
		while( removeSteps < numTriangles )
		{

			if( invertFill )
			{
				t.RemoveRange( 0, 3 );
			}
			else
			{
				v.RemoveAt( v.Count - 1 );
				t.RemoveRange( t.Count - 3, 3 );
			}

			removeSteps += 1;

		}

		if( fillAmount < 1f )
		{

			var startIndex = t[ invertFill ? 2 : t.Count - 2 ];
			var endIndex = t[ invertFill ? 1 : t.Count - 1 ];

			var pos = ( FillAmount - lastStep ) / stepSize;

			v[ endIndex ] = Vector3.Lerp( v[ startIndex ], v[ endIndex ], pos );

		}

		// Calculate UV coords based on the vertices (which are still in normalized 
		// offset space) before adjusting them to world space
		uv = buildUV( v );

		#region Convert vertices to world space

		var p2u = PixelsToUnits();
		var scale = p2u * this.size;

		var offset = pivot.TransformToCenter( size ) * p2u;

		for( int i = 0; i < v.Count; i++ )
		{
			v[ i ] = ( Vector3.Scale( v[ i ], scale ) + offset );
		}

		#endregion

	}

	private void makeFirst( List<Vector3> list, int index )
	{

		if( index == 0 )
			return;

		var temp = list.GetRange( index, list.Count - index );
		list.RemoveRange( index, list.Count - index );
		list.InsertRange( 0, temp );

	}

	private List<int> buildTriangles( List<Vector3> verts )
	{

		var triangles = new List<int>();

		var count = verts.Count;
		for( int i = 1; i < count - 1; i++ )
		{
			triangles.Add( 0 );
			triangles.Add( i );
			triangles.Add( i + 1 );
		}

		return triangles;

	}

	private List<Vector2> buildUV( List<Vector3> verts )
	{

		var spriteInfo = SpriteInfo;
		if( spriteInfo == null )
			return null;

		var rect = spriteInfo.region;

		#region Flip texture if needed

		if( flip.IsSet( dfSpriteFlip.FlipHorizontal ) )
		{
			rect = new Rect( rect.xMax, rect.y, -rect.width, rect.height );
		}
		if( flip.IsSet( dfSpriteFlip.FlipVertical ) )
		{
			rect = new Rect( rect.x, rect.yMax, rect.width, -rect.height );
		}

		#endregion

		var offset = new Vector2( rect.x, rect.y );
		var normalized = new Vector2( 0.5f, 0.5f );
		var scale = new Vector2( rect.width, rect.height );

		var uv = new List<Vector2>( verts.Count );
		for( int i = 0; i < verts.Count; i++ )
		{
			var temp = (Vector2)verts[ i ] + normalized;
			uv.Add( Vector2.Scale( temp, scale ) + offset );
		}

		return uv;

	}

	private Color32[] buildColors( int vertCount )
	{

		var color = ApplyOpacity( IsEnabled ? this.color : this.disabledColor );

		var colors = new Color32[ vertCount ];
		for( int i = 0; i < colors.Length; i++ )
			colors[ i ] = color;

		return colors;

	}

}
