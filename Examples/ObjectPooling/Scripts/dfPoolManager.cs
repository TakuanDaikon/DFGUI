/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Object Pooling/Object Pool Manager" )]
[Serializable]
public class dfPoolManager : MonoBehaviour
{

	// PLEASE NOTE: This is not intended to be a fully-featured professional
	// object pooling solution; it is intended to provide a basic solution 
	// that is adequate for the needs of the example scenes in which it is 
	// used.

	#region Enumerations 

	/// <summary>
	/// Indicates how an ObjectPool should behave when there are no more 
	/// instances available in the pool and the MaxInstances limit has 
	/// been reached.
	/// </summary>
	public enum LimitReachedAction
	{
		/// <summary>
		/// A NULL value will be returned when the pool is empty and the 
		/// maximum number of instances has already been created
		/// </summary>
		Nothing,
		/// <summary>
		/// An <see cref="System.Exception"/> will be thrown when the pool is empty
		/// and the maximum number of instances has already been created
		/// </summary>
		Error,
		/// <summary>
		/// The oldest current instance will be recycled when the pool is empty and the
		/// maximum number of instances has already been created
		/// </summary>
		Recycle,
	}

	#endregion

	#region Public events 

	public delegate void PoolManagerLoadingEvent();
	public delegate void PoolManagerProgressEvent( int TotalItems, int Current );

	public event PoolManagerLoadingEvent LoadingStarted;
	public event PoolManagerLoadingEvent LoadingComplete;
	public event PoolManagerProgressEvent LoadingProgress;

	#endregion

	#region Singleton implementation

	/// <summary>
	/// Singleton instance of the <see cref="dfPoolManager"/> class
	/// </summary>
	public static dfPoolManager Pool { get; private set; }

	#endregion

	#region Public fields 

	public bool AutoPreload = true;

	public bool PreloadInBackground = true;

	#endregion

	#region Serialized private fields

	[SerializeField]
	private List<ObjectPool> objectPools = new List<ObjectPool>();

	#endregion

	#region Private instance fields 

	private bool poolsPreloaded = false;

	#endregion

	#region Unity events

	void Awake() 
	{

		if( dfPoolManager.Pool != null )
		{
			throw new Exception( "Cannot have more than one instance of the " + GetType().Name + " class" );
		}

		dfPoolManager.Pool = this;

		if( AutoPreload )
		{
			Preload();
		}

	}

	void OnDestroy()
	{
		ClearAllPools();
	}

	void OnLevelWasLoaded()
	{
		ClearAllPools();
	}

	#endregion

	#region Public methods

	public void ClearAllPools()
	{

		poolsPreloaded = false;

		for( int i = 0; i < objectPools.Count; i++ )
		{
			objectPools[ i ].Clear();
		}

	}

	public void Preload()
	{

		if( poolsPreloaded )
			return;
			
		if( PreloadInBackground )
		{
			StartCoroutine( preloadPools() );
		}
		else
		{
			
			var enumerator = preloadPools();
			while( enumerator.MoveNext() )
			{
#pragma warning disable 0168
				var temp = enumerator.Current;
#pragma warning restore 0168
			}

		}

	}

	public void AddPool( string name, GameObject prefab )
	{

		if( objectPools.Any( p => p.PoolName == name ) )
			throw new Exception( "Duplicate key: " + name );

		if( prefab.activeSelf )
		{
			prefab.SetActive( false );
		}

		var pool = new ObjectPool()
		{
			Prefab = prefab,
			PoolName = name
		};
		
		this.objectPools.Add( pool );

	}

	#endregion

	#region Preloading 

	private IEnumerator preloadPools()
	{

		poolsPreloaded = true;

		var totalItems = 0;
		for( int i = 0; i < objectPools.Count; i++ )
		{
			totalItems += objectPools[ i ].InitialPoolSize;
		}

		if( LoadingStarted != null )
		{
			LoadingStarted();
		}

		var currentItem = 0;
		for( int i = 0; i < objectPools.Count; i++ )
		{

			objectPools[ i ].Preload( () =>
			{
				if( LoadingProgress != null )
				{
					LoadingProgress( totalItems, currentItem );
				}
				currentItem += 1;
			} );

			yield return null;

		}

		if( LoadingComplete != null )
		{
			LoadingComplete();
		}

	}

	#endregion

	#region Indexers

	public ObjectPool this[ string name ]
	{
		get
		{
			
			for( int i = 0; i < objectPools.Count; i++ )
			{
				if( objectPools[ i ].PoolName == name )
				{
					return objectPools[ i ];
				}
			}

			throw new KeyNotFoundException( "Object pool not found: " + name );

		}
	}

	#endregion

	#region Nested Classes 

	[Serializable]
	public class ObjectPool
	{

		#region Private variables 

		private dfList<GameObject> pool = dfList<GameObject>.Obtain();
		private dfList<GameObject> spawned = dfList<GameObject>.Obtain();

		#endregion

		#region Serialized fields 

		[SerializeField]
		private string poolName = "";

		[SerializeField]
		private LimitReachedAction limitType = LimitReachedAction.Nothing;

		[SerializeField]
		private GameObject prefab;

		[SerializeField]
		private int maxInstances = -1;

		[SerializeField]
		private int initialPoolSize = 0;

		[SerializeField]
		private bool allowReparenting = true;

		#endregion

		#region Public properties 

		public string PoolName
		{
			get { return this.poolName; }
			set { this.poolName = value; }
		}

		public LimitReachedAction LimitReached
		{
			get { return this.limitType; }
			set { this.limitType = value; }
		}

		public GameObject Prefab
		{
			get { return this.prefab; }
			set { this.prefab = value; }
		}

		public int MaxInstances
		{
			get { return this.maxInstances; }
			set { this.maxInstances = value; }
		}

		public int InitialPoolSize
		{
			get { return this.initialPoolSize; }
			set { this.initialPoolSize = value; }
		}

		public bool AllowReparenting
		{
			get { return this.allowReparenting; }
			set { this.allowReparenting = value; }
		}

		/// <summary>
		/// Returns the available number of "unspawned" objects in the pool.
		/// Note that this number includes the number that may be allocated
		/// before reaching the limit specified by <see cref="MaxInstances"/>, 
		/// not just the number that have already been added to the pool.
		/// </summary>
		public int Available
		{
			get
			{
				
				if( this.maxInstances == -1 )
					return int.MaxValue;

				return Mathf.Max( pool.Count, this.maxInstances );

			}
		}

		#endregion

		#region Public methods

		public void Clear()
		{

			while( spawned.Count > 0 )
			{
				pool.Enqueue( spawned.Dequeue() );
			}

			for( int i = 0; i < pool.Count; i++ )
			{
				var instance = pool[ i ];
				DestroyImmediate( instance );
			}

			pool.Clear();

		}

		public GameObject Spawn( Transform parent, Vector3 position, Quaternion rotation, bool activate )
		{

			var instance = Spawn( position, rotation, activate );
			instance.transform.parent = parent;

			return instance;

		}

		public GameObject Spawn( Vector3 position, Quaternion rotation )
		{
			return Spawn( position, rotation, true );
		}

		public GameObject Spawn( Vector3 position, Quaternion rotation, bool activate )
		{

			var instance = Spawn( false );

			instance.transform.position = position;
			instance.transform.rotation = rotation;

			if( activate )
			{
				instance.SetActive( true );
			}

			return instance;

		}

		public GameObject Spawn( bool activate )
		{
			
			// Always return an object from the pool first if one is available
			if( pool.Count > 0 )
			{
				var pooled = pool.Dequeue();
				spawnInstance( pooled, activate );
				return pooled;
			}

			// If the pool is empty but the number of current prefab instances
			// has not been reached, instantiate a new instance
			if( maxInstances == -1 || spawned.Count < maxInstances )
			{
				var instantiated = Instantiate();
				spawnInstance( instantiated, activate );
				return instantiated;
			}

			// Already hit the limit, and asked to return NULL
			if( limitType == LimitReachedAction.Nothing )
				return null;

			// Already hit the limit and asked to throw an exception
			if( limitType == LimitReachedAction.Error )
				throw new Exception( string.Format( "The {0} object pool has already allocated its limit of {1} objects", PoolName, MaxInstances ) );

			// Return the oldest already-spawned instance
			var recycled = spawned.Dequeue();
			spawnInstance( recycled, activate );
			return recycled;

		}

		public void Despawn( GameObject instance )
		{

			// Remove the instance from the spawned list. If it was not
			// found in the spawned list, then do nothing.
			if( !spawned.Remove( instance ) )
				return;

			// Find the PooledObject component and notify any observers of the despawn
			var component = instance.GetComponent<dfPooledObject>();
			if( component != null )
			{
				component.OnDespawned();
			}

			// Disable the instance
			instance.SetActive( false );

			// Add the instance back to the "free instance" pool
			pool.Enqueue( instance );

			// Reparent the instance to keep the scene hierarchy tree tidy
			if( allowReparenting && dfPoolManager.Pool != null )
			{
				instance.transform.parent = dfPoolManager.Pool.transform;
			}

		}

		#endregion

		#region Private utility methods

		internal void Preload()
		{
			Preload( null );
		}

		internal void Preload( Action callback )
		{

			if( prefab.activeSelf )
			{
				prefab.SetActive( false );
			}

			var max = this.maxInstances == -1 ? int.MaxValue : this.maxInstances;
			var poolSize = Mathf.Min( this.initialPoolSize, max );

			// Using a while loop here instead of a for loop because background
			// loading may force dependant code to call Spawn() before preloading
			// is completed.
			while( pool.Count + spawned.Count < poolSize )
			{

				this.pool.Add( Instantiate() );

				if( callback != null )
				{
					callback();
				}

			}

		}

		private void spawnInstance( GameObject instance, bool activate )
		{

			spawned.Enqueue( instance );

			// Find the PooledObject component and notify any observers of the despawn
			var poolComponent = instance.GetComponent<dfPooledObject>();
			if( poolComponent != null )
			{
				poolComponent.OnSpawned();
			}

			if( activate )
			{
				instance.SetActive( true );
			}

		}

		private GameObject Instantiate()
		{

			var instance = GameObject.Instantiate( prefab ) as GameObject;
			instance.name = string.Format( "{0} {1}", PoolName, pool.Count + 1 );
			//instance.hideFlags = HideFlags.HideAndDontSave;

			if( allowReparenting )
			{
				instance.transform.parent = dfPoolManager.Pool.transform;
			}

			var poolComponent = instance.GetComponent<dfPooledObject>();
			if( poolComponent == null )
			{
				poolComponent = instance.AddComponent<dfPooledObject>();
			}

			poolComponent.Pool = this;

			// TODO: Any other initialization needed?

			return instance;
		
		}

		#endregion

	}

	#endregion

}
