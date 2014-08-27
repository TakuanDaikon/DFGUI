/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Object = UnityEngine.Object;

public class dfEditorUtil
{

	public static Component clipboard;

	private static Queue<System.Action> actionQueue = null;

	private static Texture2D backgroundTextureDark = null;
	private static Texture2D backgroundTextureLight = null;
	private static GUIStyle backgroundStyleDark = null;
	private static GUIStyle backgroundStyleLight = null;

	public static GUIStyle BoxStyleLight
	{
		get
		{

			if( backgroundTextureLight == null )
			{
				var color = new Color( 0.5f, 0.5f, 0.5f, 1 );
				backgroundTextureLight = new Texture2D( 2, 2 ) { hideFlags = HideFlags.DontSave };
				backgroundTextureLight.SetPixel( 0, 0, color );
				backgroundTextureLight.SetPixel( 0, 1, color );
				backgroundTextureLight.SetPixel( 1, 0, color );
				backgroundTextureLight.SetPixel( 1, 1, color );
				backgroundTextureLight.Apply();
			}

			if( backgroundStyleLight == null )
			{
				backgroundStyleLight = new GUIStyle()
				{
					normal = new GUIStyleState()
					{
						textColor = Color.black,
						background = backgroundTextureLight
					}
				};
			}

			return backgroundStyleLight;

		}
	}

	public static GUIStyle BoxStyleDark
	{
		get
		{

			if( backgroundTextureDark == null )
			{
				var color = new Color( 0.1f, 0.1f, 0.1f, 1 );
				backgroundTextureDark = new Texture2D( 2, 2 ) { hideFlags = HideFlags.DontSave };
				backgroundTextureDark.SetPixel( 0, 0, color );
				backgroundTextureDark.SetPixel( 0, 1, color );
				backgroundTextureDark.SetPixel( 1, 0, color );
				backgroundTextureDark.SetPixel( 1, 1, color );
				backgroundTextureDark.Apply();
			}

			if( backgroundStyleDark == null )
			{
				backgroundStyleDark = new GUIStyle()
				{
					normal = new GUIStyleState()
					{
						textColor = Color.white,
						background = backgroundTextureDark
					}
				};
			}

			return backgroundStyleDark;

		}
	}

	public static void DrawHandle( int id, Vector3 point, GUIStyle style )
	{

		if( Event.current == null || Event.current.type != EventType.repaint )
			return;

		float fixedWidth = style.fixedWidth;
		float fixedHeight = style.fixedHeight;

		point = HandleUtility.WorldToGUIPoint( point );

		var rect = new Rect( point.x - fixedWidth * 0.5f, point.y - fixedHeight * 0.5f, fixedWidth, fixedHeight );
		style.Draw( rect, GUIContent.none, id, true );

	}

	public static dfPropertyGroup BeginGroup( string label )
	{
		return BeginGroup( label, LabelWidth );
	}

	public static dfPropertyGroup BeginGroup( string label, float labelWidth )
	{
		return new dfPropertyGroup( label, labelWidth );
	}

	public static void DelayedInvoke( System.Action callback )
	{

		if( actionQueue == null )
		{

			actionQueue = new Queue<System.Action>();

			EditorApplication.update += () =>
			{
				while( actionQueue.Count > 0 )
				{
					var action = actionQueue.Dequeue();
					action();
				}
			};

		}

		actionQueue.Enqueue( callback );

	}

	public static float LabelWidth
	{
		get
		{

#if UNITY_4_3
			return EditorGUIUtility.labelWidth;
#else
			var members = typeof( EditorGUIUtility ).GetMember( "labelWidth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static );
			if( members == null || members.Length != 1 )
				return 75f;

			var member = members[ 0 ];

			if( member is FieldInfo )
			{
				return (float)( (FieldInfo)member ).GetValue( null );
			}

			if( member is PropertyInfo )
			{
				return (float)( (PropertyInfo)member ).GetValue( null, null );
			}

			return 75f;
#endif

		}
		set
		{

#if UNITY_4_3
			EditorGUIUtility.labelWidth = value;
#else
			var members = typeof( EditorGUIUtility ).GetMember( "labelWidth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static );
			if( members == null || members.Length != 1 )
				return;

			var member = members[ 0 ];

			if( member is FieldInfo )
			{
				( (FieldInfo)member ).SetValue( null, value );
			}

			if( member is PropertyInfo )
			{
				( (PropertyInfo)member ).SetValue( null, value, null );
			}
#endif

		}
	}

	public static void MarkUndo( Object target, string UndoMessage )
	{

#if UNITY_4_3
		
		// HACK: Workaround for broken Unity undo handling
		var views = Object.FindObjectsOfType<dfGUIManager>();
		for( int i = 0; i < views.Length; i++ )
		{
			var manager = views[ i ];
			Undo.RegisterFullObjectHierarchyUndo( manager );
			EditorUtility.SetDirty( manager );
		}

		Undo.RegisterCompleteObjectUndo( target, UndoMessage );

#else
		Undo.RegisterSceneUndo( UndoMessage );
#endif

		EditorUtility.SetDirty( target );

	}

	public static Vector2 EditInt2( string groupLabel, string label1, string label2, Vector2 value )
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

	public static RectOffset EditPadding( string groupLabel, RectOffset value )
	{

		var savedLabelWidth = dfEditorUtil.LabelWidth;

		EditorGUI.BeginChangeCheck();

		var retVal = new RectOffset();

		GUILayout.BeginHorizontal();
		{

			EditorGUILayout.LabelField( groupLabel, "", GUILayout.Width( dfEditorUtil.LabelWidth - 15 ) );

			GUILayout.BeginVertical();
			{

				dfEditorUtil.LabelWidth = 65f;

				retVal.left = EditorGUILayout.IntField( "Left", value != null ? value.left : 0 );
				retVal.right = EditorGUILayout.IntField( "Right", value != null ? value.right : 0 );
				retVal.top = EditorGUILayout.IntField( "Top", value != null ? value.top : 0 );
				retVal.bottom = EditorGUILayout.IntField( "Bottom", value != null ? value.bottom : 0 );

			}
			GUILayout.EndVertical();

			GUILayout.FlexibleSpace();

		}
		GUILayout.EndHorizontal();

		dfEditorUtil.LabelWidth = savedLabelWidth;

		if( EditorGUI.EndChangeCheck() )
			return retVal;
		else
			return value;

	}

	public static void DrawHorzLine()
	{
		DrawHorzLine ( 2, 4 );
	}

	public static void DrawHorzLine( int height, int padding )
	{

		GUILayout.Space( padding );

		var savedColor = GUI.color;
		GUI.color = EditorGUIUtility.isProSkin ? new Color( 0.157f, 0.157f, 0.157f ) : new Color( 0.75f, 0.75f, 0.75f );
		GUILayout.Box( "", GUILayout.ExpandWidth( true ), GUILayout.Height( height ) );
		GUI.color = savedColor;

		GUILayout.Space( padding );

	}

	public static void DrawSeparator()
	{

		GUILayout.Space( 12f );

		if( Event.current.type == EventType.Repaint )
		{

			Texture2D tex = EditorGUIUtility.whiteTexture;

			Rect rect = GUILayoutUtility.GetLastRect();

			var savedColor = GUI.color;
			GUI.color = new Color( 0f, 0f, 0f, 0.25f );

			GUI.DrawTexture( new Rect( 0f, rect.yMin + 6f, Screen.width, 4f ), tex );
			GUI.DrawTexture( new Rect( 0f, rect.yMin + 6f, Screen.width, 1f ), tex );
			GUI.DrawTexture( new Rect( 0f, rect.yMin + 9f, Screen.width, 1f ), tex );

			GUI.color = savedColor;

		}

	}

	public static Component ComponentField( string label, Component value )
	{
		return ComponentField( label, value, null );
	}

	public static Component ComponentField( string label, Component value, Type componentType )
	{

		componentType = componentType ?? typeof( MonoBehaviour );

		EditorGUILayout.BeginHorizontal();
		{

			EditorGUILayout.LabelField( label, "", GUILayout.Width( dfEditorUtil.LabelWidth - 10 ) );

			GUILayout.Space( 5 );

			var displayText = value == null ? "[none]" : value.ToString();
			GUILayout.Label( displayText, "TextField", GUILayout.ExpandWidth( true ), GUILayout.MinWidth( 100 ) );

			var evt = Event.current;
			if( evt != null )
			{
				var textRect = GUILayoutUtility.GetLastRect();
				if( evt.type == EventType.mouseDown && evt.clickCount == 2 )
				{
					if( textRect.Contains( evt.mousePosition ) )
					{
						if( GUI.enabled && value != null )
						{
							Selection.activeObject = value;
							EditorGUIUtility.PingObject( value );
							GUIUtility.hotControl = value.GetInstanceID();
						}
					}
				}
				else if( evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform )
				{
					if( textRect.Contains( evt.mousePosition ) )
					{

						var reference = DragAndDrop.objectReferences.First();
						var draggedComponent = (Component)null;
						if( reference is Transform )
						{
							draggedComponent = (Transform)reference;
						}
						else if( reference is GameObject )
						{
							draggedComponent =
								( (GameObject)reference )
								.GetComponents( componentType )
								.FirstOrDefault();
						}
						else if( reference is Component )
						{
							draggedComponent = reference as Component;
							if( draggedComponent == null )
							{
								draggedComponent =
									( (Component)reference )
									.GetComponents( componentType )
									.FirstOrDefault();
							}
						}

						DragAndDrop.visualMode = ( draggedComponent == null ) ? DragAndDropVisualMode.None : DragAndDropVisualMode.Copy;

						if( evt.type == EventType.DragPerform )
						{
							value = draggedComponent;
						}

						evt.Use();

					}
				}
			}

			GUI.enabled = ( clipboard != null );
			{
				var tooltip = ( clipboard != null ) ? string.Format( "Paste {0} ({1})", clipboard.name, clipboard.GetType().Name ) : "";
				var content = new GUIContent( "Paste", tooltip );
				if( GUILayout.Button( content, "minibutton", GUILayout.Width( 50 ) ) )
				{
					value = clipboard;
				}
			}
			GUI.enabled = true;

		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space( 3 );

		return value;

	}

	public static void DrawTexture( Rect rect, Texture2D texture )
	{

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

	internal static void DrawSprite( Rect rect, dfAtlas atlas, string sprite )
	{
		var spriteInfo = atlas[ sprite ];
		GUI.DrawTextureWithTexCoords( rect, atlas.Material.mainTexture, spriteInfo.region, true );
	}

}

public class dfPropertyGroup : IDisposable
{

	private float savedLabelWidth = 0;

	public dfPropertyGroup( string label ) : this( label, 100 )
	{
	}

	public dfPropertyGroup( string label, float labelWidth )
	{

		savedLabelWidth = dfEditorUtil.LabelWidth;

		GUILayout.Label( label, "HeaderLabel" );
		EditorGUI.indentLevel += 1;

		dfEditorUtil.LabelWidth = labelWidth;

	}

	#region IDisposable Members

	public void Dispose()
	{
		EditorGUI.indentLevel -= 1;
		dfEditorUtil.LabelWidth = savedLabelWidth;
	}

	#endregion

}

[InitializeOnLoad]
public static class dfEditorExtensions
{

	private static readonly EditorApplication.HierarchyWindowItemCallback hiearchyItemCallback;

	private static Texture2D hierarchyIcon;
	private static Texture2D HierarchyIcon
	{
		get
		{
			if( hierarchyIcon == null )
			{
				hierarchyIcon = (Texture2D)Resources.Load( "DFGUI_Icon" );
			}
			return hierarchyIcon;
		}
	}

	// constructor
	static dfEditorExtensions()
	{
		hiearchyItemCallback = new EditorApplication.HierarchyWindowItemCallback( DrawHierarchyIcon );
		EditorApplication.hierarchyWindowItemOnGUI = (EditorApplication.HierarchyWindowItemCallback)Delegate.Combine( EditorApplication.hierarchyWindowItemOnGUI, hiearchyItemCallback );

	}

	private static void DrawHierarchyIcon( int instanceID, Rect selectionRect )
	{

		if( HierarchyIcon == null )
		{
			return;
		}

		GameObject gameObject = EditorUtility.InstanceIDToObject( instanceID ) as GameObject;
		if( gameObject == null )
			return;

		var view = gameObject.GetComponent<dfGUIManager>();
		if( view == null )
			return;

		Rect rect = new Rect( selectionRect.x + selectionRect.width - 18f, selectionRect.y, 16f, 16f );
		GUI.DrawTexture( rect, HierarchyIcon );

	}

}

public static class dfMenuExtensions
{

	[MenuItem( "Tools/Daikon Forge/Help/Getting Started", false, 98 )]
	public static void ShowDocumentation( MenuCommand command )
	{
		Help.BrowseURL( "http://www.daikonforge.com/docs/Getting%20Started%20with%20DFGUI.pdf" );
	}

	[MenuItem( "Tools/Daikon Forge/Help/Daikon Forge Website" )]
	public static void ShowHelp( MenuCommand command )
	{
		Help.BrowseURL( "http://www.daikonforge.com/dfgui/" );
	}

	[MenuItem( "Tools/Daikon Forge/Help/Support Forums" )]
	public static void ShowSupportForums( MenuCommand command )
	{
		Help.BrowseURL( "http://www.daikonforge.com/dfgui/forums/" );
	}

	[MenuItem( "Tools/Daikon Forge/Help/Class Library Documentation", false, 99 )]
	public static void ShowAPIDocs( MenuCommand command )
	{
		Help.BrowseURL( "http://www.daikonforge.com/docs/df-gui/" );
	}

	[MenuItem( "CONTEXT/Component/Copy Component Reference" )]
	public static void CopyControlReference( MenuCommand command )
	{
		var control = command.context as Component;
		Debug.Log( "Control reference copied: " + control.name + " (" + control.GetType().Name + ")" );
		dfEditorUtil.clipboard = control;
	}

}

internal static class dfReflectionExtensions
{

	/// <summary>
	/// Returns all instance fields on an object, including inherited fields
	/// </summary>
	internal static FieldInfo[] GetAllFields( this Type type )
	{

		// http://stackoverflow.com/a/1155549/154165

		if( type == null )
			return new FieldInfo[ 0 ];

		BindingFlags flags =
			BindingFlags.Public |
			BindingFlags.NonPublic |
			BindingFlags.Instance |
			BindingFlags.DeclaredOnly;

		return
			type.GetFields( flags )
			.Concat( GetAllFields( type.BaseType ) )
			.Where( f => !f.IsDefined( typeof( HideInInspector ), true ) )
			.ToArray();

	}

	internal static object GetProperty( this object target, string property )
	{

		if( target == null )
			throw new NullReferenceException( "Target is null" );

		var members = target.GetType().GetMember( property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
		if( members == null || members.Length == 0 )
			throw new IndexOutOfRangeException( "Property not found: " + property );

		var member = members[ 0 ];

		if( member is FieldInfo )
		{
			return ( (FieldInfo)member ).GetValue( target );
		}

		if( member is PropertyInfo )
		{
			return ( (PropertyInfo)member ).GetValue( target, null );
		}

		throw new InvalidOperationException( "Member type not supported: " + member.MemberType );

	}

	internal static void SetProperty( this object target, string property, object value )
	{

		if( target == null )
			throw new NullReferenceException( "Target is null" );

		var members = target.GetType().GetMember( property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
		if( members == null || members.Length == 0 )
			throw new IndexOutOfRangeException( "Property not found: " + property );

		var member = members[ 0 ];

		if( member is FieldInfo )
		{
			( (FieldInfo)member ).SetValue( target, value );
			return;
		}

		if( member is PropertyInfo )
		{
			( (PropertyInfo)member ).SetValue( target, value, null );
			return;
		}

		throw new InvalidOperationException( "Member type not supported: " + member.MemberType );

	}

}

