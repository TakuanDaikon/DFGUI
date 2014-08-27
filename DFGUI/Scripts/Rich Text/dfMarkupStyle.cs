using System;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using UnityEngine;

public struct dfMarkupBorders
{

	public int left;
	public int top;
	public int right;
	public int bottom;

	public int horizontal { get { return left + right; } }
	public int vertical { get { return top + bottom; } }

	public dfMarkupBorders( int left, int right, int top, int bottom )
	{
		this.left = left;
		this.top = top;
		this.right = right;
		this.bottom = bottom;
	}

	public static dfMarkupBorders Parse( string value )
	{

		// https://developer.mozilla.org/en-US/docs/Web/CSS/margin#Syntax

		var result = new dfMarkupBorders();

		value = Regex.Replace( value, "\\s+", " " );
		var parts = value.Split( ' ' );

		if( parts.Length == 1 )
		{

			int num = dfMarkupStyle.ParseSize( value, 0 );
			result.left = result.right = num;
			result.top = result.bottom = num;

		}
		else if( parts.Length == 2 )
		{

			int vert = dfMarkupStyle.ParseSize( parts[ 0 ], 0 );
			result.top = result.bottom = vert;

			int horz = dfMarkupStyle.ParseSize( parts[ 1 ], 0 );
			result.left = result.right = horz;

		}
		else if( parts.Length == 3 )
		{

			int top = dfMarkupStyle.ParseSize( parts[ 0 ], 0 );
			result.top = top;

			int horz = dfMarkupStyle.ParseSize( parts[ 1 ], 0 );
			result.left = result.right = horz;

			int bottom = dfMarkupStyle.ParseSize( parts[ 2 ], 0 );
			result.bottom = bottom;

		}
		else if( parts.Length == 4 )
		{

			int top = dfMarkupStyle.ParseSize( parts[ 0 ], 0 );
			result.top = top;

			int right = dfMarkupStyle.ParseSize( parts[ 1 ], 0 );
			result.right = right;

			int bottom = dfMarkupStyle.ParseSize( parts[ 2 ], 0 );
			result.bottom = bottom;

			int left = dfMarkupStyle.ParseSize( parts[ 3 ], 0 );
			result.left = left;

		}

		return result;

	}

	#region System.Object overrides 

	public override string ToString()
	{
		return string.Format( "[T:{0},R:{1},L:{2},B:{3}]", top, right, left, bottom );
	}

	#endregion

}

public enum dfMarkupTextDecoration
{
	None,
	Underline,
	Overline,
	LineThrough
}

public enum dfMarkupTextAlign
{
	/// <summary>
	/// The inline contents are aligned to the left edge of the line box.
	/// </summary>
	Left,
	/// <summary>
	/// The inline contents are aligned to the right edge of the line box.
	/// </summary>
	Right,
	/// <summary>
	/// The inline contents are centered within the line box.
	/// </summary>
	Center,
	/// <summary>
	/// The text is justified. Text should line up their left and right edges to the left and right content edges of the paragraph.
	/// </summary>
	Justify
}

public enum dfMarkupVerticalAlign
{
	/// <summary> 
	/// Aligns the baseline of the element with the baseline of its parent. 
	/// </summary>
	Baseline,
	/// <summary>
	/// Align the top of the element and its descendants with the top of the entire line.
	/// </summary>
	Top,
	/// <summary>
	/// Aligns the middle of the element with the middle of lowercase letters in the parent.
	/// </summary>
	Middle,
	/// <summary>
	/// Align the bottom of the element and its descendants with the bottom of the entire line.
	/// </summary>
	Bottom,
	/// <summary>
	/// Do not align the element vertically
	/// </summary>
	None
}

public struct dfMarkupStyle
{

	#region Static variables 

	private static Dictionary<string, Color> namedColors = new Dictionary<string, Color>()
	{
		{ "aqua", UIntToColor( 0x00ffff ) },
		{ "black", Color.black },
		{ "blue", Color.blue },
		{ "cyan", Color.cyan },
		{ "fuchsia", UIntToColor( 0xFF00FF ) },
		{ "gray", Color.gray },
		{ "green", Color.green },
		{ "lime", UIntToColor( 0x00FF00 ) },
		{ "magenta", Color.magenta },
		{ "maroon", UIntToColor( 0x800000 ) },
		{ "navy", UIntToColor( 0x000080 ) },
		{ "olive", UIntToColor( 0x808000 ) },
		{ "orange", UIntToColor( 0xFFA500 ) },
		{ "purple", UIntToColor( 0x800080 ) },
		{ "red", Color.red },
		{ "silver", UIntToColor( 0xC0C0C0 ) },
		{ "teal", UIntToColor( 0x008080 ) },
		{ "white", Color.white },
		{ "yellow", Color.yellow }
	};

	#endregion

	#region Public fields

	/// <summary> For internal use only </summary>
	internal int Version;

	/// <summary>
	/// References the dfRichTextLabel host
	/// </summary>
	public dfRichTextLabel Host;

	/// <summary>
	/// The default atlas used when rendering sprites or blank textures
	/// </summary>
	public dfAtlas Atlas;

	/// <summary> The default font used when rendering text </summary>
	public dfDynamicFont Font;

	/// <summary> The default size in pixels of the font used when rendering text </summary>
	public int FontSize;

	/// <summary> The default font style used when rendering text </summary>
	public FontStyle FontStyle;

	public dfMarkupTextDecoration TextDecoration;

	/// <summary>
	/// The desired height (in pixels) of a line of text
	/// </summary>
	public int LineHeight
	{
		get 
		{
			if( lineHeight == 0 )
				return Mathf.CeilToInt( FontSize );
			return Mathf.Max( FontSize, lineHeight ); 
		}
		set 
		{ 
			lineHeight = value; 
		}
	}

	/// <summary> The default text alignment used when rendering text </summary>
	public dfMarkupTextAlign Align;

	/// <summary> Specifies the vertical alignment of an inline or table-cell box </summary>
	public dfMarkupVerticalAlign VerticalAlign;

	/// <summary> The foreground color used when rendering text </summary>
	public Color Color;

	/// <summary>
	/// The background color used when rendering text
	/// </summary>
	public Color BackgroundColor;

	/// <summary>
	/// The opacity level of the rendered elements
	/// </summary>
	public float Opacity;

	/// <summary> 
	/// Indicates whether all whitespace should be preserved. If set to FALSE (the 
	/// default value) all whitespace will be collapsed.
	/// </summary>
	public bool PreserveWhitespace;

	/// <summary>
	/// Indicates whether the content is preformatted (such as with the &lt;pre&gt; tag)
	/// </summary>
	public bool Preformatted;

	/// <summary>
	/// Indicates the amount of additional spacing to add between words
	/// </summary>
	public int WordSpacing;

	/// <summary>
	/// Indicates the amount of additional spacing to add between characters in text
	/// </summary>
	public int CharacterSpacing;

	#endregion 

	#region Private variables 

	private int lineHeight;

	#endregion

	#region Constructor

	public dfMarkupStyle( dfDynamicFont Font, int FontSize, FontStyle FontStyle )
	{

		Host = null;
		Atlas = null;

		this.Version = 0x00;
		this.Font = Font;
		this.FontSize = FontSize;
		this.FontStyle = FontStyle;

		Align = dfMarkupTextAlign.Left;
		VerticalAlign = dfMarkupVerticalAlign.Baseline;
		Color = UnityEngine.Color.white;
		BackgroundColor = UnityEngine.Color.clear;
		TextDecoration = dfMarkupTextDecoration.None;

		PreserveWhitespace = false;
		Preformatted = false;
		WordSpacing = 0;
		CharacterSpacing = 0;
		lineHeight = 0;
		Opacity = 1f;

	}

	#endregion 

	#region Public helper methods 

	public static dfMarkupTextDecoration ParseTextDecoration( string value )
	{
		
		if( value == "underline" )
			return dfMarkupTextDecoration.Underline;
		else if( value == "overline" )
			return dfMarkupTextDecoration.Overline;
		else if( value == "line-through" )
			return dfMarkupTextDecoration.LineThrough;

		return dfMarkupTextDecoration.None;

	}

	public static dfMarkupVerticalAlign ParseVerticalAlignment( string value )
	{
		if( value == "top" )
			return dfMarkupVerticalAlign.Top;
		else if( value == "center" || value == "middle" )
			return dfMarkupVerticalAlign.Middle;
		else if( value == "bottom" )
			return dfMarkupVerticalAlign.Bottom;
		else
			return dfMarkupVerticalAlign.Baseline;
	}

	public static dfMarkupTextAlign ParseTextAlignment( string value )
	{
		if( value == "right" )
			return dfMarkupTextAlign.Right;
		else if( value == "center" )
			return dfMarkupTextAlign.Center;
		else if( value == "justify" )
			return dfMarkupTextAlign.Justify;
		else
			return dfMarkupTextAlign.Left;
	}

	public static FontStyle ParseFontStyle( string value, FontStyle baseStyle )
	{

		if( value == "normal" )
			return FontStyle.Normal;

		if( value == "bold" )
		{
			if( baseStyle == FontStyle.Normal )
				return FontStyle.Bold;
			else if( baseStyle == FontStyle.Italic )
				return FontStyle.BoldAndItalic;
		}
		else if( value == "italic" )
		{
			if( baseStyle == FontStyle.Normal )
				return FontStyle.Italic;
			else if( baseStyle == FontStyle.Bold )
				return FontStyle.BoldAndItalic;
		}

		return baseStyle;

	}

	public static int ParseSize( string value, int baseValue )
	{

		if( value.Length > 1 && value.EndsWith( "%" ) )
		{
			int percent;
			if( int.TryParse( value.TrimEnd( '%' ), out percent ) )
			{
				return (int)( baseValue * ( percent / 100f ) );
			}
		}

		if( value.EndsWith( "px" ) )
		{
			value = value.Substring( 0, value.Length - 2 );
		}

		int height;
		if( int.TryParse( value, out height ) )
		{
			return height;
		}

		return baseValue;

	}

	public static Color ParseColor( string color, Color defaultColor )
	{

		var result = defaultColor;

		if( color.StartsWith( "#" ) )
		{

			uint intColor = 0;
			if( uint.TryParse( color.Substring( 1 ), NumberStyles.HexNumber, null, out intColor ) )
			{
				result = UIntToColor( intColor );
			}
			else
			{
				result = Color.red;
			}

		}
		else
		{

			Color named;
			if( namedColors.TryGetValue( color.ToLowerInvariant(), out named ) )
			{
				result = named;
			}

		}

		return result;

	}

	#endregion

	#region Private utility methods 
	
	private static Color32 UIntToColor( uint color )
	{

		var r = (byte)( color >> 16 );
		var g = (byte)( color >> 8 );
		var b = (byte)( color >> 0 );

		return new Color32( r, g, b, 255 );

	}

	#endregion

}

