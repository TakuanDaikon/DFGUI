/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CustomEditor( typeof( SpellInventory ) )]
public class SpellInventoryInspector : Editor
{

	public override void OnInspectorGUI()
	{

		var control = target as SpellInventory;

		var spellList = getSpellList();
		var assignedIndex = control.Spell != null ? Array.IndexOf( spellList, control.Spell ) : -1;
		var index = EditorGUILayout.Popup( "Spell", assignedIndex, spellList );
		if( index != assignedIndex )
		{
			dfEditorUtil.MarkUndo( control, "Assign spell" );
			control.Spell = ( index > 0 ? spellList[ index ] : "" );
		}

	}

	private string[] getSpellList()
	{

		var spellList = SpellDefinition.AllSpells
			.Select( spell => spell.Name )
			.OrderBy( name => name )
			.ToList();

		spellList.Insert( 0, "-- NONE --" );

		return spellList.ToArray();

	}

}
