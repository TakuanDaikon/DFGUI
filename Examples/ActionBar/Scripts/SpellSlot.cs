using System;
using System.Linq;
using System.Collections;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Examples/Actionbar/Spell Slot" )]
[ExecuteInEditMode]
public class SpellSlot : MonoBehaviour
{

	#region Protected serialized fields 

	[SerializeField]
	protected string spellName = "";

	[SerializeField]
	protected int slotNumber = 0;

	[SerializeField]
	protected bool isActionSlot = false;

	#endregion

	#region Private non-serialized fields 

	private bool isSpellActive = false;

	#endregion

	#region Public properties

	public bool IsActionSlot
	{
		get { return this.isActionSlot; }
		set 
		{ 
			this.isActionSlot = value;
			refresh();
		}
	}

	public string Spell
	{
		get { return this.spellName; }
		set
		{
			this.spellName = value;
			refresh();
		}
	}

	public int SlotNumber
	{
		get { return this.slotNumber; }
		set
		{
			this.slotNumber = value;
			refresh();
		}
	}

	#endregion

	#region Unity events 

	void OnEnable()
	{
		refresh();
	}

	void Start()
	{
		refresh();
	}

	void Update()
	{

		if( IsActionSlot && !string.IsNullOrEmpty( Spell ) )
		{
			if( Input.GetKeyDown( (KeyCode)( this.slotNumber + 48 ) ) )
			{
				castSpell();
			}
		}

	}

	#endregion

	#region Event handlers 

	public void onSpellActivated( SpellDefinition spell )
	{

		if( spell.Name != this.Spell )
			return;

		StartCoroutine( showCooldown() );

	}

	void OnDoubleClick()
	{

		if( !isSpellActive && !string.IsNullOrEmpty( Spell ) )
		{
			castSpell();
		}

	}

	#endregion

	#region Drag and drop

	void OnDragStart( dfControl source, dfDragEventArgs args )
	{

		if( allowDrag( args ) )
		{

			if( string.IsNullOrEmpty( Spell ) )
			{
				// Indicates that the drag-and-drop operation cannot be performed
				args.State = dfDragDropState.Denied;
			}
			else
			{

				// Get the offset that will be used for the drag cursor
				var sprite = GetComponent<dfControl>().Find( "Icon" ) as dfSprite;
				var ray = sprite.GetCamera().ScreenPointToRay( Input.mousePosition );
				var dragCursorOffset = Vector2.zero;
				if( !sprite.GetHitPosition( ray, out dragCursorOffset ) )
					return;

				// Set the variables that will be used to render the drag cursor. 
				// The UI library provides all of the drag and drop events necessary 
				// but does not provide a default drag visualization and requires 
				// that the application provide the visualization. We'll do that by
				// supplying a Texture2D that will be placed at the mouse location 
				// in the OnGUI() method. 
				ActionbarsDragCursor.Show( sprite, Input.mousePosition, dragCursorOffset );

				if( IsActionSlot )
				{
					// Visually indicate that they are *moving* the spell rather than
					// just dragging it into a slot
					sprite.SpriteName = "";
				}

				// Indicate that the drag and drop operation can continue and set
				// the user-defined data that will be sent to potential drop targets
				args.State = dfDragDropState.Dragging;
				args.Data = this;

			}

			// Do not let the OnDragStart event "bubble up" to higher-level controls
			args.Use();

		}

	}

	void OnDragEnd( dfControl source, dfDragEventArgs args )
	{

		ActionbarsDragCursor.Hide();

		if( !this.isActionSlot )
			return;

		if( args.State == dfDragDropState.CancelledNoTarget )
		{
			Spell = "";
		}

		refresh();

	}

	void OnDragDrop( dfControl source, dfDragEventArgs args )
	{

		if( allowDrop( args ) )
		{
			
			args.State = dfDragDropState.Dropped;

			var otherSlot = args.Data as SpellSlot;

			var temp = this.spellName;
			this.Spell = otherSlot.Spell;

			if( otherSlot.IsActionSlot )
			{
				otherSlot.Spell = temp;
			}
			
		}
		else
		{
			args.State = dfDragDropState.Denied;
		}

		args.Use();

	}

	private bool allowDrag( dfDragEventArgs args )
	{
		// Do not allow the user to drag and drop empty SpellSlot instances
		return !isSpellActive && !string.IsNullOrEmpty( spellName );
	}

	private bool allowDrop( dfDragEventArgs args )
	{

		if( isSpellActive )
			return false;

		// Only allow drop if the source is another SpellSlot and
		// this SpellSlot is assignable
		var slot = args.Data as SpellSlot;
		return slot != null && this.IsActionSlot;

	}

	#endregion

	#region Private utility methods 

	private IEnumerator showCooldown()
	{

		isSpellActive = true;

		var assignedSpell = SpellDefinition.FindByName( this.Spell );

		var sprite = GetComponent<dfControl>().Find( "CoolDown" ) as dfSprite;
		sprite.IsVisible = true;

		var startTime = Time.realtimeSinceStartup;
		var endTime = startTime + assignedSpell.Recharge;

		while( Time.realtimeSinceStartup < endTime )
		{

			var elapsed = Time.realtimeSinceStartup - startTime;
			var lerp = 1f - elapsed / assignedSpell.Recharge;

			sprite.FillAmount = lerp;

			yield return null;

		}

		sprite.FillAmount = 1f;
		sprite.IsVisible = false;

		isSpellActive = false;
		
	}

	private void castSpell()
	{

		var view = FindObjectsOfType( typeof( ActionBarViewModel ) ).FirstOrDefault() as ActionBarViewModel;
		if( view != null )
		{
			view.CastSpell( this.Spell );
		}

	}

	private void refresh()
	{

		var assignedSpell = SpellDefinition.FindByName( this.Spell );

		var sprite = GetComponent<dfControl>().Find<dfSprite>( "Icon" );
		sprite.SpriteName = assignedSpell != null ? assignedSpell.Icon : "";

		var label = GetComponentInChildren<dfButton>();
		label.IsVisible = this.IsActionSlot;
		label.Text = this.slotNumber.ToString();

	}

	#endregion

}
