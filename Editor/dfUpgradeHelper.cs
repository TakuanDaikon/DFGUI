// *******************************************************
// Copyright 2013-2014 Daikon Forge, all rights reserved under 
// US Copyright Law and international treaties
// *******************************************************
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

public class dfInstallPostProcessor : AssetPostprocessor
{

	private const string VERSION_KEY = "DaikonForge.UserInterface.InstalledVersion";
	private const string CURRENT_VERSION = "1.0.16 hf1";

	static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
	{

		var lastVersion = EditorPrefs.GetString( VERSION_KEY, "" );
		if( lastVersion != CURRENT_VERSION )
		{

			EditorApplication.delayCall += ShowWelcomeDialog;

			Debug.Log( "New version of Daikon Forge GUI installed: " + CURRENT_VERSION );
			EditorPrefs.SetString( VERSION_KEY, CURRENT_VERSION );

		}

	}

	private static void ShowWelcomeDialog()
	{

		EditorApplication.delayCall -= ShowWelcomeDialog;

		dfWelcomeDialog.ShowWelcomeMessage();

	}

}

[InitializeOnLoad]
public class dfUpgradeHelper
{

	private static List<GameObject> allPrefabsInProject = null;

	// Slowly migrating menu option locations, will remove older ones as 
	// users become used to the new locations
	[MenuItem( "Tools/Daikon Forge/Texture Atlas/Upgrade Atlas Prefabs" )]
	public static void UpgradeAtlases()
	{

		var message = "This option allows you to upgrade a Texture Atlas created with DF-GUI v1.0.8 or earlier to the latest version. It is not needed if your texture atlases were created with v1.0.10 or later.";
		if( !EditorUtility.DisplayDialog( "Upgrade Prefabs", message, "OK", "CANCEL" ) )
			return;

		var atlases = findPrefabsOfType<dfAtlas>();

		EditorUtility.DisplayProgressBar( "Upgrade", "Upgrading atlases", 0 );

		var count = atlases.Count;
		for( int i = 0; i < count; i++ )
		{

			var atlas = atlases[ i ];

			EditorUtility.DisplayProgressBar( "Upgrade", "Upgrading atlas: " + atlas.name, (float)i / (float)count );
			Debug.Log( "Upgrading Texture Atlas: " + atlas.name, atlas );

			UpgradeAtlas( atlas );

		}

		EditorUtility.ClearProgressBar();

	}

	public static void UpgradeAtlas( dfAtlas atlas )
	{

		try
		{

			var sprites = atlas.Items;
			for( int i = 0; i < sprites.Count; i++ )
			{

				var sprite = sprites[ i ];

				if( sprite.texture != null )
				{

					var spritePath = AssetDatabase.GetAssetPath( sprite.texture );
					var guid = AssetDatabase.AssetPathToGUID( spritePath );

					sprite.sizeInPixels = new Vector2( sprite.texture.width, sprite.texture.height );
					sprite.textureGUID = guid;
					sprite.texture = null;

				}
				else if( !string.IsNullOrEmpty( sprite.textureGUID ) )
				{

					var path = AssetDatabase.GUIDToAssetPath( sprite.textureGUID );
					var texture = AssetDatabase.LoadAssetAtPath( path, typeof( Texture2D ) ) as Texture2D;

					sprite.sizeInPixels = new Vector2( texture.width, texture.height );

				}

			}

			EditorUtility.SetDirty( atlas );

		}
		catch( Exception err )
		{
			Debug.LogError( "Error upgrading atlas " + atlas.name + ": " + err.Message, atlas );
		}

	}

	private static List<T> findPrefabsOfType<T>() where T : MonoBehaviour
	{

		if( allPrefabsInProject == null )
		{

			allPrefabsInProject = new List<GameObject>();

			var progressTime = Environment.TickCount;

			var allAssetPaths = AssetDatabase.GetAllAssetPaths();
			for( int i = 0; i < allAssetPaths.Length; i++ )
			{

				if( Environment.TickCount - progressTime > 250 )
				{
					progressTime = Environment.TickCount;
					EditorUtility.DisplayProgressBar( "Daikon Forge GUI", "Loading prefabs", (float)i / (float)allAssetPaths.Length );
				}

				var path = allAssetPaths[ i ];
				if( !path.EndsWith( ".prefab", StringComparison.OrdinalIgnoreCase ) )
					continue;

				var gameObject = AssetDatabase.LoadMainAssetAtPath( path ) as GameObject;
				if( IsPrefab( gameObject ) )
				{
					allPrefabsInProject.Add( gameObject );
				}

			}

			EditorUtility.ClearProgressBar();

			allPrefabsInProject.Sort( ( GameObject lhs, GameObject rhs ) =>
			{
				return lhs.name.CompareTo( rhs.name );
			} );

		}

		var result = new List<T>();

		foreach( var item in allPrefabsInProject )
		{

			var component = item.GetComponent( typeof( T ) );
			if( component != null )
				result.Add( (T)component );

		}

		return result;

	}

	private static bool IsPrefab( GameObject item )
	{
		return
			item != null &&
			PrefabUtility.GetPrefabParent( item ) == null &&
			PrefabUtility.GetPrefabObject( item ) != null;
	}

}

