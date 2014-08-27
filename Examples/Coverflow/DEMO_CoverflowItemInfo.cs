using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Coverflow/Item Info" )]
public class DEMO_CoverflowItemInfo : MonoBehaviour 
{

	public dfCoverflow scroller;
	public string[] descriptions;

	private dfLabel label;

	public void Start()
	{
		this.label = GetComponent<dfLabel>();
	}

	void Update()
	{

		if( scroller == null || descriptions == null || descriptions.Length == 0 )
			return;

		var index = Mathf.Max( 0, Mathf.Min( descriptions.Length - 1, scroller.selectedIndex ) );
		label.Text = descriptions[ index ];

	}

}
