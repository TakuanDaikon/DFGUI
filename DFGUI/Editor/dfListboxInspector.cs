/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfListbox ) )]
public class dfListboxInspector : dfControlInspector
{
	
	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		var control = target as dfListbox;
		if( control == null )
			return false;

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Listbox Properties", true ) )
			return false;

		dfEditorUtil.LabelWidth = 105f;

		SelectTextureAtlas( "Atlas", control, "Atlas", false, true );

		using( dfEditorUtil.BeginGroup( "Listbox" ) )
		{

			SelectSprite( "Back Sprite", control.Atlas, control, "BackgroundSprite", false );

			var backColor = EditorGUILayout.ColorField( "Back Color", control.Color );
			if( backColor != control.Color )
			{
				dfEditorUtil.MarkUndo( control, "Change Background Color" );
				control.Color = backColor;
			}

			var listPadding = dfEditorUtil.EditPadding( "Padding", control.ListPadding );
			if( !listPadding.Equals( control.ListPadding ) )
			{
				dfEditorUtil.MarkUndo( control, "Modify padding" );
				control.ListPadding = listPadding;
			}

			var scrollbar = EditorGUILayout.ObjectField( "Scrollbar", control.Scrollbar, typeof( dfScrollbar ), true ) as dfScrollbar;
			if( scrollbar != control.Scrollbar )
			{
				dfEditorUtil.MarkUndo( control, "Assign ScrollBar" );
				control.Scrollbar = scrollbar;
			}

			var animateHover = EditorGUILayout.Toggle( "Animate Hover", control.AnimateHover );
			if( animateHover != control.AnimateHover )
			{
				dfEditorUtil.MarkUndo( control, "Change AnimateHover property" );
				control.AnimateHover = animateHover;
			}

		}

		using( dfEditorUtil.BeginGroup( "List Item Appearance" ) )
		{

			SelectFontDefinition( "Font", control.Atlas, control, "Font", true, true );

			var textColor = EditorGUILayout.ColorField( "Text Color", control.ItemTextColor );
			if( textColor != control.ItemTextColor )
			{
				dfEditorUtil.MarkUndo( control, "Change Text Color" );
				control.ItemTextColor = textColor;
			}

			var effectiveFontSize = Mathf.CeilToInt( control.Font.FontSize * control.ItemTextScale );
			EditorGUI.BeginChangeCheck();
			effectiveFontSize = EditorGUILayout.IntField( "Font Size", effectiveFontSize );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( control, "Change Font Size" );
				control.ItemTextScale = (float)effectiveFontSize / (float)control.Font.FontSize;
			}

			//var scaleMode = (dfTextScaleMode)EditorGUILayout.EnumPopup( "Auto Scale", control.TextScaleMode );
			//if( scaleMode != control.TextScaleMode )
			//{
			//    dfEditorUtil.MarkUndo( control, "Change Text Scale Mode" );
			//    control.TextScaleMode = scaleMode;
			//}

			var alignment = (TextAlignment)EditorGUILayout.EnumPopup( "Alignment", control.ItemAlignment );
			if( alignment != control.ItemAlignment )
			{
				dfEditorUtil.MarkUndo( control, "Change Item Text Alignment" );
				control.ItemAlignment = alignment;
			}

			var itemHeight = EditorGUILayout.IntField( "Item Height", control.ItemHeight );
			if( itemHeight != control.ItemHeight )
			{
				dfEditorUtil.MarkUndo( control, "Change Item Height" );
				control.ItemHeight = itemHeight;
			}

			SelectSprite( "Highlight", control.Atlas, control, "ItemHighlight", false );
			SelectSprite( "Hover", control.Atlas, control, "ItemHover", false );

			var padding = dfEditorUtil.EditPadding( "Text Padding", control.ItemPadding );
			if( padding != control.ItemPadding )
			{
				dfEditorUtil.MarkUndo( control, "Change control Padding" );
				control.ItemPadding = padding;
			}

			var shadow = EditorGUILayout.Toggle( "Shadow Effect", control.Shadow );
			if( shadow != control.Shadow )
			{
				dfEditorUtil.MarkUndo( control, "Change Shadow Effect" );
				control.Shadow = shadow;
			}

			if( shadow )
			{

				var shadowColor = EditorGUILayout.ColorField( "Shadow Color", control.ShadowColor );
				if( shadowColor != control.ShadowColor )
				{
					dfEditorUtil.MarkUndo( control, "Change Shadow Color" );
					control.ShadowColor = shadowColor;
				}

				var shadowOffset = dfEditorUtil.EditInt2( "Shadow Offset", "X", "Y", control.ShadowOffset );
				if( shadowOffset != control.ShadowOffset )
				{
					dfEditorUtil.MarkUndo( control, "Change Shadow Color" );
					control.ShadowOffset = shadowOffset;
				}

				dfEditorUtil.LabelWidth = 120f;

			}

			dfEditorUtil.LabelWidth = 110f;

		}

		using( dfEditorUtil.BeginGroup( "List Data" ) )
		{

			var totalItemHeight = control.Items.Length * control.ItemHeight;
			var clientHeight = control.Size.y - control.ListPadding.vertical;
			var maxScroll = totalItemHeight < clientHeight ? 0 : totalItemHeight - clientHeight;
			var scroll = EditorGUILayout.Slider( "Scroll Position", control.ScrollPosition, 0, maxScroll );
			if( !Mathf.Approximately( scroll, control.ScrollPosition ) )
			{
				dfEditorUtil.MarkUndo( control, "Change Scroll Index" );
				control.ScrollPosition = scroll;
			}

			var index = EditorGUILayout.IntSlider( "Selected Index", control.SelectedIndex, -1, control.Items.Length - 1 );
			if( index != control.SelectedIndex )
			{
				dfEditorUtil.MarkUndo( control, "Change Selected Index" );
				control.SelectedIndex = index;
			}

			EditOptions( control );

		}

		return true;

	}

	private static void EditOptions( dfListbox control )
	{

		GUILayout.BeginHorizontal();
		{

			EditorGUILayout.LabelField( "Options", "", GUILayout.Width( 100 ) );

			EditorGUI.BeginChangeCheck();
			var optionsString = string.Join( "\n", control.Items );
			var optionsEdit = EditorGUILayout.TextArea( optionsString, GUILayout.Height( 100f ), GUILayout.MaxWidth( 225 ) );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( control, "Change options" );
				var options = optionsEdit.Trim().Split( new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries );
				control.Items = options;
			}

		}
		GUILayout.EndHorizontal();

		dfEditorUtil.LabelWidth = 100f;

	}

}
