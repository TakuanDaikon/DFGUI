using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Add-Remove Controls/Create Child Control" )]
public class DemoCreateChildControls : MonoBehaviour 
{

	public dfScrollPanel target;

	// These colors are used to visually differentiate different
	// groups of controls just to make sure they are added in 
	// the right order
	private int colorNum = 0;
	private Color32[] colors = new Color32[]
	{
		Color.white,
		Color.red,
		Color.green,
		Color.black
	};

	public void Start()
	{
		// If not already assigned, grab a reference to the 
		// Scroll Panel that will contain the dynamically-
		// created controls.
		if( target == null )
		{
			this.target = GetComponent<dfScrollPanel>();
		}
	}

	public void OnClick()
	{

		for( int i = 0; i < 10; i++ )
		{

			// Creates the new control and adds it to the target
			// Scroll Panel. Returns a reference to the new
			// control.
			var button = target.AddControl<dfButton>();

			// Set whatever properties the control needs after creation
			button.NormalBackgroundColor = colors[ colorNum % colors.Length ];
			button.BackgroundSprite = "button-normal";
			button.Text = string.Format( "Button {0}", button.ZOrder );
			button.Anchor = dfAnchorStyle.Left | dfAnchorStyle.Right;
			button.Width = target.Width - target.ScrollPadding.horizontal;

		}

		colorNum += 1;

	}

	public void OnDoubleClick()
	{
		OnClick();
	}

}
