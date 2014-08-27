/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;
using System.Collections;

public class dfGUIWizard : EditorWindow
{

	#region Editor menu integration 

	// Slowly migrating menu option locations, will remove older ones as 
	// users become used to the new locations
	[MenuItem( "Tools/Daikon Forge/UI Wizard", false, 0 )]
	[MenuItem( "GameObject/Daikon Forge/UI Wizard", false, 0 )]
	static void ShowGUIWizard()
	{
		var window = GetWindow<dfGUIWizard>();
		window.title = "GUI Wizard";
		window.LoadPrefs();
	}

	#endregion

	#region Private variables 

	private int layer = 0;
	private bool orthographic = true;
	private bool pixelPerfect = true;

	private bool useJoystick = false;
	private KeyCode joystickClickButton = KeyCode.None;
	private string horizontalAxis = "Horizontal";
	private string verticalAxis = "Vertical";

	#endregion

	#region Unity Editor window events 

	private void OnGUI()
	{

		using( dfEditorUtil.BeginGroup( "GUI Manager settings" ) )
		{

			layer = EditorGUILayout.LayerField( "UI Layer", layer );
			if( layer > 30 )
			{
				EditorGUILayout.HelpBox( "Daikon Forge GUI will not work correctly on the selected layer. Please choose another layer.", MessageType.Warning );
				return;
			}

			orthographic = EditorGUILayout.Toggle( "Orthographic", orthographic );
			pixelPerfect = EditorGUILayout.Toggle( "Pixel Perfect", pixelPerfect );

		}

		EditorGUILayout.Separator();

		using( dfEditorUtil.BeginGroup( "GUI Input Manager settings" ) )
		{
			useJoystick = EditorGUILayout.Toggle( "Use Joystick", useJoystick );
			joystickClickButton = (KeyCode)EditorGUILayout.EnumPopup( "Joystick Click Button", joystickClickButton );
			horizontalAxis = EditorGUILayout.TextField( "Horizontal Axis", horizontalAxis );
			verticalAxis = EditorGUILayout.TextField( "Vertical Axis", verticalAxis );
		}

		EditorGUILayout.Separator();

		EditorGUILayout.BeginHorizontal();
		{

			if( GUILayout.Button( "Help" ) )
			{
				Application.OpenURL( "http://www.daikonforge.com/dfgui/getting-started/" );
			}

			if( GUILayout.Button( "Create" ) )
			{
				SavePrefs();
				CreateUI();
			}

		}
		EditorGUILayout.EndHorizontal();

	}

	#endregion

	#region Public methods 

	public void SavePrefs()
	{

		EditorPrefs.SetInt( "DFGUIWizard.Layer", layer );
		EditorPrefs.SetBool( "DFGUIWizard.Ortho", orthographic );
		EditorPrefs.SetBool( "DFGUIWizard.PixelPerfect", pixelPerfect );

		EditorPrefs.SetBool( "DFGUIWizard.UseJoystick", useJoystick );
		EditorPrefs.SetInt( "DFGUIWizard.JoystickClickButton", (int)joystickClickButton );
		EditorPrefs.SetString( "DFGUIWizard.HorizontalAxis", horizontalAxis );
		EditorPrefs.SetString( "DFGUIWizard.VerticalAxis", verticalAxis );

	}

	public void LoadPrefs()
	{

		layer = EditorPrefs.GetInt( "DFGUIWizard.Layer", 0 );
		orthographic = EditorPrefs.GetBool( "DFGUIWizard.Ortho", true );
		pixelPerfect = EditorPrefs.GetBool( "DFGUIWizard.PixelPerfect", true );

		useJoystick = EditorPrefs.GetBool( "DFGUIWizard.UseJoystick", false );
		joystickClickButton = (KeyCode)EditorPrefs.GetInt( "DFGUIWizard.JoystickClickButton", (int)KeyCode.None );
		horizontalAxis = EditorPrefs.GetString( "DFGUIWizard.HorizontalAxis", horizontalAxis );
		verticalAxis = EditorPrefs.GetString( "DFGUIWizard.VerticalAxis", verticalAxis );

	}

	#endregion

	#region Private utility methods 

	private void CreateUI()
	{

		// Make sure other cameras already in the scene don't render the designated layer
		var sceneCameras = FindObjectsOfType( typeof( Camera ) ) as Camera[];
		for( int i = 0; i < sceneCameras.Length; i++ )
		{
			var sceneCamera = sceneCameras[ i ];
			if( sceneCamera.gameObject.activeInHierarchy && sceneCamera.GetComponent<dfGUICamera>() == null )
			{
				sceneCameras[ i ].cullingMask &= ~( 1 << layer );
				sceneCameras[ i ].eventMask &= ~( 1 << layer );
				EditorUtility.SetDirty( sceneCameras[ i ] );
			}
		}

		GameObject go = new GameObject( "UI Root" );
		go.transform.position = new Vector3( -100, 100, 0 );
		go.layer = layer;

		GameObject cam_go = new GameObject( "UI Camera" );
		cam_go.transform.parent = go.transform;
		cam_go.transform.localPosition = Vector3.zero;
		cam_go.transform.localRotation = Quaternion.identity;

		Camera cam = cam_go.AddComponent<Camera>();
		cam.depth = getGuiCameraDepth();
		cam.farClipPlane = 5;
		cam.clearFlags = CameraClearFlags.Depth;
		cam.cullingMask = ( 1 << layer );
		cam.isOrthoGraphic = orthographic;
		 
		dfGUIManager guiManager = go.AddComponent<dfGUIManager>();
		guiManager.RenderCamera = cam;
		guiManager.PixelPerfectMode = pixelPerfect;
		guiManager.UIScaleLegacyMode = false;

		dfInputManager inputManager = go.GetComponent<dfInputManager>();
		inputManager.RenderCamera = cam;
		inputManager.UseJoystick = useJoystick;
		inputManager.JoystickClickButton = joystickClickButton;
		inputManager.HorizontalAxis = horizontalAxis;
		inputManager.VerticalAxis = verticalAxis;
		inputManager.HoverStartDelay = 0f;
		inputManager.HoverNotificationFrequency = 0f;

#if WEB_PLAYER
		guiManager.FixedHeight = PlayerSettings.defaultWebScreenHeight;
		guiManager.RenderCamera.aspect = (float)PlayerSettings.defaultWebScreenWidth / (float)PlayerSettings.defaultWebScreenHeight;
#else
		guiManager.FixedHeight = PlayerSettings.defaultScreenHeight;
		guiManager.RenderCamera.aspect = (float)PlayerSettings.defaultScreenWidth / (float)PlayerSettings.defaultScreenHeight;
#endif

		dfEditorUtil.DelayedInvoke( () =>
		{

			Selection.activeObject = guiManager;

			var scene = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;
			if( scene != null )
			{
				scene.orthographic = true;
				scene.pivot = guiManager.transform.position;
				scene.AlignViewToObject( guiManager.transform );
			}

		} );

		this.Close();

	}

	private float getGuiCameraDepth()
	{

		if( Camera.main != null )
			return Mathf.Max( 1, Camera.main.depth + 1 );

		return 1;

	}

	#endregion

}