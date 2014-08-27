/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CustomEditor( typeof( dfGUIManager ) )]
public class dfGUIManagerInspector : Editor
{

	private static float lastScale = 1f;
	private static int lastWidth = 800;
	private static int lastHeight = 600;
	private static bool showDrawCalls = false;

	private dfGUIManager lastSelected = null;
	private dfDesignGuide selectedGuide = null;
	private EditorAction currentAction = EditorAction.None;

	public void OnEnable()
	{
		lastSelected = null;
	}

	public override void OnInspectorGUI()
	{

		var view = target as dfGUIManager;

		if( lastSelected != view )
		{
			lastSelected = view;
			lastWidth = view.FixedWidth;
			lastHeight = view.FixedHeight;
			lastScale = view.UIScale;
		}

		if( view.enabled )
		{

			var screenSize = view.GetScreenSize();
			var screenSizeFormat = string.Format( "{0}x{1}", (int)screenSize.x, (int)screenSize.y );

			var totalControls =
				view.GetComponentsInChildren<dfControl>()
				.Length;

			var statusFormat = @"
	Screen size: {4}
	Total draw calls: {0}
	Total triangles: {1}
	Controls rendered: {2}
	Total controls: {3}
	";

			var status = string.Format(
				statusFormat.Trim(),
				view.TotalDrawCalls,
				view.TotalTriangles,
				view.NumControlsRendered,
				totalControls,
				screenSizeFormat
				);

			EditorGUILayout.HelpBox( status, MessageType.Info );

		}

		dfEditorUtil.LabelWidth = 130f;

		using( dfEditorUtil.BeginGroup( "Rendering and Behavior" ) )
		{

			if( view.RenderCamera == null )
				GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.red;

			var camera = EditorGUILayout.ObjectField( "Render Camera", view.RenderCamera, typeof( Camera ), true ) as Camera;
			if( camera != view.RenderCamera )
			{
				dfEditorUtil.MarkUndo( view, "Assign Render Camera" );
				view.RenderCamera = camera;
			}

			if( camera == null )
				return;

			var renderQueue = EditorGUILayout.IntField( "Render Queue", view.RenderQueueBase );
			if( renderQueue != view.RenderQueueBase )
			{
				dfEditorUtil.MarkUndo( view, "Change RenderQueue Base Value" );
				view.RenderQueueBase = renderQueue;
			}

			var activeViews = FindObjectsOfType( typeof( dfGUIManager ) ) as dfGUIManager[];
			var duplicateCheck = activeViews.Count( v => v.RenderQueueBase == renderQueue );
			if( duplicateCheck > 1 )
			{
				EditorGUILayout.HelpBox( "There is more than one UI Root in this scene with the same Render Queue value", MessageType.Warning );
			}

			var renderModes = new string[] { "Orthographic", "Perspective" };
			var currentMode = camera.isOrthoGraphic ? 0 : 1;
			var selectedMode = EditorGUILayout.Popup( "Render Mode", currentMode, renderModes );
			if( currentMode != selectedMode )
			{

				dfEditorUtil.MarkUndo( view, "Change Render Mode" );

				if( selectedMode == 0 )
				{

					camera.isOrthoGraphic = true;
					camera.nearClipPlane = -2;
					camera.farClipPlane = 2;
					camera.transform.position = view.transform.position;

					view.transform.localScale = Vector3.one;

				}
				else
				{

					camera.isOrthoGraphic = false;
					camera.nearClipPlane = 0.01f;
					camera.hideFlags = (HideFlags)0x00;

					// http://stackoverflow.com/q/2866350/154165
					var fov = camera.fieldOfView * Mathf.Deg2Rad;
					var corners = view.GetCorners();
					var width = Vector3.Distance( corners[ 3 ], corners[ 0 ] );
					var distance = width / ( 2f * Mathf.Tan( fov / 2f ) );
					var back = view.transform.TransformDirection( Vector3.back ) * distance;

					camera.transform.position = view.transform.position + back;
					camera.farClipPlane = distance * 2f;

				}

			}

			var pixelPerfect = EditorGUILayout.Toggle( "Pixel Perfect", view.PixelPerfectMode );
			if( pixelPerfect != view.PixelPerfectMode )
			{
				dfEditorUtil.MarkUndo( view, "Change Pixel Perfect Mode" );
				view.PixelPerfectMode = pixelPerfect;
				view.Render();
			}

			var eatMouseEvents = EditorGUILayout.Toggle( "Consume Mouse", view.ConsumeMouseEvents );
			if( eatMouseEvents != view.ConsumeMouseEvents )
			{
				dfEditorUtil.MarkUndo( view, "Change 'Consume Mouse Events' property" );
				view.ConsumeMouseEvents = eatMouseEvents;
			}

			if( view.ConsumeMouseEvents )
			{
				EditorGUILayout.HelpBox( "Using the 'Consume Mouse' feature is likely to result in reduced performance on mobile platforms", MessageType.Info );
			}

		}

		using( dfEditorUtil.BeginGroup( "Defaults and Materials" ) )
		{

			SelectTextureAtlas( "Default Atlas", view, "DefaultAtlas", false, true, 125 );
			SelectFontDefinition( "Default Font", view.DefaultAtlas, view, "DefaultFont", true, 125 );

			var merge = EditorGUILayout.Toggle( "Merge Materials", view.MergeMaterials );
			if( merge != view.MergeMaterials )
			{
				dfEditorUtil.MarkUndo( view, "Change Material Merge Property" );
				view.MergeMaterials = merge;
				view.Render();
			}

			var generateNormals = EditorGUILayout.Toggle( "Generate Normals", view.GenerateNormals );
			if( generateNormals != view.GenerateNormals )
			{
				dfRenderData.FlushObjectPool();
				dfEditorUtil.MarkUndo( view, "Changed Generate Normals property" );
				view.GenerateNormals = generateNormals;
				view.Render();
			}

		}

		using( dfEditorUtil.BeginGroup( "Target Resolution" ) )
		{

			#region Force user to apply changes to scale

			lastScale = EditorGUILayout.FloatField( "UI Scale", lastScale );
			GUI.enabled = !Mathf.Approximately( lastScale, view.UIScale );

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space( dfEditorUtil.LabelWidth + 5 );
				if( GUILayout.Button( "Apply" ) )
				{
					dfEditorUtil.MarkUndo( view, "Change UI Scale" );
					view.UIScale = lastScale;
					view.Render();
				}
			}
			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;

			var useLegacyMode = EditorGUILayout.Toggle( "Use Legacy Mode", view.UIScaleLegacyMode );
			if( useLegacyMode != view.UIScaleLegacyMode )
			{
				dfEditorUtil.MarkUndo( view, "Change UI Scale Mode" );
				view.UIScaleLegacyMode = useLegacyMode;
				view.Render();
			}

			#endregion

			if( Application.isPlaying )
			{

				var offset = EditInt2( "Offset", "X", "Y", view.UIOffset );
				if( offset != view.UIOffset )
				{
					dfEditorUtil.MarkUndo( view, "Change UI Offset" );
					view.UIOffset = offset;
					view.Render();
				}

			}

			#region Force user to apply changes to width or height

			lastWidth = EditorGUILayout.IntField( "Screen Width", lastWidth );
			lastHeight = EditorGUILayout.IntField( "Screen Height", lastHeight );

			GUI.enabled = lastWidth != view.FixedWidth || lastHeight != view.FixedHeight;

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space( dfEditorUtil.LabelWidth + 5 );
				if( GUILayout.Button( "Apply" ) )
				{
					dfEditorUtil.MarkUndo( view, "Change Resolution" );
					view.FixedWidth = lastWidth;
					view.FixedHeight = lastHeight;
					view.Render();
				}
			}
			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;

			#endregion

#if !UNITY_ANDROID
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space( dfEditorUtil.LabelWidth + 5 );
				if( GUILayout.Button( "Use Build Settings" ) )
				{

					dfEditorUtil.MarkUndo( view, "Change Resolution" );

					var newWidth = PlayerSettings.defaultScreenWidth;
					var newHeight = PlayerSettings.defaultScreenHeight;

#if UNITY_WEBPLAYER
						newWidth = PlayerSettings.defaultWebScreenWidth;
						newHeight = PlayerSettings.defaultWebScreenHeight;
#endif

					view.FixedWidth = newWidth;
					view.FixedHeight = newHeight;

					view.RenderCamera.aspect = view.FixedWidth / view.FixedHeight;
					view.Render();

					lastWidth = view.FixedWidth;
					lastHeight = view.FixedHeight;

				}
			}
			EditorGUILayout.EndHorizontal();
#endif

		}

		using( dfEditorUtil.BeginGroup( "Design-Time Properties", 150 ) )
		{

			var showMeshConfig = EditorPrefs.GetBool( "dfGUIManager.ShowMesh", false );
			var showMesh = EditorGUILayout.Toggle( "Show Wireframe", showMeshConfig );
			if( showMesh != showMeshConfig )
			{

				EditorPrefs.SetBool( "dfGUIManager.ShowMesh", showMesh );

				var meshRenderer = view.GetComponent<MeshRenderer>();
				if( meshRenderer != null )
				{
					EditorUtility.SetSelectedWireframeHidden( meshRenderer, !showMesh );
				}

				SceneView.RepaintAll();

			}

			var showRulersConfig = EditorPrefs.GetBool( "dfGUIManager.ShowRulers", true );
			var showRulers = EditorGUILayout.Toggle( "Show Rulers", showRulersConfig );
			if( showRulers != showRulersConfig )
			{
				EditorPrefs.SetBool( "dfGUIManager.ShowRulers", showRulers );
				SceneView.RepaintAll();
			}

			var showGuidesConfig = EditorPrefs.GetBool( "dfGUIManager.ShowGuides", true );
			var showGuides = EditorGUILayout.Toggle( "Show Guides", showGuidesConfig );
			if( showGuides != showGuidesConfig )
			{
				EditorPrefs.SetBool( "dfGUIManager.ShowGuides", showGuides );
				SceneView.RepaintAll();
			}

			var showHintsConfig = EditorPrefs.GetBool( "DaikonForge.ShowHints", true );
			var showHints = EditorGUILayout.Toggle( "Show Hints", showHintsConfig );
			if( showHints != showHintsConfig )
			{
				EditorPrefs.SetBool( "DaikonForge.ShowHints", showHints );
				SceneView.RepaintAll();
			}

			var showExtentsConfig = UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowControlExtents", true );
			var showExtents = EditorGUILayout.Toggle( "Show Size Markers", showExtentsConfig );
			if( showExtentsConfig != showExtents )
			{
				EditorPrefs.SetBool( "dfGUIManager.ShowControlExtents", showExtents );
				SceneView.RepaintAll();
			}

			#region Disable "Snap to Grid" for testing new "Snap to Guide" functionality
			EditorPrefs.SetBool( "dfGUIManager.SnapToGrid", false );
			//var snapToGridConfig = EditorPrefs.GetBool( "dfGUIManager.SnapToGrid", false );
			//var snapToGrid = EditorGUILayout.Toggle( "Snap To Grid", snapToGridConfig );
			//if( snapToGrid != snapToGridConfig )
			//{
			//    EditorPrefs.SetBool( "dfGUIManager.SnapToGrid", snapToGrid );
			//    SceneView.RepaintAll();
			//}
			#endregion

			var showGridConfig = EditorPrefs.GetBool( "dfGUIManager.ShowGrid", false );
			var showGrid = EditorGUILayout.Toggle( "Show Grid", showGridConfig );
			if( showGrid != showGridConfig )
			{
				EditorPrefs.SetBool( "dfGUIManager.ShowGrid", showGrid );
				SceneView.RepaintAll();
			}

			var gridSizeConfig = EditorPrefs.GetInt( "dfGUIManager.GridSize", 25 );
			var gridSize = Mathf.Max( EditorGUILayout.IntField( "Grid Size", gridSizeConfig ), 5 );
			if( gridSize != gridSizeConfig )
			{
				EditorPrefs.SetInt( "dfGUIManager.GridSize", gridSize );
				SceneView.RepaintAll();
			}

			var showSafeAreaConfig = EditorPrefs.GetBool( "ShowSafeArea", false );
			var showSafeArea = EditorGUILayout.Toggle( "Show Safe Area", showSafeAreaConfig );
			if( showSafeArea != showSafeAreaConfig )
			{
				EditorPrefs.SetBool( "ShowSafeArea", showSafeArea );
				SceneView.RepaintAll();
			}

			if( showSafeArea )
			{
				var marginConfig = EditorPrefs.GetFloat( "SafeAreaMargin", 10f );
				var safeAreaMargin = EditorGUILayout.Slider( "Safe %", marginConfig, 0f, 50f );
				if( marginConfig != safeAreaMargin )
				{
					EditorPrefs.SetFloat( "SafeAreaMargin", safeAreaMargin );
					SceneView.RepaintAll();
				}
			}

		}

		//dfEditorUtil.DrawHorzLine();
		EditorGUILayout.Separator();

		EditorGUILayout.BeginHorizontal();
		{

			if( GUILayout.Button( "Help" ) )
			{
				var url = "http://www.daikonforge.com/dfgui/tutorials/";
				Application.OpenURL( url );
				Debug.Log( "Opened tutorial page at " + url );
			}

			if( GUILayout.Button( "Force Refresh" ) )
			{
				dfGUIManager.RefreshAll( true );
				Debug.Log( "User interface manually refreshed" );
			}

		}
		EditorGUILayout.EndHorizontal();

		showDrawCallInfo( view );

	}

	private void showDrawCallInfo( dfGUIManager view )
	{

		dfEditorUtil.DrawSeparator();

		using( dfEditorUtil.BeginGroup( "Draw Calls" ) )
		{
			showDrawCalls = EditorGUILayout.Toggle( "Show Draw Calls", showDrawCalls );
		}

		if( !EditorApplication.isCompiling && showDrawCalls )
		{

			dfEditorUtil.DrawHorzLine();

			var controls = view.ControlsRendered;
			var indices = view.DrawCallStartIndices;
			var drawCallCount = view.TotalDrawCalls;

			var list = dfList<string>.Obtain();

			var last = 0;
			var lastControl = "";

			try
			{

				for( int i = 0; i < drawCallCount; i++ )
				{

					using( dfEditorUtil.BeginGroup( string.Format( "Draw call {0} of {1}", i + 1, drawCallCount ) ) )
					{


						var buffer = view.GetDrawCallBuffer( i );

						list.Clear();

						var baseSelectionIndex = last;
						while( last < controls.Count && indices[ last ] == i )
						{
							lastControl = getObjectPath( controls[ last++ ] );
							list.Add( ( list.Count + 1 ) + ". " + lastControl );
						}

						EditorGUILayout.ObjectField( "Material", buffer.Material, typeof( Material ), false );
						EditorGUILayout.ObjectField( "Texture", buffer.Material.mainTexture, typeof( Texture2D ), false );
						EditorGUILayout.IntField( "Triangles", buffer.Triangles.Count / 3 );

						if( list.Count == 0 )
						{
							var message = "Control '{0}' creates multiple draw calls";
							message = string.Format( message, getObjectPath( controls[ last ] ) );
							EditorGUILayout.HelpBox( message, MessageType.Info );
						}
						else
						{

							EditorGUILayout.IntField( "Controls", list.Count );

							list.Insert( 0, "Select a control" );

							int selectedIndex = EditorGUILayout.Popup( "Select Control", 0, list.ToArray() );
							if( selectedIndex > 0 )
							{
								dfEditorUtil.DelayedInvoke( () =>
								{
									Selection.activeObject = controls[ baseSelectionIndex + selectedIndex - 1 ];
								} );
							}

						}

						dfEditorUtil.DrawHorzLine();

					}

				}

			}
			catch { }

			list.Release();

		}

	}

	private string getObjectPath( dfControl control )
	{

		var buffer = new System.Text.StringBuilder( 1024 );
		while( control != null )
		{
			if( buffer.Length > 0 ) buffer.Insert( 0, "\\" );
			buffer.Insert( 0, control.name );
			control = control.Parent;
		}

		return buffer.ToString();

	}

	public void OnSceneGUI()
	{

		if( Selection.objects.Length > 1 )
		{
			return;
		}

		if( Event.current.isMouse && Event.current.button == 2 )
			return;

		var view = target as dfGUIManager;

		var evt = Event.current;
		var id = GUIUtility.GetControlID( GetType().Name.GetHashCode(), FocusType.Passive );
		var eventType = evt.GetTypeForControl( id );

		var showGuidesConfig = EditorPrefs.GetBool( "dfGUIManager.ShowGuides", true );
		var createVerticalGuideRect = new Rect();
		var createHorizontalGuideRect = new Rect();

		if( SceneView.currentDrawingSceneView.camera.isOrthoGraphic )
		{

			//if( createNewGuide() )
			//{
			//    GUIUtility.hotControl = GUIUtility.keyboardControl = id;
			//    evt.Use();
			//}

			if( showGuidesConfig )
			{

				EditorGUIUtility.AddCursorRect( createVerticalGuideRect = getNewVerticalGuideRect(), MouseCursor.ResizeHorizontal );
				EditorGUIUtility.AddCursorRect( createHorizontalGuideRect = getNewHorizontalGuideRect(), MouseCursor.ResizeVertical );

				addGuideCursorRects();

			}

		}

		if( eventType == EventType.repaint )
		{
			drawActionHints( evt );
		}
		else if( eventType == EventType.mouseDown )
		{

			var modifierKeyPressed = evt.alt || evt.control || evt.shift;
			if( evt.button != 0 || modifierKeyPressed )
			{

				if( evt.button == 1 && !modifierKeyPressed )
				{

					// Ensure that the mouse point is actually contained within the Manager
					var ray = HandleUtility.GUIPointToWorldRay( evt.mousePosition );
					RaycastHit hitInfo;
					if( view.collider.Raycast( ray, out hitInfo, 1000 ) )
					{

						displayContextMenu();
						evt.Use();

						return;

					}

				}

				GUIUtility.hotControl = GUIUtility.keyboardControl = 0;

			}

			if( Tools.current == Tool.Move && evt.button == 0 && showGuidesConfig )
			{

				if( createVerticalGuideRect.Contains( evt.mousePosition ) )
				{
					view.guides.Add( selectedGuide = new dfDesignGuide() { orientation = dfControlOrientation.Vertical, position = 0 } );
					GUIUtility.hotControl = GUIUtility.keyboardControl = id;
					evt.Use();
					currentAction = EditorAction.DraggingGuide;
				}
				else if( createHorizontalGuideRect.Contains( evt.mousePosition ) )
				{
					view.guides.Add( selectedGuide = new dfDesignGuide() { orientation = dfControlOrientation.Horizontal, position = 0 } );
					GUIUtility.hotControl = GUIUtility.keyboardControl = id;
					evt.Use();
					currentAction = EditorAction.DraggingGuide;
				}
				else
				{

					var guide = getGuideUnderMouse();
					if( guide != null )
					{
						currentAction = EditorAction.DraggingGuide;
						selectedGuide = guide;
						GUIUtility.hotControl = GUIUtility.keyboardControl = id;
						evt.Use();
						SceneView.RepaintAll();
					}
					else
					{
						var hit = HandleUtility.PickGameObject( evt.mousePosition, true );
						if( hit != null )
						{
							Selection.activeGameObject = hit;
							return;
						}
					}

				}

			}

		}
		else if( eventType == EventType.mouseUp && selectedGuide != null )
		{
			currentAction = EditorAction.None;
			endDraggingGuide();
			SceneView.RepaintAll();
		}
		else if( evt.keyCode == KeyCode.Escape && selectedGuide != null )
		{
			currentAction = EditorAction.None;
			endDraggingGuide();
			SceneView.RepaintAll();
		}
		else if( currentAction == EditorAction.DraggingGuide && selectedGuide != null )
		{
			dragDesignGuide( evt.mousePosition );
		}

		if( evt.type == EventType.keyDown && evt.keyCode == KeyCode.G && evt.shift && evt.control )
		{
			var showGuides = EditorPrefs.GetBool( "dfGUIManager.ShowGuides", true );
			EditorPrefs.SetBool( "dfGUIManager.ShowGuides", !showGuides );
			SceneView.RepaintAll();
		}

	}

	private dfDesignGuide getGuideUnderMouse()
	{

		var view = this.target as dfGUIManager;

		for( int i = 0; i < view.guides.Count; i++ )
		{

			var guide = view.guides[ i ];
			var guideRect = calculateGuideCursorRect( guide );

			if( guideRect.Contains( Event.current.mousePosition ) )
			{
				return guide;
			}

		}

		return null;

	}

	private void addGuideCursorRects()
	{

		var view = this.target as dfGUIManager;

		for( int i = 0; i < view.guides.Count; i++ )
		{

			var guide = view.guides[ i ];

			if( selectedGuide != null && guide != selectedGuide )
				continue;

			var guideRect = calculateGuideCursorRect( guide );
			var cursor = guide.orientation == dfControlOrientation.Vertical ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical;

			EditorGUIUtility.AddCursorRect( guideRect, cursor );

		}

	}

	private Rect calculateGuideCursorRect( dfDesignGuide guide )
	{

		var view = this.target as dfGUIManager;

		var corners = view.GetCorners();
		var cursorRect = new Rect();

		if( guide.orientation == dfControlOrientation.Vertical )
		{

			var pos1 = HandleUtility.WorldToGUIPoint( Vector3.Lerp( corners[ 0 ], corners[ 1 ], (float)guide.position / (float)view.FixedWidth ) );
			var pos2 = HandleUtility.WorldToGUIPoint( Vector3.Lerp( corners[ 3 ], corners[ 2 ], (float)guide.position / (float)view.FixedWidth ) );

			cursorRect = new Rect(
				pos1.x - 5,
				pos1.y,
				10,
				Mathf.Abs( pos2.y - pos1.y )
			);

		}
		else
		{

			var pos1 = HandleUtility.WorldToGUIPoint( Vector3.Lerp( corners[ 0 ], corners[ 3 ], (float)guide.position / (float)view.FixedHeight ) );
			var pos2 = HandleUtility.WorldToGUIPoint( Vector3.Lerp( corners[ 1 ], corners[ 2 ], (float)guide.position / (float)view.FixedHeight ) );

			cursorRect = new Rect(
				pos1.x,
				pos1.y - 5,
				Mathf.Abs( pos2.x - pos1.x ),
				10
			);

		}

		// Compensate for fast mouse movement
		if( guide == selectedGuide )
		{
			cursorRect.x -= 100;
			cursorRect.y -= 100;
			cursorRect.width += 200;
			cursorRect.height += 200;
		}

		return cursorRect;

	}

	private void drawActionHints( Event evt )
	{

		var showHints = EditorPrefs.GetBool( "DaikonForge.ShowHints", true );
		if( !showHints )
			return;

		var view = (dfGUIManager)target;
		var screenWidth = SceneView.currentDrawingSceneView.camera.pixelWidth;
		var screenHeight = SceneView.currentDrawingSceneView.camera.pixelHeight;

		if( currentAction == EditorAction.DraggingGuide )
		{

			var position = evt.mousePosition;

			if( !evt.alt )
			{

				Handles.BeginGUI();

				var rect = new Rect( position.x + 10, position.y - 20, 125, 20 );
				GUI.Box( rect, GUIContent.none, dfEditorUtil.BoxStyleDark );

				var width = view.FixedWidth;
				var info = string.Format( "{0}px, {1:P0}", selectedGuide.position, selectedGuide.position / (float)width );
				EditorGUI.DropShadowLabel( rect, info, "PreLabel" );

				Handles.EndGUI();

			}

		}

		var hintRect = new Rect( 0, screenHeight - 24, screenWidth, 24 );
		GUI.Box( hintRect, GUIContent.none, dfEditorUtil.BoxStyleLight );
		hintRect.y += 18;
		GUI.Window( -1, hintRect, drawStatusFunc, GUIContent.none, (GUIStyle)"sv_iconselector_back" );

	}

	private void drawStatusFunc( int id )
	{

		var screenWidth = SceneView.currentDrawingSceneView.camera.pixelWidth;
		var showGuides = UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowGuides", true );
		//var showGrid = UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowGrid", false );

		var statusMessage = "";

		if( currentAction == EditorAction.DraggingGuide )
		{

			var actionRect = new Rect( 5, 2, 85, 20 );
			EditorGUI.DropShadowLabel( actionRect, "Guide", (GUIStyle)"GUIEditor.BreadcrumbLeft" );

			statusMessage = "CTRL - Snap to every 5 pixels, SHIFT - Snap to grid, ALT - Hide info, Right click to edit";

		}
		else if( currentAction == EditorAction.None )
		{

			statusMessage = "Right-click " + target.name + " for context menu";

			if( showGuides )
				statusMessage = append( statusMessage, "Drag mouse from ruler area to create guide" );

			statusMessage = append( statusMessage, "CTRL-SHIFT-G toggles guides" );

		}

		var hintRect = new Rect( 0, 2, screenWidth, 20 );
		EditorGUI.DropShadowLabel( hintRect, statusMessage, "PreLabel" );

	}

	private string append( string value, string message )
	{
		if( value.Length > 0 )
			return value + ", " + message;
		else
			return message;
	}

	private void dragDesignGuide( Vector2 position )
	{

		var view = this.target as dfGUIManager;

		if( Event.current.type == EventType.mouseDrag )
		{

			var guidePosition = getGUIManagerPosition( position );
			if( selectedGuide.orientation == dfControlOrientation.Vertical )
			{
				selectedGuide.position = (int)Mathf.Max( 0, Mathf.Min( view.FixedWidth, guidePosition.x ) );
			}
			else
			{
				selectedGuide.position = (int)Mathf.Max( 0, Mathf.Min( view.FixedHeight, guidePosition.y ) );
			}

			if( Event.current.control )
			{
				selectedGuide.position = selectedGuide.position.Quantize( 5 );
			}
			else if( Event.current.shift )
			{
				var gridSize = EditorPrefs.GetInt( "dfGUIManager.GridSize", 25 );
				selectedGuide.position = selectedGuide.position.RoundToNearest( gridSize );
			}

			SceneView.RepaintAll();
			Event.current.Use();

		}

	}

	private void endDraggingGuide()
	{

		var view = target as dfGUIManager;

		if( selectedGuide == null )
			return;

		if( selectedGuide.position == 0 || selectedGuide.position == view.FixedWidth )
			view.guides.Remove( selectedGuide );

		selectedGuide = null;

		GUIUtility.hotControl = GUIUtility.keyboardControl = 0;
		SceneView.RepaintAll();

	}

	private Vector2 getGUIManagerPosition( Vector2 position )
	{

		position.y = Camera.current.pixelHeight - position.y;

		var manager = this.target as dfGUIManager;

		var ray = Camera.current.ScreenPointToRay( position );
		var corner = manager.GetCorners()[ 0 ];
		var plane = new Plane( manager.transform.TransformDirection( Vector3.forward ), corner );

		var distance = 0f;
		plane.Raycast( ray, out distance );

		var hit = ray.origin + ray.direction * distance;

		var offset = ( ( hit - corner ) / manager.PixelsToUnits() ).RoundToInt();
		offset.y = Mathf.Min( manager.FixedHeight, Mathf.Max( 0, -offset.y ) );
		offset.x = Mathf.Min( manager.FixedWidth, Mathf.Max( 0, offset.x ) );

		return offset;

	}

	private Rect getNewHorizontalGuideRect()
	{

		var view = target as dfGUIManager;

		var corners = view.GetCorners();
		var ul = HandleUtility.WorldToGUIPoint( corners[ 0 ] );
		var ur = HandleUtility.WorldToGUIPoint( corners[ 1 ] );

		var cursorRect = new Rect(
			ul.x,
			ul.y - 50,
			ur.x - ul.x,
			50
		);

		return cursorRect;

	}

	private Rect getNewVerticalGuideRect()
	{

		var view = target as dfGUIManager;

		var corners = view.GetCorners();
		var ul = HandleUtility.WorldToGUIPoint( corners[ 0 ] );
		var ll = HandleUtility.WorldToGUIPoint( corners[ 3 ] );

		var cursorRect = new Rect(
			ul.x - 50,
			ul.y,
			50,
			ll.y - ul.y
		);

		return cursorRect;

	}

	private void displayContextMenu()
	{

		var menu = new GenericMenu();

		var items = new List<ContextMenuItem>();
		FillContextMenu( items );

		var actionFunc = new System.Action<int>( ( command ) =>
		{
			var handler = items[ command ].Handler;
			handler();
		} );

		menu.AddDisabledItem( new GUIContent( string.Format( "{0} (GUI Manager)", target.name ) ) );
		menu.AddSeparator( "" );

		var options = items.Select( i => i.MenuText ).ToList();
		for( int i = 0; i < options.Count; i++ )
		{
			var index = i;
			if( options[ i ] == "-" )
				menu.AddSeparator( "" );
			else
				menu.AddItem( new GUIContent( options[ i ] ), false, () => { actionFunc( index ); } );
		}

		menu.ShowAsContext();

	}

	protected Vector2 EditInt2( string groupLabel, string label1, string label2, Vector2 value )
	{

		var retVal = Vector2.zero;

		var savedLabelWidth = dfEditorUtil.LabelWidth;

		GUILayout.BeginHorizontal();
		{

			EditorGUILayout.LabelField( groupLabel, "", GUILayout.Width( dfEditorUtil.LabelWidth - 12 ) );

			GUILayout.BeginVertical();
			{

				dfEditorUtil.LabelWidth = 60f;

				var x = EditorGUILayout.IntField( label1, Mathf.RoundToInt( value.x ) );
				var y = EditorGUILayout.IntField( label2, Mathf.RoundToInt( value.y ) );

				retVal.x = x;
				retVal.y = y;

			}
			GUILayout.EndVertical();

			GUILayout.FlexibleSpace();

		}
		GUILayout.EndHorizontal();

		dfEditorUtil.LabelWidth = savedLabelWidth;

		return retVal;

	}

	protected void FillContextMenu( List<ContextMenuItem> menu )
	{

		// Adds a menu item for each dfControl class in the assembly that has 
		// an AddComponentMenu attribute defined.
		addContextMenuChildControls( menu );
		menu.Add( new ContextMenuItem() { MenuText = "-" } );

		// Add an option to allow the user to select any Prefab that 
		// has a dfControl component as the main component
		addContextSelectPrefab( menu );

		var showGuides = EditorPrefs.GetBool( "dfGUIManager.ShowGuides", true );
		if( showGuides )
		{

			// If the mouse is currently over a design guide, add the menu
			// options specific to those
			var designGuide = getGuideUnderMouse();
			if( designGuide != null )
			{
				addContextDesignGuide( menu, designGuide );
			}

			var scene = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;
			var isOrthographic = ( scene != null ) ? scene.camera.isOrthoGraphic : false;
			if( isOrthographic )
			{
				addContextCreateGuide( menu );
			}

		}

	}

	private void addContextCreateGuide( List<ContextMenuItem> menu )
	{

		var view = target as dfGUIManager;
		var mousePosition = getGUIManagerPosition( Event.current.mousePosition );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Guide/New Vertical Guide",
			Handler = () =>
			{
				view.guides.Add( new dfDesignGuide()
				{
					orientation = dfControlOrientation.Vertical,
					position = (int)mousePosition.x
				} );
			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Guide/New Horizontal Guide",
			Handler = () =>
			{
				view.guides.Add( new dfDesignGuide()
				{
					orientation = dfControlOrientation.Horizontal,
					position = (int)mousePosition.y
				} );
			}
		} );

		if( view.guides.Count > 0 )
		{

			menu.Add( new ContextMenuItem()
			{
				MenuText = "Guide/Remove All Guides",
				Handler = () =>
				{
					view.guides.Clear();
				}
			} );

		}

	}

	private void addContextDesignGuide( List<ContextMenuItem> menu, dfDesignGuide designGuide )
	{

		var view = target as dfGUIManager;

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Guide/Edit Guide",
			Handler = () =>
			{
				dfEditorUtil.DelayedInvoke( () =>
				{
					dfDesignGuideEditor.Edit( view, designGuide );
				} );
			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Guide/Delete Guide",
			Handler = () =>
			{
				view.guides.Remove( designGuide );
			}
		} );

		if( view.guides.Count > 0 )
		{
			menu.Add( new ContextMenuItem()
			{
				MenuText = "Guide/Clear All Guides",
				Handler = () =>
				{
					view.guides.Clear();
					SceneView.RepaintAll();
				}
			} );
		}

	}

	private void addContextSelectPrefab( List<ContextMenuItem> menu )
	{

		var view = target as dfGUIManager;

		// Need to determine final control position immediately, as 
		// this information is more difficult to obtain inside of an
		// anonymous delegate
		var mousePos = Event.current.mousePosition;
		var controlPosition = raycast( mousePos );

		System.Action selectPrefab = () =>
		{
			dfPrefabSelectionDialog.Show(
				"Select a prefab Control",
				typeof( dfControl ),
				( prefab ) =>
				{

					if( prefab == null )
						return;

					dfEditorUtil.MarkUndo( view, "Add child control - " + prefab.name );

					var newGameObject = PrefabUtility.InstantiatePrefab( prefab ) as GameObject;
					var childControl = newGameObject.GetComponent<dfControl>();
					childControl.transform.parent = view.transform;
					childControl.transform.position = controlPosition;

					childControl.ResetLayout();
					childControl.Invalidate();
					view.BringToFront( childControl );

					Selection.activeObject = childControl;

				},
				null,
				null
			);
		};

		menu.Add( new ContextMenuItem() { MenuText = "Add Prefab...", Handler = selectPrefab } );

	}

	private void addContextMenuChildControls( List<ContextMenuItem> menu )
	{

		var assembly = Assembly.GetAssembly( target.GetType() );
		var types = assembly.GetTypes();

		var controlTypes = types
			.Where( t =>
				typeof( dfControl ).IsAssignableFrom( t ) &&
				t.IsDefined( typeof( AddComponentMenu ), false )
			).ToList();

		// Look for user-defined types to add to the list
		var userAssembly = getUserAssembly();
		if( userAssembly != assembly )
		{

			var userTypes = userAssembly.GetTypes();

			var assemblyTypes =
				userTypes
				.Where( t =>
					typeof( dfControl ).IsAssignableFrom( t ) &&
					t.IsDefined( typeof( AddComponentMenu ), false )
				).ToList();

			controlTypes.AddRange( assemblyTypes );

		}

		var options = new List<ContextMenuItem>();

		for( int i = 0; i < controlTypes.Count; i++ )
		{
			var type = controlTypes[ i ];
			var componentMenuAttribute = type.GetCustomAttributes( typeof( AddComponentMenu ), false ).First() as AddComponentMenu;
			var optionText = componentMenuAttribute.componentMenu.Replace( "Daikon Forge/User Interface/", "" );
			options.Add( buildAddChildMenuItem( optionText, type ) );
		}

		options.Sort( ( lhs, rhs ) => { return lhs.MenuText.CompareTo( rhs.MenuText ); } );

		menu.AddRange( options );

	}

	/// <summary>
	/// Returns the Assembly containing user-defined types
	/// </summary>
	/// <returns></returns>
	private Assembly getUserAssembly()
	{

		var editorAssembly = typeof( Editor ).Assembly.GetName();

		var scriptTypes = Resources.FindObjectsOfTypeAll( typeof( MonoScript ) ) as MonoScript[];
		for( int i = 0; i < scriptTypes.Length; i++ )
		{

			// Fix for Unity error that results in a crash when it calls 
			// MonoScript.GetClass() on certain shaders (and other files?)
			if( scriptTypes[ i ].GetType() != typeof( MonoScript ) )
			{
				continue;
			}

			var path = AssetDatabase.GetAssetPath( scriptTypes[ i ] );
			if( string.IsNullOrEmpty( path ) || path.Contains( "editor", true ) || !path.EndsWith( ".cs", StringComparison.OrdinalIgnoreCase ) )
				continue;

			var scriptClass = scriptTypes[ i ].GetClass();
			if( scriptClass == null )
				continue;

			var scriptAssembly = scriptClass.Assembly;
			var referencedAssemblies = scriptAssembly.GetReferencedAssemblies();
			if( !referencedAssemblies.Contains( editorAssembly ) )
			{
				return scriptAssembly;
			}

		}

		return null;

	}

	private ContextMenuItem buildAddChildMenuItem( string optionText, Type type )
	{

		var view = target as dfGUIManager;

		// Need to determine final control position immediately, as 
		// this information is more difficult to obtain inside of an
		// anonymous delegate
		var mousePos = Event.current.mousePosition;
		var controlPosition = raycast( mousePos );

		return new ContextMenuItem()
		{
			MenuText = "Add control/" + optionText,
			Handler = () =>
			{

				var childName = type.Name;
				if( childName.StartsWith( "df" ) )
					childName = childName.Substring( 2 );

				childName = ObjectNames.NicifyVariableName( childName ) + buildControlNameSuffix( type );

				dfEditorUtil.MarkUndo( view, "Add Control - " + childName );

				var child = view.AddControl( type );
				child.name = childName;
				child.transform.position = controlPosition;

				Selection.activeGameObject = child.gameObject;

				child.Invalidate();
				view.Render();

			}
		};

	}

	private string buildControlNameSuffix( Type type )
	{

		var count = 0;
		using( var controls = getTopLevelControls() )
		{
			for( int i = 0; i < controls.Count; i++ )
			{
				if( controls[ i ].GetType() == type )
					count += 1;
			}
		}

		if( count > 0 )
			return " " + count.ToString();

		return "";

	}

	private dfList<dfControl> getTopLevelControls()
	{

		var view = target as dfGUIManager;
		var transform = view.transform;
		var childCount = transform.childCount;

		var controls = dfList<dfControl>.Obtain( childCount );

		for( int i = 0; i < childCount; i++ )
		{
			var control = transform.GetChild( i ).GetComponent<dfControl>();
			if( control != null )
				controls.Add( control );
		}

		controls.Sort();

		return controls;

	}

	private int getMaxZOrder()
	{

		var manager = target as dfGUIManager;
		var transform = manager.transform;

		var maxValue = 0;

		for( int i = 0; i < transform.childCount; i++ )
		{
			var control = transform.GetChild( i ).GetComponent<dfControl>();
			if( control != null )
			{
				maxValue = Mathf.Max( maxValue, control.ZOrder );
			}
		}

		return maxValue;

	}

	private Vector3 raycast( Vector2 mousePos )
	{

		var view = target as dfGUIManager;

		var plane = new Plane( view.transform.rotation * Vector3.back, view.transform.position );
		var ray = HandleUtility.GUIPointToWorldRay( mousePos );

		var distance = 0f;
		plane.Raycast( ray, out distance );

		return ray.origin + ray.direction * distance;

	}

	private static void setValue( dfGUIManager control, string propertyName, object value )
	{
		var property = control.GetType().GetProperty( propertyName );
		if( property == null )
			throw new ArgumentException( "Property '" + propertyName + "' does not exist on " + control.GetType().Name );
		property.SetValue( control, value, null );
	}

	private static object getValue( dfGUIManager control, string propertyName )
	{
		var property = control.GetType().GetProperty( propertyName );
		if( property == null )
			throw new ArgumentException( "Property '" + propertyName + "' does not exist on " + control.GetType().Name );
		return property.GetValue( control, null );
	}

	protected internal static void SelectTextureAtlas( string label, dfGUIManager view, string propertyName, bool readOnly, bool colorizeIfMissing )
	{
		SelectTextureAtlas( label, view, propertyName, readOnly, colorizeIfMissing, 95 );
	}

	protected internal static void SelectTextureAtlas( string label, dfGUIManager view, string propertyName, bool readOnly, bool colorizeIfMissing, int labelWidth )
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
				dfEditorUtil.DelayedInvoke( (System.Action)( () =>
				{
					var dialog = dfPrefabSelectionDialog.Show( "Select Texture Atlas", typeof( dfAtlas ), selectionCallback, dfTextureAtlasInspector.DrawAtlasPreview, null );
					dialog.previewSize = 200;
				} ) );
			}

		}
		finally
		{
			GUI.enabled = true;
			GUI.color = savedColor;
		}

	}

	protected internal static void SelectFontDefinition( string label, dfAtlas atlas, dfGUIManager view, string propertyName, bool colorizeIfMissing )
	{
		SelectFontDefinition( label, atlas, view, propertyName, colorizeIfMissing, 95 );
	}

	protected internal static void SelectFontDefinition( string label, dfAtlas atlas, dfGUIManager view, string propertyName, bool colorizeIfMissing, int labelWidth )
	{

		var savedColor = GUI.color;
		var showDialog = false;

		try
		{

			GUI.enabled = ( atlas != null );

			var value = (dfFontBase)getValue( view, propertyName );

			if( value == null && colorizeIfMissing )
				GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.red;

			dfPrefabSelectionDialog.FilterCallback filterCallback = delegate( GameObject item )
			{

				if( atlas == null )
					return false;

				var font = item.GetComponent<dfFontBase>();
				if( font == null )
					return false;

				if( font is dfFont )
				{
					var bitmappedFont = (dfFont)font;
					if( bitmappedFont.Atlas == null || !dfAtlas.Equals( bitmappedFont.Atlas, atlas ) )
						return false;
				}

				return true;

			};

			dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
			{
				var font = ( item == null ) ? null : item.GetComponent<dfFontBase>();
				dfEditorUtil.MarkUndo( view, "Change Font" );
				setValue( view, propertyName, font );
			};

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
							var draggedFont =
								( draggedObject != null )
								? draggedObject.GetComponent<dfFontBase>()
								: null;

							DragAndDrop.visualMode =
								( draggedFont != null )
								? DragAndDropVisualMode.Copy
								: DragAndDropVisualMode.None;

							if( evt.type == EventType.DragPerform )
							{
								selectionCallback( draggedObject );
							}

							evt.Use();

						}
					}
				}

				if( GUI.enabled && GUILayout.Button( new GUIContent( " ", "Edit Font" ), "IN ObjectField", GUILayout.Width( 14 ) ) )
				{
					showDialog = true;
				}

			}
			EditorGUILayout.EndHorizontal();

			if( value is dfDynamicFont || ( value is dfFont && !dfAtlas.Equals( atlas, ( (dfFont)value ).Atlas ) ) )
			{
				GUI.color = Color.white;
				EditorGUILayout.HelpBox( "The specified font uses a different Material, which could result in an additional draw call each time it is used.", MessageType.Warning );
			}

			GUILayout.Space( 2 );

			if( showDialog )
			{
				dfEditorUtil.DelayedInvoke( (System.Action)( () =>
				{
					dfPrefabSelectionDialog.Show(
						"Select Font",
						typeof( dfFontBase ),
						selectionCallback,
						dfFontDefinitionInspector.DrawFontPreview,
						filterCallback
					);
				} ) );
			}

		}
		finally
		{
			GUI.enabled = true;
			GUI.color = savedColor;
		}

	}

	protected class ContextMenuItem
	{
		public string MenuText;
		public System.Action Handler;
	}

	public bool HasFrameBounds()
	{
		return true;
	}

	public Bounds OnGetFrameBounds()
	{

		var view = target as dfGUIManager;
		var corners = view.GetCorners();

		var size = ( corners[ 2 ] - corners[ 0 ] ) * 0.5f;
		var center = corners[ 0 ] + size;

		return new Bounds( center, size * 0.85f );

	}

	#region Private nested types

	private enum EditorAction
	{
		None,
		DraggingGuide
	}

	#endregion

}

