// *******************************************************
// Copyright 2013-2014 Daikon Forge, all rights reserved under 
// US Copyright Law and international treaties
// *******************************************************
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfSpriteAnimation ) )]
public class dfSpriteAnimationInspector : Editor
{

	public override void OnInspectorGUI()
	{

		var animation = target as dfSpriteAnimation;

		dfEditorUtil.LabelWidth = 100f;

		using( dfEditorUtil.BeginGroup( "General" ) )
		{

			var name = EditorGUILayout.TextField( "Name", animation.TweenName );
			if( !string.Equals( name, animation.TweenName ) )
			{
				dfEditorUtil.MarkUndo( animation, "Change Tween Name" );
				animation.TweenName = name;
			}

			SelectAnimationClip( "Clip", animation );

		}

		using( dfEditorUtil.BeginGroup( "Target" ) )
		{

			if( animation.Target == null )
				animation.Target = new dfComponentMemberInfo();

			var animTarget = animation.Target;

			var sourceComponent = dfEditorUtil.ComponentField( "Component", animTarget.Component );
			if( sourceComponent != animTarget.Component )
			{
				dfEditorUtil.MarkUndo( animation, "Assign DataSource Component" );
				animTarget.Component = sourceComponent;
			}

			if( sourceComponent == null )
			{
				EditorGUILayout.HelpBox( "Missing component", MessageType.Error );
				return;
			}

			var sourceComponentMembers =
				getMemberList( sourceComponent )
				.Where( x => isValidFieldType( x, typeof( string ) ) )
				.Select( m => m.Name )
				.ToArray();

			var memberIndex = findIndex( sourceComponentMembers, animTarget.MemberName );
			var selectedIndex = EditorGUILayout.Popup( "Property", memberIndex, sourceComponentMembers );
			if( selectedIndex >= 0 && selectedIndex < sourceComponentMembers.Length )
			{
				var memberName = sourceComponentMembers[ selectedIndex ];
				if( memberName != animTarget.MemberName )
				{
					dfEditorUtil.MarkUndo( animation, "Assign DataSource Member" );
					animTarget.MemberName = memberName;
				}
			}

			EditorGUILayout.Separator();

		}

		if( animation.Target == null || string.IsNullOrEmpty( animation.Target.MemberName ) )
		{
			return;
		}

		using( dfEditorUtil.BeginGroup( "Animation" ) )
		{

			EditorGUI.BeginChangeCheck();
			var autoRun = EditorGUILayout.Toggle( "Auto Run", animation.AutoRun );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Change AutoRun property" );
				animation.AutoRun = autoRun;
			}

			EditorGUI.BeginChangeCheck();
			var loop = (dfTweenLoopType)EditorGUILayout.EnumPopup( "Loop", animation.LoopType );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify tween loop" );
				animation.LoopType = loop;
			}

			EditorGUI.BeginChangeCheck();
			var length = EditorGUILayout.FloatField( "Length", animation.Length );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify tween time" );
				animation.Length = length;
			}

			var direction = (dfPlayDirection)EditorGUILayout.EnumPopup( "Direction", animation.Direction );
			if( direction != animation.Direction )
			{
				dfEditorUtil.MarkUndo( target, "Change play direction" );
				animation.Direction = direction;
			}

		}

		// Show "Play" button when application is playing
		showDebugPlayButton( animation );

	}

	private static void showDebugPlayButton( dfTweenPlayableBase tween )
	{

		if( !Application.isPlaying )
			return;

		using( dfEditorUtil.BeginGroup( "Debug" ) )
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

		EditorGUILayout.Separator();

	}

	private Component getDefaultComponent()
	{

		var gameObject = ( (Component)target ).gameObject;
		var control = gameObject.GetComponent<dfControl>();
		if( control != null )
			return control;

		var defaultComponent = gameObject.GetComponents<MonoBehaviour>().Where( c => c != target ).FirstOrDefault();

		return defaultComponent;

	}

	private int findIndex( string[] list, string value )
	{

		for( int i = 0; i < list.Length; i++ )
		{
			if( list[ i ] == value )
				return i;
		}

		return -1;

	}

	private MemberInfo[] getMemberList( Component component )
	{

		var baseMembers = component
			.GetType()
			.GetMembers( BindingFlags.Public | BindingFlags.Instance )
			.Where( m =>
				( m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property ) &&
				!m.IsDefined( typeof( HideInInspector ), true ) &&
				m.DeclaringType != typeof( MonoBehaviour ) &&
				m.DeclaringType != typeof( Behaviour ) &&
				m.DeclaringType != typeof( Component ) &&
				m.DeclaringType != typeof( UnityEngine.Object ) &&
				m.DeclaringType != typeof( System.Object )
			)
			.OrderBy( m => m.Name )
			.ToArray();

		return baseMembers;

	}

	private bool isValidFieldType( MemberInfo member, Type requiredType )
	{

		if( member is FieldInfo )
			return isValidFieldType( ( (FieldInfo)member ).FieldType, requiredType );

		if( member is PropertyInfo )
			return isValidFieldType( ( (PropertyInfo)member ).PropertyType, requiredType );

		return false;

	}

	private bool isValidFieldType( Type type, Type requiredType )
	{

		if( requiredType.Equals( type ) )
			return true;

		if( requiredType.IsAssignableFrom( type ) )
			return true;

		if( typeof( IEnumerable ).IsAssignableFrom( type ) )
		{
			var genericType = type.GetGenericArguments();
			if( genericType.Length == 1 )
				return isValidFieldType( genericType[ 0 ], requiredType );
		}

		if( type != typeof( int ) && type != typeof( double ) && type != typeof( float ) )
		{
			return false;
		}

		if( requiredType != typeof( int ) && requiredType != typeof( double ) && requiredType != typeof( float ) )
		{
			return false;
		}

		return true;

	}

	protected internal static void SelectAnimationClip( string label, dfSpriteAnimation animation )
	{

		var savedColor = GUI.color;
		var showDialog = false;

		try
		{

			var clip = animation.Clip;
			if( clip == null )
				GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.red;

			dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
			{
				var newClip = ( item == null ) ? null : item.GetComponent<dfAnimationClip>();
				dfEditorUtil.MarkUndo( animation, "Change Atlas" );
				animation.Clip = newClip;
			};

			var value = clip;

			EditorGUILayout.BeginHorizontal();
			{

				EditorGUILayout.LabelField( label, "", GUILayout.Width( dfEditorUtil.LabelWidth - 6 ) );

				GUILayout.Space( 2 );

				var displayText = value == null ? "[none]" : value.name;
				GUILayout.Label( displayText, "TextField" );

				var evt = Event.current;
				if( evt != null )
				{
					Rect textRect = GUILayoutUtility.GetLastRect();
					if( evt.type == EventType.mouseDown && evt.clickCount == 2 )
					{
						if( textRect.Contains( evt.mousePosition ) )
						{
							if( GUI.enabled && value != null )
							{
								Selection.activeObject = value;
								EditorGUIUtility.PingObject( value );
							}
						}
					}
					else if( evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform )
					{
						if( textRect.Contains( evt.mousePosition ) )
						{
							var draggedObject = DragAndDrop.objectReferences.First() as GameObject;
							var draggedFont = draggedObject != null ? draggedObject.GetComponent<dfAtlas>() : null;
							DragAndDrop.visualMode = ( draggedFont != null ) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
							if( evt.type == EventType.DragPerform )
							{
								selectionCallback( draggedObject );
							}
							evt.Use();
						}
					}
				}

				if( GUI.enabled && GUILayout.Button( new GUIContent( " ", "Edit Clip" ), "IN ObjectField", GUILayout.Width( 14 ) ) )
				{
					showDialog = true;
				}

			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space( 2 );

			if( showDialog )
			{
				var dialog = dfPrefabSelectionDialog.Show( "Select Animation Clip", typeof( dfAnimationClip ), selectionCallback, dfAnimationClipInspector.RenderPreview, null );
				dialog.previewSize = 200;
			}

		}
		finally
		{
			GUI.color = savedColor;
		}

	}

}
