using UnityEngine;

using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Tooltip/Floating Tooltip" )]
public class DemoFloatingTooltip : MonoBehaviour 
{

	/// <summary>
	/// The amount of time (in seconds) before the tooltip is displayed
	/// </summary>
	public float tooltipDelay = 1f;

	private dfLabel _tooltip;
	private dfControl lastControl;
	private float tooltipDelayStart = 0f;

	public void Start()
	{

		// Obtain a reference to the dfLabel control that will 
		// render the tooltip data
		this._tooltip = GetComponent<dfLabel>();

		// We don't want the tooltip to intercept mouse messages
		_tooltip.IsInteractive = false;
		_tooltip.IsEnabled = false;

	}

	public void Update()
	{

		// Find out if there is currently a control under the mouse.
		// Note: Always returns the top-most control.
		var control = dfInputManager.ControlUnderMouse;

		// If there is no control then hide the tooltip
		if( control == null )
		{
			_tooltip.Hide();
		}

		// If there is a control under the mouse then display its tooltip
		else if( control != lastControl )
		{

			// Keep track of when the mouse started hovering over the control
			tooltipDelayStart = Time.realtimeSinceStartup;

			// If the control does not have tooltip data, hide the label
			if( string.IsNullOrEmpty( control.Tooltip ) )
			{
				_tooltip.Hide();
			}
			else
			{
				// Set the label's Text to match the control's tooltip
				_tooltip.Text = control.Tooltip;
			}

		}
		else if( lastControl != null && !string.IsNullOrEmpty( lastControl.Tooltip ) )
		{

			if( Time.realtimeSinceStartup - tooltipDelayStart > tooltipDelay )
			{

				// Show the tooltip and make sure it's the topmost control on the screen
				_tooltip.Show();
				_tooltip.BringToFront();

			}

		}

		// If the tooltip is currently visible, then display it near the 
		// current mouse position
		if( _tooltip.IsVisible )
		{
			setPosition( Input.mousePosition );
		}

		// Keep track of the last control so that we know when to change
		// the tooltip
		lastControl = control;

	}

	private void setPosition( Vector2 position )
	{

		// The tooltip should appear above the mouse
		var cursorOffset = new Vector2( 0, _tooltip.Height + 25 );

		// Convert position from "screen coordinates" to "gui coordinates"
		var manager = _tooltip.GetManager();
		position = manager.ScreenToGui( position ) - cursorOffset;

		// Ensure that the tooltip remains fully visible
		if( position.y < 0 )
		{
			// TODO: Make tooltip appear *below* mouse cursor if forced to overlap
			position.y = 0;
		}
		if( position.x + _tooltip.Width > manager.GetScreenSize().x )
		{
			position.x = manager.GetScreenSize().x - _tooltip.Width;
		}

		// Center the control on the mouse/touch
		_tooltip.RelativePosition = position;

	}

}
