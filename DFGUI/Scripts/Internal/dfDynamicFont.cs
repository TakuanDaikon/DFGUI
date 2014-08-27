/* Copyright 2013-2014 Daikon Forge */

/****************************************************************************
 * PLEASE NOTE: The code in this file is under extremely active development
 * and is likely to change quite frequently. It is not recommended to modify
 * the code in this file, as your changes are likely to be overwritten by
 * the next product update when it is published.
 * **************************************************************************/

using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityMaterial = UnityEngine.Material;
using UnityFont = UnityEngine.Font;
using UnityShader = UnityEngine.Shader;

#region Documentation
/// <summary>
/// The dfDynamicFont class is used to render TrueType or OpenType fonts
/// in your user interface. 
/// You can create a dfDynamicFont by following this tutorial - http://www.youtube.com/embed/Tx4NIIK5sWo
/// </summary>
#endregion
[ExecuteInEditMode]
[Serializable]
[AddComponentMenu( "Daikon Forge/User Interface/Dynamic Font" )]
public class dfDynamicFont : dfFontBase
{

	#region Static variables

	private static List<dfDynamicFont> loadedFonts = new List<dfDynamicFont>();

	#endregion

	#region Serialized private fields

	[SerializeField]
	private UnityFont baseFont;

	[SerializeField]
	private UnityMaterial material;

	[SerializeField]
	private UnityShader shader;

	[SerializeField]
	private int baseFontSize = -1;

	[SerializeField]
	private int baseline = -1;

	[SerializeField]
	private int lineHeight = 0;

	#endregion

	#region Private runtime variables

	protected dfList<FontCharacterRequest> requests = new dfList<FontCharacterRequest>();

	#endregion

	#region dfFontBase overrides

	/// <summary>
	/// Gets or sets the Material that will be used to render text
	/// </summary>
	public override UnityMaterial Material
	{
		get
		{
			if( this.baseFont != null && this.material != null )
			{
				this.material.mainTexture = this.baseFont.material.mainTexture;
				this.material.shader = Shader;
			}
			return this.material;
		}
		set
		{
			if( value != this.material )
			{
				this.material = value;
				dfGUIManager.RefreshAll();
			}
		}
	}

	/// <summary>
	/// Gets or sets a reference to the shader that will be used to render the text
	/// </summary>
	public UnityShader Shader
	{
		get 
		{
			if( this.shader == null )
				this.shader = Shader.Find( "Daikon Forge/Dynamic Font Shader" );
			return this.shader;
		}
		set
		{
			this.shader = value;
			dfGUIManager.RefreshAll();
		}
	}

	/// <summary>
	/// Returns a reference to the texture which contains the
	/// glyph images that will be used to render text
	/// </summary>
	public override Texture Texture
	{
		get { return this.baseFont.material.mainTexture; }
	}

	/// <summary>
	/// Returns a value indicating whether the font configuration is valid
	/// </summary>
	public override bool IsValid
	{
		get
		{
			return
				this.baseFont != null &&
				this.Material != null &&
				this.Texture != null;
		}
	}

	/// <summary>
	/// Gets or sets the base font size that the Font is set to. This value
	/// *must* match the [Font Size] value of the Font, as it is used during
	/// rendering to calculate the vertical offset of each character.
	/// </summary>
	public override int FontSize
	{
		get { return this.baseFontSize; }
		set
		{
			if( value != this.baseFontSize )
			{
				this.baseFontSize = value;
				dfGUIManager.RefreshAll();
			}
		}
	}

	/// <summary>
	/// Gets or sets the default minimum height of each line of rendered text
	/// </summary>
	public override int LineHeight
	{
		get { return this.lineHeight; }
		set
		{
			if( value != this.lineHeight )
			{
				this.lineHeight = value;
				dfGUIManager.RefreshAll();
			}
		}
	}

	public override dfFontRendererBase ObtainRenderer()
	{
		return DynamicFontRenderer.Obtain( this );
	}

	#endregion

	#region Public properties

	/// <summary>
	/// Gets or sets the TrueType or OpenType baseFont that will be used to render
	/// any text using this dfDynamicFont
	/// </summary>
	public UnityFont BaseFont
	{
		get { return this.baseFont; }
		set
		{
			if( value != this.baseFont )
			{
				this.baseFont = value;
				dfGUIManager.RefreshAll();
			}
		}
	}

	public int Baseline
	{
		get { return this.baseline; }
		set
		{
			if( value != this.baseline )
			{
				this.baseline = value;
				dfGUIManager.RefreshAll();
			}
		}
	}

	public int Descent
	{
		get { return this.LineHeight - this.baseline; }
	}

	#endregion

	#region Static methods

	public static dfDynamicFont FindByName( string name )
	{

		for( int i = 0; i < loadedFonts.Count; i++ )
		{
			if( string.Equals( loadedFonts[ i ].name, name, StringComparison.OrdinalIgnoreCase ) )
				return loadedFonts[ i ];
		}

		var asset = Resources.Load( name ) as GameObject;
		if( asset == null )
		{
			return null;
		}

		var newFont = asset.GetComponent<dfDynamicFont>();
		if( newFont == null )
		{
			return null;
		}

		loadedFonts.Add( newFont );

		return newFont;

	}

	#endregion

	#region Public methods

	/// <summary>
	/// Returns a Vector2 structure containing the width and height that would be
	/// required to render a single line of text at the given size and with the given style
	/// </summary>
	/// <param name="text">The single line of text to measure</param>
	/// <param name="size">The desired font size</param>
	/// <param name="style">The desired text style</param>
	/// <returns></returns>
	public Vector2 MeasureText( string text, int size, FontStyle style )
	{

		RequestCharacters( text, size, style );

		var multiplier = (float)size / (float)FontSize;

		var lineHeight = Mathf.CeilToInt( Baseline * multiplier );
		var measuredSize = new Vector2( 0, lineHeight );

		UnityEngine.CharacterInfo glyph = new UnityEngine.CharacterInfo();

		for( int i = 0; i < text.Length; i++ )
		{

			BaseFont.GetCharacterInfo( text[ i ], out glyph, size, style );

			var width = Mathf.Ceil( glyph.vert.x + glyph.vert.width );

			if( text[ i ] == ' ' )
			{
				width = Mathf.Ceil( glyph.width );
			}
			else if( text[ i ] == '\t' )
			{
				width += size * 4;
			}

			measuredSize.x += width;

		}

		return measuredSize;

	}

	/// <summary>
	/// Ensures that the requested characters are available in the Unity Font's
	/// texture atlas at the given size and style. 
	/// </summary>
	/// <param name="text">The set of characters that will be rendered</param>
	/// <param name="size">The maximum height, in pixels, of the text to be rendered</param>
	/// <param name="style">The style of text that will be rendered</param>
	public void RequestCharacters( string text, int size, FontStyle style )
	{

		if( this.baseFont == null )
			throw new NullReferenceException( "Base Font not assigned: " + this.name );

		// Ensure that the font manager knows that there is a possibility that this 
		// font's atlas has been changed
		dfFontManager.Invalidate( this );

		// Request the characters and retrieve the glyph data
		this.baseFont.RequestCharactersInTexture( text, size, style );

	}

	/// <summary>
	/// Request characters to be added to the font texture in the indicated size and font style
	/// </summary>
	public virtual void AddCharacterRequest( string characters, int fontSize, FontStyle style )
	{

		dfFontManager.FlagPendingRequests( this );

		var request = FontCharacterRequest.Obtain();
		request.Characters = characters;
		request.FontSize = fontSize;
		request.FontStyle = style;

		requests.Add( request );

	}

	/// <summary>
	/// Issues all pending character requests (added by AddCharacterRequest)
	/// </summary>
	public virtual void FlushCharacterRequests()
	{

		for( int i = 0; i < requests.Count; i++ )
		{
			var request = requests[ i ];
			this.baseFont.RequestCharactersInTexture( request.Characters, request.FontSize, request.FontStyle );
		}

		requests.ReleaseItems();

	}

	#endregion

	#region Nested classes


	/// <summary>
	/// Represents a request for characters in a font
	/// </summary>
	protected class FontCharacterRequest : IPoolable
	{

		#region Object pooling

		private static dfList<FontCharacterRequest> pool = new dfList<FontCharacterRequest>();

		public static FontCharacterRequest Obtain()
		{
			return ( pool.Count > 0 ) ? pool.Pop() : new FontCharacterRequest();
		}

		public void Release()
		{

			Characters = null;
			FontSize = 0;
			FontStyle = UnityEngine.FontStyle.Normal;

			pool.Add( this );

		}

		#endregion

		#region Public fields

		/// <summary>
		/// The characters to request
		/// </summary>
		public string Characters;

		/// <summary>
		/// The font size requested
		/// </summary>
		public int FontSize;

		/// <summary>
		/// The font style requested
		/// </summary>
		public FontStyle FontStyle;

		#endregion

	}

	public class DynamicFontRenderer : dfFontRendererBase, IPoolable
	{

		#region Object pooling

		private static Queue<DynamicFontRenderer> objectPool = new Queue<DynamicFontRenderer>();

		#endregion

		#region Static variables and constants

		private static Vector2[] OUTLINE_OFFSETS = new Vector2[] 
		{
			new Vector2( -1, -1 ),
			new Vector2( -1, 1 ),
			new Vector2( 1, -1 ),
			new Vector2( 1, 1 )
		};

		private static int[] TRIANGLE_INDICES = new int[] { 0, 1, 3, 3, 1, 2 };

		private static Stack<Color32> textColors = new Stack<Color32>();

		#endregion

		#region Public properties

		/// <summary>
		/// Returns a count of lines of text that were rendered
		/// </summary>
		public int LineCount { get { return lines.Count; } }

		/// <summary>
		/// Gets or sets a reference to the dfAtlas instance that contains
		/// any sprites that will be rendered
		/// </summary>
		public dfAtlas SpriteAtlas { get; set; }

		/// <summary>
		/// Gets or sets a reference to the dfRenderData buffer that will 
		/// hold any sprites which are rendered
		/// </summary>
		public dfRenderData SpriteBuffer { get; set; }

		#endregion

		#region Private instance fields

		private dfList<LineRenderInfo> lines = null;
		private dfList<dfMarkupToken> tokens = null;

		private bool inUse = false;

		#endregion

		#region Constructors

		internal DynamicFontRenderer()
		{
		}

		#endregion

		#region Object pooling

		public static dfFontRendererBase Obtain( dfDynamicFont font )
		{

			var renderer = objectPool.Count > 0 ? objectPool.Dequeue() : new DynamicFontRenderer();
			renderer.Reset();
			renderer.Font = font;
			renderer.inUse = true;

			return renderer;

		}

		public override void Release()
		{

			if( !inUse )
				return;

			inUse = false;

			this.Reset();

			if( this.tokens != null )
			{
				this.tokens.Release();
				this.tokens = null;
			}

			if( lines != null )
			{
				lines.ReleaseItems();
				lines.Release();
				lines = null;
			}

			this.BottomColor = (Color32?)null;

			objectPool.Enqueue( this );

		}

		#endregion

		#region Public methods

		/// <summary>
		/// Returns an array of float values, each one corresponding 
		/// to the width of the character at the same position of the 
		/// source text. NOTE: Does not do any markup processing, and
		/// must only be used on single-line plaintext.
		/// </summary>
		public override float[] GetCharacterWidths( string text )
		{
			var totalWidth = 0f;
			return GetCharacterWidths( text, 0, text.Length - 1, out totalWidth );
		}

		/// <summary>
		/// Returns an array of float values, each one corresponding 
		/// to the width of the character at the same position of the 
		/// source text. NOTE: Does not do any markup processing, and
		/// must only be used on single-line plaintext.
		/// </summary>
		public float[] GetCharacterWidths( string text, int startIndex, int endIndex, out float totalWidth )
		{

			totalWidth = 0f;

			var font = (dfDynamicFont)Font;
			var fontSize = Mathf.CeilToInt( font.FontSize * TextScale );

			var output = new float[ text.Length ];

			var last = 0f;
			var maxWidth = 0f;

			font.RequestCharacters( text, fontSize, FontStyle.Normal );
			UnityEngine.CharacterInfo glyph = new UnityEngine.CharacterInfo();

			for( int i = startIndex; i <= endIndex; i++, last = maxWidth )
			{

				if( !font.BaseFont.GetCharacterInfo( text[ i ], out glyph, fontSize, FontStyle.Normal ) )
					continue;

				if( text[ i ] == '\t' )
				{
					maxWidth += TabSize;
				}
				else if( text[ i ] == ' ' )
				{
					maxWidth += glyph.width;
				}
				else
				{
					maxWidth += ( glyph.vert.x + glyph.vert.width );
				}

				output[ i ] = ( maxWidth - last ) * PixelRatio;

			}

			return output;

		}

		/// <summary>
		/// Measures the given text and returns the size (in pixels) required 
		/// to render the text.
		/// </summary>
		/// <param name="text">The text to be measured</param>
		/// <returns>The size required to render the text</returns>
		public override Vector2 MeasureString( string text )
		{

			// Ensure that the required characters can be found in the dynamic font's
			// character data.
			var font = (dfDynamicFont)Font;
			var fontSize = Mathf.CeilToInt( font.FontSize * TextScale );
			font.RequestCharacters( text, fontSize, FontStyle.Normal );

			tokenize( text );
			var lines = calculateLinebreaks();

			var totalWidth = 0f;
			var totalHeight = 0f;

			for( int i = 0; i < lines.Count; i++ )
			{
				totalWidth = Mathf.Max( lines[ i ].lineWidth, totalWidth );
				totalHeight += lines[ i ].lineHeight;
			}

			this.tokens.Release();
			this.tokens = null;

			var result = new Vector2( totalWidth, totalHeight );

			return result;

		}

		/// <summary>
		/// Render the given text as mesh data to the given destination buffer
		/// </summary>
		/// <param name="text">The text to be rendered</param>
		/// <param name="destination">The dfRenderData buffer that will hold the 
		/// text mesh information</param>
		public override void Render( string text, dfRenderData destination )
		{

			//@Profiler.BeginSample( "Render dynamic font text" );

			textColors.Clear();
			textColors.Push( Color.white );

			// Ensure that the required characters can be found in the dynamic font's
			// character data.
			var font = (dfDynamicFont)Font;
			var fontSize = Mathf.CeilToInt( font.FontSize * TextScale );
			font.RequestCharacters( text, fontSize, FontStyle.Normal );

			tokenize( text );
			var lines = calculateLinebreaks();

			destination.EnsureCapacity( getAnticipatedVertCount( tokens ) );

			var maxWidth = 0;
			var maxHeight = 0;

			var position = ( VectorOffset / PixelRatio ).CeilToInt();

			for( int i = 0; i < lines.Count; i++ )
			{

				var line = lines[ i ];
				var lineStartIndex = destination.Vertices.Count;
				var spriteStartIndex = ( SpriteBuffer != null ) ? SpriteBuffer.Vertices.Count : 0;

				renderLine( lines[ i ], textColors, position, destination );

				position.y -= line.lineHeight;

				maxWidth = Mathf.Max( (int)line.lineWidth, maxWidth );
				maxHeight += Mathf.CeilToInt( line.lineHeight );

				if( line.lineWidth > MaxSize.x )
				{
					clipRight( destination, lineStartIndex );
					clipRight( SpriteBuffer, spriteStartIndex );
				}

				clipBottom( destination, lineStartIndex );
				clipBottom( SpriteBuffer, spriteStartIndex );

			}

			this.RenderedSize = new Vector2(
				Mathf.Min( MaxSize.x, maxWidth ),
				Mathf.Min( MaxSize.y, maxHeight )
			) * TextScale;

			this.tokens.Release();
			this.tokens = null;

			//@Profiler.EndSample();

		}

		#endregion

		#region Private utility methods

		private int getAnticipatedVertCount( dfList<dfMarkupToken> tokens )
		{

			var textSize = 4 + ( Shadow ? 4 : 0 ) + ( Outline ? 4 : 0 );

			var count = 0;
			for( int i = 0; i < tokens.Count; i++ )
			{

				var token = tokens[ i ];

				if( token.TokenType == dfMarkupTokenType.Text )
				{
					count += textSize * token.Length;
				}
				else if( token.TokenType == dfMarkupTokenType.StartTag )
				{
					count += 4;
				}

			}

			return count;

		}
		
		/// <summary>
		/// Renders a single line of text
		/// </summary>
		private void renderLine( LineRenderInfo line, Stack<Color32> colors, Vector3 position, dfRenderData destination )
		{

			position.x += calculateLineAlignment( line );

			for( int i = line.startOffset; i <= line.endOffset; i++ )
			{

				var token = tokens[ i ];
				var type = token.TokenType;

				if( type == dfMarkupTokenType.Text )
				{
					renderText( token, colors.Peek(), position, destination );
				}
				else if( type == dfMarkupTokenType.StartTag )
				{
					if( token.Matches( "sprite" ) && SpriteAtlas != null && SpriteBuffer != null )
					{
						renderSprite( token, colors.Peek(), position, SpriteBuffer );
					}
					else if( token.Matches( "color" ) )
					{
						colors.Push( parseColor( token ) );
					}
				}
				else if( type == dfMarkupTokenType.EndTag )
				{
					if( token.Matches( "color" ) && colors.Count > 1 )
					{
						colors.Pop();
					}
				}

				position.x += token.Width;

			}

		}

		private void renderText( dfMarkupToken token, Color32 color, Vector3 position, dfRenderData renderData )
		{

			try
			{

				//@Profiler.BeginSample( "Render text token" );

				var font = (dfDynamicFont)Font;
				var fontSize = Mathf.CeilToInt( font.FontSize * TextScale );
				var fontStyle = FontStyle.Normal;
				var glyph = new UnityEngine.CharacterInfo();
				var descent = font.Descent;

				var verts = renderData.Vertices;
				var triangles = renderData.Triangles;
				var uvs = renderData.UV;
				var colors = renderData.Colors;

				var x = position.x;
				var y = position.y;

				// Set the render material in the output buffer *after* requesting
				// glyph data, which may result in CharacterInfo in the dfDynamicFont's 
				// texture atlas being rebuilt.
				renderData.Material = font.Material;

				var topColor = applyOpacity( multiplyColors( color, DefaultColor ) );
				var bottomColor = topColor;

				if( BottomColor.HasValue )
				{
					bottomColor = applyOpacity( multiplyColors( color, BottomColor.Value ) );
				}

				for( int i = 0; i < token.Length; i++ )
				{

					if( i > 0 )
						x += CharacterSpacing * TextScale;

					if( !font.baseFont.GetCharacterInfo( token[ i ], out glyph, fontSize, fontStyle ) )
						continue;

					var yadjust = ( font.FontSize + glyph.vert.y ) - fontSize + descent;
					var quadLeft = ( x + glyph.vert.x );
					var quadTop = ( y + yadjust );
					var quadRight = ( quadLeft + glyph.vert.width );
					var quadBottom = ( quadTop + glyph.vert.height );

					var v0 = new Vector3( quadLeft, quadTop ) * PixelRatio;
					var v1 = new Vector3( quadRight, quadTop ) * PixelRatio;
					var v2 = new Vector3( quadRight, quadBottom ) * PixelRatio;
					var v3 = new Vector3( quadLeft, quadBottom ) * PixelRatio;

					if( Shadow )
					{

						addTriangleIndices( verts, triangles );

						var activeShadowOffset = (Vector3)ShadowOffset * PixelRatio;
						verts.Add( v0 + activeShadowOffset );
						verts.Add( v1 + activeShadowOffset );
						verts.Add( v2 + activeShadowOffset );
						verts.Add( v3 + activeShadowOffset );

						var activeShadowColor = applyOpacity( ShadowColor );
						colors.Add( activeShadowColor );
						colors.Add( activeShadowColor );
						colors.Add( activeShadowColor );
						colors.Add( activeShadowColor );

						addUVCoords( uvs, glyph );

					}

					if( Outline )
					{
						for( int o = 0; o < OUTLINE_OFFSETS.Length; o++ )
						{

							addTriangleIndices( verts, triangles );

							var activeOutlineOffset = (Vector3)OUTLINE_OFFSETS[ o ] * OutlineSize * PixelRatio;
							verts.Add( v0 + activeOutlineOffset );
							verts.Add( v1 + activeOutlineOffset );
							verts.Add( v2 + activeOutlineOffset );
							verts.Add( v3 + activeOutlineOffset );

							var activeOutlineColor = applyOpacity( OutlineColor );
							colors.Add( activeOutlineColor );
							colors.Add( activeOutlineColor );
							colors.Add( activeOutlineColor );
							colors.Add( activeOutlineColor );

							addUVCoords( uvs, glyph );

						}
					}

					addTriangleIndices( verts, triangles );

					verts.Add( v0 );
					verts.Add( v1 );
					verts.Add( v2 );
					verts.Add( v3 );

					colors.Add( topColor );
					colors.Add( topColor );
					colors.Add( bottomColor );
					colors.Add( bottomColor );

					addUVCoords( uvs, glyph );

					x += Mathf.CeilToInt( glyph.vert.x + glyph.vert.width );

				}

			}
			finally
			{
				//@Profiler.EndSample();
			}

		}

		private static void addUVCoords( dfList<Vector2> uvs, UnityEngine.CharacterInfo glyph )
		{

			var region = glyph.uv;
			var uvLeft = region.x;
			var uvTop = region.y + region.height;
			var uvRight = uvLeft + region.width;
			var uvBottom = region.y;

			if( glyph.flipped )
			{
				uvs.Add( new Vector2( uvRight, uvBottom ) );
				uvs.Add( new Vector2( uvRight, uvTop ) );
				uvs.Add( new Vector2( uvLeft, uvTop ) );
				uvs.Add( new Vector2( uvLeft, uvBottom ) );
			}
			else
			{
				uvs.Add( new Vector2( uvLeft, uvTop ) );
				uvs.Add( new Vector2( uvRight, uvTop ) );
				uvs.Add( new Vector2( uvRight, uvBottom ) );
				uvs.Add( new Vector2( uvLeft, uvBottom ) );
			}

		}

		private void renderSprite( dfMarkupToken token, Color32 color, Vector3 position, dfRenderData destination )
		{

			try
			{

				//@Profiler.BeginSample( "Render embedded sprite" );

				var spriteName = token.GetAttribute( 0 ).Value.Value;
				var spriteInfo = SpriteAtlas[ spriteName ];
				if( spriteInfo == null )
					return;

				var options = new dfSprite.RenderOptions()
				{
					atlas = SpriteAtlas,
					color = color,
					fillAmount = 1,
					flip = dfSpriteFlip.None,
					offset = position,
					pixelsToUnits = PixelRatio,
					size = new Vector2( token.Width, token.Height ),
					spriteInfo = spriteInfo
				};

				dfSprite.renderSprite( SpriteBuffer, options );

			}
			finally
			{
				//@Profiler.EndSample();
			}

		}

		private Color32 parseColor( dfMarkupToken token )
		{

			var color = UnityEngine.Color.white;

			if( token.AttributeCount == 1 )
			{

				var value = token.GetAttribute( 0 ).Value.Value;

				if( value.Length == 7 && value[ 0 ] == '#' )
				{

					uint intColor = 0;
					uint.TryParse( value.Substring( 1 ), NumberStyles.HexNumber, null, out intColor );

					color = UIntToColor( intColor | 0xFF000000 );

				}
				else
				{
					color = dfMarkupStyle.ParseColor( value, DefaultColor );
				}

			}

			return applyOpacity( color );

		}

		private Color32 UIntToColor( uint color )
		{

			var a = (byte)( color >> 24 );
			var r = (byte)( color >> 16 );
			var g = (byte)( color >> 8 );
			var b = (byte)( color >> 0 );

			return new Color32( r, g, b, a );

		}

		/// <summary>
		/// Determine where each line of text starts. Assumes that the
		/// tokens array is already populated and that the render size
		/// of each token has already been determined.
		/// </summary>
		/// <returns></returns>
		private dfList<LineRenderInfo> calculateLinebreaks()
		{

			try
			{

				//@Profiler.BeginSample( "Calculate line breaks" );

				if( lines != null )
				{
					return lines;
				}

				lines = dfList<LineRenderInfo>.Obtain();

				var font = (dfDynamicFont)Font;
				var lastBreak = 0;
				var startIndex = 0;
				var index = 0;
				var lineWidth = 0;
				var lineHeight = font.Baseline * TextScale;

				while( index < tokens.Count && lines.Count * lineHeight <= MaxSize.y + lineHeight )
				{

					var token = tokens[ index ];
					var type = token.TokenType;

					if( type == dfMarkupTokenType.Newline )
					{

						lines.Add( LineRenderInfo.Obtain( startIndex, index ) );

						startIndex = lastBreak = ++index;
						lineWidth = 0;

						continue;

					}

					var tokenWidth = Mathf.CeilToInt( token.Width );

					var canWrap =
						WordWrap &&
						lastBreak > startIndex &&
						(
							type == dfMarkupTokenType.Text ||
							( type == dfMarkupTokenType.StartTag && token.Matches( "sprite" ) )
						);

					if( canWrap && lineWidth + tokenWidth >= MaxSize.x )
					{

						if( lastBreak > startIndex )
						{

							lines.Add( LineRenderInfo.Obtain( startIndex, lastBreak - 1 ) );

							startIndex = index = ++lastBreak;
							lineWidth = 0;

						}
						else
						{

							lines.Add( LineRenderInfo.Obtain( startIndex, lastBreak - 1 ) );

							startIndex = lastBreak = ++index;
							lineWidth = 0;

						}

						continue;

					}

					if( type == dfMarkupTokenType.Whitespace )
					{
						lastBreak = index;
					}

					lineWidth += tokenWidth;
					index += 1;

				}

				if( startIndex < tokens.Count )
				{
					lines.Add( LineRenderInfo.Obtain( startIndex, tokens.Count - 1 ) );
				}

				for( int i = 0; i < lines.Count; i++ )
				{
					calculateLineSize( lines[ i ] );
				}

				return lines;

			}
			finally
			{
				//@Profiler.EndSample();
			}

		}

		private int calculateLineAlignment( LineRenderInfo line )
		{

			var width = line.lineWidth;

			if( TextAlign == TextAlignment.Left || width < 1 )
				return 0;

			var x = 0f;

			if( TextAlign == TextAlignment.Right )
			{
				x = MaxSize.x - width;
			}
			else
			{
				x = ( MaxSize.x - width ) * 0.5f;
			}

			return Mathf.CeilToInt( Mathf.Max( 0f, x ) );

		}

		private void calculateLineSize( LineRenderInfo line )
		{

			var font = (dfDynamicFont)Font;
			line.lineHeight = font.Baseline * TextScale;

			var width = 0;
			for( int i = line.startOffset; i <= line.endOffset; i++ )
			{
				width += tokens[ i ].Width;
			}

			line.lineWidth = width;

		}

		/// <summary>
		/// Splits the source text into tokens and preprocesses the
		/// tokens to determine render size required, etc.
		/// </summary>
		private void tokenize( string text )
		{

			try
			{

				//@Profiler.BeginSample( "Tokenize text" );

				if( this.ProcessMarkup )
					this.tokens = dfMarkupTokenizer.Tokenize( text );
				else
					this.tokens = dfPlainTextTokenizer.Tokenize( text );

				for( int i = 0; i < tokens.Count; i++ )
				{
					calculateTokenRenderSize( tokens[ i ] );
				}

			}
			finally
			{
				//@Profiler.EndSample();
			}

		}

		/// <summary>
		/// Calculates the size, in pixels, required to render this
		/// token on screen. Does not account for scale.
		/// </summary>
		/// <param name="token"></param>
		private void calculateTokenRenderSize( dfMarkupToken token )
		{

			try
			{

				//@Profiler.BeginSample( "Calculate token render size" );

				var totalWidth = 0f;
				var ch = '\0';

				var font = (dfDynamicFont)Font;
				var glyph = new UnityEngine.CharacterInfo();

				if( token.TokenType == dfMarkupTokenType.Text )
				{

					var fontSize = Mathf.CeilToInt( font.FontSize * TextScale );

					for( int i = 0; i < token.Length; i++ )
					{

						// Dereference the original character and obtain character information
						ch = token[ i ];
						font.baseFont.GetCharacterInfo( ch, out glyph, fontSize, FontStyle.Normal );

						// TODO: Implement 'tab stops' calculation
						if( ch == '\t' )
						{
							totalWidth += this.TabSize;
							continue;
						}

						// Add the character width to the total
						totalWidth += ( ch != ' ' )
							? ( glyph.vert.x + glyph.vert.width )
							: ( glyph.width + CharacterSpacing * TextScale );

					}

					if( token.Length > 2 )
					{
						totalWidth += ( token.Length - 2 ) * CharacterSpacing * TextScale;
					}

					token.Height = Font.LineHeight;
					token.Width = Mathf.CeilToInt( totalWidth );

				}
				else if( token.TokenType == dfMarkupTokenType.Whitespace )
				{

					var fontSize = Mathf.CeilToInt( font.FontSize * TextScale );
					var spacing = CharacterSpacing * TextScale;

					for( int i = 0; i < token.Length; i++ )
					{

						// Dereference the original character
						ch = token[ i ];

						// TODO: Implement 'tab stops' calculation
						if( ch == '\t' )
						{
							totalWidth += this.TabSize;
						}
						else if( ch == ' ' )
						{
							font.baseFont.GetCharacterInfo( ch, out glyph, fontSize, FontStyle.Normal );
							totalWidth += glyph.width + spacing;
						}

					}

					token.Height = Font.LineHeight;
					token.Width = Mathf.CeilToInt( totalWidth );

				}
				else if( token.TokenType == dfMarkupTokenType.StartTag )
				{

					if( token.Matches( "sprite" ) && SpriteAtlas != null )
					{

						if( token.AttributeCount == 1 )
						{

							var texture = SpriteAtlas.Texture;
							var lineHeight = font.Baseline * TextScale;

							var spriteName = token.GetAttribute( 0 ).Value.Value;
							var sprite = SpriteAtlas[ spriteName ];

							if( sprite != null )
							{
								var aspectRatio = ( sprite.region.width * texture.width ) / ( sprite.region.height * texture.height );
								totalWidth = Mathf.CeilToInt( lineHeight * aspectRatio );
							}

							token.Height = Mathf.CeilToInt( lineHeight );
							token.Width = Mathf.CeilToInt( totalWidth );

						}

					}

				}

			}
			finally
			{
				//@Profiler.EndSample();
			}

		}

		private float getTabStop( float position )
		{

			var scale = PixelRatio * TextScale;

			if( TabStops != null && TabStops.Count > 0 )
			{
				for( int i = 0; i < TabStops.Count; i++ )
				{
					if( TabStops[ i ] * scale > position )
						return TabStops[ i ] * scale;
				}
			}

			if( TabSize > 0 )
				return position + TabSize * scale;

			return position + ( this.Font.FontSize * 4 * scale );

		}

		private void clipRight( dfRenderData destination, int startIndex )
		{

			if( destination == null )
				return;

			var limit = VectorOffset.x + MaxSize.x * PixelRatio;

			var verts = destination.Vertices;
			var uv = destination.UV;

			for( int i = startIndex; i < verts.Count; i += 4 )
			{

				var ul = verts[ i + 0 ];
				var ur = verts[ i + 1 ];
				var br = verts[ i + 2 ];
				var bl = verts[ i + 3 ];

				var w = ur.x - ul.x;

				if( ur.x > limit )
				{

					var clip = 1f - ( ( limit - ur.x + w ) / w );

					verts[ i + 0 ] = ul = new Vector3( Mathf.Min( ul.x, limit ), ul.y, ul.z );
					verts[ i + 1 ] = ur = new Vector3( Mathf.Min( ur.x, limit ), ur.y, ur.z );
					verts[ i + 2 ] = br = new Vector3( Mathf.Min( br.x, limit ), br.y, br.z );
					verts[ i + 3 ] = bl = new Vector3( Mathf.Min( bl.x, limit ), bl.y, bl.z );

					var uvx = Mathf.Lerp( uv[ i + 1 ].x, uv[ i ].x, clip );
					uv[ i + 1 ] = new Vector2( uvx, uv[ i + 1 ].y );
					uv[ i + 2 ] = new Vector2( uvx, uv[ i + 2 ].y );

					w = ur.x - ul.x;

				}

			}

		}

		private void clipBottom( dfRenderData destination, int startIndex )
		{

			if( destination == null )
				return;

			var limit = VectorOffset.y - MaxSize.y * PixelRatio;

			var verts = destination.Vertices;
			var uv = destination.UV;
			var colors = destination.Colors;

			for( int i = startIndex; i < verts.Count; i += 4 )
			{

				var ul = verts[ i + 0 ];
				var ur = verts[ i + 1 ];
				var br = verts[ i + 2 ];
				var bl = verts[ i + 3 ];

				var h = ul.y - bl.y;

				if( bl.y <= limit )
				{

					var clip = 1f - ( Mathf.Abs( -limit + ul.y ) / h );

					verts[ i + 0 ] = ul = new Vector3( ul.x, Mathf.Max( ul.y, limit ), ur.z );
					verts[ i + 1 ] = ur = new Vector3( ur.x, Mathf.Max( ur.y, limit ), ur.z );
					verts[ i + 2 ] = br = new Vector3( br.x, Mathf.Max( br.y, limit ), br.z );
					verts[ i + 3 ] = bl = new Vector3( bl.x, Mathf.Max( bl.y, limit ), bl.z );

					uv[ i + 3 ] = Vector2.Lerp( uv[ i + 3 ], uv[ i + 0 ], clip );
					uv[ i + 2 ] = Vector2.Lerp( uv[ i + 2 ], uv[ i + 1 ], clip );

					var color = Color.Lerp( colors[ i + 3 ], colors[ i ], clip );
					colors[ i + 3 ] = color;
					colors[ i + 2 ] = color;

				}

			}

		}

		private Color32 applyOpacity( Color32 color )
		{
			color.a = (byte)( Opacity * 255 );
			return color;
		}

		private static void addTriangleIndices( dfList<Vector3> verts, dfList<int> triangles )
		{

			var vcount = verts.Count;
			var indices = TRIANGLE_INDICES;

			for( int ii = 0; ii < indices.Length; ii++ )
			{
				triangles.Add( vcount + indices[ ii ] );
			}

		}

		private Color multiplyColors( Color lhs, Color rhs )
		{

			return new Color
			(
				lhs.r * rhs.r,
				lhs.g * rhs.g,
				lhs.b * rhs.b,
				lhs.a * rhs.a
			);

		}

		#endregion

	}

	private class LineRenderInfo : IPoolable
	{

		#region Public fields and properties

		public int startOffset;
		public int endOffset;
		public float lineWidth;
		public float lineHeight;

		public int length { get { return endOffset - startOffset + 1; } }

		#endregion

		#region Object Pooling

		private static dfList<LineRenderInfo> pool = new dfList<LineRenderInfo>();

		private LineRenderInfo()
		{
		}

		public static LineRenderInfo Obtain( int start, int end )
		{

			var result = ( pool.Count > 0 ) ? pool.Pop() : new LineRenderInfo();

			result.startOffset = start;
			result.endOffset = end;
			result.lineHeight = 0;

			return result;

		}

		public void Release()
		{

			startOffset = endOffset = 0;
			lineWidth = lineHeight = 0;

			pool.Add( this );

		}

		#endregion

	}

	#endregion

}
