using System;
using System.Linq;
using System.Collections;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Examples/Actionbar/Spell Inventory" )]
[ExecuteInEditMode]
public class SpellInventory : MonoBehaviour
{

	#region Protected serialized fields

	[SerializeField]
	protected string spellName = "";

	#endregion

	#region Private runtime variables 

	private bool needRefresh = true;

	#endregion

	#region Public properties

	public string Spell
	{
		get { return this.spellName; }
		set
		{
			this.spellName = value;
			refresh();
		}
	}

	#endregion 

	#region Events

	void OnEnable()
	{
	
		refresh();

		var control = gameObject.GetComponent<dfControl>();
		control.SizeChanged += delegate( dfControl source, Vector2 value )
		{
			// Queue the refresh to be processed in LateUpdate after the
			// control and its child controls have recalculated their 
			// new render size
			needRefresh = true;
		};

	}

	void LateUpdate()
	{
		if( needRefresh )
		{
			needRefresh = false;
			refresh();
		}
	}

	public void OnResolutionChanged()
	{
		needRefresh = true;
	}

	#endregion

	#region Private utility methods

	private void refresh()
	{

		var control = gameObject.GetComponent<dfControl>();
		var container = control.Parent as dfScrollPanel;

		if( container != null )
		{
			control.Width = container.Width - container.ScrollPadding.horizontal;
		}

		var slot = control.GetComponentInChildren<SpellSlot>();
		var lblCosts = control.Find<dfLabel>( "lblCosts" );
		var lblName = control.Find<dfLabel>( "lblName" );
		var lblDescription = control.Find<dfLabel>( "lblDescription" );

		if( lblCosts == null ) throw new Exception( "Not found: lblCosts" );
		if( lblName == null ) throw new Exception( "Not found: lblName" );
		if( lblDescription == null ) throw new Exception( "Not found: lblDescription" );

		var assignedSpell = SpellDefinition.FindByName( this.Spell );
		if( assignedSpell == null )
		{
			slot.Spell = "";
			lblCosts.Text = "";
			lblName.Text = "";
			lblDescription.Text = "";
			return;
		}
		else
		{
			slot.Spell = this.spellName;
			lblName.Text = assignedSpell.Name;
			lblCosts.Text = string.Format( "{0}/{1}/{2}", assignedSpell.Cost, assignedSpell.Recharge, assignedSpell.Delay );
			lblDescription.Text = assignedSpell.Description;
		}

		// Resize this control to match the size of the contents
		var descriptionHeight = lblDescription.RelativePosition.y + lblDescription.Size.y;
		var costsHeight = lblCosts.RelativePosition.y + lblCosts.Size.y;
		control.Height = Mathf.Max( descriptionHeight, costsHeight ) + 5;

	}

	#endregion

}
