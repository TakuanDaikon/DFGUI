using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Drag and Drop/Drag Cursor" )]
public class DnDExample_DragCursor : MonoBehaviour 
{

	private static dfSprite _sprite;
	private static dfLabel _label;

	// Called by Unity just before any of the Update methods is called the first time.
	public void Start()
	{

		_sprite = GetComponent<dfSprite>();
		_sprite.IsVisible = false;

		_label = _sprite.Find<dfLabel>( "Count" );

	}

	public void Update()
	{
		if( _sprite.IsVisible )
		{
			SetPosition( Input.mousePosition );
		}
	}

	public static void Show( DndExample_InventoryItem item, Vector2 Position )
	{

		SetPosition( Position );

		_sprite.SpriteName = item.Icon;
		_sprite.IsVisible = true;
		_sprite.BringToFront();

		_label.Text = ( item.Count > 1 ) ? item.Count.ToString() : "";

	}

	public static void Hide()
	{
		_sprite.IsVisible = false;
	}

	public static void SetPosition( Vector2 position )
	{

		// Convert position from "screen coordinates" to "gui coordinates"
		position = _sprite.GetManager().ScreenToGui( position );

		// Center the control on the mouse/touch
		_sprite.RelativePosition = position - _sprite.Size * 0.5f;

	}

}
