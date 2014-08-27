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

[CustomEditor( typeof( dfResizeHandle ) )]
public class dfResizeHandleInspector : dfControlInspector
{

	protected override bool OnCustomInspector()
	{

		dfEditorUtil.DrawSeparator();

		var handle = target as dfResizeHandle;

		dfEditorUtil.LabelWidth = 120f;

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			SelectTextureAtlas( "Atlas", handle, "Atlas", false, true );
			if( handle.GUIManager != null && !dfAtlas.Equals( handle.Atlas, handle.GUIManager.DefaultAtlas ) )
			{
				EditorGUILayout.HelpBox( "This control does not use the same Texture Atlas as the View, which will result in an additional draw call.", MessageType.Info );
			}

			SelectSprite( "Background", handle.Atlas, handle, "BackgroundSprite", false );

			var backColor = EditorGUILayout.ColorField( "Back Color", handle.Color );
			if( backColor != handle.Color )
			{
				dfEditorUtil.MarkUndo( handle, "Change Background Color" );
				handle.Color = backColor;
			}

		}

		using( dfEditorUtil.BeginGroup( "Edges" ) )
		{

			var edges = handle.Edges;

			EditorGUI.BeginChangeCheck();
			var left = EditorGUILayout.Toggle( "Left", ( edges & dfResizeHandle.ResizeEdge.Left ) == dfResizeHandle.ResizeEdge.Left );
			if( EditorGUI.EndChangeCheck() )
			{

				if( left )
				{
					edges |= dfResizeHandle.ResizeEdge.Left;
					edges &= ~dfResizeHandle.ResizeEdge.Right;
				}
				else
				{
					edges &= ~dfResizeHandle.ResizeEdge.Left;
				}

			}

			EditorGUI.BeginChangeCheck();
			var right = EditorGUILayout.Toggle( "Right", ( edges & dfResizeHandle.ResizeEdge.Right ) == dfResizeHandle.ResizeEdge.Right );
			if( EditorGUI.EndChangeCheck() )
			{

				if( right )
				{
					edges |= dfResizeHandle.ResizeEdge.Right;
					edges &= ~dfResizeHandle.ResizeEdge.Left;
				}
				else
				{
					edges &= ~dfResizeHandle.ResizeEdge.Right;
				}

			}

			EditorGUI.BeginChangeCheck();
			var top = EditorGUILayout.Toggle( "Top", ( edges & dfResizeHandle.ResizeEdge.Top ) == dfResizeHandle.ResizeEdge.Top );
			if( EditorGUI.EndChangeCheck() )
			{

				if( top )
				{
					edges |= dfResizeHandle.ResizeEdge.Top;
					edges &= ~dfResizeHandle.ResizeEdge.Bottom;
				}
				else
				{
					edges &= ~dfResizeHandle.ResizeEdge.Top;
				}

			}

			EditorGUI.BeginChangeCheck();
			var bottom = EditorGUILayout.Toggle( "Bottom", ( edges & dfResizeHandle.ResizeEdge.Bottom ) == dfResizeHandle.ResizeEdge.Bottom );
			if( EditorGUI.EndChangeCheck() )
			{

				if( bottom )
				{
					edges |= dfResizeHandle.ResizeEdge.Bottom;
					edges &= ~dfResizeHandle.ResizeEdge.Top;
				}
				else
				{
					edges &= ~dfResizeHandle.ResizeEdge.Bottom;
				}

			}

			handle.Edges = edges;

		}

		return base.OnCustomInspector();

	}

}
