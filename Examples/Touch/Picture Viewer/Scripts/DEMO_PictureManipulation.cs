using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DEMO_PictureManipulation : MonoBehaviour 
{

	private dfTextureSprite control;
	private bool isMouseDown = false;

	#region Unity messages 

	public void Start()
	{
		this.control = GetComponent<dfTextureSprite>();
	}

	#endregion 

	#region Control events 

	protected void OnMouseUp()
	{
		isMouseDown = false;
	}

	protected void OnMouseDown()
	{
		isMouseDown = true;
		control.BringToFront();
	}

	#endregion 

	#region Gesture handlers

	public IEnumerator OnFlickGesture( dfGestureBase gesture )
	{
		return handleMomentum( gesture );
	}

	public IEnumerator OnDoubleTapGesture()
	{
		return resetZoom();
	}

	public void OnRotateGestureBegin( dfRotateGesture gesture )
	{ 
		rotateAroundPoint( gesture.StartPosition, gesture.AngleDelta * 0.5f );
	}

	public void OnRotateGestureUpdate( dfRotateGesture gesture )
	{
		rotateAroundPoint( gesture.CurrentPosition, gesture.AngleDelta * 0.5f );
	}

	public void OnResizeGestureUpdate( dfResizeGesture gesture )
	{
		const float scale = 1f;
		zoomToPoint( gesture.StartPosition, gesture.SizeDelta * scale );
	}

	public void OnPanGestureStart( dfPanGesture gesture )
	{
		control.BringToFront();
		control.RelativePosition += (Vector3)gesture.Delta.Scale( 1, -1 );
	}

	public void OnPanGestureMove( dfPanGesture gesture )
	{
		control.RelativePosition += (Vector3)gesture.Delta.Scale( 1, -1 );
	}

	public void OnMouseWheel( dfControl sender, dfMouseEventArgs args )
	{
		zoomToPoint( args.Position, Mathf.Sign( args.WheelDelta ) * 75 );
	}

	#endregion 

	#region Private utility methods 

	private IEnumerator resetZoom()
	{

		var controlSize = control.Size;
		var screenSize = control.GetManager().GetScreenSize();
		var animatedSize = new dfAnimatedVector2( control.Size, controlSize, 0.2f );

		if( controlSize.x >= screenSize.x - 10 || controlSize.y >= screenSize.y - 10 )
		{
			animatedSize.EndValue = fitImage( screenSize.x * 0.75f, screenSize.y * 0.75f, control.Width, control.Height );
		}
		else
		{
			animatedSize.EndValue = fitImage( screenSize.x, screenSize.y, control.Width, control.Height );
		}

		var endPosition = new Vector3( screenSize.x - animatedSize.EndValue.x, screenSize.y - animatedSize.EndValue.y, 0 ) * 0.5f;
		var animatedPosition = new dfAnimatedVector3( control.RelativePosition, endPosition, animatedSize.Length );
		var animatedRotation = new dfAnimatedQuaternion( control.transform.rotation, Quaternion.identity, animatedSize.Length );

		while( !animatedSize.IsDone )
		{

			control.Size = animatedSize;
			control.RelativePosition = animatedPosition;
			control.transform.rotation = animatedRotation;

			yield return null;

		}

	}

	private IEnumerator handleMomentum( dfGestureBase gesture )
	{

		isMouseDown = false;

		const float SPEED = 10f;

		var direction = (Vector3)( gesture.CurrentPosition - gesture.StartPosition ) * control.PixelsToUnits();
		var planes = GeometryUtility.CalculateFrustumPlanes( control.GetCamera() );

		var startTime = Time.realtimeSinceStartup;
		while( !isMouseDown )
		{

			var timeNow = Time.realtimeSinceStartup;
			var elapsed = timeNow - startTime;
			if( elapsed > 1f )
				break;

			control.transform.position += direction * Time.deltaTime * SPEED * ( 1f - elapsed );

			yield return null;

		}

		if( !GeometryUtility.TestPlanesAABB( planes, control.collider.bounds ) )
		{

			control.enabled = false;
			DestroyImmediate( gameObject );

		}

	}

	private void rotateAroundPoint( Vector2 point, float delta )
	{

		var transform = control.transform;
		var corners = control.GetCorners();

		var plane = new Plane( corners[ 0 ], corners[ 1 ], corners[ 2 ] );
		var ray = control.GetCamera().ScreenPointToRay( point );
		var distance = 0f;
		plane.Raycast( ray, out distance );
		var worldAnchor = ray.GetPoint( distance );

		var deltaAngle = new Vector3( 0, 0, delta );
		var angles = ( transform.eulerAngles + deltaAngle );

		var newPos = rotatePointAroundPivot( transform.position, worldAnchor, deltaAngle );
		transform.position = newPos;
		transform.eulerAngles = angles;

	}

	private Vector3 rotatePointAroundPivot( Vector3 point, Vector3 pivot, Vector3 angles )
	{
		var dir = Quaternion.Euler( angles ) * ( point - pivot );
		return dir + pivot; 
	}

	private void zoomToPoint( Vector2 point, float delta )
	{

		var transform = control.transform;
		var corners = control.GetCorners();
		var p2u = control.PixelsToUnits();

		var plane = new Plane( corners[ 0 ], corners[ 1 ], corners[ 2 ] );
		var ray = control.GetCamera().ScreenPointToRay( point );
		var distance = 0f;
		plane.Raycast( ray, out distance );
		var worldAnchor = ray.GetPoint( distance );

		var prevSize = control.Size * p2u;
		var prevOffset = ( transform.position - worldAnchor );
		var relativeOffset = new Vector3( prevOffset.x / prevSize.x, prevOffset.y / prevSize.y );

		var aspect = control.Height / control.Width;
		var newWidth = control.Width + delta;
		var newHeight = newWidth * aspect;

		if( newWidth < 256 || newHeight < 256 )
			return;

		control.Size = new Vector2( newWidth, newHeight );

		var newOffset = new Vector3(
			control.Width * relativeOffset.x * p2u,
			control.Height * relativeOffset.y * p2u,
			prevOffset.z
		);

		transform.position += ( newOffset - prevOffset );

	}

	private static Vector2 fitImage( float maxWidth, float maxHeight, float imageWidth, float imageHeight )
	{

		float widthScale = maxWidth / imageWidth;
		float heightScale = maxHeight / imageHeight;
		float scale = Mathf.Min( widthScale, heightScale );

		return new Vector2( Mathf.Floor( imageWidth * scale ), Mathf.Ceil( imageHeight * scale ) );

	}
	
	#endregion 

}
