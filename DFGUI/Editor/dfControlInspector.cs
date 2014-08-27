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

[CanEditMultipleObjects()]
[CustomEditor( typeof( dfControl ), true )]
public class dfControlInspector : Editor
{

	private static Dictionary<int, bool> commonFoldouts = new Dictionary<int, bool>();

	private List<OperationUndoInfo> controlUndoData = new List<OperationUndoInfo>();

	private int targetObjectCount = 0;

	private static Tool lastTool = Tool.Move;
	private EditorAction currentAction = EditorAction.None;
	private bool undoInformationSaved = false;

	private Vector3 dragStartPosition = Vector2.zero;
	private Vector2 dragCursorOffset = Vector2.zero;
	private Vector3 dragCursorOffset3 = Vector2.zero;
	private Vector2 dragStartSize = Vector2.zero;
	private Vector3 dragAnchorPoint = Vector3.zero;
	private float dragStartAngle = 0f;

	private List<EditorHandle> handles = new List<EditorHandle>();
	private Vector3[] handleLocations = new Vector3[ 9 ];
	private dfPivotPoint activeResizeHandle = dfPivotPoint.MiddleCenter;

	public override void OnInspectorGUI()
	{ 

		// Hide custom control inspectors when the application is compiling
		if( EditorApplication.isCompiling )
		{
			GUI.enabled = false;
			base.OnInspectorGUI();
			return;
		}

		handleLocations = new Vector3[ 9 ];

		targetObjectCount = 
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.Count();

		if( targetObjectCount > 1 )
		{
			OnInspectMultiple();
			return;
		}

		var control = target as dfControl;
		if( control == null )
			return;

		if( !control.gameObject.activeInHierarchy )
		{
			EditorGUILayout.HelpBox( "The GameObject is disabled", MessageType.Warning );
		}

		if( !control.enabled )
		{
			EditorGUILayout.HelpBox( "The control component is disabled", MessageType.Warning );
		}

		var isValidControl =
			control.gameObject.activeInHierarchy &&
			control.transform.parent != null &&
			control.GetManager() != null; 

		if( !isValidControl )
		{
			EditorGUILayout.HelpBox( "This control must be a child of a GUI Manager or another control", MessageType.Error );
			return;
		}

		if( isFoldoutExpanded( commonFoldouts, "Control Properties" ) )
		{
			OnInspectCommonProperties( control );
		}

		OnCustomInspector();

		EditorGUILayout.Separator();

	}

	private void OnInspectMultiple()
	{

		showAlignmentButtons();

		dfEditorUtil.DrawSeparator();
		GUILayout.Space( 5 );
		
		using( dfEditorUtil.BeginGroup( ObjectNames.NicifyVariableName( target.GetType().Name ) ) )
		{

			var warningMessage = "NOTE: Editing multiple controls at the same time is currently an EXPERIMENTAL " +
				"feature of Daikon Forge GUI, and we're still working on it. Use at your own risk.\n\nSome properties may " +
				"not update on the screen immediately when edited in this manner.";

			EditorGUILayout.HelpBox( warningMessage, MessageType.Warning );
			GUILayout.Space( 10 );

			base.OnInspectorGUI();

		}

	}

	private void showAlignmentButtons()
	{

		dfEditorUtil.LabelWidth = 110f;

		using( dfEditorUtil.BeginGroup( "Align Edges" ) )
		{

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space( 25 );
				if( GUILayout.Button( "Left" ) ) { alignEdgeLeft(); }
				if( GUILayout.Button( "Right" ) ) { alignEdgeRight(); }
				if( GUILayout.Button( "Top" ) ) { alignEdgeTop(); }
				if( GUILayout.Button( "Bottom" ) ) { alignEdgeBottom(); }
			}
			GUILayout.EndHorizontal();

		}

		using( dfEditorUtil.BeginGroup( "Align Centers" ) )
		{

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space( 25 );
				if( GUILayout.Button( "Horizontally" ) ) { alignCenterHorz(); }
				if( GUILayout.Button( "Vertically" ) ) { alignCenterVert(); }
			}
			GUILayout.EndHorizontal();

		}

		using( dfEditorUtil.BeginGroup( "Distribute" ) )
		{

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space( 25 );
				if( GUILayout.Button( "Horizontally" ) ) { distributeControlsHorizontally(); }
				if( GUILayout.Button( "Vertically" ) ) { distributeControlsVertically(); }
			}
			GUILayout.EndHorizontal();

		}

		using( dfEditorUtil.BeginGroup( "Make Same Size" ) )
		{

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space( 25 );
				if( GUILayout.Button( "Horizontally" ) ) { makeSameSizeHorizontally(); }
				if( GUILayout.Button( "Vertically" ) ) { makeSameSizeVertically(); }
			}
			GUILayout.EndHorizontal();

		}

	}

	private void OnInspectCommonProperties( dfControl control )
	{

		dfEditorUtil.LabelWidth = 110f;

		using( dfEditorUtil.BeginGroup( "Layout" ) )
		{

			EditorGUI.BeginChangeCheck();
			var rel = dfEditorUtil.EditInt2( "Position", "Left", "Top", control.RelativePosition );
			if( EditorGUI.EndChangeCheck() && Vector2.Distance( rel, control.RelativePosition ) > float.Epsilon )
			{
				dfEditorUtil.MarkUndo( control, "Change control Position" );
				control.RelativePosition = rel;
			}

			EditorGUI.BeginChangeCheck();
			var size = dfEditorUtil.EditInt2( "Size", "Width", "Height", control.Size );
			if( EditorGUI.EndChangeCheck() && Vector2.Distance( size, control.Size ) > float.Epsilon )
			{

				dfEditorUtil.MarkUndo( control, "Change control Size" );
				control.Size = size;

				// If control's anchor includes centering, its layout should be recalculated
				control.PerformLayout();

			}

			var pivot = (dfPivotPoint)EditorGUILayout.EnumPopup( "Pivot", control.Pivot );
			if( pivot != control.Pivot )
			{
				dfEditorUtil.MarkUndo( control, "Change control Pivot" );
				control.Pivot = pivot;
			}

			var anchor = EditAnchor( control.Anchor );
			if( anchor != control.Anchor )
			{
				dfEditorUtil.MarkUndo( control, "Change control Anchor" );
				control.Anchor = anchor;
				if( anchor.IsAnyFlagSet( dfAnchorStyle.CenterHorizontal | dfAnchorStyle.CenterVertical ) )
				{
					control.PerformLayout();
				}
			}

		}

		using( dfEditorUtil.BeginGroup( "Size Limits" ) )
		{

			var minSize = dfEditorUtil.EditInt2( "Min. Size", "Width", "Height", control.MinimumSize );
			if( Vector2.Distance( minSize, control.MinimumSize ) > float.Epsilon )
			{
				dfEditorUtil.MarkUndo( control, "Change minimum size" );
				control.MinimumSize = minSize;
			}

			var maxSize = dfEditorUtil.EditInt2( "Max. Size", "Width", "Height", control.MaximumSize );
			if( Vector2.Distance( maxSize, control.MaximumSize ) > float.Epsilon )
			{
				dfEditorUtil.MarkUndo( control, "Change minimum size" );
				control.MaximumSize = maxSize;
			}

			var hotZoneScale = EditFloat2( "Hot Zone Scale", "X", "Y", control.HotZoneScale );
			if( !Vector2.Equals( hotZoneScale, control.HotZoneScale ) )
			{
				dfEditorUtil.MarkUndo( control, "Change Hot Zone Scale" );
				control.HotZoneScale = hotZoneScale;
			}

		}

		using( dfEditorUtil.BeginGroup( "Behavior" ) )
		{

			var enabled = EditorGUILayout.Toggle( "Enabled", control.IsEnabled );
			if( enabled != control.IsEnabled )
			{
				dfEditorUtil.MarkUndo( control, "Change control Enabled" );
				control.IsEnabled = enabled;
			}

			GUI.enabled = canEditIsVisibleProperty( control );
			var visible = EditorGUILayout.Toggle( "Visible", control.IsVisible );
			if( visible != control.IsVisible )
			{
				dfEditorUtil.MarkUndo( control, "Change control Visible" );
				control.IsVisible = visible;
			}
			GUI.enabled = true;

			var localized = EditorGUILayout.Toggle( "Localized", control.IsLocalized );
			if( localized != control.IsLocalized )
			{
				dfEditorUtil.MarkUndo( control, "Change IsLocalized Property" );
				control.IsLocalized = localized;
			}

			var interactive = EditorGUILayout.Toggle( "Interactive", control.IsInteractive );
			if( interactive != control.IsInteractive )
			{
				dfEditorUtil.MarkUndo( control, "Change control Interactive property" );
				control.IsInteractive = interactive;
			}

			var canFocus = EditorGUILayout.Toggle( "Can Focus", control.CanFocus );
			if( canFocus != control.CanFocus )
			{
				dfEditorUtil.MarkUndo( control, "Change CanFocus property" );
				control.CanFocus = canFocus;
			}

			if( canFocus )
			{
				var focusOnEnable = EditorGUILayout.Toggle( "Auto Focus", control.AutoFocus );
				if( focusOnEnable != control.AutoFocus )
				{
					dfEditorUtil.MarkUndo( control, "Toggle FocusOnEnable property" );
					control.AutoFocus = focusOnEnable;
				}
			}

			var clips = EditorGUILayout.Toggle( "Clip Children", control.ClipChildren );
			if( clips != control.ClipChildren )
			{
				dfEditorUtil.MarkUndo( control, "Change control ClipChildren property" );
				control.ClipChildren = clips;
			}

			var allowSignal = EditorGUILayout.Toggle( "Signal Events", control.AllowSignalEvents );
			if( allowSignal != control.AllowSignalEvents )
			{
				dfEditorUtil.MarkUndo( control, "Toggle 'Allow Signal Events' property" );
				control.AllowSignalEvents = allowSignal;
			}

		}

		using( dfEditorUtil.BeginGroup( "Other" ) )
		{

			//var color = EditorGUILayout.ColorField( "Color", control.Color );
			//if( color != control.Color )
			//{
			//	dfEditorUtil.MarkUndo( control, "Change control Color" );
			//	control.Color = color;
			//}

			//var disabledColor = EditorGUILayout.ColorField( "Disabled Color", control.DisabledColor );
			//if( disabledColor != control.DisabledColor )
			//{
			//	dfEditorUtil.MarkUndo( control, "Change control Disabled Color" );
			//	control.DisabledColor = disabledColor;
			//}

			// NOTE: dfControl.Opacity is quantized to 255 levels
			EditorGUI.BeginChangeCheck();
			var opacity = EditorGUILayout.Slider( "Opacity", control.Opacity, 0, 1 );
			if( EditorGUI.EndChangeCheck() )
			{
				dfEditorUtil.MarkUndo( control, "Change control Opacity" );
				control.Opacity = opacity;
			}

			var controlGroup = getControlGroup( control );
			var maxIndex = controlGroup != null && controlGroup.Count > 0 
				? controlGroup.Max( x => x.ZOrder )
				: 0;

			var zorder = EditorGUILayout.IntSlider( "Z-Order", control.ZOrder, 0, maxIndex );
			if( zorder != control.ZOrder )
			{
				
				dfEditorUtil.MarkUndo( control, "Change control Z-Order" );

				if( control.Parent == null )
				{
					setZOrder( controlGroup, control, zorder );
				}
				else
				{
					control.ZOrder = zorder;
				}

			}

			var tabIndex = EditorGUILayout.IntField( "Tab Index", control.TabIndex );
			if( tabIndex != control.TabIndex )
			{
				dfEditorUtil.MarkUndo( control, "Change control Tab Index" );
				control.TabIndex = tabIndex;
			}

			var tooltip = EditorGUILayout.TextField( "Tooltip", control.Tooltip );
			if( tooltip != control.Tooltip )
			{
				dfEditorUtil.MarkUndo( control, "Change control Tooltip" );
				control.Tooltip = tooltip;
			}

		}

		EditorGUILayout.Separator();

		EditorGUILayout.BeginHorizontal();
		{

			if( GUILayout.Button( "Help" ) )
			{
				var url = "http://www.daikonforge.com/dfgui/tutorials/";
				Application.OpenURL( url );
				Debug.Log( "View online help at " + url );
			}

			if( GUILayout.Button( "Snap to Pixel" ) )
			{
				dfEditorUtil.MarkUndo( control, "Snap to pixel boundaries" );
				control.MakePixelPerfect();
			}

		}
		EditorGUILayout.EndHorizontal();

	}

	private bool canEditIsVisibleProperty( dfControl control )
	{

		if( control.Parent == null )
			return true;

		control = control.Parent;

		while( control != null )
		{
			
			var serialized = new SerializedObject( control );
			var property = serialized.FindProperty( "isVisible" ).boolValue;
			if( !property )
				return false;

			control = control.Parent;

		}

		return true;

	}

	private void setZOrder( dfList<dfControl> group, dfControl control, int zorder )
	{

		for( int i = 0; i < group.Count; i++ )
		{
			var other = group[ i ];
			if( other != control && other.ZOrder == zorder )
			{
				other.ZOrder = control.ZOrder;
				break;
			}
		}

		control.ZOrder = zorder;

		group.Sort();

		for( int i = 0; i < group.Count; i++ )
		{
			group[ i ].ZOrder = i;
		}

	}

	private dfList<dfControl> topControls = dfList<dfControl>.Obtain();
	private dfList<dfControl> getControlGroup( dfControl control )
	{

		if( control.transform.parent != null && control.Parent != null )
		{
			return control.Parent.Controls;
		}

		topControls.Clear();
		var top = control.GetManager().transform;

		for( int i = 0; i < top.childCount; i++ )
		{
			var childControl = top.GetChild( i ).GetComponent<dfControl>();
			if( childControl != null )
			{
				topControls.Add( childControl );
			}
		}

		return topControls;

	}

	protected virtual bool OnCustomInspector()
	{
		// Intended to be overridden
		return false;
	}

	protected bool isFoldoutExpanded( Dictionary<int, bool> list, string label )
	{
		return isFoldoutExpanded( list, label, true );
	}

	protected bool isFoldoutExpanded( Dictionary<int, bool> list, string label, bool defaultValue )
	{

		var isExpanded = defaultValue;
		var controlID = target.GetInstanceID();
		if( list.ContainsKey( controlID ) )
		{
			isExpanded = list[ controlID ];
		}
		isExpanded = EditorGUILayout.Foldout( isExpanded, label );
		list[ controlID ] = isExpanded;

		return isExpanded;

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

				var x = EditorGUILayout.IntField( label1, (int)value.x );
				var y = EditorGUILayout.IntField( label2, (int)value.y );

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

	protected Vector2 EditFloat2( string groupLabel, string label1, string label2, Vector2 value )
	{

		var retVal = Vector2.zero;

		var savedLabelWidth = dfEditorUtil.LabelWidth;

		GUILayout.BeginHorizontal();
		{

			EditorGUILayout.LabelField( groupLabel, "", GUILayout.Width( dfEditorUtil.LabelWidth - 12 ) );

			GUILayout.BeginVertical();
			{

				dfEditorUtil.LabelWidth = 60f;

				var x = EditorGUILayout.FloatField( label1, value.x );
				var y = EditorGUILayout.FloatField( label2, value.y );

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

	protected internal static void SelectPrefab<T>( string label, dfControl control, string propertyName, dfPrefabSelectionDialog.PreviewCallback previewCallback, dfPrefabSelectionDialog.FilterCallback filter ) where T : dfControl
	{

		var value = getValue( control, propertyName ) as dfControl;

		dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
		{
			var newValue = ( item == null ) ? null : item.GetComponent<T>();
			dfEditorUtil.MarkUndo( control, "Change " + ObjectNames.NicifyVariableName( propertyName ) );
			setValue( control, propertyName, newValue );
		};

		EditorGUILayout.BeginHorizontal();
		{

			EditorGUILayout.LabelField( label, "", GUILayout.Width( dfEditorUtil.LabelWidth - 5 ) );

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
						var draggedFont = draggedObject != null ? draggedObject.GetComponent<T>() : null;
						DragAndDrop.visualMode = ( draggedFont != null ) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
						if( evt.type == EventType.DragPerform )
						{
							selectionCallback( draggedObject );
						}
						evt.Use();
					}
				}
			}

			if( GUI.enabled && GUILayout.Button( new GUIContent( " ", "Edit" ), "IN ObjectField", GUILayout.Width( 14 ) ) )
			{
				dfEditorUtil.DelayedInvoke( (System.Action)( () =>
				{
					dfPrefabSelectionDialog.Show( "Select " + ObjectNames.NicifyVariableName( typeof( T ).Name ), typeof( T ), selectionCallback, previewCallback, filter );
				} ) );
			}

		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space( 2 );

	}

	protected internal static void SelectTextureAtlas( string label, dfControl control, string propertyName, bool readOnly, bool colorizeIfMissing )
	{

		var savedColor = GUI.color;

		try
		{

			var atlas = getValue( control, propertyName ) as dfAtlas;

			GUI.enabled = !readOnly;

			if( atlas == null && colorizeIfMissing )
				GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.red;

			dfPrefabSelectionDialog.SelectionCallback selectionCallback = delegate( GameObject item )
			{
				var newAtlas = ( item == null ) ? null : item.GetComponent<dfAtlas>();
				dfEditorUtil.MarkUndo( control, "Change Atlas" );
				setValue( control, propertyName, newAtlas );
			};

			var value = (dfAtlas)getValue( control, propertyName );

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
		finally
		{
			GUI.enabled = true;
			GUI.color = savedColor;
		}

	}

	protected internal static void SelectFontDefinition( string label, dfAtlas atlas, dfControl control, string propertyName, bool colorizeIfMissing )
	{
		SelectFontDefinition( label, atlas, control, propertyName, colorizeIfMissing, false );
	}

	protected internal static void SelectFontDefinition( string label, dfAtlas atlas, dfControl control, string propertyName, bool colorizeIfMissing, bool allowDynamicFonts )
	{

		var savedColor = GUI.color;

		try
		{

			var value = (dfFontBase)getValue( control, propertyName );

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
				
				dfEditorUtil.MarkUndo( control, "Change Font" );
				setValue( control, propertyName, font );

			};

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
							var draggedFont =
								( draggedObject != null )
								? draggedObject.GetComponent<dfFontBase>() 
								: null;

							var block = ( draggedFont is dfDynamicFont ) &&!allowDynamicFonts;

							DragAndDrop.visualMode = ( draggedFont != null && !block )
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
			EditorGUILayout.EndHorizontal();

			if( value is dfDynamicFont || ( value is dfFont && !dfAtlas.Equals( atlas, ( (dfFont)value ).Atlas ) ) )
			{
				GUI.color = Color.white;
				EditorGUILayout.HelpBox( "The specified font uses a different Material, which will result in an additional draw call.", MessageType.Warning );
			}

			GUILayout.Space( 2 );

		}
		finally
		{
			GUI.color = savedColor;
		}

	}

	protected internal static void SelectSprite( string label, dfAtlas atlas, dfControl control, string propertyName )
	{
		SelectSprite( label, atlas, control, propertyName, true );
	}

	protected internal static void SelectSprite( string label, dfAtlas atlas, dfControl control, string propertyName, bool colorizeIfMissing )
	{

		var savedColor = GUI.color;

		try
		{

			GUI.enabled = ( atlas != null );

			dfSpriteSelectionDialog.SelectionCallback callback = delegate( string spriteName )
			{
				dfEditorUtil.MarkUndo( control, "Change Sprite" );
				setValue( control, propertyName, spriteName );
			};

			var value = (string)getValue( control, propertyName );
			if( atlas == null || atlas[ value ] == null && colorizeIfMissing )
				GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.red;

			EditorGUILayout.BeginHorizontal();
			{

				EditorGUILayout.LabelField( label, "", GUILayout.Width( dfEditorUtil.LabelWidth - 6 ) );

				GUILayout.Space( 2 );

				var displayText = string.IsNullOrEmpty( value ) ? "[none]" : value;
				GUILayout.Label( displayText, "TextField" );

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

				if( GUI.enabled && GUILayout.Button( new GUIContent( " ", "Edit " + label ), "IN ObjectField", GUILayout.Width( 14 ) ) )
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
		finally
		{
			GUI.enabled = true;
			GUI.color = savedColor;
		}

	}

	private static void setValue( dfControl control, string propertyName, object value )
	{
		var property = control.GetType().GetProperty( propertyName );
		if( property == null )
			throw new ArgumentException( "Property '" + propertyName + "' does not exist on " + control.GetType().Name );
		property.SetValue( control, value, null );
	}

	private static object getValue( dfControl control, string propertyName )
	{
		var property = control.GetType().GetProperty( propertyName );
		if( property == null )
			throw new ArgumentException( "Property '" + propertyName + "' does not exist on " + control.GetType().Name );
		return property.GetValue( control, null );
	}

	private dfAnchorStyle EditAnchor( dfAnchorStyle value )
	{

		const float OPTION_WIDTH = 100f;
		var labelWidth = 85;// dfEditorUtil.LabelWidth;

		var retVal = value;

		using( dfEditorUtil.BeginGroup( "Anchor" ) )
		{

			GUILayout.BeginHorizontal();
			{

				EditorGUILayout.LabelField( "Anchor", "", GUILayout.Width( labelWidth ) );

				GUILayout.BeginVertical();
				{

					dfEditorUtil.LabelWidth = OPTION_WIDTH;

					var left = EditorGUILayout.Toggle( "Left", retVal.IsFlagSet( dfAnchorStyle.Left ) );
					var right = EditorGUILayout.Toggle( "Right", retVal.IsFlagSet( dfAnchorStyle.Right ) );
					var top = EditorGUILayout.Toggle( "Top", retVal.IsFlagSet( dfAnchorStyle.Top ) );
					var bottom = EditorGUILayout.Toggle( "Bottom", retVal.IsFlagSet( dfAnchorStyle.Bottom ) );

					retVal = retVal.SetFlag( dfAnchorStyle.Top, top );
					retVal = retVal.SetFlag( dfAnchorStyle.Left, left );
					retVal = retVal.SetFlag( dfAnchorStyle.Right, right );
					retVal = retVal.SetFlag( dfAnchorStyle.Bottom, bottom );

					if( top || bottom )
					{
						retVal = retVal.SetFlag( dfAnchorStyle.CenterVertical, false );
					}

					if( left || right )
					{
						retVal = retVal.SetFlag( dfAnchorStyle.CenterHorizontal, false );
					}

				}
				GUILayout.EndVertical();

				GUILayout.FlexibleSpace();

			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{

				EditorGUILayout.LabelField( "Center", "", GUILayout.Width( labelWidth ) );

				GUILayout.BeginVertical();
				{

					dfEditorUtil.LabelWidth = OPTION_WIDTH;

					var horz = EditorGUILayout.Toggle( "Horizontal", retVal.IsFlagSet( dfAnchorStyle.CenterHorizontal ) );
					var vert = EditorGUILayout.Toggle( "Vertical", retVal.IsFlagSet( dfAnchorStyle.CenterVertical ) );

					retVal = retVal.SetFlag( dfAnchorStyle.CenterHorizontal, horz );
					retVal = retVal.SetFlag( dfAnchorStyle.CenterVertical, vert );

					if( horz )
					{
						retVal = retVal.SetFlag( dfAnchorStyle.Left, false );
						retVal = retVal.SetFlag( dfAnchorStyle.Right, false );
					}

					if( vert )
					{
						retVal = retVal.SetFlag( dfAnchorStyle.Top, false );
						retVal = retVal.SetFlag( dfAnchorStyle.Bottom, false );
					}

				}
				GUILayout.EndVertical();

				GUILayout.FlexibleSpace();

			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{

				EditorGUILayout.LabelField( "Mode", "", GUILayout.Width( labelWidth ) );

				GUILayout.BeginVertical();
				{

					dfEditorUtil.LabelWidth = OPTION_WIDTH;

					var proportional = EditorGUILayout.Toggle( "Proportional", retVal.IsFlagSet( dfAnchorStyle.Proportional ) );
					retVal = retVal.SetFlag( dfAnchorStyle.Proportional, proportional );

				}
				GUILayout.EndVertical();

				GUILayout.FlexibleSpace();

			}
			GUILayout.EndHorizontal();

		}

		return retVal;

	}

	/// <summary>
	/// Hook to allow custom dfControl inspectors to perform a "default" action
	/// when the user double-clicks the control in the Editor
	/// </summary>
	/// <param name="control">The control that was double-clicked</param>
	/// <param name="evt">The event information</param>
	/// <returns>Returns TRUE if the event was processed, FALSE otherwise</returns>
	protected virtual bool OnControlDoubleClick( dfControl control, Event evt )
	{

		var camera = control.GetCamera();
		var mousePos = Event.current.mousePosition;
		var ray = HandleUtility.GUIPointToWorldRay( mousePos );
		var maxDistance = 1000f;

		var hits = Physics.RaycastAll( ray, maxDistance, camera.cullingMask );
		var controls = hits
			.Select( h => h.collider.GetComponent<dfControl>() )
			.Where( c => c != null )
			.OrderBy( c => c.RenderOrder )
			.ToList();

		if( controls.Count <= 1 )
			return false;

		var controlIndex = controls.FindIndex( c => c == control );
		if( controlIndex == -1 )
			return false;

		var focus = control;
		if( controlIndex > 0 )
			focus = controls[ controlIndex - 1 ];
		else
			focus = controls[ controls.Count - 1 ];

		GUIUtility.hotControl = GUIUtility.keyboardControl = 0;
		Selection.activeGameObject = focus.gameObject;
		SceneView.lastActiveSceneView.Repaint();

		return true;

	}

	#region Implementation of Resize and Move tools, guide snapping, etc 

	public virtual void OnSceneGUI()
	{

		var control = target as dfControl;
		if( control == null )
			return;

		var evt = Event.current;
		var id = GUIUtility.GetControlID( GetType().Name.GetHashCode(), FocusType.Passive );
		var eventType = evt.GetTypeForControl( id );

		var handleColor = Handles.color;
		var gizmoColor = Gizmos.color;

		// Clear the list of editor handles, which will be re-filled below
		handles.Clear();

		// Calculate the control's world-space corners and dereference
		var corners = control.GetCorners();
		var ulc = corners[ 0 ];
		var urc = corners[ 1 ];
		var llc = corners[ 2 ];
		var lrc = corners[ 3 ]; 

		// Render control outline
		Handles.color = Color.white;
		Handles.DrawLine( ulc, urc );
		Handles.DrawLine( llc, lrc );
		Handles.DrawLine( ulc, llc );
		Handles.DrawLine( urc, lrc );

		if( control.gameObject == Selection.activeGameObject )
		{
			createEditHandles( control, id, eventType, corners );
		}

		Handles.color = handleColor;
		Gizmos.color = gizmoColor;

		switch( eventType )
		{

			case EventType.repaint:
				drawActionHints( evt );
				break;

			case EventType.keyDown:
				if( evt.keyCode == KeyCode.Escape && currentAction != EditorAction.None )
				{

					showTool();

					currentAction = EditorAction.None;
					activeResizeHandle = dfPivotPoint.MiddleCenter;

					cancelEditAction();

					GUIUtility.hotControl = GUIUtility.keyboardControl = 0;
					evt.Use();

					dfGUIManager.RefreshAll();
					SceneView.RepaintAll();

				}
				else if( currentAction != EditorAction.None )
				{
					switch( currentAction )
					{
						case EditorAction.Move:
							doControlMoveAction( control, evt );
							break;
						case EditorAction.Resize:
							doControlResizeAction( control, evt );
							break;
						case EditorAction.Rotate:
							doControlRotateAction( control, evt );
							break;
					}
				}
				else if( evt.keyCode == KeyCode.G && evt.shift && evt.control )
				{
					var showGuides = EditorPrefs.GetBool( "dfGUIManager.ShowGuides", true );
					EditorPrefs.SetBool( "dfGUIManager.ShowGuides", !showGuides );
					SceneView.RepaintAll();
				}
				else
				{
					var mult = evt.control ? 10 : evt.shift ? 5 : 1;
					switch( evt.keyCode )
					{
						case KeyCode.LeftArrow:
							control.Position += Vector3.left * mult;
							evt.Use();
							break;
						case KeyCode.RightArrow:
							control.Position += Vector3.right * mult;
							evt.Use();
							break;
						case KeyCode.UpArrow:
							control.Position += Vector3.up * mult;
							evt.Use();
							break;
						case KeyCode.DownArrow:
							control.Position += Vector3.down * mult;
							evt.Use();
							break;
					}
				}
				break;

			case EventType.mouseDown:
				var modifierKeyPressed = evt.alt || evt.control || evt.shift;
				if( evt.button != 0 )
				{

					if( evt.button == 1 && !modifierKeyPressed )
					{
						displayContextMenu();
						evt.Use();
						return;
					}

					GUIUtility.hotControl = GUIUtility.keyboardControl = 0;

					break;

				}

				if( evt.clickCount == 2 )
				{
					if( OnControlDoubleClick( control, evt ) )
					{
						evt.Use();
						return;
					}
					break;
				}

				if( Tools.current != Tool.Move )
					return;

				// No resize handle currently selected
				activeResizeHandle = dfPivotPoint.MiddleCenter;

				for( int i = 0; i < handles.Count; i++ )
				{
					var handle = handles[ i ];
					if( handle.rect.Contains( evt.mousePosition ) )
					{

						currentAction = handle.action;
						if( currentAction == EditorAction.Resize )
						{
							activeResizeHandle = handle.pivot;
						}
						
						break;

					}
				}

				if( currentAction == EditorAction.Resize )
				{

					dragStartPosition = raycast( evt.mousePosition );
					dragStartSize = control.Size;
					dragAnchorPoint = handleLocations[ (int)getResizeAnchor( activeResizeHandle ) ];

					prepareEditAction( id );
					saveControlUndoInfo();
					hideTool();
					
					break;

				}

				if( currentAction == EditorAction.Rotate )
				{

					prepareEditAction( id );
					saveControlUndoInfo();
					hideTool();

					dragStartPosition = evt.mousePosition;
					dragStartAngle = control.transform.eulerAngles.z;

					break;

				}

				if(
					Selection.activeGameObject != control.gameObject ||
					Selection.gameObjects.Length != 1 ||
					( !isMouseOverControl( evt.mousePosition ) && !mouseIsOnToolHandle( evt.mousePosition ) )
					)
				{
					currentAction = EditorAction.None;
					break;
				}


				if( currentAction == EditorAction.Move )
				{

					prepareEditAction( id );
					saveControlUndoInfo();
					hideTool();

					if( control.transform.localEulerAngles.ClampRotation().magnitude <= float.Epsilon )
					{
						dragStartPosition = getVirtualScreenPosition( evt.mousePosition );
						control.GetHitPosition( HandleUtility.GUIPointToWorldRay( evt.mousePosition ), out dragCursorOffset );
					}
					else
					{
						dragStartPosition = raycast( evt.mousePosition );
						dragCursorOffset3 = dragStartPosition - control.transform.position;
					}

					break;

				}

				break;

			//case EventType.mouseMove:
			case EventType.mouseDrag:
				if( currentAction == EditorAction.Move )
				{
					ensureUndoInfoSaved( "Move control" );
					doControlMoveAction( control, evt );
					evt.Use();
				}
				else if( currentAction == EditorAction.Rotate )
				{
					ensureUndoInfoSaved( "Rotate control" );
					doControlRotateAction( control, evt );
					evt.Use();
				}
				else if( currentAction == EditorAction.Resize )
				{
					ensureUndoInfoSaved( "Resize control" );
					doControlResizeAction( control, evt );
					evt.Use();
				}
				break;

			case EventType.mouseUp:
				EditorGUIUtility.SetWantsMouseJumping( 0 );
				if( currentAction == EditorAction.Resize )
				{

					controlUndoData.Clear();

					var controls =
						Selection.gameObjects
						.Select( c => c.GetComponent<dfControl>() )
						.Where( c => c != null )
						.ToList();

					for( int i = 0; i < controls.Count; i++ )
					{

						control = controls[ i ];

						// Probably unnecessary, but it's for design time so I am putting
						// it in there "just in case"
						control.ResetLayout();

						// If the control's anchor includes centering, should give 
						// it a chance to update the layout
						control.PerformLayout();

					}

					activeResizeHandle = dfPivotPoint.MiddleCenter;
					SceneView.RepaintAll();

				}
				if( currentAction != EditorAction.None )
				{

					GUIUtility.hotControl = GUIUtility.keyboardControl = 0;
					resetSelectionLayouts();
					evt.Use();

					var virtualScreenPos = getVirtualScreenPosition( evt.mousePosition );
					if( Vector2.Distance( dragStartPosition, virtualScreenPos ) <= 5 )
					{
						selectObjectUnderMouse();
					}

				}
				else
				{
					resetSelectionLayouts();
				}

				activeResizeHandle = dfPivotPoint.MiddleCenter;
				showTool();
				currentAction = EditorAction.None;

				break;

		}

	}

	private void ensureUndoInfoSaved( string actionName )
	{
		if( !undoInformationSaved )
		{
			undoInformationSaved = true;
			dfEditorUtil.MarkUndo( target, actionName );
		}
	}

	private void prepareEditAction( int id )
	{

		undoInformationSaved = false;

		GUIUtility.hotControl = GUIUtility.keyboardControl = id;
		Event.current.Use();
		EditorGUIUtility.SetWantsMouseJumping( 1 );

		if( Event.current.type == EventType.Layout )
		{
			HandleUtility.AddDefaultControl( id );
		}

	}

	private void drawActionHints( Event evt )
	{

		var showControlSize = UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowControlExtents", true );
		if( showControlSize && ( Event.current.alt ^ currentAction == EditorAction.Resize ) )
		{
			// This has to be called after Handles.EndGUI() in order to render correctly
			drawControlDimensions();
		}

		var showHints = EditorPrefs.GetBool( "DaikonForge.ShowHints", true );
		if( !showHints )
			return;

		var control = target as dfControl;
		var screenWidth = SceneView.currentDrawingSceneView.camera.pixelWidth;
		var screenHeight = SceneView.currentDrawingSceneView.camera.pixelHeight;

		if( currentAction == EditorAction.Rotate && !evt.shift )
		{

			Handles.BeginGUI();

			var center = HandleUtility.WorldToGUIPoint( control.transform.position );
			var labelRect = new Rect( center.x - 20, center.y - 25, 40, 20 );
			GUI.Box( labelRect, GUIContent.none, dfEditorUtil.BoxStyleDark );

			labelRect.y -= 5;
			var angle = clampAngle( control.transform.localEulerAngles.z );
			EditorGUI.DropShadowLabel( labelRect, string.Format( "{0:0}", angle, "PreLabel" ) );

			Handles.EndGUI();

		}

		var hintRect = new Rect( 0, screenHeight - 24, screenWidth, 24 );
		GUI.Box( hintRect, GUIContent.none, dfEditorUtil.BoxStyleLight );
		hintRect.y += 18;
		GUI.Window( -1, hintRect, drawStatusFunc, GUIContent.none, (GUIStyle)"sv_iconselector_back" );

	}

	private static float clampAngle( float angle )
	{
		if( angle < 0f )
			return angle + ( 360f * (int)( ( angle / 360f ) + 1 ) );
		else if( angle > 360f )
			return angle - ( 360f * (int)( angle / 360f ) );
		else
			return angle;
	}

	private void drawStatusFunc( int id )
	{

		var evt = Event.current;
		var screenWidth = SceneView.currentDrawingSceneView.camera.pixelWidth;

		var showControlSize = UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowControlExtents", true );
		var showGrid = UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowGrid", false );
		var showGuides = UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowGuides", true );

		var statusMessage = "";

		if( currentAction == EditorAction.Rotate )
		{
			
			statusMessage = "CTRL - Snap rotation to 5 degrees";
			
			if( !evt.shift )
				statusMessage = append( statusMessage, "SHIFT - Hide angle info" );

			statusMessage = append( statusMessage, "ESC - Cancel Rotate" );

		}
		else if( currentAction == EditorAction.Move )
		{

			statusMessage = append( statusMessage, "Drag with mouse to move control" );

			if( showGuides )
				statusMessage = append( statusMessage, "CTRL - Snap to guides" );

			if( showGrid ) 
				statusMessage = append( statusMessage, "SHIFT - Snap to grid" );

			statusMessage = append( statusMessage, "ESC - Cancel Move" );

		}
		else if( activeResizeHandle != dfPivotPoint.MiddleCenter || currentAction == EditorAction.Resize )
		{

			statusMessage = append( statusMessage, "Drag handle with mouse to resize control" );

			//if( showGuides )
			//    statusMessage = append( statusMessage, "CTRL - Snap to guides" );

			//if( showGrid ) 
			//    statusMessage = append( statusMessage, "SHIFT - Snap to grid" );

			if( showControlSize && !evt.alt )
				statusMessage = append( statusMessage, "ALT - Hide control size" );

			if(
				activeResizeHandle == dfPivotPoint.TopLeft ||
				activeResizeHandle == dfPivotPoint.TopRight ||
				activeResizeHandle == dfPivotPoint.BottomLeft ||
				activeResizeHandle == dfPivotPoint.BottomRight
				)
				statusMessage = append( statusMessage, "SHIFT - Keep aspect ratio" );

			statusMessage = append( statusMessage, "ESC - Cancel Resize" );

		}
		else
		{
			
			statusMessage = "Right-click the control for a context menu, drag handles to resize, drag center to move";

			if( showControlSize && !evt.alt )
				statusMessage = append( statusMessage, "ALT - Show control size" );

		}

		var hintRect = new Rect( 0, 2, screenWidth, 20 );
		EditorGUI.DropShadowLabel( hintRect, statusMessage, "PreLabel" );

		if( currentAction != EditorAction.None )
		{
			var actionRect = new Rect( 5, 2, 85, 20 );
			EditorGUI.DropShadowLabel( actionRect, currentAction.ToString(), (GUIStyle)"GUIEditor.BreadcrumbLeft" );
		}

	}

	private string append( string value, string message )
	{
		if( value.Length > 0 )
			return value + ", " + message;
		else
			return message;
	}

	private Vector3 getMidPoint( Vector3 lhs, Vector3 rhs )
	{
		return lhs + (rhs - lhs) * 0.5f;
	}

	private void createEditHandles( dfControl control, int id, EventType eventType, Vector3[] corners )
	{

		// Does not currently support moving/resizing/rotating multiple object simultaneously
		if( Selection.objects.Length > 1 )
			return;

		#region Do not create edit handles if the object is not visible in the Scene View

		var sceneView = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;
		if( sceneView == null )
			return;

		var frustum = GeometryUtility.CalculateFrustumPlanes( sceneView.camera );
		if( !GeometryUtility.TestPlanesAABB( frustum, control.GetBounds() ) )
			return;

		#endregion

		var ulc = corners[ 0 ];
		var urc = corners[ 1 ];
		var llc = corners[ 2 ];
		var lrc = corners[ 3 ];
		
		var style = (GUIStyle)"U2D.dragDot";
		Handles.BeginGUI();

		// Create corner resize handles
		createResizeHandle( id, ulc, style, dfPivotPoint.TopLeft );
		createResizeHandle( id, urc, style, dfPivotPoint.TopRight );
		createResizeHandle( id, llc, style, dfPivotPoint.BottomLeft );
		createResizeHandle( id, lrc, style, dfPivotPoint.BottomRight );

		// Create mid-edge resize handles
		if( !Event.current.shift )
		{

			const int MIN_DISTANCE = 12;

			var left = getMidPoint( ulc, llc );
			var right = getMidPoint( urc, lrc );
			if( screenDistance( left, ulc ) > MIN_DISTANCE )
			{
				createResizeHandle( id, left, style, dfPivotPoint.MiddleLeft );
				createResizeHandle( id, right, style, dfPivotPoint.MiddleRight );
			}

			var top = getMidPoint( ulc, urc );
			var bottom = getMidPoint( llc, lrc );
			if( screenDistance( top, ulc ) > MIN_DISTANCE )
			{
				createResizeHandle( id, top, style, dfPivotPoint.TopCenter );
				createResizeHandle( id, bottom, style, dfPivotPoint.BottomCenter );
			}

		}

		if( eventType == EventType.repaint && currentAction == EditorAction.Rotate )
		{
			// Render pivot point
			var styleRotate = (GUIStyle)"sv_label_0";
			var point = HandleUtility.WorldToGUIPoint( control.transform.position );
			var rect = new Rect( point.x - 7, point.y - 7, 14, 14 );
			styleRotate.Draw( rect, GUIContent.none, id, true );
		}
		else if( currentAction == EditorAction.None || currentAction == EditorAction.Rotate )
		{

			var center = control.GetCenter();
			var distance = control.GetManager().PixelsToUnits() * 7;
			var controlPosition = control.transform.position;

			// Create rotate handles
			for( int i = 0; i < 4; i++ )
			{
				if( Vector3.Distance( corners[ i ], controlPosition ) > float.Epsilon )
				{
					var rotateHandlePos = corners[ i ] + ( corners[ i ] - center ).normalized * distance;
					addEditorHandle( id, rotateHandlePos, null, MouseCursor.RotateArrow, EditorAction.Rotate );
				}
			}

		}

		if( currentAction == EditorAction.None )
		{
			createControlMoveHandle( control );
		}

		Handles.EndGUI();

	}

	private float screenDistance( Vector3 lhs, Vector3 rhs )
	{
		return Vector2.Distance(
			HandleUtility.WorldToGUIPoint( lhs ),
			HandleUtility.WorldToGUIPoint( rhs )
		);
	}

	private void createResizeHandle( int id, Vector3 location, GUIStyle style, dfPivotPoint pivot )
	{
		addEditorHandle( id, location, style, getResizeCursor( pivot ), EditorAction.Resize ).pivot = pivot;
		handleLocations[ (int)pivot ] = location;
	}

	private MouseCursor getResizeCursor( dfPivotPoint pivot )
	{

		var control = target as dfControl;
		var rot = control.transform.localEulerAngles;
		if( rot.ClampRotation().magnitude > float.Epsilon )
			return MouseCursor.ScaleArrow;

		if( pivot == dfPivotPoint.MiddleLeft || pivot == dfPivotPoint.MiddleRight )
			return MouseCursor.ResizeHorizontal;

		if( pivot == dfPivotPoint.TopCenter || pivot == dfPivotPoint.BottomCenter )
			return MouseCursor.ResizeVertical;

		if( pivot == dfPivotPoint.TopLeft || pivot == dfPivotPoint.BottomRight )
			return MouseCursor.ResizeUpLeft;

		if( pivot == dfPivotPoint.BottomLeft || pivot == dfPivotPoint.TopRight )
			return MouseCursor.ResizeUpRight;

		return MouseCursor.ScaleArrow;

	}

	private void createControlMoveHandle( dfControl control )
	{
	
		if( Selection.gameObjects.Length != 1 )
			return;

		var worldCorners = control.GetCorners();
		Vector2 min = Vector2.one * float.MaxValue;
		Vector2 max = Vector2.one * float.MinValue;
		for( int i = 0; i < 4; i++ )
		{
			var point = HandleUtility.WorldToGUIPoint( worldCorners[ i ] );
			min = Vector2.Min( min, point );
			max = Vector2.Max( max, point );
		}

		var rect = new Rect( min.x, min.y, max.x - min.x, max.y - min.y );
		var test = rect; test.x += 20; test.y += 20; test.width -= 40; test.height -= 40;
		var mousePosition = Event.current.mousePosition;
		if( control.HitTest( HandleUtility.GUIPointToWorldRay( mousePosition ) ) )
		{
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.MoveArrow );
			handles.Add( new EditorHandle() { action = EditorAction.Move, rect = rect } );
		}
		else
		{
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Arrow );
		}

		if( currentAction == EditorAction.None && Event.current.type == EventType.MouseMove )
		{
			// Apparently Event.Use() is required to actually update the damned mouse cursor
			Event.current.Use();
		}

	}

	private EditorHandle addEditorHandle( int id, Vector3 worldPoint, GUIStyle style, MouseCursor cursor, EditorAction action )
	{

		var guiPoint = HandleUtility.WorldToGUIPoint( worldPoint );

		var rect = new Rect( guiPoint.x - 10, guiPoint.y - 10, 20, 20 );
		if( style != null )
		{

			float fixedWidth = style.fixedWidth;
			float fixedHeight = style.fixedHeight;

			rect = new Rect( guiPoint.x - fixedWidth * 0.5f, guiPoint.y - fixedHeight * 0.5f, fixedWidth, fixedHeight );

			dfEditorUtil.DrawHandle( id, worldPoint, style );

		}

		EditorGUIUtility.AddCursorRect( rect, cursor );

		var newHandle = new EditorHandle() { action = action, rect = rect, position = worldPoint };
		handles.Add( newHandle );

		return newHandle;

	}

	private void doControlRotateAction( dfControl control, Event evt )
	{

		var mousePosition = evt.mousePosition;
		var controlPos = HandleUtility.WorldToGUIPoint( control.transform.position );
		var startDirection = (Vector2)dragStartPosition - controlPos;
		var currentDirection = mousePosition - controlPos;
		float angle = -Vector3.Angle( startDirection, currentDirection );

		float dot = Vector3.Dot( Vector3.Cross( startDirection, currentDirection ), control.transform.forward );
		if( dot < 0f ) 
			angle = -angle;

		angle += dragStartAngle;

		if( evt.control )
		{
			angle = angle.Quantize( 5 );
		}

		var euler = control.transform.localEulerAngles;
		euler.z = angle;
		control.transform.localEulerAngles = euler;

	}

	private void doControlMoveAction( dfControl control, Event evt )
	{

		if( control.transform.localEulerAngles.ClampRotation().magnitude > float.Epsilon )
		{
			var current = raycast( evt.mousePosition );
			control.transform.position = current - dragCursorOffset3;
			control.Invalidate();

			return;

		}

		var guiMousePos = getVirtualScreenPosition( evt.mousePosition, false );
		var controlPosition = guiMousePos - dragCursorOffset;

		if( evt.control )
		{

			var verticalGuideLeft = getGuideSnapPosition( controlPosition.x, dfControlOrientation.Vertical );
			if( verticalGuideLeft > -1 )
			{
				controlPosition.x = verticalGuideLeft;
			}
			else
			{
				var verticalGuideRight = getGuideSnapPosition( controlPosition.x + control.Width, dfControlOrientation.Vertical );
				if( verticalGuideRight > -1 )
				{
					controlPosition.x = verticalGuideRight - control.Width;
				}
				else
				{
					var verticalGuideCenter = getGuideSnapPosition( controlPosition.x + control.Width * 0.5f, dfControlOrientation.Vertical );
					if( verticalGuideCenter > -1 )
					{
						controlPosition.x = verticalGuideCenter - control.Width * 0.5f;
					}
				}
			}

			var horizontalGuideTop = getGuideSnapPosition( controlPosition.y, dfControlOrientation.Horizontal );
			if( horizontalGuideTop > -1 )
			{
				controlPosition.y = horizontalGuideTop;
			}
			else
			{
				var horizontalGuideBottom = getGuideSnapPosition( controlPosition.y + control.Height, dfControlOrientation.Horizontal );
				if( horizontalGuideBottom > -1 )
				{
					controlPosition.y = horizontalGuideBottom - control.Height;
				}
				else
				{
					var horizontalGuideCenter = getGuideSnapPosition( controlPosition.y + control.Height * 0.5f, dfControlOrientation.Horizontal );
					if( horizontalGuideCenter > -1 )
					{
						controlPosition.y = horizontalGuideCenter - control.Height * 0.5f;
					}
				}
			}

		}
		else if( evt.shift )
		{
			var gridSize = EditorPrefs.GetInt( "dfGUIManager.GridSize", 25 );
			controlPosition = controlPosition.Quantize( gridSize );
		}

		Vector3 offset = ( control.Parent == null ) ? Vector3.zero : control.Parent.GetAbsolutePosition();
		control.RelativePosition = (Vector3)controlPosition - offset;

	}

	/// <summary>
	/// Returns a "snapped" value if the given position is within a small tolerance of
	/// any grid line, and the value (-1) otherwise
	/// </summary>
	private float getGridSnapPosition( float position )
	{

		var gridSize = EditorPrefs.GetInt( "dfGUIManager.GridSize", 25 );

		// HACK: Currently, snapping to grid when resizing vertically works
		// horribly when the grid size is less than 20. Will remove this hack
		// when I've got the time to resolve the issue at the source
		if( gridSize < 20 )
			return -1;

		var snapped = position.RoundToNearest( gridSize );
		if( Mathf.Abs( snapped - position ) < 5 )
			return snapped;

		return -1;

	}

	private float getGuideSnapPosition( float position, dfControlOrientation orientation )
	{
		return getGuideSnapPosition( position, orientation, 5 );
	}

	/// <summary>
	/// Returns a "snapped" value if the given position is within a small tolerance of
	/// any active design guides, and the value (-1) otherwise
	/// </summary>
	private float getGuideSnapPosition( float position, dfControlOrientation orientation, int snapDistance )
	{

		var showGuides = EditorPrefs.GetBool( "dfGUIManager.ShowGuides", true );
		if( !showGuides )
			return float.MinValue;

		// Only allow snap-to-guide in orthographic mode
		if( SceneView.currentDrawingSceneView != null )
		{
			if( SceneView.currentDrawingSceneView.camera != null )
			{
				if( !SceneView.currentDrawingSceneView.camera.isOrthoGraphic )
				{
					return float.MinValue;
				}
			}
		}

		var manager = ( (dfControl)target ).GetManager();
		var guides = manager.guides;

		var closestGuide = (dfDesignGuide)null;
		var closestDistance = float.MaxValue;
		for( int i = 0; i < guides.Count; i++ )
		{
			var guide = guides[ i ];
			if( guide.orientation == orientation )
			{
				var distance = Mathf.Abs( guide.position - position );
				if( distance < closestDistance )
				{
					closestDistance = distance;
					closestGuide = guide;
				}
			}
		}

		if( closestDistance <= snapDistance )
		{
			return closestGuide.position;
		}

		if( Mathf.Abs( position ) < snapDistance )
			return 0;

		if( orientation == dfControlOrientation.Vertical )
		{
			if( Mathf.Abs( position - manager.FixedWidth ) < snapDistance )
				return manager.FixedWidth;
		}
		else
		{
			if( Mathf.Abs( position - manager.FixedHeight ) < snapDistance )
				return manager.FixedHeight;
		}

		return float.MinValue;

	}

	private bool mouseIsOnToolHandle( Vector2 mousePosition )
	{
		var screenPosition = HandleUtility.WorldToGUIPoint( Tools.handlePosition );
		return Vector2.Distance( screenPosition, mousePosition ) <= 25;
	}

	private void showTool()
	{
		Tools.current = lastTool != Tool.None ? lastTool : Tool.Move;
	}

	private void hideTool()
	{
		lastTool = Tools.current;
		Tools.current = Tool.None;
	}

	private void resetSelectionLayouts()
	{

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.OrderByDescending( c => c.RenderOrder )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{

			var controlComponent = controls[ i ];
			if( controlComponent != null )
			{
				
				var anchor = controlComponent.Anchor;
				if( anchor.IsAnyFlagSet( dfAnchorStyle.CenterHorizontal | dfAnchorStyle.CenterVertical ) )
				{
					controlComponent.PerformLayout();
				}

				controlComponent.ResetLayout( false, true );
				EditorUtility.SetDirty( controlComponent );

			}

		}

	}

	private Vector2 virtualScreenToSceneView( Vector2 position )
	{

		var control = this.target as dfControl;

		var manager = control.GetManager();
		var transform = manager.transform;
		var origin = manager.GetCorners()[ 0 ];
		var p2u = manager.PixelsToUnits();
		var worldPosition = origin + ( transform.rotation * position.Scale( 1, -1 ) * p2u );

		return HandleUtility.WorldToGUIPoint( worldPosition );

	}

	private Vector2 getVirtualScreenPosition( Vector2 position )
	{
		return getVirtualScreenPosition( position, true );
	}

	private Vector2 getVirtualScreenPosition( Vector2 position, bool clamp )
	{

		position.y = Camera.current.pixelHeight - position.y;

		var control = this.target as dfControl;
		var manager = control.GetManager();

		var ray = Camera.current.ScreenPointToRay( position );
		var corner = manager.GetCorners()[ 0 ];
		var plane = new Plane( manager.transform.TransformDirection( Vector3.forward ), corner );

		var distance = 0f;
		if( !plane.Raycast( ray, out distance ) )
			return position;

		var hit = ray.GetPoint( distance );

		var virtualScreenPos = ( ( hit - corner ) / manager.PixelsToUnits() );

		if( clamp )
		{
			virtualScreenPos.y = Mathf.Min( manager.FixedHeight, Mathf.Max( 0, -virtualScreenPos.y ) );
			virtualScreenPos.x = Mathf.Min( manager.FixedWidth, Mathf.Max( 0, virtualScreenPos.x ) );
		}
		else
		{
			virtualScreenPos.y *= -1;
		}

		return virtualScreenPos;

	}

	private void saveControlUndoInfo()
	{

		this.controlUndoData.Clear();

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{
			controlUndoData.Add( new OperationUndoInfo( controls[ i ] ) );
		}

	}

	private void cancelEditAction()
	{

		for( int i = 0; i < controlUndoData.Count; i++ )
		{
			controlUndoData[ i ].UndoResize();
		}

		controlUndoData.Clear();
		undoInformationSaved = false;

	}

	private void selectObjectUnderMouse()
	{

		var ray = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );

		var hits = Physics.RaycastAll( ray, 1000f, 1 << ( (dfControl)target ).gameObject.layer );
		var controlClicked = hits
			.Select( h => h.collider.GetComponent<dfControl>() )
			.Where( c => c != null )
			.OrderByDescending( c => c.RenderOrder )
			.FirstOrDefault();

		if( controlClicked == null )
			return;

		Selection.activeGameObject = controlClicked.gameObject;

	}

	private void doMultiSelectRaycast()
	{

		var ray = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );

		var hits = Physics.RaycastAll( ray, 1000f, 1 << ( (dfControl)target ).gameObject.layer );
		var controlClicked = hits
			.Select( h => h.collider.GetComponent<dfControl>() )
			.Where( c => c != null )
			.OrderByDescending( c => c.RenderOrder )
			.FirstOrDefault();

		if( controlClicked == null )
			return;

		var selectedObjects = Selection.objects.ToList();
		if( selectedObjects.Contains( controlClicked ) )
			selectedObjects.Remove( controlClicked );
		else
			selectedObjects.Add( controlClicked );

		Selection.objects = selectedObjects.ToArray();

	}

	private bool isMouseOverControl( Vector2 mousePos )
	{

		var control = target as dfControl;

		var gameObj = HandleUtility.PickGameObject( mousePos, true );
		if( gameObj == null )
			return false;

		return gameObj.transform.IsChildOf( control.transform );

	}

	private void doControlResizeAction( dfControl control, Event evt )
	{

		var mousePosition = evt.mousePosition;
		var anchor = getResizeAnchor( activeResizeHandle );

		var mouseGUIPos = getVirtualScreenPosition( mousePosition, false );
		mousePosition = virtualScreenToSceneView( mouseGUIPos );

		var p2u = control.GetManager().PixelsToUnits();
		var worldToLocal = control.transform.worldToLocalMatrix;
		var mouseWorldPos = raycast( mousePosition );

		var localDelta = (
			worldToLocal.MultiplyPoint( mouseWorldPos ) -
			worldToLocal.MultiplyPoint( dragStartPosition )
		).Scale( 1, -1, 1 ) / p2u;

		var size = control.Size;

		localDelta = localDelta.RoundToInt();

		switch( activeResizeHandle )
		{
			case dfPivotPoint.TopLeft:
				size.x = dragStartSize.x - localDelta.x;
				size.y = dragStartSize.y - localDelta.y;
				break;
			case dfPivotPoint.TopCenter:
				size.y = dragStartSize.y - localDelta.y;
				break;
			case dfPivotPoint.TopRight:
				size.x = dragStartSize.x + localDelta.x;
				size.y = dragStartSize.y - localDelta.y;
				break;
			case dfPivotPoint.MiddleLeft:
				size.x = dragStartSize.x - localDelta.x;
				break;
			case dfPivotPoint.MiddleRight:
				size.x = dragStartSize.x + localDelta.x;
				break;
			case dfPivotPoint.BottomLeft:
				size.x = dragStartSize.x - localDelta.x;
				size.y = dragStartSize.y + localDelta.y;
				break;
			case dfPivotPoint.BottomCenter:
				size.y = dragStartSize.y + localDelta.y;
				break;
			case dfPivotPoint.BottomRight:
				size.x = dragStartSize.x + localDelta.x;
				size.y = dragStartSize.y + localDelta.y;
				break;
			default:
				return;
		}

		// Controls with centered pivots need to keep even dimensions
		var pivot = control.Pivot;
		if( pivot == dfPivotPoint.TopCenter || pivot == dfPivotPoint.BottomCenter || pivot == dfPivotPoint.MiddleCenter )
		{
			size.x = size.x.RoundToNearest( 2 );
		}
		if( pivot == dfPivotPoint.MiddleLeft || pivot == dfPivotPoint.MiddleRight || pivot == dfPivotPoint.MiddleCenter )
		{
			size.y = size.y.RoundToNearest( 2 );
		}

		// Attempt to account for size constraints (may not be final, the control may enforce other constraints)
		var minimumSize = control.CalculateMinimumSize();
		var maximumSize = control.MaximumSize.magnitude <= float.Epsilon ? Vector2.one * float.MaxValue : control.MaximumSize;
		size = Vector2.Max( minimumSize, Vector2.Min( size, maximumSize ) );

		// Retain aspect ratio while scaling
		if( !evt.control && evt.shift )
		{

			var canKeepAspect =
				activeResizeHandle == dfPivotPoint.TopLeft ||
				activeResizeHandle == dfPivotPoint.TopRight ||
				activeResizeHandle == dfPivotPoint.BottomLeft ||
				activeResizeHandle == dfPivotPoint.BottomRight;

			if( canKeepAspect )
			{

				var percentWidth = size.x / dragStartSize.x;
				var percentHeight = size.y / dragStartSize.y;
				var percent = percentHeight < percentWidth ? percentHeight : percentWidth;

				size.x = ( dragStartSize.x * percent );
				size.y = ( dragStartSize.y * percent );

				if( size.y <= minimumSize.y || size.x <= minimumSize.x )
					return;

			}

		}

		// Assign the new size to the control. Since the control may constrain the
		// size, get the actual size back for positioning.
		control.Size = Vector2.Max( size, Vector2.zero );
		size = control.Size;

		var anchorOffset = Vector2.one - anchor.AsOffset();
		var relativeOffset = anchorOffset.Scale( size.x, -size.y ) - pivot.AsOffset().Scale( size.x, -size.y );

		var rotation = control.transform.rotation;
		var controlPosition = dragAnchorPoint - rotation * relativeOffset * p2u;

		Vector3 offset = Vector3.zero;
		switch( activeResizeHandle )
		{

			case dfPivotPoint.TopLeft:
				offset = rotation * size.Scale( 1, -1 );
				break;
			case dfPivotPoint.TopCenter:
				offset = rotation * size.Scale( 0, -1 );
				break;
			case dfPivotPoint.TopRight:
				offset = rotation * size.Scale( -1, -1 );
				break;
			
			case dfPivotPoint.MiddleLeft:
				offset = rotation * size.Scale( 1, 0 );
				break;
			case dfPivotPoint.MiddleRight:
				offset = rotation * size.Scale( -1, 0 );
				break;

			case dfPivotPoint.BottomLeft:
				offset = rotation * size.Scale( 1, 1 );
				break;
			case dfPivotPoint.BottomCenter:
				offset = rotation * size.Scale( 0, 1 );
				break;
			case dfPivotPoint.BottomRight:
				offset = rotation * size.Scale( -1, 1 );
				break;

		}

		control.transform.position = controlPosition - offset * p2u;
		control.Invalidate();

	}

	/// <summary>
	/// Returns the pivot point that is *opposite* of the value passed in <paramref name="point"/>,
	/// which is used as the "anchor" point during control resizing
	/// </summary>
	private dfPivotPoint getResizeAnchor( dfPivotPoint pivot )
	{

		switch( pivot )
		{
			case dfPivotPoint.TopLeft:
				return dfPivotPoint.BottomRight;
			case dfPivotPoint.TopCenter:
				return dfPivotPoint.BottomCenter;
			case dfPivotPoint.TopRight:
				return dfPivotPoint.BottomLeft;
			case dfPivotPoint.MiddleLeft:
				return dfPivotPoint.MiddleRight;
			case dfPivotPoint.MiddleCenter:
				return pivot;
			case dfPivotPoint.MiddleRight:
				return dfPivotPoint.MiddleLeft;
			case dfPivotPoint.BottomLeft:
				return dfPivotPoint.TopRight;
			case dfPivotPoint.BottomCenter:
				return dfPivotPoint.TopCenter;
			case dfPivotPoint.BottomRight:
				return dfPivotPoint.TopLeft;
			default:
				throw new InvalidDataException( "Unknown pivot type" );
		}

	}

	private Vector2 snapToNearestGridPosition( Vector2 point )
	{

		var gridSize = EditorPrefs.GetInt( "dfGUIManager.GridSize", 25 );

		var guiPosition = getVirtualScreenPosition( point );
		var difference = point - guiPosition;

		return roundToNearest( guiPosition + difference, gridSize );

	}

	private Vector2 snapToNearestGuidePosition( Vector2 point )
	{
		return snapToNearestGuidePosition( point, 10 );
	}

	private Vector2 snapToNearestGuidePosition( Vector2 point, int snapDistance )
	{

		var snapX = getGuideSnapPosition( point.x, dfControlOrientation.Vertical, snapDistance );
		if( snapX > -1 ) point.x = snapX;

		var snapY = getGuideSnapPosition( point.y, dfControlOrientation.Horizontal, snapDistance );
		if( snapY > -1 ) point.y = snapY;

		return point;

	}

	private void drawControlDimensions()
	{

		var showHints = EditorPrefs.GetBool( "DaikonForge.ShowHints", true );
		if( !showHints )
			return;

		var showExtents = EditorPrefs.GetBool( "dfGUIManager.ShowControlExtents", true );
		if( !showExtents )
			return;

		const int LINE_OFFSET = 15;
		const int END_SIZE = 5;

		var control = target as dfControl;

		var p2u = control.GetManager().PixelsToUnits();
		var corners = control.GetCorners();
		var plane = new Plane( corners[ 0 ], corners[ 1 ], corners[ 3 ] );

		var up = ( corners[ 0 ] - corners[ 2 ] ).normalized * p2u;
		var left = ( corners[ 0 ] - corners[ 1 ] ).normalized * p2u;

		var v0 = corners[ 0 ] + up * LINE_OFFSET;
		var v1 = corners[ 1 ] + up * LINE_OFFSET;

		drawShadowedLine3D( plane, v0, v1 );
		drawShadowedLine3D( plane, v0 + up * END_SIZE, v0 + up * -END_SIZE );
		drawShadowedLine3D( plane, v1 + up * END_SIZE, v1 + up * -END_SIZE );

		var v2 = corners[ 0 ] + left * LINE_OFFSET;
		var v3 = corners[ 2 ] + left * LINE_OFFSET;

		drawShadowedLine3D( plane, v2, v3 );
		drawShadowedLine3D( plane, v2 + left * END_SIZE, v2 + left * -END_SIZE );
		drawShadowedLine3D( plane, v3 + left * END_SIZE, v3 + left * -END_SIZE );

		Handles.BeginGUI();

		var center = HandleUtility.WorldToGUIPoint( v0 + ( v1 - v0 ) * 0.5f );
		var labelRect = new Rect( center.x - 20, center.y - 10, 40, 20 );
		GUI.Box( labelRect, "", dfEditorUtil.BoxStyleDark );
		EditorGUI.DropShadowLabel( labelRect, ( (int)control.Width ).ToString(), "PreLabel" );

		center = HandleUtility.WorldToGUIPoint( v2 + ( v3 - v2 ) * 0.5f );
		labelRect = new Rect( center.x - 20, center.y - 10, 40, 20 );
		GUI.Box( labelRect, "", dfEditorUtil.BoxStyleDark );
		EditorGUI.DropShadowLabel( labelRect, ( (int)control.Height ).ToString(), "PreLabel" );

		Handles.EndGUI();

	}

	private void drawShadowedLine3D( Plane plane, Vector3 from, Vector3 to )
	{
		var guiFrom = HandleUtility.WorldToGUIPoint( from );
		var guiTo = HandleUtility.WorldToGUIPoint( to );
		drawShadowedLine( plane, guiFrom, guiTo );
	}

	private void drawShadowedLine( Plane plane, Vector2 from, Vector2 to )
	{

		var saved = Handles.color;

		Handles.color = Color.black;
		drawScreenLine( plane, from + Vector2.one, to + Vector2.one );

		Handles.color = saved;
		drawScreenLine( plane, from, to );

	}

	private void drawScreenLine( Plane plane, Vector2 from, Vector2 to )
	{

		var dist = 0f;

		var ray = HandleUtility.GUIPointToWorldRay( from );
		plane.Raycast( ray, out dist );
		var fromWorld = ray.GetPoint( dist );

		ray = HandleUtility.GUIPointToWorldRay( to );
		plane.Raycast( ray, out dist );
		var toWorld = ray.GetPoint( dist );

		Handles.DrawLine( fromWorld, toWorld );

	}

	private Vector2 roundToNearest( Vector2 pos, float gridSize )
	{
		pos.x = pos.x.RoundToNearest( gridSize );
		pos.y = pos.y.RoundToNearest( gridSize );
		return pos;
	}

	private Vector3 roundToNearest( Vector3 pos, float gridSize )
	{
		pos.x = pos.x.RoundToNearest( gridSize );
		pos.y = pos.y.RoundToNearest( gridSize );
		pos.z = pos.z.RoundToNearest( gridSize );
		return pos;
	}

	private Vector3 raycast( Vector2 mousePos )
	{

		var control = target as dfControl;

		var plane = new Plane( control.transform.rotation * Vector3.back, control.transform.position );
		var ray = HandleUtility.GUIPointToWorldRay( mousePos );

		var distance = 0f;
		plane.Raycast( ray, out distance );

		return ray.GetPoint( distance );

	}

	#endregion

	#region Context menu 

	protected virtual void FillContextMenu( List<ContextMenuItem> menu )
	{

		// If this function is overridden the derived class may have added
		// items to the beginning of the menu that can be assumed to be 
		// specific to the control's class, so add a seperator automatically
		if( menu.Count > 0 )
			menu.Add( new ContextMenuItem() { MenuText = "-" } );

		if( Selection.objects.Length == 1 )
		{

			// Adds a menu item for each dfControl class in the assembly that has 
			// an AddComponentMenu attribute defined.
			addContextMenuChildControls( menu );

			// Adds a menu item for each Tween class in the assembly that has 
			// an AddComponentMenu attribute defined.
			addContextTweens( menu );

			// Adds a menu item for each IDataBindingComponent class in the assembly that has 
			// an AddComponentMenu attribute defined.
			addContextDatabinding( menu );

			// Add an option to allow the user to select any Prefab that 
			// has a dfControl component as the main component
			addContextSelectPrefab( menu );

			// Add a "Create Script" menu item to display the "Create Script" dialog
			menu.Add( new ContextMenuItem() { MenuText = "Create Script...", Handler = ( selectedControl ) =>
			{
				dfScriptWizard.CreateScript( selectedControl );
			}});

			menu.Add( new ContextMenuItem() { MenuText = "-" } );

			addContextControlOrder( menu );
			menu.Add( new ContextMenuItem() { MenuText = "-" } );

			// Add a menu option to select any control under the cursor
			addContextSelectControl( menu );

			// Add standard menu options
			addContextCenter( menu );
			addContextFill( menu );
			addContextDock( menu );

		}
		else
		{
			addContextMultiEdit( menu );
		}

	}

	private void addContextMultiEdit( List<ContextMenuItem> menu )
	{
		addContextMultiAlignEdges( menu );
		addContextMultiAlignCenters( menu );
		addContextMultiDistribute( menu );
		addContextMultiSameSize( menu );
	}

	private void addContextMultiSameSize( List<ContextMenuItem> menu )
	{

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Make Same Size/Horizontally",
			Handler = ( control ) => { makeSameSizeHorizontally(); }
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Make Same Size/Vertically",
			Handler = ( control ) => { makeSameSizeVertically(); }
		} );

	}

	private void addContextMultiDistribute( List<ContextMenuItem> menu )
	{

		targetObjectCount =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.Count();

		if( targetObjectCount <= 2 )
			return;

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Distribute/Horizontally",
			Handler = ( control ) => { distributeControlsHorizontally(); }
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Distribute/Vertically",
			Handler = ( control ) => { distributeControlsVertically(); }
		} );

	}

	private void addContextMultiAlignEdges( List<ContextMenuItem> menu )
	{

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Align Edges/Left",
			Handler = ( control ) => { alignEdgeLeft(); }
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Align Edges/Right",
			Handler = ( control ) => { alignEdgeRight(); }
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Align Edges/Top",
			Handler = ( control ) => { alignEdgeTop(); }
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Align Edges/Bottom",
			Handler = ( control ) => { alignEdgeBottom(); }
		} );

	}

	private void addContextMultiAlignCenters( List<ContextMenuItem> menu )
	{

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Align Centers/Horizontally",
			Handler = ( control ) => { alignCenterHorz(); }
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Align Centers/Vertically",
			Handler = ( control ) => { alignCenterVert(); }
		} );

	}

	private void addContextControlOrder( List<ContextMenuItem> menu )
	{

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Ordering/Bring to Front",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Bring to front" );
				control.BringToFront();
				SceneView.lastActiveSceneView.Repaint();
			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Ordering/Send To Back",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Send to back" );
				control.SendToBack();
				SceneView.lastActiveSceneView.Repaint();
			}
		} );

	}

	private void addContextSelectControl( List<ContextMenuItem> menu )
	{

		var camera = ( (dfControl)target ).GetCamera();
		if( camera == null )
			return;

		var mousePos = Event.current.mousePosition;
		var ray = HandleUtility.GUIPointToWorldRay( mousePos );
		var maxDistance = 1000f;

		var controls = Physics
			.RaycastAll( ray, maxDistance, camera.cullingMask )
			.Select( x => x.collider.GetComponent<dfControl>() )
			.Where( x => x != null && x != target )
			.Select( x => new { control = x, name = getObjectPath( x ) } )
			.OrderBy( x => x.name )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{

			var controlInfo = controls[ i ];

			menu.Add( new ContextMenuItem()
			{
				MenuText = "Select Control/" + ( i + 1 ) + ". " + controlInfo.name,
				Handler = ( obj ) =>
				{
					Selection.activeGameObject = controlInfo.control.gameObject;
				}
			} );

		}

		if( controls.Count > 0 )
		{
			menu.Add( new ContextMenuItem() { MenuText = "-" } );
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

	private void addContextSelectPrefab( List<ContextMenuItem> menu )
	{

		// Need to determine final control position immediately, as 
		// this information is more difficult to obtain inside of an
		// anonymous delegate
		var mousePos = Event.current.mousePosition;
		var controlPosition = raycast( mousePos );

		Action<dfControl> selectPrefab = ( control ) =>
		{
			dfPrefabSelectionDialog.Show(
				"Select a prefab Control",
				typeof( dfControl ),
				( prefab ) =>
				{

					if( prefab == null )
						return;

					dfEditorUtil.MarkUndo( control, "Add child control - " + prefab.name );
						
					var newGameObject = PrefabUtility.InstantiatePrefab( prefab ) as GameObject;
					var childControl = newGameObject.GetComponent<dfControl>();
					childControl.transform.parent = control.transform;
					childControl.transform.position = controlPosition;
						
					control.AddControl( childControl );
						
					Selection.activeGameObject = newGameObject;

				},
				null,
				null
			);
		};

		menu.Add( new ContextMenuItem() { MenuText = "Add Prefab...", Handler = selectPrefab } );

	}

	private void addContextDatabinding( List<ContextMenuItem> menu )
	{

		var assembly = typeof( IDataBindingComponent ).Assembly;
		var types = assembly.GetTypes();

		var controlTypes = types
			.Where( t =>
				typeof( IDataBindingComponent ).IsAssignableFrom( t ) &&
				t.IsDefined( typeof( AddComponentMenu ), false )
			).ToList();

		var options = new List<ContextMenuItem>();

		for( int i = 0; i < controlTypes.Count; i++ )
		{
			var type = controlTypes[ i ];
			var componentMenuAttribute = type.GetCustomAttributes( typeof( AddComponentMenu ), false ).First() as AddComponentMenu;
			var optionText = componentMenuAttribute.componentMenu.Replace( "Daikon Forge/Data Binding/", "" );
			options.Add( new ContextMenuItem()
			{
				MenuText = "Add Binding/" + optionText,
				Handler = ( control ) =>
				{
					dfEditorUtil.MarkUndo( control, "Add Binding - " + type.Name );
					var child = control.gameObject.AddComponent( type );
					Selection.activeGameObject = child.gameObject;
				}
			} );
		}

		options.Sort( ( lhs, rhs ) => { return lhs.MenuText.CompareTo( rhs.MenuText ); } );

		menu.AddRange( options );

	}

	private void addContextTweens( List<ContextMenuItem> menu )
	{

		var assembly = Assembly.GetAssembly( target.GetType() );
		var types = assembly.GetTypes();

		var controlTypes = types
			.Where( t =>
				typeof( dfTweenPlayableBase ).IsAssignableFrom( t ) &&
				t.IsDefined( typeof( AddComponentMenu ), false )
			).ToList();

		var options = new List<ContextMenuItem>();

		for( int i = 0; i < controlTypes.Count; i++ )
		{
			var type = controlTypes[ i ];
			var componentMenuAttribute = type.GetCustomAttributes( typeof( AddComponentMenu ), false ).First() as AddComponentMenu;
			var optionText = componentMenuAttribute.componentMenu.Replace( "Daikon Forge/Tweens/", "" );
			options.Add( new ContextMenuItem()
			{
				MenuText = "Add Tween/" + optionText,
				Handler = ( control ) =>
				{
					dfEditorUtil.MarkUndo( control, "Add Tween - " + type.Name );
					var child = control.gameObject.AddComponent( type );
					Selection.activeGameObject = child.gameObject;
				}
			} );
		}

		options.Sort( ( lhs, rhs ) => { return lhs.MenuText.CompareTo( rhs.MenuText ); } );

		menu.AddRange( options );

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

		var options = new List<ContextMenuItem>();

		for( int i = 0; i < controlTypes.Count; i++ )
		{
			var type = controlTypes[i];
			var componentMenuAttribute = type.GetCustomAttributes( typeof( AddComponentMenu ), false ).First() as AddComponentMenu;
			var optionText = componentMenuAttribute.componentMenu.Replace( "Daikon Forge/User Interface/", "" );
			options.Add( buildAddChildMenuItem( optionText, type ) );
		}

		options.Sort( ( lhs, rhs ) => { return lhs.MenuText.CompareTo( rhs.MenuText ); } );

		menu.AddRange( options );

	}

	private ContextMenuItem buildAddChildMenuItem( string optionText, Type type )
	{

		// Need to determine final control position immediately, as 
		// this information is more difficult to obtain inside of an
		// anonymous delegate
		var mousePos = Event.current.mousePosition;
		var controlPosition = raycast( mousePos );

		return new ContextMenuItem()
		{
			MenuText = "Add Control/" + optionText,
			Handler = ( control ) => 
			{
				
				var childName = type.Name;
				if( childName.StartsWith( "df" ) )
					childName = childName.Substring( 2 );

				childName = ObjectNames.NicifyVariableName( childName ) + buildControlNameSuffix( control, type );

				dfEditorUtil.MarkUndo( control, "Add Control - " + childName );

				var child = control.AddControl( type );
				child.name = childName;
				child.transform.position = controlPosition;

				Selection.activeGameObject = child.gameObject;

			}
		};

	}

	private string buildControlNameSuffix( dfControl control, Type type )
	{

		var count = 0;
		var controls = control.Controls;
		for( int i = 0; i < controls.Count; i++ )
		{
			if( controls[ i ].GetType() == type )
				count += 1;
		}

		if( count > 0 )
			return " " + count.ToString();

		return "";

	}

	private void addContextDock( List<ContextMenuItem> menu )
	{

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Dock/Left",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Dock control" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				control.Size = new Vector2( control.Size.x, containerSize.y );
				control.RelativePosition = new Vector3( 0, 0 );
				control.Anchor = dfAnchorStyle.Left | dfAnchorStyle.Top | dfAnchorStyle.Bottom;
			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Dock/Top",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Dock control" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				control.Size = new Vector2( containerSize.x, control.Size.y );
				control.RelativePosition = new Vector3( 0, 0 );
				control.Anchor =  dfAnchorStyle.Left | dfAnchorStyle.Right | dfAnchorStyle.Top;
			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Dock/Right",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Dock control" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				control.Size = new Vector2( control.Size.x, containerSize.y );
				control.RelativePosition = new Vector3( containerSize.x - control.Size.x, 0 );
				control.Anchor = dfAnchorStyle.Top | dfAnchorStyle.Bottom | dfAnchorStyle.Right;
			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = "Dock/Bottom",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Dock control" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				control.Size = new Vector2( containerSize.x, control.Size.y );
				control.RelativePosition = new Vector3( 0, containerSize.y - control.Size.y );
				control.Anchor = dfAnchorStyle.Left | dfAnchorStyle.Right | dfAnchorStyle.Bottom;
			}
		} );

	}

	private void addContextFill( List<ContextMenuItem> menu )
	{

		var targetControl = target as dfControl;

		var containerName = targetControl.Parent != null ? targetControl.Parent.name : "screen";
		var rootText = "Fit to " + containerName;

		menu.Add( new ContextMenuItem()
		{
			MenuText = rootText + "/Horizontally",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Fit to container" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				control.Size = new Vector2( containerSize.x, control.Size.y );
				control.RelativePosition = new Vector3( 0, control.RelativePosition.y );

			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = rootText + "/Vertically",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Fit to container" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				control.Size = new Vector2( control.Size.x, containerSize.y );
				control.RelativePosition = new Vector3( control.RelativePosition.x, 0 );

			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = rootText + "/Both",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Fit to container" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				control.Size = containerSize;
				control.RelativePosition = Vector3.zero;

			}
		} );

	}

	private void addContextCenter( List<ContextMenuItem> menu )
	{

		var targetControl = target as dfControl;

		var containerName = targetControl.Parent != null ? "in " + targetControl.Parent.name : " on screen";
		var rootText = "Center " + containerName;

		menu.Add( new ContextMenuItem()
		{
			MenuText = rootText + "/Horizontally",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Center horizontally" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				var posX = ( containerSize.x - control.Size.x ) * 0.5f;
				var pos = control.RelativePosition;
				control.RelativePosition = new Vector3( posX, pos.y );
			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = rootText + "/Vertically",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Center vertically" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				var posY = ( containerSize.y - control.Size.y ) * 0.5f;
				var pos = control.RelativePosition;
				control.RelativePosition = new Vector3( pos.x, posY );
			}
		} );

		menu.Add( new ContextMenuItem()
		{
			MenuText = rootText + "/Both",
			Handler = ( control ) =>
			{
				dfEditorUtil.MarkUndo( control, "Center" );

				var containerSize = ( control.Parent != null )
					? control.Parent.Size
					: control.GetManager().GetScreenSize();

				var posX = ( containerSize.x - control.Size.x ) * 0.5f;
				var posY = ( containerSize.y - control.Size.y ) * 0.5f;
				control.RelativePosition = new Vector3( posX, posY );
			}
		} );

	}

	private void displayContextMenu()
	{

		var control = target as dfControl;
		if( control == null )
			return;

		var menu = new GenericMenu();

		var items = new List<ContextMenuItem>();
		FillContextMenu( items );

		var actionFunc = new Action<int>( ( command ) =>
		{
			var handler = items[ command ].Handler;
			handler( control );
		} );

		menu.AddDisabledItem( new GUIContent( string.Format( "{0} ({1})", control.name, control.GetType().Name ) ) );
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

	private void markUndo( List<dfControl> controls, string undoMessage )
	{

		if( controls == null || controls.Count == 0 )
			return;

#if UNITY_4_3

		// HACK: Workaround for broken Unity undo handling
		var manager = controls[ 0 ].GetManager();
		Undo.RegisterCompleteObjectUndo( manager, undoMessage );
		EditorUtility.SetDirty( manager );

		// Record undo information for each selected control
		controls.ForEach( c =>
		{
			Undo.RegisterCompleteObjectUndo( c, undoMessage );
		} );

#else
		Undo.RegisterSceneUndo( undoMessage );
#endif

	}

	#region Control alignment functions 

	private void alignEdgeLeft()
	{

		var minX = float.MaxValue;

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];
			var corners = control.GetCorners();

			minX = Mathf.Min( minX, corners[ 0 ].x );

		}

		markUndo( controls, "Align Left" );

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];

			EditorUtility.SetDirty( control );

			var position = control.transform.position;
			var corners = control.GetCorners();
			var offset = corners[ 0 ] - position;

			control.transform.position = new Vector3(
				minX - offset.x,
				position.y,
				position.z
			);

			control.MakePixelPerfect();
			control.ResetLayout( false, true );

		}

		dfGUIManager.RefreshAll( true );

	}

	private void alignEdgeRight()
	{

		var maxX = float.MinValue;

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];
			var corners = control.GetCorners();

			maxX = Mathf.Max( maxX, corners[ 1 ].x );

		}

		markUndo( controls, "Align Right" );

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];

			EditorUtility.SetDirty( control );

			var position = control.transform.position;
			var corners = control.GetCorners();
			var offset = corners[ 1 ] - position;

			control.transform.position = new Vector3(
				maxX - offset.x,
				position.y,
				position.z
			);

			control.MakePixelPerfect();
			control.ResetLayout( false, true );

		}

		dfGUIManager.RefreshAll( true );

	}

	private void alignEdgeTop()
	{

		var maxY = float.MinValue;

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];
			var corners = control.GetCorners();

			maxY = Mathf.Max( maxY, corners[ 1 ].y );

		}

		markUndo( controls, "Align Top" );

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];

			EditorUtility.SetDirty( control );

			var position = control.transform.position;
			var corners = control.GetCorners();
			var offset = corners[ 1 ] - position;

			control.transform.position = new Vector3(
				position.x,
				maxY - offset.y,
				position.z
			);

			control.MakePixelPerfect();
			control.ResetLayout( false, true );

		}

		dfGUIManager.RefreshAll( true );

	}

	private void alignEdgeBottom()
	{

		var minY = float.MaxValue;

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];
			var corners = control.GetCorners();

			minY = Mathf.Min( minY, corners[ 2 ].y );

		}

		markUndo( controls, "Align Top" );

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];

			EditorUtility.SetDirty( control );

			var position = control.transform.position;
			var corners = control.GetCorners();
			var offset = corners[ 2 ] - position;

			control.transform.position = new Vector3(
				position.x,
				minY - offset.y,
				position.z
			);

			control.MakePixelPerfect();
			control.ResetLayout( false, true );

		}

		dfGUIManager.RefreshAll( true );

	}

	private void alignCenterHorz()
	{

		var centers = new List<Vector3>();

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{
			centers.Add( controls[ i ].GetCenter() );
		}

		var averagedCenter = centers[ 0 ];
		for( int i = 1; i < centers.Count; i++ )
		{
			averagedCenter = averagedCenter + centers[ i ];
		}

		averagedCenter /= centers.Count;

		markUndo( controls, "Align Center Horizontally" );

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];

			EditorUtility.SetDirty( control );

			var position = control.transform.position;
			var offset = control.GetCenter() - position;

			control.transform.position = new Vector3(
				averagedCenter.x - offset.x,
				position.y,
				position.z
			);

			control.MakePixelPerfect();
			control.ResetLayout( false, true );

		}

		dfGUIManager.RefreshAll( true );

	}

	private void alignCenterVert()
	{

		var centers = new List<Vector3>();

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{
			var control = controls[ i ];
			centers.Add( control.GetCenter() );
		}

		var averagedCenter = centers[ 0 ];
		for( int i = 1; i < centers.Count; i++ )
		{
			averagedCenter = averagedCenter + centers[ i ];
		}

		averagedCenter /= centers.Count;

		markUndo( controls, "Align Center Vertically" );

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];

			EditorUtility.SetDirty( control );

			var position = control.transform.position;
			var offset = control.GetCenter() - position;

			control.transform.position = new Vector3(
				position.x,
				averagedCenter.y - offset.y,
				position.z
			);

			control.MakePixelPerfect();
			control.ResetLayout( false, true );

		}

		dfGUIManager.RefreshAll( true );

	}

	private void makeSameSizeHorizontally()
	{

		var maxWidth = 0f;

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{
			maxWidth = Mathf.Max( controls[ i ].Width, maxWidth );
		}

		markUndo( controls, "Make same size" );

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];

			control.Width = maxWidth;
			control.PerformLayout();
			control.MakePixelPerfect();

			EditorUtility.SetDirty( control );

		}

		dfGUIManager.RefreshAll( true );

	}

	private void makeSameSizeVertically()
	{

		var maxHeight = 0f;

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.ToList();

		for( int i = 0; i < controls.Count; i++ )
		{
			maxHeight = Mathf.Max( controls[ i ].Height, maxHeight );
		}

		markUndo( controls, "Make same size" );

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];

			control.Height = maxHeight;
			control.PerformLayout();
			control.MakePixelPerfect();

			EditorUtility.SetDirty( control );

		}

		dfGUIManager.RefreshAll( true );

	}

	private void distributeControlsHorizontally()
	{

		var minX = float.MaxValue;
		var maxX = float.MinValue;

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.OrderBy( c => ( (dfControl)c ).transform.position.x )
			.ToList();

		if( controls.Count <= 2 )
			return;

		for( int i = 0; i < controls.Count; i++ )
		{

			var pos = controls[ i ].transform.position;

			minX = Mathf.Min( pos.x, minX );
			maxX = Mathf.Max( pos.x, maxX );

		}

		var step = ( maxX - minX ) / ( controls.Count - 1 );

		markUndo( controls, "Distribute Horizontally" );

		for( int i = 0; i < controls.Count; i++ )
		{
		
			var control = controls[ i ];
			var position = control.transform.position;

			control.transform.position = new Vector3(
				minX + i * step,
				position.y,
				position.z
			);

			control.MakePixelPerfect();
			control.ResetLayout( false, true );

		}

		dfGUIManager.RefreshAll( true );

	}

	private void distributeControlsVertically()
	{

		var minY = float.MaxValue;
		var maxY = float.MinValue;

		var controls =
			Selection.gameObjects
			.Select( c => c.GetComponent<dfControl>() )
			.Where( c => c != null )
			.OrderByDescending( c => c.transform.position.y )
			.ToList();

		if( controls.Count <= 2 )
			return;

		for( int i = 0; i < controls.Count; i++ )
		{

			var pos = controls[ i ].transform.position;

			minY = Mathf.Min( pos.y, minY );
			maxY = Mathf.Max( pos.y, maxY );

		}

		var step = ( maxY - minY ) / ( controls.Count - 1 );

		markUndo( controls, "Distribute Horizontally" );

		for( int i = 0; i < controls.Count; i++ )
		{

			var control = controls[ i ];
			var position = control.transform.position;

			control.transform.position = new Vector3(
				position.x,
				maxY - i * step,
				position.z
			);

			control.MakePixelPerfect();
			control.ResetLayout( false, true );

		}

		dfGUIManager.RefreshAll( true );

	}

	#endregion

	#endregion

	#region Private utility classes 

	private enum EditorAction
	{
		None,
		Move,
		Resize,
		Rotate
	}

	private class EditorHandle
	{
		public EditorAction action;
		public Vector3 position;
		public Rect rect;
		public dfPivotPoint pivot;
	}

	protected class ContextMenuItem
	{
		public string MenuText;
		public Action<dfControl> Handler;
	}

	protected class OperationUndoInfo
	{

		private dfControl control;
		private Vector3 position;
		private Vector3 rotation;
		private Vector2 size;

		public OperationUndoInfo( dfControl control )
		{
			this.control = control;
			this.position = control.Position;
			this.rotation = control.transform.localEulerAngles;
			this.size = control.Size;
		}

		public void UndoResize()
		{
			this.control.Size = this.size;
			this.control.Position = this.position;
			control.transform.localEulerAngles = this.rotation;
		}

	}

	#endregion

}
