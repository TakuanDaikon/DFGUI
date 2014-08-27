/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable 0618

[CustomEditor( typeof( dfExpressionPropertyBinding ) )]
public class dfExpressionBindingInspector : Editor
{

	public override void OnInspectorGUI()
	{

#if UNITY_IPHONE
		EditorGUILayout.HelpBox( "Dynamic code generation is not supported on iOS devices", MessageType.Error );
		return;
#else

		var binder = target as dfExpressionPropertyBinding;

		var showDialog = false;

		using( dfEditorUtil.BeginGroup( "Data Source" ) )
		{

			var sourceComponent = dfEditorUtil.ComponentField( "Component", binder.DataSource );
			if( sourceComponent != binder.DataSource )
			{
				dfEditorUtil.MarkUndo( binder, "Assign DataSource Component" );
				binder.DataSource = sourceComponent;
			}

			if( binder.DataSource == null )
			{

				EditorGUILayout.HelpBox( "Please select the Data Source", MessageType.None );

				return;

			}

		}

		using( dfEditorUtil.BeginGroup( "Expression" ) )
		{

			EditorGUILayout.BeginHorizontal();
			{

				var expression = EditorGUILayout.TextField( binder.Expression );
				if( expression != binder.Expression )
				{
					dfEditorUtil.MarkUndo( binder, "Edit data binding expression" );
					binder.Expression = expression;
				}

				if( GUILayout.Button( "", EditorStyles.popup, GUILayout.MaxWidth( 20 ) ) )
				{
					showDialog = true;
				}

			}
			EditorGUILayout.EndHorizontal();

		}

		using( dfEditorUtil.BeginGroup( "Data Target" ) )
		{

			var dataTarget = binder.DataTarget;
			if( dataTarget.Component == null )
			{
				dataTarget.Component = binder.gameObject.GetComponents( typeof( MonoBehaviour ) ).FirstOrDefault();
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
				getMemberList( targetComponent )
				.Select( m => m.Name )
				.ToArray();

			var memberIndex = findIndex( targetComponentMembers, dataTarget.MemberName );
			var selectedIndex = EditorGUILayout.Popup( "Property", memberIndex, targetComponentMembers );
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

		// Moved the dialog display code outside of all grouping code to resolve
		// an InvalidOperationException that happens in some circumstances and 
		// appears to be Mac-specific
		if( showDialog )
		{
			dfExpressionEditorWindow.Show( "Edit Expression", binder.Expression, ( text ) =>
			{
				binder.Expression = text;
			} );
		}

#endif
	}

	#region Private utility methods

	private bool isCompatibleType( MemberInfo member, Type type )
	{

		if( member.IsDefined( typeof( HideInInspector ), true ) )
			return false;

		if( member is FieldInfo )
		{

			var fieldInfo = (FieldInfo)member;

			if( type.IsAssignableFrom( fieldInfo.FieldType ) )
				return true;

			if( isNumericConversion( fieldInfo.FieldType, type ) )
			{
				return true;
			}

		}
		else if( member is PropertyInfo )
		{

			var propertyInfo = (PropertyInfo)member;

			if( type.IsAssignableFrom( propertyInfo.PropertyType ) )
				return true;

			if( isNumericConversion( propertyInfo.PropertyType, type ) )
			{
				return true;
			}

		}

		return false;

	}

	private bool isNumericConversion( Type lhs, Type rhs )
	{

		if( !lhs.IsValueType || !rhs.IsValueType )
			return false;

		var numericTypes = new Type[] 
		{
			typeof( int ), typeof( uint ), typeof( float ), typeof( double )
		};

		return numericTypes.Contains( lhs ) && numericTypes.Contains( rhs );

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

	private MemberInfo[] getMemberList( Component component )
	{

		var baseMembers = component
			.GetType()
			.GetMembers( BindingFlags.Public | BindingFlags.Instance )
			.Where( m =>
				(
					m.MemberType == MemberTypes.Field ||
					m.MemberType == MemberTypes.Property
				) &&
				m.DeclaringType != typeof( MonoBehaviour ) &&
				m.DeclaringType != typeof( Behaviour ) &&
				m.DeclaringType != typeof( Component ) &&
				m.DeclaringType != typeof( UnityEngine.Object )
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

	#endregion

}

#pragma warning restore 0618
