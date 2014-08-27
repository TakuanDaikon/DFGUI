/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Object Pooling/Pooled Object" )]
public class dfPooledObject : MonoBehaviour
{

	#region Events 

	public delegate void SpawnEventHandler( GameObject instance );

	public event SpawnEventHandler Spawned;
	public event SpawnEventHandler Despawned;

	#endregion

	#region Properties 

	public dfPoolManager.ObjectPool Pool { get; set; }

	#endregion

	#region Unity events

	void Awake() { }
	void OnEnable() { }
	void OnDisable() { }
	void OnDestroy() { }

	#endregion

	#region Public methods

	public void Despawn()
	{
		this.Pool.Despawn( gameObject );
	}

	#endregion

	#region Utility methods 

	internal void OnSpawned()
	{
		if( Spawned != null )
		{
			Spawned( this.gameObject );
		}
		SendMessage( "OnObjectSpawned", SendMessageOptions.DontRequireReceiver );
	}

	internal void OnDespawned()
	{
		if( Despawned != null )
		{
			Despawned( this.gameObject );
		}
		SendMessage( "OnObjectDespawned", SendMessageOptions.DontRequireReceiver );
	}

	#endregion

}
