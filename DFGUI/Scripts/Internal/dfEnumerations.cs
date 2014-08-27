/* Copyright 2013-2014 Daikon Forge */

/* 
 * @file dfEnumerations.cs 
 * @brief Contains enum definitions used by various classes throughought the DFGUI library
 */

using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Indicates how clipping should be performed
/// </summary>
public enum dfClippingMethod
{
	/// <summary>
	/// Clipping will be performed on the CPU
	/// </summary>
	Software,
	/// <summary>
	/// Clipping will be performed on the GPU
	/// </summary>
	Shader
}

/// <summary>
/// Indicates the direction that the animation should play in 
/// </summary>
public enum dfPlayDirection : int
{
	Forward = 0,
	Reverse = 1
}

/// <summary>
/// Specifies how auto-scaling of text will be determined
/// </summary>
public enum dfTextScaleMode
{
	/// <summary>
	/// Do not auto-scale the text
	/// </summary>
	None = 0,
	/// <summary>
	/// Auto-scale the text based on the size of the control
	/// </summary>
	ControlSize,
	/// <summary>
	/// Auto-size the text based on the screen resolution
	/// </summary>
	ScreenResolution
}

/// <summary>
/// Specifies the vertical alignment that will be used when rendering text
/// </summary>
public enum dfVerticalAlignment
{
	/// <summary>
	/// Text will be rendered at the top of the client area
	/// </summary>
	Top = 0,
	/// <summary>
	/// Text will be vertically centered within the client area
	/// </summary>
	Middle = 1,
	/// <summary>
	/// Text will be rendered at the bottom of the client area
	/// </summary>
	Bottom = 2
}

/// <summary>
/// Indicates the result of testing a dfControl for intersection against
/// a list of Planes.
/// </summary>
/// @class dfIntersectionType
public enum dfIntersectionType
{
	/// <summary>
	/// Control lies entirely outside of clipping region 
	/// </summary>
	None,
	/// <summary>
	/// Control lies entirely inside of clipping region 
	/// </summary>
	Inside,
	/// <summary>
	/// Control is intersected by one or more clipping planes
	/// </summary>
	Intersecting
}

/// <summary>
/// Specifies when a dfControl should display the on-screen keyboard 
/// on a mobile platform
/// </summary>
/// @class dfMobileKeyboardTrigger
public enum dfMobileKeyboardTrigger
{
	/// <summary>
	/// The dfControl will not automatically display the mobile keyboard
	/// </summary>
	Manual,
	/// <summary>
	/// The dfControl will show the mobile keyboard when it receives input focus
	/// </summary>
	ShowOnFocus,
	/// <summary>
	/// The dfControl will show the mobile keyboard when it is clicked
	/// </summary>
	ShowOnClick
}

[Flags]
public enum dfMouseButtons : int
{
	None = 0,
	Left = 1,
	Right = 2,
	Middle = 4
}

public static class dfMouseButtonsExtensions
{
	public static bool IsSet( this dfMouseButtons value, dfMouseButtons flag )
	{
		return flag == ( value & flag );
	}
}

/// <summary>
/// Specifies how the progress indicator on a dfProgressBar or dfSlider 
/// control will be sized.
/// </summary>
/// @class dfProgressFillMode
public enum dfProgressFillMode
{
	/// <summary>
	/// The progress indicator will be stretched
	/// </summary>
	Stretch,
	/// <summary>
	/// The progress indicator will use dfSprite Fill 
	/// </summary>
	Fill
}

/// <summary>
/// Specifies the orientation of controls or elements of controls
/// </summary>
/// @class dfControlOrientation
public enum dfControlOrientation : int
{
	/// <summary>
	/// The control or element is oriented horizontally
	/// </summary>
	Horizontal = 0,
	/// <summary>
	/// The control or element is oriented vertically
	/// </summary>
	Vertical = 1
}

/// <summary>
/// Indicates the state of the current Drag-and-Drop operation
/// </summary>
/// @class dfDragDropState
public enum dfDragDropState : int
{
	None = 0,
	Dragging = 1,
	Dropped = 2,
	Denied = 3,
	Cancelled = 4,
	CancelledNoTarget = 5
}

/// <summary>
/// Indicates the direction that a dfSprite will use for Fill operations
/// </summary>
/// @class dfFillDirection
public enum dfFillDirection : int
{
	Horizontal = 0,
	Vertical = 1
}

/// <summary>
/// When a control is anchored to an edge of its container, the distance between 
/// the control and the specified edge remains constant when the container resizes. 
/// For example, if a control is anchored to the right edge of its container, the 
/// distance between the right edge of the control and the right edge of the container 
/// remains constant when the container resizes. A control can be anchored to any 
/// combination of control edges. If the control is anchored to opposite edges of 
/// its container (for example, to the top and bottom), it resizes when the container 
/// resizes. If a control has its Anchor property set to AnchorStyle.None, control 
/// will behave as if its anchor was set to Anchor.Top | Anchor.Left if it is the
/// child of another control, or if it is a top-level control the behavior (in the
/// case of the screen resolution changing) is unspecified.
/// </summary>
/// @class dfAnchorStyle
[Flags]
public enum dfAnchorStyle : int
{
	/// <summary>The control is anchored to the top edge of its container.</summary>
	Top = 1,
	/// <summary>The control is anchored to the bottom edge of its container.</summary>
	Bottom = 2,
	/// <summary>The control is anchored to the left edge of its container.</summary>
	Left = 4,
	/// <summary>The control is anchored to the right edge of its container.</summary>
	Right = 8,
	/// <summary>The control is anchored to all edges of its container</summary>
	All = Left | Top | Right | Bottom,
	/// <summary>The control will be horizontally centered within its container</summary>
	CenterHorizontal = 64,
	/// <summary>The control will be vertically centered within its container</summary>
	CenterVertical = 128,
	/// <summary>The control layout represents proportional dimensions</summary>
	Proportional = 256,
	/// <summary>The control is not anchored to any edges of its container.</summary>
	None = 0
}

/// <summary>
/// Represents the distance between the control's edges and the corresponding
/// edges of the control's container. This information is used to dynamically 
/// resize controls that have an Anchor Layout defined.
/// </summary>
/// @class dfAnchorMargins
[Serializable]
public class dfAnchorMargins
{

	/// <summary>
	/// Represents the distance between the left edge of the <see cref="dfControl"/>
	/// and the left edge of its container
	/// </summary>
	[SerializeField]
	public float left;

	/// <summary>
	/// Represents the distance between the top edge of the <see cref="dfControl"/>
	/// and the top edge of its container
	/// </summary>
	[SerializeField]
	public float top;

	/// <summary>
	/// Represents the distance between the right edge of the <see cref="dfControl"/>
	/// and the right edge of its container
	/// </summary>
	[SerializeField]
	public float right;

	/// <summary>
	/// Represents the distance between the bottom edge of the <see cref="dfControl"/>
	/// and the bottom edge of its container
	/// </summary>
	[SerializeField]
	public float bottom;

	/// <summary>
	/// Returns a formatted string summarizing this object's state
	/// </summary>
	public override string ToString()
	{
		return string.Format( "[L:{0},T:{1},R:{2},B:{3}]", left, top, right, bottom );
	}

}

public static class AnchorStyleExtensions
{
	public static bool IsFlagSet( this dfAnchorStyle value, dfAnchorStyle flag )
	{
		return flag == ( value & flag );
	}
	public static bool IsAnyFlagSet( this dfAnchorStyle value, dfAnchorStyle flag )
	{
		return 0 != ( value & flag );
	}
	public static dfAnchorStyle SetFlag( this dfAnchorStyle value, dfAnchorStyle flag )
	{
		return value | flag;
	}
	public static dfAnchorStyle SetFlag( this dfAnchorStyle value, dfAnchorStyle flag, bool on )
	{
		if( on )
			return value | flag;
		else
			return value & ~flag;
	}
}

/// <summary>
/// Indicates the axes that will be flipped when a sprite is rendered
/// </summary>
/// @class dfSpriteFlip
[Flags]
public enum dfSpriteFlip : int
{
	/// <summary>
	/// Not flipped
	/// </summary>
	None = 0,
	/// <summary>
	/// Flip along the horizontal axis
	/// </summary>
	FlipHorizontal = 1,
	/// <summary>
	/// Flip along the vertical axis
	/// </summary>
	FlipVertical = 2
}

public static class dfSpriteFlipExtensions
{
	public static bool IsSet( this dfSpriteFlip value, dfSpriteFlip flag )
	{
		return flag == ( value & flag );
	}
	public static dfSpriteFlip SetFlag( this dfSpriteFlip value, dfSpriteFlip flag, bool on )
	{
		if( on )
			return value | flag;
		else
			return value & ~flag;
	}
}

/// <summary>
/// Used to indicate the "origin" or pivot point of a control or render element.
/// Controls will rotate around this point, and resize operations will resize 
/// away from this point.
/// </summary>
/// @class dfPivotPoint
public enum dfPivotPoint : int
{
	TopLeft = 0,
	TopCenter = 1,
	TopRight = 2,
	MiddleLeft = 3,
	MiddleCenter = 4,
	MiddleRight = 5,
	BottomLeft = 6,
	BottomCenter = 7,
	BottomRight = 8
}

public static class dfPivotExtensions
{

	/// <summary>
	/// Returns the pivot as an offset where 0,0 corresponds to the top-left corner
	/// of the control and 1,1 corresponds to the bottom right
	/// </summary>
	/// <param name="?"></param>
	/// <returns></returns>
	public static Vector2 AsOffset( this dfPivotPoint pivot )
	{
		switch( pivot )
		{
			case dfPivotPoint.TopLeft:
				return Vector2.zero;
			case dfPivotPoint.TopCenter:
				return new Vector2( 0.5f, 0 );
			case dfPivotPoint.TopRight:
				return new Vector2( 1f, 0 );
			case dfPivotPoint.MiddleLeft:
				return new Vector2( 0, 0.5f );
			case dfPivotPoint.MiddleCenter:
				return new Vector2( 0.5f, 0.5f );
			case dfPivotPoint.MiddleRight:
				return new Vector2( 1f, 0.5f );
			case dfPivotPoint.BottomLeft:
				return new Vector2( 0, 1f );
			case dfPivotPoint.BottomCenter:
				return new Vector2( 0.5f, 1f );
			case dfPivotPoint.BottomRight:
				return new Vector2( 1f, 1f );
			default:
				return Vector2.zero;
		}
	}

	/// <summary>
	/// Returns the value needed to translate the control's Transform.localposition
	/// value to the center of the control
	/// </summary>
	/// <param name="pivot"></param>
	/// <param name="size"></param>
	/// <returns></returns>
	public static Vector3 TransformToCenter( this dfPivotPoint pivot, Vector2 size )
	{

		switch( pivot )
		{

			case dfPivotPoint.TopLeft:
				return new Vector2( 0.5f * size.x, 0.5f * -size.y );
			case dfPivotPoint.TopCenter:
				return new Vector2( 0f, 0.5f * -size.y );
			case dfPivotPoint.TopRight:
				return new Vector2( 0.5f * -size.x, 0.5f * -size.y );

			case dfPivotPoint.MiddleLeft:
				return new Vector2( 0.5f * size.x, 0f );
			case dfPivotPoint.MiddleCenter:
				return new Vector2( 0f, 0f );
			case dfPivotPoint.MiddleRight:
				return new Vector2( 0.5f * -size.x, 0f );

			case dfPivotPoint.BottomLeft:
				return new Vector2( 0.5f * size.x, 0.5f * size.y );
			case dfPivotPoint.BottomCenter:
				return new Vector2( 0f, 0.5f * size.y );
			case dfPivotPoint.BottomRight:
				return new Vector2( 0.5f * -size.x, 0.5f * size.y );

		}

		throw new Exception( "Unhandled " + pivot.GetType().Name + " value: " + pivot );

	}

	public static Vector3 UpperLeftToTransform( this dfPivotPoint pivot, Vector2 size )
	{
		return TransformToUpperLeft( pivot, size ).Scale( -1, -1, 1 );
	}

	/// <summary>
	/// Returns the offset of the control's upper left corner with respect to 
	/// the GameObject's Transform.localposition value
	/// </summary>
	/// <param name="pivot"></param>
	/// <returns></returns>
	public static Vector3 TransformToUpperLeft( this dfPivotPoint pivot, Vector2 size )
	{

		switch( pivot )
		{

			case dfPivotPoint.TopLeft:
				return new Vector2( 0f, 0f );
			case dfPivotPoint.TopCenter:
				return new Vector2( 0.5f * -size.x, 0f );
			case dfPivotPoint.TopRight:
				return new Vector2( -size.x, 0f );

			case dfPivotPoint.MiddleLeft:
				return new Vector2( 0f, 0.5f * size.y );
			case dfPivotPoint.MiddleCenter:
				return new Vector2( 0.5f * -size.x, 0.5f * size.y );
			case dfPivotPoint.MiddleRight:
				return new Vector2( -size.x, 0.5f * size.y );

			case dfPivotPoint.BottomLeft:
				return new Vector2( 0, size.y );
			case dfPivotPoint.BottomCenter:
				return new Vector2( 0.5f * -size.x, size.y );
			case dfPivotPoint.BottomRight:
				return new Vector2( -size.x, size.y );

		}

		throw new Exception( "Unhandled " + pivot.GetType().Name + " value: " + pivot );

	}

}

