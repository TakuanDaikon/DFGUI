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
[CustomEditor( typeof( dfTabContainer ) )]
public class dfTabPageContainerInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Tab Page Properties", true ) )
			return false;

		var control = target as dfTabContainer;

		dfEditorUtil.LabelWidth = 110f;

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			SelectTextureAtlas( "Atlas", control, "Atlas", false, false );
			if( control.GUIManager != null && !dfAtlas.Equals( control.Atlas, control.GUIManager.DefaultAtlas ) )
			{
				EditorGUILayout.HelpBox( "This control does not use the same Texture Atlas as the View, which will result in an additional draw call.", MessageType.Info );
			}

			SelectSprite( "Background", control.Atlas, control, "BackgroundSprite", false );

		}

		using( dfEditorUtil.BeginGroup( "Tab Pages" ) )
		{

			var tabCount = control.Controls.Count;
			var selectedIndex = EditorGUILayout.IntSlider( "Selected Tab", control.SelectedIndex, 0, tabCount - 1 );
			if( selectedIndex != control.SelectedIndex )
			{
				dfEditorUtil.MarkUndo( control, "Change Selected Tab" );
				control.SelectedIndex = selectedIndex;
			}

			if( GUILayout.Button( "Add Tab Page" ) )
			{
				dfEditorUtil.MarkUndo( control, "Add Tab Page" );
				Selection.activeGameObject = control.AddTabPage().gameObject;
			}

		}

		using( dfEditorUtil.BeginGroup( "Layout" ) )
		{

			var flowPadding = dfEditorUtil.EditPadding( "Padding", control.Padding );
			if( !RectOffset.Equals( flowPadding, control.Padding ) )
			{
				dfEditorUtil.MarkUndo( control, "Change Padding" );
				control.Padding = flowPadding;
			}

		}

		return true;

	}

}
