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

public class dfPrefabSelectionDialog : ScriptableWizard
{

	/// <summary>
	/// This callback allows the dialog's caller to specify a routine for 
	/// rendering the asset preview for each item in the dialog. It is
	/// expected that this callback will return TRUE if the preview is 
	/// successfully rendered, and FALSE otherwise.
	/// </summary>
	/// <param name="item">The item to render a preview for</param>
	/// <param name="rect">The space provided for rendering the preview</param>
	/// <returns>Returns a value indicating whether the preview was generated</returns>
	public delegate bool PreviewCallback( GameObject item, Rect rect );

	public delegate bool FilterCallback( GameObject item );

	public delegate void SelectionCallback( GameObject selectedItem );

	public float previewSize = 100f;
	public float padding = 10f;

	#region Private instance variables

	private const float DEFAULT_PREVIEW_SIZE = 150f;
	private const float DEFAULT_PADDING = 10f;

	private Type componentType;
	private GameObject selectedObject;
	private SelectionCallback callback;
	private PreviewCallback previewCallback;
	private FilterCallback filterCallback;

	private List<GameObject> allPrefabsInProject;

	private Vector2 currentScrollPos = Vector2.zero;
	private bool focusSearchField = true;
	private string searchFilter = "";

	#endregion

	#region Static methods

	public static dfPrefabSelectionDialog Show( string title, Type componentType, SelectionCallback callback, PreviewCallback previewCallback )
	{
		return Show ( title, componentType, callback, previewCallback, null );
	}

	public static dfPrefabSelectionDialog Show( string title, Type componentType, SelectionCallback callback, PreviewCallback previewCallback, FilterCallback filterCallback )
	{

		if( componentType == null )
			throw new Exception( "Component type cannot be null" );

		var dialog = ScriptableWizard.DisplayWizard<dfPrefabSelectionDialog>( title );
		dialog.previewSize = DEFAULT_PREVIEW_SIZE;
		dialog.padding = DEFAULT_PADDING;
		dialog.componentType = componentType;
		dialog.minSize = new Vector2( 640, 480 );
		dialog.callback = callback;
		dialog.previewCallback = previewCallback;
		dialog.filterCallback = filterCallback;

		dialog.getFilteredItems();

		dialog.ShowAuxWindow();

		return dialog;

	}

	#endregion

	void OnGUI()
	{

		if( EditorApplication.isCompiling )
		{
			this.Cancel();
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

		var statusMessage = "";
		if( allPrefabsInProject == null || allPrefabsInProject.Count == 0 )
			statusMessage = "Loading, please wait...";

		var statusRect = new Rect( 0, base.position.height - 25f, base.position.width, 50f );
		GUI.BeginGroup( statusRect, statusMessage, "ObjectPickerToolbar" );
		{
			var test = new Rect( 10, 5, base.position.width - 20, 25 );
			GUI.Label( test, buildItemTooltip( selectedObject ) );
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

		var loadingMessage = "";
		if( allPrefabsInProject == null || allPrefabsInProject.Count == 0 )
		{
			loadingMessage = "Loading, please wait...";
		}

		var viewportRect = new Rect( 0f, 50, base.position.width, base.position.height - 75 );
		GUI.Box( viewportRect, loadingMessage, "ObjectPickerPreviewBackground" );

		var filteredItems = getFilteredItems();

		var labelHeight = 18f;
		var viewWidth = viewportRect.width - 20f;
		var maxColumns = Mathf.Max( Mathf.FloorToInt( viewWidth / ( previewSize + padding ) ), 1 );
		var numItemsDisplayed = filteredItems.Count + 1;
		var rows = numItemsDisplayed / maxColumns;
		if( numItemsDisplayed % maxColumns > 0 ) rows += 1;

		var scrollRect = new Rect( 0, 50, viewWidth, rows * ( previewSize + padding + labelHeight ) );
		var previewRect = new Rect( padding, 50, previewSize, previewSize );

		currentScrollPos = GUI.BeginScrollView( viewportRect, currentScrollPos, scrollRect );
		{

			drawItem( previewRect, "", null );

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
				drawItem( previewRect, item.name, item );

				col += 1;
				previewRect.x += previewSize + padding;

			}

		}
		GUI.EndScrollView();

	}

	private bool IsPrefab( GameObject item )
	{

		try
		{
			return
				item != null &&
				PrefabUtility.GetPrefabParent( item ) == null &&
				PrefabUtility.GetPrefabObject( item ) != null;
		}
		catch
		{
			return false;
		}

	}

	private List<GameObject> getFilteredItems()
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
					EditorUtility.DisplayProgressBar( this.title, "Loading prefabs", (float)i / (float)allAssetPaths.Length );
				}

				var path = allAssetPaths[ i ];
				if( !path.EndsWith( ".prefab", StringComparison.OrdinalIgnoreCase ) )
					continue;

				try
				{
					var gameObject = AssetDatabase.LoadMainAssetAtPath( path ) as GameObject;
					if( IsPrefab( gameObject ) )
					{
						allPrefabsInProject.Add( gameObject );
					}
				}
				catch( Exception err )
				{
					Debug.LogError( "Error loading prefab at " + path + " - " + err.Message );
				}

			}

			EditorUtility.ClearProgressBar();

			allPrefabsInProject.Sort( ( GameObject lhs, GameObject rhs ) =>
			{
				return lhs.name.CompareTo( rhs.name );
			} );

		}

		var result = new List<GameObject>();

		foreach( var item in allPrefabsInProject )
		{

			if( item == null || item.GetComponent( componentType ) == null )
				continue;

			try
			{
				if( filterCallback != null && !filterCallback( item ) )
					continue;
			}
			catch { continue; }

			if( item.name.IndexOf( searchFilter, StringComparison.OrdinalIgnoreCase ) != -1 )
			{
				result.Add( item );
			}

		}

		return result;

	}

	private void drawItem( Rect rect, string name, GameObject item )
	{

		GUI.Box( rect, "", "ObjectPickerBackground" );
		if( item != null )
		{
			drawItem( item, rect );
		}

		var labelStyle = "ObjectPickerResultsGridLabel";
		if( item == selectedObject )
		{
			labelStyle = "ObjectPickerSmallStatus";
			DrawOutline( rect, Color.blue );
		}

		var labelRect = new Rect( rect.x, rect.y + rect.height, rect.width, 18f );
		GUI.Label( labelRect, string.IsNullOrEmpty( name ) ? "None" : name, labelStyle );

		var evt = Event.current;
		if( evt != null && evt.type == EventType.mouseDown )
		{

			if( rect.Contains( evt.mousePosition ) || labelRect.Contains( evt.mousePosition ) )
			{

				selectedObject = item;
				this.Repaint();

				if( evt.clickCount == 2 )
				{
					selectItem( item );
				}

			}

		}

	}

	private void drawItem( GameObject item, Rect rect )
	{

		if( previewCallback != null )
		{
			if( previewCallback( item, rect ) )
				return;
		}

		var texture = AssetPreview.GetMiniThumbnail( PrefabUtility.GetPrefabObject( item ) );
		if( texture == null )
		{
			return;
		}

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

	private string buildItemTooltip( GameObject item )
	{

		if( item == null )
			return "";

		return string.Format(
			"{0}  Path: {1}",
			item.name,
			AssetDatabase.GetAssetPath( item.GetInstanceID() )
		);

	}

	private void selectItem( GameObject item )
	{
		if( callback != null )
		{
			callback( item );
		}
		Cancel();
	}

	static public void DrawOutline( Rect rect, Color color )
	{
		if( Event.current.type == EventType.Repaint )
		{
			Texture2D tex = EditorGUIUtility.whiteTexture;
			GUI.color = color;
			GUI.DrawTexture( new Rect( rect.xMin, rect.yMin, 1f, rect.height ), tex );
			GUI.DrawTexture( new Rect( rect.xMax, rect.yMin, 1f, rect.height ), tex );
			GUI.DrawTexture( new Rect( rect.xMin, rect.yMin, rect.width, 1f ), tex );
			GUI.DrawTexture( new Rect( rect.xMin, rect.yMax, rect.width, 1f ), tex );
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

			GUI.SetNextControlName( "PrefabSearchFilter" );
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
			GUI.FocusControl( "PrefabSearchFilter" );
			focusSearchField = false;
		}

		GUI.changed = false;

		GUILayout.BeginArea( new Rect( 0f, 26f, base.position.width, 50f ) );
		GUILayout.BeginHorizontal( new GUILayoutOption[ 0 ] );
		GUILayout.Toggle( true, "Prefabs", "ObjectPickerTab", new GUILayoutOption[ 0 ] );
		GUILayout.EndHorizontal();
		GUILayout.EndArea();

	}

}
