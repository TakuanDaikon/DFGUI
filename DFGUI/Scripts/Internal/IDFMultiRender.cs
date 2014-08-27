using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Indicates that a control is capable of returning multiple 
/// render buffers during rendering
/// </summary>
public interface IDFMultiRender
{
	dfList<dfRenderData> RenderMultiple();
}
