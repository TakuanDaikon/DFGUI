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

[CanEditMultipleObjects()]
[CustomEditor( typeof( dfRichTextLabel ) )]
public class dfRichTextLabelInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Label Properties", true ) )
			return false;

		var control = target as dfRichTextLabel;

		dfEditorUtil.LabelWidth = 120f;

		using( dfEditorUtil.BeginGroup( "Defaults" ) )
		{

			inspectFont( control );

			if( control.Font == null )
				return false;

			SelectTextureAtlas( "Default Atlas", control, "Atlas", false, true );
			if( control.GUIManager != null && !dfAtlas.Equals( control.Atlas, control.GUIManager.DefaultAtlas ) )
			{
				EditorGUILayout.HelpBox( "This control does not use the same Texture Atlas as the View, which will result in an additional draw call.", MessageType.Info );
			}

			SelectSprite( "Blank Texture", control.Atlas, control, "BlankTextureSprite" );
			if( string.IsNullOrEmpty( control.BlankTextureSprite ) )
			{
				EditorGUILayout.HelpBox( "This control needs a blank texture to use for rendering the selection, background and cursor", MessageType.Info );
			}

		}

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			var fontSize = EditorGUILayout.IntField( "Font Size", control.FontSize );
			if( fontSize != control.FontSize )
			{
				dfEditorUtil.MarkUndo( control, "Change Font Size" );
				control.FontSize = fontSize;
				control.Invalidate();
			}

			var lineheight = EditorGUILayout.IntField( "Line Height", control.LineHeight );
			if( lineheight != control.LineHeight )
			{
				dfEditorUtil.MarkUndo( control, "Change Line Height" );
				control.LineHeight = lineheight;
			}

			var scaleMode = (dfTextScaleMode)EditorGUILayout.EnumPopup( "Auto Scale", control.TextScaleMode );
			if( scaleMode != control.TextScaleMode )
			{
				dfEditorUtil.MarkUndo( control, "Change Text Scale Mode" );
				control.TextScaleMode = scaleMode;
			}

			var fontStyle = (FontStyle)EditorGUILayout.EnumPopup( "Text Style", control.FontStyle );
			if( fontStyle != control.FontStyle )
			{
				dfEditorUtil.MarkUndo( control, "Change Font Style" );
				control.FontStyle = fontStyle;
			}

			var preserveWhitespace = EditorGUILayout.Toggle( "Keep Whitespace", control.PreserveWhitespace );
			if( preserveWhitespace != control.PreserveWhitespace )
			{
				dfEditorUtil.MarkUndo( control, "Change Preserve Whitespace" );
				control.PreserveWhitespace = preserveWhitespace;
			}

			var textColor = EditorGUILayout.ColorField( "Text Color", control.Color );
			if( textColor != control.Color )
			{
				dfEditorUtil.MarkUndo( control, "Change Text Color" );
				control.Color = textColor;
			}

		}

		using( dfEditorUtil.BeginGroup( "Alignment" ) )
		{

			var align = (dfMarkupTextAlign)EditorGUILayout.EnumPopup( "Text Align", control.TextAlignment );
			if( align != control.TextAlignment )
			{
				dfEditorUtil.MarkUndo( control, "Change label Text Alignment" );
				control.TextAlignment = align;
			}

		}

		using( dfEditorUtil.BeginGroup( "Layout" ) )
		{

			var autoHeight = EditorGUILayout.Toggle( "Auto Height", control.AutoHeight );
			if( control.AutoHeight != autoHeight )
			{
				dfEditorUtil.MarkUndo( control, "Toggle Auto Height property" );
				control.AutoHeight = autoHeight;
			}

		}

		if( !control.AutoHeight )
		{

			using( dfEditorUtil.BeginGroup( "Scrolling" ) )
			{

				var allowScrolling = EditorGUILayout.Toggle( "Allow Scroll", control.AllowScrolling );
				if( allowScrolling != control.AllowScrolling )
				{
					dfEditorUtil.MarkUndo( control, "Toggle 'Allow Scrolling'" );
					control.AllowScrolling = allowScrolling;
				}

				GUI.enabled = allowScrolling;

				var scrollOffset = dfEditorUtil.EditInt2( "Scroll Pos.", "X", "Y", control.ScrollPosition );
				if( scrollOffset != control.ScrollPosition )
				{
					dfEditorUtil.MarkUndo( control, "Change Scroll Position" );
					control.ScrollPosition = scrollOffset;
				}

				var useMomentum = EditorGUILayout.Toggle( "Add Momentum", control.UseScrollMomentum );
				if( useMomentum != control.UseScrollMomentum )
				{
					dfEditorUtil.MarkUndo( control, "Toggle Momentum" );
					control.UseScrollMomentum = useMomentum;
				}

				var horzScroll = (dfScrollbar)EditorGUILayout.ObjectField( "Horz. Scrollbar", control.HorizontalScrollbar, typeof( dfScrollbar ), true );
				if( horzScroll != control.HorizontalScrollbar )
				{
					dfEditorUtil.MarkUndo( control, "Set Horizontal Scrollbar" );
					control.HorizontalScrollbar = horzScroll;
				}

				var vertScroll = (dfScrollbar)EditorGUILayout.ObjectField( "Vert. Scrollbar", control.VerticalScrollbar, typeof( dfScrollbar ), true );
				if( vertScroll != control.VerticalScrollbar )
				{
					dfEditorUtil.MarkUndo( control, "Set Vertical Scrollbar" );
					control.VerticalScrollbar = vertScroll;
				}

				GUI.enabled = true;

			}

		}

		var showDialog = false;
		using( dfEditorUtil.BeginGroup( "Text" ) )
		{

			GUI.SetNextControlName( "Text" );
			var text = EditorGUILayout.TextArea( control.Text, GUI.skin.textArea, GUILayout.Height( 300f ) );
			if( text != control.Text )
			{
				dfEditorUtil.MarkUndo( control, "Change label Text" );
				control.Text = text;
			}

		}

		// Moved the dialog display code outside of all grouping code to resolve
		// an InvalidOperationException that happens in some circumstances and 
		// appears to be Mac-specific
		if( showDialog )
		{
			dfTextEditorWindow.Show( "Edit Rich Text", control.Text, ( text ) =>
			{
				control.Text = text;
			} );
		}

		return true;

	}

	private static void inspectFont( dfRichTextLabel control )
	{

		dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
		{
			var font = ( item == null ) ? null : item.GetComponent<dfDynamicFont>();
			dfEditorUtil.MarkUndo( control, "Assign Dynamic Font" );
			control.Font = font;
		};

		var value = control.Font;

		EditorGUILayout.BeginHorizontal();
		{

			EditorGUILayout.LabelField( "Default Font", "", GUILayout.Width( dfEditorUtil.LabelWidth ) );

			var displayText = value == null ? "[none]" : value.name;
			GUILayout.Label( displayText, "TextField" );

			var evt = Event.current;
			if( evt != null )
			{
				Rect textRect = GUILayoutUtility.GetLastRect();
				if( evt.type == EventType.mouseDown && evt.clickCount == 2 )
				{
					if( textRect.Contains( evt.mousePosition ) )
					{
						if( GUI.enabled && value != null )
						{
							Selection.activeObject = value;
							EditorGUIUtility.PingObject( value );
						}
					}
				}
				else if( evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform )
				{
					if( textRect.Contains( evt.mousePosition ) )
					{
						var draggedObject = DragAndDrop.objectReferences.First() as GameObject;
						var draggedFont = draggedObject != null ? draggedObject.GetComponent<dfDynamicFont>() : null;
						DragAndDrop.visualMode = ( draggedFont != null ) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
						if( evt.type == EventType.DragPerform )
						{
							selectionCallback( draggedObject );
						}
						evt.Use();
					}
				}
			}

			if( GUI.enabled && GUILayout.Button( new GUIContent( " ", "Select Font" ), "IN ObjectField", GUILayout.Width( 14 ) ) )
			{
				dfEditorUtil.DelayedInvoke( (System.Action)( () =>
				{
					var dialog = dfPrefabSelectionDialog.Show( "Select Dynamic Font", typeof( dfDynamicFont ), selectionCallback, dfFontDefinitionInspector.DrawFontPreview, null );
					dialog.previewSize = 200;
				} ) );
			}

		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space( 2 );

	}

}
