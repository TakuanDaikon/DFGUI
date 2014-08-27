/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfProgressBar ) )]
public class dfProgressbarInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		var control = target as dfProgressBar;
		if( control == null )
			return false;

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Progressbar Properties", true ) )
			return false;

		dfEditorUtil.LabelWidth = 120f;

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			SelectTextureAtlas( "Atlas", control, "Atlas", false, true );
			if( control.GUIManager != null && !dfAtlas.Equals( control.Atlas, control.GUIManager.DefaultAtlas ) )
			{
				EditorGUILayout.HelpBox( "This control does not use the same Texture Atlas as the View, which will result in an additional draw call.", MessageType.Info );
			}

			SelectSprite( "Background", control.Atlas, control, "BackgroundSprite" );
			var backColor = EditorGUILayout.ColorField( "Back Color", control.Color );
			if( backColor != control.Color )
			{
				dfEditorUtil.MarkUndo( control, "Change background color" );
				control.Color = backColor;
			}

			SelectSprite( "Progress", control.Atlas, control, "ProgressSprite" );
			var progressColor = EditorGUILayout.ColorField( "Progress Color", control.ProgressColor );
			if( progressColor != control.ProgressColor )
			{
				dfEditorUtil.MarkUndo( control, "Change background color" );
				control.ProgressColor = progressColor;
			}

			var mode = (dfProgressFillMode)EditorGUILayout.EnumPopup( "Fill Mode", control.FillMode );
			if( mode != control.FillMode )
			{
				dfEditorUtil.MarkUndo( control, "Change Fill Mode" );
				control.FillMode = mode;
			}

			var padding = dfEditorUtil.EditPadding( "Padding", control.Padding );
			if( !RectOffset.Equals( padding, control.Padding ) )
			{
				dfEditorUtil.MarkUndo( control, "Modify Padding" );
				control.Padding = padding;
			}

		}

		using( dfEditorUtil.BeginGroup( "Behavior" ) )
		{

			var actAsSlider = EditorGUILayout.Toggle( "Act as Slider", control.ActAsSlider );
			if( actAsSlider != control.ActAsSlider )
			{
				dfEditorUtil.MarkUndo( control, "Change ActAsSlider property" );
				control.ActAsSlider = actAsSlider;
			}

		}

		using( dfEditorUtil.BeginGroup( "Data" ) )
		{

			var min = EditorGUILayout.FloatField( "Min Value", control.MinValue );
			if( min != control.MinValue )
			{
				dfEditorUtil.MarkUndo( control, "Change Minimum Value" );
				control.MinValue = min;
			}

			var max = EditorGUILayout.FloatField( "Max Value", control.MaxValue );
			if( max != control.MaxValue )
			{
				dfEditorUtil.MarkUndo( control, "Change Maximum Value" );
				control.MaxValue = max;
			}

			var value = EditorGUILayout.Slider( "Value", control.Value, control.MinValue, control.MaxValue );
			if( value != control.Value )
			{
				dfEditorUtil.MarkUndo( control, "Change Slider Value" );
				control.Value = value;
			}

		}

		return true;

	}

}
