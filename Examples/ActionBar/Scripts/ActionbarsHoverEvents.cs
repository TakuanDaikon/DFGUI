using UnityEngine;

using System.Linq;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Actionbar/Hover Events" )]
public class ActionbarsHoverEvents : MonoBehaviour 
{

	private dfControl actionBar;

	private dfControl lastTarget;
	private dfControl target;

	private bool isTooltipVisible = false;

	public void Start()
	{
		actionBar = GetComponent<dfControl>();
	}

	public void OnMouseHover( dfControl control, dfMouseEventArgs mouseEvent )
	{

		if( isTooltipVisible )
			return;

		var isSpellSlot = actionBar.Controls.Contains( mouseEvent.Source );
		if( isSpellSlot )
		{

			target = mouseEvent.Source;
			if( target == lastTarget )
				return;

			lastTarget = target;

			isTooltipVisible = true;

			var slot = target.GetComponentInChildren<SpellSlot>();
			if( string.IsNullOrEmpty( slot.Spell ) )
				return;

			var spell = SpellDefinition.FindByName( slot.Spell );
			if( spell == null )
				return;

			ActionbarsTooltip.Show( spell );

		}
		else
		{
			lastTarget = null;
		}

	}

	public void OnMouseDown()
	{
	
		isTooltipVisible = false;

		ActionbarsTooltip.Hide();
		target = null;

	}

	public void OnMouseLeave()
	{

		if( target == null )
			return;

		var mousePosition = Input.mousePosition;
		mousePosition.y = Screen.height - mousePosition.y;

		if( !target.GetScreenRect().Contains( mousePosition, true ) )
		{

			isTooltipVisible = false;
			
			ActionbarsTooltip.Hide();
			target = null;

		}

	}

}
