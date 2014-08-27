/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfWebSprite ) )]
public class dfWebSpriteInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		var control = target as dfWebSprite;
		if( control == null )
			return false;

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "WebSprite Properties", true ) )
			return false;

		dfEditorUtil.LabelWidth = 120f;

		using( dfEditorUtil.BeginGroup( "General" ) )
		{

			var texture = EditorGUILayout.ObjectField( "Texture", control.Texture, typeof( Texture2D ), false ) as Texture2D;
			if( texture != control.Texture )
			{
				dfEditorUtil.MarkUndo( control, "Assign texture" );
				control.Texture = texture;
			}

			var material = EditorGUILayout.ObjectField( "Material", control.Material, typeof( Material ), false ) as Material;
			if( material != control.Material )
			{
				dfEditorUtil.MarkUndo( control, "Assign material" );
				control.Material = material;
			}

			var backColor = EditorGUILayout.ColorField( "Color", control.Color );
			if( backColor != control.Color )
			{
				dfEditorUtil.MarkUndo( control, "Change Sprite Color" );
				control.Color = backColor;
			}

		}

		using( dfEditorUtil.BeginGroup( "Web" ) )
		{

			var autoLoad = EditorGUILayout.Toggle( "Auto Load", control.AutoDownload );
			if( autoLoad != control.AutoDownload )
			{
				dfEditorUtil.MarkUndo( control, "Toggle Auto Download property" );
				control.AutoDownload = autoLoad;
			}

			var url = EditorGUILayout.TextField( "URL", control.URL );
			if( url != control.URL )
			{
				dfEditorUtil.MarkUndo( control, "Modify URL" );
				control.URL = url;
			}

			var loadingImage = EditorGUILayout.ObjectField( "Loading Image", control.LoadingImage, typeof( Texture2D ), false ) as Texture2D;
			if( loadingImage != control.LoadingImage )
			{
				dfEditorUtil.MarkUndo( control, "Change Loading Image" );
				control.LoadingImage = loadingImage;
			}

			var errorImage = EditorGUILayout.ObjectField( "Error Image", control.ErrorImage, typeof( Texture2D ), false ) as Texture2D;
			if( errorImage != control.ErrorImage )
			{
				dfEditorUtil.MarkUndo( control, "Change Error Image" );
				control.ErrorImage = errorImage;
			}

		}

		return true;

	}

}
