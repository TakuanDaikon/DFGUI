using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class dfJumpButtonEvents : MonoBehaviour 
{

	public bool isMouseDown = false;

	public void OnMouseDown( dfControl control, dfMouseEventArgs mouseEvent )
	{
		isMouseDown = true;
	}

	public void OnMouseUp( dfControl control, dfMouseEventArgs mouseEvent )
	{
		isMouseDown = false;
	}

}
