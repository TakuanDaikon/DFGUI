using UnityEngine;
using System.Collections;

[AddComponentMenu( "Daikon Forge/Examples/General/Animate Popup" )]
public class AnimatePopup : MonoBehaviour
{

	private const float ANIMATION_LENGTH = 0.15f;

	private dfListbox target = null;

	void OnDropdownOpen( dfDropdown dropdown, dfListbox popup )
	{

		if( this.target != null )
		{
			StopCoroutine( "animateOpen" );
			StopCoroutine( "animateClose" );
			Destroy( this.target.gameObject );
		}

		this.target = popup;

		StartCoroutine( animateOpen( popup ) );

	}

	void OnDropdownClose( dfDropdown dropdown, dfListbox popup )
	{
		StartCoroutine( animateClose( popup ) );
	}

	IEnumerator animateOpen( dfListbox popup )
	{

		var runningTime = 0f;

		var startAlpha = 0f;
		var endAlpha = 1f;

		var startHeight = 20f;
		var endHeight = popup.Height;

		while( this.target == popup && runningTime < ANIMATION_LENGTH )
		{

			runningTime = Mathf.Min( runningTime + Time.deltaTime, ANIMATION_LENGTH );
			popup.Opacity = Mathf.Lerp( startAlpha, endAlpha, runningTime / ANIMATION_LENGTH );

			var height = Mathf.Lerp( startHeight, endHeight, runningTime / ANIMATION_LENGTH );
			popup.Height = height;

			yield return null;

		}

		popup.Opacity = 1f;
		popup.Height = endHeight;

		yield return null;

		popup.Invalidate();

	}

	IEnumerator animateClose( dfListbox popup )
	{

		var runningTime = 0f;

		var startAlpha = 1f;
		var endAlpha = 0f;

		var startHeight = popup.Height;
		var endHeight = 20f;

		while( this.target == popup && runningTime < ANIMATION_LENGTH )
		{

			runningTime = Mathf.Min( runningTime + Time.deltaTime, ANIMATION_LENGTH );
			popup.Opacity = Mathf.Lerp( startAlpha, endAlpha, runningTime / ANIMATION_LENGTH );

			var height = Mathf.Lerp( startHeight, endHeight, runningTime / ANIMATION_LENGTH );
			popup.Height = height;

			yield return null;

		}

		this.target = null;
		Destroy( popup.gameObject );

	}

}
