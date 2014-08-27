using System;
using System.Collections;

using UnityEngine;

[AddComponentMenu( "Daikon Forge/Examples/General/Debug Render Info" )]
public class DebugRenderInfo : MonoBehaviour
{

	public float interval = 0.5F;

	private dfLabel info;
	private dfGUIManager view;

	private float lastUpdate = 0f;
	private int frameCount = 0;

	void Start()
	{

		info = GetComponent<dfLabel>();
		if( info == null )
		{
			this.enabled = false;
			throw new Exception( "No Label component found" );
		}

		info.Text = "";

	}

	void Update()
	{

		if( view == null )
		{
			view = info.GetManager();
		}

		frameCount += 1;

		var elapsed = Time.realtimeSinceStartup - lastUpdate;
		if( elapsed < interval )
			return;

		lastUpdate = Time.realtimeSinceStartup;

		float fps = 1f / ( elapsed / (float)frameCount );

#if UNITY_EDITOR
		var screenSize = view.GetScreenSize();
		var screenSizeFormat = string.Format( "{0}x{1}", (int)screenSize.x, (int)screenSize.y );
#else
		var screenSize = new Vector2( Screen.width, Screen.height );
		var screenSizeFormat = string.Format( "{0}x{1}", (int)screenSize.x, (int)screenSize.y );
#endif

		var statusFormat = @"Screen : {0}, DrawCalls: {1}, Triangles: {2}, Mem: {3:F0}MB, FPS: {4:F0}";

		var totalMemory = Profiler.supported
			? Profiler.GetMonoUsedSize() / 1048576f
			: GC.GetTotalMemory( false ) / 1048576f;

		var status = string.Format(
			statusFormat,
			screenSizeFormat,
			view.TotalDrawCalls,
			view.TotalTriangles,
			totalMemory,
			fps
		);

		info.Text = status.Trim();

		frameCount = 0;

	}

}
