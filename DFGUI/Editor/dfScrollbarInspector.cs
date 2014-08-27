/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfScrollbar ) )]
public class dfScrollbarInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		var control = target as dfScrollbar;
		if( control == null )
			return false;

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Scrollbar Properties", true ) )
			return false;

		dfEditorUtil.LabelWidth = 100f;

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			SelectTextureAtlas( "Atlas", control, "Atlas", false, true );

			var orientation = (dfControlOrientation)EditorGUILayout.EnumPopup( "Orientation", control.Orientation );
			if( orientation != control.Orientation )
			{
				dfEditorUtil.MarkUndo( control, "Change Orientation" );
				control.Orientation = orientation;
			}

		}

		using( dfEditorUtil.BeginGroup( "Behavior" ) )
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

			var step = EditorGUILayout.FloatField( "Snap", control.StepSize );
			if( step != control.StepSize )
			{
				dfEditorUtil.MarkUndo( control, "Change Snap" );
				control.StepSize = step;
			}

			var increment = EditorGUILayout.FloatField( "Increment", control.IncrementAmount );
			if( increment != control.IncrementAmount )
			{
				dfEditorUtil.MarkUndo( control, "Change Increment Amount" );
				control.IncrementAmount = increment;
			}

			var scroll = EditorGUILayout.FloatField( "Scroll Size", control.ScrollSize );
			if( scroll != control.ScrollSize )
			{
				dfEditorUtil.MarkUndo( control, "Change Scroll Increment" );
				control.ScrollSize = scroll;
			}

			var value = EditorGUILayout.Slider( "Value", control.Value, control.MinValue, control.MaxValue - control.ScrollSize );
			if( value != control.Value )
			{
				dfEditorUtil.MarkUndo( control, "Change Value" );
				control.Value = value;
			}

			var autoHide = EditorGUILayout.Toggle( "Auto Hide", control.AutoHide );
			if( autoHide != control.AutoHide )
			{
				dfEditorUtil.MarkUndo( control, "Change AutoHide property" );
				control.AutoHide = autoHide;
			}

		}

		using( dfEditorUtil.BeginGroup( "Controls" ) )
		{

			var track = EditorGUILayout.ObjectField( "Track", control.Track, typeof( dfControl ), true ) as dfControl;
			if( track != control.Track )
			{
				if( track == null || track.transform.IsChildOf( control.transform ) )
				{
					dfEditorUtil.MarkUndo( control, "Assign Track" );
					control.Track = track;
				}
				else
				{
					EditorUtility.DisplayDialog( "Invalid Control", "You can only assign controls to this property that are a child of the " + control.name + " control", "OK" );
				}
			}

			var incButton = EditorGUILayout.ObjectField( "Inc. Button", control.IncButton, typeof( dfControl ), true ) as dfControl;
			if( incButton != control.IncButton )
			{
				if( incButton == null || incButton.transform.IsChildOf( control.transform ) )
				{
					dfEditorUtil.MarkUndo( control, "Assign Increment Button" );
					control.IncButton = incButton;
				}
				else
				{
					EditorUtility.DisplayDialog( "Invalid Control", "You can only assign controls to this property that are a child of the " + control.name + " control", "OK" );
				}
			}

			var decButton = EditorGUILayout.ObjectField( "Dec. Button", control.DecButton, typeof( dfControl ), true ) as dfControl;
			if( decButton != control.DecButton )
			{
				if( decButton == null || decButton.transform.IsChildOf( control.transform ) )
				{
					dfEditorUtil.MarkUndo( control, "Assign Decrement Button" );
					control.DecButton = decButton;
				}
				else
				{
					EditorUtility.DisplayDialog( "Invalid Control", "You can only assign controls to this property that are a child of the " + control.name + " control", "OK" );
				}
			}

			var thumb = EditorGUILayout.ObjectField( "Thumb", control.Thumb, typeof( dfControl ), true ) as dfControl;
			if( thumb != control.Thumb )
			{
				if( thumb == null || thumb.transform.IsChildOf( control.transform ) )
				{
					dfEditorUtil.MarkUndo( control, "Assign Thumb" );
					control.Thumb = thumb;
				}
				else
				{
					EditorUtility.DisplayDialog( "Invalid Control", "You can only assign controls to this property that are a child of the " + control.name + " control", "OK" );
				}
			}

			if( thumb != null )
			{

				var minThumb = dfEditorUtil.EditInt2( "Min. Size", "Width", "Height", thumb.MinimumSize );
				if( minThumb != thumb.MinimumSize )
				{
					dfEditorUtil.MarkUndo( thumb, "Change Minimum Size" );
					thumb.MinimumSize = minThumb;
				}

				var thumbPadding = dfEditorUtil.EditPadding( "Padding", control.ThumbPadding );
				if( !RectOffset.Equals( thumbPadding, control.ThumbPadding ) )
				{
					dfEditorUtil.MarkUndo( control, "Change thumb Padding" );
					control.ThumbPadding = thumbPadding;
				}

			}

		}

		return true;

	}

}
