// @cond DOXY_IGNORE
/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[Serializable]
[ExecuteInEditMode]
[RequireComponent( typeof( Camera ) )]
[AddComponentMenu( "Daikon Forge/User Interface/GUI Camera" )]
public class dfGUICamera : MonoBehaviour
{

	public void Awake() { }
	public void OnEnable() { }

	public void Start()
	{

		camera.transparencySortMode = TransparencySortMode.Orthographic;
		camera.useOcclusionCulling = false;

		// Disable built-in OnMouse* messages - http://unity3d.com/unity/whats-new/unity-4.1
		// The built-in SendMouseEvents() functionality throws errors if you have methods
		// with the same name but different signature, as is the case with many of this
		// library's OnMouse* event handling methods. They really should have implemented
		// silent fail so as not to force developers into this situation.
		camera.eventMask &= ~camera.cullingMask;

	}

}

// @endcond DOXY_IGNORE
