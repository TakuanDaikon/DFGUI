using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class dfFollowObjectSorter : MonoBehaviour
{

	#region Singleton implementation 

	private static dfFollowObjectSorter _instance;

	public static dfFollowObjectSorter Instance
	{
		get
		{
			lock( typeof( dfFollowObjectSorter ) )
			{
				if( _instance == null && Application.isPlaying )
				{
					var go = new GameObject( "Follow Object Sorter" );
					_instance = go.AddComponent<dfFollowObjectSorter>();
					list.Clear();
				}
				return _instance;
			}
		}
	}

	#endregion 

	#region Private runtime variables 

	private static dfList<FollowSortRecord> list = new dfList<FollowSortRecord>();

	#endregion 

	#region Public methods 

	public static void Register( dfFollowObject follow )
	{
		if( Application.isPlaying )
		{
			Instance.register( follow );
		}
	}

	public static void Unregister( dfFollowObject follow )
	{

		for( int i = 0; i < list.Count; i++ )
		{
			if( list[ i ].follow == follow )
			{
				list.RemoveAt( i );
				return;
			}
		}

	}

	#endregion 

	#region Monobehaviour events 

	public void Update()
	{

		var minZOrder = int.MaxValue;
		
		for( int i = 0; i < list.Count; i++ )
		{

			var item = list[ i ];

			item.distance = getDistance( item.follow );

			if( item.control.ZOrder < minZOrder )
			{
				minZOrder = item.control.ZOrder;
			}

		}

		list.Sort();

		for( int i = 0; i < list.Count; i++ )
		{
			var control = list[ i ].control;
			control.ZOrder = minZOrder++;
		}

	}

	#endregion 

	#region Private utility methods 

	private void register( dfFollowObject follow )
	{

		// Ensure no duplicates
		for( int i = 0; i < list.Count; i++ )
		{
			if( list[ i ].follow == follow )
			{
				return;
			}
		}

		list.Add( new FollowSortRecord( follow ) );

	}

	private float getDistance( dfFollowObject follow )
	{
		return ( follow.mainCamera.transform.position - follow.attach.transform.position ).sqrMagnitude;
	}

	#endregion

	#region Nested types

	private class FollowSortRecord : IComparable<FollowSortRecord>
	{

		public float distance;
		public dfFollowObject follow;
		public dfControl control;

		public FollowSortRecord( dfFollowObject follow )
		{
			this.follow = follow;
			this.control = follow.GetComponent<dfControl>();
		}

		#region IComparable<SortRecord> Members

		public int CompareTo( FollowSortRecord other )
		{
			return other.distance.CompareTo( this.distance );
		}

		#endregion

	}

	#endregion 

}
