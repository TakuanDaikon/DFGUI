/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Encapsulates the information needed to render a control as a Mesh.
/// GUI Controls will fill this structure with all of the information 
/// needed to render the control on-screen as a Mesh, with the implicit 
/// assumption that all vertex coordinates are parent-relative coordinates 
/// and that all uv coordinates refer to locations within a common
/// texture atlas or <see cref="Material.mainTexture"/>.
/// </summary>
public class dfRenderData : IDisposable
{

	#region Static fields 

	private static Queue<dfRenderData> pool = new Queue<dfRenderData>();

	#endregion

	#region Public fields

	/// <summary>
	/// The <see cref="UnityEngine.Material"/> which will be used as the source
	/// Material for rendering
	/// </summary>
	public Material Material;

	/// <summary>
	/// The specific Shader that will be used to render the data in this render
	/// buffer. Only used when overriding the Shader for the Material, most of 
	/// the time this property will be NULL.
	/// </summary>
	public Shader Shader;

	/// <summary>
	/// The transformation matrix (if any) that needs to be applied to the 
	/// vertices before rendering. Typically this will be a "local to world" 
	/// matrix.
	/// </summary>
	public Matrix4x4 Transform;

	/// <summary>
	/// The list of vertices used to render the mesh
	/// </summary>
	public dfList<Vector3> Vertices;

	/// <summary>
	/// The list of UV coordinates for each vertex
	/// </summary>
	public dfList<Vector2> UV;

	/// <summary>
	/// The list of normals used when rendering the mesh
	/// </summary>
	public dfList<Vector3> Normals;

	public dfList<Vector4> Tangents;

	/// <summary>
	/// A list of triangle indices
	/// </summary>
	public dfList<int> Triangles;

	/// <summary>
	/// A list of colors for each vertex
	/// </summary>
	public dfList<Color32> Colors;

	/// <summary>
	/// Used by GUIManager to determine whether cached data is 
	/// still valid
	/// </summary>
	public uint Checksum;

	/// <summary>
	/// Contains the cached results from the last intersection test
	/// performed on the contained render data. Used by GUIManager 
	/// to determine whether a new intersection test needs to be 
	/// performed for triangle clipping
	/// </summary>
	public dfIntersectionType Intersection;

	#endregion 

	#region Constructor and Pooling 

	/// <summary>
	/// Initializes a new instance of the <see cref="dfRenderData"/> class.
	/// Direct instantiation of this class is discouraged in favor of using
	/// the <see cref="Lookup"/> method to benefit from object pooling.
	/// </summary>
	/// <param name="capacity">The initial capacity of the internal lists</param>
	internal dfRenderData() : this( 32 ) {}

	/// <summary>
	/// Initializes a new instance of the <see cref="dfRenderData"/> class.
	/// Direct instantiation of this class is discouraged in favor of using
	/// the <see cref="Lookup"/> method to benefit from object pooling.
	/// </summary>
	/// <param name="capacity">The initial capacity of the internal lists</param>
	internal dfRenderData( int capacity )
	{

		Vertices = new dfList<Vector3>( capacity );
		Triangles = new dfList<int>( capacity );
		Normals = new dfList<Vector3>( capacity );
		Tangents = new dfList<Vector4>( capacity );
		UV = new dfList<Vector2>( capacity );
		Colors = new dfList<Color32>( capacity );

		this.Transform = Matrix4x4.identity;

	}

	/// <summary>
	/// Obtains a <see cref="dfRenderData"/> instance from the object pool
	/// </summary>
	/// <returns>A <see cref="dfRenderData"/> instance that can be used to 
	/// receive <see cref="dfControl"/> render data</returns>
	public static dfRenderData Obtain()
	{
		lock( pool )
		{
			return pool.Count > 0 ? pool.Dequeue() : new dfRenderData();
		}
	}

	/// <summary>
	/// Flushes the <see cref="dfRenderData"/> object pool, freeing all 
	/// memory. This operation should only be performed when a new level
	/// is loaded.
	/// </summary>
	public static void FlushObjectPool()
	{

		lock( pool )
		{

			while( pool.Count > 0 )
			{

				var data = pool.Dequeue();

				data.Vertices.TrimExcess();
				data.Triangles.TrimExcess();
				data.UV.TrimExcess();
				data.Colors.TrimExcess();

			}

		}

	}

	#endregion

	#region Public methods 

	/// <summary>
	/// Returns the <see cref="dfRenderData"/> instance to the object pool
	/// </summary>
	public void Release()
	{
		lock( pool )
		{
			Clear();
			pool.Enqueue( this );
		}
	}

	/// <summary>
	/// Used to reset all lists in preparation for filling with new or
	/// updated rendering data. NOTE: While this function does appear
	/// to empty the data lists, it does not free the internal memory 
	/// used to hold the information, with the assumption that this 
	/// will glyphData in fewer garbage collections.
	/// </summary>
	public void Clear()
	{

		Material = null;
		Shader = null;
		Transform = Matrix4x4.identity;
		Checksum = 0x00;
		Intersection = dfIntersectionType.None;

		Vertices.Clear();
		UV.Clear();
		Triangles.Clear();
		Colors.Clear();
		Normals.Clear();
		Tangents.Clear();

	}

	/// <summary>
	/// Returns a value indicating whether the data in this object can be 
	/// used to create a valid Mesh.
	/// </summary>
	/// <returns></returns>
	public bool IsValid()
	{

		var count = Vertices.Count;

		return
			count > 0 &&
			count <= 65000 &&
			UV.Count == count &&
			Colors.Count == count;

	}

	/// <summary>
	/// Ensures that the lists have enough memory allocated to store
	/// the number of elements indicated by <paramref name="capacity"/>, 
	/// to reduce memory thrashing
	/// </summary>
	/// <param name="capacity"></param>
	public void EnsureCapacity( int capacity )
	{

		Vertices.EnsureCapacity( capacity );
		Triangles.EnsureCapacity( Mathf.RoundToInt( capacity * 1.5f ) );
		UV.EnsureCapacity( capacity );
		Colors.EnsureCapacity( capacity );

		if( Normals != null ) Normals.EnsureCapacity( capacity );
		if( Tangents != null ) Tangents.EnsureCapacity( capacity );

	}

	/// <summary>
	/// Merges another <see cref="dfRenderData"/> instance with this instance
	/// </summary>
	/// <param name="buffer">The render data to be appended to this instance</param>
	/// <param name="transformVertices">If set to TRUE, the render data in 
	/// <paramref name="buffer"/> will be transformed by its <see cref="Transform"/> 
	/// before being merged with this instance. If set to FALSE, the data will be 
	/// merged without tranforming.</param>
	public void Merge( dfRenderData buffer )
	{
		Merge( buffer, true );
	}

	/// <summary>
	/// Merges another <see cref="dfRenderData"/> instance with this instance
	/// </summary>
	/// <param name="buffer">The render data to be appended to this instance</param>
	/// <param name="transformVertices">If set to TRUE, the render data in 
	/// <paramref name="buffer"/> will be transformed by its <see cref="Transform"/> 
	/// before being merged with this instance. If set to FALSE, the data will be 
	/// merged without tranforming.</param>
	public void Merge( dfRenderData buffer, bool transformVertices )
	{

		// We need the current currentIndex of vertices before adding any more so that 
		// we have a proper base index for our triangle indices
		var baseIndex = Vertices.Count;

		#region Merge vertices 

		Vertices.AddRange( buffer.Vertices );
		if( transformVertices )
		{
			
			var rawVerts = Vertices.Items;
			var count = buffer.Vertices.Count;
			var transform = buffer.Transform;

			for( int i = baseIndex; i < baseIndex + count; i++ )
			{
				rawVerts[ i ] = transform.MultiplyPoint( rawVerts[ i ] );
			}

		}

		#endregion 

		#region Merge triangles 

		var triangleBaseIndex = Triangles.Count;

		Triangles.AddRange( buffer.Triangles );

		var bufferTriangleCount = buffer.Triangles.Count;
		var rawTriangles = Triangles.Items;
		for( int i = triangleBaseIndex; i < triangleBaseIndex + bufferTriangleCount; i++ )
		{
			rawTriangles[ i ] += baseIndex;
		}

		#endregion 

		UV.AddRange( buffer.UV );
		Colors.AddRange( buffer.Colors );
		Normals.AddRange( buffer.Normals );
		Tangents.AddRange( buffer.Tangents );

	}

	#endregion

	#region Private utility methods 

	internal void ApplyTransform( Matrix4x4 transform )
	{

		var count = Vertices.Count;

		var rawVerts = Vertices.Items;
		for( int i = 0; i < count; i++ )
		{
			rawVerts[ i ] = transform.MultiplyPoint( rawVerts[ i ] );		
		}

		if( Normals.Count > 0 )
		{
			var rawNormals = Normals.Items;
			for( int i = 0; i < count; i++ )
			{
				rawNormals[ i ] = transform.MultiplyVector( rawNormals[ i ] );
			}
		}

	}

	#endregion

	#region System.Object overrides 

	/// <summary>
	/// Returns a formatted string summarizing this object's state
	/// </summary>
	public override string ToString()
	{

		return string.Format(
			"V:{0} T:{1} U:{2} C:{3}",
			Vertices.Count,
			Triangles.Count,
			UV.Count,
			Colors.Count
		);

	}

	#endregion

	#region IDisposable Members

	public void Dispose()
	{
		this.Release();
	}

	#endregion

}
