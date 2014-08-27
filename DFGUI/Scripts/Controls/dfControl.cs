// *******************************************************
// Copyright 2013-2014 Daikon Forge
// *******************************************************
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using UnityColor = UnityEngine.Color;
using UnityColor32 = UnityEngine.Color32;

#region Documentation
/// <summary>
/// <b>Base class for all GUI controls</b>. Provides common functionality, event
/// handling, and properties for all user interface controls.
/// <h3>Control Position and Relative Coordinates</h3>
/// <para>DF-GUI uses a parent-relative coordinate system where the origin (0,0)
/// is at the upper left corner of the parent container, the horizontal axis
/// increases to the right, and the vertical axis increases in the downward direction.</para>
/// <para>For example, a control whose dfControl.RelativePosition value is set to 
/// (10,15) will be located ten pixels to the right and fifteen pixels down from the 
/// upper left corner of its container (or the upper-left corner of the screen if 
/// the control has no parent container).</para>
/// <para>Note that the value of the dfControl.RelativePosition and dfControl.Size
/// properties are assumed to represent pixels.</para>
/// <h3>Control Events</h3>
/// <para>Control events in DF-GUI have been designed so that they can be used 
/// in different ways depending on the needs of your game.</para>
/// <h4>Direct events</h4>
/// <para>Each control exposes a number of .NET events which can be subscribed to 
/// using standard .NET event subscription semantics. For example, a script that 
/// wishes to be notified whenever a dfLabel control named <b>health</b> has its 
/// <b>Text</b> property changed might attach an event handler to the <b>dfLabel.TextChanged</b>
/// event as follows:
/// <pre>
/// using UnityEngine;
/// using System.Collections;
/// using System.Collections.Generic;
/// 
/// public class Scratchpad : MonoBehaviour 
/// {
/// 
/// 	public dfLabel health;
/// 
/// 	public void Start()
/// 	{
/// 		health.TextChanged += new PropertyChangedEventHandler&lt;string&gt;( health_TextChanged );
/// 	}
/// 
/// 	void health_TextChanged( dfControl control, string value )
/// 	{
/// 		Debug.Log( "Health changed" );
/// 	}
/// 
/// }
/// </pre>
/// </para>
/// <h4>Reflection-based events</h4>
/// <para>A script component which is attached to the same this.gameObject as a control component
/// can also subscribe to events using a reflection-based model which involves creating an
/// appropriately-named method for the desired event which follows a specific pattern.</para>
/// <para>Such methods must be named On{XXX}, where {XXX} represents the name of the desired
/// event. For example, a script wishing to be notified of a Click event occuring would define
/// an event handler method as follows:
/// <pre>
/// public void OnClick( dfControl control, dfMouseEventArgs args )
/// {
/// 	Debug.Log( "My control was clicked" );
/// }
/// </pre></para>
/// <para>In the above example, the method's name indicates that it should be called when the 
/// <b>Click</b> event occurs, and the parameter list matches the MouseEventHandler signature 
/// specified by the <b>Click</b> event.</para>
/// <para>Event handlers specified in this manner also have the option of omitting all parameters.
/// If only the fact that the control was clicked were important, and the parameters are not
/// needed by the script, the above method could alternately be defined as:
/// <pre>
/// public void OnClick()
/// {
/// 	Debug.Log( "My control was clicked" );
/// }
/// </pre></para>
/// <para>Note that either all parameters must be included, or no parameters must be included.
/// It is not possible to only define only a subset of the defined parameters.</para>
/// <h4>Event Bubbling</h4>
/// <para>Many of the events raised by a dfControl instance will "bubble" up the control hierarchy.
/// This means that the event will first be processed by the control that raises the event, then 
/// that control's parent, then the control's parent's parent, and so on all the way up the control
/// hierarchy.</para>
/// <para>Specifically, any event whose method signature includes a dfControlEventArgs parameter
/// (or any subclass of dfControlEventArgs such as dfMouseEventArgs or dfDragEventArgs) will 
/// bubble up the hierarchy.</para>
/// <para>For these events, the original source of the event can be determined by the value of the
/// dfControlEventArgs.Source property. Whether the event has already been handled can be 
/// determined by the value of the dfControlEventArgs.Used property.</para>
/// <h4>Event handlers as coroutines</h4>
/// <para>Your event handlers can also be called as a <a href="http://docs.unity3d.com/Documentation/Manual/Coroutines.html" target="_blank">coroutine</a>, 
/// so that when the desired event is raised, your code can perform long-running asynchronous operations.</para>
/// <para>In order to function as a coroutine, your event handler must return an IEnumerator object and use the 
/// <a href="http://docs.unity3d.com/412/Documentation/ScriptReference/index.Coroutines_26_Yield.html" target="_blank">yield</a> keyword within the method body, like the following example:</para>
/// <pre>public IEnumerator OnClick( dfControl control, dfMouseEventArgs mouseEvent )
/// {
/// 
/// 	Debug.Log( "OnClick() started" );
/// 
/// 	yield return new WaitForSeconds( 2 );
/// 
/// 	Debug.Log( "OnClick() over" );
/// 
/// }</pre>
/// </summary>
#endregion
[Serializable]
[ExecuteInEditMode]
public abstract class dfControl : MonoBehaviour, IDFControlHost, IComparable<dfControl>
{

	#region Public events 

	#region Child dfControl Events 

	/// <summary>
	/// Occurs when a control is added to the Controls collection
	/// </summary>
	[HideInInspector]
	public event ChildControlEventHandler ControlAdded;

	/// <summary>
	/// Occurs when a control is removed from the Controls collection
	/// </summary>
	[HideInInspector]
	public event ChildControlEventHandler ControlRemoved;

	#endregion

	#region Focus events 

	/// <summary>
	/// Occurs when the control receives input focus
	/// </summary>
	public event FocusEventHandler GotFocus;

	/// <summary>
	/// Occurs when the control or any of its child controls receives input focus
	/// </summary>
	public event FocusEventHandler EnterFocus;

	/// <summary>
	/// Occurs when the control loses input focus
	/// </summary>
	public event FocusEventHandler LostFocus;

	/// <summary>
	/// Occurs when the control and all of its child controls lose input focus
	/// </summary>
	public event FocusEventHandler LeaveFocus;

	#endregion

	#region Property change events 

	/// <summary>
	/// Occurs when a control's IsVisible property is changed to TRUE.
	/// This event does not bubble up the control hierarchy.
	/// </summary>
	public event PropertyChangedEventHandler<bool> ControlShown;

	/// <summary>
	/// Occurs when a control's IsVisible property is changed to FALSE.
	/// This event does not bubble up the control hierarchy.
	/// </summary>
	public event PropertyChangedEventHandler<bool> ControlHidden;

	/// <summary>
	/// Raised whenever the clipping state of the control is changed, such as when the control 
	/// is moved from within the active clipping region to outside of the clipping region, or
	/// vice versa.
	/// </summary>
	public event PropertyChangedEventHandler<bool> ControlClippingChanged;

	/// <summary>
	/// Occurs when the control's TabIndex property is changed
	/// </summary>
	public event PropertyChangedEventHandler<int> TabIndexChanged;

	/// <summary>
	/// Occurs when the control's Position property is changed
	/// </summary>
	public event PropertyChangedEventHandler<Vector2> PositionChanged;

	/// <summary>
	/// Occurs when the control's Size propert is changed
	/// </summary>
	public event PropertyChangedEventHandler<Vector2> SizeChanged;

	/// <summary>
	/// Occurs when the control's Color value changes
	/// </summary>
	[HideInInspector]
	public event PropertyChangedEventHandler<Color32> ColorChanged;

	/// <summary>
	/// Occurs when the control's Visible property changes
	/// </summary>
	public event PropertyChangedEventHandler<bool> IsVisibleChanged;

	/// <summary>
	/// Occurs when the control's Enabled property value changes
	/// </summary>
	public event PropertyChangedEventHandler<bool> IsEnabledChanged;

	/// <summary>
	/// Occurs when the control's Opacity property value changes
	/// </summary>
	[HideInInspector]
	public event PropertyChangedEventHandler<float> OpacityChanged;

	/// <summary>
	/// Occurs when the control's Anchor property value changes
	/// </summary>
	[HideInInspector]
	public event PropertyChangedEventHandler<dfAnchorStyle> AnchorChanged;

	/// <summary>
	/// Occurs when the control's Pivot property value changes
	/// </summary>
	[HideInInspector]
	public event PropertyChangedEventHandler<dfPivotPoint> PivotChanged;

	/// <summary>
	/// Occurs when the control's ZOrder property value changes
	/// </summary>
	[HideInInspector]
	public event PropertyChangedEventHandler<int> ZOrderChanged;

	#endregion

	#region Drag and Drop events 

	/// <summary> Occurs (on the source dfControl) when a drag-and-drop operation is starting </summary>
	public event DragEventHandler DragStart;

	/// <summary> Occurs (on the source dfControl) when a drag-and-drop operation has ended </summary>
	public event DragEventHandler DragEnd;

	/// <summary> Occurs when a drag-and-drop operation is completed </summary>
	public event DragEventHandler DragDrop;

	/// <summary> Occurs when an object is dragged into the control's bounds </summary>
	public event DragEventHandler DragEnter;

	/// <summary> Occurs when an object is dragged out of the control's bounds </summary>
	public event DragEventHandler DragLeave;

	/// <summary> Occurs when an object is dragged over the control's bounds </summary>
	public event DragEventHandler DragOver;

	#endregion

	#region Mouse and Input Events 

	/// <summary> Occurs when the user presses a key while the control has input focus </summary>
	public event KeyPressHandler KeyPress;

	/// <summary> Occurs when the user presses a key while the control has input focus </summary>
	public event KeyPressHandler KeyDown;

	/// <summary> Occurs when the user releases a key while the control has input focus </summary>
	public event KeyPressHandler KeyUp;

	/// <summary> Occurs when more than one Touch is active for the control </summary>
	public event ControlMultiTouchEventHandler MultiTouch;

	/// <summary> Occurs when a touch is lifted and there is only one Touch now active for the control </summary>
	public event ControlCallbackHandler MultiTouchEnd;

	/// <summary> Occurs when the mouse pointer enters the control </summary>
	public event MouseEventHandler MouseEnter;

	/// <summary> Occurs when the mouse pointer is moved over the control </summary>
	public event MouseEventHandler MouseMove;

	/// <summary> Occurs when the mouse pointer rests on the control </summary>
	public event MouseEventHandler MouseHover;

	/// <summary> Occurs when the mouse pointer leaves the control </summary>
	public event MouseEventHandler MouseLeave;

	/// <summary> Occurs when the mouse pointer is over the control and a mouse button is pressed </summary>
	public event MouseEventHandler MouseDown;

	/// <summary> Occurs when a mouse button had previously been pressed on a control and a mouse button is released while the pointer is still over the control </summary>
	public event MouseEventHandler MouseUp;

	/// <summary> Occurs when the mouse wheel moves while the control has focus </summary>
	public event MouseEventHandler MouseWheel;

	/// <summary> Occurs when the control is clicked by the mouse </summary>
	public event MouseEventHandler Click;

	/// <summary> Occurs when the control is double clicked by the mouse </summary>
	public event MouseEventHandler DoubleClick;

	#endregion

	#endregion

	#region Constants and static variables 

	/// <summary>
	/// The threshold at which a nearly-invisible control becomes completely invisible
	/// </summary>
	private const float MINIMUM_OPACITY = 0.0125f;

	/// <summary>
	/// Global version counter for controls, ensures that each control 
	/// has a unique Version number
	/// </summary>
	// @private
	private static uint versionCounter;

	#endregion

	#region Serialized protected fields

	[SerializeField]
	protected dfAnchorStyle anchorStyle = dfAnchorStyle.None;

	[SerializeField]
	protected bool isEnabled = true;

	[SerializeField]
	protected bool isVisible = true;

	[SerializeField]
	protected bool isInteractive = true;

	[SerializeField]
	protected string tooltip = null;

	[SerializeField]
	protected dfPivotPoint pivot = dfPivotPoint.TopLeft;

	[HideInInspector]
	[SerializeField]
	public int zindex = int.MaxValue;

	[SerializeField]
	protected Color32 color = new Color32( 255, 255, 255, 255 );

	[SerializeField]
	protected Color32 disabledColor = new Color32( 255, 255, 255, 255 );

	[SerializeField]
	protected Vector2 size = Vector2.zero;

	[SerializeField]
	protected Vector2 minSize = Vector2.zero;

	[SerializeField]
	protected Vector2 maxSize = Vector2.zero;

	[SerializeField]
	protected bool clipChildren = false;

	[HideInInspector]
	[SerializeField]
	protected int tabIndex = -1;

	[HideInInspector]
	[SerializeField]
	protected bool canFocus = false;

	[SerializeField]
	protected bool autoFocus = false;

	/// <summary>
	/// Responsible for performing control resizing and layout to maintain 
	/// the anchor style
	/// </summary>
	[HideInInspector]
	[SerializeField]
	protected AnchorLayout layout = null;

	/// <summary>
	/// Used by the GUI system to correctly determine the order in which 
	/// controls should be considered during raycasting
	/// </summary>
	[HideInInspector]
	[SerializeField]
	protected int renderOrder = -1;

	/// <summary>
	/// Indicates whether this control should attempt to use localization
	/// </summary>
	[SerializeField]
	protected bool isLocalized = false;

	/// <summary>
	/// Multiplier applied to collider to allow for "hot zone" that is 
	/// larger than the control
	/// </summary>
	[SerializeField]
	protected Vector2 hotZoneScale = Vector2.one;

	[SerializeField]
	protected bool allowSignalEvents = true;

	#endregion

	#region Private runtime variables 

	private static object[] signal1 = new object[ 1 ];
	private static object[] signal2 = new object[ 2 ];
	private static object[] signal3 = new object[ 3 ];

	/// <summary>
	/// When set to TRUE, indicates that the control's render information is not 
	/// synchronized with the control's state and must be regenerated
	/// </summary>
	// @private
	protected bool isControlInvalidated = true;

	/// <summary>
	/// Indicates whether the control is currently clipped.
	/// </summary>
	// @private
	protected bool isControlClipped = false;

	/// <summary>
	/// Gets or sets the parent container of the control
	/// </summary>
	// @private
	protected dfControl parent = null;

	/// <summary>
	/// The collection of controls contained within the control
	/// </summary>
	// @private
	protected dfList<dfControl> controls = dfList<dfControl>.Obtain();

	/// <summary>
	/// The <see cref="dfGUIManager"/> instance which is responsible for rendering this control
	/// </summary>
	// @private
	protected dfGUIManager cachedManager = null;

	/// <summary>
	/// The <see cref="dfLanguageManager"/> instance which is responsible for returning
	/// localized versions of data
	/// </summary>
	// @private
	protected dfLanguageManager languageManager = null;

	/// <summary>
	/// Indicates whether a search for the localization manager has already been 
	/// performed. Minimizes calls to GetComponent().
	/// </summary>
	// @private
	protected bool languageManagerChecked = false;

	/// <summary>
	/// Used to detect when child nodes have been added to or removed
	/// from the dfControl's <see cref="UnityEngine.Transform"/>
	/// </summary>
	// @private
	protected int cachedChildCount = 0;

	/// <summary>
	/// Used to detect when the user moves the control via the 
	/// transform instead of the Position property
	/// </summary>
	// @private
	protected Vector3 cachedPosition = Vector3.one * float.MinValue;

	/// <summary>
	/// Used to detect when the control's rotation changes, in order
	/// to determine whether the control's render data needs to be rebuilt
	/// </summary>
	// @private
	protected Quaternion cachedRotation = Quaternion.identity;

	/// <summary>
	/// Used to detect when the control's scale changes, in order 
	/// to determine whether the control's render data needs to be rebuilt.
	/// Scale is not used by this library directly, but a developer may 
	/// wish to use scale for hover effects, etc.
	/// </summary>
	// @private
	protected Vector3 cachedScale = Vector3.one;

	/// <summary>
	/// Represents the control's axis-aligned bounding box
	/// </summary>
	// @private
	protected Bounds? cachedBounds;

	/// <summary>
	/// Tracking the cached parent transform helps in determining when 
	/// to invalidate control relationships and layouts. While the
	/// updateControlHierarchy() method takes care of most of this, it
	/// cannot detect when a control is reparented to a non-control 
	/// GameObject (such as the GUI Manager or a pool manager). This is 
	/// generally only useful for dynamically-instantiated objects and 
	/// prefabs, which might retain invalid layout information between 
	/// instantiations.
	/// </summary>
	protected Transform cachedParentTransform = null;

	/// <summary>
	/// Caching the "world units to pixels" conversion ratio allows the
	/// control to somewhat reduce the number of method calls
	/// </summary>
	// @private
	protected float cachedPixelSize = 0f;

	/// <summary>
	/// Calculating RelativePosition is expensive enough to warrant caching
	/// </summary>
	// @private
	protected Vector3 cachedRelativePosition = Vector3.one * float.MinValue;

	/// <summary>
	/// Used to determine whether relative position needs to be recalculated
	/// </summary>
	// @private
	protected uint relativePositionCacheVersion = uint.MaxValue;

	/// <summary>
	/// Maintains the information needed to render this control
	/// </summary>
	// @private
	protected dfRenderData renderData;

	/// <summary>
	/// Will be set to TRUE when the mouse is over the control. Can be used by 
	/// controls to determine how to display their current state
	/// </summary>
	// @private
	protected bool isMouseHovering = false;

	/// <summary>
	/// Contains user-defined data about the control
	/// </summary>
	// @private
	private new object tag;

	/// <summary>
	/// Will be set to TRUE when the object is being destroyed
	/// </summary>
	// @private
	protected bool isDisposing = false;

	/// <summary>
	/// Set when the control is performing layout, so that recursive 
	/// layouts do not loop forever
	/// </summary>
	// @private
	private bool performingLayout;

	/// <summary>
	/// Describes the control's upper-left, upper-right, bottom-left, and bottom-right 
	/// corner positions in world space
	/// </summary>
	// @private
	protected Vector3[] cachedCorners = new Vector3[ 4 ];

	/// <summary>
	/// Describes the set of Plane objects that comprise the control's clipping region
	/// </summary>
	// @private
	protected Plane[] cachedClippingPlanes = new Plane[ 4 ];

	/// <summary>
	/// Indicates that an application shutdown is in progress
	/// </summary>
	private bool shutdownInProgress;

	/// <summary>
	/// This value will be updated each time the dfControl is invalidated, and can 
	/// be compared against a cached value to determine whether a dfControl has 
	/// changed the last time the value was queried.
	/// </summary>
	// @private
	private uint version;

	/// <summary>
	/// Indicates whether the initializeControl() method has been called.
	/// </summary>
	protected bool isControlInitialized = false;

	/// <summary>
	/// Indicates whether the control is currently rendering, to prevent recursive render loops
	/// </summary>
	private bool rendering;

	/// <summary>
	/// Stores the control's localization key so that the control can be re-localized when the 
	/// active language is changed at runtime
	/// </summary>
	protected string localizationKey;

	#endregion

	#region Static fields 

	public static readonly dfList<dfControl> ActiveInstances = new dfList<dfControl>();

	#endregion 

	#region Public properties

	/// <summary>
	/// Gets or sets a value indicating whether this control can use Signal() to call event
	/// handlers via reflection. Defaults to TRUE.
	/// </summary>
	public bool AllowSignalEvents
	{
		get { return this.allowSignalEvents; }
		set { allowSignalEvents = value; }
	}

	/// <summary>
	/// Returns TRUE if the control needs to be re-rendered
	/// </summary>
	internal bool IsInvalid
	{
		get { return this.isControlInvalidated; }
	}

	/// <summary>
	/// Returns TRUE if the control is not visible due to clipping by the parent, and FALSE otherwise
	/// </summary>
	internal bool IsControlClipped
	{
		get { return this.isControlClipped; }
	}

	/// <summary>
	/// Returns a reference to the <see cref="dfGUIManager"/> instance 
	/// responsible for rendering this <see cref="dfControl"/>
	/// </summary>
	public dfGUIManager GUIManager
	{
		get { return GetManager(); }
	}

	/// <summary>
	/// Gets or sets a value indicating whether the control can respond to 
	/// user interaction.
	/// </summary>
	public bool IsEnabled
	{
		get 
		{
			if( !enabled ) return false;
			if( this.gameObject != null && !this.gameObject.activeSelf ) return false;
			return parent != null ? isEnabled && parent.IsEnabled : isEnabled; 
		}
		set
		{
			if( value != isEnabled )
			{
				isEnabled = value;
				OnIsEnabledChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the control and all its child 
	/// controls are displayed.
	/// </summary>
	[SerializeField]
	public bool IsVisible
	{
		get 
		{
			return parent == null ? isVisible : isVisible && parent.IsVisible; 
		}
		set
		{
			if( value != this.isVisible )
			{

				if( Application.isPlaying && !this.IsInteractive )
				{
					collider.enabled = false;
				}
				else
				{
					collider.enabled = value;
				}

				isVisible = value;
				OnIsVisibleChanged();

			}
		}
	}

	/// <summary>
	/// Gets/Sets a value indicating whether the control is user-interactive,
	/// ie: Whether the control responds to user input. This flag controls 
	/// whether the control has an current BoxCollider, because there are real
	/// performance implications for having a collider on a large number of
	/// controls that will never process user input.
	/// </summary>
	public virtual bool IsInteractive
	{
		get { return this.isInteractive; }
		set
		{
			if( this.HasFocus && !value )
			{
				dfGUIManager.SetFocus( null );
			}
			this.isInteractive = value;
		}
	}

	/// <summary>
	/// The tooltip to be displayed to the user when the mouse hovers over 
	/// the control. Currently not used directly by this library.
	/// </summary>
	[SerializeField]
	public string Tooltip
	{
		get { return this.tooltip; }
		set
		{
			if( value != tooltip )
			{
				tooltip = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the edges of the container to which a control is bound 
	/// and determines how a control is resized with its parent. 
	/// </summary>
	[SerializeField]
	public dfAnchorStyle Anchor
	{
		get 
		{
			ensureLayoutExists();
			return this.anchorStyle;
		}
		set
		{
			if( value != this.anchorStyle )
			{
				this.anchorStyle = value;
				OnAnchorChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the opacity level of the control
	/// </summary>
	public float Opacity
	{
		get { return (float)this.color.a / 255f; }
		set
		{
			value = Mathf.Max( 0, Mathf.Min( 1, value ) );
			var alpha = (float)this.color.a / 255f;
			if( !Mathf.Approximately( value, alpha ) )
			{
				this.color.a = (byte)( value * 255 );
				OnOpacityChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the color of the control
	/// </summary>
	public Color32 Color
	{
		get { return this.color; }
		set
		{
			value.a = (byte)( this.Opacity * 255 );
			if( !color.Equals( value ) )
			{
				color = value;
				OnColorChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the color that will be used when this control is disabled
	/// </summary>
	public UnityColor32 DisabledColor
	{
		get { return this.disabledColor; }
		set
		{
			if( !value.Equals( disabledColor ) )
			{
				disabledColor = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the Pivot Point of the control
	/// </summary>
	public dfPivotPoint Pivot
	{
		get { return pivot; }
		set
		{

			if( value != pivot )
			{

				var pos = Position;

				pivot = value;
				var offset = Position - pos;

				SuspendLayout();

				Position = pos;

				for( int i = 0; i < controls.Count; i++ )
				{
					controls[ i ].Position += offset;
				}

				ResumeLayout();

				OnPivotChanged();

			}

		}
	}

	/// <summary>
	/// Returns the relative coordinates of the upper-left corner of the control
	/// relative to the upper-left corner of the parent control, expressed in 
	/// "screen space" coordinates (left-top origin): x increases right, 
	/// y increases down
	/// </summary>
	public Vector3 RelativePosition
	{
		get
		{
			return getRelativePosition();
		}
		set
		{
			setRelativePosition( ref value );
		}
	}

	/// <summary>
	/// Gets or sets the local position of the upper-left
	/// corner of the control relative to its container's 
	/// pivot point
	/// </summary>
	public Vector3 Position
	{
		get
		{
			var transformPos = transform.localPosition / PixelsToUnits();
			return transformPos + pivot.TransformToUpperLeft( Size );
		}
		set
		{
			setPositionInternal( value );
		}
	}

	/// <summary>
	/// Gets or sets the size (in pixels) of the control
	/// </summary>
	public Vector2 Size
	{
		get { return this.size; }
		set
		{

			// Enforce minimum size
			value = Vector2.Max( CalculateMinimumSize(), value );

			// Enforce maximum size, if specified (value of 0 means "unlimited")
			value.x = maxSize.x > 0f ? Mathf.Min( value.x, maxSize.x ) : value.x;
			value.y = maxSize.y > 0f ? Mathf.Min( value.y, maxSize.y ) : value.y;

			// If there is no significant difference between the current size and
			// the specified size, then just exit without doing anything.
			if( ( value - size ).sqrMagnitude <= 1f )
				return;

			// Assign the new size value
			size = value;

			// Notify any listeners that this control's Size has changed
			OnSizeChanged();

		}
	}

	/// <summary>
	/// Gets or sets the width of the control in pixels
	/// </summary>
	public float Width
	{
		get { return size.x; }
		set { Size = new Vector2( value, size.y ); }
	}

	/// <summary>
	/// Gets or sets the height of the control in pixels
	/// </summary>
	public float Height
	{
		get { return size.y; }
		set { Size = new Vector2( size.x, value ); }
	}

	/// <summary>
	/// Gets or sets the minimum allowed size of the control
	/// </summary>
	public Vector2 MinimumSize
	{
		get { return this.minSize; }
		set
		{
			value = Vector2.Max( Vector2.zero, value.RoundToInt() );
			if( value != minSize )
			{
				minSize = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets or sets the maximum allowed size of the control
	/// </summary>
	public Vector2 MaximumSize
	{
		get { return this.maxSize; }
		set
		{
			value = Vector2.Max( Vector2.zero, value.RoundToInt() );
			if( value != maxSize )
			{
				maxSize = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Gets/Sets a value indicating the rendering order of this control
	/// </summary>
	[HideInInspector]
	public int ZOrder
	{
		get 
		{ 
			return this.zindex; 
		}
		set
		{
			if( value != zindex )
			{

				if( parent != null )
				{
					parent.SetControlIndex( this, value );
				}
				else
				{
					this.zindex = Mathf.Max( -1, value );
				}

				OnZOrderChanged();
				
			}
		}
	}

	/// <summary>
	/// Gets or sets the tab order of the control within its container. Set this
	/// value to -1 to remove this control from the tab order.
	/// </summary>
	[HideInInspector]
	public int TabIndex
	{
		get { return this.tabIndex; }
		set
		{
			if( value != this.tabIndex )
			{
				this.tabIndex = Mathf.Max( -1, value );
				OnTabIndexChanged();
			}
		}
	}

	/// <summary>
	/// Gets the collection of controls contained within the control
	/// </summary>
	public dfList<dfControl> Controls
	{
		get { return this.controls; }
	}

	/// <summary>
	/// Gets the parent container of the control
	/// </summary>
	public dfControl Parent
	{
		get { return this.parent; }
	}

	/// <summary>
	/// Indicates whether child controls will be clipped to the bounds 
	/// of this control
	/// </summary>
	public bool ClipChildren
	{
		get { return this.clipChildren; }
		set
		{
			if( value != this.clipChildren )
			{
				this.clipChildren = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Returns a value indicating whether the dfControl's layout engine is 
	/// currently suspended
	/// </summary>
	protected bool IsLayoutSuspended
	{
		get
		{
			return ( performingLayout || ( layout != null && layout.IsLayoutSuspended ) );
		}
	}

	/// <summary>
	/// Returns TRUE if the control is currently performing a layout operation
	/// and FALSE otherwise
	/// </summary>
	protected bool IsPerformingLayout
	{
		get
		{

			if( performingLayout )
				return true;

			if( layout != null && layout.IsPerformingLayout )
				return true;

			//if( parent != null && parent.IsPerformingLayout )
			//    return false;

			return false;

		}
	}

	/// <summary>
	/// Can be used to store additional data about the control instance
	/// </summary>
	public object Tag
	{
		get { return this.tag; }
		set { this.tag = value; }
	}

	/// <summary>
	/// Represents a dfControl's "version number", which is changed each
	/// time a dfControl's properties change. Can be compared against a cached
	/// value to determine whether the dfControl has changed since the last 
	/// time the value was queried.
	/// </summary>
	internal uint Version
	{
		get { return this.version; }
	}

	/// <summary>
	/// Gets or sets a value indicating whether this controls should use
	/// localized versions of its (class-specific) properties
	/// </summary>
	public bool IsLocalized
	{
		get { return this.isLocalized; }
		set
		{
			this.isLocalized = value;
			if( value )
			{
				this.Localize();
			}
		}
	}

	/// <summary>
	/// Gets or sets the size (as a multiplier) of the "hot zone" around 
	/// the control
	/// </summary>
	public Vector2 HotZoneScale
	{
		get { return this.hotZoneScale; }
		set 
		{
			this.hotZoneScale = Vector2.Max( value, Vector2.zero );
			Invalidate();
		}
	}

	/// <summary>
	/// Gets or sets whether the control will automatically be focused when enabled or shown
	/// </summary>
	public bool AutoFocus
	{
		get { return this.autoFocus; }
		set
		{
			if( value != this.autoFocus )
			{
				this.autoFocus = value;
				if( value && this.IsEnabled && this.CanFocus )
				{
					this.Focus();
				}
			}
		}
	}

	#endregion

	#region Members used by GUIManager class and intended to be overridden 

	/// <summary>
	/// Gets a value indicating whether the control can receive focus.
	/// </summary>
	public virtual bool CanFocus 
	{ 
		get { return this.canFocus && this.IsInteractive; }
		set { this.canFocus = value; }
	}

	/// <summary>
	/// Gets a value indicating whether the control, or one of its child controls, currently has the input focus.
	/// </summary>
	public virtual bool ContainsFocus
	{
		get { return dfGUIManager.ContainsFocus( this ); }
	}

	/// <summary>
	/// Gets a value indicating whether the control has user input focus.
	/// </summary>
	public virtual bool HasFocus 
	{
		get { return dfGUIManager.HasFocus( this ); }
	}

	/// <summary>
	/// Returns TRUE when the mouse is contained within the bounds of the control
	/// </summary>
	public bool ContainsMouse
	{
		get { return this.isMouseHovering; }
	}

	#region Raycast order management - Used for rendering and raycasting 

	internal void setRenderOrder( ref int order )
	{

		//updateControlHierarchy();
		this.renderOrder = ++order;

		var count = controls.Count;
		var items = controls.Items;

		for( int i = 0; i < count; i++ )
		{
			// NOTE: Edge case - Using Destroy( control ) without first removing a control from 
			// the parent's Controls collection will result in a null reference inside of the 
			// former parent control's Controls collection. Always use Parent.RemoveControl() 
			// before calling Destroy().
			if( items[ i ] != null )
			{
				items[ i ].setRenderOrder( ref order );
			}
		}

	}

	/// <summary>
	/// Returns the order in which this dfControl will be rendered.
	/// </summary>
	[HideInInspector]
	public int RenderOrder { get { return renderOrder; } }

	#endregion

	#region Drag-and-Drop events 

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when a drag operation 
	/// is requested on the control. 
	/// </summary>
	/// <param name="args">Contains information regarding the drag operation.
	/// Set the <paramref name="args"/>.State property to <see cref="dfDragDropState.Dragging"/>
	/// to allow the drag operation to continue, or <see cref="dfDragDropState.Denied"/> to 
	/// prevent the drag operation</param>
	internal virtual void OnDragStart( dfDragEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnDragStart", this, args );

			if( !args.Used && DragStart != null )
			{
				DragStart( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnDragStart( args );
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when a drag operation 
	/// for this control instance has completed.
	/// </summary>
	/// <param name="args">Contains information regarding the drag operation. Query the 
	/// <paramref name="args.State"/> property for the reason for the drag operation ending.
	/// This property will contain <see cref="dfDragDropState.Dropped"/> if the control 
	/// was dropped on a valid target, <see cref="dfDragDropState.Denied"/> if the drop
	/// operation was denied, <see cref="dfDragDropState.Cancelled"/> if the user cancelled
	/// the drag-and-drop operation (by pressing ESC for instance), or 
	/// <see cref="dfDragDropState.CancelledNoTarget"/> if the control was dropped without 
	/// being dropped on a valid drop target. If <see cref="args"/>.State is set to 
	/// <see cref="dfDragDropState.CancelledNoTarget"/> then args.Ray can be used to 
	/// perform a raycast to see where the control was dropped (such as a user dropping
	/// an inventory item on the ground, etc)</param>
	internal virtual void OnDragEnd( dfDragEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnDragEnd", this, args );

			if( !args.Used && DragEnd != null )
			{
				DragEnd( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnDragEnd( args );
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when another control is dropped
	/// on this control instance.
	/// </summary>
	/// <param name="args">Contains information about the drop operation. Set 
	/// <paramref name="args.State"/> to <see cref="dfDragDropState.Dropped"/> or 
	/// call <paramref name="args.Use()"/> to indicate a successful drop operation.</param>
	internal virtual void OnDragDrop( dfDragEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnDragDrop", this, args );

			if( !args.Used && DragDrop != null )
			{
				DragDrop( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnDragDrop( args );
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when another control is being 
	/// dragged over this control and has just entered the bounds of this control.
	/// </summary>
	/// <param name="args">Contains information about the drag operation</param>
	internal virtual void OnDragEnter( dfDragEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnDragEnter", this, args );

			if( !args.Used && DragEnter != null )
			{
				DragEnter( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnDragEnter( args );
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when another control is being 
	/// dragged over this control and has just left the bounds of this control.
	/// </summary>
	/// <param name="args">Contains information about the drag operation</param>
	internal virtual void OnDragLeave( dfDragEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnDragLeave", this, args );

			if( !args.Used && DragLeave != null )
			{
				DragLeave( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnDragLeave( args );
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when another control is being 
	/// dragged over this control and was already within the bounds of this control
	/// </summary>
	/// <param name="args">Contains information about the drag operation</param>
	internal virtual void OnDragOver( dfDragEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnDragOver", this, args );

			if( !args.Used && DragOver != null )
			{
				DragOver( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnDragOver( args );
		}

	}

	#endregion

	#region Multi-touch events 

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when a touch has been lifted
	/// and there is now only one 
	/// </summary>
	protected internal virtual void OnMultiTouchEnd()
	{

		Signal( "OnMultiTouchEnd", this );

		if( MultiTouchEnd != null )
		{
			MultiTouchEnd( this );
		}

		if( parent != null )
		{
			parent.OnMultiTouchEnd();
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when there is more than one 
	/// Touch event active for this control
	/// </summary>
	/// <param name="args">Contains information for all active Touch events</param>
	protected internal virtual void OnMultiTouch( dfTouchEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnMultiTouch", this, args );

			if( MultiTouch != null )
			{
				MultiTouch( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnMultiTouch( args );
		}

	}

	#endregion

	#region Mouse events

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when the mouse enters the
	/// bounds of this control
	/// </summary>
	/// <param name="args">Contains information about the action that triggered this event</param>
	protected internal virtual void OnMouseEnter( dfMouseEventArgs args )
	{

		this.isMouseHovering = true;
		
		if( !args.Used )
		{

			Signal( "OnMouseEnter", this, args );

			if( MouseEnter != null )
			{
				MouseEnter( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnMouseEnter( args );
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when the mouse leaves the
	/// bounds of this control
	/// </summary>
	/// <param name="args">Contains information about the action that triggered this event</param>
	protected internal virtual void OnMouseLeave( dfMouseEventArgs args )
	{

		isMouseHovering = false;

		if( !args.Used )
		{

			Signal( "OnMouseLeave", this, args );

			if( MouseLeave != null )
			{
				MouseLeave( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnMouseLeave( args );
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when the mouse is being moved
	/// over this control.
	/// </summary>
	/// <param name="args">Contains information about the action that triggered this event</param>
	protected internal virtual void OnMouseMove( dfMouseEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnMouseMove", this, args );

			if( MouseMove != null )
			{
				MouseMove( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnMouseMove( args );
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> periodically when the mouse 
	/// is within the bounds of this control but is not being moved by the user.
	/// </summary>
	/// <param name="args">Contains information about the action that triggered this event</param>
	protected internal virtual void OnMouseHover( dfMouseEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnMouseHover", this, args );

			if( MouseHover != null )
			{
				MouseHover( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnMouseHover( args );
		}

	}

	/// <summary>
	/// Called by the <see cref="dfInputManager"/> when the user rotates the
	/// mouse scroll wheel
	/// </summary>
	/// <param name="args">Contains information about the mouse action that triggered this event</param>
	protected internal virtual void OnMouseWheel( dfMouseEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnMouseWheel", this, args );

			if( MouseWheel != null )
			{
				MouseWheel( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnMouseWheel( args );
		}

	}

	/// <summary>
	/// Called by <see cref="dfInputManager"/> when the user presses a mouse
	/// button while the mouse is over this control
	/// </summary>
	/// <param name="args">Contains information about the mouse action that triggered this event</param>
	protected internal virtual void OnMouseDown( dfMouseEventArgs args )
	{

		var canSetFocus =
			IsInteractive &&
			IsEnabled &&
			IsVisible &&
			this.CanFocus &&
			!this.ContainsFocus;

		if( canSetFocus )
		{
			this.Focus();
		}

		if( !args.Used )
		{

			Signal( "OnMouseDown", this, args );

			if( MouseDown != null )
			{
				MouseDown( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnMouseDown( args );
		}

	}

	/// <summary>
	/// Called by <see cref="dfInputManager"/> when the user releases a mouse
	/// button while the mouse is over this control
	/// </summary>
	/// <param name="args">Contains information about the mouse action that triggered this event</param>
	protected internal virtual void OnMouseUp( dfMouseEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnMouseUp", this, args );

			if( MouseUp != null )
			{
				MouseUp( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnMouseUp( args );
		}

	}

	/// <summary>
	/// Processes a user click event
	/// </summary>
	protected internal virtual void OnClick( dfMouseEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnClick", this, args );

			if( Click != null )
			{
				Click( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnClick( args );
		}

	}

	/// <summary>
	/// Processes a user double-click event
	/// </summary>
	protected internal virtual void OnDoubleClick( dfMouseEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnDoubleClick", this, args );

			if( DoubleClick != null )
			{
				DoubleClick( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnDoubleClick( args );
		}

	}

	#endregion

	#region Keyboard events 

	/// <summary>
	/// Called by <see cref="dfInputManager"/> when the user presses a key
	/// while this control contains input focus. This method differs from
	/// <see cref="OnKeyDown"/> in that it is called to process user 
	/// text input events rather than for control key events.
	/// </summary>
	/// <param name="args">Contains information about the keyboard action that triggered this event</param>
	protected internal virtual void OnKeyPress( dfKeyEventArgs args )
	{

		if( this.IsInteractive && !args.Used )
		{

			Signal( "OnKeyPress", this, args );

			if( KeyPress != null )
			{
				KeyPress( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnKeyPress( args );
		}

	}

	/// <summary>
	/// Called by <see cref="dfInputManager"/> when the user presses a key
	/// while this control contains input focus. This method differs from 
	/// <see cref="OnKeyPress"/> in that it is called to process control key 
	/// events rather than text input events
	/// </summary>
	/// <param name="args">Contains information about the keyboard action that triggered this event</param>
	protected internal virtual void OnKeyDown( dfKeyEventArgs args )
	{

		if( this.IsInteractive && !args.Used )
		{

			// Special case : Tab key is used to navigate
			if( args.KeyCode == KeyCode.Tab )
			{

				// Call overridable function to handle tab key.
				OnTabKeyPressed( args );

			}

			// Need to check args.Used again in case it was a tab key
			if( !args.Used )
			{

				Signal( "OnKeyDown", this, args );

				if( KeyDown != null )
				{
					KeyDown( this, args );
				}

			}

		}

		if( parent != null )
		{
			parent.OnKeyDown( args );
		}

	}

	protected virtual void OnTabKeyPressed( dfKeyEventArgs args )
	{

		var sceneControls =
			GetManager().GetComponentsInChildren<dfControl>()
			.Where( c =>
				c != this &&
				c.TabIndex >= 0 &&
				c.IsInteractive &&
				c.CanFocus &&
				c.IsVisible
			)
			.ToList();

		if( sceneControls.Count == 0 )
			return;

		sceneControls.Sort( ( lhs, rhs ) =>
		{
			
			if( lhs.TabIndex == rhs.TabIndex )
				return lhs.RenderOrder.CompareTo( rhs.RenderOrder );

			return lhs.TabIndex.CompareTo( rhs.TabIndex );

		} );

		if( !args.Shift )
		{

			for( int i = 0; i < sceneControls.Count; i++ )
			{

				var nextControl = sceneControls[ i ];

				if( nextControl.TabIndex >= this.TabIndex )
				{

					sceneControls[ i ].Focus();
					args.Use();
					
					return;

				}

			}

			sceneControls[ 0 ].Focus();
			args.Use();

			return;

		}

		for( int i = sceneControls.Count - 1; i >= 0; i-- )
		{

			var nextControl = sceneControls[ i ];

			if( nextControl.TabIndex <= this.TabIndex )
			{

				sceneControls[ i ].Focus();
				args.Use();

				return;

			}

		}

		sceneControls[ sceneControls.Count - 1 ].Focus();
		args.Use();

	}

	/// <summary>
	/// Called by <see cref="dfInputManager"/> when the user releases a key
	/// while this control contains user input focus
	/// </summary>
	/// <param name="args">Contains information about the keyboard action that triggered this event</param>
	protected internal virtual void OnKeyUp( dfKeyEventArgs args )
	{

		if( this.IsInteractive )
		{

			Signal( "OnKeyUp", this, args );

			if( KeyUp != null )
			{
				KeyUp( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnKeyUp( args );
		}

	}

	#endregion

	#region Focus events

	/// <summary>
	/// Called by <see cref="dfGUIManager"/> when this control or one of its
	/// children (at any level) obtains user input focus. This differs from 
	/// the <see cref="OnGotFocus"/> event in that <see cref="OnGotFocus"/>
	/// refers specifically to the control itself gaining input focus, while
	/// this method will be called if any control lower in the hierarchy 
	/// gains input focus. This is analogous to the difference between the
	/// <see cref="HasFocus"/> and <see cref="ContainsFocus"/> members.
	/// </summary>
	/// <param name="args">Contains information about the focus change event</param>
	protected internal virtual void OnEnterFocus( dfFocusEventArgs args )
	{

		Signal( "OnEnterFocus", this, args );

		if( EnterFocus != null )
		{
			EnterFocus( this, args );
		}

		// NOTE: This event does not bubble up the hierarchy,
		// the UI manager sends it directly to each control 
		// individually

	}

	/// <summary>
	/// Called by <see cref="dfGUIManager"/> when this control and all of its
	/// child controls (at any level) no longer contain user input focus. This 
	/// differs from <see cref="OnLostFocus"/> in that <see cref="OnLostFocus"/>
	/// refers specifically to the control itself losing input focus, which 
	/// this method is only called when all controls lower in the hierarchy no
	/// longer have input focus. This is analogous to the difference between the
	/// <see cref="HasFocus"/> and <see cref="ContainsFocus"/> members.
	/// </summary>
	/// <param name="args">Contains information about the focus change event</param>
	protected internal virtual void OnLeaveFocus( dfFocusEventArgs args )
	{

		Signal( "OnLeaveFocus", this, args );

		if( LeaveFocus != null )
		{
			LeaveFocus( this, args );
		}

		// NOTE: This event does not bubble up the hierarchy,
		// the UI manager sends it directly to each control 
		// individually

	}

	/// <summary>
	/// Called by <see cref="dfGUIManager"/> when the control gains input focus
	/// </summary>
	/// <param name="args">Contains information about the focus change event</param>
	protected internal virtual void OnGotFocus( dfFocusEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnGotFocus", this, args );

			if( GotFocus != null )
			{
				GotFocus( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnGotFocus( args );
		}

	}

	/// <summary>
	/// Called by <see cref="dfGUIManager"/> when the control loses input focus
	/// </summary>
	/// <param name="args">Contains information about the focus change event</param>
	protected internal virtual void OnLostFocus( dfFocusEventArgs args )
	{

		if( !args.Used )
		{

			Signal( "OnLostFocus", this, args );

			if( LostFocus != null )
			{
				LostFocus( this, args );
			}

		}

		if( parent != null )
		{
			parent.OnLostFocus( args );
		}

	}

	#endregion

	/// <summary>
	/// Performs a SendMessage()-like event notification by searching the this.gameObject
	/// for components which have a method with the same name as the <paramref name="eventName"/>
	/// parameter and which have a signature that matches the types in the 
	/// <paramref name="args"/> array. 
	/// </summary>
	/// <param name="eventName">The name of the method to invoke</param>
	/// <param name="args">The parameters that will be passed to the method</param>
	/// <returns>Returns TRUE if a matching event handler was found and invoked</returns>
	// @private
	protected internal bool Signal( string eventName, object arg )
	{

		// Copy arguments to a static array to minimize memory allocation
		signal1[ 0 ] = arg;

		return Signal( this.gameObject, eventName, signal1 );

	}

	/// <summary>
	/// Performs a SendMessage()-like event notification by searching the this.gameObject
	/// for components which have a method with the same name as the <paramref name="eventName"/>
	/// parameter and which have a signature that matches the types in the 
	/// <paramref name="args"/> array. 
	/// </summary>
	/// <param name="eventName">The name of the method to invoke</param>
	/// <param name="args">The parameters that will be passed to the method</param>
	/// <returns>Returns TRUE if a matching event handler was found and invoked</returns>
	// @private
	protected internal bool Signal( string eventName, object arg1, object arg2 )
	{

		// Copy arguments to a static array to minimize memory allocation
		signal2[ 0 ] = arg1;
		signal2[ 1 ] = arg2;

		return Signal( this.gameObject, eventName, signal2 );

	}

	/// <summary>
	/// Performs a SendMessage()-like event notification by searching the this.gameObject
	/// for components which have a method with the same name as the <paramref name="eventName"/>
	/// parameter and which have a signature that matches the types in the 
	/// <paramref name="args"/> array. 
	/// </summary>
	/// <param name="eventName">The name of the method to invoke</param>
	/// <param name="args">The parameters that will be passed to the method</param>
	/// <returns>Returns TRUE if a matching event handler was found and invoked</returns>
	// @private
	protected internal bool Signal( string eventName, object arg1, object arg2, object arg3 )
	{

		// Copy arguments to a static array to minimize memory allocation
		signal3[ 0 ] = arg1;
		signal3[ 1 ] = arg2;
		signal3[ 2 ] = arg3;

		return Signal( this.gameObject, eventName, signal3 );

	}

	/// <summary>
	/// Performs a SendMessage()-like event notification by searching the this.gameObject
	/// for components which have a method with the same name as the <paramref name="eventName"/>
	/// parameter and which have a signature that matches the types in the 
	/// <paramref name="args"/> array. 
	/// </summary>
	/// <param name="eventName">The name of the method to invoke</param>
	/// <param name="args">The parameters that will be passed to the method</param>
	/// <returns>Returns TRUE if a matching event handler was found and invoked</returns>
	// @private
	protected internal bool Signal( string eventName, object[] args )
	{
		return Signal( this.gameObject, eventName, args );
	}

	/// <summary>
	/// Performs a SendMessage()-like event notification by searching the this.gameObject
	/// for components which have a method with the same name as the <paramref name="eventName"/>
	/// parameter and which have a signature that matches the types in the 
	/// <paramref name="args"/> array. This function will walk up the object hierarchy until
	/// it finds a component with a matching event handler.
	/// </summary>
	/// <param name="eventName">The name of the method to invoke</param>
	/// <param name="args">The parameters that will be passed to the method</param>
	/// <returns>Returns TRUE if a matching event handler was found and invoked</returns>
	protected internal bool SignalHierarchy( string eventName, params object[] args )
	{

		if( !allowSignalEvents )
			return false;

		var signalReceived = false;
		var loop = transform;

		while( !signalReceived && loop != null )
		{
			signalReceived = Signal( loop.gameObject, eventName, args );
			loop = loop.parent;
		}

		return signalReceived;

	}

	/// <summary>
	/// Performs a SendMessage()-like event notification by searching the gameObject
	/// for components which have a method with the same name as the <paramref name="eventName"/>
	/// parameter and which have a signature that matches the types in the 
	/// <paramref name="args"/> array. 
	/// </summary>
	/// <param name="target">The gameObject on which to raise the event</param>
	/// <param name="eventName">The name of the method to invoke</param>
	/// <param name="args">The parameters that will be passed to the method</param>
	/// <returns>Returns TRUE if a matching event handler was found and invoked</returns>
	[HideInInspector]
	// @private
	protected internal bool Signal( GameObject target, string eventName, object arg )
	{

		// Copy arguments to a static array to minimize memory allocation
		signal1[ 0 ] = arg;

		return Signal( target, eventName, signal1 );

	}

	/// <summary>
	/// Performs a SendMessage()-like event notification by searching the gameObject
	/// for components which have a method with the same name as the <paramref name="eventName"/>
	/// parameter and which have a signature that matches the types in the 
	/// <paramref name="args"/> array. 
	/// </summary>
	/// <param name="target">The gameObject on which to raise the event</param>
	/// <param name="eventName">The name of the method to invoke</param>
	/// <param name="args">The parameters that will be passed to the method</param>
	/// <returns>Returns TRUE if a matching event handler was found and invoked</returns>
	[HideInInspector]
	// @private
	protected internal bool Signal( GameObject target, string eventName, object[] args )
	{

		if( !allowSignalEvents || target == null || shutdownInProgress || !Application.isPlaying )
			return false;

		// Retrieve the list of MonoBehaviour instances on the target object
		var components = target.GetComponents<MonoBehaviour>();

		// Exit early if there are no attached behaviors or this dfControl is 
		// the only attached behavior
		if( components == null || ( target == this.gameObject && components.Length == 1 ) )
			return false;

		// Need to ensure that the 'this' pointer is always sent with the 
		// event arguments. This also has the side benefit of differentiating
		// the method signatures from the built-in methods
		if( args.Length == 0 || !ReferenceEquals( args[ 0 ], this ) )
		{
			var newArgs = new object[ args.Length + 1 ];
			Array.Copy( args, 0, newArgs, 1, args.Length );
			newArgs[ 0 ] = this;
			args = newArgs;
		}

		bool wasHandled = false;

		for( int i = 0; i < components.Length; i++ )
		{

			var component = components[ i ];

			// Should never happen, but seems to happen occasionally during a 
			// long recompile in the Editor. Unity bug?
			if( component == null || component.GetType() == null )
				continue;

			if( component == this )
				continue;

			if( component is MonoBehaviour && !( (MonoBehaviour)component ).enabled )
				continue;

			object returnValue = null;

			var handledByComponent = SignalCache.Invoke( component, eventName, args, out returnValue );
			if( handledByComponent )
			{

				wasHandled = true;

				if( returnValue is IEnumerator && component is MonoBehaviour )
				{
					( (MonoBehaviour)component ).StartCoroutine( (IEnumerator)returnValue );
				}

			}

		}

		return wasHandled;

	}

	#endregion

	#region Public methods 

	/// <summary>
	/// Returns TRUE if this control is a top-level control in the 
	/// control hierarchy of the specified dfGUIManager instance.
	/// This function is intended for internal use only.
	/// </summary>
	internal bool IsTopLevelControl( dfGUIManager manager )
	{
		return this.parent == null && this.cachedManager == manager;
	}

	/// <summary>
	/// Returns the raw value of the isVisible field, without 
	/// the recursive costs of using the IsVisible property.
	/// </summary>
	/// <returns></returns>
	internal bool GetIsVisibleRaw()
	{
		return this.isVisible;
	}

	/// <summary>
	/// Causes the control to use localized data for its (class-specific)
	/// properties.
	/// </summary>
	public void Localize()
	{

		if( !this.IsLocalized )
			return;

		if( languageManager == null )
		{

			this.languageManager = GetManager().GetComponent<dfLanguageManager>();
			if( this.languageManager == null )
				return;

		}

		OnLocalize();

	}

	/// <summary>
	/// Simulates the user clicking on the control
	/// </summary>
	public void DoClick()
	{

		var renderCamera = GetCamera();
		var location = renderCamera.WorldToScreenPoint( GetCenter() );
		var ray = renderCamera.ScreenPointToRay( location );

		this.OnClick( new dfMouseEventArgs( this, dfMouseButtons.Left, 1, ray, location, 0f ) );

	}

	/// <summary>
	/// Detaches all event handlers for the named event
	/// </summary>
	/// <param name="eventName"></param>
	[HideInInspector]
	protected internal void RemoveEventHandlers( string eventName )
	{

		var namedEvent =
			this.GetType()
			.GetAllFields()
			.FirstOrDefault( f => 
				typeof( Delegate ).IsAssignableFrom( f.FieldType ) &&
				f.Name == eventName
			);

		if( namedEvent != null )
		{
			namedEvent.SetValue( this, null );
		}

	}

	/// <summary>
	/// Detaches all event handlers for all events on this <see cref="dfControl"/>
	/// </summary>
	[HideInInspector]
	internal void RemoveAllEventHandlers()
	{

		var events =
			this.GetType()
			.GetAllFields()
			.Where( f => typeof( Delegate ).IsAssignableFrom( f.FieldType ) )
			.ToArray();

		for( int i = 0; i < events.Length; i++ )
		{
			events[ i ].SetValue( this, null );
		}

	}

	/// <summary>
	/// Show the control. Bindable alternative to setting IsVisible = true
	/// </summary>
	public void Show()
	{
		this.IsVisible = true;
	}

	/// <summary>
	/// Hide the control. Bindable alternative to setting IsVisible = false
	/// </summary>
	public void Hide()
	{
		this.IsVisible = false;
	}

	/// <summary>
	/// Enable the control. Bindable alternative to setting IsEnabled = true
	/// </summary>
	public void Enable()
	{
		this.IsEnabled = true;
	}

	/// <summary>
	/// Disables the control. Bindable alternative to setting IsEnabled = false
	/// </summary>
	public void Disable()
	{
		this.IsEnabled = false;
	}

	/// <summary>
	/// Returns TRUE if the indicated ray intersects the control, and FALSE otherwise
	/// </summary>
	public bool HitTest( Ray ray )
	{

		var plane = new Plane( transform.TransformDirection( Vector3.back ), transform.position );

		var distance = 0f;
		if( !plane.Raycast( ray, out distance ) )
		{
			return false;
		}

		var hit = ray.origin + ray.direction * distance;

		var planes = ClipChildren ? this.GetClippingPlanes() : null;
		if( planes != null && planes.Length > 0 )
		{
			for( int i = 0; i < planes.Length; i++ )
			{
				if( !planes[ i ].GetSide( hit ) )
				{
					return false;
				}
			}
		}

		return true;

	}

	/// <summary>
	/// Returns the relative position in screen coordinates (X increases to the right, 
	/// Y increases downward, top-left origin) of the point where the ray intersects this control. Returns 
	/// TRUE if the ray intersects the control and assigns the relative hit location to 
	/// the <paramref name="ray"/> argument.
	/// </summary>
	public Vector2 GetHitPosition( Ray ray )
	{

		Vector2 position;
		if( !GetHitPosition( ray, out position, false ) )
		{
			return Vector2.one * float.MinValue;
		}

		return position;

	}

	/// <summary>
	/// Returns the relative position in screen coordinates (X increases to the right, 
	/// Y increases downward, top-left origin) of the point where the ray intersects this control. Returns 
	/// TRUE if the ray intersects the control and assigns the relative hit location to 
	/// the <paramref name="position"/> argument.
	/// </summary>
	public bool GetHitPosition( Ray ray, out Vector2 position )
	{
		return GetHitPosition( ray, out position, true );
	}

	/// <summary>
	/// Returns the relative position in screen coordinates (X increases to the right, 
	/// Y increases downward, top-left origin) of the point where the ray intersects this control. Returns 
	/// TRUE if the ray intersects the control and assigns the relative hit location to 
	/// the <paramref name="position"/> argument.
	/// </summary>
	public bool GetHitPosition( Ray ray, out Vector2 position, bool clamp )
	{

		position = Vector2.one * float.MinValue;

		var plane = new Plane( transform.TransformDirection( Vector3.back ), transform.position );

		var distance = 0f;
		if( !plane.Raycast( ray, out distance ) )
		{
			return false;
		}

		var hit = ray.GetPoint( distance );
		 
		var planes = ClipChildren ? this.GetClippingPlanes() : null;
		if( planes != null && planes.Length > 0 )
		{
			for( int i = 0; i < planes.Length; i++ )
			{
				if( !planes[ i ].GetSide( hit ) )
				{
					return false;
				}
			}
		}

		var corners = GetCorners();
		var ul = corners[ 0 ];
		var ur = corners[ 1 ];
		var bl = corners[ 2 ];

		var closest = closestPointOnLine( ul, ur, hit, clamp );
		var lerp = ( closest - ul ).magnitude / ( ur - ul ).magnitude;
		var x = size.x * lerp;

		closest = closestPointOnLine( ul, bl, hit, clamp );
		lerp = ( closest - ul ).magnitude / ( bl - ul ).magnitude;
		var y = size.y * lerp;

		position = new Vector2( x, y );

		return true;

	}

	/// <summary>
	/// Performs a breadth-first search for a dfControl instance with the same
	/// name as the <paramref name="controlName"/> argument <i>and</i> which is of the specified Type. 
	/// This search is case-sensitive.
	/// </summary>
	/// <typeparam name="T">The Type of control to find (must derive from <see cref="dfControl"/>)</typeparam>
	/// <param name="controlName">The name of the dfControl you wish to find</param>
	/// <returns>TRUE if the dfControl was located, FALSE otherwise</returns>
	public T Find<T>( string controlName ) where T : dfControl
	{

		if( this.name == controlName && this is T )
			return (T)this;

		updateControlHierarchy( true );

		for( var i = 0; i < controls.Count; i++ )
		{
			var test = controls[ i ] as T;
			if( test != null && test.name == controlName )
				return test;
		}

		for( var i = 0; i < controls.Count; i++ )
		{
			var test = controls[ i ].Find<T>( controlName );
			if( test != null )
				return test;
		}


		return null;

	}

	/// <summary>
	/// Performs a breadth-first search for a dfControl instance with the same
	/// name as the <paramref name="controlName"/> argument. This search is case-sensitive.
	/// </summary>
	/// <param name="controlName">The name of the dfControl you wish to find</param>
	/// <returns>TRUE if the dfControl was located, FALSE otherwise</returns>
	public dfControl Find( string controlName )
	{

		if( this.name == controlName )
			return this;

		updateControlHierarchy( true );

		for( int i = 0; i < controls.Count; i++ )
		{
			var test = controls[ i ];
			if( test.name == controlName )
				return test;
		}

		for( int i = 0; i < controls.Count; i++ )
		{
			var test = controls[ i ].Find( controlName );
			if( test != null )
				return test;
		}

		return null;

	}

	/// <summary>
	/// Sets the global input focus to this object
	/// </summary>
	public void Focus()
	{

		if( !CanFocus || HasFocus || !IsEnabled || !IsVisible )
			return;

		dfGUIManager.SetFocus( this );

		Invalidate();

	}

	/// <summary>
	/// Removes global input focus from this object
	/// </summary>
	public void Unfocus()
	{
		if( ContainsFocus )
		{
			dfGUIManager.SetFocus( null );
		}
	}

	/// <summary>
	/// Returns a reference to the top-most container for this control
	/// </summary>
	public dfControl GetRootContainer()
	{
		var loop = this;
		while( loop.Parent != null )
		{
			loop = loop.Parent;
		}
		return loop;
	}

	/// <summary>
	/// Brings this control to the front so that it appears in front of all 
	/// other controls within the same container
	/// </summary>
	public virtual void BringToFront()
	{
		if( parent == null )
		{
			GetManager().BringToFront( this );
		}
		else
		{
			parent.SetControlIndex( this, int.MaxValue );
		}
		Invalidate();
	}

	/// <summary>
	/// Sends this control to the back so that it appears behind all 
	/// other controls within the same container
	/// </summary>
	public virtual void SendToBack()
	{
		if( parent == null )
		{
			GetManager().SendToBack( this );
		}
		else
		{
			parent.SetControlIndex( this, 0 );
		}
		Invalidate();
	}

	/// <summary>
	/// Renders this control to a <see cref="dfRenderData"/> buffer and
	/// returns the buffer
	/// </summary>
	/// <returns>A <see cref="dfRenderData"/> buffer containing the data 
	/// needed to render this <see cref="dfControl"/> instance as a Mesh</returns>
	internal dfRenderData Render()
	{

		// Prevent recursion 
		if( rendering )
			return this.renderData;

		try
		{

#if UNITY_EDITOR
			//@Profiler.BeginSample( "Rendering " + GetType().Name );
#endif

			rendering = true;

			// NOTE: We can get away with checking the isVisible field instead
			// of the recursive IsVisible property because GUIManager.Render()
			// recursively iterates through all controls, and it's the first 
			// dfControl with the isVisible field set to FALSE that controls the
			// entire heirarchy below it.
			var controlIsVisible = this.isVisible;
			var controlIsEnabled = this.enabled && this.gameObject.activeSelf;
			if( !controlIsVisible || !controlIsEnabled )
				return null;

			if( renderData == null )
			{
				renderData = dfRenderData.Obtain();
				isControlInvalidated = true;
			}

			if( isControlInvalidated )
			{

				// Rebuild the control's render data and set collider size
				renderData.Clear();
				OnRebuildRenderData();
				updateCollider();

			}

			// *ALWAYS* provide the current local-to-world transform!
			// This allows the RenderData to be re-used when the control
			// is translated, rotated, or scaled without having to completely
			// rebuild the buffer.
			renderData.Transform = this.transform.localToWorldMatrix;

			return renderData;

		}
		finally
		{
		
			isControlInvalidated = false;
			rendering = false;

			//if( AfterControlRendered != null )
			//{
			//	AfterControlRendered( this, this.renderData );
			//}

#if UNITY_EDITOR
			//@Profiler.EndSample();
#endif
		
		}

	}

	/// <summary>
	/// Called when the control needs to rebuild its render information
	/// </summary>
	[HideInInspector]
	public virtual void Invalidate()
	{

		if( shutdownInProgress )
			return;

		updateVersion();

		this.isControlInvalidated = true;
		this.cachedBounds = null;

		// NOTE: Not using the cached [view] property here. Workaround for a Unity bug
		var myView = GetManager();
		if( myView != null )
		{
			myView.Invalidate();
		}

		dfRenderGroup.InvalidateGroupForControl( this );

	}

	/// <summary>
	/// Causes the control to reset all layout information 
	/// </summary>
	/// <param name="recursive">Set to TRUE if the layout information should be
	/// reset recursively</param>
	/// <param name="force">Set to TRUE to force the layout, even if SuspendLayout
	/// has been set to TRUE</param>
	[HideInInspector]
	public void ResetLayout()
	{
		ResetLayout( false, false );
	}

	/// <summary>
	/// Causes the control to reset all layout information 
	/// </summary>
	/// <param name="recursive">Set to TRUE if the layout information should be
	/// reset recursively</param>
	/// <param name="force">Set to TRUE to force the layout, even if SuspendLayout
	/// has been set to TRUE</param>
	[HideInInspector]
	public void ResetLayout( bool recursive, bool force )
	{

		if( shutdownInProgress )
			return;

		bool dontPerformLayout = ( IsPerformingLayout || IsLayoutSuspended );
		if( !force && dontPerformLayout )
			return;

		if( layout == null )
		{
			layout = new AnchorLayout( this.anchorStyle, this );
		}
		else
		{
			layout.Attach( this );
			layout.Reset( true );
		}

		if( recursive )
		{

			// Recursively reset the layout for all child controls
			var count = controls.Count;
			var items = controls.Items;
			for( int i = 0; i < count; i++ )
			{
				items[ i ].ResetLayout();
			}

		}

	}

	/// <summary>
	/// Causes this control to update its layout
	/// </summary>
	[HideInInspector]
	public void PerformLayout()
	{

		if( shutdownInProgress )
			return;

		if( isDisposing || performingLayout )
			return;

		try
		{
			
			performingLayout = true;

			ensureLayoutExists();

			layout.PerformLayout();
			Invalidate();

		}
		finally
		{
			performingLayout = false;
		}

	}

	/// <summary>
	/// Temporarily suspends the layout logic for the control
	/// </summary>
	[HideInInspector]
	public void SuspendLayout()
	{

		ensureLayoutExists(); 
		layout.SuspendLayout();

		for( int i = 0; i < controls.Count; i++ )
		{
			controls[ i ].SuspendLayout();
		}

	}

	/// <summary>
	/// Resumes usual layout logic
	/// </summary>
	[HideInInspector]
	public void ResumeLayout()
	{

		ensureLayoutExists(); 
		layout.ResumeLayout();

		for( int i = 0; i < controls.Count; i++ )
		{
			controls[ i ].ResumeLayout();
		}

	}

	/// <summary>
	/// Used during layout to determine the effective minimum size of the control,
	/// which may be different than that specified by the MinimumSize property. 
	/// </summary>
	/// <returns>A Vector2 value that represents the minimum control size</returns>
	public virtual Vector2 CalculateMinimumSize()
	{
		return this.MinimumSize;
	}

	/// <summary>
	/// Causes this control to textAlign its <see cref="Position"/> and <see cref="Size"/>
	/// properties so that they lie exactly on pixel boundaries
	/// </summary>
	[HideInInspector]
	public void MakePixelPerfect()
	{
		MakePixelPerfect( true );
	}	
	/// <summary>
	/// Causes this control to textAlign its <see cref="Position"/> and <see cref="Size"/>
	/// properties so that they lie exactly on pixel boundaries
	/// </summary>
	[HideInInspector]
	public void MakePixelPerfect( bool recursive )
	{

		this.size = this.size.RoundToInt();

		var p2u = PixelsToUnits();
		transform.position = ( transform.position / p2u ).RoundToInt() * p2u;
		cachedPosition = transform.localPosition;

		for( int i = 0; i < controls.Count && recursive; i++ )
		{
			controls[ i ].MakePixelPerfect();
		}

		Invalidate();

	}

	/// <summary>
	/// Returns the axis-aligned bounding box enclosing this <see cref="dfControl"/>
	/// </summary>
	public Bounds GetBounds()
	{

		// If we've already got a collider on this control (control is interactive), 
		// then we get a performance gain by simply returning the collider's Bounds property.
		if( isInteractive && collider != null )
		{
			cachedBounds = collider.bounds;
			return cachedBounds.Value;
		}

		if( cachedBounds.HasValue )
			return cachedBounds.Value;

		var corners = GetCorners();

		var center = corners[ 0 ] + ( corners[ 3 ] - corners[ 0 ] ) * 0.5f;
		var min = center;
		var max = center;

		for( int i = 0; i < corners.Length; i++ )
		{
			min = Vector3.Min( min, corners[ i ] );
			max = Vector3.Max( max, corners[ i ] );
		}

		cachedBounds = new Bounds( center, ( max - min ) );

		return cachedBounds.Value;

	}

	/// <summary>
	/// Returns a <see cref="UnityEngine.Vector3"/> representing the global 
	/// coordinates of the dfControl's center
	/// </summary>
	public Vector3 GetCenter()
	{
		var corners = GetCorners();
		return corners[ 0 ] + ( corners[ 3 ] - corners[ 0 ] ) * 0.5f;
	}

	/// <summary>
	/// Returns the control's absolute position relative to the dfGUIManager instance 
	/// that is hosting the control
	/// </summary>
	/// <returns></returns>
	public Vector3 GetAbsolutePosition()
	{

		var absolutePosition = Vector3.zero;
		var loop = this;

		while( loop != null )
		{
			absolutePosition += loop.getRelativePosition();
			loop = loop.Parent;
		}

		return absolutePosition;

	}

	/// <summary>
	/// Returns an array of Vector3 values corresponding to the global
	/// positions of this object's bounding box. The corners are specified
	/// in the following order: Top Left, Top Right, Bottom Left, Bottom Right
	/// </summary>
	public Vector3[] GetCorners()
	{

		var p2u = PixelsToUnits();
		var matrix = transform.localToWorldMatrix;

		var upperLeft = pivot.TransformToUpperLeft( size );
		var upperRight = upperLeft + new Vector3( size.x, 0 );
		var bottomLeft = upperLeft + new Vector3( 0, -size.y );
		var bottomRight = upperRight + new Vector3( 0, -size.y );

		if( cachedCorners == null )
			cachedCorners = new Vector3[ 4 ];

		cachedCorners[ 0 ] = matrix.MultiplyPoint( upperLeft * p2u );
		cachedCorners[ 1 ] = matrix.MultiplyPoint( upperRight * p2u );
		cachedCorners[ 2 ] = matrix.MultiplyPoint( bottomLeft * p2u );
		cachedCorners[ 3 ] = matrix.MultiplyPoint( bottomRight * p2u );

		return cachedCorners;

	}

	/// <summary>
	/// Returns a reference to the <see cref="UnityEngine.Camera"/> that is
	/// responsible for rendering this <see cref="dfControl"/>
	/// </summary>
	public Camera GetCamera()
	{

		var view = GetManager();
		if( view == null )
		{
			Debug.LogError( "The Manager hosting this control could not be determined" );
			return null;
		}

		return view.RenderCamera;

	}

	/// <summary>
	/// Returns the padding used when clipping is enabled and 
	/// the renderer is using shader-based clipping
	/// </summary>
	/// <returns></returns>
	protected internal virtual RectOffset GetClipPadding()
	{
		return dfRectOffsetExtensions.Empty;
	}

	/// <summary>
	/// Returns the Screen-based coordinates containing this control
	/// </summary>
	public Rect GetScreenRect()
	{

		var renderCamera = GetCamera();
		var corners = GetCorners();

		var min = Vector2.one * float.MaxValue;
		var max = Vector2.one * float.MinValue;

		var count = corners.Length;
		for( var i = 0; i < count; i++ )
		{
			var screenPos = renderCamera.WorldToScreenPoint( corners[ i ] );
			min = Vector2.Min( min, screenPos );
			max = Vector2.Max( max, screenPos );
		}

		return new Rect(
			min.x,
			Screen.height - max.y,
			max.x - min.x,
			max.y - min.y
		);

	}

	/// <summary>
	/// Returns a reference to the <see cref="dfGUIManager"/> instance that 
	/// is responsible for rendering the control
	/// </summary>
	public dfGUIManager GetManager()
	{

		// If the view is already cached or there is no way of obtaining
		// a reference to the view, return the cached value
		if( cachedManager != null || !this.gameObject.activeInHierarchy )
			return cachedManager;

		// If this dfControl's parent has already done the work of looking
		// for the Manager, then use that information instead
		if( parent != null && parent.cachedManager != null )
			return cachedManager = parent.cachedManager;

		// Walk up the scene hierarchy looking for the root Manager
		var loop = this.gameObject;
		while( loop != null )
		{

			var test = loop.GetComponent<dfGUIManager>();
			if( test != null )
				return cachedManager = test;

			if( loop.transform.parent == null )
				break;

			loop = loop.transform.parent.gameObject;

		}

		// Failed to find a dfGUIManager instance in the control hierarchy. This might 
		// mean that this control is a newly-instantiated prefab which is not yet 
		// part of the control hierarchy. Return a reference to the first dfGUIManager
		// instance in the scene, but do not assign the cached reference

		return dfGUIManager.ActiveManagers.FirstOrDefault();

	}

	/// <summary>
	/// Returns a number representing the conversion of World Units to pixels,
	/// used to convert a dfControl's "pixel-based" position and location properties
	/// into world units for rendering and raycasting purposes.
	/// </summary>
	public float PixelsToUnits()
	{

		if( cachedPixelSize > float.Epsilon )
			return cachedPixelSize;

		var view = GetManager();
		if( view == null )
		{

			// When no camera is available to get pixel size information from,
			// this default will return a reasonable approximation of a screen
			// whose height is 768 pixels
			const float DEFAULT_PIXEL_SIZE = 0.0026f;

			return DEFAULT_PIXEL_SIZE;

		}

		return cachedPixelSize = view.PixelsToUnits();

	}

	/// <summary>
	/// Returns the set of clipping planes used to clip child controls.
	/// Planes are specified in the following order: Left, Right, Top, Bottom
	/// </summary>
	/// <returns>Returns an array of <see cref="Plane"/> that enclose the object in world coordinates</returns>
	protected internal virtual Plane[] GetClippingPlanes()
	{

		var corners = GetCorners();

		var right = transform.TransformDirection( Vector3.right );
		var left = transform.TransformDirection( Vector3.left );
		var up = transform.TransformDirection( Vector3.up );
		var down = transform.TransformDirection( Vector3.down );

		cachedClippingPlanes[ 0 ] = new Plane( right, corners[ 0 ] );
		cachedClippingPlanes[ 1 ] = new Plane( left, corners[ 1 ] );
		cachedClippingPlanes[ 2 ] = new Plane( up, corners[ 2 ] );
		cachedClippingPlanes[ 3 ] = new Plane( down, corners[ 0 ] );

		return cachedClippingPlanes;

	}

	/// <summary>
	/// Retrieves a value indicating whether the specified control is a child of this control.
	/// </summary>
	/// <param name="child">The <see cref="dfControl"/> to evaluate</param>
	/// <returns>TRUE if the specified control is a child of the control and FALSE otherwise</returns>
	public bool Contains( dfControl child )
	{
		return ( child != null ) && child.transform.IsChildOf( this.transform );
	}

	#endregion

	#region Protected methods 

	[HideInInspector]
	protected internal virtual void OnLocalize()
	{
		// Stub. Intended to be overridden by specific control types
	}

	[HideInInspector]
	protected internal string getLocalizedValue( string key )
	{

		// If the control is not localized, or the application is not 
		// currently running, then return original value
		if( !this.IsLocalized || !Application.isPlaying )
			return key;

		if( languageManager == null )
		{

			// No language manager exists, return original value
			if( languageManagerChecked )
				return key;

			// Indicate that a search has already been performed 
			// for the active language manager
			languageManagerChecked = true;

			// Attempt to find a dfLanguageManager instance. If one
			// could not be found, return the original value.
			this.languageManager = GetManager().GetComponent<dfLanguageManager>();
			if( this.languageManager == null )
				return key;

		}

		// Return the localized version of the string
		return languageManager.GetValue( key );

	}

	[HideInInspector]
	protected internal void updateCollider()
	{

		var myCollider = collider as BoxCollider;
		if( myCollider == null )
		{

			if( collider != null )
				throw new Exception( "Invalid collider type on control: " + collider.GetType().Name );

			myCollider = this.gameObject.AddComponent<BoxCollider>();

		}

		if( Application.isPlaying && !this.isInteractive )
		{
			myCollider.enabled = false;
			return;
		}

		var p2u = PixelsToUnits();
		var sizeInUnits = size * p2u;

		var center = pivot.TransformToCenter( sizeInUnits );
		var colliderSize = new Vector3( sizeInUnits.x * hotZoneScale.x, sizeInUnits.y * hotZoneScale.y, 0.001f );
		var colliderEnabled = this.enabled && this.IsVisible;

		myCollider.isTrigger = false;
		myCollider.enabled = colliderEnabled;
		myCollider.size = colliderSize;
		myCollider.center = center;

	}

	/// <summary>
	/// Called by the <see cref="dfControl"/> class during rendering to 
	/// allow any derived classes to rebuild the <see cref="renderData"/>
	/// buffer.
	/// </summary>
	[HideInInspector]
	protected virtual void OnRebuildRenderData()
	{
		// This is just a placeholder function - It is intended to be overridden
	}

	/// <summary>
	/// Raises the ControlAdded event
	/// </summary>
	[HideInInspector]
	protected internal virtual void OnControlAdded( dfControl child )
	{

		Invalidate();

		if( ControlAdded != null )
		{
			ControlAdded( this, child );
		}

		Signal( "OnControlAdded", this, child );

	}

	/// <summary>
	/// Raises the ControlRemoved event
	/// </summary>
	[HideInInspector]
	protected internal virtual void OnControlRemoved( dfControl child )
	{

		Invalidate();

		if( ControlRemoved != null )
		{
			ControlRemoved( this, child );
		}

		Signal( "OnControlRemoved", this, child );

	}

	/// <summary>
	/// Raises the PositionChanged event
	/// </summary>
	[HideInInspector]
	protected internal virtual void OnPositionChanged()
	{

		// The dfControl's cached RenderData is still usable, 
		// just need to re-render with a new transpose Matrix 
		// (which will be set in dfControl.Render)
		updateVersion();
		GetManager().Invalidate();
		dfRenderGroup.InvalidateGroupForControl( this );

		// Cache the object's local position
		cachedPosition = transform.localPosition;

		// Add a kinematic rigidbody at runtime to make moving controls and 
		// updating the collider less expensive (in theory, not conclusive)
		if( this.isControlInitialized && Application.isPlaying && rigidbody == null )
		{

			// Too many active RigidBody components gives Windows Metro the falling fits
#if UNITY_EDITOR || !UNITY_METRO

			var rigidBody = this.gameObject.AddComponent<Rigidbody>();

			rigidBody.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
			rigidBody.isKinematic = true;
			rigidbody.useGravity = false;
			rigidBody.detectCollisions = false;

#endif

		}

		ResetLayout();

		if( PositionChanged != null )
		{
			PositionChanged( this, this.Position );
		}

	}

	/// <summary>
	/// Raises the SizeChanged event
	/// </summary>
	[HideInInspector]
	protected internal virtual void OnSizeChanged()
	{

		updateCollider();
		Invalidate();

		// If there is no layout being performed (checked in ResetLayout), it 
		// means that the Size was set independantly of layout either in code 
		// or by the developer, so reset the layout.
		ResetLayout();

		// If either "center" flag is set, need to perform centering
		if( Anchor.IsAnyFlagSet( dfAnchorStyle.CenterHorizontal | dfAnchorStyle.CenterVertical ) )
		{
			PerformLayout();
		}

		raiseSizeChangedEvent();

		for( int i = 0; i < controls.Count; i++ )
		{
			controls[ i ].PerformLayout();
		}

	}

	/// <summary>
	/// Raises the PivotChanged event
	/// </summary>
	[HideInInspector]
	protected internal virtual void OnPivotChanged()
	{

		Invalidate();

		if( Anchor.IsAnyFlagSet( dfAnchorStyle.CenterHorizontal | dfAnchorStyle.CenterVertical ) )
		{
			ResetLayout();
			PerformLayout();
		}

		if( PivotChanged != null )
		{
			PivotChanged( this, pivot );
		}

	}

	/// <summary>
	/// Raises the AnchorChanged event
	/// </summary>
	[HideInInspector]
	protected internal virtual void OnAnchorChanged()
	{

		ResetLayout();

		if( this.anchorStyle.IsAnyFlagSet( dfAnchorStyle.CenterHorizontal | dfAnchorStyle.CenterVertical ) )
		{
			PerformLayout();
		}

		if( AnchorChanged != null )
		{
			AnchorChanged( this, this.anchorStyle );
		}

		Invalidate();

	}

	[HideInInspector]
	protected internal virtual void OnOpacityChanged()
	{

		Invalidate();

		var newOpacity = this.Opacity;

		if( OpacityChanged != null )
		{
			OpacityChanged( this, newOpacity );
		}

		for( int i = 0; i < controls.Count; i++ )
		{
			controls[ i ].OnOpacityChanged();
		}

	}

	[HideInInspector]
	protected internal virtual void OnColorChanged()
	{

		Invalidate();

		var newColor = Color;

		if( ColorChanged != null )
		{
			ColorChanged( this, newColor );
		}

		for( int i = 0; i < controls.Count; i++ )
		{
			controls[ i ].OnColorChanged();
		}

	}

	[HideInInspector]
	protected internal virtual void OnZOrderChanged()
	{

		if( ZOrderChanged != null )
		{
			ZOrderChanged( this, this.zindex );
		}

		Invalidate();

	}

	[HideInInspector]
	protected internal virtual void OnTabIndexChanged()
	{

		Invalidate();

		if( TabIndexChanged != null )
		{
			TabIndexChanged( this, this.tabIndex );
		}

	}

	[HideInInspector]
	protected virtual void OnControlClippingChanged()
	{

		if( ControlClippingChanged != null )
		{
			ControlClippingChanged( this, isControlClipped );
		}

		Signal( "OnControlClippingChanged", this, isControlClipped );

	}

	[HideInInspector]
	protected internal virtual void OnIsVisibleChanged()
	{

		updateCollider();

		// Calculate visibility once to avoid duplication of 
		// processing (IsVisible walks up the control hierarchy).
		var visible = IsVisible;

		// If the control had focus and is now hidden, then no
		// control should have focus
		if( HasFocus && !visible )
		{
			dfGUIManager.SetFocus( null );
		}

		// Notify attached component scripts 
		Signal( "OnIsVisibleChanged", this, visible );

		// Raise the event for subscribers
		if( IsVisibleChanged != null )
		{
			IsVisibleChanged( this, visible );
		}

		// IsVisible affects the entire hierarchy below this control
		var children = controls.Items;
		var childCount = controls.Count;
		for( var i = 0; i < childCount; i++ )
		{
			children[ i ].OnIsVisibleChanged();
		}

		#region Support for event-bindable Shown and Hidden events 

		// Having specific events for Show and Hide operations allows
		// event bindings to fire only when one of the two states
		// becomes active

		if( visible )
		{
			if( ControlShown != null )
			{
				ControlShown( this, true );
			}
			Signal( "OnControlShown", this, true );
		}
		else
		{
			if( ControlHidden != null )
			{
				ControlHidden( this, true );
			}
			Signal( "OnControlHidden", this, false );
		}

		#endregion

		// Control hierarchy must be re-rendered
		Invalidate();

		// Automatically sets focus if the AutoFocus property is true
		if( visible )
		{
			doAutoFocus();
		}
		else if( !Application.isPlaying )
		{
			// Disable colliders for invisible controls during design time
			var boxCollider = collider as BoxCollider;
			boxCollider.size = Vector3.zero;
		}

	}

	[HideInInspector]
	protected internal virtual void OnIsEnabledChanged()
	{

		if( shutdownInProgress )
			return;

		var enabledProperty = this.IsEnabled && this.enabled;

		updateCollider();

		if( dfGUIManager.ContainsFocus( this ) && !enabledProperty )
		{
			dfGUIManager.SetFocus( null );
		}

		Invalidate();

		Signal( "OnIsEnabledChanged", this, enabledProperty );

		if( IsEnabledChanged != null )
		{
			IsEnabledChanged( this, enabledProperty );
		}

		for( int i = 0; i < controls.Count; i++ )
		{
			controls[ i ].OnIsEnabledChanged();
		}

		doAutoFocus();

	}

	/// <summary>
	/// Calculates the final opacity of this control, taking into account
	/// the opacity of all controls higher in the control hierarchy
	/// </summary>
	/// <returns>The final opacity that should be used to render this control</returns>
	protected internal float CalculateOpacity()
	{
		if( parent == null ) return this.Opacity;
		return this.Opacity * parent.CalculateOpacity();
	}

	/// <summary>
	/// Applies the results of <see cref="CalculateOpacity"/> to the given 
	/// <see cref="Color32"/> value. This is a convenience function used 
	/// to determine vector colors when rendering the control.
	/// </summary>
	protected internal Color32 ApplyOpacity( Color32 rawColor )
	{
		float opacity = CalculateOpacity();
		rawColor.a = (byte)( rawColor.a * opacity );
		return rawColor;
	}

	/// <summary>
	/// Returns the relative position in screen coordinates of the mouse cursor within
	/// the bounds of the control. Returns (Vector2.one * float.MinValue) if the mouse
	/// cursor does not fall within the bounds of the control.
	/// </summary>
	protected internal Vector2 GetHitPosition( dfMouseEventArgs args )
	{

		Vector2 result;
		GetHitPosition( args.Ray, out result );

		return result;

	}

	/// <summary>
	/// Transforms a direction vector and scales it to the same scale
	/// as the Manager which contains this control
	/// </summary>
	/// <param name="direction"></param>
	/// <returns></returns>
	protected internal Vector3 getScaledDirection( Vector3 direction )
	{

		var scale = GetManager().transform.localScale;

		direction = transform.TransformDirection( direction );

		return Vector3.Scale( direction, scale );

	}

	/// <summary>
	/// Transforms and scales a vector such that it will always
	/// represent the same fixed distance on-screen even when the 
	/// user interface is scaled
	/// </summary>
	/// <param name="offset"></param>
	/// <returns></returns>
	protected internal Vector3 transformOffset( Vector3 offset )
	{

		var x = offset.x * getScaledDirection( Vector3.right );
		var y = offset.y * getScaledDirection( Vector3.down );

		return ( x + y ) * PixelsToUnits();

	}

	/// <summary>
	/// This function is called by the GUI system when the screen resolution has changed.
	/// </summary>
	/// <param name="previousResolution">The previous screen resolution</param>
	/// <param name="currentResolution">The new screen resolution</param>
	protected internal virtual void OnResolutionChanged( Vector2 previousResolution, Vector2 currentResolution )
	{

		// OnResolutionChanged() happens very early in the startup process, and the 
		// execution order is indeterminate, so make sure that the control hierarchy 
		// is properly set up when this function is called.
		updateControlHierarchy();

		// Make sure that the control remains at the same relative position
		cachedPixelSize = 0f;
		var oldPos = transform.localPosition / ( 2f / previousResolution.y );
		cachedPosition = transform.localPosition = oldPos * ( 2f / currentResolution.y );

		// Ensure that the control's layout is correct
		const dfAnchorStyle flags = dfAnchorStyle.Proportional | dfAnchorStyle.CenterHorizontal | dfAnchorStyle.CenterVertical;
		if( Anchor.IsAnyFlagSet( flags ) )
		{
			PerformLayout();
		}

		// Make sure that the control's collider reflects the new position
		updateCollider();

		// Notify any third-party scripts that this control has changed due to 
		// the change in screen resolution
		Signal( "OnResolutionChanged", this, previousResolution, currentResolution );

		// Make sure that the control gets rendered on the next frame
		Invalidate();

	}

	#endregion

	#region Unity events 

#if UNITY_EDITOR

	[HideInInspector]
	public virtual void OnValidate()
	{

		// Fix for Unity crash issue on startup
		if( !isControlInitialized )
			return;

		try
		{
			// This only exists for design-time support of editing
			// multiple controls at the same time, and isn't part
			// of the regular operation of the control.
			Invalidate();
		}
		catch { }

	}

	[HideInInspector]
	public virtual void OnDrawGizmos()
	{

		var controlCollider = this.collider as BoxCollider;
		if( controlCollider == null )
			return;

		controlCollider.hideFlags = HideFlags.HideInInspector;

		if( !IsVisible || Opacity < MINIMUM_OPACITY )
			return;

		var controlCenter = pivot.TransformToCenter( Size ) * PixelsToUnits();
		var controlSize = ( (Vector3)this.Size ) * this.PixelsToUnits();

		Gizmos.matrix = Matrix4x4.TRS( transform.position, transform.rotation, transform.localScale );

		if( !UnityEditor.Selection.gameObjects.Contains( this.gameObject ) )
		{
			Gizmos.color = new UnityColor( 0, 0, 0, 0.175f );
			Gizmos.DrawWireCube( controlCenter, controlSize );
		}

		// Rendering a clear cube allows the user to click on the control
		// in the Unity Editor Scene Manager
		var thickness = new Vector3( 0, 0, 0.01f * ( renderOrder + 2 ) );
		Gizmos.color = UnityColor.clear;
		Gizmos.DrawCube( controlCenter, controlSize + thickness );

		// Render hot zone if set
		if( !Vector2.Equals( this.HotZoneScale, Vector2.one ) )
		{

			controlSize.x *= hotZoneScale.x;
			controlSize.y *= hotZoneScale.y;

			Gizmos.color = new UnityColor( 1, 1, 0, 0.175f );
			Gizmos.DrawWireCube( controlCenter, controlSize );

		}

	}

	[HideInInspector]
	public virtual void OnDrawGizmosSelected()
	{

		if( !UnityEditor.Selection.gameObjects.Contains( this.gameObject ) )
		{
			return;
		}

		if( !IsVisible )
			return;

		var controlCenter = pivot.TransformToCenter( Size ) * PixelsToUnits();
		var controlSize = ( (Vector3)this.Size ) * this.PixelsToUnits();

		// Rendering a clear cube allows the user to click on the control
		// in the Unity Editor Scene Manager
		Gizmos.matrix = Matrix4x4.TRS( transform.position, transform.rotation, transform.localScale );
		var thickness = new Vector3( 0, 0, 0.007f * ( renderOrder + 1 ) );
		Gizmos.color = UnityColor.clear;
		Gizmos.DrawCube( controlCenter, controlSize + thickness );

	}

#endif

	/// <summary>
	/// Awake is called by the Unity engine when the script instance is being loaded.
	/// </summary>
	[HideInInspector]
	public virtual void Awake()
	{

		// Some people have reported crashing the Unity Editor when adding
		// a dfControl component to a GameObject which already has a 
		// dfControl component attached. This is an invalid condition that
		// the user should not be doing, but we don't want the Editor 
		// to crash.
#if UNITY_EDITOR
		if( !Application.isPlaying )
		{
			var controlComponents = GetComponents<dfControl>();
			if( controlComponents.Length > 1 )
			{

				// NEW BEHAVIOR: Add a child control of the same type, since this is 
				// most likely what the user meant to do in the first place, even 
				// though they did it wrong :)
				var newControl = controlComponents[ 0 ].AddControl( this.GetType() );
				newControl.transform.position = this.transform.position;

				// Delete this control instance
				this.enabled = false;
				DestroyImmediate( this );

				// Select the new control instance in the inspector
				UnityEditor.Selection.activeGameObject = newControl.gameObject;

				// Do not proceed with initialization
				return;

			}
		}
#endif

		cachedParentTransform = this.transform.parent;

		// HACK: Since anchorStyle is now being serialized on its own, this 
		// block of code allows us to "migrate" the value from the persisted
		// layout to the new field. This may be removed eventually.
		if( this.anchorStyle == dfAnchorStyle.None && this.layout != null )
		{
			this.anchorStyle = this.layout.AnchorStyle;
		}

		// This component requires a BoxCollider component, but using the 
		// RequireComponent() attribute causes the Unity Editor to crash
		// in certain situations. Because of this, a new BoxCollider 
		// component will be added "on demand" if one does not already
		// exist on the GameObject.
		if( this.collider == null )
		{
			gameObject.AddComponent<BoxCollider>();
		}

	}

	/// <summary>
	/// Start is called by the Unity engine before any of the <see cref="Update"/> 
	/// methods is called for the first time
	/// </summary>
	[HideInInspector]
	public virtual void Start()
	{
		// Intended to be overridden
	}

	/// <summary>
	/// This function is called by the Unity engine when the object becomes enabled and current.
	/// </summary>
	[HideInInspector]
	public virtual void OnEnable()
	{

		// Set all cached variables to default values
		this.cachedManager = null;
		this.cachedBounds = null;
		this.cachedChildCount = 0;
		this.cachedParentTransform = null;
		this.cachedPosition = Vector3.zero;
		this.cachedRelativePosition = Vector3.zero;
		this.cachedRotation = Quaternion.identity;
		this.cachedScale = Vector3.one;

		// Keep track of all active dfControl instances
		ActiveInstances.Add( this );

		// Perform control initialization
		initializeControl();

		// Localize controls at startup
		if( Application.isPlaying && this.IsLocalized )
		{
			Localize();
		}

		// Notify observers that the control is now enabled
		OnIsEnabledChanged();

	}

	/// <summary>
	/// Sent to all game objects by the Unity engine before the application is quit.
	/// </summary>
	[HideInInspector]
	public virtual void OnApplicationQuit()
	{
		shutdownInProgress = true;
		RemoveAllEventHandlers();
	}

	/// <summary>
	/// This function is called by the Unity engine when the control becomes 
	/// disabled or inactive.
	/// </summary>
	[HideInInspector]
	public virtual void OnDisable()
	{

		// Keep track of all active dfControl instances
		ActiveInstances.Remove( this );

		// NOTE: Try..Catch was added to work around an uncommon and as far as I could
		// discover a non-deterministic Unity issue in web builds. 
		try
		{

			Invalidate();

			if( dfGUIManager.HasFocus( this ) )
			{
				dfGUIManager.SetFocus( null );
			}
			else if( dfGUIManager.GetModalControl() == this )
			{
				dfGUIManager.PopModal();
			}

			OnIsEnabledChanged();

		}
		catch
		{
		}
		finally
		{
			isControlInitialized = false;
		}
			
	}

	/// <summary>
	/// This function is called by the Unity engine when the control will be destroyed.
	/// </summary>
	[HideInInspector]
	public virtual void OnDestroy()
	{

		isDisposing = true;
		isControlInitialized = false;

		// Detach all event handlers
		if( Application.isPlaying )
		{
			RemoveAllEventHandlers();
		}

		// Do not allow modal controls to stay on the stack when destroyed
		if( dfGUIManager.GetModalControl() == this )
		{
			dfGUIManager.PopModal();
		}
		
		// Make sure that the layout component does not hold on to references
		if( layout != null )
		{
			layout.Dispose();
		}

		// Make sure that the parent control no longer holds a reference to this control
		if( parent != null && parent.controls != null && !parent.isDisposing )
		{
			if( parent.controls.Remove( this ) )
			{
				parent.cachedChildCount -= 1;
				parent.OnControlRemoved( this );
			}
		}

		// Make sure that child controls no longer hold a reference to this control
		for( var i = 0; i < controls.Count; i++ )
		{

			if( controls[ i ].layout != null )
			{
				controls[ i ].layout.Dispose();
				controls[ i ].layout = null;
			}

			controls[ i ].parent = null;

		}

		// Make sure that this control no longer holds references to any child controls
		controls.Release();

		// Let the GUIManager instance responsible for rendering this control
		// know that it needs to refresh
		if( cachedManager != null )
		{
			cachedManager.Invalidate();
		}

		// Make sure the control does not hold on to cached render data
		if( this.renderData != null )
		{
			this.renderData.Release();
		}

		// Clear references
		layout = null;
		cachedManager = null;
		parent = null;
		cachedClippingPlanes = null;
		cachedCorners = null;
		renderData = null;
		controls = null;

	}

	/// <summary>
	/// Called by the Unity engine every frame (after <see cref="Update"/>) if
	/// the control component is enabled
	/// </summary>
	[HideInInspector]
	public virtual void LateUpdate()
	{

		// Make sure that pending layout requests get processed. There might be
		// pending layout requests if a control's PerformLayout() method was 
		// called while layout was suspended using SuspendLayout/ResumeLayout
		if( layout != null && layout.HasPendingLayoutRequest )
		{
			layout.PerformLayout();
		}

	}

	/// <summary>
	/// Called by the Unity engine every frame if the control component is enabled
	/// </summary>
	[HideInInspector]
	public virtual void Update()
	{

		// Make sure that the control is properly initialized
		if( !isControlInitialized )
			initializeControl();

		// Cache the .transform property to avoid repeated calls to the
		// property get method
		var cachedTransform = this.transform;

		// Layouts are invalid when controls are reparented
		if( cachedTransform.parent != cachedParentTransform )
		{

			// Control might have been assigned to another dfGUIManager
			cachedManager = null;
			GetManager();

			cachedParentTransform = cachedTransform.parent;
			ResetLayout( false, true );

		}

		// Maintain the control hierarchy - Keeps track of when child Controls are 
		// added to or removed from this dfControl.
		updateControlHierarchy();

		// If the transform has been changed this dfControl needs to determine whether
		// the changes are already reflected in the Position and Size properties and
		// act accordingly if they are not
		if( cachedTransform.hasChanged )
		{

			// Control bounds are no longer valid
			cachedBounds = null;

			// If the control's scale has changed, need to rebuild render data
			if( cachedScale != cachedTransform.localScale )
			{
				cachedScale = cachedTransform.localScale;
				Invalidate();
			}

			// If the developer has moved the control using the on-screen translate
			// widget in the editor or the control has been moved via script without
			// using the built-in Position/RelativePosition methods, keep track of
			// the new position and raise the PositionChanged event.
			// To paraphrase a song, I just dropped in to see what condition my position is in.
			if( Vector3.Distance( cachedPosition, cachedTransform.localPosition ) > float.Epsilon )
			{

				// Keep track of "last known" position so that the OnPositionChanged event
				// can be fired when the user moves the control through either the Position
				// property in the Inspector, via Transform.position, or via gizmos in the scene.
				cachedPosition = cachedTransform.localPosition;

				// Notify any observers that the control has been moved
				OnPositionChanged();

			}

			// If the control was rotated, notify any observers
			if( cachedRotation != cachedTransform.localRotation )
			{
				cachedRotation = cachedTransform.localRotation;
				Invalidate();
			}

			// Clear the changed flag
			cachedTransform.hasChanged = false;

		}

	}

	#endregion

	#region Manage control hierarchy 

	protected internal void SetControlIndex( dfControl child, int zorder )
	{

		// Backward compatibiity - In previous versions, -1 meant "make topmost"
		if( zorder < 0 )
			zorder = int.MaxValue;

		controls.Sort();
		controls.Remove( child );

		if( zorder >= controls.Count )
			controls.Add( child );
		else
			controls.Insert( zorder, child );

		child.zindex = zorder;

		for( int i = 0; i < controls.Count; i++ )
		{
			if( controls[ i ].zindex != i )
			{
				var changed = controls[ i ];
				changed.zindex = i;
				changed.OnZOrderChanged();
			}
		}

	}

	/// <summary>
	/// Creates a new <see cref="dfControl"/> instance of the specified
	/// type and adds it as a child of this instance
	/// </summary>
	/// <typeparam name="T">The type of control to be created</typeparam>
	/// <returns>A reference to the newly-created control instance</returns>
	public T AddControl<T>() where T : dfControl
	{
		return (T)AddControl( typeof( T ) );
	}

	/// <summary>
	/// Creates a new <see cref="dfControl"/> instance of the specified
	/// type and adds it as a child of this instance
	/// </summary>
	/// <param name="controlType">The type of control to be created</param>
	/// <returns>A reference to the newly-created control instance</returns>
	public dfControl AddControl( Type controlType )
	{

#if !UNITY_EDITOR && UNITY_METRO 
		var isControlType = typeof( dfControl ).GetTypeInfo().IsAssignableFrom( controlType.GetTypeInfo() );
#else
		var isControlType = typeof( dfControl ).IsAssignableFrom( controlType );
#endif

		if( !isControlType )
		{
			throw new System.InvalidCastException( string.Format( "Type {0} does not inherit from {1}", controlType.Name, typeof( dfControl ).Name ) );
		}

		var go = new GameObject( controlType.Name );
		go.transform.parent = this.transform;
		go.layer = this.gameObject.layer;

		var position = Size * PixelsToUnits() * 0.5f;
		go.transform.localPosition = new Vector3( position.x, position.y, 0 );

		var child = go.AddComponent( controlType ) as dfControl;
		child.parent = this;
		child.cachedManager = this.cachedManager;
		child.zindex = -1;

		AddControl( child );

		return child;

	}

	/// <summary>
	/// Adds the child control to the list of child controls for this instance
	/// </summary>
	/// <param name="child">The <see cref="dfControl"/> instance to add to the list of child controls</param>
	public void AddControl( dfControl child )
	{

		// Cannot just call AddControl( new dfControl() ), use AddControl<Type>() instead.
		if( child.transform == null )
		{
			throw new NullReferenceException( "The child control does not have a Transform" );
		}

		// Edge case - Some object pooling solutions can create a situation where the parent
		// pointer is not cleaned up properly. 
		if( child.parent != null && child.parent != this )
		{
			child.parent.RemoveControl( child );
		}

		// Add the control to the hierarchy if not already included
		if( !controls.Contains( child ) )
		{
			controls.Add( child );
			child.parent = this;
			child.transform.parent = this.transform;
			child.cachedManager = this.cachedManager;
			child.cachedParentTransform = this.transform;
		}
			
		// A ZOrder value of -1 means "make it the topmost control"
		// If this is not what is desired, the caller must specify
		// the custom ZOrder afterwards.
		if( child.zindex == -1 || child.zindex == int.MaxValue )
		{
			// Auto-increment the control's ZOrder
			SetControlIndex( child, int.MaxValue );
		}
		else 
		{
			// The list of child controls must always remain sorted.
			// NOTE: No attempt is made to fill "gaps" in the ZOrder
			// of the controls collection, since doing so at startup
			// will result in ZOrder changes due to non-deterministic
			// control activation order.
			controls.Sort();
		}

		// Notify all observers that a control has been added
		OnControlAdded( child );

		// Controls need to update their version number (and therefore clipping
		// data) and redraw themselves after changing relationships
		child.Invalidate();

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
		child.parent = this;
		child.zindex = -1;

		AddControl( child );

		return child;

	}

	/// <summary>
	/// Returns the highest value of the ZOrder property of this control's child controls
	/// </summary>
	private int getMaxZOrder()
	{

		var max = -1;
		for( int i = 0; i < controls.Count; i++ )
		{
			max = Mathf.Max( controls[ i ].zindex, max );
		}

		return max;

	}

	/// <summary>
	/// Removes the indicated control from the list of child controls for this instance
	/// </summary>
	/// <param name="child">The control to remove from the list of child controls</param>
	public void RemoveControl( dfControl child )
	{

		if( isDisposing )
			return;

		if( child.Parent == this )
			child.parent = null;

		if( controls.Remove( child ) )
		{

			// Notify all interested observers
			OnControlRemoved( child );

			// Controls need to update their version number (and therefore clipping
			// data) and redraw themselves after changing relationships
			child.Invalidate();
			this.Invalidate();

		}

	}

	/// <summary>
	/// Removes any gaps in the ZOrder of child controls and 
	/// sorts the Controls collection according to ZOrder
	/// </summary>
	[HideInInspector]
	public void RebuildControlOrder()
	{

		controls.Sort();
		for( int i = 0; i < controls.Count; i++ )
		{
			if( controls[ i ].zindex != i )
			{
				var child = controls[ i ];
				child.zindex = i;
				child.OnZOrderChanged();
			}
		}

	}

	#endregion

	#region Private utility methods

	// @private
	internal void setClippingState( bool isClipped )
	{
		
		if( isClipped == isControlClipped )
			return;

		this.isControlClipped = isClipped;
		OnControlClippingChanged();

	}

	private void doAutoFocus()
	{

		var doAutoFocus =
			Application.isPlaying &&
			this.IsEnabled &&
			this.enabled &&
			this.AutoFocus &&
			this.CanFocus &&
			this.IsVisible &&
			gameObject.activeSelf &&
			gameObject.activeInHierarchy;

		if( doAutoFocus )
		{
			// Need to focus on the *next* frame to allow all startup actions 
			// to be completed first
			StartCoroutine( focusOnNextFrame() );
		}

	}

	private IEnumerator focusOnNextFrame()
	{
		yield return null;
		this.Focus();
	}

	protected void raiseSizeChangedEvent()
	{

		if( SizeChanged != null )
		{
			SizeChanged( this, Size );
		}

	}

	protected void raiseMouseDownEvent( dfMouseEventArgs args )
	{
		if( MouseDown != null )
		{
			MouseDown( this, args );
		}
	}

	protected void raiseMouseMoveEvent( dfMouseEventArgs args )
	{
		if( MouseMove != null )
		{
			MouseMove( this, args );
		}
	}

	protected void raiseMouseWheelEvent( dfMouseEventArgs args )
	{
		if( MouseWheel != null )
		{
			MouseWheel( this, args );
		}
	}

	private void initializeControl()
	{

		// Cache the transform's parent since it is referenced so frequently in this function
		var transformParent = transform.parent;

		// If control is newly instantiated (or currently being activated in an object pooling scenario)
		// and is not yet part of a UI hierarchy, do not continue with control initialization
		if( transformParent == null || transformParent.GetComponent( typeof( IDFControlHost ) ) == null )
			return;

		if( transformParent != null || cachedParentTransform != transformParent )
		{

			var parentControl = transformParent.GetComponent<dfControl>();
			if( parentControl != null )
			{
				this.parent = parentControl;
				parentControl.AddControl( this );
			}

			if( this.controls == null )
			{
				updateControlHierarchy();
			}

		}

		// If RenderOrder has not yet been set, then default to the control's ZOrder
		if( renderOrder == -1 )
		{
			renderOrder = ZOrder;
		}

		// Make sure that the collider matches the desired size
		updateCollider();

		// Make sure that the layout has all of the information it needs
		ensureLayoutExists();
		layout.Attach( this );

		// Design-time support - workaround for prefab layout issues
		if( !Application.isPlaying )
		{
			PerformLayout();
		}

		// Control needs to be rendered on startup
		Invalidate();

		// Indicate that the control initialization has completed successfully
		isControlInitialized = true;

	}

	/// <summary>
	/// Compensates for Unity3D's lack of a consistent order of startup events
	/// and lack of hierarchy change notifications by manually tracking changes 
	/// to the this.gameObject's hierarchy and updating the Controls collection to match.
	/// </summary>
	internal void updateControlHierarchy()
	{
		updateControlHierarchy( false );	
	}

	/// <summary>
	/// Compensates for Unity3D's lack of a consistent order of startup events
	/// and lack of hierarchy change notifications by manually tracking changes 
	/// to the this.gameObject's hierarchy and updating the Controls collection to match.
	/// </summary>
	internal void updateControlHierarchy( bool force )
	{

		// Assume that the control hierarchy is correct if Transform.childCount
		// matches the number of controls in our collection
		var transformChildCount = transform.childCount;
		if( !force && transformChildCount == cachedChildCount )
		{
			return;
		}

		// Keep track of the number of nodes attached to the dfControl's Transform
		cachedChildCount = transformChildCount;

		var childControls = getChildControls();

		for( int i = 0; i < childControls.Count; i++ )
		{

			var child = childControls[ i ];
			if( controls.Contains( child ) )
				continue;

			// Add the dfControl to the hierarchy
			child.parent = this;
			child.cachedParentTransform = this.transform;

			// TODO: Do controls need a new layout when parented to another dfControl?
			if( !Application.isPlaying )
			{
				child.ResetLayout();
			}

			// Notify any observers that the control was added
			OnControlAdded( child );
			child.updateControlHierarchy();

		}

		// Determine whether any controls have been deleted without performing 
		// cleanup (such as removing itself from the parent's Controls list). 
		// This can happen when the child control is deleted in the Unity Editor,
		// which does not seem to fire the OnDestroy function while in Edit Mode
		for( int i = 0; i < controls.Count; i++ )
		{

			var child = controls[ i ];
			if( child == null || !childControls.Contains( child ) )
			{

				// Notify any observers that this dfControl's hierarchy has
				// changed. Observers will need to check for NULL, but 
				// unfortunately there's no way to know which control was 
				// removed at this point.
				OnControlRemoved( child );

				// If the control was reparented in the Editor Scene Hierarchy
				// it will probably still have an invalid Parent reference to 
				// this control. 
				if( child != null && child.parent == this )
				{
					child.parent = null;
				}

			}

		}

		// Start using the new list
		this.controls.Release();
		this.controls = childControls;

		// Always keep the Controls sorted by ZOrder
		RebuildControlOrder();

	}

	/// <summary>
	/// Iterates the Transform's children looking for dfControl components
	/// </summary>
	/// <returns></returns>
	private dfList<dfControl> getChildControls()
	{

		var childCount = transform.childCount;

		var result = dfList<dfControl>.Obtain( childCount );

		for( int i = 0; i < childCount; i++ )
		{
				
			var childTransform = transform.GetChild( i );
			if( !childTransform.gameObject.activeSelf )
				continue;

			var control = childTransform.GetComponent<dfControl>();
			if( control != null )
			{
				result.Add( control );
			}

		}

		return result;

	}

	private void ensureLayoutExists()
	{

		// Create a new layout, if needed
		if( layout == null )
		{
			var anchor = ( this.anchorStyle != dfAnchorStyle.None ) ? this.anchorStyle : dfAnchorStyle.Left | dfAnchorStyle.Top;
			layout = new AnchorLayout( anchor, this );
			this.anchorStyle = anchor;
		}
		else
		{
			layout.Attach( this );
		}

		// Make sure all child controls also have a layout
		for( int i = 0; Controls != null && i < Controls.Count; i++ )
		{
			if( controls[ i ] != null )
			{
				controls[ i ].ensureLayoutExists();
			}
		}

	}

	protected internal void updateVersion()
	{
		this.version = ++versionCounter;
	}

	private Vector3 getRelativePosition()
	{

		if( relativePositionCacheVersion == this.version )
			return cachedRelativePosition;

		relativePositionCacheVersion = this.version;

		if( transform.parent == null )
		{
			return Vector3.zero;
		}

		if( parent != null )
		{

			var p2u = PixelsToUnits();
			var parentPos = transform.parent.position;
			var controlPos = transform.position;

			var relativeTransform = transform.parent;

			var relativeParentPos = relativeTransform.InverseTransformPoint( parentPos / p2u );
			relativeParentPos += parent.pivot.TransformToUpperLeft( parent.size );

			var relativeControlPos = relativeTransform.InverseTransformPoint( controlPos / p2u );
			relativeControlPos += this.pivot.TransformToUpperLeft( this.size );

			var relativePos = relativeControlPos - relativeParentPos;

			return cachedRelativePosition = relativePos.Scale( 1, -1, 1 );

		}

		// Need a reference to the parent Manager in order to proceed
		var view = GetManager();
		if( view == null )
		{
			Debug.LogError( "Cannot get position: View not found" );
			relativePositionCacheVersion = uint.MaxValue;
			return Vector3.zero;
		}

		// Calculate the upper left corner in world-space coordinates
		var pixelSize = PixelsToUnits();
		var upperLeft = transform.position + pivot.TransformToUpperLeft( size ) * pixelSize;

		// The Manager's clipping planes are ordered left, right, top, bottom
		var planes = view.GetClippingPlanes();
		var x = planes[ 0 ].GetDistanceToPoint( upperLeft ) / pixelSize;
		var y = planes[ 3 ].GetDistanceToPoint( upperLeft ) / pixelSize;

		// Return the relative distance from the left and top frustum planes
		cachedRelativePosition = new Vector3( x, y ).RoundToInt();

		return cachedRelativePosition;

	}

	private void setPositionInternal( Vector3 value )
	{

		value += pivot.UpperLeftToTransform( Size );
		value *= PixelsToUnits();

		if( Vector3.Distance( value, cachedPosition ) <= float.Epsilon )
			return;

		cachedPosition = transform.localPosition = value;

		OnPositionChanged();

	}

	private void setRelativePosition( ref Vector3 value )
	{

		if( transform.parent == null )
		{
			Debug.LogError( "Cannot set relative position without a parent Transform." );
			return;
		}

		if( ( value - getRelativePosition() ).magnitude <= float.Epsilon )
			return;

		if( parent != null )
		{

			var newPos =
				value.Scale( 1, -1, 1 )
				+ pivot.UpperLeftToTransform( this.size )
				- parent.pivot.UpperLeftToTransform( parent.size )
				;

			newPos *= PixelsToUnits();

			if( ( newPos - transform.localPosition ).sqrMagnitude >= float.Epsilon )
			{

				transform.localPosition = newPos;
				cachedPosition = newPos;

				OnPositionChanged();

			}

			return;

		}

		// Need a reference to the parent Manager in order to proceed
		var view = GetManager();
		if( view == null )
		{
			Debug.LogError( "Cannot get position: View not found" );
			return;
		}

		// The Manager's corners are ordered TopLeft, TopRight, BottomRight, BottomLeft
		var corners = view.GetCorners();
		var screenUpperLeft = corners[ 0 ];

		// Convert value to world coordinates
		var pixelSize = PixelsToUnits(); 
		value = value.Scale( 1, -1, 1 ) * pixelSize;

		// Calculate offset needed to translate from upper left to transform
		var offset = pivot.UpperLeftToTransform( Size ) * pixelSize;

		// Calculate new position 
		var newPosition =
			screenUpperLeft +
			view.transform.TransformDirection( value ) +
			offset;

		// No need to fire the OnPositionChanged event if the position has not
		// actually changed significantly
		if( Vector3.Distance( newPosition, cachedPosition ) > float.Epsilon )
		{

			// Assign the transform position and keep a cached copy
			transform.position = newPosition;
			cachedPosition = transform.localPosition;

			// Notify any observers that the control's position has changed
			OnPositionChanged();

		}

	}

	private static Vector3 closestPointOnLine( Vector3 start, Vector3 end, Vector3 test, bool clamp )
	{

		// http://www.gamedev.net/community/forums/topic.asp?topic_id=198199&whichpage=1&#1250842

		Vector3 c = test - start;				// Vector from a to Point
		Vector3 v = ( end - start ).normalized;	// Unit Vector from a to b
		float d = ( end - start ).magnitude;	// Length of the line segment
		float t = Vector3.Dot( v, c );			// Intersection point Distance from a

		// Check to see if the point is on the line
		// if not then return the endpoint
		if( clamp )
		{
			if( t < 0 ) return start;
			if( t > d ) return end;
		}

		// get the distance to move from point a
		v *= t;

		// move from point a to the nearest point on the segment
		return start + v;

	}

	#endregion

	#region IComparable<dfControl> Members

	/// <summary>
	/// Used to compare <see cref="dfControl"/> instances in order to 
	/// sort them according to <see cref="ZOrder"/> value.
	/// </summary>
	/// <param name="other">The other <see cref="dfControl"/> instance to compare against</param>
	/// <returns>
	/// A signed number indicating the relative values of this instance and value: 
	/// Less than zero if this instance has a lower ZOrder value than <paramref name="other"/>, 
	/// greater than zero if this instance has a higher ZOrder value than <paramref name="other"/>,
	/// and zero if both instances have the same ZOrder value
	/// </returns>
	public int CompareTo( dfControl other )
	{

		// Sort dfControl instances with a negative ZOrder *after* controls
		// with already-assigned ZOrder values
		if( this.ZOrder < 0 )
		{
			return other.ZOrder < 0 ? 0 : 1;
		}

		return ZOrder.CompareTo( other.ZOrder );

	}

	#endregion

	#region Private nested classes 

	protected class SignalCache
	{

		private static readonly List<SignalCacheItem> cache = new List<SignalCacheItem>();

		public static bool Invoke( Component target, string eventName, object[] arguments, out object returnValue )
		{

			returnValue = null;

			if( target == null )
				return false;

			var componentType = target.GetType();

			var item = getItem( componentType, eventName );
			if( item == null )
			{

				var paramTypes = new Type[ arguments.Length ];
				for( int i = 0; i < paramTypes.Length; i++ )
				{
					if( arguments[ i ] == null )
					{
						paramTypes[ i ] = typeof( object );
					}
					else
					{
						paramTypes[ i ] = arguments[ i ].GetType();
					}
				}

				item = new SignalCacheItem( componentType, eventName, paramTypes );
				cache.Add( item );

			}

			return item.Invoke( target, arguments, out returnValue );

		}

		private static SignalCacheItem getItem( Type componentType, string eventName )
		{

			for( int i = 0; i < cache.Count; i++ )
			{

				var item = cache[ i ];

				if( item.ComponentType == componentType && item.EventName == eventName )
					return item;

			}

			return null;

		}

		#region Nested classes

		private class SignalCacheItem
		{

			public readonly Type ComponentType;
			public readonly string EventName;

			private readonly MethodInfo method;
			private readonly bool usesParameters;

			public SignalCacheItem( Type componentType, string eventName, Type[] paramTypes )
			{

				this.ComponentType = componentType;
				this.EventName = eventName;

				#region First try to find a MethodInfo with the exact signature

				var handlerWithParams = getMethod( componentType, eventName, paramTypes );
				if( handlerWithParams != null )
				{
					method = handlerWithParams;
					usesParameters = true;
					return;
				}

				#endregion

				#region Look for a parameterless method with the given name

				this.method = getMethod( componentType, eventName, dfReflectionExtensions.EmptyTypes );

				usesParameters = false;

				#endregion

			}

			public bool Invoke( object target, object[] arguments, out object returnValue )
			{

				if( method == null )
				{
					returnValue = null;
					return false;
				}

				if( !usesParameters )
					arguments = null;

				returnValue = method.Invoke( target, arguments );

				return true;

			}

		}

		#endregion

		#region Private methods

		private static MethodInfo getMethod( System.Type type, string name, System.Type[] paramTypes )
		{

			// NOTE: There is a bug in Unity 4.3.3+ on Windows Phone that causes all reflection 
			// method overloads that take a BindingFlags parameter to throw a runtime exception.
			// This means that we cannot have 100% compatibility between Unity 4.3.3 and prior
			// versions on the Windows Phone platform, and that some functionality 
			// will unfortunately be lost.

#if UNITY_EDITOR || !UNITY_WP8
			
			var method = type.GetMethod(
				name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				paramTypes,
				null
			);

			return method;

#else

			var methods = type.GetMethods();
			for( int i = 0; i < methods.Length; i++ )
			{

				var info = methods[ i ];
				if( info.IsStatic || info.Name != name )
					continue;

				if( matchesParameterTypes( info, paramTypes ) )
					return info;

			}

			return null;

#endif

		}

		private static bool matchesParameterTypes( MethodInfo method, Type[] types )
		{

			var parameters = method.GetParameters();
			if( parameters.Length != types.Length )
				return false;

			for( var i = 0; i < types.Length; i++ )
			{
				if( !parameters[ i ].ParameterType.IsAssignableFrom( types[ i ] ) )
					return false;
			}

			return true;

		}

		#endregion

	}

	/// <summary>
	/// Implements basic Anchor Layout - Allows the control to "anchor" each edge
	/// to the corresponding edge of its container such that the control resizes 
	/// properly when its container is resized.
	/// @internal
	/// </summary>
	[Serializable]
	protected class AnchorLayout
	{

		#region Protected serialized fields

		[SerializeField]
		protected dfAnchorStyle anchorStyle;

		[SerializeField]
		protected dfAnchorMargins margins;

		[SerializeField]
		protected dfControl owner;

		#endregion

		#region Private fields

		private int suspendLayoutCounter;
		private bool performingLayout;
		private bool disposed;
		private bool pendingLayoutRequest;

		#endregion

		#region Constructor

		internal AnchorLayout( dfAnchorStyle anchorStyle )
		{
			this.anchorStyle = anchorStyle;
		}

		internal AnchorLayout( dfAnchorStyle anchorStyle, dfControl owner )
			: this( anchorStyle )
		{
			Attach( owner );
			Reset();
		}

		#endregion

		#region Public properties

		internal dfAnchorStyle AnchorStyle
		{
			get { return this.anchorStyle; }
			set
			{
				if( value != this.anchorStyle )
				{
					this.anchorStyle = value;
					Reset();
				}
			}
		}

		internal bool IsPerformingLayout
		{
			get { return performingLayout; }
		}

		internal bool IsLayoutSuspended
		{
			get { return suspendLayoutCounter > 0; }
		}

		internal bool HasPendingLayoutRequest
		{
			get { return pendingLayoutRequest; }
		}

		#endregion

		#region Public methods

		internal void Dispose()
		{

			if( !disposed )
			{
				disposed = true;
				owner = null;
			}

		}

		internal void SuspendLayout()
		{
			suspendLayoutCounter += 1;
		}

		internal void ResumeLayout()
		{

			var wasSuspended = suspendLayoutCounter > 0;
			suspendLayoutCounter = Mathf.Max( 0, suspendLayoutCounter - 1 );
			if( wasSuspended && suspendLayoutCounter == 0 && pendingLayoutRequest )
			{
				PerformLayout();
			}

		}

		internal void PerformLayout()
		{

			if( disposed )
				return;

			if( suspendLayoutCounter > 0 )
			{
				pendingLayoutRequest = true;
			}
			else
			{
				performLayoutInternal();
			}

		}

		internal void Attach( dfControl ownerControl )
		{
			this.owner = ownerControl;
			if( ownerControl != null )
			{
				this.anchorStyle = ownerControl.anchorStyle;
			}
		}

		internal void Reset()
		{
			Reset( false );
		}

		internal void Reset( bool force )
		{

			if( owner == null || owner.transform.parent == null || anchorStyle == dfAnchorStyle.None )
				return;

			var layoutInProgress = !force && ( IsPerformingLayout || IsLayoutSuspended );

			var cannotReset =
				layoutInProgress ||
				owner == null ||
				!owner.gameObject.activeSelf;

			if( cannotReset )
			{
				return;
			}

			if( anchorStyle.IsFlagSet( dfAnchorStyle.Proportional ) )
				resetLayoutProportional();
			else
				resetLayoutAbsolute();

		}

		private void resetLayoutProportional()
		{

			var upperLeft = owner.RelativePosition;
			var controlSize = owner.Size;
			var parentSize = getParentSize();

			var left = upperLeft.x;
			var top = upperLeft.y;
			var right = left + controlSize.x;
			var bottom = top + controlSize.y;

			if( margins == null ) margins = new dfAnchorMargins();

			margins.left = left / parentSize.x;
			margins.right = right / parentSize.x;
			margins.top = top / parentSize.y;
			margins.bottom = bottom / parentSize.y;

		}

		private void resetLayoutAbsolute()
		{

			var upperLeft = owner.RelativePosition;
			var controlSize = owner.Size;
			var parentSize = getParentSize();

			var left = upperLeft.x;
			var top = upperLeft.y;
			var right = parentSize.x - controlSize.x - left;
			var bottom = parentSize.y - controlSize.y - top;

			if( margins == null ) margins = new dfAnchorMargins();

			margins.left = left;
			margins.right = right;
			margins.top = top;
			margins.bottom = bottom;

		}

		#endregion

		#region Private utility methods

		protected void performLayoutInternal()
		{

			if( anchorStyle == dfAnchorStyle.None )
				return;

			var hierarchyUpdateNeeded =
				owner == null ||
				owner.transform.parent == null;

			if( hierarchyUpdateNeeded )
			{
				pendingLayoutRequest = true;
				return;
			}

			var cannotPerformLayout =
				margins == null ||
				IsPerformingLayout ||
				IsLayoutSuspended ||
				!owner.gameObject.activeSelf;

			if( cannotPerformLayout )
				return;

			try
			{

				performingLayout = true;
				pendingLayoutRequest = false;

				var parentSize = getParentSize();
				var controlSize = owner.Size;

				if( anchorStyle.IsFlagSet( dfAnchorStyle.Proportional ) )
					performLayoutProportional( parentSize, controlSize );
				else
					performLayoutAbsolute( parentSize, controlSize );

			}
			finally
			{
				performingLayout = false;
			}

		}

		private void performLayoutProportional( Vector2 parentSize, Vector2 controlSize )
		{

			var left = ( margins.left * parentSize.x );
			var right = ( margins.right * parentSize.x );
			var top = ( margins.top * parentSize.y );
			var bottom = ( margins.bottom * parentSize.y );

			var newPosition = owner.RelativePosition;
			var newSize = controlSize;

			if( anchorStyle.IsFlagSet( dfAnchorStyle.Left ) )
			{

				newPosition.x = left;

				if( anchorStyle.IsFlagSet( dfAnchorStyle.Right ) )
				{
					newSize.x = ( margins.right - margins.left ) * parentSize.x;
				}

			}
			else if( anchorStyle.IsFlagSet( dfAnchorStyle.Right ) )
			{
				newPosition.x = right - controlSize.x;
			}
			else if( anchorStyle.IsFlagSet( dfAnchorStyle.CenterHorizontal ) )
			{
				newPosition.x = ( parentSize.x - controlSize.x ) * 0.5f;
			}

			if( anchorStyle.IsFlagSet( dfAnchorStyle.Top ) )
			{

				newPosition.y = top;

				if( anchorStyle.IsFlagSet( dfAnchorStyle.Bottom ) )
				{
					newSize.y = ( margins.bottom - margins.top ) * parentSize.y;
				}

			}
			else if( anchorStyle.IsFlagSet( dfAnchorStyle.Bottom ) )
			{
				newPosition.y = bottom - controlSize.y;
			}
			else if( anchorStyle.IsFlagSet( dfAnchorStyle.CenterVertical ) )
			{
				newPosition.y = ( parentSize.y - controlSize.y ) * 0.5f;
			}

			// NOTE: It is very important to set Size before setting Position,
			// since the Position property relies on the value of the Size
			// property when calculating the transform position based on the 
			// current value of the Pivot property
			owner.Size = newSize;
			owner.RelativePosition = newPosition;

			// Proportional resizing has a very high likelihood of resulting
			// in positions and dimensions that are fractional pixel sizes,
			// which looks atrocious in pixel perfect mode. 
			var manager = owner.GetManager();
			if( manager != null && manager.PixelPerfectMode )
			{
				owner.MakePixelPerfect( false );
			}

		}

		private void performLayoutAbsolute( Vector2 parentSize, Vector2 controlSize )
		{

			var left = margins.left;
			var top = margins.top;
			var right = left + controlSize.x;
			var bottom = top + controlSize.y;

			if( anchorStyle.IsFlagSet( dfAnchorStyle.CenterHorizontal ) )
			{
				left = Mathf.RoundToInt( ( parentSize.x - controlSize.x ) * 0.5f );
				right = Mathf.RoundToInt( left + controlSize.x );
			}
			else
			{

				if( anchorStyle.IsFlagSet( dfAnchorStyle.Left ) )
				{
					left = margins.left;
					right = left + controlSize.x;
				}

				if( anchorStyle.IsFlagSet( dfAnchorStyle.Right ) )
				{
					right = ( parentSize.x - margins.right );
					if( !anchorStyle.IsFlagSet( dfAnchorStyle.Left ) )
					{
						left = right - controlSize.x;
					}
				}

			}

			if( anchorStyle.IsFlagSet( dfAnchorStyle.CenterVertical ) )
			{
				top = Mathf.RoundToInt( ( parentSize.y - controlSize.y ) * 0.5f );
				bottom = Mathf.RoundToInt( top + controlSize.y );
			}
			else
			{

				if( anchorStyle.IsFlagSet( dfAnchorStyle.Top ) )
				{
					top = margins.top;
					bottom = top + controlSize.y;
				}

				if( anchorStyle.IsFlagSet( dfAnchorStyle.Bottom ) )
				{
					bottom = ( parentSize.y - margins.bottom );
					if( !anchorStyle.IsFlagSet( dfAnchorStyle.Top ) )
					{
						top = bottom - controlSize.y;
					}
				}

			}

			var newSize = new Vector2(
				Mathf.Max( 0, right - left ),
				Mathf.Max( 0, bottom - top )
				);

			var newPosition = new Vector3( left, top );

			// NOTE: It is very important to set Size before setting Position,
			// since the Position property relies on the value of the Size
			// property when calculating the transform position based on the 
			// current value of the Pivot property
			owner.Size = newSize;
			owner.setRelativePosition( ref newPosition );

		}

		private Vector2 getParentSize()
		{

			// NOTE: There may be some circumstances where owner.parent 
			// does not contain the correct value at this point, such
			// as with newly-instantiated controls or prefabs. Unfortunately,
			// testing with control.transform.parent.GetComponent<dfControl>()
			// would always return the correct value, but allocates 
			// memory. This does not seem to be consistent between versions
			// of Unity, with 4.2 not allocating much (or any memory), and 
			// 4.3 allocating a significant amount of memory.

			var parent = owner.parent;
			if( parent != null )
				return parent.Size;

			var manager = owner.GetManager();
			var screenSize = manager.GetScreenSize();

			return screenSize;

		}

		#endregion

		#region System.Object overrides

		/// <summary>
		/// Returns a formatted string summarizing this object's state
		/// </summary>
		public override string ToString()
		{

			if( owner == null )
				return "NO OWNER FOR ANCHOR";

			var parent = owner.parent;

			return string.Format( "{0}.{1} - {2}", parent != null ? parent.name : "SCREEN", owner.name, margins );

		}

		#endregion

	}

	#endregion

}

