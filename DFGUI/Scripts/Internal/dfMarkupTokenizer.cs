using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

/// <summary>
/// Used to indicate the lexical meaning of a dfMarkupToken instance
/// </summary>
public enum dfMarkupTokenType
{
	/// <summary>
	/// The type of token has not been assigned.
	/// </summary>
	Invalid = 0,
	/// <summary>
	/// A section of plain text
	/// </summary>
	Text,
	/// <summary>
	/// One or more whitespace characters
	/// </summary>
	Whitespace,
	/// <summary>
	/// A newline ('\n') character
	/// </summary>
	Newline,
	/// <summary>
	/// A start tag
	/// </summary>
	StartTag,
	/// <summary>
	/// And end tag
	/// </summary>
	EndTag
}

public class dfMarkupToken : IPoolable
{

	#region Object pooling

	private static dfList<dfMarkupToken> pool = new dfList<dfMarkupToken>();

	protected dfMarkupToken()
	{
		// Prevents the use of the default constructor
		// outside of this class, encouraging the use
		// of object pooling
	}

	public static dfMarkupToken Obtain( string source, dfMarkupTokenType type, int startIndex, int endIndex )
	{

		var instance = ( pool.Count > 0 ) ? pool.Pop() : new dfMarkupToken();

		instance.inUse = true;
		instance.Source = source;
		instance.TokenType = type;
		instance.StartOffset = startIndex;
		instance.EndOffset = Mathf.Min( source.Length - 1, endIndex );

		return instance;

	}

	public void Release()
	{

		// HACK: For some reason, DFGUI 1.x seems to be adding some tokens back to the 
		// pool multiple times, and due to scheduling pressure I can't take a week to 
		// sort out why. This "bandaid" will let me issue an update in the meantime.
		// TODO: Determine why markup tokens are being added back to the pool mutliple times.
		if( !inUse )
			return;

		inUse = false;

		this.value = null;
		this.Source = null;

		this.TokenType = dfMarkupTokenType.Invalid;
		this.Width = this.Height = 0;
		this.StartOffset = this.EndOffset = 0;

		this.attributes.ReleaseItems();

		pool.Add( this );

	}

	#endregion

	#region Private fields

	private bool inUse = false;

	/// <summary>
	/// Used to cache the string representation of the token
	/// </summary>
	private string value = null;

	/// <summary>
	/// Used to hold the attributes, if any, associated with the token
	/// </summary>
	private dfList<dfMarkupTokenAttribute> attributes = new dfList<dfMarkupTokenAttribute>();

	#endregion

	#region Public properties

	/// <summary>
	/// Returns the number of dfMarkupTokenAttribute attributes 
	/// that were defined on this token (assumes that the token
	/// is of type dfMarkupTokenType.StartTag)
	/// </summary>
	public int AttributeCount
	{
		get { return this.attributes.Count; }
	}

	/// <summary>
	/// Indicates the type of token representedS
	/// </summary>
	public dfMarkupTokenType TokenType { get; private set; }

	/// <summary>
	/// Returns a reference to the original markup source
	/// </summary>
	public string Source { get; private set; }

	/// <summary>
	/// Indicates the index within the source markup where this token starts
	/// </summary>
	public int StartOffset { get; private set; }

	/// <summary>
	/// Indicates the index within the source markup where this token ends
	/// </summary>
	public int EndOffset { get; private set; }

	/// <summary>
	/// Gets the rendered width of the token 
	/// </summary>
	public int Width { get; internal set; }

	/// <summary>
	/// Gets the rendered height of the tokenS
	/// </summary>
	public int Height { get; set; }

	/// <summary>
	/// Returns the length of the token's text (if this token represents a start
	/// tag or end tag, this property returns the length of the tag name, omitting
	/// the enclosing braces and slash)
	/// </summary>
	public int Length
	{
		get
		{
			return EndOffset - StartOffset + 1;
		}
	}

	/// <summary>
	/// Returns the string value of the token. 
	/// Do not use this property unless absolutely necessary, 
	/// as it will result in a memory allocation.
	/// </summary>
	public string Value
	{
		get
		{

			if( value == null )
			{

				var length = Mathf.Min( EndOffset - StartOffset + 1, Source.Length - StartOffset );

				value = Source.Substring( StartOffset, length );

			}

			return value;

		}
	}

	/// <summary>
	/// Returns the character at the specified index
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public char this[ int index ]
	{
		get
		{

			if( index < 0 || index >= Length )
				throw new System.IndexOutOfRangeException( string.Format( "Index {0} is out of range ({2}:{1})", index, Length, Value ) );

			return Source[ StartOffset + index ];

		}
	}

	#endregion

	#region Public methods

	internal bool Matches( dfMarkupToken other )
	{

		var length = this.Length;

		if( length != other.Length )
			return false;

		for( int i = 0; i < length; i++ )
		{
			if( char.ToLower( Source[ StartOffset + i ] ) != char.ToLower( other.Source[ other.StartOffset + i ] ) )
				return false;
		}

		return true;

	}

	internal bool Matches( string value )
	{

		var length = this.Length;
		if( length != value.Length )
			return false;

		for( int i = 0; i < length; i++ )
		{
			if( char.ToLower( Source[ StartOffset + i ] ) != char.ToLower( value[ i ] ) )
				return false;
		}

		return true;

	}

	internal void AddAttribute( dfMarkupToken key, dfMarkupToken value )
	{
		attributes.Add( dfMarkupTokenAttribute.Obtain( key, value ) );
	}

	public dfMarkupTokenAttribute GetAttribute( int index )
	{

		if( index < 0 || index >= attributes.Count )
			throw new System.IndexOutOfRangeException( "Invalid attribute index: " + index );

		return attributes[ index ];

	}

	#endregion

	#region System.Object overrides

#if false // For debugging use only!
		public override string ToString()
		{

			// This method is for debugging purposes only, and should never be relied 
			// on by runtime code, as it is slow and inefficient and performs several
			// memory allocations

			if( TokenType == MarkupTokenType.EndTag )
				return "</" + this.Value + ">";

			var result = TokenType == MarkupTokenType.StartTag ? "<" : "";

			result += this.Value;

			if( AttributeCount > 0 )
			{

				result += " [";

				for( int i = 0; i < AttributeCount; i++ )
				{

					if( i > 0 )
						result += ", ";

					var attribute = GetAttribute( i );
					result += attribute.Key.Value;
					result += "='";
					result += attribute.Value.Value;
					result += "'";

				}

				result += "]";

			}

			result += TokenType == MarkupTokenType.StartTag ? ">" : "";

			return result;

		}
#endif

	#endregion

}

public class dfMarkupTokenAttribute : IPoolable
{

	#region Public properties and fields

	public dfMarkupToken Key;
	public dfMarkupToken Value;

	#endregion

	#region Object pooling

	private static dfList<dfMarkupTokenAttribute> pool = new dfList<dfMarkupTokenAttribute>();

	private dfMarkupTokenAttribute()
	{
		// Prevents the use of the default constructor
		// outside of this class, encouraging use of 
		// object pooling
	}

	public static dfMarkupTokenAttribute Obtain( dfMarkupToken key, dfMarkupToken value )
	{

		var instance = ( pool.Count > 0 ) ? pool.Pop() : new dfMarkupTokenAttribute();
		instance.Key = key;
		instance.Value = value;

		return instance;

	}

	#endregion

	#region IPoolable Members

	public void Release()
	{

		if( this.Key != null )
		{
			this.Key.Release();
			this.Key = null;
		}

		if( this.Value != null )
		{
			this.Value.Release();
			this.Value = null;
		}

		if( !pool.Contains( this ) )
		{
			pool.Add( this );
		}

	}

	#endregion

}

public class dfMarkupTokenizer : IDisposable, IPoolable
{

	#region Object pooling

	private static dfList<dfMarkupTokenizer> pool = new dfList<dfMarkupTokenizer>();

	public static dfList<dfMarkupToken> Tokenize( string source )
	{

		using( var tokenizer = ( pool.Count > 0 ) ? pool.Pop() : new dfMarkupTokenizer() )
		{
			return tokenizer.tokenize( source );
		}

	}

	public void Release()
	{

		this.source = null;
		this.index = 0;

		if( !pool.Contains( this ) )
		{
			pool.Add( this );
		}

	}

	#endregion

	#region Private variables

	private static List<string> validTags = new List<string>() { "color", "sprite" };

	private string source;
	private int index;

	#endregion

	#region Private utility methods

	private dfList<dfMarkupToken> tokenize( string source )
	{

		var tokens = dfList<dfMarkupToken>.Obtain();
		tokens.EnsureCapacity( estimateTokenCount( source ) );
		tokens.AutoReleaseItems = true;

		this.source = source;
		this.index = 0;

		while( index < source.Length )
		{

			var next = Peek();

			if( AtTagPosition() )
			{

				var tagToken = parseTag();
				if( tagToken != null )
				{
					tokens.Add( tagToken );
				}

				continue;

			}

			dfMarkupToken token = null;

			if( char.IsWhiteSpace( next ) )
			{
				// Skip carriage return, parse all other whitespace
				if( next != '\r' )
				{
					token = parseWhitespace();
				}
			}
			else
			{
				token = parseNonWhitespace();
			}

			if( token == null )
			{
				Advance();
			}
			else
			{
				tokens.Add( token );
			}

		}

		return tokens;

	}

	private int estimateTokenCount( string source )
	{

		if( string.IsNullOrEmpty( source ) )
			return 0;

		int tokenCount = 1;

		var isWhitespace = char.IsWhiteSpace( source[ 0 ] );

		for( int i = 1; i < source.Length; i++ )
		{

			var ch = source[ i ];

			if( char.IsControl( ch ) || ch == '<' )
			{
				tokenCount += 1;
			}
			else
			{
				var charIsWhitespace = char.IsWhiteSpace( ch );
				if( charIsWhitespace != isWhitespace )
				{
					tokenCount += 1;
					isWhitespace = charIsWhitespace;
				}
			}

		}

		return tokenCount;

	}

	private bool AtTagPosition()
	{

		if( Peek() != '[' )
			return false;

		var next = Peek( 1 );
		if( next == '/' )
		{

			if( char.IsLetter( Peek( 2 ) ) )
				return isValidTag( index + 2, true );

			return false;

		}

		if( char.IsLetter( next ) )
			return isValidTag( index + 1, false );

		return false;

	}

	private bool isValidTag( int index, bool endTag )
	{

		for( int i = 0; i < validTags.Count; i++ )
		{

			var tag = validTags[ i ];
			var isTagValid = true;

			for( int x = 0; x < tag.Length - 1 && x + index < source.Length - 1; x++ )
			{

				if( !endTag && source[ x + index ] == ' ' )
					break;

				if( source[ x + index ] == ']' )
					break;

				if( char.ToLowerInvariant( tag[ x ] ) != char.ToLowerInvariant( source[ x + index ] ) )
				{
					isTagValid = false;
					break;
				}
			}

			if( isTagValid )
				return true;

		}

		return false;

	}

	private dfMarkupToken parseQuotedString()
	{

		var delim = Peek();
		if( delim != '"' && delim != '\'' )
		{
			return null;
		}

		Advance();

		var startOffset = index;
		var endoffset = index;

		while( index < source.Length && Advance() != delim )
		{
			endoffset += 1;
		}

		if( Peek() == delim )
			Advance();

		var token = dfMarkupToken.Obtain( source, dfMarkupTokenType.Text, startOffset, endoffset );
		return token;

	}

	private dfMarkupToken parseNonWhitespace()
	{

		var startOffset = index;
		var endOffset = index;

		while( index < source.Length )
		{

			var next = Advance();
			if( char.IsWhiteSpace( next ) || AtTagPosition() )
			{
				break;
			}

			endOffset += 1;

		}

		var token = dfMarkupToken.Obtain( source, dfMarkupTokenType.Text, startOffset, endOffset );
		return token;

	}

	private dfMarkupToken parseWhitespace()
	{

		var startOffset = index;
		var endOffset = index;

		// Newlines are always treated as a single token, even
		// when they are consecutive
		if( Peek() == '\n' )
		{

			Advance();

			return dfMarkupToken.Obtain(
				source,
				dfMarkupTokenType.Newline,
				startOffset,
				startOffset
			);

		}

		while( index < source.Length )
		{

			var next = Advance();
			if( next == '\n' || next == '\r' || !char.IsWhiteSpace( next ) )
			{
				break;
			}

			endOffset += 1;

		}

		var token = dfMarkupToken.Obtain( source, dfMarkupTokenType.Whitespace, startOffset, endOffset );
		return token;

	}

	private dfMarkupToken parseWord()
	{

		if( !char.IsLetter( Peek() ) )
			return null;

		var startOffset = index;
		var endOffset = index;

		while( index < source.Length && char.IsLetter( Advance() ) )
		{
			endOffset += 1;
		}

		var token = dfMarkupToken.Obtain( source, dfMarkupTokenType.Text, startOffset, endOffset );
		return token;

	}

	private dfMarkupToken parseTag()
	{

		if( Peek() != '[' )
			return null;

		var next = Peek( 1 );

		if( next == '/' )
		{
			return parseEndTag();
		}

		Advance();

		next = Peek();
		if( !char.IsLetterOrDigit( next ) )
		{
			return null;
		}

		var startOffset = index;
		var endOffset = index;
		while( index < source.Length && char.IsLetterOrDigit( Advance() ) )
		{
			endOffset += 1;
		}

		var token = dfMarkupToken.Obtain( source, dfMarkupTokenType.StartTag, startOffset, endOffset );

		if( index < source.Length && Peek() != ']' )
		{

			next = Peek();
			if( char.IsWhiteSpace( next ) )
			{
				parseWhitespace();
			}

			var attributeStart = index;
			var attributeEnd = index;

			if( Peek() == '"' )
			{
				var attribute = parseQuotedString();
				token.AddAttribute( attribute, attribute );
			}
			else
			{

				while( index < source.Length && Advance() != ']' )
				{
					attributeEnd += 1;
				}

				var attribute = dfMarkupToken.Obtain(
					source,
					dfMarkupTokenType.Text,
					attributeStart,
					attributeEnd
				);

				token.AddAttribute( attribute, attribute );

			}

		}

		if( Peek() == ']' )
			Advance();

		return token;

	}

	private dfMarkupToken parseAttributeValue()
	{

		var startOffset = index;
		var endOffset = index;

		while( index < source.Length )
		{

			var next = Advance();
			if( next == ']' || char.IsWhiteSpace( next ) )
			{
				break;
			}

			endOffset += 1;

		}

		var token = dfMarkupToken.Obtain( source, dfMarkupTokenType.Text, startOffset, endOffset );
		return token;

	}

	private dfMarkupToken parseEndTag()
	{

		// Advance past </
		Advance( 2 );

		var startOffset = index;
		var endOffset = index;

		while( index < source.Length && char.IsLetterOrDigit( Advance() ) )
		{
			endOffset += 1;
		}

		if( Peek() == ']' )
			Advance();

		var token = dfMarkupToken.Obtain(
			source,
			dfMarkupTokenType.EndTag,
			startOffset,
			endOffset
		);

		return token;

	}

	private char Peek()
	{
		return Peek( 0 );
	}

	private char Peek( int offset )
	{

		if( index + offset > source.Length - 1 )
		{
			return '\0';
		}

		return source[ index + offset ];

	}

	private char Advance()
	{
		return Advance( 1 );
	}

	private char Advance( int amount )
	{
		index += amount;
		return Peek();
	}

	#endregion

	#region IDisposable Members

	public void Dispose()
	{
		Release();
	}

	#endregion

}

public class dfPlainTextTokenizer
{

	#region Singleton

	private static dfPlainTextTokenizer singleton;

	public static dfList<dfMarkupToken> Tokenize( string source )
	{

		if( singleton == null ) singleton = new dfPlainTextTokenizer();

		return singleton.tokenize( source );

	}

	#endregion

	#region Private utility methods

	private dfList<dfMarkupToken> tokenize( string source )
	{

		var tokens = dfList<dfMarkupToken>.Obtain();
		tokens.EnsureCapacity( estimateTokenCount( source ) );
		tokens.AutoReleaseItems = true;

		var i = 0;
		var x = 0;
		var length = source.Length;

		while( i < length )
		{

			// Skip carriage returns altogether
			if( source[ i ] == '\r' )
			{
				i += 1;
				x = i;
				continue;
			}

			#region Extract non-whitespace text

			while( i < length && !char.IsWhiteSpace( source[ i ] ) )
			{
				i += 1;
			}

			if( i > x )
			{

				tokens.Add( dfMarkupToken.Obtain(
					source,
					dfMarkupTokenType.Text,
					x,
					i - 1
				) );

				x = i;

			}

			#endregion

			#region Extract newlines seperately from other whitespace

			if( i < length && source[ i ] == '\n' )
			{

				tokens.Add( dfMarkupToken.Obtain(
					source,
					dfMarkupTokenType.Newline,
					i,
					i
				) );

				i += 1;
				x = i;

			}

			#endregion

			#region Extract whitespace

			while( i < length && source[ i ] != '\n' && source[ i ] != '\r' && char.IsWhiteSpace( source[ i ] ) )
			{
				i += 1;
			}

			if( i > x )
			{

				tokens.Add( dfMarkupToken.Obtain(
					source,
					dfMarkupTokenType.Whitespace,
					x,
					i - 1
				) );

				x = i;

			}

			#endregion

		}

		return tokens;

	}

	private int estimateTokenCount( string source )
	{

		if( string.IsNullOrEmpty( source ) )
			return 0;

		int tokenCount = 1;

		var isWhitespace = char.IsWhiteSpace( source[ 0 ] );

		for( int i = 1; i < source.Length; i++ )
		{

			var ch = source[ i ];

			if( char.IsControl( ch ) )
			{
				tokenCount += 1;
			}
			else
			{
				var charIsWhitespace = char.IsWhiteSpace( ch );
				if( charIsWhitespace != isWhitespace )
				{
					tokenCount += 1;
					isWhitespace = charIsWhitespace;
				}
			}

		}

		return tokenCount;

	}

	#endregion

}
