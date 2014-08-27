/* Copyright 2013-2014 Daikon Forge */
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Used by the <see cref="dfGUIManager"/> to manage user input
/// </summary>
[Serializable]
[AddComponentMenu( "Daikon Forge/User Interface/Input Manager" )]
public class dfInputManager : MonoBehaviour
{

	#region Static variables

	/// <summary>
	/// Specifies a list of key codes that, if currently pressed, will cause 
	/// joystick processing to be skipped for the current frame. This helps 
	/// to mitigate undesirable effects from improperly-configured input
	/// axes.
	/// </summary>
	private static KeyCode[] wasd = new KeyCode[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.LeftArrow, KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow };

	/// <summary>
	/// Retains a reference to the control currently under the mouse, if any
	/// </summary>
	private static dfControl controlUnderMouse = null;

	private static dfList<dfInputManager> activeInstances = new dfList<dfInputManager>();

	#endregion

	#region Serialized fields

	[SerializeField]
	protected Camera renderCamera = null;

	[SerializeField]
	protected bool useTouch = true;

	[SerializeField]
	protected bool useMouse = true;

	[SerializeField]
	protected bool useJoystick = false;

	[SerializeField]
	protected KeyCode joystickClickButton = KeyCode.Joystick1Button1;

	[SerializeField]
	protected string horizontalAxis = "Horizontal";

	[SerializeField]
	protected string verticalAxis = "Vertical";

	[SerializeField]
	protected float axisPollingInterval = 0.15f;

	[SerializeField]
	protected bool retainFocus = false;

	[HideInInspector]
	[SerializeField]
	protected int touchClickRadius = 125;

	[SerializeField]
	protected float hoverStartDelay = 0.25f;

	[SerializeField]
	protected float hoverNotifactionFrequency = 0.1f;

	#endregion

	#region Private runtime variables

	private IDFTouchInputSource touchInputSource;
	private TouchInputManager touchHandler;
	private MouseInputManager mouseHandler;
	private dfGUIManager guiManager;

	private dfControl buttonDownTarget;
	private IInputAdapter adapter;
	private float lastAxisCheck = 0f;

	#endregion

	#region Static properties

	/// <summary>
	/// Returns a list of all active dfInputManager instances
	/// </summary>
	public static IList<dfInputManager> ActiveInstances
	{
		get { return activeInstances; }
	}

	/// <summary>
	/// Returns a reference to the topmost control currently under the 
	/// mouse cursor, if any
	/// </summary>
	public static dfControl ControlUnderMouse
	{
		get { return controlUnderMouse; }
	}

	#endregion

	#region Public properties

	/// <summary>
	/// Returns the <see cref="UnityEngine.Camera"/> that is used to render 
	/// the <see cref="dfGUIManager"/> and all of its controls
	/// </summary>
	public Camera RenderCamera
	{
		get { return renderCamera; }
		set { renderCamera = value; }
	}

	/// <summary>
	/// Gets or sets whether DFGUI will process Touch events for controls
	/// </summary>
	public bool UseTouch
	{
		get { return this.useTouch; }
		set { this.useTouch = value; }
	}

	/// <summary>
	/// Gets or sets whether DFGUI will process Mouse events for controls
	/// </summary>
	public bool UseMouse
	{
		get { return this.useMouse; }
		set { this.useMouse = value; }
	}

	/// <summary>
	/// If set to TRUE, joystick input will be converted to appropriate 
	/// <see cref="UnityEngine.KeyCode"/> values and sent to the current
	/// <see cref="dfControl"/> via the OnKeyDown method
	/// </summary>
	public bool UseJoystick
	{
		get { return this.useJoystick; }
		set { this.useJoystick = value; }
	}

	/// <summary>
	/// Gets/Sets the <see cref="UnityEngine.KeyCode"/> value that, when pressed,
	/// will be converted to a Click event for the currently current <see cref="dfControl"/>
	/// </summary>
	public KeyCode JoystickClickButton
	{
		get { return this.joystickClickButton; }
		set { this.joystickClickButton = value; }
	}

	/// <summary>
	/// Gets/Sets the axis which will be translated into left/right key events
	/// </summary>
	public string HorizontalAxis
	{
		get { return this.horizontalAxis; }
		set { this.horizontalAxis = value; }
	}

	/// <summary>
	/// Gets/Sets the axis which will be translated into up/down key events
	/// </summary>
	public string VerticalAxis
	{
		get { return this.verticalAxis; }
		set { this.verticalAxis = value; }
	}

	/// <summary>
	/// Gets/Sets a reference to the IInputAdapter object that will be used
	/// to translate user input for a specific deployment configuration
	/// </summary>
	public IInputAdapter Adapter
	{
		get { return this.adapter; }
		set { this.adapter = value ?? new DefaultInput(); }
	}

	/// <summary>
	/// Gets or sets whether the input manager will allow the focused control
	/// to retain input focus when the mouse is clicked on an empty area of 
	/// the screen
	/// </summary>
	public bool RetainFocus
	{
		get { return this.retainFocus; }
		set { this.retainFocus = value; }
	}

	/// <summary>
	/// Gets or sets the IDFTouchInputSource instance that will be used to
	/// provide Touch information to the application
	/// </summary>
	public IDFTouchInputSource TouchInputSource
	{
		get { return this.touchInputSource; }
		set { this.touchInputSource = value; }
	}

	/// <summary>
	/// Gets or sets the delay before OnMouseHover events are sent to target controls (non-touch only).
	/// </summary>
	public float HoverStartDelay
	{
		get { return this.hoverStartDelay; }
		set { this.hoverStartDelay = value; }
	}

	/// <summary>
	/// Gets or sets the frequency with which OnMouseHover events will be sent (non-touch only).
	/// Set this value to zero to receive event notifications on every frame
	/// </summary>
	public float HoverNotificationFrequency
	{
		get { return this.hoverNotifactionFrequency; }
		set { this.hoverNotifactionFrequency = value; }
	}

	#endregion

	#region Unity event handlers

	public void Awake()
	{
		this.useGUILayout = false;
	}

	public void Start() { }

	public void OnDisable()
	{

		activeInstances.Remove( this );

		var activeControl = dfGUIManager.ActiveControl;
		if( activeControl != null && activeControl.transform.IsChildOf( this.transform ) )
		{
			dfGUIManager.SetFocus( null );
		}

	}

	public void OnEnable()
	{

		activeInstances.Add( this );

		// Mouse input will be handled by a MouseInputManager instance to 
		// consolidate the complexity of mouse operations
		mouseHandler = new MouseInputManager();

		if( useTouch )
		{
			// Multi-touch input will be handled by a TouchInputHandler instance
			// to localize code complexity
			touchHandler = new TouchInputManager( this );
		}

		// If an input adapter has not already been assigned, look for 
		// a replacement or assign the default
		if( this.adapter == null )
		{

			// Look for a replacement IInputAdapter component
			var inputAdapter =
				GetComponents( typeof( MonoBehaviour ) )
				.Where( c => c != null && c.GetType() != null && typeof( IInputAdapter ).IsAssignableFrom( c.GetType() ) )
				.FirstOrDefault();

			// Use the replacement if found, otherwise use the default adapter
			this.adapter = (IInputAdapter)inputAdapter ?? new DefaultInput();

		}

		Input.simulateMouseWithTouches = !this.useTouch;

	}

	public void Update()
	{

		if( !Application.isPlaying )
			return;

		if( guiManager == null )
		{
			guiManager = GetComponent<dfGUIManager>();
			if( guiManager == null )
			{
				Debug.LogWarning( "No associated dfGUIManager instance", this );
				this.enabled = false;
				return;
			}
		}

		var activeControl = dfGUIManager.ActiveControl;

		if( this.useTouch && processTouchInput() )
		{
			return;
		}
		else if( useMouse )
		{
			processMouseInput();
		}

		if( activeControl == null )
			return;

#if UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8

		// Do not process any other control events while the 
		// mobile keyboard is being displayed. Note that this 
		// means that controls which use the mobile keyboard
		// need to handle that process themselves.
		if( TouchScreenKeyboard.visible )
		{
			return;
		}

#endif

		if( processKeyboard() )
			return;

		if( useJoystick )
		{

			#region Eliminate WASD navigation in UI

			// NOTE: By default, Unity includes the WASD keys in the Horizontal and
			// Vertical Axis definitions in Input Manager. While it is certainly 
			// possible to either modify those definitions or specify different 
			// axes which do not include those keys, it seems reasonable to try
			// to simply ignore those keys for UI navigation.
			//
			// If this is not the default behavior, then delete or comment out the 
			// following block of code.
			//
			// The following code only partially works... If the user holds down
			// one of the WASD keys, then an arrow keycode will be generated after 
			// they release the key. If this is a problem, then the only solution 
			// appears to be that modifying the axes in the Input Manager is required.
			//
			// ADDENDUM: Expanded to include arrow keys after discovering that 
			// incorrectly-configured input axes can also produce spurious arrow
			// key events

			for( int i = 0; i < wasd.Length; i++ )
			{

				if(
					Input.GetKey( wasd[ i ] ) ||
					Input.GetKeyDown( wasd[ i ] ) ||
					Input.GetKeyUp( wasd[ i ] )
					)
				{
					return;
				}

			}

			#endregion

			processJoystick();

		}

	}

	public void OnGUI()
	{

		var e = Event.current;
		if( e == null )
			return;

		if( e.isKey && e.keyCode != KeyCode.None )
		{
			processKeyEvent( e.type, e.keyCode, e.modifiers );
			return;
		}

	}

	#endregion

	#region Private utility methods

	private void processJoystick()
	{

		try
		{

			var target = dfGUIManager.ActiveControl;
			if( target == null || !target.transform.IsChildOf( this.transform ) )
				return;

			#region Translate horz and vertical axes to keycodes

			var horizontal = adapter.GetAxis( horizontalAxis );
			var vertical = adapter.GetAxis( verticalAxis );

			if( Mathf.Abs( horizontal ) < 0.5f && Mathf.Abs( vertical ) <= 0.5f )
			{
				lastAxisCheck = Time.deltaTime - axisPollingInterval;
			}

			if( Time.realtimeSinceStartup - lastAxisCheck > axisPollingInterval )
			{

				if( Mathf.Abs( horizontal ) >= 0.5f )
				{
					lastAxisCheck = Time.realtimeSinceStartup;
					var keyCode = ( horizontal > 0f ) ? KeyCode.RightArrow : KeyCode.LeftArrow;
					target.OnKeyDown( new dfKeyEventArgs( target, keyCode, false, false, false ) );
				}

				if( Mathf.Abs( vertical ) >= 0.5f )
				{
					lastAxisCheck = Time.realtimeSinceStartup;
					var keyCode = ( vertical > 0f ) ? KeyCode.UpArrow : KeyCode.DownArrow;
					target.OnKeyDown( new dfKeyEventArgs( target, keyCode, false, false, false ) );
				}

			}

			#endregion

			#region Poll for joystick buttons and convert to KeyCode

			if( joystickClickButton != KeyCode.None )
			{

				var buttonDown = adapter.GetKeyDown( joystickClickButton );
				if( buttonDown )
				{

					var center = target.GetCenter();
					var camera = target.GetCamera();
					var ray = camera.ScreenPointToRay( camera.WorldToScreenPoint( center ) );
					var mouseDownArgs = new dfMouseEventArgs( target, dfMouseButtons.Left, 0, ray, center, 0 );

					target.OnMouseDown( mouseDownArgs );

					buttonDownTarget = target;

				}

				var buttonUp = adapter.GetKeyUp( joystickClickButton );
				if( buttonUp )
				{

					if( buttonDownTarget == target )
					{
						target.DoClick();
					}

					var center = target.GetCenter();
					var camera = target.GetCamera();
					var ray = camera.ScreenPointToRay( camera.WorldToScreenPoint( center ) );
					var mouseUpArgs = new dfMouseEventArgs( target, dfMouseButtons.Left, 0, ray, center, 0 );

					target.OnMouseUp( mouseUpArgs );

					buttonDownTarget = null;

				}

			}

			for( var code = KeyCode.Joystick1Button0; code <= KeyCode.Joystick1Button19; code++ )
			{

				var buttonDown = adapter.GetKeyDown( code );
				if( buttonDown )
				{
					target.OnKeyDown( new dfKeyEventArgs( target, code, false, false, false ) );
				}

			}

			#endregion

		}
		catch( UnityException err )
		{
			// Input axis is not set up or another problem with Input has
			// been encountered. Temporarily disable joystick input and 
			// notify user of issue.
			Debug.LogError( err.ToString(), this );
			this.useJoystick = false;
		}

	}

	private void processKeyEvent( EventType eventType, KeyCode keyCode, EventModifiers modifiers )
	{

		var activeControl = dfGUIManager.ActiveControl;

		if( activeControl == null || !activeControl.IsEnabled || !activeControl.transform.IsChildOf( this.transform ) )
			return;

		var controlKey =
			( ( modifiers & EventModifiers.Control ) == EventModifiers.Control ) ||
			( ( modifiers & EventModifiers.Command ) == EventModifiers.Command );

		var shiftKey = ( modifiers & EventModifiers.Shift ) == EventModifiers.Shift;
		var altKey = ( modifiers & EventModifiers.Alt ) == EventModifiers.Alt;

		var args = new dfKeyEventArgs(
			activeControl,
			keyCode,
			controlKey,
			shiftKey,
			altKey
		);

		if( keyCode >= KeyCode.Space && keyCode <= KeyCode.Z )
		{
			var ch = (char)keyCode;
			args.Character = shiftKey ? char.ToUpper( ch ) : char.ToLower( ch );
		}

		if( eventType == EventType.keyDown )
		{
			activeControl.OnKeyDown( args );
		}
		else if( eventType == EventType.keyUp )
		{
			activeControl.OnKeyUp( args );
		}

		if( args.Used || eventType == EventType.keyUp )
			return;

		// TODO: Implement Tab and Enter key processing?

	}

	private bool processKeyboard()
	{

		var activeControl = dfGUIManager.ActiveControl;
		if( activeControl == null || string.IsNullOrEmpty( Input.inputString ) || !activeControl.transform.IsChildOf( this.transform ) )
			return false;

		var inputString = Input.inputString;
		for( int i = 0; i < inputString.Length; i++ )
		{

			var ch = inputString[ i ];
			if( ch == '\b' || ch == '\n' )
				continue;

			var keyCode = (KeyCode)ch;

			var args = new dfKeyEventArgs( activeControl, keyCode, false, false, false );
			args.Character = ch;

			activeControl.OnKeyPress( args );

		}

		return true;

	}

	private bool processTouchInput()
	{

		if( touchInputSource == null )
		{

			// Attempt first to find an attached component that will act as the 
			// touch input source.
			var inputSources = GetComponents<dfTouchInputSourceComponent>().OrderByDescending( x => x.Priority ).ToArray();
			for( int i = 0; i < inputSources.Length; i++ )
			{

				var component = inputSources[ i ];
				if( !component.enabled )
					continue;

				touchInputSource = component.Source;
				if( touchInputSource != null )
					break;

			}

			// If still no touch input source assigned, assign default
			if( touchInputSource == null )
			{
				touchInputSource = dfMobileTouchInputSource.Instance;
			}

		}

		touchInputSource.Update();

		var touchCount = touchInputSource.TouchCount;
		if( touchCount == 0 )
			return false;

		this.touchHandler.Process( this.transform, renderCamera, touchInputSource, retainFocus );

		return true;

	}

	private void processMouseInput()
	{

		if( guiManager == null )
			return;

		var mouseScreenPos = adapter.GetMousePosition();
		var ray = renderCamera.ScreenPointToRay( mouseScreenPos );

		controlUnderMouse = dfGUIManager.HitTestAll( mouseScreenPos );
		if( controlUnderMouse != null && !controlUnderMouse.transform.IsChildOf( this.transform ) )
			controlUnderMouse = null;

		mouseHandler.ProcessInput( this, adapter, ray, controlUnderMouse, this.retainFocus );

	}

	/// <summary>
	/// Sorts RaycastHit instances by distance
	/// </summary>
	internal static int raycastHitSorter( RaycastHit lhs, RaycastHit rhs )
	{
		return lhs.distance.CompareTo( rhs.distance );
	}

	/// <summary>
	/// Refines the list of RaycastHit results and narrows it down to a single
	/// unclipped dfControl object if possible
	/// </summary>
	/// <param name="hits"></param>
	/// <returns></returns>
	internal dfControl clipCast( RaycastHit[] hits )
	{

		if( hits == null || hits.Length == 0 )
			return null;

		var match = (dfControl)null;
		var modalControl = dfGUIManager.GetModalControl();

		for( int i = hits.Length - 1; i >= 0; i-- )
		{

			var hit = hits[ i ];
			var control = hit.transform.GetComponent<dfControl>();
			var skipControl =
				control == null ||
				( modalControl != null && !control.transform.IsChildOf( modalControl.transform ) ) ||
				!control.enabled ||
				combinedOpacity( control ) <= 0.01f ||
				!control.IsEnabled ||
				!control.IsVisible ||
				!control.transform.IsChildOf( this.transform );

			if( skipControl )
				continue;

			if( isInsideClippingRegion( hit.point, control ) )
			{
				if( match == null || control.RenderOrder > match.RenderOrder )
				{
					match = control;
				}
			}

		}

		return match;

	}

	/// <summary>
	/// Determines whether the raycast point on the given control is
	/// inside of the control's clip region hierarchy
	/// </summary>
	/// <param name="control"></param>
	/// <returns></returns>
	internal static bool isInsideClippingRegion( Vector3 point, dfControl control )
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

	private static float combinedOpacity( dfControl control )
	{

		var opacity = 1f;
		while( control != null )
		{
			opacity *= control.Opacity;
			control = control.Parent;
		}

		return opacity;

	}

	#endregion

	#region Nested classes

	private class TouchInputManager
	{

		#region Private fields

		private List<ControlTouchTracker> tracked = new List<ControlTouchTracker>();

		private List<int> untracked = new List<int>();

		private dfInputManager manager;

		#endregion

		#region Constructor

		private TouchInputManager()
		{
			// Disallow parameterless constructor
		}

		public TouchInputManager( dfInputManager manager )
		{
			this.manager = manager;
		}

		#endregion

		#region Public methods

		internal void Process( Transform transform, Camera renderCamera, IDFTouchInputSource input, bool retainFocusSetting )
		{

			controlUnderMouse = null;

			var touches = input.Touches;
			for( int i = 0; i < touches.Count; i++ )
			{

				// Dereference Touch information
				var touch = touches[ i ];

				// Keep track of the last control under the "mouse"
				var touchedControl = dfGUIManager.HitTestAll( touch.position );
				if( touchedControl != null && touchedControl.transform.IsChildOf( manager.transform ) )
				{
					controlUnderMouse = touchedControl;
				}

				#region Don't track touches on empty space

				if( controlUnderMouse == null )
				{
					if( touch.phase == TouchPhase.Began )
					{

						if( !retainFocusSetting && untracked.Count == 0 )
						{
							var focusControl = dfGUIManager.ActiveControl;
							if( focusControl != null && focusControl.transform.IsChildOf( manager.transform ) )
							{
								focusControl.Unfocus();
							}
						}

						untracked.Add( touch.fingerId );

						continue;

					}
				}

				if( untracked.Contains( touch.fingerId ) )
				{

					if( touch.phase == TouchPhase.Ended )
						untracked.Remove( touch.fingerId );

					continue;

				}

				#endregion

				var ray = renderCamera.ScreenPointToRay( touch.position );
				var info = new TouchRaycast( controlUnderMouse, touch, ray );

				var captured = tracked.FirstOrDefault( x => x.IsTrackingFinger( info.FingerID ) );
				if( captured != null )
				{
					captured.Process( info );
					continue;
				}

				var processed = false;
				for( int x = 0; x < tracked.Count; x++ )
				{
					if( tracked[ x ].Process( info ) )
					{
						processed = true;
						break;
					}
				}

				if( !processed && controlUnderMouse != null )
				{

					if( !tracked.Any( x => x.control == controlUnderMouse ) )
					{

						var newTracker = new ControlTouchTracker( manager, controlUnderMouse );

						tracked.Add( newTracker );
						newTracker.Process( info );

					}

				}

			}

		}

		#endregion

		#region Private utility methods

		/// <summary>
		/// Refines the list of RaycastHit results and narrows it down to a single
		/// unclipped dfControl object if possible
		/// </summary>
		/// <param name="hits"></param>
		/// <returns></returns>
		private dfControl clipCast( Transform transform, RaycastHit[] hits )
		{

			if( hits == null || hits.Length == 0 )
				return null;

			var match = (dfControl)null;
			var modalControl = dfGUIManager.GetModalControl();

			for( int i = hits.Length - 1; i >= 0; i-- )
			{

				var hit = hits[ i ];
				var control = hit.transform.GetComponent<dfControl>();
				var skipControl =
					control == null ||
					( modalControl != null && !control.transform.IsChildOf( modalControl.transform ) ) ||
					!control.enabled ||
					control.Opacity < 0.01f ||
					!control.IsEnabled ||
					!control.IsVisible ||
					!control.transform.IsChildOf( transform );

				if( skipControl )
					continue;

				if( isInsideClippingRegion( hit, control ) )
				{
					if( match == null || control.RenderOrder > match.RenderOrder )
					{
						match = control;
					}
				}

			}

			return match;

		}

		/// <summary>
		/// Determines whether the raycast point on the given control is
		/// inside of the control's clip region hierarchy
		/// </summary>
		/// <param name="control"></param>
		/// <returns></returns>
		private bool isInsideClippingRegion( RaycastHit hit, dfControl control )
		{

			var point = hit.point;

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

		#endregion

		#region Nested classes

		private class ControlTouchTracker
		{

			#region Public fields and properties

			public readonly dfControl control;
			public readonly Dictionary<int, TouchRaycast> touches = new Dictionary<int, TouchRaycast>();
			public readonly List<int> capture = new List<int>();

			public bool IsDragging { get { return this.dragState == dfDragDropState.Dragging; } }
			public int TouchCount { get { return touches.Count; } }

			#endregion

			#region Private variables

			private dfInputManager manager;

			private Vector3 controlStartPosition;
			private dfDragDropState dragState = dfDragDropState.None;
			private object dragData = null;

			#endregion

			#region Constructor

			public ControlTouchTracker( dfInputManager manager, dfControl control )
			{
				this.manager = manager;
				this.control = control;
				this.controlStartPosition = control.transform.position;
			}

			#endregion

			#region Public methods

			public bool IsTrackingFinger( int fingerID )
			{
				return touches.ContainsKey( fingerID );
			}

			/// <summary>
			/// Processes touch information for a single control
			/// </summary>
			/// <param name="info">The touch/raycast data to be processed</param>
			/// <returns>Returns TRUE if the touch information was processed</returns>
			public bool Process( TouchRaycast info )
			{

				#region Drag / Drop

				if( IsDragging )
				{

					// Not interested in any fingers other than the one that 
					// started the drag operation
					if( !capture.Contains( info.FingerID ) )
						return false;

					// Nothing to do if there's no user action
					if( info.Phase == TouchPhase.Stationary )
						return true;

					// If the touch was cancelled, raise OnDragEnd event
					if( info.Phase == TouchPhase.Canceled )
					{

						control.OnDragEnd( new dfDragEventArgs( control, dfDragDropState.Cancelled, dragData, info.ray, info.position ) );
						dragState = dfDragDropState.None;

						touches.Clear();
						capture.Clear();

						return true;
					}

					// If the finger was lifted, attempt drop
					if( info.Phase == TouchPhase.Ended )
					{

						// Dropped on nothing
						if( info.control == null || info.control == control )
						{

							control.OnDragEnd( new dfDragEventArgs( control, dfDragDropState.CancelledNoTarget, dragData, info.ray, info.position ) );
							dragState = dfDragDropState.None;

							touches.Clear();
							capture.Clear();

							return true;

						}

						// Dropped on another control
						var dropArgs = new dfDragEventArgs( info.control, dfDragDropState.Dragging, dragData, info.ray, info.position );
						info.control.OnDragDrop( dropArgs );

						// If there was no event consumer, or if the event consumer did not
						// change the state from Dragging (which is not a valid state for
						// a drop event) then just cancel the operation
						if( !dropArgs.Used || dropArgs.State != dfDragDropState.Dropped )
						{
							dropArgs.State = dfDragDropState.Cancelled;
						}

						// Let the control know that it is no longer being dragged
						var endArgs = new dfDragEventArgs( control, dropArgs.State, dragData, info.ray, info.position );
						endArgs.Target = info.control;
						control.OnDragEnd( endArgs );

						// Clear state
						dragState = dfDragDropState.None;
						touches.Clear();
						capture.Clear();

						return true;

					}

					return true;

				}

				#endregion

				#region New touch for this control

				if( !touches.ContainsKey( info.FingerID ) )
				{

					// Touch is not over control, and is not "captured" by control
					if( info.control != control )
						return false;

					// Start tracking this finger
					touches[ info.FingerID ] = info;

					// See if this is the first touch to be tracked for this control
					if( touches.Count == 1 )
					{

						// This is the first Touch to be associated with the control
						control.OnMouseEnter( info );

						// If the touch was also started while over the control,
						// then raise the OnMouseDown event
						if( info.Phase == TouchPhase.Began )
						{

							capture.Add( info.FingerID );
							controlStartPosition = control.transform.position;

							control.OnMouseDown( info );

							// Prevent "click-through"
							if( Event.current != null )
								Event.current.Use();

						}

						return true;

					}

					// Switch control to "multi-touch" mode if touch began on control,
					// otherwise ignore the new touch info
					if( info.Phase == TouchPhase.Began )
					{

						// Send multi-touch event
						var activeTouches = getActiveTouches();
						var multiTouchArgs = new dfTouchEventArgs( control, activeTouches, info.ray );
						control.OnMultiTouch( multiTouchArgs );

					}

					return true;


				}

				#endregion

				#region Previously tracked touch has now ended

				if( info.Phase == TouchPhase.Canceled || info.Phase == TouchPhase.Ended )
				{

					var phase = info.Phase;
					var touch = touches[ info.FingerID ];

					// Remove the finger from the list of touches
					touches.Remove( info.FingerID );

					if( touches.Count == 0 && phase != TouchPhase.Canceled )
					{

						if( capture.Contains( info.FingerID ) )
						{

							if( canFireClickEvent( info, touch ) )
							{

								if( info.control == this.control )
								{
									if( info.touch.tapCount > 1 )
										control.OnDoubleClick( info );
									else
										control.OnClick( info );
								}

							}

							info.control = this.control;
							if( this.control )
							{
								this.control.OnMouseUp( info );
							}

						}

						capture.Remove( info.FingerID );

						return true;

					}
					else
					{
						capture.Remove( info.FingerID );
					}

					if( touches.Count == 1 )
					{

						// Explicitly notify control that multi-touch state has ended
						control.OnMultiTouchEnd();

						return true;

					}

				}

				#endregion

				#region Multi-touch events

				// If there is more than one touch active for this control, 
				// then raise the OnMultiTouch event instead of converting
				// the touch info to mouse events
				if( touches.Count > 1 )
				{

					var activeTouches = getActiveTouches();
					var multiTouchArgs = new dfTouchEventArgs( control, activeTouches, info.ray );

					control.OnMultiTouch( multiTouchArgs );

					return true;

				}

				#endregion

				// If the touch has not moved, send hover message
				if( !IsDragging && info.Phase == TouchPhase.Stationary )
				{
					if( info.control == this.control )
					{
						control.OnMouseHover( info );
						return true;
					}
					return false;
				}

				#region See if the control supports (and allows) drag-and-drop

				var canStartDrag =
					capture.Contains( info.FingerID ) &&
					dragState == dfDragDropState.None &&
					info.Phase == TouchPhase.Moved;

				if( canStartDrag )
				{

					// Query the control to see if drag-and-drop is allowed
					var dragStartArgs = (dfDragEventArgs)info;
					control.OnDragStart( dragStartArgs );

					// If control set State and Used properties to the correct values, 
					// enter drag-and-drop mode
					if( dragStartArgs.State == dfDragDropState.Dragging && dragStartArgs.Used )
					{

						this.dragState = dfDragDropState.Dragging;
						this.dragData = dragStartArgs.Data;

						return true;

					}

					// Flag that we've already tried drag and drop
					this.dragState = dfDragDropState.Denied;

				}

				#endregion

				// Check for mouse leave
				if( info.control != this.control )
				{

					// If "capture" is not active, fire the OnMouseLeave event
					if( !capture.Contains( info.FingerID ) )
					{

						info.control = this.control;
						this.control.OnMouseLeave( info );

						this.touches.Remove( info.FingerID );

						return true;

					}

				}

				// At this point, the only remaining option is to send OnMouseMove event
				info.control = this.control;
				this.control.OnMouseMove( info );

				return true;

			}

			private bool canFireClickEvent( TouchRaycast info, TouchRaycast touch )
			{

				// It is possible that a control could have been deleted during event processing.
				if( this.control == null )
					return false;

				#region Should not fire click event if control has been moved

				var p2u = control.PixelsToUnits();
				var startPosition = controlStartPosition / p2u;
				var currentPosition = control.transform.position / p2u;

				if( Vector3.Distance( startPosition, currentPosition ) > 1f )
					return false;

				#endregion

				return true;

			}

			#endregion

			#region Private utility methods

			private List<dfTouchInfo> getActiveTouches()
			{

				var liveTouches = manager.touchInputSource.Touches;
				var result = touches.Select( x => x.Value.touch ).ToList();
				for( int i = 0; i < result.Count; )
				{

					bool contains = false;

					for( int j = 0; i < liveTouches.Count; j++ )
					{
						if( liveTouches[ j ].fingerId == result[ i ].fingerId )
						{
							contains = true;
							break;
						}
					}

					if( contains )
					{
						result[ i ] = liveTouches.First( x => x.fingerId == result[ i ].fingerId );
						i += 1;
					}
					else
					{
						result.RemoveAt( i );
					}

				}

				return result;

			}

			#endregion

		}

		/// <summary>
		/// Represents the results of a raycast against a list of controls, encapsulating
		/// the data about which control was hit, which finger was used, and where on the
		/// screen the raycast originated
		/// </summary>
		private class TouchRaycast
		{

			#region Public fields and properties

			public dfControl control;
			public dfTouchInfo touch;
			public Ray ray;
			public Vector2 position;

			public int FingerID { get { return touch.fingerId; } }
			public TouchPhase Phase { get { return touch.phase; } }

			#endregion

			#region Constructor

			public TouchRaycast( dfControl control, dfTouchInfo touch, Ray ray )
			{
				this.control = control;
				this.touch = touch;
				this.ray = ray;
				this.position = touch.position;
			}

			#endregion

			#region Type conversion

			public static implicit operator dfTouchEventArgs( TouchRaycast touch )
			{
				var args = new dfTouchEventArgs( touch.control, touch.touch, touch.ray );
				return args;
			}

			public static implicit operator dfDragEventArgs( TouchRaycast touch )
			{
				var args = new dfDragEventArgs( touch.control, dfDragDropState.None, null, touch.ray, touch.position );
				return args;
			}

			#endregion

		}

		#endregion

	}

	private class MouseInputManager
	{

		#region Private fields

		/// <summary> Name of the axis used for the mouse scroll wheel </summary>
		private const string scrollAxisName = "Mouse ScrollWheel";

		/// <summary> The maximum time window for consecutive Click events to be considered a DoubleClick </summary>
		private const float DOUBLECLICK_TIME = 0.25f;

		/// <summary> The number of pixels the user must move the mouse before it is considered a drag operation </summary>
		private const int DRAG_START_DELTA = 2;

		/// <summary> The last dfControl to have received mouse events </summary>
		private dfControl activeControl;

		/// <summary> The last dfControl to have received mouse events </summary>
		private Vector3 activeControlPosition;

		/// <summary> The last mouse position tracked, used to determine when to fire MouseMove events </summary>
		private Vector2 lastPosition = Vector2.one * int.MinValue;

		/// <summary> The distance the mouse has moved since the last frame </summary>
		private Vector2 mouseMoveDelta = Vector2.zero;

		/// <summary> Keeps track of the last time a Click event was generated on a control </summary>
		private float lastClickTime = 0f;

		/// <summary> Keeps track of the last time an OnMouseHover event was generated on a control </summary>
		private float lastHoverTime = 0f;

		/// <summary> Indicates the current Drag-and-Drop state </summary>
		private dfDragDropState dragState = dfDragDropState.None;

		/// <summary> The Drag-and-Drop data provided by the event source</summary>
		private object dragData = null;

		/// <summary> The last control over which the drag-and-drop operation occured </summary>
		private dfControl lastDragControl = null;

		/// <summary> Mouse buttons which are current depressed </summary>
		private dfMouseButtons buttonsDown;

		/// <summary> Mouse buttons which were released in the most recent update </summary>
		private dfMouseButtons buttonsReleased;

		/// <summary> Mouse buttons which were pressed in the most recent update </summary>
		private dfMouseButtons buttonsPressed;

		#endregion

		#region Public methods

		public void ProcessInput( dfInputManager manager, IInputAdapter adapter, Ray ray, dfControl control, bool retainFocusSetting )
		{

			var position = adapter.GetMousePosition();

			buttonsDown = dfMouseButtons.None;
			buttonsReleased = dfMouseButtons.None;
			buttonsPressed = dfMouseButtons.None;

			getMouseButtonInfo( adapter, ref buttonsDown, ref buttonsReleased, ref buttonsPressed );

			float scroll = adapter.GetAxis( scrollAxisName );
			if( !Mathf.Approximately( scroll, 0f ) )
			{
				// By default the mouse wheel is reported in increments of 0.1f, 
				// which is just a useless number for UI, but this can be changed
				// by the user in the Unity Input Manager. We'll assume that if the 
				// number reported is less than 1 then it is probably safe to 
				// assume that we can massage it for UI purposes.
				scroll = Mathf.Sign( scroll ) * Mathf.Max( 1, Mathf.Abs( scroll ) );
			}

			mouseMoveDelta = position - lastPosition;
			lastPosition = position;

			#region Drag and drop

			if( dragState == dfDragDropState.Dragging )
			{

				if( buttonsReleased == dfMouseButtons.None )
				{

					// Do nothing if the drag operation is over the source control 
					// and no buttons have been released.
					if( control == activeControl )
						return;

					if( control != lastDragControl )
					{

						if( lastDragControl != null )
						{
							var dragArgs = new dfDragEventArgs( lastDragControl, dragState, dragData, ray, position );
							lastDragControl.OnDragLeave( dragArgs );
						}

						if( control != null )
						{
							var dragArgs = new dfDragEventArgs( control, dragState, dragData, ray, position );
							control.OnDragEnter( dragArgs );
						}

						lastDragControl = control;

						return;

					}

					if( control != null )
					{

						if( mouseMoveDelta.magnitude > 1.0f )
						{
							var dragArgs = new dfDragEventArgs( control, dragState, dragData, ray, position );
							control.OnDragOver( dragArgs );
						}

					}

					return;

				}

				if( control != null && control != activeControl )
				{

					var dragArgs = new dfDragEventArgs( control, dfDragDropState.Dragging, dragData, ray, position );
					control.OnDragDrop( dragArgs );

					// If there was no event consumer, or if the event consumer did not
					// change the state from Dragging (which is not a valid state for
					// a drop event) then just cancel the operation
					if( !dragArgs.Used || dragArgs.State == dfDragDropState.Dragging )
						dragArgs.State = dfDragDropState.Cancelled;

					dragArgs = new dfDragEventArgs( activeControl, dragArgs.State, dragArgs.Data, ray, position );
					dragArgs.Target = control;
					activeControl.OnDragEnd( dragArgs );

				}
				else
				{
					var cancelState = ( control == null ) ? dfDragDropState.CancelledNoTarget : dfDragDropState.Cancelled;
					var dragArgs = new dfDragEventArgs( activeControl, cancelState, dragData, ray, position );
					activeControl.OnDragEnd( dragArgs );
				}

				dragState = dfDragDropState.None;
				lastDragControl = null;
				activeControl = null;
				lastClickTime = 0f;
				lastHoverTime = 0f;
				lastPosition = position;

				return;

			}

			#endregion

			#region Mouse button pressed

			if( buttonsPressed != dfMouseButtons.None )
			{

				lastHoverTime = Time.realtimeSinceStartup + manager.hoverStartDelay;

				if( activeControl != null )
				{
					// If a control has capture, forward all events to it
					if( activeControl.transform.IsChildOf( manager.transform ) )
					{
						activeControl.OnMouseDown( new dfMouseEventArgs( activeControl, buttonsPressed, 0, ray, position, scroll ) );
					}
				}
				else if( control == null || control.transform.IsChildOf( manager.transform ) )
				{

					setActive( manager, control, position, ray );
					if( control != null )
					{
						dfGUIManager.SetFocus( control );
						control.OnMouseDown( new dfMouseEventArgs( control, buttonsPressed, 0, ray, position, scroll ) );
					}
					else if( !retainFocusSetting )
					{
						var focusControl = dfGUIManager.ActiveControl;
						if( focusControl != null && focusControl.transform.IsChildOf( manager.transform ) )
						{
							focusControl.Unfocus();
						}
					}

				}

				if( buttonsReleased == dfMouseButtons.None )
					return;

			}

			#endregion

			#region Mouse button released

			if( buttonsReleased != dfMouseButtons.None )
			{

				lastHoverTime = Time.realtimeSinceStartup + manager.hoverStartDelay;

				// Mouse up without a control having capture is ignored
				if( activeControl == null )
				{
					setActive( manager, control, position, ray );
					return;
				}

				// If the mouse button is released over the same control it was pressed on,
				// the Click event gets generated (in addition to MouseUp)
				if( activeControl == control && buttonsDown == dfMouseButtons.None )
				{

					var p2u = activeControl.PixelsToUnits();
					var startPosition = activeControlPosition / p2u;
					var currentPosition = activeControl.transform.position / p2u;

					// Don't fire click events if the control has been moved since the mouse was down
					if( Vector3.Distance( startPosition, currentPosition ) <= 1 )
					{

						if( Time.realtimeSinceStartup - lastClickTime < DOUBLECLICK_TIME )
						{
							lastClickTime = 0f;
							activeControl.OnDoubleClick( new dfMouseEventArgs( activeControl, buttonsReleased, 1, ray, position, scroll ) );
						}
						else
						{
							lastClickTime = Time.realtimeSinceStartup;
							activeControl.OnClick( new dfMouseEventArgs( activeControl, buttonsReleased, 1, ray, position, scroll ) );
						}

					}

				}

				// Let the last control know that the button was released whether it was 
				// released over the control or not
				activeControl.OnMouseUp( new dfMouseEventArgs( activeControl, buttonsReleased, 0, ray, position, scroll ) );

				// If all buttons are up, then we need to reset the mouse state
				if( buttonsDown == dfMouseButtons.None && activeControl != control )
				{
					setActive( manager, null, position, ray );
				}

				return;

			}

			#endregion

			#region Doesn't matter if buttons are down or not

			if( activeControl != null && activeControl == control )
			{

				if( mouseMoveDelta.magnitude == 0 && Time.realtimeSinceStartup - lastHoverTime > manager.hoverNotifactionFrequency )
				{
					activeControl.OnMouseHover( new dfMouseEventArgs( activeControl, buttonsDown, 0, ray, position, scroll ) );
					lastHoverTime = Time.realtimeSinceStartup;
				}

			}

			#endregion

			#region No buttons down

			if( buttonsDown == dfMouseButtons.None )
			{

				if( scroll != 0 && control != null )
				{
					setActive( manager, control, position, ray );
					control.OnMouseWheel( new dfMouseEventArgs( control, buttonsDown, 0, ray, position, scroll ) );
					return;
				}

				setActive( manager, control, position, ray );

			}

			#endregion

			#region Some buttons down

			else if( buttonsDown != dfMouseButtons.None ) // Some buttons are down
			{

				if( activeControl != null )
				{

					// Special case: Another control with a higher RenderOrder is now under the mouse.
					// This can happen when a control moves, such as when you click on a slider and the 
					// thumb position is updated to be under the mouse (when it wasn't previously)
					if( control != null && control.RenderOrder > activeControl.RenderOrder )
					{
						// TODO: What to do about this when a control has capture?
					}

					// If the mouse was moved notify the control, otherwise nothing to do
					// NOTE: This is similar to "mouse capture" on Windows Forms 
					if( mouseMoveDelta.magnitude >= DRAG_START_DELTA )
					{

						if( ( buttonsDown & ( dfMouseButtons.Left | dfMouseButtons.Right ) ) != 0 && dragState != dfDragDropState.Denied )
						{
							var dragArgs = new dfDragEventArgs( activeControl ) { Position = position };
							activeControl.OnDragStart( dragArgs );
							if( dragArgs.State == dfDragDropState.Dragging )
							{
								dragState = dfDragDropState.Dragging;
								dragData = dragArgs.Data;
								return;
							}
							else
							{
								dragState = dfDragDropState.Denied;
							}
						}

					}

				}

			}

			#endregion

			if( activeControl != null && mouseMoveDelta.magnitude >= 1 )
			{
				var moveArgs = new dfMouseEventArgs( activeControl, buttonsDown, 0, ray, position, scroll ) { MoveDelta = mouseMoveDelta };
				activeControl.OnMouseMove( moveArgs );
			}

		}

		#endregion

		#region Private utility methods

		private static void getMouseButtonInfo( IInputAdapter adapter, ref dfMouseButtons buttonsDown, ref dfMouseButtons buttonsReleased, ref dfMouseButtons buttonsPressed )
		{

			for( int i = 0; i < 3; i++ )
			{

				if( adapter.GetMouseButton( i ) )
				{
					buttonsDown |= (dfMouseButtons)( 1 << i );
				}
				if( adapter.GetMouseButtonUp( i ) )
				{
					buttonsReleased |= (dfMouseButtons)( 1 << i );
				}
				if( adapter.GetMouseButtonDown( i ) )
				{
					buttonsPressed |= (dfMouseButtons)( 1 << i );
				}

			}

		}

		private void setActive( dfInputManager manager, dfControl control, Vector2 position, Ray ray )
		{

			if( activeControl != null && activeControl != control )
			{
				activeControl.OnMouseLeave( new dfMouseEventArgs( activeControl ) { Position = position, Ray = ray } );
			}

			if( control != null && control != activeControl )
			{
				lastClickTime = 0f;
				lastHoverTime = Time.realtimeSinceStartup + manager.hoverStartDelay;
				control.OnMouseEnter( new dfMouseEventArgs( control ) { Position = position, Ray = ray } );
			}

			activeControl = control;
			activeControlPosition = ( control != null ) ? control.transform.position : Vector3.one * float.MinValue;
			lastPosition = position;
			dragState = dfDragDropState.None;

		}

		#endregion

	}

	private class DefaultInput : IInputAdapter
	{

		public bool GetKeyDown( KeyCode key )
		{
			return Input.GetKeyDown( key );
		}

		public bool GetKeyUp( KeyCode key )
		{
			return Input.GetKeyUp( key );
		}

		public float GetAxis( string axisName )
		{
			return Input.GetAxis( axisName );
		}

		public Vector2 GetMousePosition()
		{
			return Input.mousePosition;
		}

		public bool GetMouseButton( int button )
		{
			return Input.GetMouseButton( button );
		}

		public bool GetMouseButtonDown( int button )
		{
			return Input.GetMouseButtonDown( button );
		}

		public bool GetMouseButtonUp( int button )
		{
			return Input.GetMouseButtonUp( button );
		}

	}

	#endregion

}

/// <summary>
/// Defines an interface that can be impolemented by components that will be
/// used to convert user input. For instance, you might wish to code your 
/// user interface to use KeyCode.JoystickButton0 for a particular task, and 
/// can then use platform- or hardware-specific adapters to convert the 
/// desired button to the target value without having to modify your UI code.
/// </summary>
public interface IInputAdapter
{

	/// <summary>
	/// Returns true during the frame the user starts pressing down the key identified by the given <see cref="UnityEngine.KeyCode"/>
	/// </summary>
	bool GetKeyDown( KeyCode key );

	/// <summary>
	/// Returns true during the frame the user releases the key identified by the given <see cref="UnityEngine.KeyCode"/>
	/// </summary>
	bool GetKeyUp( KeyCode key );

	/// <summary>
	///  Returns the value of the virtual axis identified by axisName.
	///  The value will be in the range -1...1 for keyboard and joystick input. 
	///  If the axis is setup to be wheel mouse movement, the mouse wheel is 
	///  multiplied by the axis sensitivity and the range is not -1...1. 
	/// </summary>
	float GetAxis( string axisName );

	/// <summary>
	/// Returns the current mouse position
	/// </summary>
	Vector2 GetMousePosition();

	/// <summary>
	/// Returns whether the given mouse button is held down.
	/// </summary>
	/// <param name="button">0 for left button, 1 for right button, 2 for the middle button</param>
	bool GetMouseButton( int button );

	/// <summary>
	/// Returns true during the frame the user pressed the given mouse button
	/// </summary>
	/// <param name="button">0 for left button, 1 for right button, 2 for the middle button</param>
	bool GetMouseButtonDown( int button );

	/// <summary>
	/// Returns true during the frame the user released the given mouse button
	/// </summary>
	/// <param name="button">0 for left button, 1 for right button, 2 for the middle button</param>
	bool GetMouseButtonUp( int button );

}
