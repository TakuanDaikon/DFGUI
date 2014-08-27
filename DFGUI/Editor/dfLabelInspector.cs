#define USE_NEW_BMFONT_RENDERER
// Uncomment the preceeding line if you wish to revert to the old 
// bitmapped font renderer

/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Object = UnityEngine.Object;

[CanEditMultipleObjects()]
[CustomEditor( typeof( dfLabel ) )]
public class dfLabelInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Label Properties", true ) )
			return false;

		var control = target as dfLabel;

		dfEditorUtil.LabelWidth = 120f;

		using( dfEditorUtil.BeginGroup( "Atlas" ) )
		{

			SelectTextureAtlas( "Atlas", control, "Atlas", false, true );
			if( control.GUIManager != null && !dfAtlas.Equals( control.Atlas, control.GUIManager.DefaultAtlas ) )
			{
				EditorGUILayout.HelpBox( "This control does not use the same Texture Atlas as the View, which will result in an additional draw call.", MessageType.Info );
			}

			SelectFontDefinition( "Font", control.Atlas, control, "Font", true, true );

		}

		if( control.Font == null )
			return false;

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			var autoSize = EditorGUILayout.Toggle( "Auto Size", control.AutoSize && !control.AutoHeight );
			if( autoSize != control.AutoSize )
			{
				dfEditorUtil.MarkUndo( control, "Change label Auto Size" );
				control.AutoSize = autoSize;
			}

			GUI.enabled = !autoSize;
			{

				var autoHeight = EditorGUILayout.Toggle( "Auto Height", control.AutoHeight && !autoSize );
				if( autoHeight != control.AutoHeight )
				{
					dfEditorUtil.MarkUndo( control, "Change label Auto Height" );
					control.AutoHeight = autoHeight;
				}

				GUI.enabled = true;

			}

			var effectiveFontSize = Mathf.CeilToInt( control.Font.FontSize * control.TextScale );
			EditorGUI.BeginChangeCheck();
			effectiveFontSize = EditorGUILayout.IntField( "Font Size", effectiveFontSize );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( control, "Change Font Size" );
				control.TextScale = (float)effectiveFontSize / (float)control.Font.FontSize;
			}

			var scaleMode = (dfTextScaleMode)EditorGUILayout.EnumPopup( "Auto Scale", control.TextScaleMode );
			if( scaleMode != control.TextScaleMode )
			{
				dfEditorUtil.MarkUndo( control, "Change Text Scale Mode" );
				control.TextScaleMode = scaleMode;
			}

			var spacing = EditorGUILayout.IntField( "Char Spacing", control.CharacterSpacing );
			if( spacing != control.CharacterSpacing )
			{
				dfEditorUtil.MarkUndo( control, "Change Character Spacing" );
				control.CharacterSpacing = spacing;
			}

		}

		using( dfEditorUtil.BeginGroup( "Background and Colors" ) )
		{

			SelectSprite( "Background", control.Atlas, control, "BackgroundSprite", false );

			var backColor = EditorGUILayout.ColorField( "Back Color", control.BackgroundColor );
			if( backColor != control.BackgroundColor )
			{
				dfEditorUtil.MarkUndo( control, "Change Background Color" );
				control.BackgroundColor = backColor;
			}

			var textColor = EditorGUILayout.ColorField( "Text Color", control.Color );
			if( textColor != control.Color )
			{
				dfEditorUtil.MarkUndo( control, "Change Text Color" );
				control.Color = textColor;
			}

		}

		using( dfEditorUtil.BeginGroup( "Tabs" ) )
		{

			var tabSize = EditorGUILayout.IntField( "Tab Size", control.TabSize );
			if( tabSize != control.TabSize )
			{
				dfEditorUtil.MarkUndo( control, "Change Tab Size" );
				control.TabSize = tabSize;
			}

#if !USE_NEW_BMFONT_RENDERER
			var tabStops = control.TabStops;

			var tabStopCount = Mathf.Max( EditorGUILayout.IntField( "Tab Stops", tabStops.Count ), 0 );

			if( tabStopCount != tabStops.Count )
			{

				dfEditorUtil.MarkUndo( control, "Add/Remove Tab Stops" );
				control.Invalidate();

				while( tabStopCount < tabStops.Count )
					tabStops.RemoveAt( tabStops.Count - 1 );

				var lastTabStop = tabStops.Count > 0 ? tabStops[ tabStops.Count - 1 ] : 0;
				while( tabStopCount > tabStops.Count )
					tabStops.Add( lastTabStop );

			}

			EditorGUI.indentLevel += 1;

			var minColSize = 0;
			for( int i = 0; i < tabStops.Count; i++ )
			{

				var column = Mathf.Max( EditorGUILayout.IntField( "Column " + ( i + 1 ), tabStops[ i ] ), minColSize );
				if( tabStops[ i ] != column )
				{

					tabStops[ i ] = column;

					dfEditorUtil.MarkUndo( control, "Modify Tab Stop" );
					control.Invalidate();

				}

				minColSize = column;

			}

			EditorGUI.indentLevel -= 1;

#endif

		}

		using( dfEditorUtil.BeginGroup( "Formatting" ) )
		{

			var align = (TextAlignment)EditorGUILayout.EnumPopup( "Text Align", control.TextAlignment );
			if( align != control.TextAlignment )
			{
				dfEditorUtil.MarkUndo( control, "Change label Text Alignment" );
				control.TextAlignment = align;
			}

			var vertAlign = (dfVerticalAlignment)EditorGUILayout.EnumPopup( "Vert Align", control.VerticalAlignment );
			if( vertAlign != control.VerticalAlignment )
			{
				dfEditorUtil.MarkUndo( control, "Change Vertical Alignment" );
				control.VerticalAlignment = vertAlign;
			}

			var wrap = EditorGUILayout.Toggle( "Word Wrap", control.WordWrap );
			if( wrap != control.WordWrap )
			{
				dfEditorUtil.MarkUndo( control, "Change label Word Wrap" );
				control.WordWrap = wrap;
			}

			var markup = EditorGUILayout.Toggle( "Process Markup", control.ProcessMarkup );
			if( markup != control.ProcessMarkup )
			{
				dfEditorUtil.MarkUndo( control, "Change Process Markup" );
				control.ProcessMarkup = markup;
			}

			GUI.enabled = markup;

			var colorize = EditorGUILayout.Toggle( "Colorize Sprites", control.ColorizeSymbols && markup );
			if( colorize != control.ColorizeSymbols )
			{
				dfEditorUtil.MarkUndo( control, "Change Colorize Sprites" );
				control.ColorizeSymbols = colorize;
			}

			GUI.enabled = true;

			var padding = dfEditorUtil.EditPadding( "Padding", control.Padding );
			if( padding != control.Padding )
			{
				dfEditorUtil.MarkUndo( control, "Change Padding" );
				control.Padding = padding;
			}

		}

		using( dfEditorUtil.BeginGroup( "Text Effects" ) )
		{

			var showGradient = EditorGUILayout.Toggle( "Draw Gradient", control.ShowGradient );
			if( showGradient != control.ShowGradient )
			{
				dfEditorUtil.MarkUndo( control, "Toggle label gradient" );
				control.ShowGradient = showGradient;
			}

			if( showGradient )
			{

				EditorGUI.indentLevel += 1;

				var textColor = EditorGUILayout.ColorField( "Top Color", control.Color );
				if( textColor != control.Color )
				{
					dfEditorUtil.MarkUndo( control, "Change Text Color" );
					control.Color = textColor;
				}

				var bottomColor = EditorGUILayout.ColorField( "Bottom Color", control.BottomColor );
				if( bottomColor != control.BottomColor )
				{
					dfEditorUtil.MarkUndo( control, "Change Text Color" );
					control.BottomColor = bottomColor;
				}

				EditorGUI.indentLevel -= 1;

			}

			var outline = EditorGUILayout.Toggle( "Draw Outline", control.Outline );
			if( outline != control.Outline )
			{
				dfEditorUtil.MarkUndo( control, "Change Label Outline" );
				control.Outline = outline;
			}

			if( outline )
			{

				EditorGUI.indentLevel += 1;

				var outlineSize = EditorGUILayout.IntField( "Outline Size", control.OutlineSize );
				if( outlineSize != control.OutlineSize )
				{
					dfEditorUtil.MarkUndo( control, "Change Outline Color" );
					control.OutlineSize = outlineSize;
				}

				var outlineColor = EditorGUILayout.ColorField( "Outline Color", control.OutlineColor );
				if( outlineColor != control.OutlineColor )
				{
					dfEditorUtil.MarkUndo( control, "Change Outline Color" );
					control.OutlineColor = outlineColor;
				}

				EditorGUI.indentLevel -= 1;

			}

			var shadow = EditorGUILayout.Toggle( "Draw Shadow", control.Shadow );
			if( shadow != control.Shadow )
			{
				dfEditorUtil.MarkUndo( control, "Change Shadow Effect" );
				control.Shadow = shadow;
			}

			if( shadow )
			{

				EditorGUI.indentLevel += 1;

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

				EditorGUI.indentLevel -= 1;

			}

		}

		var showDialog = false;
		using( dfEditorUtil.BeginGroup( "Text" ) )
		{

			GUI.SetNextControlName( "Text" );
			var text = EditorGUILayout.TextArea( control.Text, GUI.skin.textArea, GUILayout.Height( 200f ) );
			if( text != control.Text )
			{
				dfEditorUtil.MarkUndo( control, "Change label Text" );
				control.Text = text;
			}

			//if( GUILayout.Button( "Open Editor" ) )
			//{
			//    showDialog = true;
			//}

		}

		// Moved the dialog display code outside of all grouping code to resolve
		// an InvalidOperationException that happens in some circumstances and 
		// appears to be Mac-specific
		if( showDialog )
		{
			dfTextEditorWindow.Show( "Edit Label Text", control.Text, ( text ) =>
			{
				control.Text = text;
			} );
		}

		return true;

	}

}
