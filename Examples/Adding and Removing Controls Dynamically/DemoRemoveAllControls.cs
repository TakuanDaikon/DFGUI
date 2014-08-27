using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Add-Remove Controls/Remove Child Controls" )]
public class DemoRemoveAllControls : MonoBehaviour 
{

	public dfControl target;

	public void Start()
	{
		// If not already assigned, grab a reference to the 
		// Scroll Panel whose controls will be removed
		if( target == null )
		{
			this.target = GetComponent<dfControl>();
		}
	}

	public void OnClick()
	{

		// Note that a simple for loop cannot be used because
		// as we remove controls, the incrementing index will
		// no longer be correct
		while( target.Controls.Count > 0 )
		{

			// Reference the first control in the container
			var childControl = target.Controls[0];

			// Remove the container from the control before
			// destroying it
			target.RemoveControl( childControl );

			// Destroy the control
			DestroyImmediate( childControl.gameObject );

		}

	}

}
