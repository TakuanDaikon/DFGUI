using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Sprites/Hover Animation Events" )]
public class HoverAnimEvents : MonoBehaviour 
{

	public dfSpriteAnimation hoverAnimation;

	public void OnMouseEnter( dfControl control, dfMouseEventArgs mouseEvent )
	{
		hoverAnimation.PlayForward();
	}

	public void OnMouseLeave( dfControl control, dfMouseEventArgs mouseEvent )
	{
		hoverAnimation.PlayReverse();
	}

}
