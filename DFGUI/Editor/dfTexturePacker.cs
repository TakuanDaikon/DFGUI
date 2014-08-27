using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

internal static class dfTexturePacker
{

	#region Public methods 

	public static Rect[] PackTextures2( this Texture2D texture, Texture2D[] textures, int padding, int maximumAtlasSize, bool forceSquare )
	{
		return PackTextures( texture, textures, padding, 256, 128, maximumAtlasSize, forceSquare );
	}

	#endregion

	#region Private utility methods 

	private static Rect[] PackTextures( Texture2D texture, Texture2D[] sprites, int padding, int width, int height, int maxSize, bool forceSquare )
	{

		if( ( width > maxSize && height < maxSize ) || ( height > maxSize && width < maxSize ) )
		{
			width = height = maxSize;
		}
		
		if( width > maxSize || height > maxSize )
			throw new InvalidOperationException( "Packed sprites exceed maximum atlas size" );

		if( forceSquare )
		{
			var max = Mathf.Max( width, height );
			width = max;
			height = max;
		}
		else if( height > width ) 
		{ 
			int temp = width; 
			width = height; 
			height = temp; 
		}

		MaxRectsBinPack bp = new MaxRectsBinPack( width, height, false );
		Rect[] rects = new Rect[ sprites.Length ];

		for( int i = 0; i < sprites.Length; i++ )
		{

			Texture2D sprite = sprites[ i ];
			var spriteWidth = sprite.width + padding;
			var spriteHeight = sprite.height + padding;

			var packingMethodConfig = (dfTexturePacker.dfTexturePackingMethod)EditorPrefs.GetInt( "DaikonForge.AtlasPackingMethod", (int)dfTexturePacker.dfTexturePackingMethod.RectBestAreaFit );
			Rect rect = bp.Insert( 
				spriteWidth, 
				spriteHeight,
				packingMethodConfig
			);

			// If the rect could not be packed into the current dimensions, 
			// increase the texture size.
			if( rect.width == 0 || rect.height == 0 )
			{
				return PackTextures( texture, sprites, padding, ( width <= height ? width << 1 : width ), ( height < width ? height << 1 : height ), maxSize, forceSquare );
			}

			rects[ i ] = rect;

		}

		// Check for max size
		if( width > maxSize || height > maxSize )
			throw new InvalidOperationException( "Packed sprites exceed maximum atlas size" );

		texture.Resize( width, height );
		texture.SetPixels( new Color[ width * height ] );

		var extrudeEdges = EditorPrefs.GetBool( "DaikonForge.AtlasExtrudeSprites", false ) && padding > 0;
		for( int i = 0; i < sprites.Length; i++ )
		{

			Texture2D sprite = sprites[ i ];
			Rect rect = rects[ i ];
			Color[] colors = sprite.GetPixels();

			#region Edge extrusion coded provided by Mistale - http://www.daikonforge.com/dfgui/forums/topic/dilate-texture-atlas/

			// Dilate border if padding is set
			if( extrudeEdges )
			{

				int w = (int)sprite.width;
				int h = (int)sprite.height;
				int xStart = (int)rect.x;
				int yStart = (int)rect.y;
				int xStop = xStart + w - 1;
				int yStop = yStart + h - 1;

				Color[] wColors;

				int halfPadding = (int)( padding / 2f );

				// Top border
				wColors = sprite.GetPixels( 0, 0, sprite.width, 1 );
				for( int p = 0; p < halfPadding && yStart - p >= 1; p++ ) 
					texture.SetPixels( xStart, yStart - p - 1, sprite.width, 1, wColors );

				// Bottom border
				wColors = sprite.GetPixels( 0, sprite.height - 1, sprite.width, 1 );
				for( int p = 0; p < halfPadding && yStop + p < texture.height - 1; p++ ) 
					texture.SetPixels( xStart, yStop + p + 1, sprite.width, 1, wColors );

				// Left border
				wColors = sprite.GetPixels( 0, 0, 1, sprite.height );
				for( int p = 0; p < halfPadding && xStart - p >= 1; p++ ) 
					texture.SetPixels( xStart - p - 1, yStart, 1, sprite.height, wColors );

				// Right border
				wColors = sprite.GetPixels( sprite.width - 1, 0, 1, sprite.height );
				for( int p = 0; p < halfPadding && xStop + p < texture.width - 1; p++ ) 
					texture.SetPixels( xStop + p + 1, yStart, 1, sprite.height, wColors );

			}

			#endregion

			texture.SetPixels(
				(int)rect.x,
				(int)rect.y,
				(int)sprite.width,
				(int)sprite.height,
				colors
			);

			rects[ i ] = new Rect(
				rect.x / width,
				rect.y / height,
				( rect.width - padding ) / width,
				( rect.height - padding ) / height
			);

		}

		return rects;

	}

	#endregion 

	#region Nested classes

	public enum dfTexturePackingMethod
	{
		/// <summary> Positions the rectangle against the short side of a free rectangle into which it fits the best </summary>
		RectBestShortSideFit,
		/// <summary> Positions the rectangle against the long side of a free rectangle into which it fits the best </summary>
		RectBestLongSideFit,
		/// <summary> Positions the rectangle into the smallest free rect into which it fits </summary>
		RectBestAreaFit,
		/// <summary> Does the Tetris placement </summary>
		RectBottomLeftRule,
		/// <summary>  the placement where the rectangle touches other rects as much as possible </summary>
		RectContactPointRule
	};

	private class MaxRectsBinPack
	{

		/*
			Based on the Public Domain MaxRectsBinPack.cpp source by Jukka Jylänki
			https://github.com/juj/RectangleBinPack/
 
			Ported to C# by Sven Magnus
			http://wiki.unity3d.com/index.php/MaxRectsBinPack
			This version is also public domain - do whatever you want with it.
		*/

		public int binWidth = 0;
		public int binHeight = 0;
		public bool allowRotations;

		public List<Rect> usedRectangles = new List<Rect>();
		public List<Rect> freeRectangles = new List<Rect>();

		public MaxRectsBinPack( int width, int height, bool rotations )
		{
			Init( width, height, rotations );
		}

		public void Init( int width, int height, bool rotations )
		{
			binWidth = width;
			binHeight = height;
			allowRotations = rotations;

			Rect n = new Rect();
			n.x = 0;
			n.y = 0;
			n.width = width;
			n.height = height;

			usedRectangles.Clear();

			freeRectangles.Clear();
			freeRectangles.Add( n );
		}

		public Rect Insert( int width, int height, dfTexturePackingMethod method )
		{
			Rect newNode = new Rect();
			int score1 = 0; // Unused in this function. We don't need to know the score after finding the position.
			int score2 = 0;
			switch( method )
			{
				case dfTexturePackingMethod.RectBestShortSideFit: newNode = FindPositionForNewNodeBestShortSideFit( width, height, ref score1, ref score2 ); break;
				case dfTexturePackingMethod.RectBottomLeftRule: newNode = FindPositionForNewNodeBottomLeft( width, height, ref score1, ref score2 ); break;
				case dfTexturePackingMethod.RectContactPointRule: newNode = FindPositionForNewNodeContactPoint( width, height, ref score1 ); break;
				case dfTexturePackingMethod.RectBestLongSideFit: newNode = FindPositionForNewNodeBestLongSideFit( width, height, ref score2, ref score1 ); break;
				case dfTexturePackingMethod.RectBestAreaFit: newNode = FindPositionForNewNodeBestAreaFit( width, height, ref score1, ref score2 ); break;
			}

			if( newNode.height == 0 )
				return newNode;

			int numRectanglesToProcess = freeRectangles.Count;
			for( int i = 0; i < numRectanglesToProcess; ++i )
			{
				if( SplitFreeNode( freeRectangles[ i ], ref newNode ) )
				{
					freeRectangles.RemoveAt( i );
					--i;
					--numRectanglesToProcess;
				}
			}

			PruneFreeList();

			usedRectangles.Add( newNode );
			return newNode;
		}

		public void Insert( List<Rect> rects, List<Rect> dst, dfTexturePackingMethod method )
		{
			dst.Clear();

			while( rects.Count > 0 )
			{
				int bestScore1 = int.MaxValue;
				int bestScore2 = int.MaxValue;
				int bestRectIndex = -1;
				Rect bestNode = new Rect();

				for( int i = 0; i < rects.Count; ++i )
				{
					int score1 = 0;
					int score2 = 0;
					Rect newNode = ScoreRect( (int)rects[ i ].width, (int)rects[ i ].height, method, ref score1, ref score2 );

					if( score1 < bestScore1 || ( score1 == bestScore1 && score2 < bestScore2 ) )
					{
						bestScore1 = score1;
						bestScore2 = score2;
						bestNode = newNode;
						bestRectIndex = i;
					}
				}

				if( bestRectIndex == -1 )
					return;

				PlaceRect( bestNode );
				rects.RemoveAt( bestRectIndex );
			}
		}

		void PlaceRect( Rect node )
		{
			int numRectanglesToProcess = freeRectangles.Count;
			for( int i = 0; i < numRectanglesToProcess; ++i )
			{
				if( SplitFreeNode( freeRectangles[ i ], ref node ) )
				{
					freeRectangles.RemoveAt( i );
					--i;
					--numRectanglesToProcess;
				}
			}

			PruneFreeList();

			usedRectangles.Add( node );
		}

		Rect ScoreRect( int width, int height, dfTexturePackingMethod method, ref int score1, ref int score2 )
		{
			Rect newNode = new Rect();
			score1 = int.MaxValue;
			score2 = int.MaxValue;
			switch( method )
			{
				case dfTexturePackingMethod.RectBestShortSideFit: newNode = FindPositionForNewNodeBestShortSideFit( width, height, ref score1, ref score2 ); break;
				case dfTexturePackingMethod.RectBottomLeftRule: newNode = FindPositionForNewNodeBottomLeft( width, height, ref score1, ref score2 ); break;
				case dfTexturePackingMethod.RectContactPointRule: newNode = FindPositionForNewNodeContactPoint( width, height, ref score1 );
					score1 = -score1; // Reverse since we are minimizing, but for contact point score bigger is better.
					break;
				case dfTexturePackingMethod.RectBestLongSideFit: newNode = FindPositionForNewNodeBestLongSideFit( width, height, ref score2, ref score1 ); break;
				case dfTexturePackingMethod.RectBestAreaFit: newNode = FindPositionForNewNodeBestAreaFit( width, height, ref score1, ref score2 ); break;
			}

			// Cannot fit the current rectangle.
			if( newNode.height == 0 )
			{
				score1 = int.MaxValue;
				score2 = int.MaxValue;
			}

			return newNode;
		}

		/// Computes the ratio of used surface area.
		public float Occupancy()
		{
			ulong usedSurfaceArea = 0;
			for( int i = 0; i < usedRectangles.Count; ++i )
				usedSurfaceArea += (uint)usedRectangles[ i ].width * (uint)usedRectangles[ i ].height;

			return (float)usedSurfaceArea / ( binWidth * binHeight );
		}

		Rect FindPositionForNewNodeBottomLeft( int width, int height, ref int bestY, ref int bestX )
		{
			Rect bestNode = new Rect();
			//memset(bestNode, 0, sizeof(Rect));

			bestY = int.MaxValue;

			for( int i = 0; i < freeRectangles.Count; ++i )
			{
				// Try to place the rectangle in upright (non-flipped) orientation.
				if( freeRectangles[ i ].width >= width && freeRectangles[ i ].height >= height )
				{
					int topSideY = (int)freeRectangles[ i ].y + height;
					if( topSideY < bestY || ( topSideY == bestY && freeRectangles[ i ].x < bestX ) )
					{
						bestNode.x = freeRectangles[ i ].x;
						bestNode.y = freeRectangles[ i ].y;
						bestNode.width = width;
						bestNode.height = height;
						bestY = topSideY;
						bestX = (int)freeRectangles[ i ].x;
					}
				}
				if( allowRotations && freeRectangles[ i ].width >= height && freeRectangles[ i ].height >= width )
				{
					int topSideY = (int)freeRectangles[ i ].y + width;
					if( topSideY < bestY || ( topSideY == bestY && freeRectangles[ i ].x < bestX ) )
					{
						bestNode.x = freeRectangles[ i ].x;
						bestNode.y = freeRectangles[ i ].y;
						bestNode.width = height;
						bestNode.height = width;
						bestY = topSideY;
						bestX = (int)freeRectangles[ i ].x;
					}
				}
			}
			return bestNode;
		}

		Rect FindPositionForNewNodeBestShortSideFit( int width, int height, ref int bestShortSideFit, ref int bestLongSideFit )
		{
			Rect bestNode = new Rect();
			//memset(&bestNode, 0, sizeof(Rect));

			bestShortSideFit = int.MaxValue;

			for( int i = 0; i < freeRectangles.Count; ++i )
			{
				// Try to place the rectangle in upright (non-flipped) orientation.
				if( freeRectangles[ i ].width >= width && freeRectangles[ i ].height >= height )
				{
					int leftoverHoriz = Mathf.Abs( (int)freeRectangles[ i ].width - width );
					int leftoverVert = Mathf.Abs( (int)freeRectangles[ i ].height - height );
					int shortSideFit = Mathf.Min( leftoverHoriz, leftoverVert );
					int longSideFit = Mathf.Max( leftoverHoriz, leftoverVert );

					if( shortSideFit < bestShortSideFit || ( shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit ) )
					{
						bestNode.x = freeRectangles[ i ].x;
						bestNode.y = freeRectangles[ i ].y;
						bestNode.width = width;
						bestNode.height = height;
						bestShortSideFit = shortSideFit;
						bestLongSideFit = longSideFit;
					}
				}

				if( allowRotations && freeRectangles[ i ].width >= height && freeRectangles[ i ].height >= width )
				{
					int flippedLeftoverHoriz = Mathf.Abs( (int)freeRectangles[ i ].width - height );
					int flippedLeftoverVert = Mathf.Abs( (int)freeRectangles[ i ].height - width );
					int flippedShortSideFit = Mathf.Min( flippedLeftoverHoriz, flippedLeftoverVert );
					int flippedLongSideFit = Mathf.Max( flippedLeftoverHoriz, flippedLeftoverVert );

					if( flippedShortSideFit < bestShortSideFit || ( flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit ) )
					{
						bestNode.x = freeRectangles[ i ].x;
						bestNode.y = freeRectangles[ i ].y;
						bestNode.width = height;
						bestNode.height = width;
						bestShortSideFit = flippedShortSideFit;
						bestLongSideFit = flippedLongSideFit;
					}
				}
			}
			return bestNode;
		}

		Rect FindPositionForNewNodeBestLongSideFit( int width, int height, ref int bestShortSideFit, ref int bestLongSideFit )
		{
			Rect bestNode = new Rect();
			//memset(&bestNode, 0, sizeof(Rect));

			bestLongSideFit = int.MaxValue;

			for( int i = 0; i < freeRectangles.Count; ++i )
			{
				// Try to place the rectangle in upright (non-flipped) orientation.
				if( freeRectangles[ i ].width >= width && freeRectangles[ i ].height >= height )
				{
					int leftoverHoriz = Mathf.Abs( (int)freeRectangles[ i ].width - width );
					int leftoverVert = Mathf.Abs( (int)freeRectangles[ i ].height - height );
					int shortSideFit = Mathf.Min( leftoverHoriz, leftoverVert );
					int longSideFit = Mathf.Max( leftoverHoriz, leftoverVert );

					if( longSideFit < bestLongSideFit || ( longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit ) )
					{
						bestNode.x = freeRectangles[ i ].x;
						bestNode.y = freeRectangles[ i ].y;
						bestNode.width = width;
						bestNode.height = height;
						bestShortSideFit = shortSideFit;
						bestLongSideFit = longSideFit;
					}
				}

				if( allowRotations && freeRectangles[ i ].width >= height && freeRectangles[ i ].height >= width )
				{
					int leftoverHoriz = Mathf.Abs( (int)freeRectangles[ i ].width - height );
					int leftoverVert = Mathf.Abs( (int)freeRectangles[ i ].height - width );
					int shortSideFit = Mathf.Min( leftoverHoriz, leftoverVert );
					int longSideFit = Mathf.Max( leftoverHoriz, leftoverVert );

					if( longSideFit < bestLongSideFit || ( longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit ) )
					{
						bestNode.x = freeRectangles[ i ].x;
						bestNode.y = freeRectangles[ i ].y;
						bestNode.width = height;
						bestNode.height = width;
						bestShortSideFit = shortSideFit;
						bestLongSideFit = longSideFit;
					}
				}
			}
			return bestNode;
		}

		Rect FindPositionForNewNodeBestAreaFit( int width, int height, ref int bestAreaFit, ref int bestShortSideFit )
		{
			Rect bestNode = new Rect();
			//memset(&bestNode, 0, sizeof(Rect));

			bestAreaFit = int.MaxValue;

			for( int i = 0; i < freeRectangles.Count; ++i )
			{
				int areaFit = (int)freeRectangles[ i ].width * (int)freeRectangles[ i ].height - width * height;

				// Try to place the rectangle in upright (non-flipped) orientation.
				if( freeRectangles[ i ].width >= width && freeRectangles[ i ].height >= height )
				{
					int leftoverHoriz = Mathf.Abs( (int)freeRectangles[ i ].width - width );
					int leftoverVert = Mathf.Abs( (int)freeRectangles[ i ].height - height );
					int shortSideFit = Mathf.Min( leftoverHoriz, leftoverVert );

					if( areaFit < bestAreaFit || ( areaFit == bestAreaFit && shortSideFit < bestShortSideFit ) )
					{
						bestNode.x = freeRectangles[ i ].x;
						bestNode.y = freeRectangles[ i ].y;
						bestNode.width = width;
						bestNode.height = height;
						bestShortSideFit = shortSideFit;
						bestAreaFit = areaFit;
					}
				}

				if( allowRotations && freeRectangles[ i ].width >= height && freeRectangles[ i ].height >= width )
				{
					int leftoverHoriz = Mathf.Abs( (int)freeRectangles[ i ].width - height );
					int leftoverVert = Mathf.Abs( (int)freeRectangles[ i ].height - width );
					int shortSideFit = Mathf.Min( leftoverHoriz, leftoverVert );

					if( areaFit < bestAreaFit || ( areaFit == bestAreaFit && shortSideFit < bestShortSideFit ) )
					{
						bestNode.x = freeRectangles[ i ].x;
						bestNode.y = freeRectangles[ i ].y;
						bestNode.width = height;
						bestNode.height = width;
						bestShortSideFit = shortSideFit;
						bestAreaFit = areaFit;
					}
				}
			}
			return bestNode;
		}

		/// Returns 0 if the two intervals i1 and i2 are disjoint, or the length of their overlap otherwise.
		int CommonIntervalLength( int i1start, int i1end, int i2start, int i2end )
		{
			if( i1end < i2start || i2end < i1start )
				return 0;
			return Mathf.Min( i1end, i2end ) - Mathf.Max( i1start, i2start );
		}

		int ContactPointScoreNode( int x, int y, int width, int height )
		{
			int score = 0;

			if( x == 0 || x + width == binWidth )
				score += height;
			if( y == 0 || y + height == binHeight )
				score += width;

			for( int i = 0; i < usedRectangles.Count; ++i )
			{
				if( usedRectangles[ i ].x == x + width || usedRectangles[ i ].x + usedRectangles[ i ].width == x )
					score += CommonIntervalLength( (int)usedRectangles[ i ].y, (int)usedRectangles[ i ].y + (int)usedRectangles[ i ].height, y, y + height );
				if( usedRectangles[ i ].y == y + height || usedRectangles[ i ].y + usedRectangles[ i ].height == y )
					score += CommonIntervalLength( (int)usedRectangles[ i ].x, (int)usedRectangles[ i ].x + (int)usedRectangles[ i ].width, x, x + width );
			}
			return score;
		}

		Rect FindPositionForNewNodeContactPoint( int width, int height, ref int bestContactScore )
		{
			Rect bestNode = new Rect();
			//memset(&bestNode, 0, sizeof(Rect));

			bestContactScore = -1;

			for( int i = 0; i < freeRectangles.Count; ++i )
			{
				// Try to place the rectangle in upright (non-flipped) orientation.
				if( freeRectangles[ i ].width >= width && freeRectangles[ i ].height >= height )
				{
					int score = ContactPointScoreNode( (int)freeRectangles[ i ].x, (int)freeRectangles[ i ].y, width, height );
					if( score > bestContactScore )
					{
						bestNode.x = (int)freeRectangles[ i ].x;
						bestNode.y = (int)freeRectangles[ i ].y;
						bestNode.width = width;
						bestNode.height = height;
						bestContactScore = score;
					}
				}
				if( allowRotations && freeRectangles[ i ].width >= height && freeRectangles[ i ].height >= width )
				{
					int score = ContactPointScoreNode( (int)freeRectangles[ i ].x, (int)freeRectangles[ i ].y, height, width );
					if( score > bestContactScore )
					{
						bestNode.x = (int)freeRectangles[ i ].x;
						bestNode.y = (int)freeRectangles[ i ].y;
						bestNode.width = height;
						bestNode.height = width;
						bestContactScore = score;
					}
				}
			}
			return bestNode;
		}

		bool SplitFreeNode( Rect freeNode, ref Rect usedNode )
		{
			// Test with SAT if the rectangles even intersect.
			if( usedNode.x >= freeNode.x + freeNode.width || usedNode.x + usedNode.width <= freeNode.x ||
				usedNode.y >= freeNode.y + freeNode.height || usedNode.y + usedNode.height <= freeNode.y )
				return false;

			if( usedNode.x < freeNode.x + freeNode.width && usedNode.x + usedNode.width > freeNode.x )
			{
				// New node at the top side of the used node.
				if( usedNode.y > freeNode.y && usedNode.y < freeNode.y + freeNode.height )
				{
					Rect newNode = freeNode;
					newNode.height = usedNode.y - newNode.y;
					freeRectangles.Add( newNode );
				}

				// New node at the bottom side of the used node.
				if( usedNode.y + usedNode.height < freeNode.y + freeNode.height )
				{
					Rect newNode = freeNode;
					newNode.y = usedNode.y + usedNode.height;
					newNode.height = freeNode.y + freeNode.height - ( usedNode.y + usedNode.height );
					freeRectangles.Add( newNode );
				}
			}

			if( usedNode.y < freeNode.y + freeNode.height && usedNode.y + usedNode.height > freeNode.y )
			{
				// New node at the left side of the used node.
				if( usedNode.x > freeNode.x && usedNode.x < freeNode.x + freeNode.width )
				{
					Rect newNode = freeNode;
					newNode.width = usedNode.x - newNode.x;
					freeRectangles.Add( newNode );
				}

				// New node at the right side of the used node.
				if( usedNode.x + usedNode.width < freeNode.x + freeNode.width )
				{
					Rect newNode = freeNode;
					newNode.x = usedNode.x + usedNode.width;
					newNode.width = freeNode.x + freeNode.width - ( usedNode.x + usedNode.width );
					freeRectangles.Add( newNode );
				}
			}

			return true;
		}

		void PruneFreeList()
		{
			for( int i = 0; i < freeRectangles.Count; ++i )
				for( int j = i + 1; j < freeRectangles.Count; ++j )
				{
					if( IsContainedIn( freeRectangles[ i ], freeRectangles[ j ] ) )
					{
						freeRectangles.RemoveAt( i );
						--i;
						break;
					}
					if( IsContainedIn( freeRectangles[ j ], freeRectangles[ i ] ) )
					{
						freeRectangles.RemoveAt( j );
						--j;
					}
				}
		}

		bool IsContainedIn( Rect a, Rect b )
		{
			return a.x >= b.x && a.y >= b.y
				&& a.x + a.width <= b.x + b.width
				&& a.y + a.height <= b.y + b.height;
		}

	}

	#endregion 

}
