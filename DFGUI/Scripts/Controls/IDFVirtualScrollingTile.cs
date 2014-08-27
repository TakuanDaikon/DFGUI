public interface IDFVirtualScrollingTile 
{
	/// <summary>
	/// Used internally to get / set the virtual index of the object to be passed
	/// through OnScrollPanelItemVirtualize.
	/// </summary>
	int VirtualScrollItemIndex { get; set; }

	/// <summary>
	/// This method is called every time the tile is recycled, and passed a new object from the backing list.
	/// This should be implemented much the same way as you would with MonoBehaviour.Start()
	/// </summary>
	/// <param name="backingListItem">This is the next object pulled from the backing list.</param>
	void OnScrollPanelItemVirtualize( object backingListItem );

	/// <summary>
	/// Get a reference to the dfPanel of this tile.
	/// </summary>
	/// <returns>The root dfPanel of the tile itself.</returns>
	dfPanel GetDfPanel();
}