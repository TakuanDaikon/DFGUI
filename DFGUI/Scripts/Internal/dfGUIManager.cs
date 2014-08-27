/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The dfGUIManager class is responsible for compiling all control rendering 
/// data into a Mesh and rendering that Mesh to the screen. This class is the 
/// primary workhorse of the DF-GUI library and in conjunction with <see cref="dfControl"/>
/// forms the core of this library's functionality.
/// </summary>
[Serializable]
[ExecuteInEditMode]
[RequireComponent( typeof( BoxCollider ) )]
[RequireComponent( typeof( dfInputManager ) )]
[AddComponentMenu( "Daikon Forge/User Interface/GUI Manager" )]
public class dfGUIManager : MonoBehaviour, IDFControlHost, IComparable<dfGUIManager>
{

	#region Callback and event definitions

	[dfEventCategory( "Modal Dialog" )]
	public delegate void ModalPoppedCallback( dfControl control );

	[dfEventCategory( "Global Callbacks" )]
	public delegate void RenderCallback( dfGUIManager manager );

	/// <summary>
	/// Called before a dfGUIManager instance begins rendering the user interface
	/// </summary>
	public static event RenderCallback BeforeRender;

	/// <summary>
	/// Called after a dfGUIManager instance has finished rendering the user interface
	/// </summary>
	public static event RenderCallback AfterRender;

	#endregion

	#region Serialized protected members

	[SerializeField]
	protected float uiScale = 1f;

	[SerializeField]
	protected bool uiScaleLegacy = true;

	[SerializeField]
	protected dfInputManager inputManager;

	[SerializeField]
	protected int fixedWidth = -1;

	[SerializeField]
	protected int fixedHeight = 600;

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected dfFontBase defaultFont;

	[SerializeField]
	protected bool mergeMaterials = false;

	[SerializeField]
	protected bool pixelPerfectMode = true;

	[SerializeField]
	protected Camera renderCamera = null;

	[SerializeField]
	protected bool generateNormals = false;

	[SerializeField]
	protected bool consumeMouseEvents = false;

	[SerializeField]
	protected bool overrideCamera = false;

	[SerializeField]
	protected int renderQueueBase = 3000;

	[SerializeField]
	public List<dfDesignGuide> guides = new List<dfDesignGuide>();

	#endregion

	#region Static fields

	/// <summary>
	/// Keeps track of all active dfGUIManager instances
	/// </summary>
	private static List<dfGUIManager> activeInstances = new List<dfGUIManager>();

	/// <summary>
	/// Global reference to the control that currently has input focus
	/// </summary>
	private static dfControl activeControl = null;

	/// <summary>
	/// Used to maintain a stack of "modal" controls
	/// </summary>
	private static Stack<ModalControlReference> modalControlStack = new Stack<ModalControlReference>();

	#endregion

	#region Private non-serialized fields

	private dfGUICamera guiCamera;
	private Mesh[] renderMesh;
	private MeshFilter renderFilter;
	private MeshRenderer meshRenderer;
	private int activeRenderMesh = 0;
	private int cachedChildCount = 0;
	private bool isDirty;
	private bool abortRendering;
	private Vector2 cachedScreenSize;
	private Vector3[] corners = new Vector3[ 4 ];

	private dfList<Rect> occluders = new dfList<Rect>( 256 );

	private Stack<dfTriangleClippingRegion> clipStack = new Stack<dfTriangleClippingRegion>();
	private static dfRenderData masterBuffer = new dfRenderData( 4096 );
	private dfList<dfRenderData> drawCallBuffers = new dfList<dfRenderData>();
	private dfList<dfRenderGroup> renderGroups = new dfList<dfRenderGroup>();
	private List<int> submeshes = new List<int>();
	private int drawCallCount = 0;
	private Vector2 uiOffset = Vector2.zero;
	private static Plane[] clippingPlanes;

	private dfList<int> drawCallIndices = new dfList<int>();
	private dfList<dfControl> controlsRendered = new dfList<dfControl>();

	private bool shutdownInProcess = false;
	private int suspendCount = 0;

	#endregion

	#region Public properties

	public static IEnumerable<dfGUIManager> ActiveManagers { get { return activeInstances; } }

	/// <summary>Returns the total number of draw calls required to render this <see cref="dfGUIManager"/> instance during the last frame </summary>
	public int TotalDrawCalls { get; private set; }

	/// <summary>Returns the total number of triangles this <see cref="dfGUIManager"/> instance rendered during the last frame </summary>
	public int TotalTriangles { get; private set; }

	/// <summary>Returns the total number of controls this <see cref="dfGUIManager"/> instance rendered during the last frame </summary>
	public int NumControlsRendered { get; private set; }

	/// <summary>Returns the total number of frames this <see cref="dfGUIManager"/> instance has rendered </summary>
	public int FramesRendered { get; private set; }

	/// <summary> Returns the list of controls actually rendered in the last render pass</summary>
	public IList<dfControl> ControlsRendered { get { return controlsRendered; } }

	/// <summary> Returns a list of indices into the ControlsRendered collection where each draw call started </summary>
	public IList<int> DrawCallStartIndices { get { return drawCallIndices; } }

	/// <summary>
	/// Gets or sets the base value that will be used for the <a href="http://docs.unity3d.com/Documentation/ScriptReference/Material-renderQueue.html" target="_blank">Render Queue</a>
	/// </summary>
	public int RenderQueueBase
	{
		get { return this.renderQueueBase; }
		set
		{
			if( value != this.renderQueueBase )
			{
				this.renderQueueBase = value;
				RefreshAll();
			}
		}
	}

	/// <summary>
	/// Returns a reference to the <see cref="dfControl"/> instance that currently has input focus
	/// </summary>
	public static dfControl ActiveControl { get { return activeControl; } }

	/// <summary>
	/// Gets or sets the multiplier by which the entire UI will be scaled
	/// </summary>
	public float UIScale
	{
		get { return this.uiScale; }
		set
		{
			if( !Mathf.Approximately( value, this.uiScale ) )
			{
				this.uiScale = value;
				onResolutionChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the UIScale property will use Legacy Mode (scales entire UI)
	/// </summary>
	public bool UIScaleLegacyMode
	{
		get { return this.uiScaleLegacy; }
		set
		{
			if( value != uiScaleLegacy )
			{
				this.uiScaleLegacy = value;
				onResolutionChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount and direction of "offset" for the entire
	/// user interface. Useful for "panning" or implementing "shake".
	/// </summary>
	public Vector2 UIOffset
	{
		get { return this.uiOffset; }
		set
		{
			if( !Vector2.Equals( this.uiOffset, value ) )
			{
				this.uiOffset = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Returns the <see cref="UnityEngine.Camera"/> that is used to render 
	/// the <see cref="dfGUIManager"/> and all of its controls
	/// </summary>
	public Camera RenderCamera
	{
		get { return renderCamera; }
		set
		{
			if( !object.ReferenceEquals( renderCamera, value ) )
			{

				this.renderCamera = value;
				Invalidate();

				if( value != null && value.gameObject.GetComponent<dfGUICamera>() == null )
				{
					value.gameObject.AddComponent<dfGUICamera>();
				}

				if( this.inputManager != null )
				{
					this.inputManager.RenderCamera = value;
				}

			}
		}
	}

	/// <summary>
	/// Gets/Sets a value indicating whether the GUIManager should attempt
	/// to consolidate drawcalls that use the same Material instance. This 
	/// can reduce the number of draw calls in some cases, but depending on 
	/// scene complexity may also affect control render order.
	/// </summary>
	public bool MergeMaterials
	{
		get { return this.mergeMaterials; }
		set
		{
			if( value != this.mergeMaterials )
			{
				this.mergeMaterials = value;
				invalidateAllControls();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether normals and tangents will be generated for the 
	/// rendered output, which is needed by some shaders. Defaults to FALSE.
	/// </summary>
	public bool GenerateNormals
	{
		get { return this.generateNormals; }
		set
		{
			if( value != this.generateNormals )
			{

				this.generateNormals = value;

				if( this.renderMesh != null )
				{
					renderMesh[ 0 ].Clear();
					renderMesh[ 1 ].Clear();
				}

				dfRenderData.FlushObjectPool();
				invalidateAllControls();

			}
		}
	}

	/// <summary>
	/// Gets/Sets a value indicating whether controls should be resized at
	/// runtime to retain the same pixel dimensions as design time. If this
	/// value is set to TRUE, controls will always remain at the same pixel
	/// resolution regardless of the resolution of the game. If set to FALSE,
	/// controls will be scaled to fit the target resolution.
	/// </summary>
	public bool PixelPerfectMode
	{
		get { return this.pixelPerfectMode; }
		set
		{
			if( value != this.pixelPerfectMode )
			{
				this.pixelPerfectMode = value;
				onResolutionChanged();
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The default <see cref="dfAtlas">Texture Atlas</see> containing the images used
	/// to render controls in this <see cref="dfGUIManager"/>. New controls added to the
	/// scene will use this Atlas by default.
	/// </summary>
	public dfAtlas DefaultAtlas
	{
		get { return atlas; }
		set
		{
			if( !dfAtlas.Equals( value, atlas ) )
			{
				this.atlas = value;
				invalidateAllControls();
			}
		}
	}

	/// <summary>
	/// The default <see cref="dfFont">Bitmapped Font</see> that will be assigned
	/// to new controls added to the scene
	/// </summary>
	public dfFontBase DefaultFont
	{
		get { return defaultFont; }
		set
		{
			if( value != this.defaultFont )
			{
				this.defaultFont = value;
				invalidateAllControls();
			}
		}
	}

	/// <summary>
	/// Returns the width of the target screen size
	/// </summary>
	public int FixedWidth
	{
		get { return this.fixedWidth; }
		set
		{
			if( value != this.fixedWidth )
			{
				this.fixedWidth = value;
				onResolutionChanged();
			}
		}
	}

	/// <summary>
	/// Gets/Sets the height of the target screen size.
	/// </summary>
	public int FixedHeight
	{
		get { return this.fixedHeight; }
		set
		{
			if( value != this.fixedHeight )
			{
				var previousValue = this.fixedHeight;
				this.fixedHeight = value;

				onResolutionChanged( previousValue, value );

			}
		}
	}

	/// <summary>
	/// Gets or sets whether mouse events generated on an active control
	/// will be "consumed" (unavailable for other in-game processing)
	/// </summary>
	public bool ConsumeMouseEvents
	{
		get { return this.consumeMouseEvents; }
		set { this.consumeMouseEvents = value; }
	}

	/// <summary>
	/// Gets or sets whether user scripts will override the RenderCamera's
	/// settins. If set to TRUE, then user scripts will be responsible for
	/// all camera settings on the UI camera
	/// </summary>
	public bool OverrideCamera
	{
		get { return this.overrideCamera; }
		set { this.overrideCamera = value; }
	}

	#endregion

	#region Unity events

	public void OnApplicationQuit()
	{
		shutdownInProcess = true;
	}

	public void OnGUI()
	{

		if( overrideCamera || !consumeMouseEvents || !Application.isPlaying || occluders == null )
			return;

		// This code prevents "click through" by iterating 
		// through the list of rendered control positions
		// and determining if the mouse or touch currently
		// overlaps. If it does overlap, then a GUI.Box()
		// occluder is rendered and the current event is 
		// consumed.

		// NOTE: This does not account for controls that are only partially "clipped", as the
		// clipped portion will still block mouse clicks due to the fact that DFGUI does not
		// currently clip the control exclusion rectangles, and doing so is likely to have a
		// nontrivial impact on per-frame performance for low-powered mobile devices.
		
		// NOTE: Using GUI drawing functions to block mouse input is pathologically slow
		// on Windows Phone 8 (and potentially other mobile devices), resulting in a significant 
		// reduction in frame rates. Unfortunately, Unity does not give *any other way* to block 
		// the built-in SendMouseEvents processing, so there is no other option. You can either 
		// block mouse input processing from sending the built-in OnMouseXXX events, or have better 
		// framerates. You apparently cannot have both.

		var mousePosition = Input.mousePosition;
		mousePosition.y = Screen.height - mousePosition.y;

		if( modalControlStack.Count > 0 )
		{
			// If there is a modal control/window being displayed, block 
			// mouse/touch input for the entire screen.
			GUI.Box( new Rect( 0, 0, Screen.width, Screen.height ), GUIContent.none, GUIStyle.none );
		}
		else
		{

			Rect lastRect = new Rect();

			// Block mouse/touch input for each screen area where a control was rendered
			for( int i = 0; i < occluders.Count; i++ )
			{

				var occluder = occluders[ i ];

				if( Event.current.isMouse && occluder.Contains( mousePosition ) )
					Event.current.Use();
				else if( !lastRect.Contains( occluder ) )
					GUI.Box( occluder, GUIContent.none, GUIStyle.none );

				lastRect = occluder;

			}
		}

		// NOTE: Not using inputManager.touchInputSource here because we're only concerned
		// with Unity's built-in touch processing when attempting to account for "click-through".
		if( Event.current.isMouse && Input.touchCount > 0 )
		{
			var touchCount = Input.touchCount;
			for( var i = 0; i < touchCount; i++ )
			{
				var touch = Input.GetTouch( i );
				if( touch.phase == TouchPhase.Began )
				{

					var touchPosition = touch.position;
					touchPosition.y = Screen.height - touchPosition.y;

					if( occluders.Any( x => x.Contains( touchPosition ) ) )
					{
						Event.current.Use();
						break;
					}

				}
			}
		}

	}

	/// <summary>
	/// Awake is called by the Unity engine when the script instance is being loaded.
	/// </summary>
	public void Awake()
	{

		// Clean up any render data that might have been allocated on a previous level
		dfRenderData.FlushObjectPool();

	}

	/// <summary>
	/// This function is called by the Unity engine when the object becomes enabled and current.
	/// </summary>
	public void OnEnable()
	{

		var sceneCameras = Camera.allCameras;
		for( var i = 0; i < sceneCameras.Length; i++ )
		{

			// Get rid of Unity's extremely annoying tendency to print errors
			// about being unable to call SendMouseEventXXX because the event
			// signatures don't match. Whose idea was that, anyways? Sheesh.
			sceneCameras[ i ].eventMask &= ~( 1 << gameObject.layer );

		}

		// Ensure that we have a reference to the MeshRenderer
		if( this.meshRenderer == null )
		{
			initialize();
		}

		// Explicitly disabling the layout stage improves performance, most notably on
		// very slow mobile platforms
		this.useGUILayout = !this.ConsumeMouseEvents;

		activeInstances.Add( this );

		FramesRendered = 0;
		TotalDrawCalls = 0;
		TotalTriangles = 0;

		if( meshRenderer != null )
		{
			meshRenderer.enabled = true;
		}

		inputManager = GetComponent<dfInputManager>() ?? gameObject.AddComponent<dfInputManager>();
		inputManager.RenderCamera = this.RenderCamera;

		FramesRendered = 0;

		if( meshRenderer != null )
		{
			meshRenderer.enabled = true;
		}

		if( Application.isPlaying )
		{
			onResolutionChanged();
		}

		Invalidate();

	}

	/// <summary>
	/// This function is called by the Unity engine when the control becomes 
	/// disabled or inactive.
	/// </summary>
	public void OnDisable()
	{

		activeInstances.Remove( this );

		if( meshRenderer != null )
		{
			meshRenderer.enabled = false;
		}

		resetDrawCalls();

	}

	public void OnDestroy()
	{

		if( activeInstances.Count == 0 )
		{
			dfMaterialCache.Clear();
		}

		if( renderMesh == null || renderFilter == null )
			return;

		renderFilter.sharedMesh = null;

		DestroyImmediate( renderMesh[ 0 ] );
		DestroyImmediate( renderMesh[ 1 ] );

		renderMesh = null;

	}

	/// <summary>
	/// Start is called by the Unity engine before any of the <see cref="Update"/> 
	/// methods is called for the first time
	/// </summary>
	public void Start()
	{

		var sceneCameras = Camera.allCameras;
		for( var i = 0; i < sceneCameras.Length; i++ )
		{

			// Get rid of Unity's extremely annoying tendency to print errors
			// about being unable to call SendMouseEventXXX because the event
			// signatures don't match. Whose idea was that, anyways? Sheesh.
			sceneCameras[ i ].eventMask &= ~( 1 << gameObject.layer );

		}

	}

	/// <summary>
	/// Called by the Unity engine every frame if the control component is enabled
	/// </summary>
	public void Update()
	{

		// Sort the active instances by RenderQueueBase value
		activeInstances.Sort();

		if( this.renderCamera == null || !enabled )
		{
			if( meshRenderer != null )
			{
				meshRenderer.enabled = false;
			}
			return;
		}

		if( this.renderMesh == null || this.renderMesh.Length == 0 )
		{

			initialize();

			// Gets rid of annoying flash when recompiling in the Editor 
			// but we don't actually want to refresh this early otherwise
			if( Application.isEditor && !Application.isPlaying )
			{
				Render();
			}

		}

		if( cachedChildCount != transform.childCount )
		{
			cachedChildCount = transform.childCount;
			Invalidate();
		}

		// If the screen size has changed since we last checked we need to let all
		// controls know about the new screen size so that they can reposition or 
		// resize themselves, etc.
		var currentScreenSize = GetScreenSize();
		if( ( currentScreenSize - cachedScreenSize ).sqrMagnitude > float.Epsilon )
		{
			onResolutionChanged( cachedScreenSize, currentScreenSize );
			cachedScreenSize = currentScreenSize;
		}

		// NOTE: Experimental - can we now remove the asinine "scene is always dirty" bit?
#if UNITY_EDITOR && EXPERIMENTAL
		// HACK: The following code makes sure that the UI is aways updated while
		// in design mode, and is a workaround for a Unity quirk where the scene's
		// materials are reset in some specific situations, which causes the unchanged
		// UI to render with elements out of order. This quirk is harmless in the 
		// sense that your UI will still render correctly when the application is 
		// playing, but makes the design-time experience somewhat distracting.
		//
		// NOTE: This can cause the scene in the editor to always be flagged as 
		// unsaved. If this bothers you, you can comment out the following code, 
		// but will then have to contend with the issue described above.
		//
		if( !Application.isPlaying )
		{
			// Setting isDirty to TRUE signals the GUIManager to redraw
			// the user interface on the next LateUpdate pass
			isDirty = true;
		}
#endif

	}

	/// <summary>
	/// Called by the Unity engine every frame (after <see cref="Update"/>) if
	/// the control component is enabled
	/// </summary>
	public void LateUpdate()
	{

		if( this.renderMesh == null || this.renderMesh.Length == 0 )
		{
			initialize();
		}

		if( !Application.isPlaying )
		{

#if UNITY_EDITOR
			// Needed for proper raycasting in Editor viewport, which doesn't 
			// update with the same frequency as the runtime application
			updateRenderOrder();
#endif

			var boxCollider = this.collider as BoxCollider;
			if( boxCollider != null )
			{
				var size = this.GetScreenSize() * PixelsToUnits();
				boxCollider.center = Vector3.zero;
				boxCollider.size = size;
			}

		}

		// The first active dfGUIManager instance is responsible for rendering
		// all dfGUIManager instances. 
		if( activeInstances[ 0 ] == this )
		{

			// Rebuild all dynamic font atlases (if needed) before rendering
			dfFontManager.RebuildDynamicFonts();

			var updateMaterials = false;

			for( int i = 0; i < activeInstances.Count; i++ )
			{

				var instance = activeInstances[ i ];

				if( instance.isDirty && instance.suspendCount <= 0 )
				{

					updateMaterials = true;

					instance.abortRendering = false;
					instance.isDirty = false;
					instance.Render();

				}

			}

			if( updateMaterials )
			{

				// Reset the material cache before rendering
				dfMaterialCache.Reset();

				updateDrawCalls();

				for( int i = 0; i < activeInstances.Count; i++ )
				{
					activeInstances[ i ].updateDrawCalls();
				}

			}

		}

	}

#if UNITY_EDITOR

	[HideInInspector]
	private bool isVisibleToSceneCamera()
	{

		var selected = UnityEditor.Selection.activeGameObject;
		if( selected != null && selected.transform.IsChildOf( this.transform ) )
			return true;

		var bounds = this.collider.bounds;

		var sceneCameras = UnityEditor.SceneView.GetAllSceneCameras();
		for( int i = 0; i < sceneCameras.Length; i++ )
		{
			var frustum = GeometryUtility.CalculateFrustumPlanes( sceneCameras[ i ] );
			if( GeometryUtility.TestPlanesAABB( frustum, bounds ) )
			{
				UnityEditor.SceneView.currentDrawingSceneView.Repaint();
				return true;
			}
		}

		return false;

	}

	[HideInInspector]
	public void OnDrawGizmos()
	{

		if( meshRenderer != null )
		{
			var debugShowMesh = UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowMesh", false );
			UnityEditor.EditorUtility.SetSelectedWireframeHidden( meshRenderer, !debugShowMesh );
		}

		collider.hideFlags = HideFlags.HideInInspector;

		// Calculate the screen size in "pixels"
		var screenSize = GetScreenSize() * PixelsToUnits();

		// Rendering a clear cube allows the user to click on the control
		// in the Unity Editor Scene Manager
		Gizmos.color = Color.clear;
		var back = transform.forward * 0.02f;
		Gizmos.DrawCube( transform.position + back, screenSize + Vector2.one * 150 * PixelsToUnits() );

		// Render the outline of the view
		Gizmos.color = new UnityEngine.Color( 0, 1, 0, 0.3f );
		var corners = GetCorners();
		for( int i = 0; i < corners.Length; i++ )
		{
			Gizmos.DrawLine( corners[ i ], corners[ ( i + 1 ) % corners.Length ] );
		}

		#region Show "Safe Area"

		var showSafeArea = UnityEditor.EditorPrefs.GetBool( "ShowSafeArea", false );
		if( showSafeArea )
		{

			var safeAreaMargin = UnityEditor.EditorPrefs.GetFloat( "SafeAreaMargin", 10f ) * 0.01f;

			// Scale corners
			var scale = 1f - safeAreaMargin;
			var center = corners[ 0 ] + ( corners[ 2 ] - corners[ 0 ] ) * 0.5f;
			for( int i = 0; i < corners.Length; i++ )
			{
				corners[ i ] = Vector3.Lerp( center, corners[ i ], scale );
			}

			Gizmos.color = new UnityEngine.Color( 0, 1, 0, 0.5f );
			for( int i = 0; i < corners.Length; i++ )
			{
				Gizmos.DrawLine( corners[ i ], corners[ ( i + 1 ) % corners.Length ] );
			}

		}

		#endregion

		// If viewing the Game tab with gizmos on, there will be no 
		// currentDrawingSceneView, so skip the rest of this function
		if( UnityEditor.SceneView.currentDrawingSceneView == null )
			return;

		if( UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowRulers", true ) )
		{
			drawRulers();
		}

		if( UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowGrid", false ) )
		{
			drawGrid();
		}

		if( !Application.isPlaying && UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowGuides", true ) )
		{
			drawDesignGuides();
		}

	}

	[HideInInspector]
	public void OnDrawGizmosSelected()
	{

		try
		{
			if( !UnityEditor.Selection.activeGameObject.transform.IsChildOf( this.transform ) )
			{
				return;
			}
		}
		catch
		{
			return;
		}

		// Render the outline of the view
		Gizmos.color = UnityEngine.Color.green;
		var worldCorners = GetCorners();
		for( var i = 0; i < worldCorners.Length; i++ )
		{
			Gizmos.DrawLine( worldCorners[ i ], worldCorners[ ( i + 1 ) % worldCorners.Length ] );
		}

	}

	private void drawDesignGuides()
	{

		if( !UnityEditor.SceneView.currentDrawingSceneView.orthographic )
			return;

		var isSelected = UnityEditor.Selection.activeGameObject == this.gameObject;

		var worldCorners = GetCorners();

		for( var i = 0; i < guides.Count; i++ )
		{

			var color = Color.magenta;
			if( UnityEditor.Selection.activeGameObject != this.gameObject )
				color.a = 0.5f;
			else
				color.a = 0.7f;

			Vector3 pos1;
			Vector3 pos2;

			var guide = guides[ i ];
			if( guide.orientation == dfControlOrientation.Vertical )
			{
				pos1 = Vector3.Lerp( worldCorners[ 0 ], worldCorners[ 1 ], (float)guide.position / (float)FixedWidth );
				pos2 = Vector3.Lerp( worldCorners[ 3 ], worldCorners[ 2 ], (float)guide.position / (float)FixedWidth );
			}
			else
			{
				pos1 = Vector3.Lerp( worldCorners[ 0 ], worldCorners[ 3 ], (float)guide.position / (float)FixedHeight );
				pos2 = Vector3.Lerp( worldCorners[ 1 ], worldCorners[ 2 ], (float)guide.position / (float)FixedHeight );
			}

			var screenPos1 = UnityEditor.HandleUtility.WorldToGUIPoint( pos1 );
			var screenPos2 = UnityEditor.HandleUtility.WorldToGUIPoint( pos2 );
			if( isSelected && distanceFromLine( screenPos1, screenPos2, Event.current.mousePosition ) < 5 )
			{
				color.a = 1f;
			}

			Gizmos.color = color;
			Gizmos.DrawLine( pos1, pos2 );

		}

	}

	private static float distanceFromLine( Vector3 start, Vector3 end, Vector3 test )
	{

		var v = start - end;
		var w = test - end;

		var c1 = Vector3.Dot( w, v );
		if( c1 <= 0 )
			return Vector3.Distance( test, end );

		var c2 = Vector3.Dot( v, v );
		if( c2 <= c1 )
			return Vector3.Distance( test, start );

		var b = c1 / c2;
		var pb = end + b * v;

		return Vector3.Distance( test, pb );

	}

	private void drawGrid()
	{

		if( !UnityEditor.SceneView.currentDrawingSceneView.orthographic )
			return;

		var worldCorners = GetCorners();

		const int SHOW_GRID_THRESHOLD = 512;

		// If the on-screen view is too zoomed out, don't show the rulers
		var screenUL = UnityEditor.HandleUtility.WorldToGUIPoint( worldCorners[ 0 ] );
		var screenLR = UnityEditor.HandleUtility.WorldToGUIPoint( worldCorners[ 2 ] );
		var screenSize = Vector3.Distance( screenUL, screenLR );
		if( screenSize < SHOW_GRID_THRESHOLD )
			return;

		Gizmos.color = new UnityEngine.Color( 0, 1, 0, 0.075f );

		var gridSize = UnityEditor.EditorPrefs.GetInt( "dfGUIManager.GridSize", 20 );
		if( gridSize < 5 )
			return;

		for( var x = 0; x < FixedWidth; x += gridSize )
		{

			var pos1 = Vector3.Lerp( worldCorners[ 0 ], worldCorners[ 1 ], (float)x / (float)FixedWidth );
			var pos2 = Vector3.Lerp( worldCorners[ 3 ], worldCorners[ 2 ], (float)x / (float)FixedWidth );

			Gizmos.DrawLine( pos1, pos2 );

		}

		for( var y = 0; y < FixedHeight; y += gridSize )
		{

			var pos1 = Vector3.Lerp( worldCorners[ 0 ], worldCorners[ 3 ], (float)y / (float)FixedHeight );
			var pos2 = Vector3.Lerp( worldCorners[ 1 ], worldCorners[ 2 ], (float)y / (float)FixedHeight );

			Gizmos.DrawLine( pos1, pos2 );

		}

	}

	private void drawRulers()
	{

		if( !UnityEditor.SceneView.currentDrawingSceneView.orthographic )
			return;

		var worldCorners = GetCorners();

		const int SHOW_RULERS_THRESHOLD = 128;
		const int SHOW_SMALL_THRESHOLD = 200;
		const int INCREASE_LINESIZE_THRESHOLD = 768;
		const int SHOW_TICKS_THRESHOLD = 1280;

		// If the on-screen view is too zoomed out, don't show the rulers
		var screenUL = UnityEditor.HandleUtility.WorldToGUIPoint( worldCorners[ 0 ] );
		var screenLL = UnityEditor.HandleUtility.WorldToGUIPoint( worldCorners[ 3 ] );
		var screenSize = Vector3.Distance( screenUL, screenLL );
		if( screenSize < SHOW_RULERS_THRESHOLD )
			return;

		Gizmos.color = new UnityEngine.Color( 0, 1, 0, 0.5f );

		var lerpMult = Mathf.Lerp( 3, 1, INCREASE_LINESIZE_THRESHOLD / screenSize );
		var lineSize = Mathf.Lerp( 20f, 5f, SHOW_SMALL_THRESHOLD / screenSize ) / Vector3.Distance( screenUL, screenLL ) * lerpMult;

		var up = Vector3.up * lineSize;
		var left = Vector3.left * lineSize;

		for( var x = 0; x < FixedWidth; x += 2 )
		{

			var pos1 = Vector3.Lerp( worldCorners[ 0 ], worldCorners[ 1 ], (float)x / (float)FixedWidth );
			var pos2 = Vector3.Lerp( worldCorners[ 3 ], worldCorners[ 2 ], (float)x / (float)FixedWidth );

			if( x % 50 == 0 )
			{
				Gizmos.DrawLine( pos1, pos1 + up * 3 );
				Gizmos.DrawLine( pos2, pos2 - up * 3 );
			}
			else if( screenSize >= SHOW_SMALL_THRESHOLD && x % 10 == 0 )
			{
				Gizmos.DrawLine( pos1, pos1 + up );
				Gizmos.DrawLine( pos2, pos2 - up );
			}
			else if( screenSize >= SHOW_TICKS_THRESHOLD )
			{
				Gizmos.DrawLine( pos1, pos1 + up * 0.5f );
				Gizmos.DrawLine( pos2, pos2 - up * 0.5f );
			}

		}

		for( var y = 0; y < FixedHeight; y += 2 )
		{

			var pos1 = Vector3.Lerp( worldCorners[ 0 ], worldCorners[ 3 ], (float)y / (float)FixedHeight );
			var pos2 = Vector3.Lerp( worldCorners[ 1 ], worldCorners[ 2 ], (float)y / (float)FixedHeight );

			if( y % 50 == 0 )
			{
				Gizmos.DrawLine( pos1, pos1 + left * 3 );
				Gizmos.DrawLine( pos2, pos2 - left * 3 );
			}
			else if( screenSize >= SHOW_SMALL_THRESHOLD && y % 10 == 0 )
			{
				Gizmos.DrawLine( pos1, pos1 + left );
				Gizmos.DrawLine( pos2, pos2 - left );
			}
			else if( screenSize >= SHOW_TICKS_THRESHOLD )
			{
				Gizmos.DrawLine( pos1, pos1 + left * 0.5f );
				Gizmos.DrawLine( pos2, pos2 - left * 0.5f );
			}

		}

		// Draw lines at ends of rulers, looks kind of unfinished otherwise
		Gizmos.DrawLine( worldCorners[ 1 ], worldCorners[ 1 ] + up * 3 );
		Gizmos.DrawLine( worldCorners[ 2 ], worldCorners[ 2 ] - left * 3 );
		Gizmos.DrawLine( worldCorners[ 2 ], worldCorners[ 2 ] - up * 3 );
		Gizmos.DrawLine( worldCorners[ 3 ], worldCorners[ 3 ] + left * 3 );

	}

#endif

	#endregion

	#region Public methods

	/// <summary>
	/// Suspends rendering until ResumeRendering is called
	/// </summary>
	/// <returns></returns>
	public void SuspendRendering()
	{
		suspendCount += 1;
	}

	/// <summary>
	/// Resumes rendering after a call to SuspendRendering()
	/// </summary>
	public void ResumeRendering()
	{

		if( suspendCount == 0 )
			return;

		if( --suspendCount == 0 )
		{
			Invalidate();
		}

	}

	/// <summary>
	/// Returns the top-most rendered control under the given screen position.
	/// NOTE: the <paramref name="screenPosition"/> parameter should be
	/// in "screen coordinates", such as the value from Input.mousePosition
	/// </summary>
	/// <param name="screenPosition">The screen position to check</param>
	/// <returns></returns>
	public static dfControl HitTestAll( Vector2 screenPosition )
	{

		var hitResult = (dfControl)null;
		var hitCameraDepth = float.MinValue;

		for( var i = 0; i < activeInstances.Count; i++ )
		{
			
			var view = activeInstances[ i ];
			var viewCamera = view.RenderCamera;

			if( viewCamera.depth < hitCameraDepth )
				continue;

			var test = view.HitTest( screenPosition );
			if( test != null )
			{
				hitResult = test;
				hitCameraDepth = viewCamera.depth;
			}

		}

		return hitResult;

	}

	/// <summary>
	/// Returns the top-most rendered control under the given screen position.
	/// NOTE: the <paramref name="screenPosition"/> parameter should be
	/// in "screen coordinates", such as the value from Input.mousePosition
	/// </summary>
	/// <param name="screenPosition">The screen position to check</param>
	/// <returns></returns>
	public dfControl HitTest( Vector2 screenPosition )
	{

		var ray = renderCamera.ScreenPointToRay( screenPosition );
		var maxDistance = renderCamera.farClipPlane - renderCamera.nearClipPlane;
		var modalControl = dfGUIManager.GetModalControl();

		// Caching these values can result in a small perf. boost
		var controls = controlsRendered;
		var controlCount = controls.Count;
		var items = controls.Items;

		// Every control rendered should have a corresponding occluder
		if( occluders.Count != controlCount )
		{
			Debug.LogWarning( "Occluder count does not match control count" );
			return null;
		}

		// "Massage" screen position to check against Rect occluders
		var occluderPosition = screenPosition;
		occluderPosition.y = RenderCamera.pixelHeight - screenPosition.y;

		// NOTE: This function takes advantage of the "inside knowledge" that the 
		// ControlsRendered list is compiled in "render order", where the order of 
		// the contained controls exactly matches the control render order, and so
		// exactly matches which controls are "on top" of other controls. Iterating
		// the list backwards will always return the "topmost" control that intersects
		// the mouse position and which is not disabled or invisible.
		for( var i = controlCount - 1; i >= 0; i-- )
		{

			if( !occluders[ i ].Contains( occluderPosition ) )
				continue;

			var control = items[ i ];
			if( control == null )
				continue;

			RaycastHit hitInfo;
			if( control.collider == null || !control.collider.Raycast( ray, out hitInfo, maxDistance ) )
				continue;

			var skipControl =
				( modalControl != null && !control.transform.IsChildOf( modalControl.transform ) ) ||
				!control.IsInteractive ||
				!control.IsEnabled;

			if( skipControl )
				continue;

			if( isInsideClippingRegion( hitInfo.point, control ) )
			{
				return control;
			}

		}

		return null;

	}

	/// <summary>
	/// Returns the GUI coordinates of a point in 3D space
	/// </summary>
	/// <param name="worldPoint"></param>
	/// <returns></returns>
	public Vector2 WorldPointToGUI( Vector3 worldPoint )
	{
		// Return screen point as GUI coordinate point 
		var mainCamera = Camera.main ?? renderCamera;
		return ScreenToGui( mainCamera.WorldToScreenPoint( worldPoint ) );
	}

	/// <summary>
	/// Returns a value indicating the size in 3D Units that corresponds to a single 
	/// on-screen pixel, based on the current value of the FixedHeight property.
	/// </summary>
	public float PixelsToUnits()
	{
		var fixedPixelSize = 2f / (float)FixedHeight;
		return fixedPixelSize * this.UIScale;
	}

	/// <summary>
	/// Returns the set of clipping planes used to clip child controls.
	/// Planes are specified in the following order: Left, Right, Top, Bottom
	/// </summary>
	/// <returns>Returns an array of <see cref="Plane"/> that enclose the object in world coordinates</returns>
	public Plane[] GetClippingPlanes()
	{

		var worldCorners = GetCorners();

		var right = transform.TransformDirection( Vector3.right );
		var left = transform.TransformDirection( Vector3.left );
		var up = transform.TransformDirection( Vector3.up );
		var down = transform.TransformDirection( Vector3.down );

		if( clippingPlanes == null )
		{
			clippingPlanes = new Plane[ 4 ];
		}

		clippingPlanes[ 0 ] = new Plane( right, worldCorners[ 0 ] );
		clippingPlanes[ 1 ] = new Plane( left, worldCorners[ 1 ] );
		clippingPlanes[ 2 ] = new Plane( up, worldCorners[ 2 ] );
		clippingPlanes[ 3 ] = new Plane( down, worldCorners[ 0 ] );

		return clippingPlanes;

	}

	/// <summary>
	/// Returns an array of Vector3 values corresponding to the global 
	/// positions of this object's bounding box. The corners are specified
	/// in the following order: Top Left, Top Right, Bottom Right, Bottom Left
	/// </summary>
	public Vector3[] GetCorners()
	{

		var p2u = PixelsToUnits();
		var size = GetScreenSize() * p2u;
		var width = size.x;
		var height = size.y;

		var upperLeft = new Vector3( -width * 0.5f, height * 0.5f );
		var upperRight = upperLeft + new Vector3( width, 0 );
		var bottomLeft = upperLeft + new Vector3( 0, -height );
		var bottomRight = upperRight + new Vector3( 0, -height );

		var matrix = transform.localToWorldMatrix;

		corners[ 0 ] = matrix.MultiplyPoint( upperLeft );
		corners[ 1 ] = matrix.MultiplyPoint( upperRight );
		corners[ 2 ] = matrix.MultiplyPoint( bottomRight );
		corners[ 3 ] = matrix.MultiplyPoint( bottomLeft );

		return corners;

	}

	/// <summary>
	/// Returns a <see cref="Vector2"/> value representing the width and 
	/// height of the screen. When the application is running, this value 
	/// will be the correct size of the screen if PixelPerfectMode is 
	/// turned off, or the size of the "virtual" screen otherwise. 
	/// When called in the Editor at design time, this function will return 
	/// the "design" size of the screen, which is derived from the value 
	/// of the <see cref="FixedWidth"/> and <see cref="FixedHeight"/> 
	/// properties.
	/// </summary>
	public Vector2 GetScreenSize()
	{

		var renderCamera = RenderCamera;

		// If the application is running and the UI is set to "pixel perfect"
		// mode, return the actual screen size. Cannot return the actual 
		// screen size while the application is not running, because Unity
		// always returns a value of 640x480 for some reason.
		var returnActualScreenSize =
			Application.isPlaying &&
			renderCamera != null;

		Vector2 result = Vector2.zero;
		if( returnActualScreenSize )
		{

			var activeScale = PixelPerfectMode ? 1 : ( renderCamera.pixelHeight / (float)fixedHeight );
			result = ( new Vector2( renderCamera.pixelWidth, renderCamera.pixelHeight ) / activeScale ).CeilToInt();

			if( uiScaleLegacy )
				result *= this.uiScale;
			else
				result /= this.uiScale;

		}
		else
		{

			result = new Vector2( FixedWidth, FixedHeight );

			if( !uiScaleLegacy )
				result /= this.uiScale;
		
		}

		return result;

	}

	/// <summary>
	/// Adds a new control of the specified type to the scene
	/// </summary>
	/// <typeparam name="T">The Type of control to create</typeparam>
	/// <returns>A reference to the new <see cref="dfControl"/>instance</returns>
	public T AddControl<T>() where T : dfControl
	{
		return (T)AddControl( typeof( T ) );
	}

	/// <summary>
	/// Adds a new control of the specified type to the scene
	/// </summary>
	/// <param name="controlType">The Type of control to create - Must derive from <see cref="dfControl"/></param>
	/// <returns>A reference to the new <see cref="dfControl"/>instance</returns>
	public dfControl AddControl( Type controlType )
	{

		if( !typeof( dfControl ).IsAssignableFrom( controlType ) )
			throw new InvalidCastException();

		var go = new GameObject( controlType.Name );
		go.transform.parent = this.transform;
		go.layer = this.gameObject.layer;

		var child = go.AddComponent( controlType ) as dfControl;
		child.ZOrder = getMaxZOrder() + 1;

		return child;

	}

	/// <summary>
	/// Adds the child control to the list of child controls for this instance
	/// </summary>
	/// <param name="child">The <see cref="dfControl"/> instance to add to the list of child controls</param>
	public void AddControl( dfControl child )
	{
		// Not much needed here, but need the method available to satisfy IDFControlHost interface
		child.transform.parent = this.transform;
	}

	/// <summary>
	/// Instantiates a new instance of the specified prefab and adds it to the control hierarchy
	/// </summary>
	/// <param name="prefab"></param>
	/// <returns></returns>
	public dfControl AddPrefab( GameObject prefab )
	{

		// Ensure that the prefab contains a dfControl component
		if( prefab.GetComponent<dfControl>() == null )
		{
			throw new InvalidCastException();
		}

		var go = Instantiate( prefab ) as GameObject;
		go.transform.parent = this.transform;
		go.layer = this.gameObject.layer;

		var child = go.GetComponent<dfControl>();
		child.transform.parent = this.transform;
		child.PerformLayout();

		BringToFront( child );

		return child;

	}

	/// <summary>
	/// Returns the render data for a specific draw call
	/// </summary>
	/// <param name="drawCallNumber">The index of the draw call to retrieve render information for</param>
	public dfRenderData GetDrawCallBuffer( int drawCallNumber )
	{
		return drawCallBuffers[ drawCallNumber ];
	}

	/// <summary>
	/// Returns a reference to the currently-current modal control, if any. 
	/// </summary>
	/// <returns>A reference to the currently-current modal control if one exists, NULL otherwise</returns>
	public static dfControl GetModalControl()
	{
		return ( modalControlStack.Count > 0 ) ? modalControlStack.Peek().control : null;
	}

	/// <summary>
	/// Converts "screen space" coordinates (y-up with origin at the bottom left corner of
	/// the screen) to gui coordinates (y-down with the origin at the top left corner of
	/// the screen)
	/// </summary>
	/// <param name="position">The screen-space coordinate to convert</param>
	public Vector2 ScreenToGui( Vector2 position )
	{

		// Need virtual screen size to know how much to adjust screen position by
		var screenSize = GetScreenSize();

		// Obtain a reference to the main camera
		var mainCamera = Camera.main ?? renderCamera;

		// Scale the movement amount by the difference between the "virtual" 
		// screen size and the real screen size
		position.x = screenSize.x * ( position.x / mainCamera.pixelWidth );
		position.y = screenSize.y * ( position.y / mainCamera.pixelHeight );

		// GUI coordinates start at the the top-left corner and 
		// increase downward for positive y-values
		position.y = screenSize.y - position.y;

		return position;

	}

	/// <summary>
	/// Push a control onto the modal control stack. When a control is modal, only that control
	/// and all of its descendants will receive user input events.
	/// </summary>
	/// <param name="control">The control to make modal</param>
	public static void PushModal( dfControl control )
	{
		PushModal( control, null );
	}

	/// <summary>
	/// Push a control onto the modal control stack. When a control is modal, only that control
	/// and all of its descendants will receive user input events.
	/// </summary>
	/// <param name="control">The control to make modal</param>
	/// <param name="callback">A function that will be called when the control is popped off of the modal stack. Can be null.</param>
	public static void PushModal( dfControl control, ModalPoppedCallback callback )
	{

		if( control == null )
			throw new NullReferenceException( "Cannot call PushModal() with a null reference" );

		modalControlStack.Push( new ModalControlReference()
		{
			control = control,
			callback = callback
		} );

	}

	/// <summary>
	/// Pop the current modal control from the modal control stack.
	/// </summary>
	public static void PopModal()
	{

		if( modalControlStack.Count == 0 )
			throw new InvalidOperationException( "Modal stack is empty" );

		var entry = modalControlStack.Pop();
		if( entry.callback != null )
		{
			entry.callback( entry.control );
		}

	}

	/// <summary>
	/// Sets input focus to the indicated control
	/// </summary>
	/// <param name="control">The control that should receive user input</param>
	public static void SetFocus( dfControl control )
	{

		if( activeControl == control || ( control != null && !control.CanFocus ) )
			return;

		var prevFocus = activeControl;
		activeControl = control;

		var args = new dfFocusEventArgs( control, prevFocus );

		var prevFocusChain = dfList<dfControl>.Obtain();
		if( prevFocus != null )
		{
			var loop = prevFocus;
			while( loop != null )
			{
				prevFocusChain.Add( loop );
				loop = loop.Parent;
			}
		}

		var newFocusChain = dfList<dfControl>.Obtain();
		if( control != null )
		{
			var loop = control;
			while( loop != null )
			{
				newFocusChain.Add( loop );
				loop = loop.Parent;
			}
		}

		if( prevFocus != null )
		{

			prevFocusChain.ForEach( c =>
			{
				if( !newFocusChain.Contains( c ) )
				{
					c.OnLeaveFocus( args );
				}
			} );

			prevFocus.OnLostFocus( args );

		}

		if( control != null )
		{

			newFocusChain.ForEach( c =>
			{
				if( !prevFocusChain.Contains( c ) )
				{
					c.OnEnterFocus( args );
				}
			} );

			control.OnGotFocus( args );

		}

		newFocusChain.Release();
		prevFocusChain.Release();

	}

	/// <summary>
	/// Returns TRUE if the control currently has input focus, FALSE otherwise.
	/// </summary>
	/// <param name="control">The <see cref="dfControl"/> instance to test for input focus</param>
	public static bool HasFocus( dfControl control )
	{

		if( control == null )
			return false;

		return ( activeControl == control );

	}

	/// <summary>
	/// Returns TRUE if the control or any of its descendants currently has input focus, FALSE otherwise.
	/// </summary>
	/// <param name="control">The <see cref="dfControl"/> instance to test for input focus</param>
	public static bool ContainsFocus( dfControl control )
	{

		if( activeControl == control )
			return true;

		if( activeControl == null || control == null )
			return object.ReferenceEquals( activeControl, control );

		return activeControl.transform.IsChildOf( control.transform );

	}

	/// <summary>
	/// Brings the control to the front so that it will display over any other control 
	/// within the same container.
	/// </summary>
	/// <param name="control">The control instance to bring to front</param>
	public void BringToFront( dfControl control )
	{

		if( control.Parent != null )
			control = control.GetRootContainer();

		using( var allControls = getTopLevelControls() )
		{

			var maxIndex = 0;

			for( int i = 0; i < allControls.Count; i++ )
			{
				var test = allControls[ i ];
				if( test != control )
				{
					test.ZOrder = maxIndex++;
				}
			}

			control.ZOrder = maxIndex;

			Invalidate();

		}

	}

	/// <summary>
	/// Brings the control to the front so that it will display behind any other control 
	/// within the same container.
	/// </summary>
	/// <param name="control">The control instance to send to back</param>
	public void SendToBack( dfControl control )
	{

		if( control.Parent != null )
			control = control.GetRootContainer();

		using( var allControls = getTopLevelControls() )
		{

			var maxIndex = 1;

			for( int i = 0; i < allControls.Count; i++ )
			{
				var test = allControls[ i ];
				if( test != control )
				{
					test.ZOrder = maxIndex++;
				}
			}

			control.ZOrder = 0;

			Invalidate();

		}

	}

	/// <summary>
	/// Invalidates the user interface and requests a refresh, which will be performed
	/// on the next frame.
	/// </summary>
	public void Invalidate()
	{

		if( isDirty == true )
			return;

		// Setting isDirty to TRUE signals the GUIManager to redraw
		// the user interface on the next LateUpdate pass
		isDirty = true;

		// Make sure all render settings are correctly configured
		updateRenderSettings();

	}

	/// <summary>
	/// Flags all dfGUIManager instances as needing to be re-rendered. The rendering will occur 
	/// on the next frame rather than immediately.
	/// </summary>
	public static void InvalidateAll()
	{

		for( int i = 0; i < activeInstances.Count; i++ )
		{
			activeInstances[ i ].Invalidate();
		}

	}

	/// <summary>
	/// Refresh all <see cref="dfGUIManager instances"/> and ensure that all <see cref="dfControl"/>
	/// instances are forced to refresh as well.
	/// </summary>
	public static void RefreshAll()
	{
		RefreshAll( false );
	}
	
	/// <summary>
	/// Refresh all <see cref="dfGUIManager instances"/> and ensure that all <see cref="dfControl"/>
	/// instances are forced to refresh as well.
	/// </summary>
	/// <param name="force">Set to TRUE to force each <see cref="dfGUIManager"/> instance to refresh immediately</param>
	public static void RefreshAll( bool force )
	{

		var views = activeInstances;
		for( int i = 0; i < views.Count; i++ )
		{

			var view = views[ i ];

			// Skip uninitialized/hidden views
			if( view.renderMesh == null || view.renderMesh.Length == 0 )
			{
				continue;
			}

			// Ensure that all of the view's controls will be re-rendered 
			view.invalidateAllControls();

			// Only force a Refresh() while in the editor, 'cause 
			// Unity sucks at design-time refresh and we're trying 
			// to use it as a visual designer. Otherwise, it's better
			// to wait until the next Update() call while running.
			if( force || !Application.isPlaying )
			{
				view.Render();
			}

		}

#if UNITY_EDITOR
		if( force && UnityEditor.SceneView.currentDrawingSceneView != null )
		{
			UnityEditor.SceneView.currentDrawingSceneView.Repaint();
		}
#endif

	}

	/// <summary>
	/// Causes the rendering process to be aborted. For internal use only.
	/// </summary>
	// @cond DOXY_IGNORE
	internal void AbortRender()
	{
		abortRendering = true;
	}
	// @endcond DOXY_IGNORE

	/// <summary>
	/// Rebuild the user interface mesh and update the renderer so that the UI will
	/// be presented to the user on the next frame. <b>NOTE</b> : This function is
	/// automatically called internally and should not be called by user code.
	/// </summary>
	public void Render()
	{

		if( meshRenderer == null )
			return;

		meshRenderer.enabled = false;

		FramesRendered += 1;

		if( BeforeRender != null )
		{
			BeforeRender( this );
		}

		try
		{

			// TODO: Make sure that having updateRenderSettings() in Invalidate is sufficient
			//updateRenderSettings();

			// Clear occluders and ensure at least enough memory for the number of controls 
			// that were rendered in the last pass
			occluders.Clear();
			occluders.EnsureCapacity( NumControlsRendered );

			// We'll be keeping track of how many controls were actually rendered,
			// as opposed to just how many exist in the scene.
			NumControlsRendered = 0;
			controlsRendered.Clear();
			drawCallIndices.Clear();
			renderGroups.Clear();

			// Other stats to be tracked for informational purposes
			TotalDrawCalls = 0;
			TotalTriangles = 0;

			if( RenderCamera == null || !enabled )
			{
				if( meshRenderer != null )
				{
					meshRenderer.enabled = false;
				}
				return;
			}

			if( meshRenderer != null && !meshRenderer.enabled )
			{
				meshRenderer.enabled = true;
			}

			if( renderMesh == null || renderMesh.Length == 0 )
			{
				Debug.LogError( "GUI Manager not initialized before Render() called" );
				return;
			}

			resetDrawCalls();

			// Define the main draw call buffer, which will be assigned as needed
			// by the renderControl() method
			var buffer = (dfRenderData)null;

			// Initialize the clipping region stack
			clipStack.Clear();
			clipStack.Push( dfTriangleClippingRegion.Obtain() );

			// This checksum is used to determine whether cached render and 
			// clipping data is still valid, and represents a unique checksum
			// of the rendering path for each control.
			uint checksum = dfChecksumUtil.START_VALUE;

			#region Render all current controls

			//@Profiler.BeginSample( "Render all controls" );

			using( var controls = getTopLevelControls() )
			{

				updateRenderOrder( controls );

				for( int i = 0; i < controls.Count && !abortRendering; i++ )
				{
					var control = controls[ i ];
					//@Profiler.BeginSample( "Render control: " + control.GetType().Name );
					renderControl( ref buffer, control, checksum, 1f );
					//@Profiler.EndSample();
				}

			}

			// Components can request that rendering be aborted. If so, then throw an exception
			// that exits the rendering loop
			if( abortRendering )
			{
				clipStack.Clear();
				throw new dfAbortRenderingException();
			}

			//@Profiler.EndSample();

			#endregion

			// Remove any empty draw call buffers. There might be empty 
			// draw call buffers due to controls that were clipped.
			drawCallBuffers.RemoveAll( isEmptyBuffer );
			drawCallCount = drawCallBuffers.Count;

			// At this point, the drawCallCount variable contains the 
			// number of draw calls needed to render the user interface.
			this.TotalDrawCalls = drawCallCount;
			if( drawCallBuffers.Count == 0 )
			{
				if( renderFilter.sharedMesh != null )
				{
					renderFilter.sharedMesh.Clear();
				}
				return;
			}

			// Consolidate all draw call buffers into one master buffer 
			// that will be used to build the Mesh
			var masterBuffer = compileMasterBuffer();
			this.TotalTriangles = masterBuffer.Triangles.Count / 3;

			//@Profiler.BeginSample( "Buiding render mesh" );

			// Build the master mesh
			var mesh = renderFilter.sharedMesh = getRenderMesh();
			mesh.Clear( true );
			mesh.vertices = masterBuffer.Vertices.Items;
			mesh.uv = masterBuffer.UV.Items;
			mesh.colors32 = masterBuffer.Colors.Items;

			// Only set the mesh normals and tangents if the GUIManager has 
			// been asked to generate that information
			if( generateNormals )
			{
				// Set the mesh normals (for lighting effects, etc)
				// TODO: Determine why normal buffer length may not be exact match for vertice buffer length on first frame
				if( masterBuffer.Normals.Items.Length == masterBuffer.Vertices.Items.Length )
				{
					mesh.normals = masterBuffer.Normals.Items;
					mesh.tangents = masterBuffer.Tangents.Items;
				}
			}

			//@Profiler.EndSample();

			#region Set sub-meshes

			//@Profiler.BeginSample( "Building draw call submeshes" );

			mesh.subMeshCount = submeshes.Count;
			for( int i = 0; i < submeshes.Count; i++ )
			{

				// Calculate the start and length of the submesh array
				var startIndex = submeshes[ i ];
				var length = masterBuffer.Triangles.Count - startIndex;
				if( i < submeshes.Count - 1 )
				{
					length = submeshes[ i + 1 ] - startIndex;
				}

				var submeshTriangles = dfTempArray<int>.Obtain( length );
				masterBuffer.Triangles.CopyTo( startIndex, submeshTriangles, 0, length );

				// Set the submesh's triangle index array
				mesh.SetTriangles( submeshTriangles, i );

			}

			//@Profiler.EndSample();

			#endregion

			// The clip stack is reset after every frame as it's only needed during rendering
			if( clipStack.Count != 1 ) Debug.LogError( "Clip stack not properly maintained" );
			clipStack.Pop().Release();
			clipStack.Clear();

			// Make sure that the render settings are properly assigned
			updateRenderSettings();

		}
		catch( dfAbortRenderingException )
		{
			// Do nothing... This exception is thrown by any component that requires
			// the rendering pipeline to be aborted. For instance, the dfDynamicFont
			// class will throw this exception when the dynamic font atlas was 
			// rebuilt, which causes any control rendered with that dynamic font
			// to be invalid and require re-rendering.
			isDirty = true;
			abortRendering = false;
		}
		finally
		{

			meshRenderer.enabled = true;

			if( AfterRender != null )
			{
				AfterRender( this );
			}

		}

	}

	#endregion

	#region Private utility methods

	/// <summary>
	/// Iterates through all dfRenderGroup components in this UI hierarchy and commands
	/// them to update their render queues
	/// </summary>
	private void updateDrawCalls()
	{

		if( meshRenderer == null )
		{
			initialize();
		}

		// Gather all Material instances needed for every draw call
		var drawCallMaterials = gatherMaterials();
		meshRenderer.sharedMaterials = drawCallMaterials;

#if UNITY_EDITOR && DEBUG

		// NOTE: There is an assumption within gatherMaterials() that it is
		// safe to use dfTempArray<Material> to hold the Materials, because
		// Unity does not hold on to the array reference. This can be proven
		// by testing the result of the .sharedMaterials property getter 
		// to see if this result is the same array reference that was assigned.
		// The code below only runs in Editor mode, and will inform me when
		// and if that behavior is ever changed in some future version of Unity.
		//var testArray = meshRenderer.sharedMaterials;
		//if( object.ReferenceEquals( testArray, drawCallMaterials ) )
		//{
		//	Debug.LogError( "MeshRenderer.sharedMaterials property behavior has changed. See comments in source by double-clicking this error", this );
		//}

#endif

		var renderQueue = this.renderQueueBase + drawCallMaterials.Length;

		var items = renderGroups.Items;
		var itemCount = renderGroups.Count;

		for( int i = 0; i < itemCount; i++ )
		{
			items[ i ].UpdateRenderQueue( ref renderQueue );
		}

	}

	/// <summary>
	/// Determines whether the raycast point on the given control is
	/// inside of the control's clip region hierarchy
	/// </summary>
	/// <param name="control"></param>
	/// <returns></returns>
	private static bool isInsideClippingRegion( Vector3 point, dfControl control )
	{

		while( control != null )
		{

			var planes = control.ClipChildren ? control.GetClippingPlanes() : null;
			if( planes != null && planes.Length > 0 )
			{
				for( int i = 0; i < planes.Length; i++ )
				{
					if( !planes[ i ].GetSide( point ) )
					{
						return false;
					}
				}
			}

			control = control.Parent;

		}

		return true;

	}

	private int getMaxZOrder()
	{

		var max = -1;
		using( var controls = getTopLevelControls() )
		{
			for( int i = 0; i < controls.Count; i++ )
			{
				max = Mathf.Max( max, controls[ i ].ZOrder );
			}
		}

		return max;

	}

	private bool isEmptyBuffer( dfRenderData buffer )
	{
		return buffer.Vertices.Count == 0;
	}

	private dfList<dfControl> getTopLevelControls()
	{

		try
		{

			//@Profiler.BeginSample( "Gather top-level controls" );

			var childCount = transform.childCount;

			var controls = dfList<dfControl>.Obtain( childCount );

			var controlItems = dfControl.ActiveInstances.Items;
			var controlCount = dfControl.ActiveInstances.Count;

			for( int i = 0; i < controlCount; i++ )
			{

				var control = controlItems[ i ];
				if( control.IsTopLevelControl( this ) )
					controls.Add( control );

			}

			controls.Sort();

			return controls;

		}
		finally
		{
			//@Profiler.EndSample();
		}

	}

	private void updateRenderSettings()
	{

		// If the user is still setting up the GUIManager, exit if mandatory
		// components are not yet created
		var camera = RenderCamera;
		if( camera == null )
			return;

		if( !overrideCamera )
		{
			updateRenderCamera( camera );
		}

		#region Enforce uniform scaling

		if( transform.hasChanged )
		{

			// Need to ensure that any scaling done is uniform. If the user 
			// attempts to change this manually (or even accidentally) it 
			// could screw up many things.
			//
			// Note that scaling the GUI Manager is not something that should
			// be done unless you have a very specific and unusual use case,
			// and is not necessary or even desirable otherwise.

			var scale = transform.localScale;
			var constrainScale =
				scale.x < float.Epsilon ||
				!Mathf.Approximately( scale.x, scale.y ) ||
				!Mathf.Approximately( scale.x, scale.z );

			if( constrainScale )
			{
				scale.y = scale.z = scale.x = Mathf.Max( scale.x, 0.001f );
				transform.localScale = scale;
			}

		}

		#endregion

		if( !overrideCamera )
		{

			// Since everything is positioned and sized according to a "design-time" pixel
			// size, we can scale the entire UI to fit actual pixel sizes by modifying
			// the camera's OrthographicSize or FOV property.
			if( Application.isPlaying && PixelPerfectMode )
			{

				var uiScale = camera.pixelHeight / (float)fixedHeight;

				camera.orthographicSize = uiScale;
				camera.fieldOfView = 60 * uiScale;

			}
			else
			{
				camera.orthographicSize = 1f;
				camera.fieldOfView = 60f;
			}

		}

		// TODO: Is setting Camera.transparencySortMode still needed?
		camera.transparencySortMode = TransparencySortMode.Orthographic;

		// cachedScreenSize is used to detect when the screen size changes, such 
		// as when the user resizes the application window
		if( cachedScreenSize.sqrMagnitude <= float.Epsilon )
		{
			cachedScreenSize = new Vector2( FixedWidth, FixedHeight );
		}

		// Resetting the hasChanged flag allows us to know when the transforms
		// have changed. This is very important because it allows us to avoid 
		// some expensive operations unless they are necessary.
		transform.hasChanged = false;

	}

	private void updateRenderCamera( Camera camera )
	{

		// If rendering to a RenderTexture, set the appropriate flags
		if( Application.isPlaying && camera.targetTexture != null )
		{
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = Color.clear;
		}
		else
		{
			camera.clearFlags = CameraClearFlags.Depth;
		}

		// Make sure that the orthographic camera is set up to properly 
		// render the user interface. This should be correct by default,
		// but can get out of whack if the user switches between Perspective
		// and Orthographic views. This also helps the user during initial
		// setup of the user interface hierarchy.
		var cameraPosition = Application.isPlaying ? -(Vector3)uiOffset * PixelsToUnits() : Vector3.zero;
		if( camera.isOrthoGraphic )
		{
			camera.nearClipPlane = Mathf.Min( camera.nearClipPlane, -1f );
			camera.farClipPlane = Mathf.Max( camera.farClipPlane, 1f );
		}
		else
		{

			// http://stackoverflow.com/q/2866350/154165
			var fov = camera.fieldOfView * Mathf.Deg2Rad;
			var corners = this.GetCorners();
			var scaleAdjust = uiScaleLegacy ? 1 : this.uiScale;
			var width = Vector3.Distance( corners[ 3 ], corners[ 0 ] ) * scaleAdjust;
			var distance = width / ( 2f * Mathf.Tan( fov / 2f ) );
			var back = transform.TransformDirection( Vector3.back ) * distance;

			camera.farClipPlane = Mathf.Max( distance * 2f, camera.farClipPlane );
			cameraPosition += back / uiScale;

		}

		var screenHeight = camera.pixelHeight;
		var pixelSize = ( 2f / screenHeight ) * ( (float)screenHeight / (float)FixedHeight );

		// Calculate a half-pixel offset for the camera, if needed
		if( Application.isPlaying && needHalfPixelOffset() )
		{

			// NOTE: The direction of the offset below is significant and should
			// not be changed. It doesn't match some of the other examples I've 
			// seen, but works well with the particulars of the DFGUI library.
			var offset = new Vector3(
				pixelSize * 0.5f,
				pixelSize * -0.5f,
				0
			);

			cameraPosition += offset;

		}

		if( !overrideCamera )
		{

			// Camera should specifically use forward rendering only, things go wonky if it's left 
			// as RenderingPath.UsePlayerSettings and the user is using deferred rendering.
			camera.renderingPath = RenderingPath.Forward;

			// Compensate for odd screen dimensions
			if( Screen.width % 2 != 0 ) cameraPosition.x += pixelSize * 0.5f;
			if( Screen.height % 2 != 0 ) cameraPosition.y += pixelSize * 0.5f;

			// Adjust camera position if needed
			if( Vector3.SqrMagnitude( camera.transform.localPosition - cameraPosition ) > float.Epsilon )
			{
				camera.transform.localPosition = cameraPosition;
			}

			camera.transform.hasChanged = false;

		}

	}

	private dfRenderData compileMasterBuffer()
	{

		try
		{

			//@Profiler.BeginSample( "Compiling master buffer" );

			submeshes.Clear();
			masterBuffer.Clear();

			var buffers = drawCallBuffers.Items;

			var masterBufferSize = 0;

			for( int i = 0; i < drawCallCount; i++ )
			{
				masterBufferSize += buffers[ i ].Vertices.Count;
			}

			masterBuffer.EnsureCapacity( masterBufferSize );

			for( int i = 0; i < drawCallCount; i++ )
			{

				submeshes.Add( masterBuffer.Triangles.Count );

				var buffer = buffers[ i ];

				if( generateNormals && buffer.Normals.Count == 0 )
				{
					generateNormalsAndTangents( buffer );
				}

				masterBuffer.Merge( buffer, false );

			}

			// Translate the "world" coordinates in the buffer back into local 
			// coordinates relative to this GUIManager. This allows the GUIManager to be 
			// positioned anywhere in the scene without being distracting
			masterBuffer.ApplyTransform( transform.worldToLocalMatrix );

			return masterBuffer;

		}
		finally
		{
			//@Profiler.EndSample();
		}

	}

	private void generateNormalsAndTangents( dfRenderData buffer )
	{

		var normal = buffer.Transform.MultiplyVector( Vector3.back ).normalized;

		var tangent = (Vector4)buffer.Transform.MultiplyVector( Vector3.right ).normalized;
		tangent.w = -1f;

		for( int i = 0; i < buffer.Vertices.Count; i++ )
		{
			buffer.Normals.Add( normal );
			buffer.Tangents.Add( tangent );
		}

	}

	private bool? applyHalfPixelOffset = null;
	private bool needHalfPixelOffset()
	{

		if( applyHalfPixelOffset.HasValue )
			return applyHalfPixelOffset.Value;

		var platform = Application.platform;
		var needsHPO =
			pixelPerfectMode &&
			(
				platform == RuntimePlatform.WindowsPlayer ||
				platform == RuntimePlatform.WindowsWebPlayer ||
				platform == RuntimePlatform.WindowsEditor
			) &&
			SystemInfo.graphicsDeviceVersion.ToLower().StartsWith( "direct" );

		var d3d11 = SystemInfo.graphicsShaderLevel >= 40;

		applyHalfPixelOffset = ( Application.isEditor || needsHPO ) && !d3d11;

		return needsHPO;

	}

	private Material[] gatherMaterials()
	{

		try
		{

			//@Profiler.BeginSample( "Gather render materials" );

			// Count the number of non-null materials 
			var materialCount = getMaterialCount();
			var materialIndex = 0;

			var materialRenderQueue = renderQueueBase;

			var renderMaterials = dfTempArray<Material>.Obtain( materialCount );
			for( int i = 0; i < drawCallBuffers.Count; i++ )
			{

				var buffer = drawCallBuffers[ i ];

				// Skip null Material instances (typically happens only during
				// initial control creation in the Unity Editor)
				if( buffer.Material == null )
					continue;

				// Obtain a reference to the material that will be used to render
				// the buffer. In simple cases this will be the same instance that 
				// was passed in, but if a new draw call is required then it may
				// return a copy of the original in order to be able to set the
				// copy's [renderQueue] property so that render order is preserved.
				var drawCallMaterial = dfMaterialCache.Lookup( buffer.Material );
				drawCallMaterial.mainTexture = buffer.Material.mainTexture;
				drawCallMaterial.shader = buffer.Shader ?? drawCallMaterial.shader;
				drawCallMaterial.renderQueue = materialRenderQueue++;

				// Disable shader-based clipping (use dfControlRender component for shader clipping)
				drawCallMaterial.mainTextureOffset = Vector2.zero;
				drawCallMaterial.mainTextureScale = Vector2.zero;

				// Copy the material to the final buffer
				renderMaterials[ materialIndex++ ] = drawCallMaterial;	// Copy the material to the final buffer

			}

			return renderMaterials;

		}
		finally
		{
			//@Profiler.EndSample();
		}

	}

	private int getMaterialCount()
	{

		var materialCount = 0;

		for( int i = 0; i < drawCallCount; i++ )
		{
			if( drawCallBuffers[ i ] != null && drawCallBuffers[ i ].Material != null )
				materialCount += 1;
		}

		return materialCount;

	}

	private void resetDrawCalls()
	{

		drawCallCount = 0;

		for( int i = 0; i < drawCallBuffers.Count; i++ )
		{
			// Release the draw call buffer back to the RenderData pool
			drawCallBuffers[ i ].Release();
		}

		drawCallBuffers.Clear();

	}

	private dfRenderData getDrawCallBuffer( Material material )
	{

		var buffer = (dfRenderData)null;

		if( MergeMaterials && material != null )
		{
			buffer = findDrawCallBufferByMaterial( material );
			if( buffer != null )
			{
				return buffer;
			}
		}

		buffer = dfRenderData.Obtain();
		buffer.Material = material;

		drawCallBuffers.Add( buffer );
		drawCallCount++;

		return buffer;

	}

	private dfRenderData findDrawCallBufferByMaterial( Material material )
	{

		for( int i = 0; i < drawCallCount; i++ )
		{
			if( drawCallBuffers[ i ].Material == material )
			{
				return drawCallBuffers[ i ];
			}
		}

		return null;

	}

	private Mesh getRenderMesh()
	{
		activeRenderMesh = ( activeRenderMesh == 1 ) ? 0 : 1;
		return renderMesh[ activeRenderMesh ];
	}

	private void renderControl( ref dfRenderData buffer, dfControl control, uint checksum, float opacity )
	{

		// Don't render controls that are not currently active
		if( !control.enabled || !control.gameObject.activeSelf )
			return;

		// Keeping a running accumulator for opacity allows us to know a control's final
		// calculated opacity
		var effectiveOpacity = opacity * control.Opacity;

		// If this control has a dfRenderGroup component on it, then pass off all 
		// responsibility for rendering that control to the component.
		var renderGroup = dfRenderGroup.GetRenderGroupForControl( control, true );
		if( renderGroup != null && renderGroup.enabled )
		{
			renderGroups.Add( renderGroup );
			renderGroup.Render( renderCamera, control, occluders, controlsRendered, checksum, effectiveOpacity );
			return;
		}

		// Don't render controls that are invisible (Opacity value is effectively zero). 
		// Don't render controls that have the IsVisible flag set to FALSE. Note that this is tested
		// *after* checking for the presence of a dfRenderGroup component, since that component (if
		// present) will need to update its own internal state if the control's IsVisible property
		// is changed.
		if( effectiveOpacity <= 0.001f || !control.GetIsVisibleRaw() )
			return;

		// Grab the current clip region information off the stack
		var clipInfo = clipStack.Peek();

		// Update the checksum to include the current control
		checksum = dfChecksumUtil.Calculate( checksum, control.Version );

		// Retrieve the control's bounds, which will be used in intersection testing
		// and triangle clipping.
		var bounds = control.GetBounds();

		// Indicates whether the control was not rendered because it fell outside
		// of the currently-active clipping region
		var wasClipped = false;

		if( !( control is IDFMultiRender ) )
		{

			// Ask the control to render itself and return a buffer of the 
			// information needed to render it as a Mesh
			var controlData = control.Render();
			if( controlData != null )
			{
				processRenderData( ref buffer, controlData, ref bounds, checksum, clipInfo, ref wasClipped );
			}

		}
		else
		{

			// Ask the control to render itself and return as many dfRenderData buffers
			// as needed to render all elements of the control as a Mesh
			var childBuffers = ( (IDFMultiRender)control ).RenderMultiple();

			if( childBuffers != null )
			{

				var buffers = childBuffers.Items;
				var bufferCount = childBuffers.Count;

				for( int i = 0; i < bufferCount; i++ )
				{

					var childBuffer = buffers[ i ];
					if( childBuffer != null )
					{
						processRenderData( ref buffer, childBuffer, ref bounds, checksum, clipInfo, ref wasClipped );
					}

				}

			}

		}

		// Allow control to keep track of its clipping state
		control.setClippingState( wasClipped );

		#region  Keep track of the number of controls rendered and where they appear on screen

		NumControlsRendered += 1;
		occluders.Add( getControlOccluder( control ) );

		controlsRendered.Add( control );

		// Keep track of controls are associated with which draw call
		drawCallIndices.Add( drawCallBuffers.Count - 1 );

		#endregion 

		// If the control has the "Clip child controls" option set, push
		// its clip region information onto the stack so that all controls
		// lower in the hierarchy are clipped against that region.
		if( control.ClipChildren )
		{
			clipInfo = dfTriangleClippingRegion.Obtain( clipInfo, control );
			clipStack.Push( clipInfo );
		}

		// Dereference raw child control list for direct access
		var childControls = control.Controls.Items;
		var childCount = control.Controls.Count;

		// Ensure lists contain enough space for child controls
		controlsRendered.EnsureCapacity( controlsRendered.Count + childCount );
		occluders.EnsureCapacity( occluders.Count + childCount );

		// Render all child controls
		for( int i = 0; i < childCount; i++ )
		{
			renderControl( ref buffer, childControls[ i ], checksum, effectiveOpacity );
		}

		// No longer need the current control's clip region information
		if( control.ClipChildren )
		{
			clipStack.Pop().Release();
		}

	}

	private Rect getControlOccluder( dfControl control )
	{

		// Do not prevent "click through" on non-interactive controls
		if( !control.IsInteractive )
			return new Rect();

		var screenRect = control.GetScreenRect();

		var hotZoneSize = new Vector2(
			screenRect.width * control.HotZoneScale.x,
			screenRect.height * control.HotZoneScale.y
		);

		var difference = new Vector2(
			hotZoneSize.x - screenRect.width,
			hotZoneSize.y - screenRect.height
		) * 0.5f;

		return new Rect(
			screenRect.x - difference.x,
			screenRect.y - difference.y,
			hotZoneSize.x,
			hotZoneSize.y
		);

	}

	private bool processRenderData( ref dfRenderData buffer, dfRenderData controlData, ref Bounds bounds, uint checksum, dfTriangleClippingRegion clipInfo, ref bool wasClipped )
	{

		wasClipped = false;

		// This shouldn't happen in practice, but need to make certain
		if( controlData == null || controlData.Material == null || !controlData.IsValid() )
			return false;

		// A new draw call is needed every time the current Material, Texture, or Shader
		// changes. If the control returned a buffer that is not empty and uses a 
		// different Material, need to grab a new draw call buffer from the object pool.
		bool needNewDrawcall = false;
		if( buffer == null )
		{
			needNewDrawcall = true;
		}
		else
		{
			if( !Material.Equals( controlData.Material, buffer.Material ) )
			{
				needNewDrawcall = true;
			}
			else if( !textureEqual( controlData.Material.mainTexture, buffer.Material.mainTexture ) )
			{
				needNewDrawcall = true;
			}
			else if( !shaderEqual( buffer.Shader, controlData.Shader ) )
			{
				needNewDrawcall = true;
			}
		}

		if( needNewDrawcall )
		{
			buffer = getDrawCallBuffer( controlData.Material );
			buffer.Material = controlData.Material;
			buffer.Material.mainTexture = controlData.Material.mainTexture;
			buffer.Material.shader = controlData.Shader ?? controlData.Material.shader;
		}

		// Ensure that the control's render data is properly clipped to the 
		// current clipping region
		if( clipInfo.PerformClipping( buffer, ref bounds, checksum, controlData ) )
		{
			return true;
		}

		// If clipInfo.PerformClipping() returns FALSE, it means that the control was
		// clipped, and was therefor not rendered
		wasClipped = true;

		// Indicate to caller that the control was not rendered
		return false;

	}

	private bool textureEqual( Texture lhs, Texture rhs )
	{
		return Texture2D.Equals( lhs, rhs );
	}

	private bool shaderEqual( Shader lhs, Shader rhs )
	{

		if( lhs == null || rhs == null )
			return object.ReferenceEquals( lhs, rhs );

		return lhs.name.Equals( rhs.name );

	}

	private void initialize()
	{

		if( Application.isPlaying && renderCamera == null )
		{
			Debug.LogError( "No camera is assigned to the GUIManager" );
			return;
		}

		meshRenderer = GetComponent<MeshRenderer>();
		if( meshRenderer == null ) meshRenderer = gameObject.AddComponent<MeshRenderer>();
		meshRenderer.hideFlags = HideFlags.HideInInspector;

		renderFilter = GetComponent<MeshFilter>();
		if( renderFilter == null ) renderFilter = gameObject.AddComponent<MeshFilter>();
		renderFilter.hideFlags = HideFlags.HideInInspector;

		renderMesh = new Mesh[ 2 ]
		{
			new Mesh() { hideFlags = HideFlags.DontSave },
			new Mesh() { hideFlags = HideFlags.DontSave }
		};
		renderMesh[ 0 ].MarkDynamic();
		renderMesh[ 1 ].MarkDynamic();

		//HACK: Upgrade old versions of dfGUIManager which didn't persist the fixedWidth value
		if( fixedWidth < 0 )
		{

			// Select a "reasonable guess" value for FixedWidth
			fixedWidth = Mathf.RoundToInt( fixedHeight * 1.33333f );

			// Each control in the scene now has to have its layout information
			// rebuilt, otherwise the controls will potentially save incorrect
			// anchor margin information
			var controls = GetComponentsInChildren<dfControl>();
			for( int i = 0; i < controls.Length; i++ )
			{
				controls[ i ].ResetLayout();
			}

		}

	}

	private void onResolutionChanged()
	{
		var newHeight = Application.isPlaying ? (int)renderCamera.pixelHeight : this.FixedHeight;
		onResolutionChanged( this.FixedHeight, newHeight );
	}

	private void onResolutionChanged( int oldSize, int currentSize )
	{

		var aspect = RenderCamera.aspect;

		var oldWidth = oldSize * aspect;
		var newWidth = currentSize * aspect;

		var oldResolution = new Vector2( oldWidth, oldSize );
		var newResolution = new Vector2( newWidth, currentSize );

		onResolutionChanged( oldResolution, newResolution );

	}

	private void onResolutionChanged( Vector2 oldSize, Vector2 currentSize )
	{

		if( shutdownInProcess )
			return;

		cachedScreenSize = currentSize;
		applyHalfPixelOffset = null;

		var aspect = RenderCamera.aspect;

		var oldWidth = oldSize.y * aspect;
		var newWidth = currentSize.y * aspect;

		var oldResolution = new Vector2( oldWidth, oldSize.y );
		var newResolution = new Vector2( newWidth, currentSize.y );

		var controls = GetComponentsInChildren<dfControl>();
		Array.Sort( controls, renderSortFunc );

		// Notify all controls that the effective or actual screen resolution has changed
		for( int i = controls.Length - 1; i >= 0; i-- )
		{

			if( pixelPerfectMode && controls[ i ].Parent == null )
			{
				// Aligning on pixel boundaries first could mean less "drift" when 
				// the screen resolution changes
				controls[ i ].MakePixelPerfect();
			}

			controls[ i ].OnResolutionChanged( oldResolution, newResolution );

		}

		// Now that all of the controls are aware of the resolution change,
		// they need to update their layouts.
		for( int i = 0; i < controls.Length; i++ )
		{
			controls[ i ].PerformLayout();
		}

		// EXPERIMENT: If in pixel-perfect mode, make sure all controls
		// are pixel perfect after resolution change
		for( int i = 0; i < controls.Length && pixelPerfectMode; i++ )
		{

			if( controls[ i ].Parent == null )
			{
				controls[ i ].MakePixelPerfect();
			}

		}

		isDirty = true;
		updateRenderSettings();

	}

	private void invalidateAllControls()
	{

		var controls = GetComponentsInChildren<dfControl>();
		for( int i = 0; i < controls.Length; i++ )
		{
			controls[ i ].Invalidate();
		}

		updateRenderOrder();

	}

	/// <summary>
	/// Sorts dfControl instances by their RenderOrder property
	/// </summary>
	private int renderSortFunc( dfControl lhs, dfControl rhs )
	{
		return lhs.RenderOrder.CompareTo( rhs.RenderOrder );
	}

	/// <summary>
	/// Updates the render order of all controls that are rendered by this <see cref="dfGUIManager"/>
	/// </summary>
	private void updateRenderOrder()
	{
		updateRenderOrder( null );
	}

	/// <summary>
	/// Updates the render order of all controls that are rendered by this <see cref="dfGUIManager"/>
	/// </summary>
	private void updateRenderOrder( dfList<dfControl> list )
	{

		var allControls = list;
		var ownList = false;

		if( list == null )
		{
			allControls = getTopLevelControls();
			ownList = true;
		}
		else
		{
			allControls.Sort();
		}

		var renderOrder = 0;
		var count = allControls.Count;
		var items = allControls.Items;

		for( int i = 0; i < count; i++ )
		{
			var control = items[ i ];
			if( control.Parent == null )
			{
				control.setRenderOrder( ref renderOrder );
			}
		}

		if( ownList )
		{
			allControls.Release();
		}

	}

	#endregion

	#region IComparable<dfGUIManager> Members

	public int CompareTo( dfGUIManager other )
	{

		int queueCompare = renderQueueBase.CompareTo( other.renderQueueBase );

		if( queueCompare == 0 )
		{
			if( RenderCamera != null && other.RenderCamera != null )
			{
				return RenderCamera.depth.CompareTo( other.RenderCamera.depth );
			}
		}

		return queueCompare;

	}

	#endregion

	#region Private nested types

	/// <summary>
	/// Encapsulates a reference to a dfControl that has been flagged 
	/// as modal with the callback that will be invoked when it is no
	/// longer modal.
	/// </summary>
	private struct ModalControlReference
	{
		public dfControl control;
		public ModalPoppedCallback callback;
	}

	#endregion

}
