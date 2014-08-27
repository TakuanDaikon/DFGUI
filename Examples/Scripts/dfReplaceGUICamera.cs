using System;
using System.Collections;

using UnityEngine;

/// <summary>
/// When attached to a dfGUIManager component, allows the user to 
/// override which Camera is responsible for rendering UI content
/// </summary>
[AddComponentMenu( "Daikon Forge/Examples/3D/Replace GUI Camera" )]
public class dfReplaceGUICamera : MonoBehaviour
{

	public Camera mainCamera;

	public void OnEnable()
	{

		// Make sure that we have a reference to the main camera
		if( mainCamera == null )
		{
			mainCamera = Camera.main;
		}

		// Obtain a reference to the attached dfGUIManager
		var view = GetComponent<dfGUIManager>();
		if( view == null )
		{
			Debug.LogError( "This script should be attached to a dfGUIManager instance", this );
			this.enabled = false;
			return;
		}

		// Allow the main camera to render the 3D UI
		mainCamera.cullingMask |= ( 1 << gameObject.layer );

		// Override the GUI's render camera
		view.OverrideCamera = true;
		view.RenderCamera = mainCamera;

	}

}
