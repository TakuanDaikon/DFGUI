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

public class dfWelcomeDialog : EditorWindow
{

	private Texture2D m_KeyImage;
	private TextAsset m_Readme;

	private Vector2 m_ReadmeScrollPosition;
	private Rect m_KeyImageRect = new Rect( 4, 4, 512, 64 );
	private Rect m_MainAreaRect = new Rect( 4, 72, 512, 324 );
	private bool m_ViewingReadme;

	[MenuItem( "Tools/Daikon Forge/Help/View the Changelog" )]
	internal static void ShowWelcomeMessage()
	{

		var window = GetWindow<dfWelcomeDialog>();
		window.title = "Welcome";
		window.minSize = window.maxSize = new Vector2( 520, 400 );

		window.ShowUtility();

	}

	void OnEnable()
	{
		m_KeyImage = Resources.Load( "dfgui-header", typeof( Texture2D ) ) as Texture2D;
		m_Readme = Resources.Load( "Change Log", typeof( TextAsset ) ) as TextAsset;
		minSize = new Vector2( 520, 400 );
		maxSize = new Vector2( 520, 400 );
		position = new Rect( position.x, position.y, 520, 400 );
	}

	public void OnGUI()
	{

		try
		{

			if( m_KeyImage == null || m_Readme == null )
			{
				EditorGUILayout.HelpBox( "This installation appears to be broken. Cannot find the 'Change Log.txt' resource", MessageType.Error );
				return;
			}

			GUI.DrawTexture( m_KeyImageRect, m_KeyImage );

			GUILayout.BeginArea( m_MainAreaRect, GUI.skin.box );

			m_ReadmeScrollPosition = GUILayout.BeginScrollView( m_ReadmeScrollPosition, false, false, GUILayout.Width( 502 ), GUILayout.Height( 292 ) );

			GUILayout.Label( m_Readme.text, EditorStyles.wordWrappedLabel );

			GUILayout.EndScrollView();

			GUILayout.BeginVertical();
			GUILayout.FlexibleSpace();

			if( GUILayout.Button( "Done", GUILayout.Height( 22 ) ) )
				this.Close();

			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();

			GUILayout.EndArea();

		}
		catch { }

	}

}
