using UnityEngine;
using System.Collections;

[AddComponentMenu( "Daikon Forge/Examples/General/Set Mobile Resolution" )]
public class SetMobileResolution : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
#if UNITY_IPHONE || UNITY_ANDROID
		if( Application.isPlaying && !Application.isEditor )
		{
			Screen.SetResolution( 1366, 768, true );
		}
#endif
	}

}
