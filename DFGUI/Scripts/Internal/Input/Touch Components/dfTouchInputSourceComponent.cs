using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public abstract class dfTouchInputSourceComponent : MonoBehaviour
{

	public int Priority;

	public abstract IDFTouchInputSource Source { get; }

}
