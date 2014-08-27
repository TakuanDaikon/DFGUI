/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Provides triangle clipping functionality for the <see cref="dfGUIManager"/> class
/// </summary>
// @private
internal class dfClippingUtil
{

	private static int[] inside = new int[ 3 ];

	private static ClipTriangle[] clipSource;
	private static ClipTriangle[] clipDest;

	static dfClippingUtil()
	{
		// Create working buffers that should be large enough to clip 
		// a single triangle against a reasonably large number of 
		// planes.
		clipSource = initClipBuffer( 1024 );
		clipDest = initClipBuffer( 1024 );
	}

	/// <summary>
	/// Clips a <see cref="dfRenderData"/> instance containing control rendering data
	/// against a list of <see cref="Plane"/> objects defined by the current clipping 
	/// region, and outputs the clipped data into <paramref name="dest"/>
	/// </summary>
	/// <param name="planes">The list of planes to clip against</param>
	/// <param name="source">The control rendering data to be clipped</param>
	/// <param name="dest">The output buffer that will hold the resulting clipped data</param>
	public static void Clip( IList<Plane> planes, dfRenderData source, dfRenderData dest )
	{

		dest.EnsureCapacity( dest.Vertices.Count + source.Vertices.Count );

		var triangleCount = source.Triangles.Count;
		var vertices = source.Vertices.Items;
		var triangles = source.Triangles.Items;
		var uvs = source.UV.Items;
		var colors = source.Colors.Items;
		var transform = source.Transform;

		var planeCount = planes.Count;

		for( int sourceIndex = 0; sourceIndex < triangleCount; sourceIndex += 3 )
		{

			for( int i = 0; i < 3; i++ )
			{

				var index = triangles[ sourceIndex + i ];

				clipSource[ 0 ].corner[ i ] = transform.MultiplyPoint( vertices[ index ] );
				clipSource[ 0 ].uv[ i ] = uvs[ index ];
				clipSource[ 0 ].color[ i ] = colors[ index ];

			}

			var count = 1;
			for( int planeIndex = 0; planeIndex < planeCount; planeIndex++ )
			{

				var clipPlane = planes[ planeIndex ];
				count = clipToPlane( ref clipPlane, clipSource, clipDest, count );

				var temp = clipSource;
				clipSource = clipDest;
				clipDest = temp;

			}

			for( int i = 0; i < count; i++ )
			{
				clipSource[ i ].CopyTo( dest );
			}

		}

	}

	private static int clipToPlane( ref Plane plane, ClipTriangle[] source, ClipTriangle[] dest, int count )
	{

		var newCount = 0;
		for( int i = 0; i < count; i++ )
		{
			newCount += clipToPlane( ref plane, ref source[ i ], dest, newCount );
		}

		return newCount;

	}

	private static int clipToPlane( ref Plane plane, ref ClipTriangle triangle, ClipTriangle[] dest, int destIndex )
	{

		var verts = triangle.corner;
		var numInside = 0;
		var outside = 0;

		var planeNormal = plane.normal;
		var planeDist = plane.distance;

		for( int i = 0; i < 3; i++ )
		{
			if( Vector3.Dot( planeNormal, verts[ i ] ) + planeDist > 0 )
				inside[ numInside++ ] = i;
			else
				outside = i;
		}

		// Entire triangle is in front of the plane
		if( numInside == 3 )
		{

			var target = dest[ destIndex ];
			Array.Copy( triangle.corner, 0, target.corner, 0, 3 );
			Array.Copy( triangle.uv, 0, target.uv, 0, 3 );
			Array.Copy( triangle.color, 0, target.color, 0, 3 );

			return 1;

		}

		// Entire triangle is behind the plane
		if( numInside == 0 )
		{
			return 0;
		}

		// We've got vertices on either side of the plane, need to slice...
		// TODO: Currently always splits in the same direction. Modify so split retains largest triangle area?
		if( numInside == 1 )
		{

			var i0 = inside[ 0 ];
			var i1 = ( i0 + 1 ) % 3;
			var i2 = ( i0 + 2 ) % 3;

			var va = verts[ i0 ];
			var vb = verts[ i1 ];
			var vc = verts[ i2 ];

			var uva = triangle.uv[ i0 ];
			var uvb = triangle.uv[ i1 ];
			var uvc = triangle.uv[ i2 ];

			var ca = triangle.color[ i0 ];
			var cb = triangle.color[ i1 ];
			var cc = triangle.color[ i2 ];

			var distance = 0f;

			var dir = vb - va;
			var ray = new Ray( va, dir.normalized );
			plane.Raycast( ray, out distance );
			var lerpDist = distance / dir.magnitude;

			var v1 = ray.GetPoint( distance );
			var uv1 = Vector2.Lerp( uva, uvb, lerpDist );
			var c1 = Color32.Lerp( ca, cb, lerpDist );

			dir = vc - va;
			ray = new Ray( va, dir.normalized );
			plane.Raycast( ray, out distance );
			lerpDist = distance / dir.magnitude;

			var v2 = ray.GetPoint( distance );
			var uv2 = Vector2.Lerp( uva, uvc, lerpDist );
			var c2 = Color32.Lerp( ca, cc, lerpDist );

			var destTriangle = dest[ destIndex ];

			destTriangle.corner[ 0 ] = va;
			destTriangle.corner[ 1 ] = v1;
			destTriangle.corner[ 2 ] = v2;

			destTriangle.uv[ 0 ] = uva;
			destTriangle.uv[ 1 ] = uv1;
			destTriangle.uv[ 2 ] = uv2;

			destTriangle.color[ 0 ] = ca;
			destTriangle.color[ 1 ] = c1;
			destTriangle.color[ 2 ] = c2;

			return 1;

		}
		else
		{

			var i0 = outside;
			var i1 = ( i0 + 1 ) % 3;
			var i2 = ( i0 + 2 ) % 3;

			var va = verts[ i0 ];
			var vb = verts[ i1 ];
			var vc = verts[ i2 ];

			var uva = triangle.uv[ i0 ];
			var uvb = triangle.uv[ i1 ];
			var uvc = triangle.uv[ i2 ];

			var ca = triangle.color[ i0 ];
			var cb = triangle.color[ i1 ];
			var cc = triangle.color[ i2 ];

			var dir = vb - va;
			var ray = new Ray( va, dir.normalized );
			var distance = 0f;
			plane.Raycast( ray, out distance );
			var lerpDist = distance / dir.magnitude;

			var v1 = ray.GetPoint( distance );
			var uv1 = Vector2.Lerp( uva, uvb, lerpDist );
			var c1 = Color32.Lerp( ca, cb, lerpDist );

			dir = vc - va;
			ray = new Ray( va, dir.normalized );
			plane.Raycast( ray, out distance );
			lerpDist = distance / dir.magnitude;

			var v2 = ray.GetPoint( distance );
			var uv2 = Vector2.Lerp( uva, uvc, lerpDist );
			var c2 = Color32.Lerp( ca, cc, lerpDist );

			var destTriangle = dest[ destIndex ];
			destTriangle.corner[ 0 ] = v1;
			destTriangle.corner[ 1 ] = vb;
			destTriangle.corner[ 2 ] = v2;
			destTriangle.uv[ 0 ] = uv1;
			destTriangle.uv[ 1 ] = uvb;
			destTriangle.uv[ 2 ] = uv2;
			destTriangle.color[ 0 ] = c1;
			destTriangle.color[ 1 ] = cb;
			destTriangle.color[ 2 ] = c2;

			destTriangle = dest[ ++destIndex ];

			destTriangle.corner[ 0 ] = v2;
			destTriangle.corner[ 1 ] = vb;
			destTriangle.corner[ 2 ] = vc;
			destTriangle.uv[ 0 ] = uv2;
			destTriangle.uv[ 1 ] = uvb;
			destTriangle.uv[ 2 ] = uvc;
			destTriangle.color[ 0 ] = c2;
			destTriangle.color[ 1 ] = cb;
			destTriangle.color[ 2 ] = cc;

			return 2;

		}

	}

	private static ClipTriangle[] initClipBuffer( int size )
	{

		var buffer = new ClipTriangle[ size ];

		for( int i = 0; i < size; i++ )
		{
			buffer[ i ].corner = new Vector3[ 3 ];
			buffer[ i ].uv = new Vector2[ 3 ];
			buffer[ i ].color = new Color32[ 3 ];
		}

		return buffer;

	}

	#region Nested classes

	protected struct ClipTriangle
	{

		#region Public fields

		public Vector3[] corner;
		public Vector2[] uv;
		public Color32[] color;

		#endregion

		#region Public methods

		public void CopyTo( ref ClipTriangle target )
		{
			Array.Copy( this.corner, 0, target.corner, 0, 3 );
			Array.Copy( this.uv, 0, target.uv, 0, 3 );
			Array.Copy( this.color, 0, target.color, 0, 3 );
		}

		public void CopyTo( dfRenderData buffer )
		{

			var baseIndex = buffer.Vertices.Count;

			buffer.Vertices.AddRange( corner );
			buffer.UV.AddRange( uv );
			buffer.Colors.AddRange( color );
			buffer.Triangles.Add( baseIndex + 0, baseIndex + 1, baseIndex + 2 );

		}

		#endregion

	}

	#endregion

}

/// <summary>
/// Encapsulates the information about a dfControl's clipping region,
/// and provides methods to clip a dfRenderData buffer against that
/// clipping region
/// </summary>
// @private
internal class dfTriangleClippingRegion : IDisposable
{

	#region Private static fields

	private static Queue<dfTriangleClippingRegion> pool = new Queue<dfTriangleClippingRegion>();
	private static dfList<Plane> intersectedPlanes = new dfList<Plane>( 32 );

	#endregion

	#region Private instance fields

	private dfList<Plane> planes;

	#endregion

	#region Constructors and object pooling

	public static dfTriangleClippingRegion Obtain()
	{
		return ( pool.Count > 0 ) ? pool.Dequeue() : new dfTriangleClippingRegion();
	}

	public static dfTriangleClippingRegion Obtain( dfTriangleClippingRegion parent, dfControl control )
	{

		var clip = ( pool.Count > 0 ) ? pool.Dequeue() : new dfTriangleClippingRegion();

		clip.planes.AddRange( control.GetClippingPlanes() );

		if( parent != null )
		{
			clip.planes.AddRange( parent.planes );
		}

		return clip;

	}

	public void Release()
	{

		planes.Clear();

		if( !pool.Contains( this ) )
		{
			pool.Enqueue( this );
		}

	}

	private dfTriangleClippingRegion()
	{
		planes = new dfList<Plane>();
	}

	#endregion

	#region Public methods

	/// <summary>
	/// Perform triangle clipping on the dfControl's RenderData and append the results
	/// to the destination RenderData
	/// </summary>
	/// <param name="dest">The buffer which will receive the final results</param>
	/// <param name="control">A <see cref="Bounds"/> instance fully enclosing the control</param>
	/// <param name="controlData">The <see cref="RenderData"/> structure generated by the dfControl</param>
	/// <returns>Returns TRUE if the dfControl was rendered, FALSE if it was not (lies entirely outside the clipping region)</returns>
	public bool PerformClipping( dfRenderData dest, ref Bounds bounds, uint checksum, dfRenderData controlData )
	{

		// If there are no clipping planes defined, then simply merge the control's
		// rendering information with the master buffer.
		if( planes == null || planes.Count == 0 )
		{
			dest.Merge( controlData );
			return true;
		}

		// If the RenderData's Checksum matches the dfControl's current checksum 
		// then in the case of a dfControl which was previously determined to be 
		// either entirely inside of all clipping planes or entirely outside
		// of any clipping plane we can skip clipping and intersection testing
		if( controlData.Checksum == checksum )
		{

			if( controlData.Intersection == dfIntersectionType.Inside )
			{
				// Merge the control's rendering information without any clipping
				//@Profiler.BeginSample( "Merge cached buffer - Fully inside" );
				dest.Merge( controlData );
				//@Profiler.EndSample();
				return true;
			}
			else if( controlData.Intersection == dfIntersectionType.None )
			{
				// Control lies entirely outside of the clipping region,
				// no need to include any of its rendering information
				//@Profiler.BeginSample( "Discard cached buffer - No intersection" );
				//@Profiler.EndSample();
				return false;
			}

		}

		var wasRendered = false;

		//@Profiler.BeginSample( "Clipping buffer data" );

		dfIntersectionType intersectionTest;
		var clipPlanes = TestIntersection( bounds, out intersectionTest );

		if( intersectionTest == dfIntersectionType.Inside )
		{
			//@Profiler.BeginSample( "Merging buffer - Fully inside" );
			dest.Merge( controlData );
			//@Profiler.EndSample();
			wasRendered = true;
		}
		else if( intersectionTest == dfIntersectionType.Intersecting )
		{
			//@Profiler.BeginSample( "Clipping intersecting buffer" );
			clipToPlanes( clipPlanes, controlData, dest, checksum );
			//@Profiler.EndSample();
			wasRendered = true;
		}

		controlData.Checksum = checksum;
		controlData.Intersection = intersectionTest;

		//@Profiler.EndSample();
		return wasRendered;

	}

	public dfList<Plane> TestIntersection( Bounds bounds, out dfIntersectionType type )
	{

		if( planes == null || planes.Count == 0 )
		{
			type = dfIntersectionType.Inside;
			return null;
		}

		intersectedPlanes.Clear();

		var center = bounds.center;
		var extents = bounds.extents;

		var intersecting = false;

		var planeCount = planes.Count;
		var rawPlanes = planes.Items;
		for( int i = 0; i < planeCount; i++ )
		{

			var plane = rawPlanes[ i ];
			var planeNormal = plane.normal;
			var planeDist = plane.distance;

			// Compute the projection interval radius of b onto L(t) = b.c + t * p.n
			float r =
				extents.x * Mathf.Abs( planeNormal.x ) +
				extents.y * Mathf.Abs( planeNormal.y ) +
				extents.z * Mathf.Abs( planeNormal.z );

			// Compute distance of box center from plane
			float distance = Vector3.Dot( planeNormal, center ) + planeDist;

			// Intersection occurs when distance falls within [-r,+r] interval
			if( Mathf.Abs( distance ) <= r )
			{
				intersecting = true;
				intersectedPlanes.Add( plane );
			}
			else
			{

				// If the control lies behind *any* of the planes, there
				// is no point in continuing with the rest of the test
				if( distance < -r )
				{
					type = dfIntersectionType.None;
					return null;
				}

			}

		}

		if( intersecting )
		{
			type = dfIntersectionType.Intersecting;
			return intersectedPlanes;
		}

		type = dfIntersectionType.Inside;

		return null;

	}

	public void clipToPlanes( dfList<Plane> planes, dfRenderData data, dfRenderData dest, uint controlChecksum )
	{

		if( data == null || data.Vertices.Count == 0 )
			return;

		if( planes == null || planes.Count == 0 )
		{
			dest.Merge( data );
			return;
		}

		dfClippingUtil.Clip( planes, data, dest );

	}

	#endregion

	#region IDisposable Members

	public void Dispose()
	{
		Release();
	}

	#endregion

}
