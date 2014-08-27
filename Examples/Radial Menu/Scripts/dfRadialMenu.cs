using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[ExecuteInEditMode]
[Serializable]
[AddComponentMenu( "Daikon Forge/Examples/Menus/Radial Menu" )]
public class dfRadialMenu : MonoBehaviour
{

	#region Public events 

	public delegate void CircularMenuEventHandler( dfRadialMenu sender );

	public event CircularMenuEventHandler BeforeMenuOpened;
	public event CircularMenuEventHandler MenuOpened;
	public event CircularMenuEventHandler MenuClosed;

	#endregion

	#region Serialized properties

	/// <summary> How far from center (in pixels) the buttons will be positioned </summary>
	public float radius = 200;

	/// <summary> The angle that the first button will be positioned at </summary>
	public float startAngle = 0;

	/// <summary> The angle that all buttons will occupy </summary>
	public float openAngle = 360;

	/// <summary> If set to TRUE, buttons will be rotated to match their final angle </summary>
	public bool rotateButtons = false;

	/// <summary> If set to TRUE, buttons will be faded in or out when opening or closing the menu </summary>
	public bool animateOpacity = false;

	/// <summary> If set to TRUE, buttons will be fanned when opening or closing the menu </summary>
	public bool animateOpenAngle = false;

	/// <summary> If set to TRUE, buttons will fly in or out when opening or closing the menu </summary>
	public bool animateRadius = false;

	/// <summary> If set to TRUE, clicking the menu's container or any child controls will toggle the menu </summary>
	public bool autoToggle = false;

	/// <summary> If set to TRUE, the menu will close when it loses focus </summary>
	public bool closeOnLostFocus = false;

	/// <summary> Indicates how long any animations will take to perform </summary>
	public float animationLength = 0.5f;

	/// <summary> If you want any child controls to *not* be part of the menu, add them to this list </summary>
	public List<dfControl> excludedControls = new List<dfControl>();

	/// <summary>
	/// This is the control whose child controls will be treated as buttons for the circular menu
	/// </summary>
	public dfControl host;

	#endregion

	#region Private runtime variables 

	private bool isAnimating = false;
	private bool isOpen = false;

	#endregion

	#region Public properties 

	public bool IsOpen
	{
		get { return this.isOpen; }
		set
		{
			if( isOpen != value )
			{
				if( value )
					Open();
				else
					Close();
			}
		}
	}

	#endregion

	#region Public methods 

	public void Open()
	{
		if( !isOpen && !isAnimating && this.enabled && gameObject.activeSelf )
		{
			StartCoroutine( openMenu() );
		}
	}

	public void Close()
	{

		if( isOpen && !isAnimating && this.enabled && gameObject.activeSelf )
		{

			StartCoroutine( closeMenu() );

			if( host.ContainsFocus )
				dfGUIManager.SetFocus( null );

		}

	}

	public void Toggle()
	{

		if( isAnimating )
			return;

		if( isOpen )
			Close();
		else
			Open();

	}

	#endregion

	#region Monobehavior events

	public void OnEnable()
	{

		if( host == null )
			host = GetComponent<dfControl>();

	}

	public void Start()
	{
		if( Application.isPlaying )
		{
			using( var list = getButtons() )
			{
				for( int i = 0; i < list.Count; i++ )
				{
					list[ i ].Hide();
				}
			}
		}
	}

	public void Update()
	{
		if( !Application.isPlaying )
		{
			arrangeButtons();
		}
	}

	#endregion

	#region dfControl events 

	public void OnLeaveFocus( dfControl sender, dfFocusEventArgs args )
	{
		if( closeOnLostFocus && !host.ContainsFocus && Application.isPlaying )
		{
			Close();
		}
	}

	public void OnClick( dfControl sender, dfMouseEventArgs args )
	{
		if( autoToggle || args.Source == host )
		{
			Toggle();
		}
	}

	#endregion

	#region Private utility methods

	private dfList<dfControl> getButtons()
	{
		return host.Controls.Where( x => x.enabled && !excludedControls.Contains( x ) );
	}

	private void arrangeButtons()
	{
		arrangeButtons( this.startAngle, this.radius, this.openAngle, 1f );
	}

	private IEnumerator openMenu()
	{

		if( BeforeMenuOpened != null ) BeforeMenuOpened( this );
		host.Signal( "OnBeforeMenuOpened", this );

		isAnimating = true;

		var animate =
			animateOpacity ||
			animateOpenAngle ||
			animateRadius;

		if( animate )
		{

			var time = Mathf.Max( 0.1f, animationLength );
			var animOpenAngle = new dfAnimatedFloat( animateOpenAngle ? 0 : openAngle, openAngle, time );
			var animRadius = new dfAnimatedFloat( animateRadius ? 0 : radius, radius, time );
			var animOpacity = new dfAnimatedFloat( this.animateOpacity ? 0 : 1, 1, time );

			var endTime = Time.realtimeSinceStartup + time;
			while( Time.realtimeSinceStartup < endTime )
			{
				arrangeButtons( this.startAngle, animRadius, animOpenAngle, animOpacity );
				yield return null;
			}

		}

		arrangeButtons();
		isOpen = true;
		isAnimating = false;

		if( MenuOpened != null )
			MenuOpened( this );

		host.Signal( "OnMenuOpened", this );

	}

	private IEnumerator closeMenu()
	{

		isAnimating = true;

		var animate =
			animateOpacity ||
			animateOpenAngle ||
			animateRadius;

		if( animate )
		{

			var time = Mathf.Max( 0.1f, animationLength );
			var animOpenAngle = new dfAnimatedFloat( openAngle, animateOpenAngle ? 0 : openAngle, time );
			var animRadius = new dfAnimatedFloat( radius, animateRadius ? 0 : radius, time );
			var animOpacity = new dfAnimatedFloat( 1, this.animateOpacity ? 0 : 1, time );

			var endTime = Time.realtimeSinceStartup + time;
			while( Time.realtimeSinceStartup < endTime )
			{
				arrangeButtons( this.startAngle, animRadius, animOpenAngle, animOpacity );
				yield return null;
			}

		}

		using( var list = getButtons() )
		{
			for( int i = 0; i < list.Count; i++ )
			{
				list[ i ].IsVisible = false;
			}
		}	
			
		isOpen = false;
		isAnimating = false;

		if( MenuClosed != null )
			MenuClosed( this );

		host.Signal( "OnMenuOpened", this );

	}

	private void arrangeButtons( float startAngle, float radius, float openAngle, float opacity )
	{

		var angle = clampRotation( startAngle );
		var distance = radius; 
		var center = (Vector3)host.Size * 0.5f;

		using( var list = getButtons() )
		{

			if( list.Count == 0 )
				return;

			var direction = Mathf.Sign( openAngle );
			var angleIncrement = direction * Mathf.Min( Mathf.Abs( clampRotation( openAngle ) ) / ( list.Count - 1 ), 360f / list.Count );

			for( int i = 0; i < list.Count; i++ )
			{
				
				var button = list[ i ];

				var rotation = Quaternion.Euler( 0, 0, angle );
				var position = center + rotation * Vector3.down * distance;

				button.RelativePosition = position - (Vector3)button.Size * 0.5f;
				if( rotateButtons )
				{
					button.Pivot = dfPivotPoint.MiddleCenter;
					button.transform.localRotation = Quaternion.Euler( 0, 0, -angle );
				}
				else
				{
					button.transform.localRotation = Quaternion.identity;
				}

				button.IsVisible = true;
				button.Opacity = opacity;

				angle += angleIncrement;

			}

		}

	}

	private float clampRotation( float rotation )
	{
		return Mathf.Sign( rotation ) * Mathf.Max( 0.1f, Mathf.Min( 360, Mathf.Abs( rotation ) ) );
	}

	#endregion

}
