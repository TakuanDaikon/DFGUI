using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Implements basic radar functionality.
/// </summary>
[AddComponentMenu( "Daikon Forge/Examples/Radar/Radar Main" )]
public class dfRadarMain : MonoBehaviour
{

	#region Serialized properties 

	public GameObject target;
	public float maxDetectDistance = 100f;
	public int radarRadius = 100;

	public List<dfControl> markerTypes;

	#endregion

	#region Private runtime variables 

	private List<dfRadarMarker> markers = new List<dfRadarMarker>();
	private dfControl control;

	#endregion

	#region Unity events 

	public void Start()
	{

		ensureControlReference();

		for( int i = 0; i < markerTypes.Count; i++ )
		{
			markerTypes[ i ].IsVisible = false;
		}

	}

	public void LateUpdate()
	{
		updateMarkers();
	}
	 
	#endregion

	#region Public methods 

	public void AddMarker( dfRadarMarker item )
	{

		if( string.IsNullOrEmpty( item.markerType ) )
			return;

		ensureControlReference();

		item.marker = instantiateMarker( item.markerType );
		if( item.marker == null )
			return;

		if( !string.IsNullOrEmpty( item.outOfRangeType ) )
			item.outOfRangeMarker = instantiateMarker( item.outOfRangeType );

		markers.Add( item );

	}

	private dfControl instantiateMarker( string markerName )
	{

		var markerType = markerTypes.Find( x => x.name == markerName );
		if( markerType == null )
		{
			Debug.LogError( "Marker type not found: " + markerName );
			return null;
		}

		var marker = (dfControl)Instantiate( markerType );
		marker.hideFlags = HideFlags.DontSave;
		marker.IsVisible = true;
		control.AddControl( marker );

		return marker;

	}

	public void RemoveMarker( dfRadarMarker item )
	{

		if( markers.Remove( item ) )
		{

			ensureControlReference();

			if( item.marker != null ) Destroy( item.marker );
			if( item.outOfRangeMarker != null ) Destroy( item.outOfRangeMarker );

			control.RemoveControl( item.marker );

		}

	}

	#endregion

	#region Private utility methods

	private void ensureControlReference()
	{

		this.control = GetComponent<dfControl>();
		if( control == null )
		{
			Debug.LogError( "Host control not found" );
			this.enabled = false;
			return;
		}

		// Pivot needs to be in the center to operate correctly
		control.Pivot = dfPivotPoint.MiddleCenter;

	}

	private void updateMarkers()
	{
		for( int i = 0; i < markers.Count; i++ )
		{
			updateMarker( markers[ i ] );
		}
	}

	private void updateMarker( dfRadarMarker item )
	{

		Vector3 centerPos = target.transform.position;
		Vector3 extPos = item.transform.position;

		float dx = centerPos.x - extPos.x;
		float dz = centerPos.z - extPos.z;

		float deltay =
			Mathf.Atan2( dx, -dz )
			* Mathf.Rad2Deg
			+ 90
			+ target.transform.eulerAngles.y;

		float dist = Vector3.Distance( centerPos, extPos );
		if( dist > maxDetectDistance )
		{

			item.marker.IsVisible = false;

			if( item.outOfRangeMarker != null )
			{

				var marker = item.outOfRangeMarker;

				marker.IsVisible = true;
				marker.transform.position = control.transform.position;
				marker.transform.eulerAngles = new Vector3( 0, 0, deltay - 90 );

			}

			return;

		}
		else if( item.outOfRangeMarker != null )
		{
			item.outOfRangeMarker.IsVisible = false;
		}

		float bX = dist * Mathf.Cos( deltay * Mathf.Deg2Rad );
		float bY = dist * Mathf.Sin( deltay * Mathf.Deg2Rad );

		var mapScale = radarRadius / maxDetectDistance * control.PixelsToUnits();
		bX = bX * mapScale; 
		bY = bY * mapScale;

		item.marker.transform.localPosition = new Vector3( bX, bY, 0 );
		item.marker.IsVisible = true;
		item.marker.Pivot = dfPivotPoint.MiddleCenter;

	}

	#endregion

}
