#define USE_NEW_BMFONT_RENDERER
// Uncomment the preceeding line if you wish to revert to the old 
// bitmapped font renderer

/* Copyright 2013-2014 Daikon Forge */

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

using Object = UnityEngine.Object;

/// <summary>
/// Implements bitmapped-font functionality for text rendering
/// </summary>
[dfHelp( "http://www.daikonforge.com/docs/df-gui/classdf_font.html" )]
[Serializable]
[AddComponentMenu( "Daikon Forge/User Interface/Font Definition" )]
public class dfFont : dfFontBase
{

	#region Nested classes

	private class GlyphKerningList
	{

		private Dictionary<int, int> list = new Dictionary<int, int>();

		public void Add( GlyphKerning kerning )
		{
			list[ kerning.second ] = kerning.amount;
		}

		public int GetKerning( int firstCharacter, int secondCharacter )
		{

			var amount = 0;
			list.TryGetValue( secondCharacter, out amount );

			return amount;

		}

	}

	[Serializable]
	public class GlyphKerning : IComparable<GlyphKerning>
	{

		public int first;
		public int second;
		public int amount;

		#region IComparable<GlyphKerning> Members

		public int CompareTo( GlyphKerning other )
		{
			if( first == other.first ) return second.CompareTo( other.second );
			return first.CompareTo( other.first );
		}

		#endregion

	}

	[Serializable]
	public class GlyphDefinition : IComparable<GlyphDefinition>
	{

		#region Serialized values

#pragma warning disable 0649

		[SerializeField]
		public int id;

		[SerializeField]
		public int x;

		[SerializeField]
		public int y;

		[SerializeField]
		public int width;

		[SerializeField]
		public int height;

		[SerializeField]
		public int xoffset;

		[SerializeField]
		public int yoffset;

		[SerializeField]
		public int xadvance;

		[SerializeField]
		public bool rotated = false;

#pragma warning restore 0649

		#endregion

		#region IComparable<Glyph> Members

		public int CompareTo( GlyphDefinition other )
		{
			return this.id.CompareTo( other.id );
		}

		#endregion

	}

	#endregion

	#region Protected serialized members

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected string sprite;

	[SerializeField]
	protected string face = "";

	[SerializeField]
	protected int size;

	[SerializeField]
	protected bool bold;

	[SerializeField]
	protected bool italic;

	[SerializeField]
	protected string charset;

	[SerializeField]
	protected int stretchH;

	[SerializeField]
	protected bool smooth;

	[SerializeField]
	protected int aa;

	[SerializeField]
	protected int[] padding;

	[SerializeField]
	protected int[] spacing;

	[SerializeField]
	protected int outline;

	[SerializeField]
	protected int lineHeight;

	[SerializeField]
	private List<GlyphDefinition> glyphs = new List<GlyphDefinition>();

	[SerializeField]
	protected List<GlyphKerning> kerning = new List<GlyphKerning>();

	#endregion

	#region Private instance members

	private Dictionary<int, GlyphDefinition> glyphMap;
	private Dictionary<int, GlyphKerningList> kerningMap;

	// TODO: Finish implementing common font API
	//private Queue<BitmappedFontRenderer> rendererPool = new Queue<BitmappedFontRenderer>();

	#endregion

	#region Public properties

	public List<GlyphDefinition> Glyphs { get { return glyphs; } }
	public List<GlyphKerning> KerningInfo { get { return kerning; } }

	/// <summary>
	/// The Texture Atlas which contains the rendered font as a bitmap sprite
	/// </summary>
	public dfAtlas Atlas
	{
		get { return this.atlas; }
		set
		{
			if( value != this.atlas )
			{
				this.atlas = value;
				glyphMap = null;
			}
		}
	}

	/// <summary>
	/// Gets or sets the Material that will be used to render text
	/// </summary>
	public override Material Material
	{
		get
		{
			return this.Atlas.Material;
		}
		set
		{
			throw new InvalidOperationException();
		}
	}

	/// <summary>
	/// Returns a reference to the texture which contains the
	/// glyph images that will be used to render text
	/// </summary>
	public override Texture Texture
	{
		get { return this.Atlas.Texture; }
	}

	/// <summary>
	/// The sprite which contains the per-rendered font graphics
	/// </summary>
	public string Sprite
	{
		get { return this.sprite; }
		set
		{
			if( value != this.sprite )
			{
				this.sprite = value;
				glyphMap = null;
			}
		}
	}

	/// <summary>
	/// Returns a value indicating whether the dfFont configuration is valid
	/// </summary>
	public override bool IsValid
	{
		get
		{
			if( Atlas == null || Atlas[ Sprite ] == null )
				return false;
			return true;
		}
	}

	/// <summary>
	/// The name of the TrueType font represented by this instance 
	/// </summary>
	public string FontFace { get { return face; } }

	/// <summary>
	///  The size (in pixels) of the TrueType font
	/// </summary>
	public override int FontSize 
	{ 
		get { return size; }
		set { throw new InvalidOperationException(); }
	}

	/// <summary>
	/// The distance in pixels between each line of text
	/// </summary>
	public override int LineHeight 
	{ 
		get { return lineHeight; }
		set { throw new InvalidOperationException(); }
	}

	/// <summary>
	/// Indicates whether this font definition is BOLD
	/// </summary>
	public bool Bold { get { return bold; } }

	/// <summary>
	/// Indicates whether this font definition is ITALIC
	/// </summary>
	public bool Italic { get { return italic; } }

	/// <summary>
	/// The amount of padding (in pixels) surrounding each character (up, right, down, left)
	/// </summary>
	public int[] Padding { get { return padding; } }

	/// <summary>
	/// The spacing for each character (horz, vert)
	/// </summary>
	public int[] Spacing { get { return spacing; } }

	/// <summary>
	/// The thickness (in pixels) of the outline for each character. Will be 0 if there is no outline.
	/// </summary>
	public int Outline { get { return outline; } }

	/// <summary>
	/// Returns the number of glyphs defined in this instance
	/// </summary>
	public int Count { get { return glyphs.Count; } }

	#endregion

	#region Unity events

	public void OnEnable()
	{
		glyphMap = null;
	}

	#endregion

	#region Public methods

	public override dfFontRendererBase ObtainRenderer()
	{
		return BitmappedFontRenderer.Obtain( this );
	}

	public void AddKerning( int first, int second, int amount )
	{
		kerning.Add( new GlyphKerning() { first = first, second = second, amount = amount } );
	}

	public int GetKerning( char previousChar, char currentChar )
	{

		try
		{

			//@Profiler.BeginSample( "Find Kerning Data" );

			if( kerningMap == null )
			{
				buildKerningMap();
			}

			GlyphKerningList list = null;
			
			if( !kerningMap.TryGetValue( previousChar, out list ) )
				return 0;

			return list.GetKerning( previousChar, currentChar );

		}
		finally
		{
			//@Profiler.EndSample();
		}

	}

	private void buildKerningMap()
	{

		var map = kerningMap = new Dictionary<int, GlyphKerningList>();

		for( int i = 0; i < kerning.Count; i++ )
		{

			var info = kerning[ i ];

			if( !map.ContainsKey( info.first ) )
			{
				map[ info.first ] = new GlyphKerningList();
			}

			var list = map[ info.first ];
			list.Add( info );

		}

	}

	public GlyphDefinition GetGlyph( char id )
	{

		#region Build glyph dictionary "on demand"

		if( glyphMap == null )
		{

			glyphMap = new Dictionary<int, GlyphDefinition>();

			for( int i = 0; i < glyphs.Count; i++ )
			{
				var glyph = glyphs[ i ];
				glyphMap[ glyph.id ] = glyph;
			}

		}

		#endregion

		GlyphDefinition result = null;
		glyphMap.TryGetValue( id, out result );

		return result;

	}

	#endregion

	#region TextRenderer class

#if USE_NEW_BMFONT_RENDERER

	public class BitmappedFontRenderer : dfFontRendererBase, IPoolable
	{

		#region Object pooling

		private static Queue<BitmappedFontRenderer> objectPool = new Queue<BitmappedFontRenderer>();

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

		public int LineCount { get { return lines.Count; } }

		#endregion

		#region Private instance fields

		private dfList<LineRenderInfo> lines = null;
		private dfList<dfMarkupToken> tokens = null;

		#endregion

		#region Constructors

		internal BitmappedFontRenderer()
		{
		}

		#endregion

		#region Object pooling 

		public static dfFontRendererBase Obtain( dfFont font )
		{

			var renderer = objectPool.Count > 0 ? objectPool.Dequeue() : new BitmappedFontRenderer();
			renderer.Reset();
			renderer.Font = font;

			return renderer;

		}

		public override void Release()
		{

			this.Reset();

			if( this.tokens != null )
			{
				this.tokens.ReleaseItems();
				this.tokens.Release();
			}
			this.tokens = null;

			if( lines != null )
			{
				lines.Release();
				lines = null;
			}

			LineRenderInfo.ResetPool();

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

			var font = (dfFont)Font;
			var output = new float[ text.Length ];

			var scale = TextScale * PixelRatio;
			var horzSpacing = CharacterSpacing * scale;

			for( int i = startIndex; i <= endIndex; i++ )
			{

				var glyph = font.GetGlyph( text[ i ] );
				if( glyph == null )
					continue;

				if( i > 0 )
				{
					output[ i - 1 ] += horzSpacing;
					totalWidth += horzSpacing;
				}

				var glyphWidth = glyph.xadvance * scale;
				output[ i ] = glyphWidth;

				totalWidth += glyphWidth;

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

			tokenize( text );
			var lines = calculateLinebreaks();

			var totalWidth = 0;
			var totalHeight = 0;

			for( int i = 0; i < lines.Count; i++ )
			{
				totalWidth = Mathf.Max( (int)lines[ i ].lineWidth, totalWidth );
				totalHeight += (int)lines[ i ].lineHeight;
			}

			return new Vector2( totalWidth, totalHeight ) * TextScale;

		}

		/// <summary>
		/// Render the given text as mesh data to the given destination buffer
		/// </summary>
		/// <param name="text">The text to be rendered</param>
		/// <param name="destination">The dfRenderData buffer that will hold the 
		/// text mesh information</param>
		public override void Render( string text, dfRenderData destination )
		{

			//@Profiler.BeginSample( "Render bitmapped font text" );

			textColors.Clear();
			textColors.Push( Color.white );

			tokenize( text );
			var lines = calculateLinebreaks();

			destination.EnsureCapacity( getAnticipatedVertCount( tokens ) );

			var maxWidth = 0;
			var maxHeight = 0;

			var position = VectorOffset;
			var scale = TextScale * PixelRatio;

			for( int i = 0; i < lines.Count; i++ )
			{

				var line = lines[ i ];
				var lineStartIndex = destination.Vertices.Count;
				
				renderLine( lines[ i ], textColors, position, destination );

				position.y -= Font.LineHeight * scale;

				maxWidth = Mathf.Max( (int)line.lineWidth, maxWidth );
				maxHeight += (int)line.lineHeight;

				if( line.lineWidth * TextScale > MaxSize.x )
				{
					clipRight( destination, lineStartIndex );
				}

				if( maxHeight * TextScale > MaxSize.y )
				{
					clipBottom( destination, lineStartIndex );
				}

			}

			this.RenderedSize = new Vector2(
				Mathf.Min( MaxSize.x, maxWidth ),
				Mathf.Min( MaxSize.y, maxHeight )
			) * TextScale;

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

			var scale = TextScale * PixelRatio;

			position.x += calculateLineAlignment( line ) * scale;

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
					if( token.Matches( "sprite" ) )
					{
						renderSprite( token, colors.Peek(), position, destination );
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

				position.x += token.Width * scale;

			}

		}

		private void renderText( dfMarkupToken token, Color32 color, Vector3 position, dfRenderData destination )
		{

			try
			{

				//@Profiler.BeginSample( "Render text token" );

				var verts = destination.Vertices;
				var triangles = destination.Triangles;
				var colors = destination.Colors;
				var uvs = destination.UV;

				var font = (dfFont)Font;
				var sprite = font.Atlas[ font.sprite ];

				var texture = font.Texture;
				var uvw = 1f / texture.width;
				var uvh = 1f / texture.height;
				var uvxofs = uvw * 0.125f;
				var uvyofs = uvh * 0.125f;
				var ratio = TextScale * PixelRatio;

				var last = '\0';
				var ch = '\0';

				var topColor = applyOpacity( multiplyColors( color, DefaultColor ) );
				var bottomColor = topColor;

				if( BottomColor.HasValue )
				{
					bottomColor = applyOpacity( multiplyColors( color, BottomColor.Value ) );
				}

				for( int i = 0; i < token.Length; i++, last = ch )
				{

					ch = token[ i ];
					if( ch == 0 )
						continue;

					var glyph = font.GetGlyph( ch );
					if( glyph == null )
						continue;

					var kerning = font.GetKerning( last, ch );

					var xofs = position.x + ( glyph.xoffset + kerning ) * ratio;
					var yofs = position.y - ( glyph.yoffset * ratio );

					var width = glyph.width * ratio;
					var height = glyph.height * ratio;

					var quadRight = ( xofs + width );
					var quadBottom = ( yofs - height );

					var v0 = new Vector3( xofs, yofs );
					var v1 = new Vector3( quadRight, yofs );
					var v2 = new Vector3( quadRight, quadBottom );
					var v3 = new Vector3( xofs, quadBottom );

					var uvLeft = sprite.region.x + glyph.x * uvw - uvxofs;
					var uvTop = sprite.region.yMax - glyph.y * uvh - uvyofs;
					var uvRight = uvLeft + glyph.width * uvw - uvxofs;
					var uvBottom = uvTop - glyph.height * uvh + uvyofs;

					if( Shadow )
					{

						addTriangleIndices( verts, triangles );

						var activeShadowOffset = (Vector3)ShadowOffset * ratio;
						verts.Add( v0 + activeShadowOffset );
						verts.Add( v1 + activeShadowOffset );
						verts.Add( v2 + activeShadowOffset );
						verts.Add( v3 + activeShadowOffset );

						var activeShadowColor = applyOpacity( ShadowColor );
						colors.Add( activeShadowColor );
						colors.Add( activeShadowColor );
						colors.Add( activeShadowColor );
						colors.Add( activeShadowColor );

						uvs.Add( new Vector2( uvLeft, uvTop ) );
						uvs.Add( new Vector2( uvRight, uvTop ) );
						uvs.Add( new Vector2( uvRight, uvBottom ) );
						uvs.Add( new Vector2( uvLeft, uvBottom ) );

					}

					if( Outline )
					{
						for( int o = 0; o < OUTLINE_OFFSETS.Length; o++ )
						{

							addTriangleIndices( verts, triangles );

							var activeOutlineOffset = (Vector3)OUTLINE_OFFSETS[ o ] * OutlineSize * ratio;
							verts.Add( v0 + activeOutlineOffset );
							verts.Add( v1 + activeOutlineOffset );
							verts.Add( v2 + activeOutlineOffset );
							verts.Add( v3 + activeOutlineOffset );

							var activeOutlineColor = applyOpacity( OutlineColor );
							colors.Add( activeOutlineColor );
							colors.Add( activeOutlineColor );
							colors.Add( activeOutlineColor );
							colors.Add( activeOutlineColor );

							uvs.Add( new Vector2( uvLeft, uvTop ) );
							uvs.Add( new Vector2( uvRight, uvTop ) );
							uvs.Add( new Vector2( uvRight, uvBottom ) );
							uvs.Add( new Vector2( uvLeft, uvBottom ) );

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

					uvs.Add( new Vector2( uvLeft, uvTop ) );
					uvs.Add( new Vector2( uvRight, uvTop ) );
					uvs.Add( new Vector2( uvRight, uvBottom ) );
					uvs.Add( new Vector2( uvLeft, uvBottom ) );

					position.x += (glyph.xadvance + kerning + CharacterSpacing) * ratio;

				}

			}
			finally
			{
				//@Profiler.EndSample();
			}

		}

		private void renderSprite( dfMarkupToken token, Color32 color, Vector3 position, dfRenderData destination )
		{

			try
			{

				//@Profiler.BeginSample( "Render embedded sprite" );

				var verts = destination.Vertices;
				var triangles = destination.Triangles;
				var colors = destination.Colors;
				var uvs = destination.UV;

				var font = (dfFont)Font;
				var spriteName = token.GetAttribute( 0 ).Value.Value;
				var spriteInfo = font.Atlas[ spriteName ];
				if( spriteInfo == null )
					return;

				var lineHeight = token.Height * TextScale * PixelRatio;
				var spriteWidth = token.Width * TextScale * PixelRatio;
				
				var left = position.x;
				var top = position.y;

				var sti = verts.Count;
				verts.Add( new Vector3( left, top ) );
				verts.Add(  new Vector3( left + spriteWidth, top ) );
				verts.Add( new Vector3( left + spriteWidth, top - lineHeight ) );
				verts.Add( new Vector3( left, top - lineHeight ) );

				triangles.Add( sti + 0 );
				triangles.Add( sti + 1 );
				triangles.Add( sti + 3 );
				triangles.Add( sti + 3 );
				triangles.Add( sti + 1 );
				triangles.Add( sti + 2 );

				var spriteColor = ColorizeSymbols 
					? applyOpacity( color ) 
					: applyOpacity( DefaultColor );

				colors.Add( spriteColor );
				colors.Add( spriteColor );
				colors.Add( spriteColor );
				colors.Add( spriteColor );

				var spriteRect = spriteInfo.region;
				uvs.Add( new Vector2( spriteRect.x, spriteRect.yMax ) );
				uvs.Add( new Vector2( spriteRect.xMax, spriteRect.yMax ) );
				uvs.Add( new Vector2( spriteRect.xMax, spriteRect.y ) );
				uvs.Add( new Vector2( spriteRect.x, spriteRect.y ) );

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

				var lastBreak = 0;
				var startIndex = 0;
				var index = 0;
				var lineWidth = 0;
				var lineHeight = Font.LineHeight * TextScale;

				while( index < tokens.Count && lines.Count * lineHeight < MaxSize.y )
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

					var tokenWidth = Mathf.CeilToInt( token.Width * TextScale );

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

			if( TextAlign == TextAlignment.Left || width == 0 )
				return 0;

			var x = 0;

			if( TextAlign == TextAlignment.Right )
			{
				x = Mathf.FloorToInt( MaxSize.x / TextScale - width );
			}
			else
			{
				x = Mathf.FloorToInt( ( MaxSize.x / TextScale - width ) * 0.5f );
			}

			return Mathf.Max( 0, x );

		}

		private void calculateLineSize( LineRenderInfo line )
		{

			line.lineHeight = Font.LineHeight;

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
		private dfList<dfMarkupToken> tokenize( string text )
		{

			try
			{

				//@Profiler.BeginSample( "Tokenize text" );

				if( this.tokens != null )
				{

					// Sanity check. You shouldn't be re-using this class
					// on multiple strings without resetting in-between,
					// though.
					if( object.ReferenceEquals( tokens[ 0 ].Source, text ) )
						return this.tokens;

					// Release current text tokens before proceeding 
					this.tokens.ReleaseItems();
					this.tokens.Release();

				}

				if( this.ProcessMarkup )
					this.tokens = dfMarkupTokenizer.Tokenize( text );
				else
					this.tokens = dfPlainTextTokenizer.Tokenize( text );

				for( int i = 0; i < tokens.Count; i++ )
				{
					calculateTokenRenderSize( tokens[ i ] );
				}

				return tokens;

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

				var font = (dfFont)Font;

				var totalWidth = 0;
				var last = '\0';
				var ch = '\0';

				var isTextToken =
					token.TokenType == dfMarkupTokenType.Whitespace ||
					token.TokenType == dfMarkupTokenType.Text;

				if( isTextToken )
				{

					for( int i = 0; i < token.Length; i++, last = ch )
					{

						// Dereference the original character
						ch = token[ i ];

						// TODO: Implement 'tab stops' calculation
						if( ch == '\t' )
						{
							totalWidth += this.TabSize;
							continue;
						}

						// Attempt to obtain a reference to the glyph data that
						// represents the character
						var glyph = font.GetGlyph( ch );

						// If glyph is not printable, just skip it
						if( glyph == null )
							continue;

						// If this is not the first character then need to apply
						// horizontal spacing and kerning
						if( i > 0 )
						{
							totalWidth += font.GetKerning( last, ch );
							totalWidth += CharacterSpacing;
						}

						// Add the character width to the total
						totalWidth += glyph.xadvance;

					}

				}
				else if( token.TokenType == dfMarkupTokenType.StartTag )
				{
					if( token.Matches( "sprite" ) )
					{
						
						if( token.AttributeCount < 1 )
							throw new Exception( "Missing sprite name in markup" );

						var texture = font.Texture;
						var lineHeight = font.LineHeight;

						var spriteName = token.GetAttribute( 0 ).Value.Value;
						var sprite = font.atlas[ spriteName ];

						if( sprite != null )
						{
							var aspectRatio = ( sprite.region.width * texture.width ) / ( sprite.region.height * texture.height );
							totalWidth = Mathf.CeilToInt( lineHeight * aspectRatio );
						}

					}
				}

				token.Height = Font.LineHeight;
				token.Width = totalWidth;

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

					var uvy = Mathf.Lerp( uv[ i + 3 ].y, uv[ i ].y, clip );
					uv[ i + 3 ] = new Vector2( uv[ i + 3 ].x, uvy );
					uv[ i + 2 ] = new Vector2( uv[ i + 2 ].x, uvy );

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

			for( int i = 0; i < TRIANGLE_INDICES.Length; i++ )
			{
				triangles.Add( vcount + TRIANGLE_INDICES[ i ] );
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

#else

	public class BitmappedFontRenderer : dfFontRendererBase
	{

		#region Object pooling 

		private static Queue<BitmappedFontRenderer> objectPool = new Queue<BitmappedFontRenderer>();

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
		#endregion

		#region Public properties

		public int LineCount { get { return lines.Count; } }

		#endregion

		#region Private instance fields

		private string text;
		private dfList<LineRenderInfo> lines = new dfList<LineRenderInfo>();
		private dfList<GlyphRenderInfo> glyphs = new dfList<GlyphRenderInfo>();

		#endregion

		#region Constructors

		internal BitmappedFontRenderer()
		{
		}

		#endregion

		#region Public methods

		public static dfFontRendererBase Obtain( dfFont font )
		{
			
			var renderer = objectPool.Count > 0 ? objectPool.Dequeue() : new BitmappedFontRenderer();
			renderer.Reset();
			renderer.Font = font;

			return renderer;

		}

		public override void Release()
		{

			this.Reset();
			
			this.lines.Clear();
			this.glyphs.Clear();
			this.BottomColor = (Color32?)null;
			
			objectPool.Enqueue( this );

		}

		public override float[] GetCharacterWidths( string text )
		{
			var totalWidth = 0f;
			return GetCharacterWidths( text, out totalWidth );
		}

		/// <summary>
		/// Measures a single line of text, with no line breaks or word wrapping
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public override Vector2 MeasureString( string text )
		{

			try
			{

				//@Profiler.BeginSample( "Measuring text" );

				this.text = preprocess( text );
				if( glyphs.Count == 0 )
				{
					throw new Exception( "No glyphs found for font: " + Font.name );
				}

				var texture = Font.Texture;

				var ratio = PixelRatio * TextScale;
				var horzSpacing = CharacterSpacing * ratio;
				var vertSpacing = 0;
				var lineHeight = Font.LineHeight * ratio;
				var renderedWidth = 0f;
				var renderedHeight = 0f;

				var x = 0f;
				var y = 0f;

				for( int li = 0; li < lines.Count; li++ )
				{

					var line = lines[ li ];
					x = Mathf.Max( calculateLineAlignment( line ), 0f );

					for( int i = line.startOffset; i <= line.endOffset; i++ )
					{

						var glyph = glyphs[ i ];

						if( i > line.startOffset )
							x += horzSpacing;

						if( glyph.character == '\x01' )
						{

							var spriteInfo = glyph.sprite;
							if( spriteInfo == null )
								continue;

							var aspectRatio = ( spriteInfo.region.width * texture.width ) / ( spriteInfo.region.height * texture.height );
							var spriteWidth = lineHeight * aspectRatio;

							x += spriteWidth;

							continue;

						}

						if( char.IsWhiteSpace( glyph.character ) )
						{

							if( glyph.character == '\t' )
								x = getTabStop( x );
							else
								x += glyph.advance * PixelRatio;

							continue;

						}

						x += glyph.advance * PixelRatio;

					}

					renderedWidth = Mathf.Max( renderedWidth, x / PixelRatio );
					renderedHeight += lineHeight + ( li > 0 ? vertSpacing : 0 );

					// Advance one line 
					x = 0;
					y -= lineHeight + vertSpacing;

				}

				return new Vector2( renderedWidth, renderedHeight / PixelRatio );

			}
			finally
			{
				//@Profiler.EndSample();
			}

		}

		public override void Render( string text, dfRenderData destination )
		{

			try
			{

				//@Profiler.BeginSample( "Rendering bitmapped text" );

				if( string.IsNullOrEmpty( text ) )
					return;

				var font = (dfFont)Font;
				var sprite = font.Atlas[ font.sprite ];

				// Ensure that the buffer already has enough memory allocated to 
				// hold the rendered text, to reduce memory thrashing.
				var anticipatedQuadCount = text.Length * ( this.Shadow ? 2 : 1 );
				destination.EnsureCapacity( destination.Vertices.Count + anticipatedQuadCount * 4 );

				RenderedSize = Vector2.zero;
				LinesRendered = 0;

				this.text = preprocess( text );
				if( glyphs.Count == 0 )
				{
					// throw new Exception( "No glyphs found for font: " + Font.name );
					return;
				}

				var verts = destination.Vertices;
				var triangles = destination.Triangles;
				var colors = destination.Colors;
				var uvs = destination.UV;

				var texture = Font.Texture;
				var uvw = 1f / texture.width;
				var uvh = 1f / texture.height;
				var uvxofs = uvw * 0.125f;
				var uvyofs = uvh * 0.125f;

				var x = 0f;
				var y = 0f;

				var ratio = PixelRatio * TextScale;
				var horzSpacing = CharacterSpacing * ratio;
				var vertSpacing = 0;
				var lineHeight = Font.LineHeight * ratio;

				var horzRenderLimit = MaxSize.x * PixelRatio;
				var renderedWidth = 0f;
				var renderedHeight = 0f;
				var lineBaseIndex = 0;

				for( int li = 0; li < lines.Count; li++ )
				{

					var line = lines[ li ];

					lineBaseIndex = verts.Count;
					x = Mathf.Max( calculateLineAlignment( line ), 0f );

					for( int i = line.startOffset; i <= line.endOffset && x <= horzRenderLimit; i++ )
					{

						var glyph = glyphs[ i ];

						if( i > line.startOffset )
							x += horzSpacing;

						if( glyph.character == '\x01' )
						{

							#region Render sprite instead of character glyph

							var spriteInfo = glyph.sprite;
							if( spriteInfo == null )
								continue;

							var aspectRatio = ( spriteInfo.region.width * texture.width ) / ( spriteInfo.region.height * texture.height );
							var spriteWidth = lineHeight * aspectRatio;
							var sti = verts.Count;

							// Do not render a sprite that extends past the visible border,
							// unless it's the only thing on this line in which case it can 
							// be clipped after rendering.
							if( i > 1 && x + spriteWidth > MaxSize.x )
							{
								x += spriteWidth;
								continue;
							}

							verts.Add( new Vector3( x, y - vertSpacing ) + VectorOffset );
							verts.Add( new Vector3( x + spriteWidth, y - vertSpacing ) + VectorOffset );
							verts.Add( new Vector3( x + spriteWidth, y - lineHeight - vertSpacing ) + VectorOffset );
							verts.Add( new Vector3( x, y - lineHeight - vertSpacing ) + VectorOffset );

							triangles.Add( sti + 0 );
							triangles.Add( sti + 1 );
							triangles.Add( sti + 3 );
							triangles.Add( sti + 3 );
							triangles.Add( sti + 1 );
							triangles.Add( sti + 2 );

							var spriteColor = ColorizeSymbols ? applyOpacity( glyph.topColor ) : applyOpacity( DefaultColor );
							colors.Add( spriteColor );
							colors.Add( spriteColor );
							colors.Add( spriteColor );
							colors.Add( spriteColor );

							var spriteRect = spriteInfo.region;
							uvs.Add( new Vector2( spriteRect.x, spriteRect.yMax ) );
							uvs.Add( new Vector2( spriteRect.xMax, spriteRect.yMax ) );
							uvs.Add( new Vector2( spriteRect.xMax, spriteRect.y ) );
							uvs.Add( new Vector2( spriteRect.x, spriteRect.y ) );

							#endregion

							x += spriteWidth;

							continue;

						}

						if( char.IsWhiteSpace( glyph.character ) )
						{

							if( glyph.character == '\t' )
								x = getTabStop( x );
							else
								x += glyph.advance * PixelRatio;

							continue;

						}

						var xofs = ( x + glyph.xoffset * ratio );
						var yofs = ( y - glyph.yoffset * ratio );
						var width = glyph.width * ratio;
						var height = glyph.height * ratio;

						var quadRight = ( xofs + width );//.RoundToNearest( PixelRatio );
						var quadBottom = ( yofs - height );//.RoundToNearest( PixelRatio );

						var v0 = ( VectorOffset + new Vector3( xofs, yofs ) );
						var v1 = ( VectorOffset + new Vector3( quadRight, yofs ) );
						var v2 = ( VectorOffset + new Vector3( quadRight, quadBottom ) );
						var v3 = ( VectorOffset + new Vector3( xofs, quadBottom ) );

						var uvLeft = sprite.region.x + glyph.uvx * uvw - uvxofs;
						var uvTop = sprite.region.yMax - glyph.uvy * uvh - uvyofs;
						var uvRight = uvLeft + glyph.width * uvw - uvxofs;
						var uvBottom = uvTop - glyph.height * uvh + uvyofs;

						if( Shadow )
						{

							addTriangleIndices( verts, triangles );

							var activeShadowOffset = (Vector3)ShadowOffset * ratio;
							verts.Add( v0 + activeShadowOffset );
							verts.Add( v1 + activeShadowOffset );
							verts.Add( v2 + activeShadowOffset );
							verts.Add( v3 + activeShadowOffset );

							var activeShadowColor = applyOpacity( ShadowColor );
							colors.Add( activeShadowColor );
							colors.Add( activeShadowColor );
							colors.Add( activeShadowColor );
							colors.Add( activeShadowColor );

							uvs.Add( new Vector2( uvLeft, uvTop ) );
							uvs.Add( new Vector2( uvRight, uvTop ) );
							uvs.Add( new Vector2( uvRight, uvBottom ) );
							uvs.Add( new Vector2( uvLeft, uvBottom ) );

						}

						if( Outline )
						{
							for( int o = 0; o < OUTLINE_OFFSETS.Length; o++ )
							{
								addTriangleIndices( verts, triangles );
								var activeOutlineOffset = (Vector3)OUTLINE_OFFSETS[ o ] * OutlineSize * ratio;
								verts.Add( v0 + activeOutlineOffset );
								verts.Add( v1 + activeOutlineOffset );
								verts.Add( v2 + activeOutlineOffset );
								verts.Add( v3 + activeOutlineOffset );
								var activeOutlineColor = applyOpacity( OutlineColor );
								colors.Add( activeOutlineColor );
								colors.Add( activeOutlineColor );
								colors.Add( activeOutlineColor );
								colors.Add( activeOutlineColor );
								uvs.Add( new Vector2( uvLeft, uvTop ) );
								uvs.Add( new Vector2( uvRight, uvTop ) );
								uvs.Add( new Vector2( uvRight, uvBottom ) );
								uvs.Add( new Vector2( uvLeft, uvBottom ) );
							}
						}
						addTriangleIndices( verts, triangles );
						verts.Add( v0 );
						verts.Add( v1 );
						verts.Add( v2 );
						verts.Add( v3 );

						colors.Add( glyph.topColor );
						colors.Add( glyph.topColor );
						colors.Add( glyph.bottomColor );
						colors.Add( glyph.bottomColor );

						uvs.Add( new Vector2( uvLeft, uvTop ) );
						uvs.Add( new Vector2( uvRight, uvTop ) );
						uvs.Add( new Vector2( uvRight, uvBottom ) );
						uvs.Add( new Vector2( uvLeft, uvBottom ) );

						x += glyph.advance * PixelRatio;

					}

					renderedWidth = Mathf.Max( renderedWidth, x / PixelRatio );
					renderedHeight += lineHeight + ( li > 0 ? vertSpacing : 0 );
					LinesRendered += 1;

					// Clip any triangles that extend past the right edge of the
					// indicated render area. 
					if( x >= horzRenderLimit )
					{
						clipRight( destination, lineBaseIndex );
					}

					// If the last rendered line extended past the vertical limit 
					// of the render area, perform triangle clipping and exit.
					if( renderedHeight >= MaxSize.y * PixelRatio )
					{
						clipBottom( destination, lineBaseIndex );
						break;
					}

					// Advance one line 
					x = 0;
					y -= lineHeight + vertSpacing;

				}

				// Keep track of the rendered text size - Used by controls to auto-adjust
				// control size, etc.
				RenderedSize = new Vector2( renderedWidth, renderedHeight / PixelRatio );

			}
			finally
			{
				//@Profiler.EndSample();
			}

		}

		#endregion

		#region Private utility methods

		private float[] GetCharacterWidths( string text, out float totalWidth )
		{

			totalWidth = 0f;

			var output = new float[ text.Length ];

			var scale = TextScale * PixelRatio;
			var horzSpacing = CharacterSpacing * scale;

			for( int i = 0; i < text.Length; i++ )
			{

				var glyph = Font.GetGlyph( text[ i ] );
				if( glyph == null )
					continue;

				if( i > 0 )
				{
					output[ i - 1 ] += horzSpacing;
					totalWidth += horzSpacing;
				}

				var glyphWidth = glyph.xadvance * scale;
				output[ i ] = glyphWidth;

				totalWidth += glyphWidth;

			}

			return output;

		}

		private void clipRight( dfRenderData destination, int startIndex )
		{

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

		private void clipBottom( dfRenderData destination, int startIndex )
		{

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

					var uvy = Mathf.Lerp( uv[ i + 3 ].y, uv[ i ].y, clip );
					uv[ i + 3 ] = new Vector2( uv[ i + 3 ].x, uvy );
					uv[ i + 2 ] = new Vector2( uv[ i + 2 ].x, uvy );

					var color = Color.Lerp( colors[ i + 3 ], colors[ i ], clip );
					colors[ i + 3 ] = color;
					colors[ i + 2 ] = color;

				}

			}

		}

		private float calculateLineAlignment( LineRenderInfo line )
		{

			if( TextAlign == TextAlignment.Left || line.length == 0 )
				return 0;

			if( line.lineWidth > MaxSize.x )
				return 0;

			if( TextAlign == TextAlignment.Right )
			{
				var horzSpacing = Mathf.Max( CharacterSpacing, 2 ) * TextScale;
				var rightAlign = ( MaxSize.x - line.lineWidth - horzSpacing ) * PixelRatio;
				return rightAlign.Quantize( PixelRatio );
			}

			var centerAlign = ( ( MaxSize.x - line.lineWidth ) * 0.5f ) * PixelRatio;
			return centerAlign.RoundToNearest( PixelRatio );

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

		private string preprocess( string text )
		{

			try
			{


				//@Profiler.BeginSample( "Preprocessing text" );

				LineRenderInfo.ResetPool();
				GlyphRenderInfo.ResetPool();

				glyphs.Clear();
				glyphs.EnsureCapacity( text.Length );

				lines.Clear();

				var markup = (MarkupParser)null;
				if( ProcessMarkup )
				{

					//@Profiler.BeginSample( "Parsing markup" );
					markup = MarkupParser.Parse( text, DefaultColor );
					//@Profiler.EndSample();

					text = markup.plainText;

				}

				this.text = text = text.Replace( "\r", " " );

				//@Profiler.BeginSample( "Preparing render information" );
				{

					var ch = '\0';
					var previous = '\x0';
					var length = text.Length;

					for( int i = 0; i < length; previous = ch, i++ )
					{

						ch = text[ i ];

						var glyphDef = Font.GetGlyph( ch );
						if( glyphDef == null )
						{

							if( ch == '\n' || ch == '\t' )
							{

								var temp = GlyphRenderInfo.Obtain();
								temp.character = ch;
								temp.textOffset = i;
								temp.height = Font.LineHeight;

								glyphs.Add( temp );

								continue;

							}

							if( ch == '\x1' )
							{

								var spriteGlyph = GlyphRenderInfo.Obtain();
								spriteGlyph.character = '\x1';
								spriteGlyph.textOffset = i;

								markup.ApplyMarkup( (dfFont)Font, i, spriteGlyph );

								// Ensure that opacity is applied in all cases
								if( OverrideMarkupColors || !ColorizeSymbols )
								{
									spriteGlyph.topColor = applyOpacity( multiplyColors( DefaultColor, spriteGlyph.topColor ) );
									spriteGlyph.bottomColor = applyOpacity( multiplyColors( DefaultColor, spriteGlyph.topColor ) );
								}
								else
								{
									spriteGlyph.topColor = applyOpacity( spriteGlyph.topColor );
									spriteGlyph.bottomColor = applyOpacity( spriteGlyph.topColor );
								}

								glyphs.Add( spriteGlyph );

								continue;

							}

							Debug.LogError( "Glyph not found in font: " + (int)ch );

							// Even if a glyph is missing from the Font we still need 
							// to keep track of it, or character offsets will be thrown 
							// off. This might happen either internally or externally 
							// since the consumer of this class likely expects that 
							// this class will have a one-to-one mapping between source
							// text and glyph/measure info. Creating a dummy glyph in
							// such a case does not adversely affect rendering.
							var missingGlyph = GlyphRenderInfo.Obtain();
							missingGlyph.character = ' ';
							missingGlyph.textOffset = i;
							missingGlyph.uvx = 1;
							missingGlyph.uvy = 1;
							missingGlyph.topColor = DefaultColor;

							glyphs.Add( missingGlyph );

							continue;

						}

						//@Profiler.BeginSample( "Process character glyph" );

						var glyphInfo = GlyphRenderInfo.Obtain();
						glyphInfo.character = ch;
						glyphInfo.textOffset = i;
						glyphInfo.kerning = Font.GetKerning( previous, ch );
						glyphInfo.xoffset = glyphDef.xoffset;
						glyphInfo.yoffset = glyphDef.yoffset;
						glyphInfo.width = glyphDef.width;
						glyphInfo.height = glyphDef.height;
						glyphInfo.advance = glyphDef.xadvance;
						glyphInfo.uvx = glyphDef.x;
						glyphInfo.uvy = glyphDef.y;
						glyphInfo.topColor = DefaultColor;

						if( markup != null )
						{
							markup.ApplyMarkup( (dfFont)Font, i, glyphInfo );
						}

						// Ensure that opacity is applied in all cases
						if( OverrideMarkupColors )
						{
							glyphInfo.topColor = applyOpacity( multiplyColors( DefaultColor, glyphInfo.topColor ) );
							glyphInfo.bottomColor = applyOpacity( multiplyColors( BottomColor ?? DefaultColor, glyphInfo.topColor ) );
						}
						else
						{
							glyphInfo.topColor = applyOpacity( glyphInfo.topColor );
							glyphInfo.bottomColor = applyOpacity( BottomColor ?? glyphInfo.topColor );
						}

						glyphs.Add( glyphInfo );

						//@Profiler.EndSample();

					}

				}
				//@Profiler.EndSample();

				// Apply TextScale to any Glyph properties that are used
				// in measuring text or advancing the render position
				for( int i = 0; i < glyphs.Count; i++ )
				{
					var glyph = glyphs[ i ];
					glyph.advance = ( glyph.advance * TextScale );
					glyph.kerning = ( glyph.kerning * TextScale );
				}

				//@Profiler.BeginSample( "Processing line breaks" );
				{

					if( MultiLine && WordWrap )
					{
						calculateWordWrap();
					}
					else if( MultiLine )
					{
						findLineBreaks();
					}
					else
					{
						lines.Add( fitSingleLine() );
					}

					measureLines();

				}
				//@Profiler.EndSample();

				if( markup != null )
				{
					markup.Release();
				}

				return text;

			}
			finally
			{
				//@Profiler.EndSample();
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

		private LineRenderInfo fitSingleLine()
		{

			var line = LineRenderInfo.Obtain( 0, 0 );

			var widths = GetCharacterWidths( text );
			var maxWidth = MaxSize.x * PixelRatio;
			var horzSpacing = CharacterSpacing * PixelRatio;

			var lineWidth = 0f;
			for( int i = 0; i < text.Length; i++ )
			{

				if( i > 0 ) lineWidth += horzSpacing;

				line.endOffset = i;

				lineWidth = Mathf.Min( maxWidth, lineWidth + widths[ i ] );
				if( lineWidth >= MaxSize.x )
					break;

			}


			return line;

		}

		private void measureLines()
		{

			var horzSpacing = CharacterSpacing * TextScale;

			for( int li = 0; li < lines.Count; li++ )
			{

				var line = lines[ li ];
				if( line.startOffset >= glyphs.Count )
				{
					Debug.LogError( "Line offset: " + line.startOffset + ", Glyphs: " + glyphs.Count );
					break;
				}

				line.lineHeight = 0f;

				// Removes the x-offset from the beginning of the line
				var xofs = glyphs[ line.startOffset ].xoffset;
				line.lineWidth = -xofs;

				for( int i = line.startOffset; i <= line.endOffset; i++ )
				{

					var glyph = glyphs[ i ];

					if( i > line.startOffset )
						line.lineWidth += horzSpacing;
					line.lineWidth += glyph.advance + glyph.kerning;

					var height = glyph.height * PixelRatio * TextScale;
					line.lineHeight = Mathf.Max( line.lineHeight, height );

				}

			}

		}

		private void calculateWordWrap()
		{

			var startIndex = 0;
			var lastBreak = 0;
			var index = 0;
			var length = text.Length;
			var horzSpacing = CharacterSpacing * TextScale;

			var lineWidth = 0f;
			var maxLineWidth = 0f;

			var scale = PixelRatio * TextScale;

			while( index < length )
			{

				if( index < 0 || index >= glyphs.Count )
				{
					Debug.LogError( "Index out of range: " + index );
					break;
				}

				if( index > startIndex )
					lineWidth += horzSpacing;

				var glyph = glyphs[ index ];
				var ch = glyph.character;

				// Need to remove the kerning from the first character in 
				// every line since kerning is not valid at that position.
				if( index == startIndex )
				{
					glyph.kerning = 0;
				}

				if( ch == '\n' )
				{
					lines.Add( LineRenderInfo.Obtain( startIndex, index - 1 ) );
					startIndex = lastBreak = ++index;
					lineWidth = 0;
					continue;
				}

				var glyphWidth = glyph.advance + glyph.kerning;

				if( ch == '\t' )
				{
					glyphWidth = getTabStop( lineWidth * scale ) / scale - lineWidth;
				}

				if( char.IsWhiteSpace( ch ) || char.IsSeparator( ch ) )
				{
					lineWidth += glyphWidth;
					lastBreak = index++;
					continue;
				}

				if( lineWidth + glyphWidth >= MaxSize.x )
				{

					if( lastBreak > startIndex )
					{
						lines.Add( LineRenderInfo.Obtain( startIndex, lastBreak - 1 ) );
						startIndex = index = ++lastBreak;
						lineWidth = 0;
					}
					else
					{
						lines.Add( LineRenderInfo.Obtain( startIndex, index ) );
						startIndex = lastBreak = ++index;
						lineWidth = 0;
					}

					continue;

				}

				lineWidth += glyphWidth;
				index += 1;

				maxLineWidth = Mathf.Max( lineWidth, maxLineWidth );

			}

			if( startIndex < length )
			{
				lines.Add( LineRenderInfo.Obtain( startIndex, length - 1 ) );
			}

			RenderedSize = new Vector2( maxLineWidth, Font.LineHeight * lines.Count );

		}

		private void findLineBreaks()
		{

			lines.Clear();

			int horzSpacing = CharacterSpacing;

			var renderWidth = 0f;
			var lineStart = 0;
			for( int i = 0; i < text.Length; i++ )
			{

				if( text[ i ] == '\n' )
				{
					lines.Add( LineRenderInfo.Obtain( lineStart, i ) );
					lineStart = i + 1;
					renderWidth = 0f;
				}
				else
				{
					var glyph = glyphs[ i ];
					renderWidth += horzSpacing + glyph.advance * PixelRatio;
				}

			}

			if( lineStart < text.Length )
			{
				lines.Add( LineRenderInfo.Obtain( lineStart, text.Length - 1 ) );
			}

		}

		#endregion

	}

#endif 

	#endregion

	#region Private nested classes

#if !USE_NEW_BMFONT_RENDERER
	private class MarkupParser
	{

		private static Queue<MarkupParser> pool = new Queue<MarkupParser>();

		private static Regex MARKUP_PATTERN = new Regex( @"(\[\/?)(?i:(?<element>color|scale|b|i|s|u|link))(\s(?<attr>.+?))*\]", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );
		private static Regex SPRITE_PATTERN = new Regex( @"(\[\/?)(?i:(?<element>sprite))(\s(?<attr>(""((\\"")|\\\\|[^""\n])*"")))*\]", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );

		private List<MarkupState> states = new List<MarkupState>();
		private List<MarkupSprite> sprites = new List<MarkupSprite>();

		internal string plainText;

		public static MarkupParser Parse( string text, Color32 defaultColor )
		{

			MarkupState.ResetPool();

			var result = pool.Count > 0 ? pool.Dequeue() : new MarkupParser();
			result.plainText = result.parseMarkup( text, defaultColor );
			result.plainText = result.parseMarkupSprites( result.plainText );

			return result;

		}

		public void Release()
		{
			states.Clear();
			sprites.Clear();
			pool.Enqueue( this );
		}

		public void ApplyMarkup( dfFont font, int offset, GlyphRenderInfo info )
		{

			try
			{

				//@Profiler.BeginSample( "Apply markup" );

				var state = getStateForOffset( offset );
				info.topColor = state.Color;
				//info.scale = state.Scale;

				var sprite = getSpriteForOffset( offset );
				if( sprite == null )
					return;

				var spriteInfo = font.Atlas[ sprite.Sprite ];
				if( spriteInfo == null )
					return;

				info.sprite = spriteInfo;

				var aspectRatio = spriteInfo.region.width / spriteInfo.region.height;
				var spriteWidth = font.LineHeight * aspectRatio;

				info.width = spriteWidth;
				info.height = font.LineHeight;
				info.advance = spriteWidth;

			}
			finally
			{
				//@Profiler.EndSample();
			}

		}

		#region Private utility methods

		private string parseMarkup( string markup, Color32 defaultColor )
		{

			var initialState = MarkupState.Obtain();
			initialState.Size = -1;
			initialState.Color = defaultColor;
			initialState.Scale = 3;

			states.Add( initialState );

			var bStack = 0;
			var iStack = 0;
			var uStack = 0;
			var sStack = 0;
			var sizeStack = new Stack<int>();
			var colorStack = new Stack<Color32>();
			var scaleStack = new Stack<float>();

			scaleStack.Push( 1f );
			sizeStack.Push( -1 );
			colorStack.Push( defaultColor );

			var matches = MARKUP_PATTERN.Matches( markup );
			foreach( Match match in matches )
			{

				var element = match.Groups[ "element" ].Value.ToLowerInvariant();
				if( match.Value.StartsWith( "[/" ) )
				{
					switch( element )
					{
						case "b": if( bStack > 0 ) bStack--; break;
						case "i": if( iStack > 0 ) iStack--; break;
						case "u": if( uStack > 0 ) uStack--; break;
						case "s": if( sStack > 0 ) sStack--; break;
						case "size": if( sizeStack.Count > 1 ) sizeStack.Pop(); break;
						case "color": if( colorStack.Count > 1 ) colorStack.Pop(); break;
						case "scale": if( scaleStack.Count > 1 ) scaleStack.Pop(); break;
					}
				}
				else
				{
					switch( element )
					{
						case "b": bStack++; break;
						case "i": iStack++; break;
						case "u": uStack++; break;
						case "s": sStack++; break;
						case "scale":
							var strScale = match.Groups[ "attr" ].Value;
							float scale = 0f;
							float.TryParse( strScale, out scale );
							scaleStack.Push( scale );
							break;
						case "size":
							var strSize = match.Groups[ "attr" ].Value;
							int size = 0;
							int.TryParse( strSize, out size );
							sizeStack.Push( size );
							break;
						case "color":
							var strColor = match.Groups[ "attr" ].Value.Trim().TrimStart( '#' );
							uint intColor = 0;
							uint.TryParse( strColor, NumberStyles.HexNumber, null, out intColor );
							colorStack.Push( UIntToColor( intColor | 0xFF000000 ) );
							break;
					}
				}

				var newState = MarkupState.Obtain();
				newState.Offset = match.Index;
				newState.Bold = bStack > 0;
				newState.Italic = iStack > 0;
				newState.Underline = uStack > 0;
				newState.Strikethrough = sStack > 0;
				newState.Color = colorStack.Peek();
				newState.Size = sizeStack.Peek();
				newState.Scale = scaleStack.Peek();

				states.Add(  newState );

			}

			var plainText = new StringBuilder( markup );
			for( int i = matches.Count - 1; i >= 0; i-- )
			{

				var index = matches[ i ].Index;
				var length = matches[ i ].Length;

				plainText.Remove( index, length );

				for( int x = i + 2; x < states.Count; x++ )
				{
					states[ x ].Offset -= length;
				}

			}

			return plainText.ToString();

		}

		private string parseMarkupSprites( string markup )
		{

			var matches = SPRITE_PATTERN.Matches( markup );
			foreach( Match match in matches )
			{

				var spriteName = match.Groups[ "attr" ].Value
					.Replace( "\"", "" )
					.Replace( "\'", "" )
					.Trim();

				sprites.Add( new MarkupSprite()
				{
					Offset = match.Index,
					Sprite = spriteName
				} );

			}

			var plainText = new StringBuilder( markup );

			for( int i = matches.Count - 1; i >= 0; i-- )
			{

				var match = matches[ i ];
				var index = match.Index;
				var length = match.Length;

				plainText.Replace( match.Value, "\x1", index, length );

				for( int x = i + 1; x < sprites.Count; x++ )
				{
					sprites[ x ].Offset -= length - 1;
				}

				for( int x = 0; x < states.Count; x++ )
				{
					if( states[ x ].Offset > index )
					{
						var adjustedOffset = states[ x ].Offset - length + 1;
						states[ x ].Offset = adjustedOffset;
					}
				}

			}

			return plainText.ToString();

		}

		private MarkupState getStateForOffset( int index )
		{

			var state = states[ 0 ];
			for( int i = 0; i < states.Count; i++ )
			{
				if( states[ i ].Offset <= index )
					state = states[ i ];
			}

			return state;

		}

		private MarkupSprite getSpriteForOffset( int index )
		{

			for( int i = 0; i < sprites.Count; i++ )
			{
				if( sprites[ i ].Offset == index )
					return sprites[ i ];
			}

			return null;

		}

		private uint ColorToUInt( Color32 color )
		{

			return (uint)(
				( (byte)color.a << 24 ) |
				( (byte)color.r << 16 ) |
				( (byte)color.g << 8 ) |
				( (byte)color.b << 0 )
			);

		}

		private Color32 UIntToColor( uint color )
		{

			var a = (byte)( color >> 24 );
			var r = (byte)( color >> 16 );
			var g = (byte)( color >> 8 );
			var b = (byte)( color >> 0 );

			return new Color32( r, g, b, a );

		}

		#endregion

		#region Private nested classes

		private class MarkupState
		{

			#region Public fields 

			public int Offset;
			public bool Bold;
			public bool Italic;
			public bool Underline;
			public bool Strikethrough;
			public Color32 Color;
			public int Size;
			public float Scale;

			#endregion

			#region Private utility methods 

			private void Reset()
			{
				Offset = 0;
				Bold = false;
				Italic = false;
				Underline = false;
				Color = UnityEngine.Color.white;
				Size = 12;
				Scale = 1f;
			}

			#endregion

			#region Object pooling

			private static dfList<MarkupState> pool = new dfList<MarkupState>();
			private static int poolIndex = 0;

			private MarkupState()
			{
			}

			public static MarkupState Obtain()
			{

				if( poolIndex >= pool.Count - 1 )
				{
					pool.Add( new MarkupState() );
				}

				var result = pool[ poolIndex ];
				result.Reset();

				poolIndex += 1;

				return result;

			}

			public static void ResetPool()
			{
				poolIndex = 0;
			}

			#endregion


		}

		private class MarkupSprite
		{
			public int Offset;
			public string Sprite;
		}

		#endregion

	}

	private class GlyphRenderInfo : IComparable<GlyphRenderInfo>
	{

		#region Public fields 

		public int textOffset;
		public Color32 topColor;
		public Color32 bottomColor;
		public char character;
		public dfAtlas.ItemInfo sprite;
		public float xoffset;
		public float yoffset;
		public float kerning;
		public float width;
		public float height;
		public float advance;
		public float uvx;
		public float uvy;

		#endregion

		#region Object pooling 

		private static dfList<GlyphRenderInfo> pool = new dfList<GlyphRenderInfo>();
		private static int poolIndex = 0;

		// Force the use of object pooling by hiding default constructor 
		private GlyphRenderInfo()
		{
		}

		public static void ResetPool()
		{
			poolIndex = 0;
		}

		public static GlyphRenderInfo Obtain()
		{

			//@Profiler.BeginSample( "Allocate glyph render info" );

			if( poolIndex >= pool.Count - 1 )
			{
				pool.Add( new GlyphRenderInfo() );
			}

			var result = pool[ poolIndex++ ];
			result.textOffset = 0;
			result.topColor = UnityEngine.Color.white;
			result.bottomColor = UnityEngine.Color.white;
			result.character = '\0';
			result.sprite = null;
			result.xoffset = 0;
			result.yoffset = 0;
			result.kerning = 0;
			result.width = 0;
			result.height = 0;
			result.advance = 0;
			result.uvx = 0;
			result.uvy = 0;

			//@Profiler.EndSample();

			return result;

		}

		#endregion

		#region IComparable<GlyphRenderInfo> Members

		public int CompareTo( GlyphRenderInfo other )
		{
			return this.textOffset.CompareTo( other.textOffset );
		}

		#endregion

	}

#endif

	private class LineRenderInfo
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
		private static int poolIndex = 0;

		private LineRenderInfo()
		{
		}

		public static void ResetPool()
		{
			poolIndex = 0;
		}

		public static LineRenderInfo Obtain( int start, int end )
		{

			if( poolIndex >= pool.Count - 1 )
			{
				pool.Add( new LineRenderInfo() );
			}

			var result = pool[ poolIndex++ ];

			result.startOffset = start;
			result.endOffset = end;
			result.lineHeight = 0;

			return result;

		}

		#endregion

	}

	#endregion

}
