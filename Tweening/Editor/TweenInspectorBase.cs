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

public class TweenInspectorBase : Editor
{

	public override void OnInspectorGUI()
	{

		var tween = target as dfTweenComponentBase;

		var tweenTarget = tween.Target;
		if( tweenTarget == null )
		{

			tweenTarget = tween.Target = new dfComponentMemberInfo()
			{
				Component = getDefaultComponent()
			};

		}

		if( tweenTarget.Component == null )
		{
			tweenTarget.Component = getDefaultComponent();
		}
		
		using( dfEditorUtil.BeginGroup( "General" ) )
		{
			var name = EditorGUILayout.TextField( "Name", tween.TweenName );
			if( !string.Equals( name, tween.TweenName ) )
			{
				dfEditorUtil.MarkUndo( tween, "Change Tween Name" );
				tween.TweenName = name;
			}
		}

		using( dfEditorUtil.BeginGroup( "Property" ) )
		{

			var tweenType =
				target
				.GetType()
				.BaseType
				.GetGenericArguments()[ 0 ];

			EditorGUI.BeginChangeCheck();
			tweenTarget = EditPropertyInfo( tweenTarget, tweenType );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify property configuration" );
				target.SetProperty( "Target", tweenTarget );
			}

		}

		if( string.IsNullOrEmpty( tweenTarget.MemberName ) )
		{
			return;
		}

		using( dfEditorUtil.BeginGroup( "Animation" ) )
		{

			EditorGUI.BeginChangeCheck();
			var autoRun = EditorGUILayout.Toggle( "Auto Run", tween.AutoRun );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Change AutoRun property" );
				tween.AutoRun = autoRun;
			}

			EditorGUI.BeginChangeCheck();
			var easingType = (dfEasingType)EditorGUILayout.EnumPopup( "Function", tween.Function );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Assign tween function" );
				tween.Function = easingType;
			}

			EditorGUI.BeginChangeCheck();
			var curve = EditorGUILayout.CurveField( "Curve", tween.AnimationCurve );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify animation curve" );
				tween.AnimationCurve = curve;
			}

			EditorGUI.BeginChangeCheck();
			var loop = (dfTweenLoopType)EditorGUILayout.EnumPopup( "Loop", tween.LoopType );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify tween loop" );
				tween.LoopType = loop;
			}

			EditorGUI.BeginChangeCheck();
			var length = EditorGUILayout.FloatField( "Length", tween.Length );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify tween time" );
				tween.Length = length;
			}

			EditorGUI.BeginChangeCheck();
			var delay = EditorGUILayout.FloatField( "Delay", tween.StartDelay );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify Tween Delay" );
				tween.StartDelay = Mathf.Max( delay, 0 );
			}

		}

		var serialized = new SerializedObject( target );

		using( dfEditorUtil.BeginGroup( "Start Value" ) )
		{

			EditorGUI.BeginChangeCheck();
			var syncStart = EditorGUILayout.Toggle( "Sync on Run", tween.SyncStartValueWhenRun );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify Sync Start property" );
				tween.SyncStartValueWhenRun = syncStart;
			}

			if( !syncStart )
			{

				EditorGUI.BeginChangeCheck();
				var startOffset = EditorGUILayout.Toggle( "Value is Offset", tween.StartValueIsOffset );
				if( EditorGUI.EndChangeCheck() )
				{
					dfEditorUtil.MarkUndo( target, "Toggle Start Offset" );
					tween.StartValueIsOffset = startOffset;
				}

				EditorGUI.BeginChangeCheck();
				var startProp = serialized.FindProperty( "startValue" );
				EditorGUILayout.PropertyField( startProp, true );
				if( EditorGUI.EndChangeCheck() )
				{
					dfEditorUtil.MarkUndo( target, "Modify start value" );
					serialized.ApplyModifiedProperties();
				}

				if( tweenTarget != null && tweenTarget.IsValid )
				{

					EditorGUILayout.BeginHorizontal();
					GUILayout.Space( 100f );
					EditorGUILayout.BeginVertical();

					if( GUILayout.Button( "Assign Current Value", "minibutton" ) )
					{
						dfEditorUtil.MarkUndo( target, "Assign current value" );
						var currentValue = tweenTarget.Component.GetProperty( tweenTarget.MemberName );
						target.SetProperty( "StartValue", currentValue );
					}

					if( GUILayout.Button( "Revert", "minibutton" ) )
					{
						dfEditorUtil.MarkUndo( target, "Assign current value" );
						var currentValue = target.GetProperty( "StartValue" );
						tweenTarget.Component.SetProperty( tweenTarget.MemberName, currentValue );
					}

					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();

				}

			}

		}

		using( dfEditorUtil.BeginGroup( "End Value" ) )
		{

			EditorGUI.BeginChangeCheck();
			var syncEnd = EditorGUILayout.Toggle( "Sync on Run", tween.SyncEndValueWhenRun );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( target, "Modify Sync End property" );
				tween.SyncEndValueWhenRun = syncEnd;
			}

			if( !syncEnd )
			{

				EditorGUI.BeginChangeCheck();
				var endOffset = EditorGUILayout.Toggle( "Value is Offset", tween.EndValueIsOffset );
				if( EditorGUI.EndChangeCheck() )
				{
					dfEditorUtil.MarkUndo( target, "Toggle End Offset" );
					tween.EndValueIsOffset = endOffset;
				}

				EditorGUI.BeginChangeCheck();
				var endProp = serialized.FindProperty( "endValue" );
				EditorGUILayout.PropertyField( endProp, true );
				if( EditorGUI.EndChangeCheck() )
				{
					dfEditorUtil.MarkUndo( target, "Modify end value" );
					serialized.ApplyModifiedProperties();
				}

				if( tweenTarget != null && tweenTarget.IsValid )
				{

					EditorGUILayout.BeginHorizontal();
					GUILayout.Space( 100f );
					EditorGUILayout.BeginVertical();

					if( GUILayout.Button( "Assign Current Value", "minibutton" ) )
					{
						dfEditorUtil.MarkUndo( target, "Assign current value" );
						var currentValue = tweenTarget.Component.GetProperty( tweenTarget.MemberName );
						target.SetProperty( "EndValue", currentValue );
					}

					if( GUILayout.Button( "Revert", "minibutton" ) )
					{
						dfEditorUtil.MarkUndo( target, "Assign current value" );
						var currentValue = target.GetProperty( "EndValue" );
						tweenTarget.Component.SetProperty( tweenTarget.MemberName, currentValue );
					}

					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();

				}

			}

		}

		showDebugButtons( tween );

	}

	private static void showDebugButtons( dfTweenPlayableBase tween )
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
			if( tween.IsPlaying && tween is dfTweenComponentBase )
			{
				var target = tween as dfTweenComponentBase;
				if( GUILayout.Button( target.IsPaused ? "Resume" : "Pause", "minibutton" ) )
				{
					target.IsPaused = !target.IsPaused;
				}
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

		if( target is dfTweenRotation )
		{
			return gameObject.transform;
		}

		var control = gameObject.GetComponent<dfControl>();
		if( control != null )
			return control;

		var defaultComponent = gameObject.GetComponents<MonoBehaviour>().Where( c => c != target ).FirstOrDefault();

		return defaultComponent;

	}

	protected dfComponentMemberInfo EditPropertyInfo( dfComponentMemberInfo MemberInfo, Type DataType )
	{

		var sourceComponent = dfEditorUtil.ComponentField( "Component", MemberInfo.Component );
		if( sourceComponent != MemberInfo.Component )
		{
			dfEditorUtil.MarkUndo( target, "Assign Data Source" );
			MemberInfo.Component = sourceComponent;
		}

		if( sourceComponent == null )
		{
			//EditorGUILayout.HelpBox( "Missing component", MessageType.Error );
			return MemberInfo;
		}

		var sourceComponentMembers = 
			getMemberList( sourceComponent )
			.Where( m => isValidFieldType( m, DataType ) )
			.Select( m => m.Name )
			.ToArray();

		var memberIndex = findIndex( sourceComponentMembers, MemberInfo.MemberName );
		var selectedIndex = EditorGUILayout.Popup( "Property", memberIndex, sourceComponentMembers );
		if( selectedIndex >= 0 && selectedIndex < sourceComponentMembers.Length )
		{
			var memberName = sourceComponentMembers[ selectedIndex ];
			if( memberName != MemberInfo.MemberName )
			{
				dfEditorUtil.MarkUndo( target, "Assign Data Source" );
				MemberInfo.MemberName = memberName;
			}
		}

		return MemberInfo;

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
				m.MemberType == MemberTypes.Field ||
				m.MemberType == MemberTypes.Property
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

}

