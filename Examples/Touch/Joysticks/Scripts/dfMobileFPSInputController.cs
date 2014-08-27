using UnityEngine;
using System.Collections;

[RequireComponent( typeof( dfCharacterMotorCS ) )]
public class dfMobileFPSInputController : MonoBehaviour
{

	public string joystickID = "LeftJoystick";

	private dfCharacterMotorCS motor;

	void Awake()
	{
		motor = GetComponent<dfCharacterMotorCS>();
	}

	void Update()
	{

		Vector2 leftStick = dfTouchJoystick.GetJoystickPosition( joystickID );

		var directionVector = new Vector3( leftStick.x, 0, leftStick.y );

		if( directionVector != Vector3.zero )
		{

			// Get the length of the directon vector and then normalize it
			// Dividing by the length is cheaper than normalizing when we already have the length anyway
			var directionLength = directionVector.magnitude;
			directionVector = directionVector / directionLength;

			// Make sure the length is no bigger than 1
			directionLength = Mathf.Min( 1, directionLength );

			// Make the input vector more sensitive towards the extremes and less sensitive in the middle
			// This makes it easier to control slow speeds when using analog sticks
			directionLength = directionLength * directionLength;

			// Multiply the normalized direction vector by the modified length
			directionVector = directionVector * directionLength;

		}

		// Apply the direction to the CharacterMotor
		motor.inputMoveDirection = transform.rotation * directionVector;

	}
}