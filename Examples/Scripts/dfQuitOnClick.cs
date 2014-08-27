using UnityEngine;
using System.Collections;

[AddComponentMenu( "Daikon Forge/Examples/General/Quit On Click" )]
public class dfQuitOnClick : MonoBehaviour
{

	void OnClick()
	{

#if UNITY_EDITOR
		if( Application.isEditor )
			UnityEditor.EditorApplication.isPlaying = false;
#endif

		Application.Quit();

	}

}
