using System;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Examples/General/Load Level On Click" )]
[Serializable]
public class dfLoadLevelByName : MonoBehaviour
{

	public string LevelName;

	void OnClick()
	{
		if( !string.IsNullOrEmpty( LevelName ) )
		{
			Application.LoadLevel( LevelName );
		}
	}

}
