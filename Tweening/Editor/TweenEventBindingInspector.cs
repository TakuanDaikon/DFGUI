/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CustomEditor( typeof( dfTweenEventBinding ) )]
public class TweenEventBindingInspector : Editor
{

	public override void OnInspectorGUI()
	{

		var binder = target as dfTweenEventBinding;

		using( dfEditorUtil.BeginGroup( "Tween" ) )
		{

			if( binder.Tween == null )
			{
				binder.Tween = binder.gameObject.GetComponent( typeof( dfTweenComponentBase ) ) as Component;
			}

			var tween = dfEditorUtil.ComponentField( "Tween", binder.Tween, typeof( dfTweenPlayableBase ) );
			if( tween != binder.Tween )
			{
				dfEditorUtil.MarkUndo( binder, "Change tween" );
				binder.Tween = tween;
			}

			if( tween == null )
				return;

		}

		using( dfEditorUtil.BeginGroup( "Events" ) )
		{

			if( binder.EventSource == null )
			{
				binder.EventSource = (Component)binder.gameObject.GetComponent<dfControl>();
			}

			var source = dfEditorUtil.ComponentField( "Component", binder.EventSource );
			if( source != binder.EventSource )
			{
				dfEditorUtil.MarkUndo( binder, "Change event source" );
				binder.EventSource = source;
			}

			if( source == null )
				return;

			var startEvent = editEvent( source, "Start Event", binder.StartEvent );
			if( startEvent != binder.StartEvent )
			{
				dfEditorUtil.MarkUndo( binder, "Set Start Event" );
				binder.StartEvent = startEvent;
			}

			var stopEvent = editEvent( source, "Stop Event", binder.StopEvent );
			if( stopEvent != binder.StopEvent )
			{
				dfEditorUtil.MarkUndo( binder, "Set Stop Event" );
				binder.StopEvent = stopEvent;
			}

			var resetEvent = editEvent( source, "Reset Event", binder.ResetEvent );
			if( resetEvent != binder.ResetEvent )
			{
				dfEditorUtil.MarkUndo( binder, "Set Reset Event" );
				binder.ResetEvent = resetEvent;
			}

		}

	}

	private string editEvent( Component eventSource, string label, string value )
	{

		var sourceComponentMembers =
			new string[] { " " }
			.Concat(
				getEventList( eventSource )
				.Select( m => m.Name )
			)
			.ToArray();

		var memberIndex = findIndex( sourceComponentMembers, value );
		var selectedIndex = EditorGUILayout.Popup( label, memberIndex, sourceComponentMembers );
		if( selectedIndex >= 0 && selectedIndex < sourceComponentMembers.Length )
		{
			return sourceComponentMembers[ selectedIndex ].Trim();
		}

		return "";

	}

	private int findIndex( string[] list, string value )
	{

		for( int i = 0; i < list.Length; i++ )
		{
			if( list[ i ] == value )
				return i;
		}

		return 0;

	}

	private FieldInfo[] getEventList( Component component )
	{

		var list =
			component.GetType()
			.GetAllFields()
			.Where( p => typeof( Delegate ).IsAssignableFrom( p.FieldType ) )
			.OrderBy( p => p.Name )
			.ToArray();

		return list;

	}

}
