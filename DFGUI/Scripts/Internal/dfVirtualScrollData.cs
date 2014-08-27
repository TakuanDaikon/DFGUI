using System.Collections.Generic;
using UnityEngine;

public class dfVirtualScrollData<T>
{

	public IList<T> BackingList;
	public List<IDFVirtualScrollingTile> Tiles = new List<IDFVirtualScrollingTile>();
	public RectOffset ItemPadding;
	public Vector2 LastScrollPosition = Vector2.zero;
	public int MaxExtraOffscreenTiles = 10;
	public IDFVirtualScrollingTile DummyTop;
	public IDFVirtualScrollingTile DummyBottom;
	public bool IsPaging = false;
	public bool IsInitialized = false;

	public void GetNewLimits( bool isVerticalFlow, bool getMaxes, out int index, out float newY )
	{

		var model = Tiles[ 0 ];
		index = model.VirtualScrollItemIndex;
		newY = ( isVerticalFlow )
			   ? model.GetDfPanel().RelativePosition.y
			   : model.GetDfPanel().RelativePosition.x;

		foreach( var tile in Tiles )
		{

			var panel = tile.GetDfPanel();
			var testY = isVerticalFlow
						? panel.RelativePosition.y
						: panel.RelativePosition.x;

			if( getMaxes )
			{
				if( testY > newY )
				{
					newY = testY;
				}

				if( tile.VirtualScrollItemIndex > index )
				{
					index = tile.VirtualScrollItemIndex;
				}
			}
			else
			{
				if( testY < newY )
				{
					newY = testY;
				}

				if( tile.VirtualScrollItemIndex < index )
				{
					index = tile.VirtualScrollItemIndex;
				}
			}

		}

		if( getMaxes )
		{
			index++;
		}
		else
		{
			index--;
		}

	}

}
