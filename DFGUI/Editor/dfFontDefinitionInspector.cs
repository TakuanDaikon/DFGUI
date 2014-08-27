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

[CustomEditor( typeof( dfFont ) )]
public class dfFontDefinitionInspector : Editor
{

	#region Unity menu integration 

	// Slowly migrating menu option locations, will remove older ones as 
	// users become used to the new locations
	[MenuItem( "Tools/Daikon Forge/Fonts/Create Bitmapped Font" )]
	[MenuItem( "Assets/Daikon Forge/Fonts/Create Bitmapped Font", false, 0 )]
	public static void CreateFontFromSelection()
	{

		var selection = Selection.GetFiltered( typeof( TextAsset ), SelectionMode.Assets ).FirstOrDefault() as TextAsset;
		if( selection == null )
		{
			EditorUtility.DisplayDialog( "Create Bitmapped Font", "Please select an AngelCode text-format font definition file before proceeding", "OK" );
			return;
		}

		var go = new GameObject();
		var font = CreateFrom( go, selection );
		if( font == null )
			return;

		go.name = font.FontFace.Replace( " ", "" ) + font.FontSize;

		var saveFolder = Path.GetDirectoryName( AssetDatabase.GetAssetPath( selection ) );
		var prefabPath = EditorUtility.SaveFilePanel( "Create Font Definition", saveFolder, go.name, "prefab" );
		if( string.IsNullOrEmpty( prefabPath ) )
		{
			DestroyImmediate( go );
			return;
		}

		prefabPath = prefabPath.MakeRelativePath();

		var prefab = PrefabUtility.CreateEmptyPrefab( prefabPath );
		prefab.name = go.name;
		PrefabUtility.ReplacePrefab( go, prefab );

		DestroyImmediate( go );
		AssetDatabase.Refresh();

		#region Delay execution of object selection to work around a Unity issue

		// Declared with null value to eliminate "uninitialized variable" 
		// compiler error in lambda below.
		EditorApplication.CallbackFunction callback = null;

		callback = () =>
		{
			EditorUtility.FocusProjectWindow();
			go = AssetDatabase.LoadMainAssetAtPath( prefabPath ) as GameObject;
			Selection.objects = new Object[] { go };
			EditorGUIUtility.PingObject( go );
			Debug.Log( "Font definition prefab created at " + prefabPath, prefab );
			EditorApplication.delayCall -= callback;
		};

		EditorApplication.delayCall += callback;

		#endregion

	}

	#region Utility methods

	public static dfFont CreateFrom( GameObject owner, TextAsset file )
	{
		var path = AssetDatabase.GetAssetPath( file );
		return CreateFromFile( owner, path );
	}

	public static dfFont CreateFromFile( GameObject owner, string path )
	{

		if( !File.Exists( path ) )
			throw new FileNotFoundException( "Font definition file not found", path );

		using( var file = File.OpenText( path ) )
		{

			var fileData = file.ReadToEnd().Trim();

			if( !fileData.StartsWith( "info " ) )
			{
				Debug.LogError( "This file does not appear to be a valid BMFont text-format file" );
				return null;
			}

			var font = owner.AddComponent<dfFont>();

			var typePattern = new Regex( @"^(?<Type>char|info|common|page|kerning|chars)\s+.*", RegexOptions.ExplicitCapture );
			var globalProperties = new Dictionary<string, string>();

			var lines = fileData.Trim().Split( '\n' );
			foreach( var line in lines )
			{

				var typeMatch = typePattern.Match( line );
				if( typeMatch == null || typeMatch.Groups[ "Type" ] == null )
					continue;

				var type = typeMatch.Groups[ "Type" ].Value;
				if( type != "char" )
				{

					if( type == "kerning" )
					{

						var kprop = new Dictionary<string, string>();
						parseProperties( kprop, line );

						font.AddKerning(
							int.Parse( kprop[ "first" ] ),
							int.Parse( kprop[ "second" ] ),
							int.Parse( kprop[ "amount" ] )
						);

					}
					else
					{

						if( type == "page" && globalProperties.ContainsKey( "page" ) )
						{
							Debug.LogError( "Multi-page fonts are currently not supported" );
							return null;
						}

						parseProperties( globalProperties, line );

					}

					continue;

				}

				var glyph = new dfFont.GlyphDefinition();
				var glyphProperties = new Dictionary<string, string>();

				parseProperties( glyphProperties, line );
				foreach( string key in glyphProperties.Keys )
				{

					var value = glyphProperties[ key ];

					var field = typeof( dfFont.GlyphDefinition ).GetField( key, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic );
					if( field != null )
					{
						var convertedValue = Convert.ChangeType( value, field.FieldType );
						field.SetValue( glyph, convertedValue );
					}

				}

				font.Glyphs.Add( glyph );

			}

			if( globalProperties.Count == 0 )
			{
				Debug.LogError( "This file does not appear to be a valid BMFont text-format file" );
				DestroyImmediate( font );
				return null;
			}

			foreach( string key in globalProperties.Keys )
			{

				var value = globalProperties[ key ];

				var field = typeof( dfFont ).GetField( key, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic );
				if( field == null )
					continue;

				try
				{

					var convertedValue = (object)null;
					if( field.FieldType == typeof( bool ) )
					{
						convertedValue = ( value != "0" );
					}
					else if( !field.FieldType.IsArray )
					{
						convertedValue = Convert.ChangeType( value, field.FieldType );
					}
					else
					{
						var elementType = field.FieldType.GetElementType();
						var elements = ( (string)value ).Split( ',' );
						var array = Array.CreateInstance( elementType, elements.Length );
						for( int i = 0; i < elements.Length; i++ )
						{
							array.SetValue( Convert.ChangeType( elements[ i ], elementType ), i );
						}
						convertedValue = array;
					}

					field.SetValue( font, convertedValue );

				}
				catch( Exception err )
				{
					var errmsg = string.Format( "Failed to set property {0} to value {1} - {2}", key, value, err.Message );
					Debug.LogError( errmsg );
				}

			}

			font.Glyphs.Sort();

			return font;

		}

	}

	private static void parseProperties( Dictionary<string, string> properties, string line )
	{

		Regex propertyPattern = new Regex( @"(?<Property>(?<Key>[a-z0-9]+)=(?<Value>(""((\\"")|\\\\|[^""\n])*"")|([\S]+))\s*)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );

		var matches = propertyPattern.Matches( line.Trim() );
		foreach( System.Text.RegularExpressions.Match property in matches )
		{

			var key = property.Groups[ "Key" ].Value;
			var value = property.Groups[ "Value" ].Value.Trim( '"' );

			properties[ key ] = value;

		}

	}

	#endregion

	#endregion 

	public override void OnInspectorGUI()
	{

		var font = this.target as dfFont;

		dfEditorUtil.LabelWidth = 90f;

		SelectTextureAtlas( "Atlas", font, "Atlas", false, true );

		if( font.Atlas == null )
			return;

		EditSprite( "Font Sprite", font, "Sprite", 95 );

	}

	protected internal static void EditSprite( string label, dfFont font, string propertyName )
	{
		EditSprite( label, font, propertyName, 90 );
	}

	protected internal static void EditSprite( string label, dfFont font, string propertyName, int labelWidth )
	{

		var atlas = font.Atlas;
		if( atlas == null )
			return;

		dfSpriteSelectionDialog.SelectionCallback callback = delegate( string spriteName )
		{
			dfEditorUtil.MarkUndo( font, "Change Sprite" );
			font.Sprite = spriteName;
		};

		var value = font.Sprite;

		EditorGUILayout.BeginHorizontal();
		{

			GUILayout.Label( label, GUILayout.Width( labelWidth ) );

			var displayText = string.IsNullOrEmpty( value ) ? "[none]" : value;
			GUILayout.Label( displayText, "TextField", GUILayout.ExpandWidth( true ) );

			var evt = Event.current;
			if( evt != null && evt.type == EventType.mouseDown && evt.clickCount == 2 )
			{
				Rect rect = GUILayoutUtility.GetLastRect();
				if( rect.Contains( evt.mousePosition ) )
				{
					if( GUI.enabled && value != null )
					{
						dfTextureAtlasInspector.SelectedSprite = value;
						Selection.activeObject = atlas;
						EditorGUIUtility.PingObject( atlas );
					}
				}
			}

			if( GUILayout.Button( new GUIContent( " ", "Edit " + label ), "IN ObjectField", GUILayout.Width( 12 ) ) )
			{
				dfEditorUtil.DelayedInvoke( (System.Action)( () =>
				{
					dfSpriteSelectionDialog.Show( "Select Sprite: " + label, atlas, value, callback );
				} ) );
			}

		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space( 2 );

	}

	public override bool HasPreviewGUI()
	{

		var font = target as dfFont;

		return
			font != null &&
			font.Atlas != null &&
			font.Atlas[ font.Sprite ] != null;

	}

	public override void OnPreviewGUI( Rect rect, GUIStyle background )
	{

		var font = target as dfFont;
		if( font == null || font.Atlas == null )
			return;

		DrawFontPreview( font, rect );

		var atlas = font.Atlas;
		var sprite = atlas[ font.Sprite ];

		var width = (int)sprite.sizeInPixels.x;
		var height = (int)sprite.sizeInPixels.y;

		string text = string.Format( "Sprite Size: {0}x{1}", width, height );
		EditorGUI.DropShadowLabel( GUILayoutUtility.GetRect( Screen.width, 18f ), text );

	}

	internal static bool DrawFontPreview( GameObject item, Rect rect )
	{

		var dynamicFont = item.GetComponent<dfDynamicFont>();
		if( dynamicFont != null )
		{
			dfDynamicFontInspector.DrawFontPreview( dynamicFont, rect );
			return true;
		}
			
		var font = item.GetComponent<dfFont>();
		if( font == null )
			return false;

		DrawFontPreview( font, rect );

		return true;

	}

	internal static void DrawFontPreview( dfFont font, Rect rect )
	{

		var previewString = "0123456789AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz!@#$%^&*()[]{}\\/| Grumpy wizards make toxic brew for the evil Queen and Jack. The quick brown fox jumps over the lazy dog.";

		var x = 0;
		var y = 0;

		var atlas = font.Atlas;
		var texture = getTexture( atlas, font.Sprite );
		if( texture == null )
			return;

		var width = 1f / (float)texture.width;
		var height = 1f / (float)texture.height;

		for( int i = 0; i < previewString.Length && y < rect.height; i++ )
		{

			var glyph = font.GetGlyph( previewString[ i ] );
			if( glyph == null )
				continue;

			if( x + glyph.xadvance > rect.width )
			{
				x = 0;
				y += font.LineHeight;
				if( y + font.LineHeight >= rect.height )
					break;
			}

			var xofs = x + ( x > 0 ? glyph.xoffset : 0 );
			var yofs = y + glyph.yoffset;
			var glyphRect = new Rect( rect.x + xofs, rect.y + yofs, glyph.width, glyph.height );

			var uv = new Rect(
				(float)glyph.x * width,
				1f - (float)glyph.y * height - (float)glyph.height * height,
				(float)glyph.width * width,
				(float)glyph.height * height
			);

			GUI.DrawTextureWithTexCoords( glyphRect, texture, uv, true );

			x += glyph.xadvance;

		}

	}

	private static Texture2D getTexture( dfAtlas atlas, string sprite )
	{

		var spriteInfo = atlas[ sprite ];
		if( spriteInfo == null )
			return null;

		var guid = atlas[ sprite ].textureGUID;
		if( string.IsNullOrEmpty( guid ) )
			return null;

		var path = AssetDatabase.GUIDToAssetPath( guid );
		if( string.IsNullOrEmpty( path ) )
			return null;
		
		return AssetDatabase.LoadAssetAtPath( path, typeof( Texture2D ) ) as Texture2D;

	}

	private static void setValue( dfFont control, string propertyName, object value )
	{

		var property = control.GetType().GetProperty( propertyName );
		if( property == null )
			throw new ArgumentException( "Property '" + propertyName + "' does not exist on " + control.GetType().Name );

		property.SetValue( control, value, null );

	}

	private static object getValue( dfFont control, string propertyName )
	{

		var property = control.GetType().GetProperty( propertyName );
		if( property == null )
			throw new ArgumentException( "Property '" + propertyName + "' does not exist on " + control.GetType().Name );

		return property.GetValue( control, null );

	}

	protected internal static void SelectTextureAtlas( string label, dfFont view, string propertyName, bool readOnly, bool colorizeIfMissing )
	{
		SelectTextureAtlas( label, view, propertyName, readOnly, colorizeIfMissing, 95 );
	}

	protected internal static void SelectTextureAtlas( string label, dfFont view, string propertyName, bool readOnly, bool colorizeIfMissing, int labelWidth )
	{

		var savedColor = GUI.color;
		var showDialog = false;

		try
		{

			var atlas = getValue( view, propertyName ) as dfAtlas;

			GUI.enabled = !readOnly;

			if( atlas == null && colorizeIfMissing )
				GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.red;

			dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
			{
				var newAtlas = ( item == null ) ? null : item.GetComponent<dfAtlas>();
				dfEditorUtil.MarkUndo( view, "Change Atlas" );
				setValue( view, propertyName, newAtlas );
			};

			var value = (dfAtlas)getValue( view, propertyName );

			EditorGUILayout.BeginHorizontal();
			{

				EditorGUILayout.LabelField( label, "", GUILayout.Width( labelWidth ) );

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
			GUI.enabled = true;
			GUI.color = savedColor;
		}

	}

}

