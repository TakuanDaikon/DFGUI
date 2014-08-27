using System.Collections.Generic;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Input/Debugging/Touch Visualizer" )]
public class DEMO_TouchVisualizer : MonoBehaviour
{

	public bool editorOnly = false;
	public bool showMouse = false;
	public bool showPlatformInfo = false;
	public int iconSize = 32;
	public Texture2D touchIcon;

	private IDFTouchInputSource input;

	void Awake()
	{
		this.useGUILayout = false;
	}

	public void OnGUI()
	{

		if( editorOnly && !Application.isEditor )
			return;

		if( input == null )
		{
			
			var inputManager = GetComponent<dfInputManager>();
			if( inputManager == null )
			{
				Debug.LogError( "No dfInputManager instance found", this );
				this.enabled = false;
				return;
			}

			if( inputManager.UseTouch )
			{

				input = inputManager.TouchInputSource;

				if( input == null )
				{
					Debug.LogError( "No dfTouchInputSource component found", this );
					this.enabled = false;
					return;
				}

			}
			else 
			{
				
				if( Application.isPlaying )
					this.enabled = false;

				return;

			}

		}

		if( showPlatformInfo )
		{
			var rect = new Rect( 5, 0, 800, 25 );
			GUI.Label( rect, "Touch Source: " + input + ", Platform: " + Application.platform );
		}

		if( showMouse && !Application.isEditor )
		{
			drawTouchIcon( Input.mousePosition );
		}

		var count = input.TouchCount;
		for( int i = 0; i < count; i++ )
		{
			var touch = input.GetTouch( i );
			drawTouchIcon( touch.position );
		}

	}

	private void drawTouchIcon( Vector3 pos )
	{

		var height = Screen.height;
		pos.y = height - pos.y;

		var rect = new Rect( pos.x - iconSize / 2, pos.y - iconSize / 2, iconSize, iconSize );
		
		GUI.DrawTexture( rect, touchIcon );

	}

}
