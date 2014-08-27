using UnityEngine;
using System.Collections;

[AddComponentMenu( "Daikon Forge/Examples/General/Window Tilt" )]
public class dfWindowTilt : MonoBehaviour
{

	private dfControl control;

	void Start()
	{
		control = GetComponent<dfControl>();
		if( control == null )
		{
			this.enabled = false;
		}
	}

	void Update()
	{

		var camera = control.GetCamera();

		var center = control.GetCenter();
		var viewPoint = camera.WorldToViewportPoint( center );

		const float yawAmount = 20f;

		control.transform.localRotation = Quaternion.Euler( 0f, ( viewPoint.x * 2f - 1f ) * yawAmount, 0f );

	}

}
