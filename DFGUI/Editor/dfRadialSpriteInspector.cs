/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfRadialSprite ) )]
public class dfRadialSpriteInspector : dfSpriteInspector
{

	protected override bool OnCustomInspector()
	{

		if( !base.OnCustomInspector() )
			return false;

		var control = target as dfRadialSprite;
		if( control == null )
			return false;

		using( dfEditorUtil.BeginGroup( "Fill" ) )
		{

			var origin = (dfPivotPoint)EditorGUILayout.EnumPopup( "Fill Origin", control.FillOrigin );
			if( origin != control.FillOrigin )
			{
				dfEditorUtil.MarkUndo( control, "Change Fill Origin" );
				control.FillOrigin = origin;
			}

			var amount = EditorGUILayout.Slider( "Fill Amount", control.FillAmount, 0f, 1f );
			if( !Mathf.Approximately( amount, control.FillAmount ) )
			{
				dfEditorUtil.MarkUndo( control, "Change Fill Amount" );
				control.FillAmount = amount;
			}

			var invert = EditorGUILayout.Toggle( "Invert Fill", control.InvertFill );
			if( invert != control.InvertFill )
			{
				dfEditorUtil.MarkUndo( control, "Change Invert Fill" );
				control.InvertFill = invert;
			}

		}

		return true;

	}

}
