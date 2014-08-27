/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfTiledSprite ) )]
public class dfTiledSpriteInspector : dfSpriteInspector
{

	protected override bool OnCustomInspector()
	{

		if( !base.OnCustomInspector() )
			return false;

		var control = target as dfTiledSprite;
		if( control == null )
			return false;

		var tileScale = EditFloat2( "Tile Scale", "X", "Y", control.TileScale );
		if( tileScale != control.TileScale )
		{
			dfEditorUtil.MarkUndo( control, "Change Tile Scale" );
			control.TileScale = tileScale;
		}

		var tileScroll = EditFloat2( "Tile Offset", "X", "Y", control.TileScroll );
		tileScroll = Vector2.Max( Vector2.zero, tileScroll );
		if( tileScroll != control.TileScroll )
		{
			dfEditorUtil.MarkUndo( control, "Change Tile Offset" );
			control.TileScroll = tileScroll;
		}

		return true;

	}

}
