using System;

public enum dfGestureState
{
	/// <summary> Gesture is not currently active </summary>
	None,
	/// <summary> Gesture is currently trying to determine if its conditions can be satisfied </summary>
	Possible,
	/// <summary> Gesture's conditions have been satisfied, gesture is currently active </summary>
	Began,
	/// <summary> Gesture's conditions have been satisfied, gesture is currently active and has been updated </summary>
	Changed,
	/// <summary> Gesture has ended </summary>
	Ended,
	/// <summary> Gesture has been cancelled </summary>
	Cancelled,
	/// <summary> The gesture's required conditions were not satisfied </summary>
	Failed
}
