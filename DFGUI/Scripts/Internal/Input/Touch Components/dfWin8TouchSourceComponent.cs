// NOTE: This class is highly experimental, and is not part of the official
// Daikon Forge GUI Library package. It is provided for your use should you
// choose to attempt to make use of it. As we don't currently have the 
// hardware available to fully test this, we do not promise that it works
// correctly in all cases, or even at all. We hope that it works well, and
// that it will provide a solid starting point for anyone who wishes to 
// extend the functionality, but we make absolutely no warranties.

#if USE_WIN8_TOUCH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Windows 8 Touch Input" )]
public class dfWin8TouchSourceComponent : dfTouchInputSourceComponent
{

	private dfWin8TouchInputSource source;

	public override IDFTouchInputSource Source
	{
		get
		{

			if( Application.isEditor )
				return null;

			if( source == null )
			{
				source = new dfWin8TouchInputSource();
				if( !source.Initialize() )
					return null;
			}

			return source;

		}
	}

	public void Start()
	{
		// Only included so that the component can be enabled/disabled in the inspector
	}

	public void OnDestroy()
	{
		if( source != null )
		{
			source.Cleanup();
			source = null;
		}
	}

}

#endif