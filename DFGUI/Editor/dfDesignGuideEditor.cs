/* Copyright 2013-2014 Daikon Forge */
using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class dfDesignGuideEditor : EditorWindow
{

	#region Private variables

	private dfGUIManager view = null;
	private dfDesignGuide guide = null;
	private bool pixelMode = true;

	#endregion

	internal static void Edit( dfGUIManager view, dfDesignGuide guide )
	{

		var window = dfDesignGuideEditor.GetWindow<dfDesignGuideEditor>();
		window.title = "Edit Guide";
		window.minSize = window.maxSize = new Vector2( 250, 75 );
		window.guide = guide;
		window.view = view;

		window.ShowUtility();

	}

	public void OnLostFocus()
	{
		dfEditorUtil.DelayedInvoke( () => { closeWindow( this ); } );
	}

	public void OnGUI()
	{

		var maxValue = ( guide.orientation == dfControlOrientation.Vertical ) ? view.FixedWidth : view.FixedHeight;

		if( pixelMode )
		{
			EditorGUI.BeginChangeCheck();
			guide.position = EditorGUILayout.IntField( "Pixel Position", Mathf.Max( 0, Mathf.Min( maxValue, guide.position ) ) );
			if( EditorGUI.EndChangeCheck() )
			{
				EditorUtility.SetDirty( view );
				SceneView.RepaintAll();
			}
		}
		else
		{

			var percent = Mathf.RoundToInt( (float)guide.position / (float)maxValue * 100 );

			EditorGUI.BeginChangeCheck();
			percent = EditorGUILayout.IntSlider( "Percent Position", percent, 0, 100 );
			if( EditorGUI.EndChangeCheck() )
			{
				guide.position = Mathf.RoundToInt( ( (float)percent / 100f ) * maxValue );
				EditorUtility.SetDirty( view );
				SceneView.RepaintAll();
			}
		}

		var selected = EditorGUILayout.Popup( "Mode", pixelMode ? 0 : 1, new string[] { "Pixels", "Percent" } );
		pixelMode = selected == 0;

		if( GUILayout.Button( "Done" ) )
		{
			closeWindow( this );
		}

	}

	private void closeWindow( EditorWindow window )
	{

		if( guide != null )
		{

			var maxValue = ( guide.orientation == dfControlOrientation.Vertical ) ? view.FixedWidth : view.FixedHeight;
			if( guide.position <= 0 || guide.position >= maxValue )
			{
				EditorUtility.SetDirty( view );
				view.guides.Remove( guide );
			}

			view = null;
			guide = null;

			this.Close();

		}

	}

}
