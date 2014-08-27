/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CustomEditor( typeof( dfPropertyBinding ) )]
public class PropertyBindingEditor : Editor
{

	private bool findDefaultComponent = true;

	public override void OnInspectorGUI()
	{

		if( switchBindingType() )
			return;

		var binder = target as dfPropertyBinding;

		dfEditorUtil.LabelWidth = 100f;

		using( dfEditorUtil.BeginGroup( "Data Source" ) )
		{

			if( binder.DataSource == null )
				binder.DataSource = new dfComponentMemberInfo();

			var dataSource = binder.DataSource;

			if( dataSource != null && dataSource.Component == null && findDefaultComponent )
			{
				findDefaultComponent = false;
				var defaultComponent = binder.GetComponents( typeof( dfControl ) ).FirstOrDefault();
				if( defaultComponent == null )
					defaultComponent = binder.GetComponents( typeof( Component ) ).Where( c => c != binder ).FirstOrDefault();
				if( defaultComponent != null )
				{
					dataSource.Component = defaultComponent;
				}
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
				getMemberList( sourceComponent )
				.Select( m => m.Name )
				.ToArray();

			var memberIndex = findIndex( sourceComponentMembers, dataSource.MemberName );
			var selectedIndex = EditorGUILayout.Popup( "Property", memberIndex, sourceComponentMembers );
			if( selectedIndex >= 0 && selectedIndex < sourceComponentMembers.Length )
			{
				var memberName = sourceComponentMembers[ selectedIndex ];
				if( memberName != dataSource.MemberName )
				{
					dfEditorUtil.MarkUndo( binder, "Assign DataSource Member" );
					dataSource.MemberName = memberName;
				}
			}

			EditorGUILayout.Separator();

		}

		if( !binder.DataSource.IsValid )
		{
			EditorGUILayout.HelpBox( "Data source configuration is invalid", MessageType.Error );
			return;
		}

		var sourcePropertyType = binder.DataSource.GetMemberType();
		if( sourcePropertyType == null )
			return;

		using( dfEditorUtil.BeginGroup( "Data Target" ) )
		{

			if( binder.DataTarget == null )
			{

				var gameObject = ( (Component)target ).gameObject;
				var defaultComponent = gameObject.GetComponent<dfControl>();

				binder.DataTarget = new dfComponentMemberInfo()
				{
					Component = defaultComponent
				};

			}

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
				.Where( member => isCompatibleType( member, sourcePropertyType, true ) )
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

			if( canUseFormatString( binder ) )
			{
				var formatString = EditorGUILayout.TextField( "Format", binder.FormatString );
				if( formatString != binder.FormatString )
				{
					dfEditorUtil.MarkUndo( binder, "Change format string" );
					binder.FormatString = formatString;
				}
			}
			else
			{
				binder.FormatString = null;
			}

		}

		using( dfEditorUtil.BeginGroup( "Synchronization" ) )
		{

			bool canSynchronize = binder.CanSynchronize();
			if( !canSynchronize )
			{
				EditorGUILayout.HelpBox( "Two-way binding is not possible between the target and source properties", MessageType.Info );
				binder.TwoWay = false;
			}
			else
			{

				var twoWay = EditorGUILayout.Toggle( "Two way", binder.TwoWay );
				if( twoWay != binder.TwoWay )
				{
					dfEditorUtil.MarkUndo( binder, "Change TwoWay property" );
					binder.TwoWay = twoWay;
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

	#region Private utility methods 

	private bool canUseFormatString( dfPropertyBinding binder )
	{

		if( binder.DataSource == null || binder.DataTarget == null )
			return false;

		if( !binder.DataTarget.IsValid || !binder.DataTarget.IsValid )
			return false;

		if( binder.DataSource.GetMemberType() != typeof( string ) )
		{
			return binder.DataTarget.GetMemberType() == typeof( string );
		}

		return false;

	}

	private bool isCompatibleType( MemberInfo member, Type type, bool allowStringType )
	{

		if( member.IsDefined( typeof( HideInInspector ), true ) )
			return false;

		if( member is FieldInfo )
		{

			var fieldInfo = (FieldInfo)member;

			if( type.IsAssignableFrom( fieldInfo.FieldType ) )
				return true;

			if( allowStringType && fieldInfo.FieldType == typeof( string ) )
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

			if( allowStringType && propertyInfo.PropertyType == typeof( string ) )
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

	private bool switchBindingType()
	{

		var spriteTypeNames = new string[] { "Polled", "Event-Driven" };
		var spriteTypes = new Type[] 
		{
			typeof( dfPropertyBinding ),
			typeof( dfEventDrivenPropertyBinding )
		}.ToList();

		var selectedIndex = spriteTypes.IndexOf( target.GetType() );
		var newIndex = EditorGUILayout.Popup( "Binding Model", selectedIndex, spriteTypeNames );
		if( newIndex != selectedIndex )
		{

			var scriptProperty = this.serializedObject.FindProperty( "m_Script" );
			if( scriptProperty == null )
			{
				return false;
			}

			var replacementScript = Resources
				.FindObjectsOfTypeAll( typeof( MonoScript ) )
				.Cast<MonoScript>()
				.Where( x =>
					x.GetType() == typeof( MonoScript ) && // Fix for Unity crash bug
					x.GetClass() == spriteTypes[ newIndex ]
				)
				.FirstOrDefault();

			if( replacementScript == null )
				return false;

			var activeObject = Selection.activeGameObject;
			Selection.activeGameObject = null;

			dfEditorUtil.DelayedInvoke( () =>
			{

				// Assign the selected MonoScript 
				scriptProperty.objectReferenceValue = replacementScript;
				scriptProperty.serializedObject.ApplyModifiedProperties();
				scriptProperty.serializedObject.Update();

				// Save the scene in case Unity crashes
				EditorUtility.SetDirty( activeObject );
				EditorApplication.SaveScene();
				EditorApplication.SaveAssets();

				dfEditorUtil.DelayedInvoke( () =>
				{
					Selection.activeGameObject = activeObject;
				} );

			} );

			return true;

		}

		return false;

	}

	#endregion

}
