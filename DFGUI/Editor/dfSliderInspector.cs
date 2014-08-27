/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfSlider ) )]
public class dfSliderInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		var control = target as dfSlider;
		if( control == null )
			return false;

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Slider Properties", true ) )
			return false;

		dfEditorUtil.LabelWidth = 100f;

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			SelectTextureAtlas( "Atlas", control, "Atlas", false, true );
			SelectSprite( "Track", control.Atlas, control, "BackgroundSprite", false );

			var backgroundColor = EditorGUILayout.ColorField( "Back color", control.Color );
			if( backgroundColor != control.Color )
			{
				dfEditorUtil.MarkUndo( control, "Change Background Color" );
				control.Color = backgroundColor;
			}

			var orientation = (dfControlOrientation)EditorGUILayout.EnumPopup( "Orientation", control.Orientation );
			if( orientation != control.Orientation )
			{
				dfEditorUtil.MarkUndo( control, "Change Orientation" );
				control.Orientation = orientation;
			}

		}

		using( dfEditorUtil.BeginGroup( "Behavior" ) )
		{

			var rtl = EditorGUILayout.Toggle( "Right to Left", control.RightToLeft );
			if( rtl != control.RightToLeft )
			{
				dfEditorUtil.MarkUndo( control, "Switch Right To Left" );
				control.RightToLeft = rtl;
			}

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

			var step = EditorGUILayout.FloatField( "Step", control.StepSize );
			if( step != control.StepSize )
			{
				dfEditorUtil.MarkUndo( control, "Change Step" );
				control.StepSize = step;
			}

			var scroll = EditorGUILayout.FloatField( "Scroll Size", control.ScrollSize );
			if( scroll != control.ScrollSize )
			{
				dfEditorUtil.MarkUndo( control, "Change Scroll Increment" );
				control.ScrollSize = scroll;
			}

			var value = EditorGUILayout.Slider( "Value", control.Value, control.MinValue, control.MaxValue );
			if( value != control.Value )
			{
				dfEditorUtil.MarkUndo( control, "Change Slider Value" );
				control.Value = value;
			}

		}

		using( dfEditorUtil.BeginGroup( "Controls" ) )
		{

			var thumb = EditorGUILayout.ObjectField( "Thumb", control.Thumb, typeof( dfControl ), true ) as dfControl;
			if( thumb != control.Thumb )
			{
				if( thumb == null || thumb.transform.IsChildOf( control.transform ) )
				{
					dfEditorUtil.MarkUndo( control, "Assign Thumb Object" );
					control.Thumb = thumb;
				}
				else
				{
					EditorUtility.DisplayDialog( "Invalid Control", "You can only assign controls to this property that are a child of the " + control.name + " control", "OK" );
				}
			}

			if( thumb != null )
			{

				var thumbPadding = dfEditorUtil.EditInt2( "Offset", "X", "Y", control.ThumbOffset );
				if( !RectOffset.Equals( thumbPadding, control.ThumbOffset ) )
				{
					dfEditorUtil.MarkUndo( control, "Change thumb Offset" );
					control.ThumbOffset = thumbPadding;
				}

			}

			var fill = EditorGUILayout.ObjectField( "Progress", control.Progress, typeof( dfControl ), true ) as dfControl;
			if( fill != control.Progress )
			{
				if( fill == null || fill.transform.IsChildOf( control.transform ) )
				{
					dfEditorUtil.MarkUndo( control, "Assign Thumb Object" );
					control.Progress = fill;
				}
				else
				{
					EditorUtility.DisplayDialog( "Invalid Control", "You can only assign controls to this property that are a child of the " + control.name + " control", "OK" );
				}
			}

			if( fill != null )
			{

				if( fill is dfSprite )
				{

					var mode = (dfProgressFillMode)EditorGUILayout.EnumPopup( "Fill Mode", control.FillMode );
					if( mode != control.FillMode )
					{
						dfEditorUtil.MarkUndo( control, "Change Fill Mode" );
						control.FillMode = mode;
					}

				}

				var padding = dfEditorUtil.EditPadding( "Padding", control.FillPadding );
				if( padding != control.FillPadding )
				{
					dfEditorUtil.MarkUndo( control, "Change Slider Padding" );
					control.FillPadding = padding;
				}

			}

		}

		return true;

	}

}
