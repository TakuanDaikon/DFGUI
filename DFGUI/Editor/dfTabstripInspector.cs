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

[CustomEditor( typeof( dfTabstrip ) )]
public class dfTabstripInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Tab Strip Properties", true ) )
			return false;

		var control = target as dfTabstrip;
		if( control == null )
			return false;

		dfEditorUtil.LabelWidth = 110f;

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			SelectTextureAtlas( "Atlas", control, "Atlas", false, false );
			if( control.GUIManager != null && !dfAtlas.Equals( control.Atlas, control.GUIManager.DefaultAtlas ) )
			{
				EditorGUILayout.HelpBox( "This control does not use the same Texture Atlas as the View, which will result in an additional draw call.", MessageType.Info );
			}

			SelectSprite( "Background", control.Atlas, control, "BackgroundSprite", false );

			var flowPadding = dfEditorUtil.EditPadding( "Tab Padding", control.LayoutPadding );
			if( !RectOffset.Equals( flowPadding, control.LayoutPadding ) )
			{
				dfEditorUtil.MarkUndo( control, "Change Layout Padding" );
				control.LayoutPadding = flowPadding;
			}

		}

		using( dfEditorUtil.BeginGroup( "Behavior" ) )
		{

			var allowKeyNav = EditorGUILayout.Toggle( "Keyboard Nav.", control.AllowKeyboardNavigation );
			if( allowKeyNav != control.AllowKeyboardNavigation )
			{
				dfEditorUtil.MarkUndo( control, "Toggle 'Allow Keyboard Navigation'" );
				control.AllowKeyboardNavigation = allowKeyNav;
			}

			var tabCount = control.Controls.Count;
			var selectedIndex = EditorGUILayout.IntSlider( "Selected Tab", control.SelectedIndex, 0, tabCount - 1 );
			if( selectedIndex != control.SelectedIndex )
			{
				dfEditorUtil.MarkUndo( control, "Change Selected Tab" );
				control.SelectedIndex = selectedIndex;
			}

			var pageContainer = EditorGUILayout.ObjectField( "Tab Pages", control.TabPages, typeof( dfTabContainer ), true ) as dfTabContainer;
			if( pageContainer != control.TabPages )
			{
				dfEditorUtil.MarkUndo( control, "Set Page Container" );
				control.TabPages = pageContainer;
			}

			if( GUILayout.Button( "Add Tab" ) )
			{
				dfEditorUtil.MarkUndo( control, "Add Tab" );
				Selection.activeGameObject = control.AddTab( "" ).gameObject;
				control.SelectedIndex = int.MaxValue;
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
				MenuText = "Add Tab",
				Handler = ( control ) =>
				{
					dfEditorUtil.MarkUndo( control, "Add Tab" );
					var tabStrip = control as dfTabstrip;
					Selection.activeGameObject = tabStrip.AddTab( "" ).gameObject;
					tabStrip.SelectedIndex = int.MaxValue;
				}
			} );

		}

		base.FillContextMenu( menu );

	}

}
