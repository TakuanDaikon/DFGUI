/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

internal class dfRenderBatch
{

	#region Public fields 

	public Material Material;

	#endregion 

	#region Private runtime variables 

	private dfList<dfRenderData> buffers = new dfList<dfRenderData>();

	#endregion 

	#region Public methods 

	public void Add( dfRenderData buffer )
	{

		if( Material == null && buffer.Material != null )
			Material = buffer.Material;

		buffers.Add( buffer );

	}

	public dfRenderData Combine()
	{

		// Obtain a buffer that will contain the combined mesh data
		var result = dfRenderData.Obtain();

		// Dereference the raw buffer array and grab size
		var bufferCount = buffers.Count;
		var bufferItems = buffers.Items;

		// If there are no buffers to combine, exit early
		if( bufferCount == 0 )
			return result;

		// Now that we know there are buffers to combine, and the implicit
		// assumption is that they are being combined because they all share
		// the same Material, we can grab the Material of the first buffer
		result.Material = buffers[ 0 ].Material;

		// Calculating the total vertex count allows us to ensure 
		// adequate space before combining the meshes, which is 
		// a significant gain in efficiency
		var totalVertCount = 0;
		for( int i = 0; i < bufferCount; i++ )
		{
			totalVertCount = bufferItems[ i ].Vertices.Count;
		}

		// Ensure that there is room in the internal arrays to hold 
		// the entire collection of mesh data. This eliminates the 
		// need to "size on demand", which could cause memory thrashing.
		result.EnsureCapacity( totalVertCount );

		// After ensuring adequate internal capacity, we can dereference
		// the internal triangles array in order to eliminate unnecessary
		// calls to dfList.getItem() and dfList.setItem(), etc.
		var rawTriangles = result.Triangles.Items;

		// Combine all of the buffers
		for( int i = 0; i < bufferCount; i++ )
		{

			// Dereference the current buffer
			var buffer = bufferItems[ i ];

			// Need to grab the base triangle index before combining so that
			// the triangle indices of the individual buffers can be adjusted
			var baseVertexIndex = result.Vertices.Count;
			var baseTriangleIndex = result.Triangles.Count;
			var bufferTriangleCount = buffer.Triangles.Count;

			// Add the current buffer's render data to the combined buffer
			result.Vertices.AddRange( buffer.Vertices );
			result.Triangles.AddRange( buffer.Triangles );
			result.Colors.AddRange( buffer.Colors );
			result.UV.AddRange( buffer.UV );

			// Adjust the triangle indices
			for( int x = baseTriangleIndex; x < baseTriangleIndex + bufferTriangleCount; x++ )
			{
				rawTriangles[ x ] += baseVertexIndex;
			}

		}

		// Return the combined mesh data
		return result;

	}

	#endregion 

}
