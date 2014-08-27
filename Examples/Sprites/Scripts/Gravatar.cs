using UnityEngine;

using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/Game Menu/Gravatar" )]
[Serializable]
public class Gravatar : MonoBehaviour
{

	private static Regex validator = new Regex( @"^[a-zA-Z][\w\.-]*[a-zA-Z0-9]@[a-zA-Z0-9][\w\.-]*[a-zA-Z0-9]\.[a-zA-Z][a-zA-Z\.]*[a-zA-Z]$", RegexOptions.IgnoreCase );

	public dfWebSprite Sprite;
	
	[SerializeField]
	protected string email = "";

	void OnEnable()
	{

#if UNITY_WP8 || UNITY_METRO
		Debug.LogError( "The " + this.GetType().Name + " example class does not work on the target platform" );
		this.enabled = false;
		return;
#else
		if( validator.IsMatch( this.email ) && this.Sprite != null )
		{
			updateImage();
		}
#endif

	}

	public string EmailAddress
	{
		get { return this.email; }
		set
		{
			if( value != this.email )
			{
				this.email = value;
				updateImage();
			}
		}
	}

	private void updateImage()
	{

		if( Sprite == null )
			return;

		if( validator.IsMatch( this.email ) )
		{
			var hash = MD5( this.email.Trim().ToLower() );
			Sprite.URL = string.Format( "http://www.gravatar.com/avatar/{0}", hash );
		}
		else
		{
			Sprite.Texture = Sprite.LoadingImage;
		}

	}

	public string MD5( string strToEncrypt )
	{

#if UNITY_WP8 ||  UNITY_METRO
		return string.Empty;
#else

		System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
		byte[] bytes = ue.GetBytes( strToEncrypt );

		// encrypt bytes
		System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		byte[] hashBytes = md5.ComputeHash( bytes );

		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";

		for( int i = 0; i < hashBytes.Length; i++ )
		{
			hashString += System.Convert.ToString( hashBytes[ i ], 16 ).PadLeft( 2, '0' );
		}

		return hashString.PadLeft( 32, '0' );
#endif

	}

}
