using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Examples/Drag and Drop/Inventory Item" )]
public class DndExample_InventoryItem : MonoBehaviour
{

	public string ItemName;
	public int Count;
	public string Icon;

	private static dfPanel _container;
	private static dfSprite _sprite;
	private static dfLabel _label;

	public void OnEnable()
	{
		Refresh();
	}

	public void OnDoubleClick( dfControl source, dfMouseEventArgs args )
	{
		// Nothing special done for double-click, just pass off to OnClick
		OnClick( source, args );
	}

	public void OnClick( dfControl source, dfMouseEventArgs args )
	{

		if( string.IsNullOrEmpty( ItemName ) )
			return;

		if( args.Buttons == dfMouseButtons.Left )
		{
			Count += 1;
		}
		else if( args.Buttons == dfMouseButtons.Right )
		{
			Count = Mathf.Max( Count - 1, 1 );
		}

		Refresh();

	}

	public void OnDragStart( dfControl source, dfDragEventArgs args )
	{

		if( this.Count > 0 )
		{

			args.Data = this;
			args.State = dfDragDropState.Dragging;
			args.Use();

			DnDExample_DragCursor.Show( this, args.Position );

		}

	}

	public void OnDragEnd( dfControl source, dfDragEventArgs args )
	{

		DnDExample_DragCursor.Hide();

		if( args.State == dfDragDropState.Dropped )
		{

			this.Count = 0;
			this.ItemName = "";
			this.Icon = "";

			Refresh();

		}

	}

	public void OnDragDrop( dfControl source, dfDragEventArgs args )
	{

		if( this.Count == 0 && args.Data is DndExample_InventoryItem )
		{

			var item = (DndExample_InventoryItem)args.Data;
			this.ItemName = item.ItemName;
			this.Icon = item.Icon;
			this.Count = item.Count;

			args.State = dfDragDropState.Dropped;
			args.Use();

		}

		Refresh();

	}

	private void Refresh()
	{

		_container = GetComponent<dfPanel>();
		_sprite = _container.Find( "Icon" ) as dfSprite;
		_label = _container.Find( "Count" ) as dfLabel;

		_sprite.SpriteName = this.Icon;
		_label.Text = ( this.Count > 1 ) ? Count.ToString() : "";

	}

}
