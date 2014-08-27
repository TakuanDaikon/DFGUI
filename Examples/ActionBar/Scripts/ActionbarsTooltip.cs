using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Actionbar/Tooltip" )]
public class ActionbarsTooltip : MonoBehaviour 
{

	private static ActionbarsTooltip _instance;

	private static dfPanel _panel;
	private static dfLabel _name;
	private static dfLabel _info;
	private static Vector2 _cursorOffset;

	// Called by Unity just before any of the Update methods is called the first time.
	public void Start()
	{

		// Store the singleton instance for reference in static functions
		_instance = this;
		
		// Obtain a reference to the control instances attached to this object
		_panel = GetComponent<dfPanel>();
		_name = _panel.Find<dfLabel>( "lblName" );
		_info = _panel.Find<dfLabel>( "lblInfo" );

		// We don't want the tooltip visible unless it is being used
		_panel.Hide();

		// We don't want the tooltip to intercept mouse messages
		_panel.IsInteractive = false;
		_panel.IsEnabled = false;

	}

	public void Update()
	{

		if( _panel.IsVisible )
		{
			setPosition( Input.mousePosition );
		}

	}

	/// <summary>
	/// Displays the drag cursor, which will follow the mouse until hidden
	/// </summary>
	/// <param name="sprite">The sprite to display in the drag cursor</param>
	/// <param name="position">The initial position of the drag cursor</param>
	/// <param name="offset">The mouse offset within the dragged object</param>
	public static void Show( SpellDefinition spell )
	{

		if( spell == null )
		{
			Hide();
			return;
		}

		// Assign label text, which will internally recalculate the label's Size
		_name.Text = spell.Name;
		_info.Text = spell.Description;

		// Resize this control to match the size of the contents
		var labelHeight = _info.RelativePosition.y + _info.Size.y;
		_panel.Height = labelHeight;

		// The tooltip should appear above the mouse
		_cursorOffset = new Vector2( 0, labelHeight + 10 );

		// Display the base panel
		_panel.Show();
		_panel.BringToFront();

		// Call the update function to position the tooltip
		_instance.Update();

	}

	public static void Hide()
	{
		// Hide the panel and make sure the collider is 
		// behind everything else
		_panel.Hide();
		_panel.SendToBack();
	}

	private static void setPosition( Vector2 position )
	{

		// Convert position from "screen coordinates" to "gui coordinates"
		position = _panel.GetManager().ScreenToGui( position );

		// Center the control on the mouse/touch
		_panel.RelativePosition = position - _cursorOffset;

	}

}
