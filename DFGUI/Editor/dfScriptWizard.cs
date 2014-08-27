/* Copyright 2013-2014 Daikon Forge */
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class dfScriptWizard : EditorWindow
{

	private static Dictionary<string, string> typeMap = new Dictionary<string, string>()
	{
		{ "System.Boolean", "bool" },
		{ "System.Byte", "byte" },
		{ "System.SByte", "sbyte" },
		{ "System.Char", "char" },
		{ "System.Decimal", "decimal" },
		{ "System.Double", "double" },
		{ "System.Single", "float" },
		{ "System.Int32", "int" },
		{ "System.UInt32", "uint" },
		{ "System.Int64", "long" },
		{ "System.UInt64", "ulong" },
		{ "System.Object", "object" },
		{ "System.Int16", "short" },
		{ "System.UInt16", "ushort" },
		{ "System.String", "string" }
	};

	private const int BUTTON_WIDTH = 120;
	private const int LABEL_WIDTH = 125;

	private dfControl target;
	private List<EventInfo> events = new List<EventInfo>();
	private List<string> expandedSections = new List<string>() { "Mouse Input" };
	private List<ComponentReference> referencedComponents = new List<ComponentReference>();

	private Vector2 previewScrollPos;
	private Vector2 eventsScrollPos;
	private string className = "EXAMPLE";
	private bool referenceControl = false;

	#region Static methods 

	public static void CreateScript( dfControl control )
	{

		if( control == null )
			return;

		if( string.IsNullOrEmpty( EditorApplication.currentScene ) )
		{
			EditorUtility.DisplayDialog( "Please save your scene", "Please save your scene before using the Script Wizard", "OK" );
			return;
		}

		var events = control
			.GetType()
			.GetAllFields()
			.Where( x => typeof( Delegate ).IsAssignableFrom( x.FieldType ) )
			.Select( x => new EventInfo( x ) )
			.OrderBy( x => x.Category + "." + x.Field.Name )
			.ToList();

		var path = buildGameObjectPath( control );

		var dialog = GetWindow<dfScriptWizard>( true, "Create Script - " + path );
		dialog.target = control;
		dialog.events = events;
		dialog.minSize = new Vector2( 640, 480 );
		dialog.className = control.name.Replace( " ", "" ) + "Events";

	}

	private static string buildGameObjectPath( dfControl control )
	{

		var obj = control.transform;
		
		var buffer = new StringBuilder();

		while( obj != null )
		{
			if( buffer.Length > 0 )
				buffer.Insert( 0, "/" );
			buffer.Insert( 0, obj.name );
			obj = obj.parent;
		}

		buffer.Append( " (" );
		buffer.Append( control.GetType().Name );
		buffer.Append( " )" );

		return buffer.ToString();

	}

	#endregion

	public void OnGUI()
	{

		dfEditorUtil.LabelWidth = LABEL_WIDTH;

		EditorGUILayout.BeginHorizontal();
		{

			GUILayout.Space( 10 );

			showPreview();
			GUILayout.Space( 10 );

			EditorGUILayout.BeginVertical();
			{
				showOptions();
				showDocumentationLink();
				GUILayout.Space( 10 );
				showButtons();
			} 
			EditorGUILayout.EndVertical();

			GUILayout.Space( 10 );

		} 
		EditorGUILayout.EndHorizontal();

		GUILayout.Space( 10 );
	
	}

	private void showDocumentationLink()
	{

		if( GUILayout.Button( "View online API documentation", "minibutton", GUILayout.ExpandWidth( true ) ) )
		{
			Application.OpenURL( "http://www.daikonforge.com/docs/df-gui/index.html" );
		}

	}

	private void showButtons()
	{

		GUILayout.BeginHorizontal();
		{

			GUILayout.FlexibleSpace();

			if( GUILayout.Button( "Cancel", GUILayout.Width( BUTTON_WIDTH ) ) )
			{
				Close();
				GUIUtility.ExitGUI();
			}

			bool guiEnabledTemp = GUI.enabled;
			if( GUILayout.Button( "Create", GUILayout.Width( BUTTON_WIDTH ) ) )
			{
				saveAndAttachScript();
			}
			GUI.enabled = guiEnabledTemp;

		}
		GUILayout.EndHorizontal();

	}

	private void saveAndAttachScript()
	{

		var scenePath = Path.GetDirectoryName( EditorApplication.currentScene ).Replace( "\\", "/" );
		var path = EditorUtility.SaveFilePanel( "Create Script", scenePath, className, "cs" );
		if( string.IsNullOrEmpty( path ) )
			return;

		var filename = Path.GetFileNameWithoutExtension( path );
		if( !Regex.IsMatch( filename, "^[a-zA-Z$_]+[a-zA-Z0-9$_]*$" ) )
		{
			EditorUtility.DisplayDialog( "Invalid file name", "You have chosen a file name that cannot be used as a valid identifier for a MonoBehavior", "CANCEL" );
			return;
		}

		using( var file = File.CreateText( path ) )
		{
			className = filename;
			file.WriteLine( generateScript() );
		}

		AssetDatabase.Refresh();
		AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceSynchronousImport );

		var gameObj = target.gameObject;
		var script = AssetDatabase.LoadAssetAtPath( path.MakeRelativePath(), typeof( MonoScript ) ) as MonoScript;
		InternalEditorUtility.AddScriptComponentUnchecked( gameObj, script );

		#region Delayed execution

		// Declared with null value to eliminate "uninitialized variable" 
		// compiler error in lambda below.
		EditorApplication.CallbackFunction callback = null;

		callback = () =>
		{
			AssetDatabase.OpenAsset( script );
			EditorApplication.delayCall -= callback;
		};

		EditorApplication.delayCall += callback;

		#endregion

		Close();
		GUIUtility.ExitGUI();

	}

	private void showOptions()
	{

		className = EditorGUILayout.TextField( "Class Name", className );
		referenceControl = EditorGUILayout.Toggle( "Reference Control", referenceControl );

		//EditComponentReferences();

		using( dfEditorUtil.BeginGroup( "Events" ) )
		{

			var categories = events.Select( x => x.Category ).Distinct().ToList();

			EditorGUILayout.BeginVertical( "TextField", GUILayout.ExpandWidth( true ) );
			{

				eventsScrollPos = EditorGUILayout.BeginScrollView( eventsScrollPos );
				{

					for( int i = 0; i < categories.Count; i++ )
					{

						var category = categories[ i ];
						if( categoryHeader( category ) )
						{

							var categoryEvents = events.Where( x => x.Category == category ).ToList();
							for( int x = 0; x < categoryEvents.Count; x++ )
							{

								var evt = categoryEvents[ x ];

								Rect toggleRect = GUILayoutUtility.GetRect( GUIContent.none, EditorStyles.toggle );
								toggleRect.x += 15;
								toggleRect.width -= 15;

								evt.IsSelected = GUI.Toggle( toggleRect, evt.IsSelected, evt.Field.Name );

							}

						}

					}

				}
				EditorGUILayout.EndScrollView();

			}
			EditorGUILayout.EndVertical();

		}

	}

	private void EditComponentReferences()
	{

		using( dfEditorUtil.BeginGroup( "References" ) )
		{

			if( referencedComponents.Count > 0 )
			{

				var collectionChanged = false;
				for( int i = 0; i < referencedComponents.Count && !collectionChanged; i++ )
				{

					var component = referencedComponents[ i ];

					var header = !string.IsNullOrEmpty( component.Name ) ? component.Name : "Item " + ( i + 1 );
					GUILayout.Label( header );
					EditorGUI.indentLevel += 1;

					var savedColor = GUI.color;
					if( referencedComponents.Count( x => x.Name == component.Name ) > 1 )
						GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.red;

					component.Name = EditorGUILayout.TextField( "Name", component.Name );

					GUI.color = savedColor;

					EditorGUILayout.BeginHorizontal();
					{

						EditorGUILayout.LabelField( "Component", "", GUILayout.Width( LABEL_WIDTH - 14 ) );
						GUILayout.Space( 2 );

						component.Component = (Component)EditorGUILayout.ObjectField( component.Component, typeof( Component ), true );

						if( GUILayout.Button( "X", GUILayout.Width( 22 ) ) )
						{
							referencedComponents.RemoveAt( i );
							collectionChanged = true;
						}

					}
					EditorGUILayout.EndHorizontal();

					EditorGUI.indentLevel -= 1;

				}

			}

			EditorGUILayout.HelpBox( "Drop a component here to add a component referece", MessageType.Info );
			var evt = Event.current;
			if( evt != null )
			{
				Rect textRect = GUILayoutUtility.GetLastRect();
				if( evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform )
				{
					if( textRect.Contains( evt.mousePosition ) )
					{
						var dragged = DragAndDrop.objectReferences;
						DragAndDrop.visualMode = ( dragged.Length > 0 ) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
						if( evt.type == EventType.DragPerform )
						{
							for( int i = 0; i < dragged.Length; i++ )
							{
								Component component = null;
								if( dragged[ i ] is Component )
								{
									component = dragged[ i ] as Component;
								}
								else
								{
									var go = dragged[ i ] as GameObject;
									if( go != null )
									{
										component = go.GetComponents( typeof( Component ) ).FirstOrDefault();
									}
								}
								if( component != null )
								{
									referencedComponents.Add( new ComponentReference( component ) );
								}
							}
						}
						evt.Use();
					}
				}
			}

		}

	}

	private bool categoryHeader( string category )
	{

		var wasExpanded = expandedSections.Contains( category );
		bool nowExpanded = GUILayout.Toggle( wasExpanded, category, EditorStyles.foldout );
		if( nowExpanded != wasExpanded )
		{
			if( nowExpanded )
				expandedSections.Add( category );
			else
				expandedSections.Remove( category );
		}

		return nowExpanded;

	}

	private void showPreview()
	{

		EditorGUILayout.BeginVertical( GUILayout.Width( Mathf.Max( position.width * 0.4f, position.width - 380f ) ) );
		{

			// Reserve room for preview title
			Rect previewHeaderRect = GUILayoutUtility.GetRect( new GUIContent( "Preview" ), "OL Title" );

			// Preview scroll view
			previewScrollPos = EditorGUILayout.BeginScrollView( previewScrollPos, "OL Box" );
			{

				EditorGUILayout.BeginHorizontal();
				{

					// Tiny space since style has no padding in right side
					GUILayout.Space( 5 );

					// Preview text itself
					string previewStr = generateScript();
					Rect r = GUILayoutUtility.GetRect(
						new GUIContent( previewStr ),
						EditorStyles.miniLabel,
						GUILayout.ExpandWidth( true ),
						GUILayout.ExpandHeight( true ) );

					GUI.Label( r, previewStr, EditorStyles.miniLabel );

				} 
				EditorGUILayout.EndHorizontal();

			}
			EditorGUILayout.EndScrollView();

			// Draw preview title after box itself because otherwise the top row
			// of pixels of the slider will overlap with the title
			GUI.Label( previewHeaderRect, new GUIContent( "Preview" ), "OL Title" );

			GUILayout.Space( 4 );

		}
		EditorGUILayout.EndVertical();

	}

	private string generateScript()
	{

		var template = @"
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class @ClassName : MonoBehaviour 
{
@ControlReference
@EventHandlers
}
";

		var controlRefTemplate = @"
	private @ControlType @ControlName;

	// Called by Unity just before any of the Update methods is called the first time.
	public void Start()
	{
		// Obtain a reference to the @ControlType instance attached to this object
		this.@ControlName = GetComponent<@ControlType>();
	}
";

		template = template.Replace( "@ClassName", className );
		template = template.Replace( "@ControlType", target.GetType().Name );
		template = template.Replace( "@ComponentReferences", generateReferenceVariables() );

		if( referenceControl )
		{
			controlRefTemplate = controlRefTemplate.Replace( "@ControlType", target.GetType().Name );
			controlRefTemplate = controlRefTemplate.Replace( "@ControlName", generateControlName( target ) );
			template = template.Replace( "@ControlReference", controlRefTemplate );
		}
		else
		{
			template = template.Replace( "@ControlReference", "" );
		}

		var buffer = new StringBuilder();
		for( int i = 0; i < events.Count; i++ )
		{
			if( events[ i ].IsSelected )
			{
				
				if( buffer.Length > 0 )
					buffer.Append( "\r\n" );

				buffer.Append( events[ i ].Signature );

			}
		}

		template = template.Replace( "@EventHandlers", buffer.ToString() ).Trim();

		return template;

	}

	private string generateReferenceVariables()
	{

		var buffer = new StringBuilder();

		for( int i = 0; i < referencedComponents.Count; i++ )
		{

			var component = referencedComponents[ i ];
			if( component == null || !component.HasValidIdentifier() || component.Component == null )
				continue;

			buffer.Append( component.BuildDeclaration() );

		}

		if( buffer.Length > 0 )
			buffer.Insert( 0, "\r\n" );

		return buffer.ToString().TrimEnd();

	}

	private string generateControlName( dfControl target )
	{

		var name = target.GetType().Name;
		if( name.StartsWith( "df" ) && name.Length > 2 )
		{
			name = char.ToLowerInvariant( name[ 2 ] ) + name.Substring( 3 );
		}

		return "_" + name;

	}

	#region Private utility classes

	private class ComponentReference
	{

		public string Name;
		public Component Component;

		public ComponentReference( Component component )
		{

			this.Component = component;

			var generatedName = "_" + component.name;// +"_" + component.GetType().Name;
			this.Name = Regex.Replace( generatedName, "[^a-zA-Z0-9$_]", "" );

		}

		public ComponentReference( string name, Component component )
		{
			this.Name = name;
			this.Component = component;
		}

		public string BuildDeclaration()
		{

			if( !HasValidIdentifier() || Component == null )
				return "";

			var buffer = new StringBuilder();
			buffer.Append( "\tpublic " );
			buffer.Append( this.Component.GetType().Name );
			buffer.Append( " " );
			buffer.Append( this.Name );
			buffer.Append( ";\r\n" );

			return buffer.ToString();

		}

		public bool HasValidIdentifier()
		{
			return Regex.IsMatch( this.Name, @"[a-zA-Z$_]+[a-zA-Z0-9$_]*" );
		}

	}

	private class EventInfo
	{

		public string Category { get; private set; }
		public FieldInfo Field { get; private set; }
		public string Signature { get; private set; }
		public bool IsSelected { get; set; }

		public EventInfo( FieldInfo field )
		{

			this.Field = field;
			this.Category = getCategory( field );

			this.Signature = getEventSignature( field );

		}

		private string getEventSignature( FieldInfo eventField )
		{

			var invoke = eventField.FieldType.GetMethod( "Invoke" );
			if( invoke == null )
			{
				return "// Could not generate event signature for " + eventField.DeclaringType.Name + "." + eventField.Name;
			}

			var buffer = new StringBuilder();

			buffer.Append( "\tpublic " );

			if( invoke.ReturnType == typeof( void ) )
				buffer.Append( "void" );
			else
				buffer.Append( invoke.ReturnType.Name );

			buffer.Append( " On" + eventField.Name );
			buffer.Append( "( " );

			var paramList = invoke.GetParameters();
			for( int i = 0; i < paramList.Length; i++ )
			{
				if( i > 0 ) buffer.Append( ", " );
				var param = paramList[ i ];
				buffer.Append( getTypeName( param.ParameterType ) );
				buffer.Append( " " );
				buffer.Append( param.Name );
			}

			buffer.Append( " )\r\n\t{\r\n\t\t// Add event handler code here\r\n\t\tDebug.Log( \"" + eventField.Name + "\" );\r\n\t}\r\n" );

			return buffer.ToString();
		
		}

		private string getTypeName( Type type )
		{

			var prefix = type.IsByRef ? "ref " : string.Empty;

			if( !type.FullName.StartsWith( "System." ) )
				return prefix + type.Name;

			var shortName = "";
			if( typeMap.TryGetValue( type.FullName.Replace( "&", "" ), out shortName ) )
				return prefix + shortName;

			return prefix + type.Name;

		}

		private static string getCategory( FieldInfo field )
		{

			var categoryAttribute = field.FieldType
				.GetCustomAttributes( typeof( dfEventCategoryAttribute ), true )
				.FirstOrDefault() as dfEventCategoryAttribute;

			if( categoryAttribute == null )
				return "";

			return categoryAttribute.Category;

		}

	}

	#endregion

}
