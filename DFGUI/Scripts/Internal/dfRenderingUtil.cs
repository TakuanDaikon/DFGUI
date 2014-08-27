/* Copyright 2013-2014 Daikon Forge */
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Used to cache instances of Material instances that are generated during
/// rendering, to mitigate the effects of having to copy materials in 
/// the WebPlayer due to a Unity bug
/// </summary>
// @private
internal class dfMaterialCache
{

	#region Static variables

	private static Dictionary<Material, Cache> caches = new Dictionary<Material, Cache>();

	#endregion

	#region Static methods

	public static Material Lookup( Material BaseMaterial )
	{

		if( BaseMaterial == null )
		{
			Debug.LogError( "Cache lookup on null material" );
			return null;
		}

		Cache item = null;
		if( !caches.TryGetValue( BaseMaterial, out item ) )
		{
			item = caches[ BaseMaterial ] = new Cache( BaseMaterial );
		}

		return item.Obtain();

	}

	public static void Reset()
	{
		Cache.ResetAll();
	}

	/// <summary>
	/// Releases all Material references
	/// </summary>
	public static void Clear()
	{
		Cache.ClearAll();
		caches.Clear();
	}

	#endregion

	#region Nested classes

	private class Cache
	{

		#region Static variables

		/// <summary>
		/// Duplicate list of all Cache instances created,
		/// so that we don't have to use Dictionary.Values to iterate 
		/// the list, which allocates an enumerator object
		/// </summary>
		private static List<Cache> cacheInstances = new List<Cache>();

		#endregion

		#region Private variables

		private Material baseMaterial;
		private List<Material> instances = new List<Material>( 10 );
		private int currentIndex = 0x00;

		#endregion

		#region Constructors

		private Cache()
		{
			// Do not allow the use of the parameterless constructor,
			// even via reflection
			throw new NotImplementedException();
		}

		public Cache( Material BaseMaterial )
		{

			this.baseMaterial = BaseMaterial;
			this.instances.Add( BaseMaterial );

			cacheInstances.Add( this );

		}

		#endregion

		#region Static methods

		/// <summary>
		/// Releases all Material references
		/// </summary>
		public static void ClearAll()
		{
			for( int i = 0; i < cacheInstances.Count; i++ )
			{
				cacheInstances[ i ].Clear();
			}
			cacheInstances.Clear();
		}

		/// <summary>
		/// Reset all cache entries 
		/// </summary>
		public static void ResetAll()
		{
			for( int i = 0; i < cacheInstances.Count; i++ )
			{
				cacheInstances[ i ].Reset();
			}
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Lookup a copy of the base Material for this cache line. 
		/// Will return an existing copy if one exists, or will create
		/// a new copy if needed.
		/// </summary>
		public Material Obtain()
		{

			if( currentIndex < instances.Count )
			{
				return instances[ currentIndex++ ];
			}

			currentIndex += 1;

			var newCopy = new Material( baseMaterial )
			{
				hideFlags = HideFlags.DontSave | HideFlags.HideInInspector,
				name = string.Format( "{0} (Copy {1})", baseMaterial.name, currentIndex )
			};

			instances.Add( newCopy );

			return newCopy;

		}

		/// <summary>
		/// Reset the current index in preparation for another render pass
		/// </summary>
		public void Reset()
		{
			currentIndex = 0;
		}

		/// <summary>
		/// Releases all Material references
		/// </summary>
		public void Clear()
		{

			currentIndex = 0;

			// NOTE: The first instance is always the original, we only destroy copies,
			// so this loop starts at index 1
			for( int i = 1; i < instances.Count; i++ )
			{
				var instance = instances[ i ];
				if( instance != null )
				{
					if( Application.isPlaying )
						UnityEngine.Object.Destroy( instance );
					else
						UnityEngine.Object.DestroyImmediate( instance );
				}
			}

			instances.Clear();

		}

		#endregion

	}

	#endregion

}

/// <summary>
/// ** FOR INTERNAL USE ONLY **
/// Keeps a cache of arrays used during the rendering process. Note that
/// the array returned from the Obtain() method is intended to have a very
/// short "useful" life span. It is intended to be used immediately and 
/// then no longer be needed, since the next call to Obtain() from any 
/// other code in the application may return the same array. This also 
/// means that you have to be extremely diligent about not nesting calls
/// to Obtain() within the useful life-time of the array.
/// </summary>
// @private
internal class dfTempArray<T>
{

	#region Private variables

	private static List<T[]> cache = new List<T[]>( 32 );

	#endregion

	#region Public methods

	public static void Clear()
	{
		cache.Clear();
	}

	public static T[] Obtain( int length )
	{
		return Obtain( length, 128 );
	}

	public static T[] Obtain( int length, int maxCacheSize )
	{

		lock( cache )
		{

			// Search for an existing list of the correct size
			for( int i = 0; i < cache.Count; i++ )
			{

				var list = cache[ i ];
				if( list.Length == length )
				{

					// Always keep the most-recently-accessed list at the
					// front of the cache
					if( i > 0 )
					{
						cache.RemoveAt( i );
						cache.Insert( 0, list );
					}

					return list;

				}

			}

			// No list of the correct size was found. If the cache is already 
			// full, remove the least-recently-accessed list from the cache 
			// in order to make room for another list.
			if( cache.Count >= maxCacheSize )
			{
				cache.RemoveAt( cache.Count - 1 );
			}

			// Create a new list of the correct size and add it to the cache
			var newList = new T[ length ];
			cache.Insert( 0, newList );

			return newList;

		}

	}

	#endregion

}
