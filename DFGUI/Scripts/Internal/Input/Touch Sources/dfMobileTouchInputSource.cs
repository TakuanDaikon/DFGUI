using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class dfMobileTouchInputSource : IDFTouchInputSource
{

	#region Singleton implementation 

	private static dfMobileTouchInputSource instance;

	public static dfMobileTouchInputSource Instance
	{

		get
		{

			if( instance == null )
			{
				instance = new dfMobileTouchInputSource();
			}

			return instance;

		}

	}

	#endregion 

	#region Private variables

	private List<dfTouchInfo> activeTouches = new List<dfTouchInfo>();

	#endregion 

	#region IDFTouchInputSource Members

	public int TouchCount
	{
		get { return Input.touchCount; }
	}

	public IList<dfTouchInfo> Touches
	{
		get { return activeTouches; }
	}

	public dfTouchInfo GetTouch( int index )
	{
		return Input.GetTouch( index );
	}

	public void Update()
	{

		activeTouches.Clear();

		for( int i = 0; i < TouchCount; i++ )
		{
			activeTouches.Add( GetTouch( i ) );
		}

	}

	#endregion

}
