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

[CustomEditor( typeof( dfTweenGroup ) )]
public class TweenGroupInspector : Editor
{

	public override void OnInspectorGUI()
	{

		var group = target as dfTweenGroup;

		dfEditorUtil.LabelWidth = 100f;

		using( dfEditorUtil.BeginGroup( "General" ) )
		{

			var name = EditorGUILayout.TextField( "Name", group.TweenName );
			if( name != group.TweenName )
			{
				dfEditorUtil.MarkUndo( group, "Change Tween Group Name" );
				group.TweenName = name;
			}

			var mode = (dfTweenGroup.TweenGroupMode)EditorGUILayout.EnumPopup( "Mode", group.Mode );
			if( mode != group.Mode )
			{
				dfEditorUtil.MarkUndo( group, "Change Tween Group Mode" );
				group.Mode = mode;
			}

			var autoStart = EditorGUILayout.Toggle( "Auto Run", group.AutoStart );
			if( autoStart != group.AutoStart )
			{
				dfEditorUtil.MarkUndo( group, "Change AutoStart" );
				group.AutoStart = autoStart;
			}

			EditorGUI.BeginChangeCheck();
			var delay = EditorGUILayout.FloatField( "Delay", group.StartDelay );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify Tween Delay" );
				group.StartDelay = Mathf.Max( delay, 0 );
			}

		}

		using( dfEditorUtil.BeginGroup( "Tweens" ) )
		{

			var tweens = group.Tweens;

			for( int i = 0; i < tweens.Count; i++ )
			{
				GUILayout.BeginHorizontal();
				{

					var component = dfEditorUtil.ComponentField( "Item " + ( i + 1 ), tweens[ i ], typeof( dfTweenPlayableBase ) ) as dfTweenPlayableBase;
					if( component != tweens[ i ] )
					{
						dfEditorUtil.MarkUndo( group, "Add/Remove Tween" );
						tweens[ i ] = component;
					}

					if( GUILayout.Button( "-", "minibutton", GUILayout.Width( 15 ) ) )
					{
						dfEditorUtil.MarkUndo( group, "Add/Remove Tween" );
						tweens.RemoveAt( i );
						break;
					}

				}
				GUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space( dfEditorUtil.LabelWidth + 5 );
				if( GUILayout.Button( "Add", "minibutton" ) )
				{
					tweens.Add( null );
				}
			}
			EditorGUILayout.EndHorizontal();

		}

		// Show "Play" button when application is playing
		showDebugPlayButton( group );

	}

	private static void showDebugPlayButton( dfTweenPlayableBase tween )
	{

		if( !Application.isPlaying )
			return;

		using( dfEditorUtil.BeginGroup( "Debug" ) )
		{

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space( dfEditorUtil.LabelWidth + 5 );
				if( GUILayout.Button( "Play", "minibutton" ) )
				{
					tween.Play();
				}
				if( GUILayout.Button( "Stop", "minibutton" ) )
				{
					tween.Stop();
				}
				if( GUILayout.Button( "Reset", "minibutton" ) )
				{
					tween.Reset();
				}
			}
			EditorGUILayout.EndHorizontal();

		}

		EditorGUILayout.Separator();

	}

}
