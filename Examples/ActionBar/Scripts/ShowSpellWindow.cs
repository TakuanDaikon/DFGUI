using UnityEngine;
using System.Collections;

[AddComponentMenu( "Daikon Forge/Examples/Actionbar/Show Spell Window" )]
public class ShowSpellWindow : MonoBehaviour
{

	private bool busy = false;
	private bool isVisible = false;

	void OnEnable()
	{
		var spellWindow = GameObject.Find( "Spell Window" ).GetComponent<dfControl>();
		spellWindow.IsVisible = false;
	}

	void OnClick()
	{

		if( busy )
			return;

		StopAllCoroutines();

		var spellWindow = GameObject.Find( "Spell Window" ).GetComponent<dfControl>();
		if( !isVisible )
			StartCoroutine( showWindow( spellWindow ) );
		else
			StartCoroutine( hideWindow( spellWindow ) );

	}

	IEnumerator hideWindow( dfControl window )
	{

		busy = true;
		isVisible = false;

		window.IsVisible = true;
		window.GetManager().BringToFront( window );

		var opacity = new dfAnimatedFloat( 1f, 0f, 0.33f );
		while( opacity > 0.05f )
		{
			window.Opacity = opacity;
			yield return null;
		}

		window.Opacity = 0f;

		busy = false;

	}

	IEnumerator showWindow( dfControl window )
	{

		isVisible = true;
		busy = true;

		window.IsVisible = true;
		window.GetManager().BringToFront( window );

		var opacity = new dfAnimatedFloat( 0f, 1f, 0.33f );
		while( opacity < 0.95f )
		{
			window.Opacity = opacity;
			yield return null;
		}

		window.Opacity = 1f;

		busy = false;
		isVisible = true;

	}

}
