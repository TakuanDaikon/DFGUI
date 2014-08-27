using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Examples/Actionbar/View Model" )]
public class ActionBarViewModel : MonoBehaviour
{

	#region Public events 

	public delegate void SpellEventHandler( SpellDefinition spell );
	public event SpellEventHandler SpellActivated;
	public event SpellEventHandler SpellDeactivated;

	#endregion

	#region Protected fields

	[SerializeField]
	private float _health = 0;

	[SerializeField]
	private int _maxHealth = 100;

	[SerializeField]
	private float _healthRegenRate = 0.5f;

	[SerializeField]
	private float _energy = 0;

	[SerializeField]
	private int _maxEnergy = 100;

	[SerializeField]
	private float _energyRegenRate = 1f;

	#endregion

	#region Public properties

	public int MaxHealth { get { return _maxHealth; } }

	public int MaxEnergy { get { return _maxEnergy; } }

	public int Health
	{
		get { return (int)_health; }
		private set { _health = Mathf.Max( 0, Mathf.Min( _maxHealth, value ) ); }
	}

	public int Energy
	{
		get { return (int)_energy; }
		private set 
		{
			_energy = Mathf.Max( 0, Mathf.Min( _maxEnergy, value ) ); 
		}
	}

	#endregion

	#region Private variables 

	private List<SpellCastInfo> activeSpells = new List<SpellCastInfo>();

	#endregion

	#region Unity events

	void OnEnable()
	{
		// HACK: http://answers.unity3d.com/questions/217941/onenable-awake-start-order.html
	}

	void Start()
	{
		// Just for demo purposes, start the stats at a low point to demonstrate regeneration
		_health = 35;
		_energy = 50;
	}

	void Update()
	{

		// Stat Regeneration
		_health = Mathf.Min( _maxHealth, _health + Time.deltaTime * _healthRegenRate );
		_energy = Mathf.Min( _maxEnergy, _energy + Time.deltaTime * _energyRegenRate );

		// Maintain the list of current spells
		for( int i = activeSpells.Count - 1; i >= 0; i-- )
		{
			var spellInfo = activeSpells[ i ];
			var activeTime = Time.realtimeSinceStartup - spellInfo.whenCast;
			if( spellInfo.spell.Recharge <= activeTime )
			{
				activeSpells.RemoveAt( i );
				if( SpellDeactivated != null )
				{
					SpellDeactivated( spellInfo.spell );
				}
			}
		}

	}

	#endregion

	#region Public methods 

	public void CastSpell( string spellName )
	{

		// Lookup a reference to the named spell
		var spell = SpellDefinition.FindByName( spellName );
		if( spell == null )
			throw new InvalidCastException(); // lol

		// Make sure that the spell is not already current
		if( activeSpells.Any( activeSpell => activeSpell.spell == spell ) )
			return;

		// Make sure there is enough Magic available
		if( Energy < spell.Cost )
			return;

		// Deduct the spell cost
		Energy -= spell.Cost;

		// Add the spell to the list of current spells
		activeSpells.Add( new SpellCastInfo() { spell = spell, whenCast = Time.realtimeSinceStartup } );

		// Notify any observers that the spell was cast
		if( SpellActivated != null )
		{
			SpellActivated( spell );
		}

	}

	#endregion

	private class SpellCastInfo
	{
		public SpellDefinition spell;
		public float whenCast;
	}

}
