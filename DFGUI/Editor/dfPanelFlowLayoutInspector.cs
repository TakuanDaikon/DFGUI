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

[CustomEditor( typeof( dfPanelFlowLayout ) )]
public class dfPanelFlowLayoutInspector : Editor
{

	public override void OnInspectorGUI()
	{

		var control = target as dfPanelFlowLayout;

		dfEditorUtil.LabelWidth = 110f;

		using( dfEditorUtil.BeginGroup( "General" ) )
		{

			var hideClipped = EditorGUILayout.Toggle( "Hide Clipped", control.HideClippedControls );
			if( hideClipped != control.HideClippedControls )
			{
				dfEditorUtil.MarkUndo( control, "Toggle 'Hide Clipped'" );
				control.HideClippedControls = hideClipped;
			}

			var padding = dfEditorUtil.EditPadding( "Border Padding", control.BorderPadding );
			if( !RectOffset.Equals( padding, control.BorderPadding ) )
			{
				dfEditorUtil.MarkUndo( control, "Change Border Padding" );
				control.BorderPadding = padding;
			}

		}

		using( dfEditorUtil.BeginGroup( "Layout" ) )
		{

			var flowDirection = (dfControlOrientation)EditorGUILayout.EnumPopup( "Flow Direction", control.Direction );
			if( flowDirection != control.Direction )
			{
				dfEditorUtil.MarkUndo( control, "Change Flow Direction Property" );
				control.Direction = flowDirection;
			}

			var itemPadding = dfEditorUtil.EditInt2( "Item Spacing", "Horz", "Vert", control.ItemSpacing );
			if( !Vector2.Equals( itemPadding, control.ItemSpacing ) )
			{
				dfEditorUtil.MarkUndo( control, "Change Layout Spacing" );
				control.ItemSpacing = itemPadding;
			}

			var sizeLabel = control.Direction == dfControlOrientation.Horizontal ? "Max Width" : "Max Height";
			var maxLayoutSize = EditorGUILayout.IntField( sizeLabel, control.MaxLayoutSize );
			if( maxLayoutSize != control.MaxLayoutSize )
			{
				dfEditorUtil.MarkUndo( control, "Change " + sizeLabel );
				control.MaxLayoutSize = maxLayoutSize;
			}

			if( GUILayout.Button( "Force Update" ) )
			{
				control.PerformLayout();
			}

		}

		using( dfEditorUtil.BeginGroup( "Excluded controls" ) )
		{
			editExcludedControls( control );
		}

	}

	private void editExcludedControls( dfPanelFlowLayout control )
	{

		EditorGUILayout.Separator();

		var collectionModified = false;

		var excluded = control.ExcludedControls;
		for( int i = 0; i < excluded.Count && !collectionModified; i++ )
		{

			EditorGUILayout.BeginHorizontal();
			{

				EditorGUI.BeginChangeCheck();
				excluded[ i ] = EditorGUILayout.ObjectField( excluded[ i ], typeof( dfControl ), true ) as dfControl;
				if( EditorGUI.EndChangeCheck() )
				{
					EditorUtility.SetDirty( control );
					control.PerformLayout();
				}

				if( GUILayout.Button( "x", "minibutton", GUILayout.Width( 20 ) ) )
				{
					dfEditorUtil.MarkUndo( control, "Remove excluded control" );
					excluded.RemoveAt( i );
					collectionModified = true;
					control.PerformLayout();
				}

			}
			EditorGUILayout.EndHorizontal();

		}

		EditorGUILayout.Separator();

		GUILayout.BeginHorizontal();
		{

			GUILayout.FlexibleSpace();

			if( GUILayout.Button( "Add To List", GUILayout.Width( 150 ) ) )
			{
				excluded.Add( null );
			}

			GUILayout.FlexibleSpace();

		}
		GUILayout.EndHorizontal();

	}

}
