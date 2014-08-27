using System;
using System.Globalization;
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

/// <summary>
/// Represents a pseudo-HTML markup tag
/// </summary>
public class dfMarkupTag : dfMarkupElement
{

	#region Static variables 

	private static int ELEMENTID = 0;

	#endregion

	#region Public properties 

	public string TagName { get; set; }

	public string ID { get { return this.id; } }

	public virtual bool IsEndTag { get; set; }
	public virtual bool IsClosedTag { get; set; }
	public virtual bool IsInline { get; set; }

	public List<dfMarkupAttribute> Attributes = null;

	public dfRichTextLabel Owner
	{
		get { return this.owner; }
		set
		{
			this.owner = value;
			for( int i = 0; i < ChildNodes.Count; i++ )
			{
				var child = ChildNodes[ i ] as dfMarkupTag;
				if( child != null )
				{
					child.Owner = value;
				}
			}
		}
	}

	#endregion

	#region Private variables

	private dfRichTextLabel owner = null;
	private string id = null;

	#endregion

	#region Constructors 

	public dfMarkupTag( string tagName )
		: base()
	{
		Attributes = new List<dfMarkupAttribute>(); 
		this.TagName = tagName;
		this.id = tagName + ( ELEMENTID++ ).ToString( "X" );
	}

	public dfMarkupTag( dfMarkupTag original )
		: base()
	{

		this.TagName = original.TagName;
		this.Attributes = original.Attributes;
		this.IsEndTag = original.IsEndTag;
		this.IsClosedTag = original.IsClosedTag;
		this.IsInline = original.IsInline;
		this.id = original.id;

		var children = original.ChildNodes;
		for( int i = 0; i < children.Count; i++ )
		{
			var child = children[ i ];
			AddChildNode( child );
		}

	}

	#endregion

	#region Public methods 

	internal override void Release()
	{
		base.Release();
	}

	#endregion

	#region Protected functions

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		// Only happens while user is in the process of typing an unfinished tag
		if( this.IsEndTag )
		{
			return;
		}

		var marginAttribute = findAttribute( "margin" );
		if( marginAttribute != null )
		{
		}

		for( int i = 0; i < ChildNodes.Count; i++ )
		{
			ChildNodes[ i ].PerformLayout( container, style );
		}

	}

	protected dfMarkupStyle applyTextStyleAttributes( dfMarkupStyle style )
	{

		var fontAttribute = findAttribute( "font", "font-family" );
		if( fontAttribute != null )
		{
			style.Font = dfDynamicFont.FindByName( fontAttribute.Value ) ?? owner.Font;
		}

		var fontStyleAttribute = findAttribute( "style", "font-style" );
		if( fontStyleAttribute != null )
		{
			style.FontStyle = dfMarkupStyle.ParseFontStyle( fontStyleAttribute.Value, style.FontStyle );
		}

		var sizeAttribute = findAttribute( "size", "font-size" );
		if( sizeAttribute != null )
		{
			style.FontSize = dfMarkupStyle.ParseSize( sizeAttribute.Value, style.FontSize );
		}

		var colorAttribute = findAttribute( "color" );
		if( colorAttribute != null )
		{
			var color = dfMarkupStyle.ParseColor( colorAttribute.Value, style.Color );
			color.a = style.Opacity;
			style.Color = color;
		}

		var alignAttribute = findAttribute( "align", "text-align" );
		if( alignAttribute != null )
		{
			style.Align = dfMarkupStyle.ParseTextAlignment( alignAttribute.Value );
		}

		var vertAlignAttribute = findAttribute( "valign", "vertical-align" );
		if( vertAlignAttribute != null )
		{
			style.VerticalAlign = dfMarkupStyle.ParseVerticalAlignment( vertAlignAttribute.Value );
		}

		var lineHeightAttribute = findAttribute( "line-height" );
		if( lineHeightAttribute != null )
		{
			style.LineHeight = dfMarkupStyle.ParseSize( lineHeightAttribute.Value, style.LineHeight );
		}

		var textDecorationAttribute = findAttribute( "text-decoration" );
		if( textDecorationAttribute != null )
		{
			style.TextDecoration = dfMarkupStyle.ParseTextDecoration( textDecorationAttribute.Value );
		}

		var backgroundAttribute = findAttribute( "background", "background-color" );
		if( backgroundAttribute != null )
		{
			style.BackgroundColor = dfMarkupStyle.ParseColor( backgroundAttribute.Value, Color.clear );
			style.BackgroundColor.a = style.Opacity;
		}

		return style;

	}

	#endregion

	#region System.Object overrides

	public override string ToString()
	{

		var buffer = new StringBuilder();

		buffer.Append( "[" );

		if( this.IsEndTag )
			buffer.Append( "/" );

		buffer.Append( this.TagName );

		for( int i = 0; i < Attributes.Count; i++ )
		{
			buffer.Append( " " );
			buffer.Append( Attributes[ i ].ToString() );
		}

		if( this.IsClosedTag )
		{
			buffer.Append( "/" );
		}

		buffer.Append( "]" );

		if( !this.IsClosedTag )
		{

			for( int i = 0; i < ChildNodes.Count; i++ )
			{
				buffer.Append( ChildNodes[ i ].ToString() );
			}

			buffer.Append( "[/" );
			buffer.Append( this.TagName );
			buffer.Append( "]" );

		}

		return buffer.ToString();

	}

	#endregion

	#region Utility functions

	protected dfMarkupAttribute findAttribute( params string[] names )
	{

		for( int i = 0; i < Attributes.Count; i++ )
		{
			for( int x = 0; x < names.Length; x++ )
			{
				if( Attributes[ i ].Name == names[ x ] )
				{
					return Attributes[ i ];
				}
			}
		}

		return null;

	}

	#endregion

}

[dfMarkupTagInfo( "span" )]
public class dfMarkupTagSpan : dfMarkupTag
{

	#region Static variables 

	private static Queue<dfMarkupTagSpan> objectPool = new Queue<dfMarkupTagSpan>();

	#endregion

	#region Constructor

	public dfMarkupTagSpan()
		: base( "span" )
	{
	}

	public dfMarkupTagSpan( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		style = applyTextStyleAttributes( style );

		dfMarkupBox spanBox = container;

		var marginAttribute = findAttribute( "margin" );
		if( marginAttribute != null )
		{
			
			spanBox = new dfMarkupBox( this, dfMarkupDisplayType.inlineBlock, style );
			spanBox.Margins = dfMarkupBorders.Parse( marginAttribute.Value );

			// Span does not utilize top and bottom margins
			spanBox.Margins.top = 0;
			spanBox.Margins.bottom = 0;

			container.AddChild( spanBox );

		}

		for( int i = 0; i < ChildNodes.Count; i++ )
		{

			var child = ChildNodes[i];

			if( child is dfMarkupString )
			{
				
				var text = child as dfMarkupString;
				if( text.Text == "\n" )
				{
					if( style.PreserveWhitespace )
					{
						spanBox.AddLineBreak();
					}
					continue;
				}

			}

			child.PerformLayout( spanBox, style );

		}

	}

	internal static dfMarkupTagSpan Obtain()
	{

		if( objectPool.Count > 0 )
		{
			return objectPool.Dequeue();
		}

		return new dfMarkupTagSpan();

	}

	internal override void Release()
	{
		base.Release();
		objectPool.Enqueue( this );
	}

}

[dfMarkupTagInfo( "a" )]
public class dfMarkupTagAnchor : dfMarkupTag
{

	#region Public properties 

	public string HRef
	{
		get
		{
			var hrefAttribute = findAttribute( "href" );
			return hrefAttribute != null ? hrefAttribute.Value : "";
		}
	}

	#endregion

	#region Constructor

	public dfMarkupTagAnchor()
		: base( "a" )
	{
	}

	public dfMarkupTagAnchor( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		style.TextDecoration = dfMarkupTextDecoration.Underline;
		style = applyTextStyleAttributes( style );

		for( int i = 0; i < ChildNodes.Count; i++ )
		{

			var child = ChildNodes[ i ];

			if( child is dfMarkupString )
			{

				var text = child as dfMarkupString;
				if( text.Text == "\n" )
				{
					if( style.PreserveWhitespace )
					{
						container.AddLineBreak();
					}
					continue;
				}

			}

			child.PerformLayout( container, style );

		}

	}

}

[dfMarkupTagInfo( "ul" )]
[dfMarkupTagInfo( "ol" )]
public class dfMarkupTagList : dfMarkupTag
{

	#region Public/Internal properties 

	internal int BulletWidth { get; private set; }

	#endregion

	#region Constructor

	public dfMarkupTagList()
		: base( "ul" )
	{
	}

	public dfMarkupTagList( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		if( this.ChildNodes.Count == 0 )
			return;

		style = applyTextStyleAttributes( style );

		style.Align = dfMarkupTextAlign.Left;

		var listContainer = new dfMarkupBox( this, dfMarkupDisplayType.block, style );
		container.AddChild( listContainer );

		calculateBulletWidth( style );

		for( int i = 0; i < ChildNodes.Count; i++ )
		{

			var child = ChildNodes[ i ] as dfMarkupTag;
			if( child == null || child.TagName != "li" )
			{
				continue;
			}

			child.PerformLayout( listContainer, style );

		}

		listContainer.FitToContents();

	}

	private void calculateBulletWidth( dfMarkupStyle style )
	{

		if( this.TagName == "ul" )
		{

			var measuredBullet = style.Font.MeasureText( "•", style.FontSize, style.FontStyle );
			
			BulletWidth = Mathf.CeilToInt( measuredBullet.x );

			return;

		}

		var itemCount = 0;
		for( int i = 0; i < ChildNodes.Count; i++ )
		{

			var child = ChildNodes[ i ] as dfMarkupTag;
			if( child != null && child.TagName == "li" )
			{
				itemCount += 1;
			}

		}

		var numberText = new string( 'X', itemCount.ToString().Length ) + ".";
		var measuredNumber = style.Font.MeasureText( numberText, style.FontSize, style.FontStyle );
		
		BulletWidth = Mathf.CeilToInt( measuredNumber.x );

	}

}

[dfMarkupTagInfo( "li" )]
public class dfMarkupTagListItem : dfMarkupTag
{

	#region Constructor

	public dfMarkupTagListItem()
		: base( "li" )
	{
	}

	public dfMarkupTagListItem( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		if( this.ChildNodes.Count == 0 )
			return;

		var containerWidth = container.Size.x;

		var listItemContainer = new dfMarkupBox( this, dfMarkupDisplayType.listItem, style );
		listItemContainer.Margins.top = 10;
		container.AddChild( listItemContainer );

		var list = this.Parent as dfMarkupTagList;
		if( list == null )
		{
			// If the list item is not contained in a list, process its 
			// child elements as normal html elements
			base._PerformLayoutImpl( container, style );
			return;
		}

		style.VerticalAlign = dfMarkupVerticalAlign.Baseline;

		var bulletText = "•";
		if( list.TagName == "ol" )
		{
			bulletText = container.Children.Count + ".";
		}

		var bulletBoxStyle = style;
		bulletBoxStyle.VerticalAlign = dfMarkupVerticalAlign.Baseline;
		bulletBoxStyle.Align = dfMarkupTextAlign.Right;

		// TODO: Pre-measure bullet item size (for ordered lists) at the <UL> tag level
		var listBulletElement = dfMarkupBoxText.Obtain( this, dfMarkupDisplayType.inlineBlock, bulletBoxStyle );
		listBulletElement.SetText( bulletText );
		listBulletElement.Width = list.BulletWidth;
		listBulletElement.Margins.left = style.FontSize * 2;
		listItemContainer.AddChild( listBulletElement );

		var listItemBox = new dfMarkupBox( this, dfMarkupDisplayType.inlineBlock, style );
		var listItemLeftMargin = style.FontSize;
		var listItemWidth = containerWidth - listBulletElement.Size.x - listBulletElement.Margins.left - listItemLeftMargin;
		listItemBox.Size = new Vector2( listItemWidth, listItemLeftMargin );
		listItemBox.Margins.left = (int)( style.FontSize * 0.5f );
		listItemContainer.AddChild( listItemBox );

		for( int i = 0; i < ChildNodes.Count; i++ )
		{
			ChildNodes[ i ].PerformLayout( listItemBox, style );
		}

		listItemBox.FitToContents();

		// The listItemBox.Parent property will actually refer to an internal
		// linebox that hosts the listItemBox, which needs to be fit to the 
		// contents of the list item box.
		if( listItemBox.Parent != null )
		{
			listItemBox.Parent.FitToContents();
		}

		listItemContainer.FitToContents();

	}

}

[dfMarkupTagInfo( "p" )]
public class dfMarkupTagParagraph : dfMarkupTag
{

	#region Constructor

	public dfMarkupTagParagraph()
		: base( "p" )
	{
	}

	public dfMarkupTagParagraph( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		if( this.ChildNodes.Count == 0 )
			return;

		style = applyTextStyleAttributes( style );

		var topMargin = container.Children.Count == 0 ? 0 : style.LineHeight;

		dfMarkupBox paragraphBox = null;

		if( style.BackgroundColor.a > 0.005f )
		{

			var spriteBox = new dfMarkupBoxSprite( this, dfMarkupDisplayType.block, style );
			spriteBox.Atlas = this.Owner.Atlas;
			spriteBox.Source = this.Owner.BlankTextureSprite;
			spriteBox.Style.Color = style.BackgroundColor;

			paragraphBox = spriteBox;

		}
		else
		{
			paragraphBox = new dfMarkupBox( this, dfMarkupDisplayType.block, style );
		}

		paragraphBox.Margins = new dfMarkupBorders( 0, 0, topMargin, style.LineHeight );

		#region Allow overriding of margins and padding 

		var marginAttribute = findAttribute( "margin" );
		if( marginAttribute != null )
		{
			paragraphBox.Margins = dfMarkupBorders.Parse( marginAttribute.Value );
		}

		var paddingAttribute = findAttribute( "padding" );
		if( paddingAttribute != null )
		{
			paragraphBox.Padding = dfMarkupBorders.Parse( paddingAttribute.Value );
		}

		#endregion 

		container.AddChild( paragraphBox );

		base._PerformLayoutImpl( paragraphBox, style );

		if( paragraphBox.Children.Count > 0 )
		{
			paragraphBox.Children[ paragraphBox.Children.Count - 1 ].IsNewline = true;
		}

		paragraphBox.FitToContents( true );

	}

}

[dfMarkupTagInfo( "strong" )]
[dfMarkupTagInfo( "b" )]
public class dfMarkupTagBold : dfMarkupTag
{

	#region Constructor

	public dfMarkupTagBold()
		: base( "b" )
	{
	}

	public dfMarkupTagBold( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		style = applyTextStyleAttributes( style );

		if( style.FontStyle == FontStyle.Normal )
			style.FontStyle = FontStyle.Bold;
		else if( style.FontStyle == FontStyle.Italic )
			style.FontStyle = FontStyle.BoldAndItalic;

		base._PerformLayoutImpl( container, style );

	}

}

[dfMarkupTagInfo( "h1" )]
[dfMarkupTagInfo( "h2" )]
[dfMarkupTagInfo( "h3" )]
[dfMarkupTagInfo( "h4" )]
[dfMarkupTagInfo( "h5" )]
[dfMarkupTagInfo( "h6" )]
public class dfMarkupTagHeading : dfMarkupTag
{

	#region Constructor

	public dfMarkupTagHeading()
		: base( "h1" )
	{
	}

	public dfMarkupTagHeading( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		var headingMargins = new dfMarkupBorders();

		var headingStyle = applyDefaultStyles( style, ref headingMargins );
		headingStyle = applyTextStyleAttributes( headingStyle );

		// Allow overriding of margins
		var marginAttribute = findAttribute( "margin" );
		if( marginAttribute != null )
		{
			headingMargins = dfMarkupBorders.Parse( marginAttribute.Value );
		}

		var headingBox = new dfMarkupBox( this, dfMarkupDisplayType.block, headingStyle );
		headingBox.Margins = headingMargins;

		container.AddChild( headingBox );

		base._PerformLayoutImpl( headingBox, headingStyle );
		headingBox.FitToContents();

	}

	private dfMarkupStyle applyDefaultStyles( dfMarkupStyle style, ref dfMarkupBorders margins )
	{

		// http://www.w3.org/TR/CSS21/sample.html

		var marginSize = 1f;
		var fontSize = 1f;

		switch( TagName )
		{
			case "h1":
				fontSize = 2f;
				marginSize = 0.65f;
				break;
			case "h2":
				fontSize = 1.5f;
				marginSize = 0.75f;
				break;
			case "h3":
				fontSize = 1.35f;
				marginSize = 0.85f;
				break;
			case "h4":
				fontSize = 1.15f;
				marginSize = 0;
				break;
			case "h5":
				fontSize = 0.85f;
				marginSize = 1.5f;
				break;
			case "h6":
				fontSize = 0.75f;
				marginSize = 1.75f;
				break;
		}

		style.FontSize = (int)( style.FontSize * fontSize );
		style.FontStyle = FontStyle.Bold;
		style.Align = dfMarkupTextAlign.Left;

		marginSize *= style.FontSize;
		margins.top = margins.bottom = (int)marginSize;

		return style;

	}

}

[dfMarkupTagInfo( "em" )]
[dfMarkupTagInfo( "i" )]
public class dfMarkupTagItalic : dfMarkupTag
{

	#region Constructor

	public dfMarkupTagItalic()
		: base( "i" )
	{
	}

	public dfMarkupTagItalic( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		style = applyTextStyleAttributes( style );

		if( style.FontStyle == FontStyle.Normal )
			style.FontStyle = FontStyle.Italic;
		else if( style.FontStyle == FontStyle.Bold )
			style.FontStyle = FontStyle.BoldAndItalic;

		base._PerformLayoutImpl( container, style );

	}

}

[dfMarkupTagInfo( "pre" )]
public class dfMarkupTagPre : dfMarkupTag
{

	#region Constructor

	public dfMarkupTagPre()
		: base( "pre" )
	{
	}

	public dfMarkupTagPre( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		style = applyTextStyleAttributes( style );

		style.PreserveWhitespace = true;
		style.Preformatted = true;

		if( style.Align == dfMarkupTextAlign.Justify )
		{
			style.Align = dfMarkupTextAlign.Left;
		}

		dfMarkupBox paragraphBox = null;

		if( style.BackgroundColor.a > 0.1f )
		{

			var spriteBox = new dfMarkupBoxSprite( this, dfMarkupDisplayType.block, style );
			spriteBox.LoadImage( this.Owner.Atlas, this.Owner.BlankTextureSprite );
			spriteBox.Style.Color = style.BackgroundColor;

			paragraphBox = spriteBox;

		}
		else
		{
			paragraphBox = new dfMarkupBox( this, dfMarkupDisplayType.block, style );
		}

		#region Allow overriding of margins and padding

		var marginAttribute = findAttribute( "margin" );
		if( marginAttribute != null )
		{
			paragraphBox.Margins = dfMarkupBorders.Parse( marginAttribute.Value );
		}

		var paddingAttribute = findAttribute( "padding" );
		if( paddingAttribute != null )
		{
			paragraphBox.Padding = dfMarkupBorders.Parse( paddingAttribute.Value );
		}

		#endregion

		container.AddChild( paragraphBox );

		base._PerformLayoutImpl( paragraphBox, style );

		paragraphBox.FitToContents();

	}

}

[dfMarkupTagInfo( "br" )]
public class dfMarkupTagBr : dfMarkupTag
{

	#region Constructor

	public dfMarkupTagBr()
		: base( "br" )
	{
		this.IsClosedTag = true;
	}

	public dfMarkupTagBr( dfMarkupTag original )
		: base( original )
	{
		this.IsClosedTag = true;
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{
		container.AddLineBreak();
	}

}

[dfMarkupTagInfo( "img" )]
public class dfMarkupTagImg : dfMarkupTag
{

	#region Constructor

	public dfMarkupTagImg()
		: base( "img" )
	{
		this.IsClosedTag = true;
	}

	public dfMarkupTagImg( dfMarkupTag original )
		: base( original )
	{
		this.IsClosedTag = true;
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		if( Owner == null )
		{
			Debug.LogError( "Tag has no parent: " + this );
			return;
		}

		style = applyStyleAttributes( style );

		var sourceAttribute = findAttribute( "src" );
		if( sourceAttribute == null )
			return;

		var source = sourceAttribute.Value;

		var imageBox = createImageBox( Owner.Atlas, source, style );
		if( imageBox == null )
			return;

		var size = Vector2.zero;

		var heightAttribute = findAttribute( "height" );
		if( heightAttribute != null )
		{
			size.y = dfMarkupStyle.ParseSize( heightAttribute.Value, (int)imageBox.Size.y );
		}

		var widthAttribute = findAttribute( "width" );
		if( widthAttribute != null )
		{
			size.x = dfMarkupStyle.ParseSize( widthAttribute.Value, (int)imageBox.Size.x );
		}

		if( size.sqrMagnitude <= float.Epsilon )
		{
			size = imageBox.Size;
		}
		else if( size.x <= float.Epsilon )
		{
			size.x = size.y * ( imageBox.Size.x / imageBox.Size.y );
		}
		else if( size.y <= float.Epsilon )
		{
			size.y = size.x * ( imageBox.Size.y / imageBox.Size.x );
		}

		imageBox.Size = size;
		imageBox.Baseline = (int)size.y;

		container.AddChild( imageBox );

	}

	private dfMarkupStyle applyStyleAttributes( dfMarkupStyle style )
	{

		var alignAttribute = findAttribute( "valign" );
		if( alignAttribute != null )
		{
			style.VerticalAlign = dfMarkupStyle.ParseVerticalAlignment( alignAttribute.Value );
		}

		var fontColorAttribute = findAttribute( "color" );
		if( fontColorAttribute != null )
		{
			var color = dfMarkupStyle.ParseColor( fontColorAttribute.Value, Owner.Color );
			color.a = style.Opacity;
			style.Color = color;
		}

		return style;

	}

	private dfMarkupBox createImageBox( dfAtlas atlas, string source, dfMarkupStyle style )
	{

		if( source.ToLowerInvariant().StartsWith( "http://" ) )
		{
			return null;
		}
		else if( atlas != null && atlas[ source ] != null )
		{

			var spriteBox = new dfMarkupBoxSprite( this, dfMarkupDisplayType.inline, style );
			spriteBox.LoadImage( atlas, source );

			return spriteBox;

		}
		else
		{
			var texture = dfMarkupImageCache.Load( source );
			if( texture != null )
			{

				var textureBox = new dfMarkupBoxTexture( this, dfMarkupDisplayType.inline, style );
				textureBox.LoadTexture( texture );

				return textureBox;

			}
		}

		return null;

	}

}

[dfMarkupTagInfo( "font" )]
public class dfMarkupTagFont : dfMarkupTag
{

	#region Constructor

	public dfMarkupTagFont()
		: base( "font" )
	{
	}

	public dfMarkupTagFont( dfMarkupTag original )
		: base( original )
	{
	}

	#endregion

	protected override void _PerformLayoutImpl( dfMarkupBox container, dfMarkupStyle style )
	{

		var fontAttribute = findAttribute( "name", "face" );
		if( fontAttribute != null )
		{
			style.Font = dfDynamicFont.FindByName( fontAttribute.Value ) ?? style.Font;
		}

		var fontSizeAttribute = findAttribute( "size", "font-size" );
		if( fontSizeAttribute != null )
		{
			style.FontSize = dfMarkupStyle.ParseSize( fontSizeAttribute.Value, style.FontSize );
		}

		var fontColorAttribute = findAttribute( "color" );
		if( fontColorAttribute != null )
		{
			style.Color = dfMarkupStyle.ParseColor( fontColorAttribute.Value, Color.red );
			style.Color.a = style.Opacity;
		}

		var fontStyleAttribute = findAttribute( "style" );
		if( fontStyleAttribute != null )
		{
			style.FontStyle = dfMarkupStyle.ParseFontStyle( fontStyleAttribute.Value, style.FontStyle );
		}

		base._PerformLayoutImpl( container, style );

	}

}

