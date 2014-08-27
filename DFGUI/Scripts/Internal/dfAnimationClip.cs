using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

/// <summary>
/// Defines an animation clip associated with a Texture Atlas
/// </summary>
[Serializable]
[AddComponentMenu( "Daikon Forge/User Interface/Animation Clip" )]
public class dfAnimationClip : MonoBehaviour
{

	#region Private serialized fields 

	[SerializeField]
	private dfAtlas atlas;

	[SerializeField]
	private List<string> sprites = new List<string>();

	#endregion

	#region Public properties

	/// <summary>
	/// The <see cref="dfAtlas">Texture Atlas</see> containing the images used by this control
	/// </summary>
	public dfAtlas Atlas
	{
		get
		{
			return this.atlas;
		}
		set
		{
			this.atlas = value;
		}
	}

	public List<string> Sprites
	{
		get { return this.sprites; }
	}


	#endregion

}
