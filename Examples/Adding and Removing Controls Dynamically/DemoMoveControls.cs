using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Add-Remove Controls/Move Child Control" )]
public class DemoMoveControls : MonoBehaviour 
{

	public dfScrollPanel from;
	public dfScrollPanel to;

	public void OnClick()
	{

		// Suspending layout when adding or removing a large
		// group of controls can speed up the process noticeably
		from.SuspendLayout();
		to.SuspendLayout();

		while( from.Controls.Count > 0 )
		{

			var control = from.Controls[ 0 ];

			// Removes the control from the source container, 
			// but does not delete it. 
			from.RemoveControl( control );

			// Setting the control's ZOrder property to -1 
			// tells the container to auto-increment it, 
			// which in this scene has the effect of placing 
			// it at the end of the list.
			control.ZOrder = -1;

			// Add the existing control to the target container
			to.AddControl( control );

		}

		// Always remember to tell the control to resume its
		// normal layout handling.
		from.ResumeLayout();
		to.ResumeLayout();

		// The from control may now have an invalid scroll position.
		from.ScrollPosition = Vector2.zero;

	}

}
