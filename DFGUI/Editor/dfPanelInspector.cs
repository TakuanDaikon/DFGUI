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

[CanEditMultipleObjects]
[CustomEditor( typeof( dfPanel ) )]
public class dfPanelInspector : dfControlInspector
{

	protected override bool OnCustomInspector()
	{

		dfEditorUtil.DrawSeparator();

		var control = target as dfPanel;

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			SelectTextureAtlas( "Atlas", control, "Atlas", false, true );
			if( control.GUIManager != null && !dfAtlas.Equals( control.Atlas, control.GUIManager.DefaultAtlas ) )
			{
				EditorGUILayout.HelpBox( "This control does not use the same Texture Atlas as the View, which will result in an additional draw call.", MessageType.Info );
			}

			SelectSprite( "Background", control.Atlas, control, "BackgroundSprite", false );

			var backgroundColor = EditorGUILayout.ColorField( "Back Color", control.BackgroundColor );
			if( backgroundColor != control.BackgroundColor )
			{
				dfEditorUtil.MarkUndo( control, "Change background color" );
				control.BackgroundColor = backgroundColor;
			}

			var clientPadding = dfEditorUtil.EditPadding( "Padding", control.Padding );
			if( !RectOffset.Equals( clientPadding, control.Padding ) )
			{
				dfEditorUtil.MarkUndo( control, "Change Client Padding" );
				control.Padding = clientPadding;
			}

		}

		return true;

	}

	protected override void FillContextMenu( List<ContextMenuItem> menu )
	{

		if( Selection.gameObjects.Length == 1 )
		{

			menu.Add( new ContextMenuItem()
			{
				MenuText = "Fit to contents",
				Handler = ( control ) =>
				{

					dfEditorUtil.MarkUndo( control, "Fit to contents" );

					var panel = control as dfPanel;
					panel.FitToContents();

				}
			} );

			menu.Add( new ContextMenuItem()
			{
				MenuText = "Center child controls",
				Handler = ( control ) =>
				{
					
					dfEditorUtil.MarkUndo( control, "Center child controls" );
					
					var panel = control as dfPanel;
					panel.CenterChildControls();

				}
			} );

		}

		base.FillContextMenu( menu );

	}

}
