using UnityEngine;
using System.Collections;

// NOTES: This class is used by the dfSprite examples to provide properties
// that can easily be bound to controls.

[AddComponentMenu( "Daikon Forge/Examples/Sprites/Sprite Properties" )]
public class SpriteProperties : MonoBehaviour
{

	public Texture2D blankTexture;
	public dfSprite sprite;

	#region Unity events 

	void Awake()
	{
		if( sprite == null )
		{
			sprite = GetComponent<dfSprite>();
		}
		ShowBorders = true;
		this.useGUILayout = false;
	}

	void Start() { }

	void OnGUI()
	{

		if( !ShowBorders || blankTexture == null )
			return;

		var screenRect = sprite.GetScreenRect();
		var borders = sprite.SpriteInfo.border;

		var x = screenRect.x;
		var y = screenRect.y;
		var width = screenRect.width;
		var height = screenRect.height;

		var left = borders.left;
		var right = borders.right;
		var top = borders.top;
		var bottom = borders.bottom;

		if( sprite.Flip.IsSet( dfSpriteFlip.FlipHorizontal ) )
		{
			left = borders.right;
			right = borders.left;
		}

		if( sprite.Flip.IsSet( dfSpriteFlip.FlipVertical ) )
		{
			top = borders.bottom;
			bottom = borders.top;
		}

		// NOTE: I've been meaning to add some immediate-mode drawing functions
		// for things like lines and overlay sprites, but that will have to wait
		// until V2 for now.
		//
		// I could easily have chosen to draw the lines using Sprites, but I 
		// think it's kind of illuminating to show how the code below increases 
		// the draw call currentIndex by 4 just to draw some simple lines using 
		// Unity GUI :)

		GUI.color = new Color( 0.5f, 0.5f, 0.5f, 0.5f );
		GUI.DrawTexture( new Rect( x - 10, y + top, width + 20, 1 ), blankTexture );
		GUI.DrawTexture( new Rect( x - 10, y + height - bottom, width + 20, 1 ), blankTexture );
		GUI.DrawTexture( new Rect( x + left, y - 10, 1, height + 20 ), blankTexture );
		GUI.DrawTexture( new Rect( x + width - right, y - 10, 1, height + 20 ), blankTexture );

	}

	#endregion

	#region Public properties

	public bool ShowBorders { get; set; }

	public float PatternScaleX
	{
		// Isolate X and Y components for easy property binding
		get { return ( (dfTiledSprite)sprite ).TileScale.x; }
		set
		{
			var tile = sprite as dfTiledSprite;
			tile.TileScale = new Vector2( value, tile.TileScale.y );
		}
	}

	public float PatternScaleY
	{
		// Isolate X and Y components for easy property binding
		get { return ( (dfTiledSprite)sprite ).TileScale.y; }
		set
		{
			var tile = sprite as dfTiledSprite;
			tile.TileScale = new Vector2( tile.TileScale.x, value );
		}
	}

	public float PatternOffsetX
	{
		// Isolate X and Y components for easy property binding
		get { return ( (dfTiledSprite)sprite ).TileScroll.x; }
		set
		{
			var tile = sprite as dfTiledSprite;
			tile.TileScroll = new Vector2( value, tile.TileScroll.y );
		}
	}

	public float PatternOffsetY
	{
		// Isolate X and Y components for easy property binding
		get { return ( (dfTiledSprite)sprite ).TileScroll.y; }
		set
		{
			var tile = sprite as dfTiledSprite;
			tile.TileScroll = new Vector2( tile.TileScroll.x, value );
		}
	}

	public int FillOrigin
	{
		// Data conversion for design-time property binding
		get { return (int)( (dfRadialSprite)sprite ).FillOrigin; }
		set { ( (dfRadialSprite)sprite ).FillOrigin = (dfPivotPoint)value; }
	}

	public bool FillVertical
	{
		get { return sprite.FillDirection == dfFillDirection.Vertical; }
		set
		{
			if( value )
				sprite.FillDirection = dfFillDirection.Vertical;
			else
				sprite.FillDirection = dfFillDirection.Horizontal;
		}
	}

	public bool FlipHorizontal
	{
		get { return sprite.Flip.IsSet( dfSpriteFlip.FlipHorizontal ); }
		set { sprite.Flip = sprite.Flip.SetFlag( dfSpriteFlip.FlipHorizontal, value ); }
	}

	public bool FlipVertical
	{
		get { return sprite.Flip.IsSet( dfSpriteFlip.FlipVertical ); }
		set { sprite.Flip = sprite.Flip.SetFlag( dfSpriteFlip.FlipVertical, value ); }
	}

	#endregion

}
