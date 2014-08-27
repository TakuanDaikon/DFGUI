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

public class dfSpriteSelectionDialog : ScriptableWizard
{

	public delegate void SelectionCallback( string selectedItem );

	#region Private instance variables 

	private dfAtlas atlas;
	private string selectedSprite;
	private SelectionCallback callback;

	private Vector2 currentScrollPos = Vector2.zero;
	private bool focusSearchField = true;
	private string searchFilter = "";
	private bool selectionShown = false;

	#endregion

	#region Static methods 

	public static void Show( string title, dfAtlas atlas, string sprite, SelectionCallback callback )
	{

		if( atlas == null )
			throw new Exception( "No Texture Atlas was specified" );

		// Detect whether the user has deleted the textures after adding them to the Atlas
		if( atlas.Texture == null )
			throw new Exception( "The Texture Atlas does not have a texture or the texture was deleted" );

		var dialog = ScriptableWizard.DisplayWizard<dfSpriteSelectionDialog>( title );
		dialog.atlas = atlas;
		dialog.selectedSprite = sprite;
		dialog.minSize = new Vector2( 300, 200 );
		dialog.callback = callback;
		dialog.selectionShown = string.IsNullOrEmpty( sprite );
		dialog.ShowAuxWindow();

	}

	#endregion

	void OnGUI()
	{

		if( atlas == null )
		{
			this.Close();
			return;
		}

		GUI.BeginGroup( new Rect( 0f, 0f, base.position.width, base.position.height ), GUIContent.none );
		{
			this.SearchArea();
			this.GridArea();
			if( Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape )
			{
				this.Cancel();
			}
		}
		GUI.EndGroup();

		var statusRect = new Rect( 0, base.position.height - 25f, base.position.width, 50f );
		GUI.BeginGroup( statusRect, "", "ObjectPickerToolbar" );
		{
			var test = new Rect( 10, 5, base.position.width - 20, 25 );
			GUI.Label( test, buildItemTooltip( atlas[ selectedSprite ] ) );
		}
		GUI.EndGroup();

		checkForCancel();

	}

	private void checkForCancel()
	{
		if( focusedWindow != this )
		{
			GUIUtility.ExitGUI();
			base.Close();
		}
	}

	private void Cancel()
	{
		base.Close();
		GUI.changed = true;
		GUIUtility.ExitGUI();
	}

	private void GridArea()
	{

		// Don't know why, but when the app is running Unity seems to mess with the
		// GUI color and as a glyphData this dialog looks rather odd
		GUI.color = Color.white;

		var viewportRect = new Rect( 0f, 50, base.position.width, base.position.height - 75 );
		GUI.Box( viewportRect, string.Empty, "ObjectPickerPreviewBackground" );

		var filteredItems =
			!string.IsNullOrEmpty( searchFilter )
			? atlas.Items.Where( i => i.name.IndexOf( searchFilter, StringComparison.OrdinalIgnoreCase ) != -1 ).ToList()
			: atlas.Items;

		var previewSize = 100f;
		var padding = 10f;
		var labelHeight = 18f;
		var viewWidth = viewportRect.width - 20f;
		var maxColumns = Mathf.Max( Mathf.FloorToInt( viewWidth / ( previewSize + padding ) ), 1 );
		var numItemsDisplayed = filteredItems.Count + 1;
		var rows = numItemsDisplayed / maxColumns;
		if( numItemsDisplayed % maxColumns > 0 ) rows += 1;

		var scrollRect = new Rect( 0, 50, viewWidth, rows * ( previewSize + padding + labelHeight ) );
		var previewRect = new Rect( padding, 53, previewSize, previewSize );

		currentScrollPos = GUI.BeginScrollView( viewportRect, currentScrollPos, scrollRect );
		{

			drawItem( viewportRect, previewRect, "", null );

			previewRect.x += previewSize + padding;

			var col = 1;
			for( int i = 0; i < filteredItems.Count; i++ )
			{

				if( col >= maxColumns )
				{
						
					col = 0;
						
					previewRect.x = padding;
					previewRect.y += previewSize + padding + labelHeight;
						
				}

				var item = filteredItems[ i ];
				drawItem( viewportRect, previewRect, item.name, item );

				col += 1;
				previewRect.x += previewSize + padding;

			}

		}
		GUI.EndScrollView();

	}

	private void drawItem( Rect viewport, Rect rect, string name, dfAtlas.ItemInfo sprite )
	{

		if( !selectionShown && name == selectedSprite )
		{

			selectionShown = true;

			if( rect.yMax > viewport.yMax )
			{
				currentScrollPos = new Vector2( 0, rect.y - viewport.height + rect.height );
			}

		}

		var labelStyle = "ObjectPickerResultsGridLabel";
		if( name == selectedSprite )
		{
			
			var outlineRect = new Rect( rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6 );
			DrawBox( outlineRect, Color.blue );

			outlineRect = new Rect( rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2 );
			DrawBox( outlineRect, Color.white );

		}

		GUI.Box( rect, "", "ObjectPickerBackground" );
		if( sprite != null )
		{
			drawSprite( rect, sprite );
		}

		var savedColor = GUI.color;
		if( !string.IsNullOrEmpty( name ) && sprite == null )
		{
			labelStyle = "minibutton";
			GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.red;
		}

		var labelRect = new Rect( rect.x, rect.y + rect.height, rect.width, 18f );
		GUI.Label( labelRect, string.IsNullOrEmpty( name ) ? "None" : name, labelStyle );

		GUI.color = savedColor;

		var evt = Event.current;
		if( evt != null && evt.type == EventType.mouseDown )
		{

			if( rect.Contains( evt.mousePosition ) || labelRect.Contains( evt.mousePosition ) )
			{

				selectedSprite = name;
				this.Repaint();

				if( evt.clickCount == 2 )
				{
					selectSprite( name );
				}

			}

		}

	}

	private string buildItemTooltip( dfAtlas.ItemInfo sprite )
	{

		if( sprite == null )
			return "";

		var width = (int)sprite.sizeInPixels.x;
		var height = (int)sprite.sizeInPixels.y;

		return string.Format( 
			"Atlas: {3}  Sprite: {0}  Size: {1}x{2}",
			sprite.name,
			width,
			height,
			atlas.name
		);

	}

	private void selectSprite( string name )
	{
		if( callback != null )
		{
			callback( name );
		}
		Cancel();
	}

	private void drawSprite( Rect areaRect, dfAtlas.ItemInfo sprite )
	{

		var size = sprite.sizeInPixels;
		var renderRect = areaRect;

		if( renderRect.width < size.x || renderRect.height < size.y )
		{

			var newHeight = size.y * areaRect.width / size.x;
			if( newHeight <= areaRect.height )
				renderRect.height = newHeight;
			else
				renderRect.width = size.x * areaRect.height / size.y;

		}
		else
		{
			renderRect.width = size.x;
			renderRect.height = size.y;
		}

		if( renderRect.width < areaRect.width ) renderRect.x = areaRect.x + ( areaRect.width - renderRect.width ) * 0.5f;
		if( renderRect.height < areaRect.height ) renderRect.y = areaRect.y + ( areaRect.height - renderRect.height ) * 0.5f;

		GUI.DrawTextureWithTexCoords( renderRect, atlas.Material.mainTexture, sprite.region, true );

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
		
	private void SearchArea()
	{

		GUI.Label( new Rect( 0f, 0f, base.position.width, 46 ), GUIContent.none, "ObjectPickerToolbar" );
			
		var cancelSearch = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;
		if( cancelSearch )
		{
			searchFilter = "";
		}

		EditorGUILayout.BeginHorizontal();
		{

			GUI.SetNextControlName( "SpriteSearchFilter" );
			searchFilter = EditorGUI.TextField( new Rect( 5f, 5f, base.position.width - 23f, 15f ), "", searchFilter, "SearchTextField" );
				
			if( GUI.Button( new Rect( base.position.width - 23f, 5f, 18, 15 ), "", "SearchCancelButton" ) )
			{
				searchFilter = "";
				GUIUtility.keyboardControl = 0;
			}

		}
		EditorGUILayout.EndHorizontal();

		if( focusSearchField )
		{
			GUI.FocusControl( "SpriteSearchFilter" );
			focusSearchField = false;
		}

		GUI.changed = false;

		GUILayout.BeginArea( new Rect( 0f, 26f, base.position.width, 50f ) );
		GUILayout.BeginHorizontal( new GUILayoutOption[ 0 ] );
		GUILayout.Toggle( true, atlas.name, "ObjectPickerTab", new GUILayoutOption[ 0 ] );
		GUILayout.EndHorizontal();
		GUILayout.EndArea();

	}

}
