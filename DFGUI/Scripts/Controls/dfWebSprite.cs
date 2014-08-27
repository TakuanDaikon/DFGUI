/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityMaterial = UnityEngine.Material;

/// <summary>
/// Downloads an image from a web URL and displays it on-screen like a sprite
/// </summary>
[dfCategory( "Basic Controls" )]
[dfTooltip( "Downloads an image from a web URL and displays it on-screen like a sprite" )]
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_web_sprite.html" )]
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Sprite/Web" )]
public class dfWebSprite : dfTextureSprite
{

	#region Public events

	public PropertyChangedEventHandler<Texture> DownloadComplete;

	public PropertyChangedEventHandler<string> DownloadError;

	#endregion

	#region Protected serialized fields

	[SerializeField]
	protected string url = "";

	[SerializeField]
	protected Texture2D loadingImage;

	[SerializeField]
	protected Texture2D errorImage;

	[SerializeField]
	protected bool autoDownload = true;

	#endregion

	#region Public properties

	/// <summary>
	/// Gets/Sets the URL that will be used to retrieve the Texture to display
	/// </summary>
	public string URL
	{
		get { return this.url; }
		set
		{
			if( value != this.url )
			{
				this.url = value;
				if( Application.isPlaying && AutoDownload )
				{
					LoadImage();
				}
			}
		}
	}

	/// <summary>
	/// Gets/Sets the <see cref="UnityEngine.Texture2D"/> that will be displayed
	/// until the web image is downloaded
	/// </summary>
	public Texture2D LoadingImage
	{
		get { return this.loadingImage; }
		set { this.loadingImage = value; }
	}

	/// <summary>
	/// Gets/Sets the <see cref="UnityEngine.Texture2D"/> that will be displayed
	/// if there is an error downloading the desired image from the web
	/// </summary>
	public Texture2D ErrorImage
	{
		get { return this.errorImage; }
		set { this.errorImage = value; }
	}

	/// <summary>
	/// Gets or sets whether images will be automatically downloaded at startup
	/// </summary>
	public bool AutoDownload
	{
		get { return this.autoDownload; }
		set { this.autoDownload = value; }
	}

	#endregion

	#region Unity events

	public override void OnEnable()
	{

		base.OnEnable();

		if( Texture == null )
		{
			Texture = this.LoadingImage;
		}

		if( Texture == null && AutoDownload && Application.isPlaying )
		{
			LoadImage();
		}

	}

	#endregion

	#region Public methods

	public void LoadImage()
	{
		StopAllCoroutines();
		StartCoroutine( downloadTexture() );
	}

	#endregion

	#region Private utility methods

	private IEnumerator downloadTexture()
	{

		this.Texture = this.loadingImage;

		if( string.IsNullOrEmpty( this.url ) )
			yield break;

		using( var request = new WWW( this.url ) )
		{

			yield return request;

			if( !string.IsNullOrEmpty( request.error ) )
			{

				this.Texture = this.errorImage ?? this.loadingImage;

				if( DownloadError != null )
				{
					DownloadError( this, request.error );
				}

				Signal( "OnDownloadError", this, request.error );

			}
			else
			{

				this.Texture = request.texture;

				if( DownloadComplete != null )
				{
					DownloadComplete( this, this.Texture );
				}

				Signal( "OnDownloadComplete", this, this.Texture );

			}

		}

	}

	#endregion

}
