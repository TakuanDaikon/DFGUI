/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfSprite ) )]
public class dfSpriteInspector : dfControlInspector
{

	private static Texture2D lineTex;

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	public override void OnInspectorGUI()
	{

		if( switchSpriteType() )
			return;

		base.OnInspectorGUI();

	}

	protected override bool OnCustomInspector()
	{

		var control = target as dfSprite;
		if( control == null )
			return false;

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Sprite Properties", true ) )
			return false;

		dfEditorUtil.LabelWidth = 110f;

		using( dfEditorUtil.BeginGroup( "Sprite" ) )
		{

			SelectTextureAtlas( "Atlas", control, "Atlas", false, true );

			if( control.Atlas == null )
				return false;

			SelectSprite( "Sprite", control.Atlas, control, "SpriteName" );

			EditorGUI.BeginChangeCheck();
			var color = EditorGUILayout.ColorField( "Color", control.Color );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( control, "Assign color" );
				control.Color = color;
			}

		}

		using( dfEditorUtil.BeginGroup( "Flip" ) )
		{

			var flipHorz = EditorGUILayout.Toggle( "Flip Horz", ( control.Flip & dfSpriteFlip.FlipHorizontal ) == dfSpriteFlip.FlipHorizontal );
			var flipVert = EditorGUILayout.Toggle( "Flip Vert", ( control.Flip & dfSpriteFlip.FlipVertical ) == dfSpriteFlip.FlipVertical );
			var flip = dfSpriteFlip.None;
			if( flipHorz ) flip |= dfSpriteFlip.FlipHorizontal;
			if( flipVert ) flip |= dfSpriteFlip.FlipVertical;
			if( flip != control.Flip )
			{
				dfEditorUtil.MarkUndo( control, "Change Sprite Flip" );
				control.Flip = flip;
			}

		}

		if( !( control is dfRadialSprite ) )
		{

			using( dfEditorUtil.BeginGroup( "Fill" ) )
			{

				var fillType = (dfFillDirection)EditorGUILayout.EnumPopup( "Fill Direction", control.FillDirection );
				if( fillType != control.FillDirection )
				{
					dfEditorUtil.MarkUndo( control, "Change Sprite Fill Direction" );
					control.FillDirection = fillType;
				}

				var fillAmount = EditorGUILayout.Slider( "Fill Amount", control.FillAmount, 0, 1 );
				if( !Mathf.Approximately( fillAmount, control.FillAmount ) )
				{
					dfEditorUtil.MarkUndo( control, "Change Sprite Fill Amount" );
					control.FillAmount = fillAmount;
				}

				var invert = EditorGUILayout.Toggle( "Invert Fill", control.InvertFill );
				if( invert != control.InvertFill )
				{
					dfEditorUtil.MarkUndo( control, "Change Sprite Invert Fill" );
					control.InvertFill = invert;
				}

			}

		}

		return true;

	}

	private bool switchSpriteType()
	{

		var spriteTypeNames = new string[] { "Basic", "Sliced", "Tiled", "Radial" };
		var spriteTypes = new Type[] 
		{
		    typeof( dfSprite ),
		    typeof( dfSlicedSprite ),
		    typeof( dfTiledSprite ),
		    typeof( dfRadialSprite )
		}.ToList();

		var selectedIndex = spriteTypes.IndexOf( target.GetType() );
		var newIndex = EditorGUILayout.Popup( "Sprite Type", selectedIndex, spriteTypeNames );
		if( newIndex != selectedIndex )
		{

			var scriptProperty = this.serializedObject.FindProperty( "m_Script" );
			if( scriptProperty == null )
			{
				return false;
			}

			var replacementScript = Resources
				.FindObjectsOfTypeAll( typeof( MonoScript ) )
				.Cast<MonoScript>()
				.Where( x =>
					x.GetType() == typeof( MonoScript ) && // Fix for Unity crash bug
					x.GetClass() == spriteTypes[ newIndex ]
				)
				.FirstOrDefault();

			if( replacementScript == null )
				return false;

			var activeObject = Selection.activeGameObject;
			Selection.activeGameObject = null;

			dfEditorUtil.DelayedInvoke( () =>
			{

				// Assign the selected MonoScript 
				scriptProperty.objectReferenceValue = replacementScript;
				scriptProperty.serializedObject.ApplyModifiedProperties();
				scriptProperty.serializedObject.Update();
				scriptProperty.serializedObject.SetIsDifferentCacheDirty();

				// Obtain a reference to the new dfSprite component
				var sprite = activeObject.GetComponent<dfSprite>();
				sprite.Invalidate();

				// Save the scene in case Unity crashes
				EditorUtility.SetDirty( sprite );
				EditorApplication.SaveScene();
				EditorApplication.SaveAssets();

				dfEditorUtil.DelayedInvoke( () =>
				{
					forceRecompile( replacementScript );
					Selection.activeGameObject = activeObject;
				} );

			} );

			return true;

		}

		return false;

	}

	private void forceRecompile( MonoScript script )
	{

		// This is a hack, but is the only way I've found so far to get Unity to recognize that
		// there is a whole new script attached to a serialized object

		AssetDatabase.StartAssetEditing();
		try
		{
			var assetPath = AssetDatabase.GetAssetPath( script );
			AssetDatabase.ImportAsset( assetPath, ImportAssetOptions.ForceUpdate );
		}
		catch { }
		AssetDatabase.StopAssetEditing();

	}

	public override bool HasPreviewGUI()
	{
		var sprite = target as dfSprite;
		return sprite != null && sprite.SpriteInfo != null;
	}

	public override void OnPreviewGUI( Rect rect, GUIStyle background )
	{

		// Do not draw preview if multiple objects are selected, it 
		// causes Unity to have apoplexy and the falling fits
		if( Selection.objects.Length > 1 )
			return;

		var sprite = target as dfSprite;

		var spriteInfo = sprite.SpriteInfo;
		if( spriteInfo == null )
			return;

		var size = sprite.SpriteInfo.sizeInPixels;
		var destRect = rect;

		if( destRect.width < size.x || destRect.height < size.y )
		{

			var newHeight = size.y * rect.width / size.x;
			if( newHeight <= rect.height )
				destRect.height = newHeight;
			else
				destRect.width = size.x * rect.height / size.y;

		}
		else
		{
			destRect.width = size.x;
			destRect.height = size.y;
		}

		if( destRect.width < rect.width ) destRect.x = rect.x + ( rect.width - destRect.width ) * 0.5f;
		if( destRect.height < rect.height ) destRect.y = rect.y + ( rect.height - destRect.height ) * 0.5f;

		//EditorGUI.DrawPreviewTexture( destRect, texture );
		dfEditorUtil.DrawSprite( destRect, sprite.Atlas, sprite.SpriteName );

		var border = spriteInfo.border;
		if( border.horizontal > 0 || border.vertical > 0 )
		{

			var lineColor = Color.white;
			lineColor.a = 0.7f;

			var left = Mathf.Floor( destRect.x + border.left * ( destRect.width / size.x ) );
			DrawLine( left, rect.y, rect.height, true, lineColor );

			var right = Mathf.Ceil( destRect.x + destRect.width - border.right * ( destRect.width / size.x ) );
			DrawLine( right, rect.y, rect.height, true, lineColor );

			var top = Mathf.Floor( destRect.y + border.top * ( destRect.height / size.y ) );
			DrawLine( rect.x, top, rect.width, false, lineColor );

			var bottom = Mathf.Ceil( destRect.y + destRect.height - border.bottom * ( destRect.height / size.y ) );
			DrawLine( rect.x, bottom, rect.width, false, lineColor );

		}

		string text = string.Format( "Sprite Size: {0}x{1}", size.x, size.y );
		EditorGUI.DropShadowLabel( GUILayoutUtility.GetRect( Screen.width, 18f ), text );

	}

	private void DrawLine( float left, float top, float size, bool vert, Color color )
	{

		if( !lineTex ) 
		{ 
			lineTex = new Texture2D( 1, 1 ) { hideFlags = HideFlags.HideAndDontSave }; 
		}

		var saveColor = GUI.color;
		GUI.color = color;

		if( !vert )
			GUI.DrawTexture( new Rect( left, top, size, 1 ), lineTex );
		else
			GUI.DrawTexture( new Rect( left, top, 1, size ), lineTex );

		GUI.color = saveColor;

	}

}
