/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CustomEditor( typeof( dfDataObjectProxy ) )]
public class dfDataObjectProxyInspector : Editor
{

	public override void OnInspectorGUI()
	{

		try
		{

			var proxy = target as dfDataObjectProxy;

			var assignedScript = getMatchingScript( proxy.TypeName );
			MonoScript selectedScript = assignedScript;

			try
			{
				selectedScript = EditorGUILayout.ObjectField( "Data Type", assignedScript, typeof( MonoScript ), false ) as MonoScript;
			}
			catch( ExitGUIException ) 
			{
				return;
			}

			if( selectedScript != assignedScript )
			{

				dfEditorUtil.MarkUndo( proxy, "Change Proxy Data Type" );

				if( selectedScript != null )
				{
					var selectedClass = selectedScript.GetClass();
					proxy.TypeName = selectedClass != null ? selectedClass.Name : "";
				}
				else
				{
					proxy.TypeName = "";
				}

			}

			if( Application.isPlaying || string.IsNullOrEmpty( proxy.TypeName ) || proxy.Data == null )
				return;

			var serialized = new SerializedObject( target );
			var property = serialized.FindProperty( "data" );
			if( property == null )
				return;

			using( dfEditorUtil.BeginGroup( "Data" ) )
			{
				EditorGUILayout.PropertyField( property, true );
			}

		}
		catch( Exception err )
		{
			Debug.LogError( "Failed to inspect Data Object Proxy: " + err.ToString(), target );
		}

	}

	private MonoScript getMatchingScript( string targetType )
	{

		if( string.IsNullOrEmpty( targetType ) )
			return null;

		MonoScript[] scripts = (MonoScript[])Resources.FindObjectsOfTypeAll( typeof( MonoScript ) );
		for( int i = 0; i < scripts.Length; i++ )
		{

			// Workaround for a Unity bug - Shaders are also included in the list
			// of all MonoScript instances, and attempting to call MonoScript.GetClass()
			// on the VertexLit shader crashes Unity. We don't care about anything 
			// that is not a Monoscript, so eliminate all subclasses
			if( scripts[ i ].GetType() != typeof( MonoScript ) )
				continue;

			var scriptClass = scripts[ i ].GetClass();
			if( scriptClass == null )
				continue;

			if( scriptClass.Name == targetType )
			{
				return scripts[ i ];
			}

		}

		return null;

	}

}
