using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CircularMenuStateIcon : MonoBehaviour 
{

	public string OffSprite;
	public string OnSprite;

	public dfSprite sprite;
	public dfRadialMenu menu;

	public void OnEnable()
	{

		if( sprite == null )
			sprite = GetComponent<dfSprite>();

		if( menu == null )
			menu = GetComponent<dfRadialMenu>();

		sprite.SpriteName = menu.IsOpen ? OnSprite : OffSprite;

		menu.MenuOpened += OnMenuOpened;
		menu.MenuClosed += OnMenuClosed;

	}

	public void OnMenuOpened( dfRadialMenu menu )
	{
		sprite.SpriteName = OnSprite;
	}

	public void OnMenuClosed( dfRadialMenu menu )
	{
		sprite.SpriteName = OffSprite;
	}

}
