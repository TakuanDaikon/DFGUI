using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Actionbar/Drag Cursor" )]
public class ActionbarsDragCursor : MonoBehaviour 
{

	private static dfSprite _sprite;
	private static Vector2 _cursorOffset;

	public void Start()
	{
		
		// Obtain a reference to the Sprite that this component is attached to
		_sprite = GetComponent<dfSprite>();
		
		// We don't want the drag cursor visible unless it is being used
		_sprite.Hide();
		
		// We don't want the drag cursor to intercept mouse messages
		_sprite.IsInteractive = false;
		_sprite.IsEnabled = false;

	}


	public void Update()
	{

		if( _sprite.IsVisible )
		{
			setPosition( Input.mousePosition );
		}

	}

	/// <summary>
	/// Displays the drag cursor, which will follow the mouse until hidden
	/// </summary>
	/// <param name="sprite">The sprite to display in the drag cursor</param>
	/// <param name="position">The initial position of the drag cursor</param>
	/// <param name="offset">The mouse offset within the dragged object</param>
	public static void Show( dfSprite sprite, Vector2 position, Vector2 offset )
	{

		_cursorOffset = offset;

		setPosition( position );

		_sprite.Size = sprite.Size;
		_sprite.Atlas = sprite.Atlas;
		_sprite.SpriteName = sprite.SpriteName;
		_sprite.IsVisible = true;
		_sprite.BringToFront();

	}

	public static void Hide()
	{
		_sprite.IsVisible = false;
	}

	private static void setPosition( Vector2 position )
	{

		// Convert position from "screen coordinates" to "gui coordinates"
		position = _sprite.GetManager().ScreenToGui( position );

		// Center the control on the mouse/touch
		_sprite.RelativePosition = position - _cursorOffset;

	}

}
