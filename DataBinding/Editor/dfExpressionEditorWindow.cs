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

public class dfExpressionEditorWindow : ScriptableWizard
{

	#region Callback definitions 

	public delegate void Callback( string expression );

	#endregion

	#region Public fields 

	public Callback callback;
	public string expression;

	#endregion

	#region Static methods

	public static dfExpressionEditorWindow Show( string title, string expression, Callback callback )
	{

		var dialog = ScriptableWizard.DisplayWizard<dfExpressionEditorWindow>( title );
		dialog.minSize = new Vector2( 300, 200 );
		dialog.callback = callback;
		dialog.expression = expression;

		dialog.ShowAuxWindow();

		return dialog;

	}

	#endregion

	void OnGUI()
	{

		var info = @"Enter your expression in the area below. You may use any valid script expression that returns a value.

The Data Source component can be referenced as the variable 'source', for example: 'source.PropertyName'

See the online documentation for more information and examples.
";

		GUILayout.Label( info, EditorStyles.wordWrappedLabel );
		EditorGUILayout.Separator();

		var wrap = EditorStyles.textField.wordWrap;
		EditorStyles.textField.wordWrap = true;

		expression = EditorGUILayout.TextArea( expression, GUILayout.ExpandHeight( true ) );

		EditorStyles.textField.wordWrap = wrap;

		EditorGUILayout.BeginHorizontal();
		{

			GUILayout.FlexibleSpace();

			if( GUILayout.Button( "Cancel", GUILayout.Width( 100 ) ) )
			{
				base.Close();
				GUIUtility.ExitGUI();
			}

			if( GUILayout.Button( "Save", GUILayout.Width( 100 ) ) )
			{
				callback( expression );
				base.Close();
				GUIUtility.ExitGUI();
			}

		}
		EditorGUILayout.EndHorizontal();

	}

}
