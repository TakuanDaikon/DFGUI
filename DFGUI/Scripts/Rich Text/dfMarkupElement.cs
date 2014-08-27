using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

/****************************************************************************
 * PLEASE NOTE: The code in this file is under extremely active development
 * and is likely to change quite frequently. It is not recommended to modify
 * the code in this file, as your changes are likely to be overwritten by
 * the next product update when it is published.
 * **************************************************************************/

public class dfMarkupEntity
{

	// http://www.w3schools.com/html/html_entities.asp
	// http://www.w3schools.com/tags/ref_entities.asp

	#region Static variables

	private static List<dfMarkupEntity> HTML_ENTITIES = new List<dfMarkupEntity>
	{
		new dfMarkupEntity( "&nbsp;", " " ),
		new dfMarkupEntity( "&quot;", "\"" ),
		new dfMarkupEntity( "&amp;", "&" ),
		new dfMarkupEntity( "&lt;", "<" ),
		new dfMarkupEntity( "&gt;", ">" ),
		new dfMarkupEntity( "&#39;", "'" ),
		new dfMarkupEntity( "&trade;", "™" ),
		new dfMarkupEntity( "&copy;", "©" ),
		new dfMarkupEntity( " ", " " ) // Hard space to normal space
	};

	private static StringBuilder buffer = new StringBuilder();

	#endregion

	public string EntityName;
	public string EntityChar;

	public dfMarkupEntity( string entityName, string entityChar )
	{
		this.EntityName = entityName;
		this.EntityChar = entityChar;
	}

	#region Static methods

	public static string Replace( string text )
	{

		buffer.EnsureCapacity( text.Length );
		buffer.Length = 0;
		buffer.Append( text );

		for( int i = 0; i < HTML_ENTITIES.Count; i++ )
		{
			var entity = HTML_ENTITIES[ i ];
			buffer.Replace( entity.EntityName, entity.EntityChar );
		}

		return buffer.ToString();

	}

	#endregion

}

[AttributeUsage( AttributeTargets.Class, Inherited = true, AllowMultiple = true )]
public class dfMarkupTagInfoAttribute : System.Attribute
{
	public string TagName { get; set; }
	public dfMarkupTagInfoAttribute( string tagName )
	{
		this.TagName = tagName;
	}
}

/// <summary>
/// Base class for pseudo-HTML markup elements
/// </summary>
public abstract class dfMarkupElement
{

	public dfMarkupElement Parent { get; protected set; }

	protected List<dfMarkupElement> ChildNodes { get; private set; }

	public dfMarkupElement()
	{
		this.ChildNodes = new List<dfMarkupElement>();
	}

	public void AddChildNode( dfMarkupElement node )
	{
		node.Parent = this;
		ChildNodes.Add( node );
	}

	public void PerformLayout( dfMarkupBox container, dfMarkupStyle style )
	{
		//@Profiler.BeginSample( "Perform markup layout: " + this.GetType().Name );
		_PerformLayoutImpl( container, style );
		//@Profiler.EndSample();
	}

	internal virtual void Release()
	{
		Parent = null;
		ChildNodes.Clear();
	}

	protected abstract void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style );

}

/// <summary>
/// Represents a section of raw text
/// </summary>
public class dfMarkupString : dfMarkupElement
{

	#region Static variables 

	private static StringBuilder buffer = new StringBuilder();
	private static Regex whitespacePattern = new Regex( "\\s+" );

	private static Queue<dfMarkupString> objectPool = new Queue<dfMarkupString>();

	#endregion

	#region Public properties 

	public string Text { get; private set; }

	public bool IsWhitespace
	{
		get { return isWhitespace; }
	}

	#endregion

	#region Private variables 

	private bool isWhitespace = false;

	#endregion

	#region Constructor

	public dfMarkupString( string text )
		: base()
	{
		this.Text = processWhitespace( dfMarkupEntity.Replace( text ) );
		this.isWhitespace = whitespacePattern.IsMatch( this.Text );
	}

	#endregion

	#region System.Object overrides 

	public override string ToString()
	{
		return this.Text;
	}

	#endregion

	#region Public methods

	internal dfMarkupElement SplitWords()
	{

		//@Profiler.BeginSample( "dfMarkupString.SplitWords()" );

		var tag = dfMarkupTagSpan.Obtain();

		var i = 0;
		var x = 0;
		var length = Text.Length;

		while( i < length )
		{

			#region Words

			while( i < length && !char.IsWhiteSpace( Text[ i ] ) )
			{
				i += 1;
			}

			if( i > x )
			{
				tag.AddChildNode( dfMarkupString.Obtain( Text.Substring( x, i - x ) ) );
				x = i;
			}

			#endregion

			#region Non-newline whitespace

			while( i < length && Text[ i ] != '\n' && char.IsWhiteSpace( Text[ i ] ) )
			{
				i += 1;
			}

			if( i > x )
			{
				tag.AddChildNode( dfMarkupString.Obtain( Text.Substring( x, i - x ) ) );
				x = i;
			}

			#endregion

			#region Newlines 

			if( i < length && Text[ i ] == '\n' )
			{
				tag.AddChildNode( dfMarkupString.Obtain( "\n" ) );
				x = ++i;
			}

			#endregion

		}

		//@Profiler.EndSample();

		return tag;

	}

	#endregion

	#region dfMarkupElement overrides 

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		if( style.Font == null )
			return;

		var effectiveText = style.PreserveWhitespace || !isWhitespace ? this.Text : " ";

		var box = dfMarkupBoxText.Obtain( this, dfMarkupDisplayType.inline, style );
		box.SetText( effectiveText );

		container.AddChild( box );

	}

	#endregion

	#region Object pooling

	internal static dfMarkupString Obtain( string text )
	{

		if( objectPool.Count > 0 )
		{

			var instance = objectPool.Dequeue();
			instance.Text = dfMarkupEntity.Replace( text );
			instance.isWhitespace = whitespacePattern.IsMatch( instance.Text );

			return instance;

		}

		return new dfMarkupString( text );

	}

	internal override void Release()
	{
		base.Release();
		objectPool.Enqueue( this );
	}

	#endregion

	#region Private utility methods

	private string processWhitespace( string text )
	{

		// http://www.w3.org/TR/CSS21/text.html#white-space-prop

		buffer.Length = 0;
		buffer.Append( text );
		buffer.Replace( "\r\n", "\n" );
		buffer.Replace( "\r", "\n" );
		buffer.Replace( "\t", "    " );

		return buffer.ToString();

	}

	#endregion

}

/// <summary>
/// Represents a key/value pair attribute defined inside of a dfMarkupTag element
/// </summary>
public class dfMarkupAttribute
{

	public string Name { get; set; }
	public string Value { get; set; }

	public dfMarkupAttribute( string name, string value )
	{
		this.Name = name;
		this.Value = value;
	}

	public override string ToString()
	{
		return string.Format( "{0}='{1}'", Name, Value );
	}

}

