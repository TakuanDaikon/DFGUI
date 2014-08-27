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

[CustomEditor( typeof( dfAnimationClip ) )]
public class dfAnimationClipInspector : Editor
{

	#region Unity menu integration

	// Slowly migrating menu option locations, will remove older ones as 
	// users become used to the new locations
	[MenuItem( "Tools/Daikon Forge/Assets/Create Animation Clip" )]
	[MenuItem( "Assets/Daikon Forge/Assets/Create Animation Clip" )]
	public static void CreateAnimationClip()
	{

		dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
		{

			var atlas = ( item == null ) ? null : item.GetComponent<dfAtlas>();
			if( atlas == null )
				return;

			var defaultPath = Path.GetDirectoryName( AssetDatabase.GetAssetPath( item ) );
			string prefabPath = EditorUtility.SaveFilePanel( "Create Animation Clip", defaultPath, "AnimationClip", "prefab" );
			if( string.IsNullOrEmpty( prefabPath ) )
				return;

			prefabPath = prefabPath.MakeRelativePath();

			var go = new GameObject() { name = Path.GetFileNameWithoutExtension( prefabPath ) };
			var clip = go.AddComponent<dfAnimationClip>();
			clip.Atlas = atlas;

			var prefab = PrefabUtility.CreateEmptyPrefab( prefabPath );
			if( prefab == null )
			{
				EditorUtility.DisplayDialog( "Create Animation Clip", "Failed to create the Animation Clip prefab", "OK" );
				return;
			}

			prefab.name = clip.name;
			PrefabUtility.ReplacePrefab( go, prefab );

			DestroyImmediate( go );
			AssetDatabase.Refresh();

			#region Delay execution of object baseFont to work around a Unity issue

			// Declared with null value to eliminate "uninitialized variable" 
			// compiler error in lambda below.
			EditorApplication.CallbackFunction callback = null;

			callback = () =>
			{
				EditorUtility.FocusProjectWindow();
				go = AssetDatabase.LoadMainAssetAtPath( prefabPath ) as GameObject;
				Selection.objects = new Object[] { go };
				EditorGUIUtility.PingObject( go );
				Debug.Log( "Animation clip created at " + prefabPath, prefab );
				EditorApplication.delayCall -= callback;
			};

			EditorApplication.delayCall += callback;

			#endregion

		};

		var dialog = dfPrefabSelectionDialog.Show( "Select Texture Atlas", typeof( dfAtlas ), selectionCallback, dfTextureAtlasInspector.DrawAtlasPreview, null );
		dialog.previewSize = 200;

	}

	#endregion 

	#region Preview rendering 

	public override bool HasPreviewGUI()
	{
		return true;
	}

	public override void OnPreviewGUI( Rect rect, GUIStyle background )
	{

		var clip = target as dfAnimationClip;
		if( clip.Atlas != null )
		{
			if( clip.Sprites != null && clip.Sprites.Count > 0 )
			{
				DrawSprite( rect, clip.Atlas, clip.Sprites[ 0 ]  );
				return;
			}
		}
		
		base.OnPreviewGUI( rect, background );

	}

	public static bool RenderPreview( GameObject item, Rect rect )
	{

		if( item == null )
			return false;

		var clip = item.GetComponent<dfAnimationClip>();
		if( clip.Atlas != null )
		{
			if( clip.Sprites != null && clip.Sprites.Count > 0 )
			{
				DrawSprite( rect, clip.Atlas, clip.Sprites[ 0 ] );
				return true;
			}
		}

		return false;

	}

	private static void DrawSprite( Rect rect, dfAtlas atlas, string sprite )
	{

		var spriteInfo = atlas[ sprite ];
		var size = spriteInfo.sizeInPixels;
		var destRect = rect;

		if( destRect.width < size.x || destRect.height < size.y )
		{

			var newHeight = size.y * rect.width / size.x;
			if( newHeight <= rect.height )
				destRect.height = newHeight;
			else
				destRect.width = size.x * rect.height / size.y;

		}
		else
		{
			destRect.width = size.x;
			destRect.height = size.y;
		}

		if( destRect.width < rect.width ) destRect.x = rect.x + ( rect.width - destRect.width ) * 0.5f;
		if( destRect.height < rect.height ) destRect.y = rect.y + ( rect.height - destRect.height ) * 0.5f;

		dfEditorUtil.DrawSprite( destRect, atlas, sprite );
	
	}

	#endregion

	public override void OnInspectorGUI()
	{

		var clip = target as dfAnimationClip;

		dfEditorUtil.LabelWidth = 110f;

		using( dfEditorUtil.BeginGroup( "Frame Source" ) )
		{
			SelectTextureAtlas( "Atlas", clip );
		}

		using( dfEditorUtil.BeginGroup( "Frames", 80 ) )
		{

			editFrames( clip );

			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

		}

	}

	private void editFrames( dfAnimationClip animation )
	{

		EditorGUILayout.Separator();

		var collectionModified = false;
		var showDialog = false;

		var sprites = animation.Sprites;
		for( int i = 0; i < sprites.Count && !collectionModified; i++ )
		{

			EditorGUILayout.BeginHorizontal();
			{

				EditorGUILayout.LabelField( "Frame " + i, "", GUILayout.Width( dfEditorUtil.LabelWidth - 6 ) );

				GUILayout.Space( 2 );

				var value = sprites[ i ];
				var displayText = string.IsNullOrEmpty( value ) ? "[none]" : value;
				GUILayout.Label( displayText, "TextField" );

				if( GUILayout.Button( new GUIContent( " ", "Select Frame" ), "IN ObjectField", GUILayout.Width( 14 ) ) )
				{
					var index = i;
					dfEditorUtil.DelayedInvoke( (System.Action)( () =>
					{
						dfSpriteSelectionDialog.Show( "Select Sprite", animation.Atlas, value, ( spriteName ) =>
						{
							dfEditorUtil.MarkUndo( animation, "Select animation frame" );
							sprites[ index ] = spriteName;
						} );
					} ) );
				}

				GUI.enabled = i > 0;
				if( GUILayout.Button( "\u25B2", "minibutton", GUILayout.Width( 22 ) ) && i > 0 )
				{
					dfEditorUtil.MarkUndo( animation, "Reorder animation frames" );
					var temp = sprites[ i - 1 ];
					sprites[ i - 1 ] = sprites[ i ];
					sprites[ i ] = temp;
				}

				GUI.enabled = i < sprites.Count - 1;
				if( GUILayout.Button( "\u25BC", "minibutton", GUILayout.Width( 22 ) ) && i < sprites.Count - 1 )
				{
					dfEditorUtil.MarkUndo( animation, "Reorder animation frames" );
					var temp = sprites[ i + 1 ];
					sprites[ i + 1 ] = sprites[ i ];
					sprites[ i ] = temp;
				}

				GUI.enabled = true;
				if( GUILayout.Button( "x", "minibutton", GUILayout.Width( 20 ) ) )
				{
					dfEditorUtil.MarkUndo( animation, "Remove animation frame" );
					sprites.RemoveAt( i );
					collectionModified = true;
				}

			}
			EditorGUILayout.EndHorizontal();

		}

		EditorGUILayout.Separator();

		GUILayout.BeginHorizontal();
		{
			
			if( sprites.Count > 0 )
			{
				if( GUILayout.Button( "Auto Fill '" + stripSuffix( sprites.Last() ) + "*'" ) )
				{
					autoFill( animation );
				}
			}

			if( GUILayout.Button( "Add Frame" ) )
			{
				showDialog = true;
			}

		}
		GUILayout.EndHorizontal();

		if( showDialog )
		{
			dfEditorUtil.DelayedInvoke( (System.Action)( () =>
			{
				dfSpriteSelectionDialog.Show( "Select Sprite", animation.Atlas, null, ( selected ) =>
				{
					if( !string.IsNullOrEmpty( selected ) )
					{
						dfEditorUtil.MarkUndo( animation.gameObject, "Add new animation frame" );
						sprites.Add( selected );
					}
				} );
			} ) );
		}

	}

	private void autoFill( dfAnimationClip animation )
	{

		var spriteName = animation.Sprites.Last();
		var prefix = stripSuffix( spriteName );
		if( string.IsNullOrEmpty( prefix ) )
		{
			EditorUtility.DisplayDialog( "Auto-Fill Animation Clip", "Unable to determine a valid sprite prefix based on the name " + spriteName, "CANCEL" );
			return;
		}

		dfEditorUtil.MarkUndo( animation, "Auto-fill animation frames" );

		var results = new List<string>();

		for( int i = 0; i < animation.Atlas.Items.Count; i++ )
		{
			var item = animation.Atlas.Items[ i ].name;
			if( item.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) )
			{
				if( !animation.Sprites.Contains( item ) )
				{
					results.Add( item );
				}
			}
		}

		if( results.Count == 0 )
		{
			EditorUtility.DisplayDialog( "Auto-Fill Animation Clip", "No additional sprites matching '" + prefix + "' could be found", "OK" );
			return;
		}

		results
			.OrderBy( x => formatName( x ) )
			.ToList()
			.ForEach( x => animation.Sprites.Add( x ) );

	}

	private string stripSuffix( string name )
	{

		for( int i = name.Length - 1; i >= 0; i-- )
		{
			if( char.IsLetter( name[ i ] ) )
			{
				return name.Substring( 0, i + 1 );
			}
		}

		return "";

	}

	private void doDragDrop( dfAnimationClip animation )
	{

		var atlas = animation.Atlas;
		if( atlas == null )
			return;

		var failed = new List<string>();

		var textures = DragAndDrop.objectReferences
			.OrderBy( x => formatName( x.name ) )
			.Select( x => x.name )
			.ToList();

		if( textures.Count > 0 )
		{
			dfEditorUtil.MarkUndo( animation, "Add frames" );
		}

		Debug.Log( "Textures dropped: " + textures.Count );

		for( int i = 0; i < textures.Count; i++ )
		{

			var name = textures[ i ];
			if( atlas[ name ] == null )
			{
				failed.Add( name );
			}
			else
			{
				animation.Sprites.Add( name );
			}

		}

		if( failed.Count > 0 )
		{
			var message = "The following textures are not in the Atlas:\r\n" + string.Join( "\r\n", failed.ToArray() );
			EditorUtility.DisplayDialog( "Texture not found", message, "OK" );
		}

	}

	/// <summary>
	/// Formats a string such that strings with common prefixes and numeric
	/// suffixes will sort in numeric order.
	/// </summary>
	static string formatName( string text )
	{

		var suffix = Regex.Matches( text, @"(\d+$)" )
			.Cast<System.Text.RegularExpressions.Match>()
			.Select( m => m.Value )
			.FirstOrDefault();

		if( string.IsNullOrEmpty( suffix ) )
		{
			return text;
		}

		int number = 0;
		if( int.TryParse( suffix, out number ) )
		{
			var index = text.LastIndexOf( suffix );
			return text.Substring( 0, index ) + number.ToString( "D12" );
		}

		return text;

	}

	protected internal static void SelectTextureAtlas( string label, dfAnimationClip clip )
	{

		var savedColor = GUI.color;
		var showDialog = false;

		try
		{

			var atlas = clip.Atlas;

			if( atlas == null )
				GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.red;

			dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
			{
				var newAtlas = ( item == null ) ? null : item.GetComponent<dfAtlas>();
				dfEditorUtil.MarkUndo( clip, "Change Atlas" );
				clip.Atlas = newAtlas;
			};

			var value = clip.Atlas;

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

				if( GUI.enabled && GUILayout.Button( new GUIContent( " ", "Edit Atlas" ), "IN ObjectField", GUILayout.Width( 14 ) ) )
				{
					showDialog = true;
				}

			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space( 2 );

			if( showDialog )
			{
				var dialog = dfPrefabSelectionDialog.Show( "Select Texture Atlas", typeof( dfAtlas ), selectionCallback, dfTextureAtlasInspector.DrawAtlasPreview, null );
				dialog.previewSize = 200;
			}

		}
		finally
		{
			GUI.color = savedColor;
		}

	}

}
