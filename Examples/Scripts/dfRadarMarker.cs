using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Examples/Radar/Radar Marker" )]
public class dfRadarMarker : MonoBehaviour
{

	public dfRadarMain radar;
	public string markerType;
	public string outOfRangeType;

	#region Used by dfRadarMain - Do not modify these values

	[NonSerialized]
	internal dfControl marker;

	[NonSerialized]
	internal dfControl outOfRangeMarker;

	#endregion

	#region Unity events

	public void OnEnable()
	{

		if( string.IsNullOrEmpty( markerType ) )
			return;

		if( radar == null )
		{
			radar = FindObjectOfType( typeof( dfRadarMain ) ) as dfRadarMain;
			if( radar == null )
			{
				Debug.LogWarning( "No radar found" );
				return;
			}
		}

		radar.AddMarker( this );

	}

	public void OnDisable()
	{
		if( radar != null )
		{
			radar.RemoveMarker( this );
		}
	}

	#endregion

}
