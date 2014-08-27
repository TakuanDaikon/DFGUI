using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Rich Text/Rich Text Events" )]
public class rtSampleEvents : MonoBehaviour 
{

	public void OnLinkClicked( dfRichTextLabel sender, dfMarkupTagAnchor tag )
	{

		var href = tag.HRef;
		if( href.ToLowerInvariant().StartsWith( "http:" ) )
		{
			Application.OpenURL( href );
		}

	}

}
