using UnityEngine;
using System.Collections;

[AddComponentMenu( "Daikon Forge/Examples/General/FPS Counter" )]
public class dfFPSCounter : MonoBehaviour
{

	public float updateInterval = 0.5F;

	private float accum = 0; // FPS accumulated over the interval
	private int frames = 0; // Frames drawn over the interval
	private float timeleft; // Left time for current interval

	private dfLabel label;

	void Start()
	{

		label = GetComponent<dfLabel>();
		if( label == null )
		{
			Debug.LogError( "FPS Counter needs a Label component!" );
		}
		
		timeleft = updateInterval;

		label.Text = "";

	}

	void Update()
	{

		if( label == null )
			return;

		timeleft -= Time.deltaTime;
		accum += Time.timeScale / Time.deltaTime;
		++frames;

		// Interval ended - update GUI text and start new interval
		if( timeleft <= 0.0 )
		{

			// display two fractional digits (f2 format)
			float fps = accum / frames;
			string format = System.String.Format( "{0:F0} FPS", fps );

			label.Text = format;

			if( fps < 30 )
			{
				label.Color = Color.yellow;
			}
			else
			{
				if( fps < 10 )
					label.Color = Color.red;
				else
					label.Color = Color.green;
			}

			timeleft = updateInterval;
			accum = 0.0F;
			frames = 0;

		}

	}

}
