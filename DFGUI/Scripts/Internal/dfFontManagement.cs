/* Copyright 2013-2014 Daikon Forge */

/****************************************************************************
 * PLEASE NOTE: The code in this file is under extremely active development
 * and is likely to change quite frequently. It is not recommended to modify
 * the code in this file, as your changes are likely to be overwritten by
 * the next product update when it is published.
 * **************************************************************************/

using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityMaterial = UnityEngine.Material;
using UnityFont = UnityEngine.Font;
using UnityShader = UnityEngine.Shader;

public interface IRendersText
{

	/// <summary>
	/// Indicates that the control should prepare for font rendering, such as 
	/// requesting any characters needed in a dynamic font.
	/// </summary>
	void UpdateFontInfo();

}

public static class dfFontManager
{

	#region Static variables

	private static dfList<dfFontBase> dirty = new dfList<dfFontBase>();
	private static dfList<dfFontBase> rebuildList = new dfList<dfFontBase>();

	#endregion

	#region Public methods

	public static void FlagPendingRequests( dfFontBase font )
	{

		var dynamicFont = font as dfDynamicFont;
		if( dynamicFont != null )
		{
			if( !rebuildList.Contains( dynamicFont ) )
			{
				rebuildList.Add( dynamicFont );
			}
		}
		
	}

	public static void Invalidate( dfFontBase font )
	{

		if( font == null || !( font is dfDynamicFont ) )
			return;

		if( !dirty.Contains( font ) )
		{
			dirty.Add( font );
		}

	}

	public static bool IsDirty( dfFontBase font )
	{
		return dirty.Contains( font );
	}

	public static bool RebuildDynamicFonts()
	{

		var fontsRebuilt = false;

		rebuildList.Clear();

		var controls = dfControl.ActiveInstances;
		for( int i = 0; i < controls.Count; i++ )
		{

			var label = controls[ i ] as IRendersText;
			if( label != null )
			{
				label.UpdateFontInfo();
			}

		}

		fontsRebuilt = rebuildList.Count > 0;

		for( int i = 0; i < rebuildList.Count; i++ )
		{
			var dynamicFont = rebuildList[ i ] as dfDynamicFont;
			if( dynamicFont != null )
			{
				dynamicFont.FlushCharacterRequests();
			}
		}

		rebuildList.Clear();
		dirty.Clear();

		return fontsRebuilt;

	}

	#endregion

}

