/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CustomEditor( typeof( dfControlKeyBinding ) )]
public class KeyBindingEditor : Editor
{

	public override void OnInspectorGUI()
	{

		var binder = target as dfControlKeyBinding;

		dfEditorUtil.LabelWidth = 100f;

		using( dfEditorUtil.BeginGroup( "Event Source" ) )
		{

			var defaultControl = binder.gameObject.GetComponent<dfControl>();

			var control = dfEditorUtil.ComponentField( "Control", binder.Control ?? defaultControl, typeof( dfControl ) ) as dfControl;
			if( control != binder.Control )
			{
				dfEditorUtil.MarkUndo( binder, "Assign Control to key binding" );
				binder.Control = control;
			}

		}

		using( dfEditorUtil.BeginGroup( "Key" ) )
		{

			EditorGUI.BeginChangeCheck();

			#region Experiment to try out "press the key combo you want" functionality 
			//GUILayout.BeginHorizontal();
			//{

			//    EditorGUILayout.LabelField( "Assigned Key", "", GUILayout.Width( dfEditorUtil.LabelWidth - 10 ) );

			//    GUILayout.Space( 5 );

			//    GUILayout.Box( new GUIContent( key.ToString(), "Press a key to assign" ), "TextField", GUILayout.ExpandWidth( true ) );

			//    var textRect = GUILayoutUtility.GetLastRect();
			//    var textID = GUIUtility.GetControlID( FocusType.Keyboard, textRect );

			//    if( Event.current != null )
			//    {
			//        var type = Event.current.type;
			//        if( type == EventType.keyDown && GUIUtility.hotControl == textID )
			//        {
			//            var pressed = Event.current.keyCode;
			//            if( pressed != KeyCode.None && pressed != key )
			//            {
			//                key = pressed;
			//                this.Repaint();
			//            }
			//            Event.current.Use();
			//        }
			//        else if( type == EventType.mouseDown && Event.current.button == 0 && textRect.Contains( Event.current.mousePosition ) )
			//        {
			//            GUIUtility.hotControl = textID;
			//        }
			//    }

			//}
			//GUILayout.EndHorizontal();
			#endregion

			var keyCode = (KeyCode)EditorGUILayout.EnumPopup( "Key", binder.KeyCode );
			var control = EditorGUILayout.Toggle( "Control", binder.ControlPressed );
			var alt = EditorGUILayout.Toggle( "Alt", binder.AltPressed );
			var shift = EditorGUILayout.Toggle( "Shift", binder.ShiftPressed );

			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( binder, "Modify KeyPress" );
				binder.KeyCode = keyCode;
				binder.ControlPressed = control;
				binder.AltPressed = alt;
				binder.ShiftPressed = shift;
			}

		}

		using( dfEditorUtil.BeginGroup( "Event Handler" ) )
		{

			var dataTarget = binder.Target;
			if( dataTarget == null )
			{
				dataTarget = binder.Target = new dfComponentMemberInfo()
				{
					Component =
						binder.gameObject
						.GetComponents( typeof( Component ) )
						.Where( c => c != binder )
						.FirstOrDefault()
				};
			}

			var targetComponent = dfEditorUtil.ComponentField( "Target", dataTarget.Component );
			if( targetComponent != dataTarget.Component )
			{
				dfEditorUtil.MarkUndo( binder, "Assign event source" );
				dataTarget.Component = targetComponent;
			}

			if( targetComponent == null )
			{
				EditorGUILayout.HelpBox( "Missing component", MessageType.Error );
				return;
			}

			var targetComponentMembers =
				getEventHandlers( targetComponent.GetType() )
				.OrderBy( m => m.Name )
				.Select( m => m.Name )
				.ToArray();

			if( targetComponentMembers.Length == 0 )
			{
				EditorGUILayout.HelpBox( "Class " + targetComponent.GetType().Name + " does not define any compatible event handlers", MessageType.Error );
			}
			else
			{

				var memberIndex = findIndex( targetComponentMembers, dataTarget.MemberName );
				var selectedIndex = EditorGUILayout.Popup( "Method", memberIndex, targetComponentMembers );
				if( selectedIndex >= 0 && selectedIndex < targetComponentMembers.Length )
				{
					var memberName = targetComponentMembers[ selectedIndex ];
					if( memberName != dataTarget.MemberName )
					{
						dfEditorUtil.MarkUndo( binder, "Assign event name" );
						dataTarget.MemberName = memberName;
					}
				}

			}

		}

	}

	private MethodInfo[] getEventHandlers( Type componentType )
	{

		var methods = componentType
			.GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )
			.Where( m =>
				!m.IsSpecialName &&
				!m.IsGenericMethod &&
				!m.IsAbstract &&
				!m.IsConstructor &&
				!m.IsDefined( typeof( HideInInspector ), true ) &&
				m.ReturnType == typeof( void ) &&
				m.DeclaringType != typeof( MonoBehaviour ) &&
				m.DeclaringType != typeof( Behaviour ) &&
				m.DeclaringType != typeof( Component ) &&
				m.DeclaringType != typeof( UnityEngine.Object ) &&
				m.DeclaringType != typeof( System.Object ) &&
				isKeyHandlerSignature( m.GetParameters() )
			)
			.ToArray();

		return methods;

	}

	private bool isKeyHandlerSignature( ParameterInfo[] parameterInfo )
	{

		if( parameterInfo.Length == 0 )
			return true;

		if( parameterInfo.Length == 2 )
		{
			
			if( parameterInfo[ 0 ].ParameterType != typeof( dfControl ) )
				return false;

			if( parameterInfo[ 1 ].ParameterType != typeof( dfKeyEventArgs ) )
				return false;

			return true;

		}

		return false;

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

}
