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

public class dfTextEditorWindow : ScriptableWizard
{

	#region Callback definitions

	public delegate void Callback( string expression );

	#endregion

	#region Private instance variables 

	private Callback callback;
	private string originalText;
	private string text;
	private Vector2 scrollPosition;

	#endregion

	#region Static methods

	public static dfTextEditorWindow Show( string title, string text, Callback callback )
	{

		var dialog = ScriptableWizard.DisplayWizard<dfTextEditorWindow>( title );
		dialog.minSize = new Vector2( 512, 512 );
		dialog.text = dialog.originalText = text;
		dialog.callback = callback;

		dialog.ShowUtility();

		return dialog;

	}

	#endregion

	void OnGUI()
	{

		scrollPosition = GUILayout.BeginScrollView( scrollPosition, false, true );
		{

			var style = EditorStyles.textField;
			var wrap = style.wordWrap;

			style.wordWrap = true;

			GUI.SetNextControlName( "Text" );
			text = GUILayout.TextArea( 
				text, 
				GUILayout.Width( this.position.width - 25 ), 
				GUILayout.ExpandHeight( true ) 
			);
			GUI.FocusControl( "Text" );

			callback( text );

			style.wordWrap = wrap;

		}
		GUILayout.EndScrollView();

		EditorGUILayout.BeginHorizontal();
		{

			GUILayout.FlexibleSpace();

			if( GUILayout.Button( "Cancel", GUILayout.Width( 100 ) ) )
			{
				callback( originalText );
				base.Close();
				GUIUtility.ExitGUI();
			}

			if( GUILayout.Button( "Save", GUILayout.Width( 100 ) ) )
			{
				callback( text );
				base.Close();
				GUIUtility.ExitGUI();
			}

		}
		EditorGUILayout.EndHorizontal();

		var currentEvent = Event.current;
		if( currentEvent != null && currentEvent.isKey )
		{
			if( currentEvent.keyCode == KeyCode.Z && currentEvent.control )
			{

				// HACK!!! Otherwise Unity just does an Edit/Undo action
				currentEvent.Use();

				var te = GUIUtility.GetStateObject( typeof( TextEditor ), GUIUtility.keyboardControl ) as TextEditor;
				if( te != null )
				{
					te.Undo();
				}

			}
		}

	}

}
