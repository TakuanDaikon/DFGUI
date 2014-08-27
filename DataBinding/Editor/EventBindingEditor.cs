/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CustomEditor( typeof( dfEventBinding ) )]
public class EventBindingEditor : Editor
{

	public override void OnInspectorGUI()
	{

		var binder = target as dfEventBinding;

		dfEditorUtil.LabelWidth = 100f;

		using( dfEditorUtil.BeginGroup( "Data Source" ) )
		{

			if( binder.DataSource == null )
			{

				binder.DataSource  = new dfComponentMemberInfo()
				{
					Component = getDefaultComponent( binder.gameObject )
				};

			}

			var dataSource = binder.DataSource;
			if( dataSource.Component == null )
			{
				dataSource.Component = getDefaultComponent( binder.gameObject );
			}

			var sourceComponent = dfEditorUtil.ComponentField( "Component", dataSource.Component );
			if( sourceComponent != dataSource.Component )
			{
				dfEditorUtil.MarkUndo( binder, "Assign DataSource Component" );
				dataSource.Component = sourceComponent;
			}

			if( sourceComponent == null )
			{
				EditorGUILayout.HelpBox( "Missing component", MessageType.Error );
				return;
			}

			var sourceComponentMembers = 
				getEventList( sourceComponent )
				.Select( m => m.Name )
				.ToArray();

			var memberIndex = findIndex( sourceComponentMembers, dataSource.MemberName );
			
			// If there is no event aready selected, attempt to select the Click event by default
			if( memberIndex == -1 )
			{
				memberIndex = Mathf.Max( 0, findIndex( sourceComponentMembers, "Click" ) );
			}

			var selectedIndex = EditorGUILayout.Popup( "Event", memberIndex, sourceComponentMembers );
			if( selectedIndex >= 0 && selectedIndex < sourceComponentMembers.Length )
			{

				var memberName = sourceComponentMembers[ selectedIndex ];
				if( memberName != dataSource.MemberName )
				{
					dfEditorUtil.MarkUndo( binder, "Assign DataSource Member" );
					dataSource.MemberName = memberName;
				}

				showSignatureButton( sourceComponent.GetType(), memberName );

			}

			EditorGUILayout.Separator();

		}

		if( !binder.DataSource.IsValid )
		{
			EditorGUILayout.HelpBox( "Data source configuration is invalid", MessageType.Error );
			return;
		}

		// Do not proceed if the source binding is invalid
		var handlerType = binder.DataSource.GetMemberType();
		if( handlerType == null )
		{
			binder.DataTarget = new dfComponentMemberInfo();
			return;
		}

		using( dfEditorUtil.BeginGroup( "Event Handler" ) )
		{

			var dataTarget = binder.DataTarget;
			if( dataTarget == null )
			{
				dataTarget = binder.DataTarget = new dfComponentMemberInfo()
				{
					Component = getDefaultComponent( binder.gameObject )
				};
			}

			if( dataTarget.Component == null )
			{
				dataTarget.Component = getDefaultComponent( binder.gameObject );
			}

			var targetComponent = dfEditorUtil.ComponentField( "Component", dataTarget.Component );
			if( targetComponent != dataTarget.Component )
			{
				dfEditorUtil.MarkUndo( binder, "Assign DataSource Component" );
				dataTarget.Component = targetComponent;
			}

			if( targetComponent == null )
			{
				EditorGUILayout.HelpBox( "Missing component", MessageType.Error );
				return;
			}

			var targetComponentMembers = 
				getEventHandlers( targetComponent.GetType(), handlerType )
				.Select( m => m.Name )
				.ToArray();

			if( targetComponentMembers.Length == 0 )
			{
				EditorGUILayout.HelpBox( "Class " + targetComponent.GetType().Name + " does not define any compatible event handlers", MessageType.Error );
			}
			else
			{

				var memberIndex = Mathf.Max( 0, findIndex( targetComponentMembers, dataTarget.MemberName ) );
				var selectedIndex = EditorGUILayout.Popup( "Event Handler", memberIndex, targetComponentMembers );
				if( selectedIndex >= 0 && selectedIndex < targetComponentMembers.Length )
				{
					var memberName = targetComponentMembers[ selectedIndex ];
					if( memberName != dataTarget.MemberName )
					{
						dfEditorUtil.MarkUndo( binder, "Assign DataSource Member" );
						dataTarget.MemberName = memberName;
					}
				}

			}

		}

		using( dfEditorUtil.BeginGroup( "Automatic Binding" ) )
		{

			var autoBind = EditorGUILayout.Toggle( "Auto Bind", binder.AutoBind );
			if( autoBind != binder.AutoBind )
			{
				dfEditorUtil.MarkUndo( binder, "Toggle AutoBind property" );
				binder.AutoBind = autoBind;
			}

			var autoUnbind = EditorGUILayout.Toggle( "Auto Unbind", binder.AutoUnbind );
			if( autoUnbind != binder.AutoUnbind )
			{
				dfEditorUtil.MarkUndo( binder, "Toggle AutoUnbind property" );
				binder.AutoUnbind = autoUnbind;
			}

		}

		if( Application.isPlaying )
		{

			using( dfEditorUtil.BeginGroup( "Debugging" ) )
			{

				EditorGUILayout.BeginHorizontal();
				{

					GUI.enabled = !binder.IsBound;
					if( GUILayout.Button( "Bind" ) )
					{
						binder.Bind();
					}

					GUILayout.Space( 10 );

					GUI.enabled = binder.IsBound;
					if( GUILayout.Button( "Unbind" ) )
					{
						binder.Unbind();
					}

					GUI.enabled = true;

				}
				EditorGUILayout.EndHorizontal();

			}

		}

	}

	private Component getDefaultComponent( GameObject gameObject )
	{
		
		var control = gameObject.GetComponent<dfControl>();
		if( control != null )
			return control;

		var defaultComponent = gameObject.GetComponents<MonoBehaviour>().Where( c => c != target ).FirstOrDefault();

		return defaultComponent;

	}

	private MethodInfo[] getEventHandlers( Type componentType, Type eventHandlerType )
	{

		var invoke = eventHandlerType.GetMethod( "Invoke" );
		if( invoke == null )
		{
			Debug.LogError( "Could not retrieve event signature for " + eventHandlerType.Name );
			return new MethodInfo[ 0 ];
		}

		var delegateParams = invoke.GetParameters();

		var methods = componentType
			.GetMethods( BindingFlags.Public | BindingFlags.Instance )
			.Where( m => 
				!m.IsSpecialName &&
				!m.IsGenericMethod && 
				!m.IsAbstract &&
				!m.IsConstructor && 
				!m.IsDefined( typeof( HideInInspector ), true ) &&
				( m.ReturnType == typeof( void ) || typeof( IEnumerator ).IsAssignableFrom( m.ReturnType ) ) &&
				m.DeclaringType != typeof( MonoBehaviour ) &&
				m.DeclaringType != typeof( Behaviour ) &&
				m.DeclaringType != typeof( Component ) &&
				m.DeclaringType != typeof( UnityEngine.Object ) &&
				m.DeclaringType != typeof( System.Object ) &&
				signatureIsCompatible( delegateParams, m.GetParameters() ) 
			)
			.OrderBy( m => m.Name )
			.ToArray();

		return methods;

	}

	private void showSignatureButton( Type type, string memberName )
	{

		EditorGUILayout.BeginHorizontal();

		GUILayout.Space( 105 );

		if( GUILayout.Button( "Copy Event Signature", "minibutton" ) )
		{

			var member = type.GetAllFields().Where( x => x.Name == memberName ).FirstOrDefault();
			if( member == null )
			{
				Debug.LogError( string.Format( "Member not found: {0}.{1}", type.Name, memberName ) );
				return;
			}

			var invoke = member.FieldType.GetMethod( "Invoke" );
			if( invoke == null )
			{
				Debug.LogError( "Could not retrieve event signature for " + type.Name + "." + memberName );
				return;
			}

			var buffer = new StringBuilder();

			buffer.Append( "public " );

			if( invoke.ReturnType == typeof( void ) )
				buffer.Append( "void" );
			else
				buffer.Append( invoke.ReturnType.Name );

			buffer.Append( " On" + memberName );
			buffer.Append( "( " );

			var paramList = invoke.GetParameters();
			for( int i = 0; i < paramList.Length; i++ )
			{
				if( i > 0 ) buffer.Append( ", " );
				var param = paramList[i];
				buffer.Append( param.ParameterType );
				buffer.Append( " " );
				buffer.Append( param.Name );
			}

			buffer.Append( " )\r\n{\r\n\t// Add event handler code here\r\n}\r\n" );

			dfClipboardHelper.clipBoard = buffer.ToString();
			Debug.Log( buffer.ToString() );

		}

		EditorGUILayout.EndHorizontal();

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

	private int findIndex( string[] list, string value )
	{

		for( int i = 0; i < list.Length; i++ )
		{
			if( list[ i ] == value )
				return i;
		}

		return -1;

	}

	/// <summary>
	/// Determines whether the dfEventBinding component class defines a 
	/// matching "proxy" method to forward event notifications
	/// </summary>
	/// <param name="lhs"></param>
	/// <returns></returns>
	private bool compatibleProxyMethodFound( ParameterInfo[] lhs )
	{

		var proxyMethod = typeof( dfEventBinding )
			.GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )
			.Where( m =>
				m.IsDefined( typeof( dfEventProxyAttribute ), true ) &&
				signatureIsCompatible( lhs, m.GetParameters(), false )
			)
			.FirstOrDefault();

		return proxyMethod != null;

	}

	private bool signatureIsCompatible( ParameterInfo[] lhs, ParameterInfo[] rhs )
	{
		return signatureIsCompatible( lhs, rhs, true );
	}

	private bool signatureIsCompatible( ParameterInfo[] lhs, ParameterInfo[] rhs, bool allowProxy )
	{

		if( lhs == null || rhs == null )
			return false;

		// HACK: Allow for "notification handlers" - Event handlers that don't care
		// about event parameters, they just need to be invoked when the event fires
		if( lhs.Length > 0 && rhs.Length == 0 )
		{
			if( allowProxy )
				return compatibleProxyMethodFound( lhs );
			else
				return false;
		}

		if( lhs.Length != rhs.Length )
			return false;

		for( int i = 0; i < lhs.Length; i++ )
		{
			if( !areTypesCompatible( lhs[i], rhs[i] ) )
				return false;
		}

		return true;

	}

	private bool areTypesCompatible( ParameterInfo lhs, ParameterInfo rhs )
	{

		if( lhs.ParameterType.Equals( rhs.ParameterType ) )
			return true;

		if( lhs.ParameterType.IsAssignableFrom( rhs.ParameterType ) )
			return true;

		return false;

	}

}
