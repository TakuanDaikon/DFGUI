using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DEMO_PictureSelector : MonoBehaviour
{

	public dfTextureSprite DisplayImage;

	protected dfTextureSprite myImage;

	public void OnEnable()
	{
		myImage = GetComponent<dfTextureSprite>();
	}

	public IEnumerator OnDoubleTapGesture()
	{

		if( DisplayImage == null )
		{
			Debug.LogWarning( "The DisplayImage property is not configured, cannot select the image" );
			yield break;
		}

		var photo = ( (GameObject)Instantiate( DisplayImage.gameObject ) ).GetComponent<dfTextureSprite>();

		myImage.GetManager().AddControl( photo );

		photo.Texture = myImage.Texture;
		photo.Size = myImage.Size;
		photo.RelativePosition = myImage.GetAbsolutePosition();
		photo.transform.rotation = Quaternion.identity;
		photo.BringToFront();
		photo.Opacity = 1f;
		photo.IsVisible = true;

		var screenSize = myImage.GetManager().GetScreenSize();
		var fullSize = new Vector2( photo.Texture.width, photo.Texture.height );
		var displaySize = fitImage( screenSize.x * 0.75f, screenSize.y * 0.75f, fullSize.x, fullSize.y );
		var displayPosition = new Vector3( ( screenSize.x - displaySize.x ) * 0.5f, ( screenSize.y - displaySize.y ) * 0.5f );

		var animatedPosition = new dfAnimatedVector3( myImage.GetAbsolutePosition(), displayPosition, 0.2f );
		var animatedSize = new dfAnimatedVector2( myImage.Size, displaySize, 0.2f );

		while( !animatedPosition.IsDone || !animatedSize.IsDone )
		{

			photo.Size = animatedSize;
			photo.RelativePosition = animatedPosition;

			yield return null;

		}

	}

	private static Vector2 fitImage( float maxWidth, float maxHeight, float imageWidth, float imageHeight )
	{

		float widthScale = maxWidth / imageWidth;
		float heightScale = maxHeight / imageHeight;
		float scale = Mathf.Min( widthScale, heightScale );

		return new Vector2( Mathf.Floor( imageWidth * scale ), Mathf.Ceil( imageHeight * scale ) );

	}

}
