/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfCheckbox ) )]
public class dfCheckboxInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		var control = target as dfCheckbox;
		if( control == null )
			return false;

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Checkbox Properties", true ) )
			return false;

		dfEditorUtil.LabelWidth = 100f;

		var isChecked = EditorGUILayout.Toggle( "Checked", control.IsChecked );
		if( isChecked != control.IsChecked )
		{
			dfEditorUtil.MarkUndo( control, "Change Checkbox Value" );
			control.IsChecked = isChecked;
		}

		EditorGUI.BeginChangeCheck();
		var useSpacebarToClick = EditorGUILayout.Toggle( "Space to Click", control.ClickWhenSpacePressed );
		if( EditorGUI.EndChangeCheck() )
		{
			dfEditorUtil.MarkUndo( control, "Change ClickWhenSpacePressed property" );
			control.ClickWhenSpacePressed = useSpacebarToClick;
		}

		var text = EditorGUILayout.TextField( "Text", control.Text );
		if( text != control.Text )
		{
			dfEditorUtil.MarkUndo( control, "Change Checkbox Text" );
			control.Text = text;
		}

		var icon = EditorGUILayout.ObjectField( "Check Icon", control.CheckIcon, typeof( dfSprite ), true ) as dfControl;
		if( icon != control.CheckIcon )
		{
			dfEditorUtil.MarkUndo( control, "Assign Checkbox Icon" );
			control.CheckIcon = icon;
		}

		var label = EditorGUILayout.ObjectField( "Label Object", control.Label, typeof( dfLabel ), true ) as dfLabel;
		if( label != control.Label )
		{
			dfEditorUtil.MarkUndo( control, "Assign Checkbox Label" );
			control.Label = label;
		}

		var group = EditorGUILayout.ObjectField( "Group", control.GroupContainer, typeof( dfControl ), true ) as dfControl;
		if( group != control.GroupContainer )
		{
			dfEditorUtil.MarkUndo( control, "Assign Checkbox Group" );
			control.GroupContainer = group;
		}

		return true;

	}

}
