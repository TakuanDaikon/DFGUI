/* Copyright 2013-2014 Daikon Forge */

/****************************************************************************
 * PLEASE NOTE: The code in this file is under extremely active development
 * and is likely to change quite frequently. It is not recommended to modify
 * the code in this file, as your changes are likely to be overwritten by
 * the next product update when it is published.
 * **************************************************************************/

using UnityEngine;

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityColor = UnityEngine.Color;
using UnityMaterial = UnityEngine.Material;

/// <summary>
/// Used by the dfRichTestLabel control to load and cache images
/// </summary>
public class dfMarkupImageCache
{

	#region Static variables

	private static Dictionary<string, Texture> cache = new Dictionary<string, Texture>();

	#endregion

	#region Public methods 

	public static void Clear()
	{
		cache.Clear();
	}

	public static void Load( string name, Texture image )
	{
		cache[ name.ToLowerInvariant() ] = image;
	}

	public static void Unload( string name )
	{
		cache.Remove( name.ToLowerInvariant() );
	}

	public static Texture Load( string path )
	{

		path = path.ToLowerInvariant();
		if( cache.ContainsKey( path ) )
		{
			return cache[ path ];
		}

		var texture = Resources.Load( path ) as Texture;
		if( texture != null )
		{
			cache[ path ] = texture;
		}

		return texture;

	}

	#endregion

}

