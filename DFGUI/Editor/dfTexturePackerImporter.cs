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

using DICT = System.Collections.Generic.Dictionary<string, object>;

/// <summary>
/// Imports texture atlases generated with the Texture Packer application.
/// https://www.codeandweb.com/texturepacker
/// </summary>
public class dfTexturePackerImporter : EditorWindow
{

	#region Private variables

	private Texture2D textureFile = null;
	private TextAsset dataFile = null;

	#endregion

	#region Editor menu integration

	// Slowly migrating menu option locations, will remove older ones as 
	// users become used to the new locations
	[MenuItem( "Tools/Daikon Forge/Texture Atlas/Import from TexturePacker" )]
	[MenuItem( "Assets/Daikon Forge/Texture Atlas/Import Texture Atlas", false, 1 )]
	[MenuItem( "GameObject/Daikon Forge/Texture Atlas/Import Texture Atlas" )]
	static void ShowImportWizard()
	{
		var window = GetWindow<dfTexturePackerImporter>();
		window.title = "Import Atlas";
		window.minSize = new Vector2( 400, 300 );
		window.ShowUtility();
	}

	#endregion

	public void OnGUI()
	{

		string message = "This wizard allows you to import a texture atlas generated" +
			"with the Texture Packer program. You must select both the Texture and the " +
			"Data File in order to proceed. You must select the 'Unity3D' format in the " +
			"'Data Format' dropdown when exporting these files from Texture Packer.";

		EditorGUILayout.HelpBox( message, MessageType.Info );

		if( GUILayout.Button( "Texture Packer Documentation", "minibutton", GUILayout.ExpandWidth( true ) ) )
		{
			Application.OpenURL( "https://www.codeandweb.com/texturepacker/documentation" );
			return;
		}

		dfEditorUtil.DrawSeparator();

		textureFile = EditorGUILayout.ObjectField( "Texture File", textureFile, typeof( Texture2D ), false ) as Texture2D;
		dataFile = EditorGUILayout.ObjectField( "Data File", dataFile, typeof( TextAsset ), false ) as TextAsset;

		if( textureFile != null && dataFile != null )
		{
			if( GUILayout.Button( "Import" ) )
			{
				doImport();
			}
		}

	}

	internal static void Reimport( dfAtlas atlas )
	{

		var textureFilePath = AssetDatabase.GUIDToAssetPath( atlas.imageFileGUID );
		if( !File.Exists( textureFilePath ) )
		{
			Debug.LogError( string.Format( "The image file for atlas {0} could not be found", atlas.name ), atlas );
			return;
		}

		var dataFilePath = AssetDatabase.GUIDToAssetPath( atlas.dataFileGUID );
		if( !File.Exists( dataFilePath ) )
		{
			Debug.LogError( string.Format( "The data file for atlas {0} could not be found", atlas.name ), atlas );
			return;
		}

		var dataFile = AssetDatabase.LoadAssetAtPath( dataFilePath, typeof( TextAsset ) ) as TextAsset;
		if( dataFile == null )
		{
			Debug.LogError( string.Format( "Faile to open the data file for the {0} atlas", atlas.name ), atlas );
			return;
		}

		var textureFile = AssetDatabase.LoadAssetAtPath( textureFilePath, typeof( Texture2D ) ) as Texture2D;
		if( textureFile == null )
		{
			Debug.LogError( string.Format( "Faile to open the image file for the {0} atlas", atlas.name ), atlas );
			return;
		}

		dfTextureAtlasInspector.setAtlasTextureSettings( textureFilePath, false );

		var uvx = 1f / textureFile.width;
		var uvy = 1f / textureFile.height;

		var newSprites = new List<dfAtlas.ItemInfo>();
		var oldSprites = atlas.Items;

		var data = JSON.JsonDecode( dataFile.text ) as DICT;
		var frames = data[ "frames" ] as DICT;
		foreach( var key in frames.Keys )
		{

			var itemData = frames[ key ] as DICT;

			var spriteName = Path.GetFileNameWithoutExtension( key );

			var isRotated = (bool)itemData[ "rotated" ];
			if( isRotated )
			{
				Debug.LogError( string.Format( "Sprite '{0}' is rotated. Rotated sprites are not yet supported", spriteName ) );
				continue;
			}

			var frameRect = extractUVRect( itemData[ "frame" ] as DICT, textureFile );
			var spriteSize = new Vector2( frameRect.width / uvx, frameRect.height / uvy );

			var sprite = new dfAtlas.ItemInfo()
			{
				name = spriteName,
				border = new RectOffset(),
				deleted = false,
				region = frameRect,
				rotated = false,
				sizeInPixels = spriteSize
			};
			newSprites.Add( sprite );

			for( int i = 0; i < oldSprites.Count; i++ )
			{
				var old = oldSprites[ i ];
				if( string.Equals( old.name, spriteName, StringComparison.OrdinalIgnoreCase ) )
				{
					sprite.border = old.border;
					break;
				}
			}

		}

		newSprites.Sort();
		atlas.Items.Clear();
		atlas.Items.AddRange( newSprites );

		EditorUtility.SetDirty( atlas.gameObject );
		EditorUtility.SetDirty( atlas );
		EditorUtility.SetDirty( atlas.Material );
		
		var prefabPath = AssetDatabase.GetAssetPath( atlas );
		var go = atlas.gameObject;

		#region Delay execution of object selection to work around a Unity issue

		// Declared with null value to eliminate "uninitialized variable" 
		// compiler error in lambda below.
		EditorApplication.CallbackFunction callback = null;

		callback = () =>
		{
			EditorUtility.FocusProjectWindow();
			go = AssetDatabase.LoadMainAssetAtPath( prefabPath ) as GameObject;
			Selection.objects = new UnityEngine.Object[] { go };
			EditorGUIUtility.PingObject( go );
			Debug.Log( "Texture Atlas prefab re-imported at " + prefabPath, go );
			EditorApplication.delayCall -= callback;
		};

		EditorApplication.delayCall += callback;

		#endregion

	}

	private void doImport()
	{

		try
		{

			var texturePath = AssetDatabase.GetAssetPath( textureFile );
			//var textureGUID = AssetDatabase.AssetPathToGUID( texturePath );

			var uvx = 1f / textureFile.width;
			var uvy = 1f / textureFile.height;

			var sprites = new List<dfAtlas.ItemInfo>();
			
			var data = JSON.JsonDecode( dataFile.text ) as DICT;
			var frames = data[ "frames" ] as DICT;
			foreach( var key in frames.Keys )
			{

				var itemData = frames[ key ] as DICT;

				var spriteName = Path.GetFileNameWithoutExtension( key );

				var isRotated = (bool)itemData[ "rotated" ];
				if( isRotated )
				{
					Debug.LogError( string.Format( "Sprite '{0}' is rotated. Rotated sprites are not yet supported", spriteName ) );
					continue;
				}

				var frameRect = extractUVRect( itemData[ "frame" ] as DICT, textureFile );
				var spriteSize = new Vector2( frameRect.width / uvx, frameRect.height / uvy );

				var sprite = new dfAtlas.ItemInfo()
				{
					name = spriteName,
					border = new RectOffset(),
					deleted = false,
					region = frameRect,
					rotated = false,
					sizeInPixels = spriteSize
				};
				sprites.Add( sprite );
				
			}

			sprites.Sort();

			var dataFilePath = AssetDatabase.GetAssetPath( dataFile );
			var defaultFilename = Path.GetFileNameWithoutExtension( dataFilePath );

			var saveFolder = Path.GetDirectoryName( texturePath );
			var prefabPath = EditorUtility.SaveFilePanel( "Import Texture Atlas", saveFolder, defaultFilename, "prefab" );
			if( string.IsNullOrEmpty( prefabPath ) )
				return;

			prefabPath = prefabPath.MakeRelativePath();

			dfTextureAtlasInspector.setAtlasTextureSettings( texturePath, true );

			var texture = AssetDatabase.LoadAssetAtPath( texturePath, typeof( Texture2D ) ) as Texture2D;
			if( texture == null )
				Debug.LogError( "Failed to find texture at " + texturePath );

			var shader = Shader.Find( "Daikon Forge/Default UI Shader" );
			var atlasMaterial = new Material( shader );
			atlasMaterial.mainTexture = texture;
			AssetDatabase.CreateAsset( atlasMaterial, Path.ChangeExtension( texturePath, "mat" ) );

			var go = new GameObject() { name = Path.GetFileNameWithoutExtension( prefabPath ) };
			var atlas = go.AddComponent<dfAtlas>();
			atlas.Material = atlasMaterial;
			atlas.AddItems( sprites );
			atlas.generator = dfAtlas.TextureAtlasGenerator.TexturePacker;
			atlas.imageFileGUID = AssetDatabase.AssetPathToGUID( texturePath );
			atlas.dataFileGUID = AssetDatabase.AssetPathToGUID( dataFilePath );

			var prefab = PrefabUtility.CreateEmptyPrefab( prefabPath );
			prefab.name = atlas.name;
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
				Selection.objects = new UnityEngine.Object[] { go };
				EditorGUIUtility.PingObject( go );
				Debug.Log( "Texture Atlas prefab created at " + prefabPath, prefab );
				EditorApplication.delayCall -= callback;
			};

			EditorApplication.delayCall += callback;

			#endregion

		}
		catch( Exception err )
		{
			EditorUtility.DisplayDialog( "Failed to parse data file", "Unable to parse the data file: " + err.Message, "CANCEL" );
		}

	}

	private static Rect extractUVRect( DICT data, Texture2D atlas )
	{

		var uvx = 1f / (float)atlas.width;
		var uvy = 1f / (float)atlas.height;

		var w = (float)(double)data[ "w" ];
		var h = (float)(double)data[ "h" ];
		var x = (float)(double)data[ "x" ];
		var y = (float)(double)data[ "y" ] + h;

		return new Rect( x * uvx, 1f - y * uvy, w * uvx, h * uvy );

	}

	#region Nested classes

	// JSON class modified from the source at http://techblog.procurios.nl/k/618/news/view/14605/14863/how-do-i-write-my-own-parser-(for-json).html
	// Removed serialization, changed List<object> and Dictionary data types 

	/// <summary>
	/// This class encodes and decodes JSON strings.
	/// Spec. details, see http://www.json.org/
	///
	/// JSON uses Arrays and Objects. These correspond here to the datatypes List<object> and DICT.
	/// All numbers are parsed to doubles.
	/// </summary>
	private class JSON
	{

		public const int TOKEN_NONE = 0;
		public const int TOKEN_CURLY_OPEN = 1;
		public const int TOKEN_CURLY_CLOSE = 2;
		public const int TOKEN_SQUARED_OPEN = 3;
		public const int TOKEN_SQUARED_CLOSE = 4;
		public const int TOKEN_COLON = 5;
		public const int TOKEN_COMMA = 6;
		public const int TOKEN_STRING = 7;
		public const int TOKEN_NUMBER = 8;
		public const int TOKEN_TRUE = 9;
		public const int TOKEN_FALSE = 10;
		public const int TOKEN_NULL = 11;

		private const int BUILDER_CAPACITY = 2000;

		/// <summary>
		/// Parses the string json into a value
		/// </summary>
		/// <param name="json">A JSON string.</param>
		/// <returns>An List<object>, a DICT, a double, a string, null, true, or false</returns>
		public static object JsonDecode( string json )
		{
			bool success = true;

			return JsonDecode( json, ref success );
		}

		/// <summary>
		/// Parses the string json into a value; and fills 'success' with the successfullness of the parse.
		/// </summary>
		/// <param name="json">A JSON string.</param>
		/// <param name="success">Successful parse?</param>
		/// <returns>An List<object>, a DICT, a double, a string, null, true, or false</returns>
		public static object JsonDecode( string json, ref bool success )
		{
			success = true;
			if( json != null )
			{
				char[] charArray = json.ToCharArray();
				int index = 0;
				object value = ParseValue( charArray, ref index, ref success );
				return value;
			}
			else
			{
				return null;
			}
		}

		protected static DICT ParseObject( char[] json, ref int index, ref bool success )
		{

			DICT table = new DICT();
			int token;

			// {
			NextToken( json, ref index );

			bool done = false;
			while( !done )
			{
				token = LookAhead( json, index );
				if( token == JSON.TOKEN_NONE )
				{
					success = false;
					return null;
				}
				else if( token == JSON.TOKEN_COMMA )
				{
					NextToken( json, ref index );
				}
				else if( token == JSON.TOKEN_CURLY_CLOSE )
				{
					NextToken( json, ref index );
					return table;
				}
				else
				{

					// name
					string name = ParseString( json, ref index, ref success );
					if( !success )
					{
						success = false;
						return null;
					}

					// :
					token = NextToken( json, ref index );
					if( token != JSON.TOKEN_COLON )
					{
						success = false;
						return null;
					}

					// value
					object value = ParseValue( json, ref index, ref success );
					if( !success )
					{
						success = false;
						return null;
					}

					table[ name ] = value;
				}
			}

			return table;
		}

		protected static List<object> ParseArray( char[] json, ref int index, ref bool success )
		{
			List<object> array = new List<object>();

			// [
			NextToken( json, ref index );

			bool done = false;
			while( !done )
			{
				int token = LookAhead( json, index );
				if( token == JSON.TOKEN_NONE )
				{
					success = false;
					return null;
				}
				else if( token == JSON.TOKEN_COMMA )
				{
					NextToken( json, ref index );
				}
				else if( token == JSON.TOKEN_SQUARED_CLOSE )
				{
					NextToken( json, ref index );
					break;
				}
				else
				{
					object value = ParseValue( json, ref index, ref success );
					if( !success )
					{
						return null;
					}

					array.Add( value );
				}
			}

			return array;
		}

		protected static object ParseValue( char[] json, ref int index, ref bool success )
		{
			switch( LookAhead( json, index ) )
			{
				case JSON.TOKEN_STRING:
					return ParseString( json, ref index, ref success );
				case JSON.TOKEN_NUMBER:
					return ParseNumber( json, ref index, ref success );
				case JSON.TOKEN_CURLY_OPEN:
					return ParseObject( json, ref index, ref success );
				case JSON.TOKEN_SQUARED_OPEN:
					return ParseArray( json, ref index, ref success );
				case JSON.TOKEN_TRUE:
					NextToken( json, ref index );
					return true;
				case JSON.TOKEN_FALSE:
					NextToken( json, ref index );
					return false;
				case JSON.TOKEN_NULL:
					NextToken( json, ref index );
					return null;
				case JSON.TOKEN_NONE:
					break;
			}

			success = false;
			return null;
		}

		protected static string ParseString( char[] json, ref int index, ref bool success )
		{
			StringBuilder s = new StringBuilder( BUILDER_CAPACITY );
			char c;

			EatWhitespace( json, ref index );

			// "
			c = json[ index++ ];

			bool complete = false;
			while( !complete )
			{

				if( index == json.Length )
				{
					break;
				}

				c = json[ index++ ];
				if( c == '"' )
				{
					complete = true;
					break;
				}
				else if( c == '\\' )
				{

					if( index == json.Length )
					{
						break;
					}
					c = json[ index++ ];
					if( c == '"' )
					{
						s.Append( '"' );
					}
					else if( c == '\\' )
					{
						s.Append( '\\' );
					}
					else if( c == '/' )
					{
						s.Append( '/' );
					}
					else if( c == 'b' )
					{
						s.Append( '\b' );
					}
					else if( c == 'f' )
					{
						s.Append( '\f' );
					}
					else if( c == 'n' )
					{
						s.Append( '\n' );
					}
					else if( c == 'r' )
					{
						s.Append( '\r' );
					}
					else if( c == 't' )
					{
						s.Append( '\t' );
					}
					else if( c == 'u' )
					{
						int remainingLength = json.Length - index;
						if( remainingLength >= 4 )
						{
							// parse the 32 bit hex into an integer codepoint
							uint codePoint;
							if( !( success = UInt32.TryParse( new string( json, index, 4 ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint ) ) )
							{
								return "";
							}
							// convert the integer codepoint to a unicode char and add to string
							s.Append( Char.ConvertFromUtf32( (int)codePoint ) );
							// skip 4 chars
							index += 4;
						}
						else
						{
							break;
						}
					}

				}
				else
				{
					s.Append( c );
				}

			}

			if( !complete )
			{
				success = false;
				return null;
			}

			return s.ToString();
		}

		protected static double ParseNumber( char[] json, ref int index, ref bool success )
		{
			EatWhitespace( json, ref index );

			int lastIndex = GetLastIndexOfNumber( json, index );
			int charLength = ( lastIndex - index ) + 1;

			double number;
			success = Double.TryParse( new string( json, index, charLength ), NumberStyles.Any, CultureInfo.InvariantCulture, out number );

			index = lastIndex + 1;
			return number;
		}

		protected static int GetLastIndexOfNumber( char[] json, int index )
		{
			int lastIndex;

			for( lastIndex = index; lastIndex < json.Length; lastIndex++ )
			{
				if( "0123456789+-.eE".IndexOf( json[ lastIndex ] ) == -1 )
				{
					break;
				}
			}
			return lastIndex - 1;
		}

		protected static void EatWhitespace( char[] json, ref int index )
		{
			for( ; index < json.Length; index++ )
			{
				if( " \t\n\r".IndexOf( json[ index ] ) == -1 )
				{
					break;
				}
			}
		}

		protected static int LookAhead( char[] json, int index )
		{
			int saveIndex = index;
			return NextToken( json, ref saveIndex );
		}

		protected static int NextToken( char[] json, ref int index )
		{
			EatWhitespace( json, ref index );

			if( index == json.Length )
			{
				return JSON.TOKEN_NONE;
			}

			char c = json[ index ];
			index++;
			switch( c )
			{
				case '{':
					return JSON.TOKEN_CURLY_OPEN;
				case '}':
					return JSON.TOKEN_CURLY_CLOSE;
				case '[':
					return JSON.TOKEN_SQUARED_OPEN;
				case ']':
					return JSON.TOKEN_SQUARED_CLOSE;
				case ',':
					return JSON.TOKEN_COMMA;
				case '"':
					return JSON.TOKEN_STRING;
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '-':
					return JSON.TOKEN_NUMBER;
				case ':':
					return JSON.TOKEN_COLON;
			}
			index--;

			int remainingLength = json.Length - index;

			// false
			if( remainingLength >= 5 )
			{
				if( json[ index ] == 'f' &&
					json[ index + 1 ] == 'a' &&
					json[ index + 2 ] == 'l' &&
					json[ index + 3 ] == 's' &&
					json[ index + 4 ] == 'e' )
				{
					index += 5;
					return JSON.TOKEN_FALSE;
				}
			}

			// true
			if( remainingLength >= 4 )
			{
				if( json[ index ] == 't' &&
					json[ index + 1 ] == 'r' &&
					json[ index + 2 ] == 'u' &&
					json[ index + 3 ] == 'e' )
				{
					index += 4;
					return JSON.TOKEN_TRUE;
				}
			}

			// null
			if( remainingLength >= 4 )
			{
				if( json[ index ] == 'n' &&
					json[ index + 1 ] == 'u' &&
					json[ index + 2 ] == 'l' &&
					json[ index + 3 ] == 'l' )
				{
					index += 4;
					return JSON.TOKEN_NULL;
				}
			}

			return JSON.TOKEN_NONE;
		}

	}

	#endregion

}
