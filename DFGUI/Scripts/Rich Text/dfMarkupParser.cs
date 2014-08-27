using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

/****************************************************************************
 * PLEASE NOTE: The code in this file is under extremely active development
 * and is likely to change quite frequently. It is not recommended to modify
 * the code in this file, as your changes are likely to be overwritten by
 * the next product update when it is published.
 * **************************************************************************/

/*
 * INTERESTING LINKS:
 * 
 * Fully managed HTML engine: http://htmlrenderer.codeplex.com/
 * Visual formatting model: http://www.w3.org/TR/CSS21/visuren.html
 * CSS blocks spec: http://www.w3.org/TR/CSS21/syndata.html#block
 * Default style sheet for HTML4: http://www.w3.org/TR/CSS21/sample.html
 * Anonymous inline boxes: http://www.w3.org/TR/CSS21/visuren.html#anonymous
 * Table height layout: http://www.w3.org/TR/CSS21/tables.html#height-layout
 * CSS length units: http://www.w3.org/TR/CSS21/syndata.html#length-units
 * CSS font element: http://www.w3.org/TR/CSS21/fonts.html#font-shorthand
 * Font metrics: http://msdn.microsoft.com/en-us/library/xwf9s90b(VS.71).aspx
 * 
 */

/// <summary>
/// Parses pseudo-HTML markup into a list of dfMarkupElement instances
/// </summary>
public class dfMarkupParser
{

	#region Static variables

	private static Regex TAG_PATTERN = null;
	private static Regex ATTR_PATTERN = null;
	private static Regex STYLE_PATTERN = null;
	private static Dictionary<string, Type> tagTypes = null;

	private static dfMarkupParser parserInstance = new dfMarkupParser();

	#endregion

	#region Static class constructor

	static dfMarkupParser()
	{

		var options = RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

		TAG_PATTERN = new Regex( @"(\<\/?)(?<tag>[a-zA-Z0-9$_]+)(\s(?<attr>.+?))?([\/]*\>)", options );
		ATTR_PATTERN = new Regex( @"(?<key>[a-zA-Z0-9$_]+)=(?<value>(""((\\"")|\\\\|[^""\n])*"")|('((\\')|\\\\|[^'\n])*')|\d+|\w+)", options );
		STYLE_PATTERN = new Regex( @"(?<key>[a-zA-Z0-9\-]+)(\s*\:\s*)(?<value>[^;]+)", options );

	}

	#endregion

	#region Static methods

	/// <summary>
	/// Parses pseudo-HTML markup into a list of dfMarkupElement instances
	/// </summary>
	public static dfList<dfMarkupElement> Parse( dfRichTextLabel owner, string source )
	{

		try
		{

			//@Profiler.BeginSample( "Parse markup" );

			parserInstance.owner = owner;

			var result = parserInstance.parseMarkup( source );

			return result;

		}
		finally
		{
			//@Profiler.EndSample();
		}

	}

	#endregion

	#region Private variables 

	private dfRichTextLabel owner = null;

	#endregion

	#region Private utility methods

	private dfList<dfMarkupElement> parseMarkup( string source )
	{

		//@Profiler.BeginSample( "Parse markup: Tokenize" );

		var tokens = new Queue<dfMarkupElement>();

		var matches = TAG_PATTERN.Matches( source );

		var index = 0;
		for( int i = 0; i < matches.Count; i++ )
		{

			var match = matches[ i ];

			if( match.Index > index )
			{

				var text = source.Substring( index, match.Index - index );
				var textElement = new dfMarkupString( text );

				tokens.Enqueue( textElement );

			}

			index = match.Index + match.Length;

			tokens.Enqueue( parseTag( match ) );

		}

		if( index < source.Length )
		{
			var text = source.Substring( index );
			var textElement = new dfMarkupString( text );
			tokens.Enqueue( textElement );
		}

		//@Profiler.EndSample();

		return processTokens( tokens );

	}

	private dfList<dfMarkupElement> processTokens( Queue<dfMarkupElement> tokens )
	{

		//@Profiler.BeginSample( "Parse markup: Process tokens" );

		var elements = dfList<dfMarkupElement>.Obtain();

		while( tokens.Count > 0 )
		{
			elements.Add( parseElement( tokens ) );
		}

		for( int i = 0; i < elements.Count; i++ )
		{
			if( elements[ i ] is dfMarkupTag )
			{
				( (dfMarkupTag)elements[ i ] ).Owner = this.owner;
			}
		}

		//@Profiler.EndSample();

		return elements;

	}

	private dfMarkupElement parseElement( Queue<dfMarkupElement> tokens )
	{

		var token = tokens.Dequeue();

		if( token is dfMarkupString )
			return ( (dfMarkupString)token ).SplitWords();

		var tag = (dfMarkupTag)token;
		if( tag.IsClosedTag || tag.IsEndTag )
		{
			return refineTag( tag );
		}

		while( tokens.Count > 0 )
		{

			var child = parseElement( tokens );

			if( child is dfMarkupTag )
			{

				var childTag = (dfMarkupTag)child;
				if( childTag.IsEndTag )
				{

					if( childTag.TagName == tag.TagName )
						break;

					return refineTag( tag );

				}

			}

			tag.AddChildNode( child );

		}

		return refineTag( tag );

	}

	/// <summary>
	/// Refines a generic dfMarkupTag instance into a more specific
	/// derived class instance, if one can be determined
	/// </summary>
	/// <param name="original"></param>
	/// <returns></returns>
	private dfMarkupTag refineTag( dfMarkupTag original )
	{

		// Don't bother refining end tags, they only exist to indicate
		// when we are done parsing/processing a tag that has child
		// elements.
		if( original.IsEndTag )
			return original;

		if( tagTypes == null )
		{

			tagTypes = new Dictionary<string, Type>();

			var assemblyTypes = getAssemblyTypes();

			for( int i = 0; i < assemblyTypes.Length; i++ )
			{
				
				var type = assemblyTypes[ i ];
				
				if( !typeof( dfMarkupTag ).IsAssignableFrom( type ) )
				{
					continue;
				}

				var attributes = type.GetCustomAttributes( typeof( dfMarkupTagInfoAttribute ), true );
				if( attributes == null || attributes.Length == 0 )
					continue;

				for( int x = 0; x < attributes.Length; x++ )
				{
					var tagName = ( (dfMarkupTagInfoAttribute)attributes[ x ] ).TagName;
					tagTypes[ tagName ] = type;
				}

			}

		}

		if( tagTypes.ContainsKey( original.TagName ) )
		{
			var tagType = tagTypes[original.TagName];
			return (dfMarkupTag)Activator.CreateInstance( tagType, original );
		}

		return original;

	}

	private Type[] getAssemblyTypes()
	{
#if !UNITY_EDITOR && UNITY_METRO
		return typeof( dfMarkupParser ).GetTypeInfo().Assembly.ExportedTypes.ToArray();
#else
		return Assembly.GetExecutingAssembly().GetExportedTypes();
#endif
	}

	private dfMarkupElement parseTag( System.Text.RegularExpressions.Match tag )
	{

		var tagName = tag.Groups[ "tag" ].Value.ToLowerInvariant();
		if( tag.Value.StartsWith( "</" ) )
		{
			return new dfMarkupTag( tagName ) { IsEndTag = true };
		}

		var element = new dfMarkupTag( tagName );

		var attributes = tag.Groups[ "attr" ].Value;
		var matches = ATTR_PATTERN.Matches( attributes );
		for( int i = 0; i < matches.Count; i++ )
		{

			var attrMatch = matches[ i ];

			var key = attrMatch.Groups[ "key" ].Value;
			var value = dfMarkupEntity.Replace( attrMatch.Groups[ "value" ].Value );

			if( value.StartsWith( "\"" ) )
				value = value.Trim( '"' );
			else if( value.StartsWith( "'" ) )
				value = value.Trim( '\'' );

			if( string.IsNullOrEmpty( value ) )
				continue;

			if( key == "style" )
			{
				parseStyleAttribute( element, value );
			}
			else
			{
				element.Attributes.Add( new dfMarkupAttribute( key, value ) );
			}

		}

		if( tag.Value.EndsWith( "/>" ) || tagName == "br" || tagName == "img" )
		{
			element.IsClosedTag = true;
		}

		return element;

	}

	private void parseStyleAttribute( dfMarkupTag element, string text )
	{

		var matches = STYLE_PATTERN.Matches( text );
		for( int i = 0; i < matches.Count; i++ )
		{

			var match = matches[ i ];

			var key = match.Groups[ "key" ].Value.ToLowerInvariant();
			var value = match.Groups[ "value" ].Value;

			element.Attributes.Add( new dfMarkupAttribute( key, value ) );

		}

	}

	#endregion

}
