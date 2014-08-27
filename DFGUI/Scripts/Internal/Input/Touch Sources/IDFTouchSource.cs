using System;
using System.Collections.Generic;

using UnityEngine;

public interface IDFTouchInputSource
{

	int TouchCount { get; }
	IList<dfTouchInfo> Touches { get; }

	void Update();
	dfTouchInfo GetTouch( int index );

}
