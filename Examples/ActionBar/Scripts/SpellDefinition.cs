using UnityEngine;
using System.Linq;
using System.Collections;

public class SpellDefinition
{

	#region Public fields 

	public string Name;
	public string Type;
	public int Cost;
	public string Icon;
	public float Recharge;
	public float Delay;
	public string Description;

	#endregion

	#region Static members

	public static SpellDefinition[] AllSpells = new SpellDefinition[]
	{
		// Make up some ridiculous spells just to have something to work with :-)
		new SpellDefinition { Name = "KnitBone", Icon = "spell-5", Cost = 15, Delay = 0.25f, Recharge = 6f, Type = "Earth", Description = "You heal at [color #00FF00]110%[/color] for the next [color #00FF00]30[/color] seconds" },
		new SpellDefinition { Name = "Meteor Storm", Icon = "spell-2", Cost = 5, Delay = 1f, Recharge = 4f, Type = "Fire", Description = "Rains fire from the sky, dealing [color #00FF00]3[/color] damage per second to all within range for [color #00FF00]5[/color] seconds" },
		new SpellDefinition { Name = "Greater Flare", Icon = "spell-1", Cost = 15, Delay = 0.5f, Recharge = 4f, Type = "Fire", Description = "Blast of fire deals [color #00FF00]25[/color] damage to target" },
		new SpellDefinition { Name = "Aura of Protection", Icon = "spell-4", Cost = 10, Delay = 1f, Recharge = 10f, Type = "Spirit", Description = "All physical attacks against you are reduced by [color #00ff00]15%[/color] damage for [color #00FF00]2[/color] seconds" },
		new SpellDefinition { Name = "Aura of Attunement", Icon = "spell-9", Cost = 25, Delay = 1f, Recharge = 10f, Type = "Spirit", Description = "Your next 2 spells are [color #00ff00]15%[/color] more effective" },
		new SpellDefinition { Name = "Enhanced Strike", Icon = "spell-3", Cost = 5, Delay = 1f, Recharge = 6f, Type = "Spirit", Description = "Attacks with a bladed weapon deal [color #00ff00]15%[/color] more damage for [color #00FF00]5[/color] seconds" },
		new SpellDefinition { Name = "Flame Blade", Icon = "spell-7", Cost = 15, Delay = 1f, Recharge = 20f, Type = "Fire", Description = "Attacks with a bladed weapon deal [color #ff0000]+5 Fire Damage[/color] for [color #00FF00]5[/color] seconds" },
		new SpellDefinition { Name = "Chilling Wind", Icon = "spell-6", Cost = 15, Delay = 1f, Recharge = 10f, Type = "Air", Description = "Attacks dealing fire damage against you and all adjacent party members are reduced by [color #00FF00]10%[/color] for [color #00FF00]2[/color] seconds" },
	};

	internal static SpellDefinition FindByName( string Name )
	{
		return AllSpells.FirstOrDefault( spell => spell.Name == Name );
	}

	#endregion

}

