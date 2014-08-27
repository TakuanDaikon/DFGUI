using System;
using System.Collections;

using UnityEngine;

/// <summary>
/// Eases development by allowing the "Pixel Perfect" behavior to be
/// different between running in the Editor vs. running on the target
/// </summary>
[AddComponentMenu( "Daikon Forge/Examples/General/Pixel-Perfect Platform Settings" )]
public class RuntimePixelPerfect : MonoBehaviour
{

	public bool PixelPerfectInEditor = false;
	public bool PixelPerfectAtRuntime = true;

	void Awake()
	{

		var manager = GetComponent<dfGUIManager>();
		if( manager == null )
			throw new MissingComponentException( "dfGUIManager instance not found" );

		if( Application.isEditor )
			manager.PixelPerfectMode = PixelPerfectInEditor;
		else
			manager.PixelPerfectMode = PixelPerfectAtRuntime;

	}

}
