using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Debugging/Simulate Touch with Mouse" )]
public class dfMouseTouchSourceComponent : dfTouchInputSourceComponent
{

	public bool editorOnly = true;

	private dfMouseTouchInputSource source;

	public override IDFTouchInputSource Source
	{
		get 
		{
			
			if( editorOnly && !Application.isEditor )
				return null;

			if( source == null )
			{
				source = new dfMouseTouchInputSource();
			}
			
			return source;

		}
	}

	public void Start()
	{
		this.useGUILayout = false;
	}

	public void OnGUI()
	{
		if( source != null )
		{
			source.MirrorAlt = !Event.current.control && !Event.current.shift;
			source.ParallelAlt = !source.MirrorAlt && Event.current.shift;
		}
	}

}
