/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Object = UnityEngine.Object;

[CustomEditor( typeof( dfAtlas ) )]
public class dfTextureAtlasInspector : Editor
{

	#region Public static properties

	public static string SelectedSprite { get; set; }

	#endregion

	#region Private fields

	private Dictionary<string, bool> selectedTextures = new Dictionary<string, bool>();
	private static Texture2D lineTex;

	#endregion

	#region Atlas creation
	
	//
	// NOTE: If you want to be able to copy sprite border information between atlases, uncomment the following
	// function, will will expose a "Copy Sprite Borders" option when right-clicking the atlas in the project view.
	//
	//[MenuItem( "Assets/Daikon Forge/Texture Atlas/Copy Sprite Borders", false, 1 )]
	//public static void CopySpriteBorders()
	//{

	//	var selection = Selection
	//		.GetFiltered( typeof( dfAtlas ), SelectionMode.Assets )
	//		.Cast<dfAtlas>()
	//		.ToArray();

	//	if( selection == null || selection.Length == 0 )
	//	{
	//		EditorUtility.DisplayDialog( "Copy Sprite Borders", "You must select the source Atlas first", "OK" );
	//		return;
	//	}

	//	selectTextureAtlas( "Select Target Atlas", ( targetAtlas ) =>
	//	{

	//		var sourceAtlas = selection[ 0 ];

	//		for( int i = 0; i < sourceAtlas.Items.Count; i++ )
	//		{

	//			var sourceSprite = sourceAtlas.Items[ i ];
	//			var targetSprite = targetAtlas[ sourceSprite.name ];

	//			if( targetSprite != null )
	//			{
	//				Debug.Log( "Copying border: " + sourceSprite.name + ", " + sourceSprite.border );
	//				targetSprite.border = sourceSprite.border;
	//			}

	//		}

	//		dfEditorUtil.MarkUndo( targetAtlas, "Copy sprite borders" );

	//		#region Delay execution of object selection to work around a Unity issue

	//		// Declared with null value to eliminate "uninitialized variable" 
	//		// compiler error in lambda below.
	//		EditorApplication.CallbackFunction callback = null;

	//		callback = () =>
	//		{
	//			EditorUtility.FocusProjectWindow();
	//			var go = targetAtlas.gameObject;
	//			Selection.objects = new Object[] { go };
	//			EditorGUIUtility.PingObject( go );
	//			EditorApplication.delayCall -= callback;
	//		};

	//		EditorApplication.delayCall += callback;

	//		#endregion


	//	} );

	//}

	// Slowly migrating menu option locations, will remove older ones as 
	// users become used to the new locations
	[MenuItem( "Tools/Daikon Forge/Texture Atlas/Create New Atlas")]
	[MenuItem( "Assets/Daikon Forge/Texture Atlas/Create New Atlas", false, 0 )]
	public static void CreateAtlasFromSelection()
	{

		try
		{

			EditorUtility.DisplayProgressBar( "Creating Texture Atlas", "Adding selected textures to the Texture Atlas", 0 );

			var selection = Selection
				.GetFiltered( typeof( Texture2D ), SelectionMode.Assets )
				.Cast<Texture2D>()
				.Where( t => isReadable( t ) )
				.OrderByDescending( t => t.width * t.height )
				.ToArray();

			if( selection.Length == 0 )
			{
				EditorUtility.DisplayDialog( "Create Texture Atlas", "Either no textures selected or none of the selected textures has Read/Write enabled", "OK" );
				return;
			}

			var saveFolder = Path.GetDirectoryName( AssetDatabase.GetAssetPath( selection[ 0 ] ) );
			var prefabPath = EditorUtility.SaveFilePanel( "Create Texture Atlas", saveFolder, "Texture Atlas", "prefab" );
			if( string.IsNullOrEmpty( prefabPath ) )
				return;

			prefabPath = prefabPath.MakeRelativePath();

			var padding = EditorPrefs.GetInt( "DaikonForge.AtlasDefaultPadding", 2 );

			var texture = new Texture2D( 1, 1, TextureFormat.ARGB32, false );
			var rects = texture.PackTextures2( selection, padding, dfTextureAtlasInspector.MaxAtlasSize, dfTextureAtlasInspector.ForceSquare );

			var texturePath = Path.ChangeExtension( prefabPath, "png" );
			byte[] bytes = texture.EncodeToPNG();
			System.IO.File.WriteAllBytes( texturePath, bytes );
			bytes = null;
			DestroyImmediate( texture );

			setAtlasTextureSettings( texturePath, true );

			texture = AssetDatabase.LoadAssetAtPath( texturePath, typeof( Texture2D ) ) as Texture2D;
			if( texture == null )
				Debug.LogError( "Failed to find texture at " + texturePath );

			var sprites = new List<dfAtlas.ItemInfo>();
			for( int i = 0; i < rects.Length; i++ )
			{

				var pixelCoords = rects[ i ];
				var size = new Vector2( selection[ i ].width, selection[ i ].height );

				var spritePath = AssetDatabase.GetAssetPath( selection[ i ] );
				var guid = AssetDatabase.AssetPathToGUID( spritePath );

				var item = new dfAtlas.ItemInfo()
				{
					name = selection[ i ].name,
					region = pixelCoords,
					rotated = false,
					textureGUID = guid,
					sizeInPixels = size
				};

				sprites.Add( item );

			}

			sprites.Sort();

			var shader = Shader.Find( "Daikon Forge/Default UI Shader" );
			var atlasMaterial = new Material( shader );
			atlasMaterial.mainTexture = texture;
			AssetDatabase.CreateAsset( atlasMaterial, Path.ChangeExtension( texturePath, "mat" ) );

			var go = new GameObject() { name = Path.GetFileNameWithoutExtension( prefabPath ) };
			var atlas = go.AddComponent<dfAtlas>();
			atlas.Material = atlasMaterial;
			atlas.AddItems( sprites );

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
				Selection.objects = new Object[] { go };
				EditorGUIUtility.PingObject( go );
				Debug.Log( "Texture Atlas prefab created at " + prefabPath, prefab );
				EditorApplication.delayCall -= callback;
			};

			EditorApplication.delayCall += callback;

			#endregion

		}
		catch( Exception err )
		{
			Debug.LogError( err.ToString() );
			EditorUtility.DisplayDialog( "Error Creating Texture Atlas", err.Message, "OK" );
		}
		finally
		{
			EditorUtility.ClearProgressBar();
		}

	}

	internal static bool rebuildAtlas( dfAtlas atlas )
	{

		try
		{

			EditorUtility.DisplayProgressBar( "Rebuilding Texture Atlas", "Processing changes to the texture atlas...", 0 );

			var sprites = atlas.Items
				.Where( i => i != null && !i.deleted )
				.Select( i => new { source = i, texture = getTexture( i.textureGUID ) } )
				.Where( i => i.texture != null )
				.OrderByDescending( i => i.texture.width * i.texture.height )
				.ToList();

			var textures = sprites.Select( i => i.texture ).ToList();

			var oldAtlasTexture = atlas.Material.mainTexture;
			var texturePath = AssetDatabase.GetAssetPath( oldAtlasTexture );

			var padding = EditorPrefs.GetInt( "DaikonForge.AtlasDefaultPadding", 2 );

			var newAtlasTexture = new Texture2D( 0, 0, TextureFormat.RGBA32, false );
			var newRects = newAtlasTexture.PackTextures2( textures.ToArray(), padding, dfTextureAtlasInspector.MaxAtlasSize, dfTextureAtlasInspector.ForceSquare );

			byte[] bytes = newAtlasTexture.EncodeToPNG();
			System.IO.File.WriteAllBytes( texturePath, bytes );
			bytes = null;
			DestroyImmediate( newAtlasTexture );

			setAtlasTextureSettings( texturePath, false );

			// Fix up the new sprite locations
			for( int i = 0; i < sprites.Count; i++ )
			{
				sprites[ i ].source.region = newRects[ i ];
				sprites[ i ].source.sizeInPixels = new Vector2( textures[ i ].width, textures[ i ].height );
				sprites[ i ].source.texture = null;
			}

			// Remove any deleted sprites
			atlas.Items.RemoveAll( i => i.deleted );

			// Re-sort the Items collection
			atlas.Items.Sort();
			atlas.RebuildIndexes();

			EditorUtility.SetDirty( atlas.gameObject );
			EditorUtility.SetDirty( atlas );
			EditorUtility.SetDirty( atlas.Material );

			dfGUIManager.RefreshAll( true );

			return true;

		}
		catch( Exception err )
		{

			Debug.LogError( err.ToString(), atlas );
			EditorUtility.DisplayDialog( "Error Rebuilding Texture Atlas", err.Message, "OK" );

			return false;

		}
		finally
		{
			EditorUtility.ClearProgressBar();
		}

	}

	private static Texture2D getTexture( string guid )
	{
		var path = AssetDatabase.GUIDToAssetPath( guid );
		return AssetDatabase.LoadAssetAtPath( path, typeof( Texture2D ) ) as Texture2D;
	}

	public void RemoveSprite( dfAtlas atlas, string spriteName )
	{
		selectedTextures.Clear();
		atlas[ spriteName ].deleted = true;
		rebuildAtlas( atlas );
	}

	public bool AddTexture( dfAtlas atlas, params Texture2D[] newTextures )
	{

		try
		{

			selectedTextures.Clear();

			var addedItems = new List<dfAtlas.ItemInfo>();

			for( int i = 0; i < newTextures.Length; i++ )
			{

				// Grab reference to existing item, if it exists, to preserve border information
				var existingItem = atlas[ newTextures[ i ].name ];

				// Remove the existing item if it already exists
				atlas.Remove( newTextures[ i ].name );

				// Keep the texture size available
				var size = new Vector2( newTextures[ i ].width, newTextures[ i ].height );

				// Determine the guid for the texture
				var spritePath = AssetDatabase.GetAssetPath( newTextures[ i ] );
				var guid = AssetDatabase.AssetPathToGUID( spritePath );

				// Add the new texture to the Items collection
				var newItem = new dfAtlas.ItemInfo()
				{
					textureGUID = guid,
					name = newTextures[ i ].name,
					border = ( existingItem != null ) ? existingItem.border : new RectOffset(),
					sizeInPixels = size
				};
				addedItems.Add( newItem );
				atlas.AddItem( newItem );

			}

			if( !rebuildAtlas( atlas ) )
			{
				atlas.Items.RemoveAll( i => addedItems.Contains( i ) );
				return false;
			}

			return true;

		}
		catch( Exception err )
		{
			Debug.LogError( err.ToString(), atlas );
			EditorUtility.DisplayDialog( "Error Adding Sprite", err.Message, "OK" );
		}

		return false;

	}

	private static bool isReadable( Texture2D texture )
	{

		try
		{

			var path = AssetDatabase.GetAssetPath( texture );
			var importer = AssetImporter.GetAtPath( path ) as TextureImporter;
			if( importer == null || !importer.isReadable )
			{
				return false;
			}

		}
		catch { return false; }

		return true;

	}

	internal static void setAtlasTextureSettings( string path, bool createMode )
	{

		AssetDatabase.Refresh();

		var importer = AssetImporter.GetAtPath( path ) as TextureImporter;
		if( importer == null )
		{
			Debug.LogError( "Failed to obtain import settings for texture: " + path );
		}

		importer.ClearPlatformTextureSettings( "iPhone" );

		var settings = new TextureImporterSettings();

		importer.ReadTextureSettings( settings );
		settings.mipmapEnabled = false;
		settings.readable = true;
		settings.maxTextureSize = 4096;
		settings.textureFormat = TextureImporterFormat.AutomaticTruecolor;
		settings.wrapMode = TextureWrapMode.Clamp;
		settings.npotScale = TextureImporterNPOTScale.None;

		if( createMode )
		{
			settings.filterMode = FilterMode.Bilinear;
			settings.alphaIsTransparency = true;
			settings.linearTexture = true;
		}

		importer.SetTextureSettings( settings );

		AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceUpdate );

	}

	#endregion

	private static void selectTextureAtlas( string title, Action<dfAtlas> callback )
	{

		var savedColor = GUI.color;

		try
		{

			dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
			{
				var newAtlas = ( item == null ) ? null : item.GetComponent<dfAtlas>();
				if( newAtlas != null )
				{
					callback( newAtlas );
				}
			};

			dfEditorUtil.DelayedInvoke( (System.Action)( () =>
			{
				var dialog = dfPrefabSelectionDialog.Show( title, typeof( dfAtlas ), selectionCallback, dfTextureAtlasInspector.DrawAtlasPreview, null );
				dialog.previewSize = 200;
			} ) );

		}
		finally
		{
			GUI.enabled = true;
			GUI.color = savedColor;
		}

	}

	public static int MaxAtlasSize
	{
		get { return EditorPrefs.GetInt( "dfAtlas.MaxAtlasSize", 4096 ); }
		set { EditorPrefs.SetInt( "dfAtlas.MaxAtlasSize", value ); }
	}

	public static bool ForceSquare
	{
		get { return EditorPrefs.GetBool( "dfAtlas.ForceSquare", false ); }
		set { EditorPrefs.SetBool( "dfAtlas.ForceSquare", value ); }
	}

	public override void OnInspectorGUI()
	{

		var atlas = target as dfAtlas;

		if( atlas.Items.Any( i => i.texture != null ) )
		{

			var upgradeMessage = "This Texture Atlas is an older unsupported format and must be upgraded before use.";
			EditorGUILayout.HelpBox( upgradeMessage, MessageType.Error );

			if( GUILayout.Button( "Upgrade Atlases" ) )
			{
				dfUpgradeHelper.UpgradeAtlases();
			}

			return;

		}

		var atlasInfo = string.Format(
			"Texture Atlas: {0}\nSprites: {1}\nTexture: {2}\nFormat: {3}\n{4}\n",
			atlas.name,
			atlas.Items.Count,
			atlas.Texture != null ? string.Format( "{0}x{1}", atlas.Texture.width, atlas.Texture.height ) : "[none]",
			atlas.Texture.format,
			atlas.generator == dfAtlas.TextureAtlasGenerator.Internal ? "Generator: Internal" : "Generator: Imported"
		);

		GUILayout.Label( atlasInfo );

		EditAtlasOptions( atlas );
		ShowAtlasActions( atlas );

		if( atlas.generator == dfAtlas.TextureAtlasGenerator.Internal )
		{
			ShowAddTextureOption( atlas );
			ShowModifiedTextures( atlas );
		}

		using( dfEditorUtil.BeginGroup( "Edit Sprite" ) )
		{

			dfEditorUtil.LabelWidth = 94f;

			EditSprite( "Edit Sprite" );

			var sprite = atlas[ SelectedSprite ];
			if( sprite == null )
			{
				showSprites( atlas );
				return;
			}

			//var spriteName = EditorGUILayout.TextField( "Name", sprite.name );
			//if( spriteName != sprite.name )
			//{
			//    dfEditorUtil.MarkUndo( target, "Change sprite name" );
			//    sprite.name = spriteName;
			//}

			EditorGUILayout.BeginHorizontal();
			{

				if( GUILayout.Button( "Back to Atlas" ) )
				{
					SelectedSprite = null;
				}

				if( atlas.generator == dfAtlas.TextureAtlasGenerator.Internal && GUILayout.Button( "Remove Sprite" ) )
				{
					if( EditorUtility.DisplayDialog( "Remove Sprite", "Are you sure you want to remove " + sprite.name + " from the Atlas?", "Yes", "No" ) )
					{
						dfEditorUtil.MarkUndo( atlas, "Remove Sprite" );
						RemoveSprite( atlas, SelectedSprite );
						EditorUtility.DisplayDialog( "Sprite Removed", SelectedSprite + " has been removed", "Ok" );
						SelectedSprite = "";
						dfGUIManager.RefreshAll();
					}
				}

			}
			EditorGUILayout.EndHorizontal();

			var atlasTexture = atlas.Material.mainTexture;
			var atlasWidth = atlasTexture.width;
			var atlasHeight = atlasTexture.height;
			var width = sprite.sizeInPixels.x;
			var height = sprite.sizeInPixels.y;

			var location = new Vector2( sprite.region.x * atlasWidth, atlasHeight - sprite.region.y * atlasHeight );
			var size = new Vector2( width, height );

			dfEditorUtil.DrawHorzLine();
			EditInt2( "Location", "Left", "Top", location, 90, false );

			dfEditorUtil.DrawHorzLine();
			EditInt2( "Size", "Width", "Height", size, 90, false );

			dfEditorUtil.DrawHorzLine();
			var borders = EditRectOffset( "Slices", "Left", "Top", "Right", "Bottom", sprite.border, 90 );
			if( !borders.Equals( sprite.border ) )
			{
				dfEditorUtil.MarkUndo( target, "Change sprite borders" );
				sprite.border = borders;
			}

			if( GUILayout.Button( "Refresh Views" ) )
			{
				dfGUIManager.RefreshAll( true );
			}

		}

	}

	private void EditAtlasOptions( dfAtlas atlas )
	{

		if( atlas.generator == dfAtlas.TextureAtlasGenerator.Internal )
		{

				using( dfEditorUtil.BeginGroup( "Global Options" ) )
				{

					var packingMethodConfig = (dfTexturePacker.dfTexturePackingMethod)EditorPrefs.GetInt( "DaikonForge.AtlasPackingMethod", (int)dfTexturePacker.dfTexturePackingMethod.RectBestAreaFit );
					var packingMethod = (dfTexturePacker.dfTexturePackingMethod)EditorGUILayout.EnumPopup( "Packing Method", packingMethodConfig );
					if( packingMethod != packingMethodConfig )
					{
						EditorPrefs.SetInt( "DaikonForge.AtlasPackingMethod", (int)packingMethod );
					}

					var sizes = new string[] { "256", "512", "1024", "2048", "4096" };
						var defaultIndex = Mathf.Max( 0, getIndex( sizes, dfTextureAtlasInspector.MaxAtlasSize.ToString() ) );
						var selectedIndex = EditorGUILayout.Popup( "Max Size", defaultIndex, sizes );
						dfTextureAtlasInspector.MaxAtlasSize = int.Parse( sizes[ selectedIndex ] );

					var paddingConfig = EditorPrefs.GetInt( "DaikonForge.AtlasDefaultPadding", 2 );
					var padding = Mathf.Max( 0, EditorGUILayout.IntField( "Padding", paddingConfig ) );
					{
						if( padding != paddingConfig )
						{
							EditorPrefs.SetInt( "DaikonForge.AtlasDefaultPadding", padding );
						}
					}

					ForceSquare = EditorGUILayout.Toggle( "Force square", ForceSquare );

					var extrudeSpritesConfig = EditorPrefs.GetBool( "DaikonForge.AtlasExtrudeSprites", false );
					var extrudeSprites = EditorGUILayout.Toggle( "Extrude Edges", extrudeSpritesConfig );
					{
						if( extrudeSprites != extrudeSpritesConfig )
						{
							EditorPrefs.SetBool( "DaikonForge.AtlasExtrudeSprites", extrudeSprites );
						}
					}

				}

		}
		else if( atlas.generator == dfAtlas.TextureAtlasGenerator.TexturePacker )
		{

			using( dfEditorUtil.BeginGroup( "Imported Texture Atlas" ) )
			{

				var dataFilePath = AssetDatabase.GUIDToAssetPath( atlas.dataFileGUID );
				var dataFile = AssetDatabase.LoadAssetAtPath( dataFilePath, typeof( TextAsset ) ) as TextAsset;
				EditorGUILayout.ObjectField( "Image File", atlas.Texture, typeof( Texture2D ), false );
				EditorGUILayout.ObjectField( "Data File", dataFile, typeof( TextAsset ), false );

			}

		}

		using( dfEditorUtil.BeginGroup( "Atlas Options" ) )
		{
			EditAtlasReplacement( atlas );
		}

	}

	private int getIndex( string[] list, string value )
	{
		for( int i = 0; i < list.Length; i++ )
		{
			if( list[ i ] == value )
				return i;
		}
		return -1;
	}

	private void EditAtlasReplacement( dfAtlas atlas )
	{

		var value = atlas.Replacement;

		dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
		{
			var newAtlas = ( item == null ) ? null : item.GetComponent<dfAtlas>();
			dfEditorUtil.MarkUndo( atlas, "Assign replacement Atlas" );
			atlas.Replacement = newAtlas;
		};

		EditorGUILayout.BeginHorizontal();
		{

			EditorGUILayout.LabelField( "Replacement", "", GUILayout.Width( dfEditorUtil.LabelWidth - 6 ) );

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
						var draggedAtlas = draggedObject != null ? draggedObject.GetComponent<dfAtlas>() : null;
						DragAndDrop.visualMode = ( draggedAtlas != null ) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
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
				dfEditorUtil.DelayedInvoke( (System.Action)( () =>
				{
					var dialog = dfPrefabSelectionDialog.Show( "Select Texture Atlas", typeof( dfAtlas ), selectionCallback, dfTextureAtlasInspector.DrawAtlasPreview, null );
					dialog.previewSize = 200;
				} ) );
			}

		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space( 2 );

	}

	private static void ShowAtlasActions( dfAtlas atlas )
	{

		dfEditorUtil.DrawSeparator();

		EditorGUILayout.BeginHorizontal();
		{

			if( atlas.generator == dfAtlas.TextureAtlasGenerator.TexturePacker )
			{
				if( GUILayout.Button( "Reimport" ) )
				{
					dfTexturePackerImporter.Reimport( atlas );
				}
			}
			else if( GUILayout.Button( "Rebuild" ) )
			{
				rebuildAtlas( atlas );
			}

			if( GUILayout.Button( "Refresh Views" ) )
			{
				dfGUIManager.RefreshAll( true );
			}

		}
		EditorGUILayout.EndHorizontal();

	}

	private void showSprites( dfAtlas atlas )
	{

		EditorGUILayout.Separator();

		GUILayout.Label( "Sprites", "HeaderLabel" );
		{

			if( showDeleteSelectedButton( atlas ) )
			{
				return;
			}

			for( int i = 0; i < atlas.Items.Count; i++ )
			{

				var sprite = atlas.Items[ i ];

				dfEditorUtil.DrawSeparator();

				EditorGUILayout.BeginHorizontal();
				{

					bool isSelected = selectedTextures.ContainsKey( sprite.name );
					if( EditorGUILayout.Toggle( isSelected, GUILayout.Width( 25 ) ) )
					{
						selectedTextures[ sprite.name ] = true;
					}
					else
					{
						selectedTextures.Remove( sprite.name );
					}

					var label = string.Format( "{0} ({1} x {2})", sprite.name, (int)sprite.sizeInPixels.x, (int)sprite.sizeInPixels.y );
					GUILayout.Label( label, GUILayout.ExpandWidth( true ) );

				}
				EditorGUILayout.EndHorizontal();

				var removeSprite = false;
				EditorGUILayout.BeginHorizontal();
				{

					if( GUILayout.Button( "Edit", GUILayout.Width( 75 ) ) )
					{
						SelectedSprite = sprite.name;
					}

					if( atlas.generator == dfAtlas.TextureAtlasGenerator.Internal && GUILayout.Button( "Delete", GUILayout.Width( 75 ) ) )
					{
						removeSprite = true;
					}

				}
				EditorGUILayout.EndHorizontal();

				if( removeSprite )
				{
					RemoveSprite( atlas, sprite.name );
					continue;
				}

				var size = 75; // Mathf.Min( 75, Mathf.Max( sprite.sizeInPixels.x, sprite.sizeInPixels.y ) );
				var rect = GUILayoutUtility.GetRect( size, size );

				drawSprite( rect, atlas, sprite );

			}

		}

		showDeleteSelectedButton( atlas );

	}

	private bool showDeleteSelectedButton( dfAtlas atlas )
	{

		if( selectedTextures.Count > 0 )
		{

			dfEditorUtil.DrawHorzLine();

			var buttonLabel = string.Format( "Delete {0} sprites", selectedTextures.Count );
			if( GUILayout.Button( buttonLabel ) )
			{

				foreach( var key in selectedTextures.Keys )
				{
					atlas[ key ].deleted = true;
				}

				rebuildAtlas( atlas );

				selectedTextures.Clear();

				return true;

			}

		}

		return false;

	}

	private void drawSprite( Rect rect, dfAtlas atlas, dfAtlas.ItemInfo sprite )
	{

		var size = sprite.sizeInPixels;
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

		GUI.DrawTextureWithTexCoords( destRect, atlas.Material.mainTexture, sprite.region );

	}

	static public void DrawBox( Rect rect, Color color )
	{
		if( Event.current.type == EventType.Repaint )
		{
			Texture2D tex = EditorGUIUtility.whiteTexture;
			GUI.color = color;
			GUI.DrawTexture( rect, tex );
			GUI.color = Color.white;
		}
	}

	private void ShowModifiedTextures( dfAtlas atlas )
	{

		dfEditorUtil.DrawSeparator();

		var atlasPath = AssetDatabase.GetAssetPath( atlas.Texture );
		if( string.IsNullOrEmpty( atlasPath ) || !File.Exists( atlasPath ) )
		{
			EditorGUILayout.HelpBox( "Could not find the path associated with the Atlas texture", MessageType.Error );
			return;
		}

		var atlasModified = File.GetLastWriteTime( atlasPath );

		var modifiedSprites = new List<dfAtlas.ItemInfo>();

		for( int i = 0; i < atlas.Items.Count; i++ )
		{

			var sprite = atlas.Items[ i ];

			var spriteTexturePath = AssetDatabase.GUIDToAssetPath( sprite.textureGUID );
			if( string.IsNullOrEmpty( spriteTexturePath ) || !File.Exists( spriteTexturePath ) )
				continue;

			var spriteModified = File.GetLastWriteTime( spriteTexturePath );
			if( spriteModified > atlasModified )
			{
				modifiedSprites.Add( sprite );
			}

		}

		if( modifiedSprites.Count == 0 )
			return;

		using( dfEditorUtil.BeginGroup( "Modified Sprites" ) )
		{

			var list = string.Join( "\n\t", modifiedSprites.Select( s => s.name ).ToArray() );
			var message = string.Format( "The following textures have been modified:\n\t{0}", list );

			EditorGUILayout.HelpBox( message, MessageType.Info );

			var performUpdate = GUILayout.Button( "Refresh Modified Sprites" );
			dfEditorUtil.DrawSeparator();

			if( performUpdate )
			{
				rebuildAtlas( atlas );
			}

		}

	}

	private void ShowAddTextureOption( dfAtlas atlas )
	{

		dfEditorUtil.DrawSeparator();

		using( dfEditorUtil.BeginGroup( "Add Sprites" ) )
		{

			EditorGUILayout.HelpBox( "You can drag and drop textures here to add them to the Texture Atlas", MessageType.Info );

			var evt = Event.current;
			if( evt != null )
			{
				Rect dropRect = GUILayoutUtility.GetLastRect();
				if( evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform )
				{
					if( dropRect.Contains( evt.mousePosition ) )
					{
						var draggedTexture = DragAndDrop.objectReferences.FirstOrDefault( x => x is Texture2D );
						DragAndDrop.visualMode = ( draggedTexture != null ) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
						if( evt.type == EventType.DragPerform )
						{
							addSelectedTextures( atlas );
						}
						evt.Use();
					}
				}
			}

		}

	}

	private void addSelectedTextures( dfAtlas atlas )
	{

		var textures = DragAndDrop.objectReferences.Where( x => x is Texture2D ).Cast<Texture2D>().ToList();
		var notReadable = textures.Where( x => !isReadable( x ) ).OrderBy( x => x.name ).Select( x => x.name ).ToArray();
		var readable = textures.Where( x => isReadable( x ) ).OrderBy( x => x.name ).ToArray();

		if( !AddTexture( atlas, readable ) )
		{
			return;
		}

		var message = string.Format( "{0} texture(s) added.", readable.Length );
		if( notReadable.Length > 0 )
		{
			message += "\nThe following textures were not set to Read/Write and could not be added:\n\n\t";
			message += string.Join( "\n\t", notReadable );
		}

		EditorUtility.DisplayDialog( "Add Sprites", message, "OK" );

		SelectedSprite = ( readable.Length > 0 ) ? readable.First().name : "";

	}

	protected Vector2 EditInt2( string groupLabel, string label1, string label2, Vector2 value )
	{
		return EditInt2( groupLabel, label1, label2, value, 95, true );
	}

	protected Vector2 EditInt2( string groupLabel, string label1, string label2, Vector2 value, int labelWidth, bool enabled )
	{

		try
		{

			var retVal = Vector2.zero;

			GUILayout.BeginHorizontal();
			{

				GUILayout.Label( groupLabel, GUILayout.Width( labelWidth ) );

				GUI.enabled = enabled;

				GUILayout.BeginVertical();
				{

					dfEditorUtil.LabelWidth = 50f;

					var x = EditorGUILayout.IntField( label1, Mathf.FloorToInt( value.x ) );
					var y = EditorGUILayout.IntField( label2, Mathf.FloorToInt( value.y ) );

					retVal.x = x;
					retVal.y = y;

				}
				GUILayout.EndVertical();

				GUILayout.FlexibleSpace();

			}
			GUILayout.EndHorizontal();

			dfEditorUtil.LabelWidth = 100f;

			return retVal;

		}
		finally
		{
			GUI.enabled = true;
		}

	}

	protected RectOffset EditRectOffset( string groupLabel, string leftLabel, string topLabel, string rightLabel, string bottomLabel, RectOffset value )
	{
		return EditRectOffset( groupLabel, leftLabel, topLabel, rightLabel, bottomLabel, value, 95 );
	}

	protected RectOffset EditRectOffset( string groupLabel, string leftLabel, string topLabel, string rightLabel, string bottomLabel, RectOffset value, int labelWidth )
	{

		EditorGUI.BeginChangeCheck();

		var retVal = new RectOffset();

		GUILayout.BeginHorizontal();
		{

			GUILayout.Label( groupLabel, GUILayout.Width( labelWidth ) );

			GUILayout.BeginVertical();
			{

				dfEditorUtil.LabelWidth = 50f;

				retVal.left = Mathf.Max( 0, EditorGUILayout.IntField( leftLabel, value != null ? value.left : 0 ) );
				retVal.right = Mathf.Max( 0, EditorGUILayout.IntField( rightLabel, value != null ? value.right : 0 ) );
				retVal.top = Mathf.Max( 0, EditorGUILayout.IntField( topLabel, value != null ? value.top : 0 ) );
				retVal.bottom = Mathf.Max( 0, EditorGUILayout.IntField( bottomLabel, value != null ? value.bottom : 0 ) );

			}
			GUILayout.EndVertical();

			GUILayout.FlexibleSpace();

		}
		GUILayout.EndHorizontal();

		dfEditorUtil.LabelWidth = 100f;

		if( EditorGUI.EndChangeCheck() )
			return retVal;
		else
			return value;

	}

	protected internal void EditSprite( string label )
	{
		EditSprite( label, 90 );
	}

	protected internal void EditSprite( string label, int labelWidth )
	{

		var atlas = target as dfAtlas;
		if( atlas == null )
			return;

		dfSpriteSelectionDialog.SelectionCallback callback = delegate( string spriteName )
		{
			EditorUtility.SetDirty( target );
			SelectedSprite = spriteName;
		};

		var value = SelectedSprite;

		EditorGUILayout.BeginHorizontal();
		{

			GUILayout.Label( label, GUILayout.Width( labelWidth ) );

			var displayText = string.IsNullOrEmpty( value ) ? "[none selected]" : value;
			GUILayout.Label( displayText, "TextField", GUILayout.ExpandWidth( true ) );

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

		var atlas = target as dfAtlas;

		return
			atlas != null &&
			atlas.Texture != null;

	}

	public override void OnPreviewGUI( Rect rect, GUIStyle background )
	{

		var atlas = target as dfAtlas;
		if( atlas == null )
			return;

		var sprite = atlas[ SelectedSprite ];
		if( sprite != null )
			previewSprite( rect );
		else
			previewAtlasTexture( atlas, rect );

		var texture = atlas.Texture;
		string text = string.Format( "Atlas Size: {0}x{1}", texture.width, texture.height );
		EditorGUI.DropShadowLabel( GUILayoutUtility.GetRect( Screen.width, 18f ), text );

	}

	internal static bool DrawAtlasPreview( GameObject item, Rect rect )
	{

		if( item == null )
			return false;

		var atlas = item.GetComponent<dfAtlas>();
		if( atlas == null )
			return false;

		previewAtlasTexture( atlas, rect );

		return true;

	}

	private static void previewAtlasTexture( dfAtlas atlas, Rect rect )
	{

		if( atlas == null )
			return;

		var texture = atlas.Texture;
		if( texture == null )
			return;

		var size = new Vector2( texture.width, texture.height );

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

		GUI.DrawTexture( destRect, texture );

	}

	private void previewSprite( Rect rect )
	{

		var atlas = target as dfAtlas;
		if( atlas == null )
			return;

		var spriteInfo = atlas[ SelectedSprite ];
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

		GUI.DrawTextureWithTexCoords( destRect, atlas.Material.mainTexture, spriteInfo.region );

		var border = spriteInfo.border;
		if( border.horizontal > 0 || border.vertical > 0 )
		{

			var lineColor = Color.white;
			lineColor.a = 0.7f;

			var left = Mathf.Floor( destRect.x + border.left * ( destRect.width / size.x ) );
			DrawLine( left, rect.y, rect.height, true, lineColor );

			var right = Mathf.Ceil( destRect.x + destRect.width - border.right * ( destRect.width / size.x ) );
			DrawLine( right, rect.y, rect.height, true, lineColor );

			var top = Mathf.Floor( destRect.y + border.top * ( destRect.height / size.y ) );
			DrawLine( rect.x, top, rect.width, false, lineColor );

			var bottom = Mathf.Ceil( destRect.y + destRect.height - border.bottom * ( destRect.height / size.y ) );
			DrawLine( rect.x, bottom, rect.width, false, lineColor );

		}

		string text = string.Format( "Sprite Size: {0}x{1}", size.x, size.y );
		EditorGUI.DropShadowLabel( GUILayoutUtility.GetRect( Screen.width, 18f ), text );

	}

	private void DrawLine( float left, float top, float size, bool vert, Color color )
	{

		if( !lineTex )
		{
			lineTex = new Texture2D( 1, 1 ) { hideFlags = HideFlags.HideAndDontSave };
		}

		var saveColor = GUI.color;
		GUI.color = color;

		if( !vert )
			GUI.DrawTexture( new Rect( left, top, size, 1 ), lineTex );
		else
			GUI.DrawTexture( new Rect( left, top, 1, size ), lineTex );

		GUI.color = saveColor;

	}

}
