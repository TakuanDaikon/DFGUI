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

public enum dfMarkupDisplayType
{

	// http://www.w3.org/TR/CSS21/visuren.html#propdef-display
	// http://www.w3.org/TR/CSS21/tables.html

	/// <summary> This value causes an element to generate one or more inline boxes. (Default value)</summary>
	inline,
	/// <summary> This value causes an element to generate a block box. </summary>
	block,
	/// <summary> This value causes an element (e.g., LI in HTML) to generate a principal block box and a marker box. </summary>
	listItem,
	/// <summary> This value causes an element to generate an inline-level block container. The inside of an inline-block is formatted as a block box, and the element itself is formatted as an atomic inline-level box. </summary>
	inlineBlock,
	/// <summary> Specifies that an element defines a block-level table: it is a rectangular block that participates in a block formatting context. </summary>
	table,
	/// <summary> Specifies that an element defines an inline-level table: it is a rectangular block that participates in an inline formatting context). </summary>
	inlineTable,
	/// <summary> Specifies that an element groups one or more rows. </summary>
	tableRowGroup,
	/// <summary> Like 'table-row-group', but for visual formatting, the row group is always displayed before all other rows and row groups and after any top captions. Print user agents may repeat header rows on each page spanned by a table. If a table contains multiple elements with 'display: table-header-group', only the first is rendered as a header; the others are treated as if they had 'display: table-row-group'. </summary>
	tableHeaderGroup,
	/// <summary> Like 'table-row-group', but for visual formatting, the row group is always displayed after all other rows and row groups and before any bottom captions. Print user agents may repeat footer rows on each page spanned by a table. If a table contains multiple elements with 'display: table-footer-group', only the first is rendered as a footer; the others are treated as if they had 'display: table-row-group'. </summary>
	tableFooterGroup,
	/// <summary> Specifies that an element is a row of cells. </summary>
	tableRow,
	/// <summary> Specifies that an element groups one or more columns. </summary>
	tableColumnGroup,
	/// <summary> Specifies that an element describes a column of cells. </summary>
	tableColumn,
	/// <summary> Specifies that an element represents a table cell. </summary>
	tableCell,
	/// <summary> Specifies a caption for the table. All elements with 'display: table-caption' must be rendered, as described in section 17.4. </summary>
	tableCaption,
	/// <summary> This value causes an element to not appear in the formatting structure. 
	/// Descendant elements do not generate any boxes either; the element and its content 
	/// are removed from the formatting structure entirely. This behavior cannot be 
	/// overridden by setting the 'display' property on the descendants. Please note that 
	/// a display of 'none' does not create an invisible box; it creates no box at all. 
	/// CSS includes mechanisms that enable an element to generate boxes in the formatting 
	/// structure that affect formatting but are not visible themselves. Please consult the 
	/// section on visibility for details. </summary>
	none,
	inherit
}

public class dfMarkupBox
{

	#region Public fields

	/// <summary> Returns a reference to the dfMarkupBox object which contains this instance </summary>
	public dfMarkupBox Parent { get; protected set; }

	/// <summary>
	/// Returns a reference to the dfMarkupElement object that created this instance
	/// </summary>
	public dfMarkupElement Element { get; protected set; }

	/// <summary>
	/// Returns the list of child instances for this dfMarkupBox instance
	/// </summary>
	public List<dfMarkupBox> Children { get { return this.children; } }

	/// <summary> The position of the box </summary>
	public Vector2 Position = Vector2.zero;

	/// <summary> The size in pixels of the box </summary>
	public Vector2 Size = Vector2.zero;

	/// <summary> Specifies the box's behavior in the formatting/layout model </summary>
	public dfMarkupDisplayType Display = dfMarkupDisplayType.inline;

	/// <summary> The minimum amount of space that should be present between this box and any other boxes </summary>
	public dfMarkupBorders Margins = new dfMarkupBorders( 0, 0, 0, 0 );

	/// <summary>
	/// Gets or sets the width (in pixels) of this box
	/// </summary>
	public int Width 
	{ 
		get { return (int)Size.x; }
		set { Size = new Vector2( value, Size.y ); }
	}

	/// <summary>
	/// Gets or sets the height (in pixels) of this box
	/// </summary>
	public int Height
	{
		get { return (int)Size.y; }
		set { Size = new Vector2( Size.x, value ); }
	}

	/// <summary> 
	/// The minimum amount of space that should be present between this box's outside
	/// dimensions and the outside edge of any contained boxes
	/// </summary>
	public dfMarkupBorders Padding = new dfMarkupBorders( 0, 0, 0, 0 );

	public dfMarkupStyle Style;

	public bool IsNewline = false;

	public int Baseline = 0;

	#endregion

	#region Private variables

	private List<dfMarkupBox> children = new List<dfMarkupBox>();

	private dfMarkupBox currentLine = null;
	private int currentLinePos = 0;

	#endregion

	#region Constructor

	private dfMarkupBox()
	{
		// Disallow parameterless constructor
		throw new NotImplementedException();
	}

	public dfMarkupBox( dfMarkupElement element, dfMarkupDisplayType display, dfMarkupStyle style )
	{

		this.Element = element;

		this.Display = display;
		this.Style = style;

		this.Baseline = style.FontSize;

	}

	#endregion

	#region Public methods

	internal dfMarkupBox HitTest( Vector2 point )
	{

		var min = this.GetOffset();
		var max = min + this.Size;

		if( point.x < min.x || point.x > max.x || point.y < min.y || point.y > max.y )
			return null;

		for( int i = 0; i < children.Count; i++ )
		{
			var test = children[ i ].HitTest( point );
			if( test != null )
				return test;
		}

		return this;
		
	}
	
	internal dfRenderData Render()
	{

		try
		{

			//@Profiler.BeginSample( "Render markup box: " + this.GetType().Name );

			endCurrentLine();

			return OnRebuildRenderData();

		}
		finally
		{
			//@Profiler.EndSample();
		}

	}

	public virtual Vector2 GetOffset()
	{

		var pos = Vector2.zero;

		var loop = this;
		while( loop != null )
		{
			pos += loop.Position;
			loop = loop.Parent;
		}

		return pos;

	}

	internal void AddLineBreak()
	{

		if( currentLine != null )
		{
			currentLine.IsNewline = true;
		}

		var lineOffsetTop = getVerticalPosition( 0 );
		endCurrentLine();

		var block = GetContainingBlock();

		currentLine = new dfMarkupBox( this.Element, dfMarkupDisplayType.block, this.Style )
		{
			Size = new Vector2( block.Size.x, Style.FontSize ),
			Position = new Vector2( 0, lineOffsetTop ),
			Parent = this
		};

		children.Add( currentLine );

	}

	public virtual void AddChild( dfMarkupBox box )
	{

		var display = box.Display;
		var needsBlockLayout =
			display == dfMarkupDisplayType.block ||
			display == dfMarkupDisplayType.table ||
			display == dfMarkupDisplayType.listItem ||
			display == dfMarkupDisplayType.tableRow;

		if( needsBlockLayout )
			addBlock( box );
		else
			addInline( box );

	}

	public virtual void Release()
	{

		for( int i = 0; i < children.Count; i++ )
		{
			children[ i ].Release();
		}

		children.Clear();

		Element = null;
		Parent = null;
		Margins = new dfMarkupBorders();

	}

	#endregion

	#region Protected virtual methods 

	protected virtual dfRenderData OnRebuildRenderData()
	{

		return null;

		// Uncomment the following if you want to make sure that the box dimensions
		// are correct - You can view the box mesh by checking "Show Mesh" on the 
		// dfGUIManager instance
		
		//var renderData = dfRenderData.Obtain();
		//renderDebugBox( renderData );
		//return renderData;

	}

	protected void renderDebugBox( dfRenderData renderData )
	{

		var v1 = Vector3.zero;
		var v2 = v1 + Vector3.right * this.Size.x;
		var v3 = v2 + Vector3.down * this.Size.y;
		var v4 = v1 + Vector3.down * this.Size.y;

		renderData.Vertices.Add( v1 );
		renderData.Vertices.Add( v2 );
		renderData.Vertices.Add( v3 );
		renderData.Vertices.Add( v4 );

		renderData.Triangles.AddRange( new int[] { 0, 1, 3, 3, 1, 2 } );

		renderData.UV.Add( Vector2.zero );
		renderData.UV.Add( Vector2.zero );
		renderData.UV.Add( Vector2.zero );
		renderData.UV.Add( Vector2.zero );

		var color = Style.BackgroundColor;
		renderData.Colors.Add( color );
		renderData.Colors.Add( color );
		renderData.Colors.Add( color );
		renderData.Colors.Add( color );

	}

	#endregion

	#region Private utility methods

	public void FitToContents()
	{
		FitToContents( false );
	}

	public void FitToContents( bool recursive )
	{

		if( this.children.Count == 0 )
		{
			this.Size = new Vector2( Size.x, 0 );
			return;
		}

		endCurrentLine();

		var max = Vector2.zero;
		for( int i = 0; i < children.Count; i++ )
		{
			var child = children[ i ];
			max = Vector2.Max( max, child.Position + child.Size );
		}

		this.Size = max;

	}

	/// <summary>
	/// Gets the containing block-box of this box. (The nearest ancestor with display=block)
	/// </summary>
	private dfMarkupBox GetContainingBlock()
	{

		var loop = this;

		while( loop != null )
		{

			var display = loop.Display;
			var isBlock =
				display == dfMarkupDisplayType.block ||
				display == dfMarkupDisplayType.inlineBlock ||
				display == dfMarkupDisplayType.listItem ||
				display == dfMarkupDisplayType.table ||
				display == dfMarkupDisplayType.tableRow ||
				display == dfMarkupDisplayType.tableCell;

			if( isBlock )
				return loop;

			loop = loop.Parent;

		}

		return null;

	}

	private void addInline( dfMarkupBox box )
	{

		var margin = box.Margins;

		bool needsWordwrap =
			!Style.Preformatted &&
			( currentLine != null && currentLinePos + box.Size.x > currentLine.Size.x );

		if( currentLine == null || needsWordwrap )
		{

			endCurrentLine();

			var lineOffsetTop = getVerticalPosition( margin.top );

			var block = GetContainingBlock();
			if( block == null )
			{
				Debug.LogError( "Containing block not found" );
				return;
			}

			var font = Style.Font ?? Style.Host.Font;
			var multiplier = (float)font.FontSize / (float)font.FontSize;
			var lineBaseline = font.Baseline * multiplier;

			currentLine = new dfMarkupBox( this.Element, dfMarkupDisplayType.block, this.Style )
			{
				Size = new Vector2( block.Size.x, Style.LineHeight ),
				Position = new Vector2( 0, lineOffsetTop ),
				Parent = this,
				Baseline = (int)lineBaseline
			};

			children.Add( currentLine );

		}

		// Eliminate whitespace at beginning of the line, if whitespace is not preserved
		if( currentLinePos == 0 && !box.Style.PreserveWhitespace && box is dfMarkupBoxText )
		{
			var text = box as dfMarkupBoxText;
			if( text.IsWhitespace )
			{
				return;
			}
		}

		var boxPosition = new Vector2( currentLinePos + margin.left, margin.top );
		box.Position = boxPosition;

		box.Parent = currentLine;
		currentLine.children.Add( box );

		currentLinePos = (int)( boxPosition.x + box.Size.x + box.Margins.right );

		var lineWidth = Mathf.Max( currentLine.Size.x, boxPosition.x + box.Size.x );
		var lineHeight = Mathf.Max( currentLine.Size.y, boxPosition.y + box.Size.y );

		currentLine.Size = new Vector2( lineWidth, lineHeight );

	}

	private int getVerticalPosition( int topMargin )
	{

		if( children.Count == 0 )
			return topMargin;

		var lowest = 0;
		var lowestIndex = 0;

		for( int i = 0; i < children.Count; i++ )
		{
			var child = children[ i ];
			var bottom = child.Position.y + child.Size.y + child.Margins.bottom;
			if( bottom > lowest )
			{
				lowest = (int)bottom;
				lowestIndex = i;
			}
		}

		var lowestBox = children[ lowestIndex ];
		var marginCollapse = Mathf.Max( lowestBox.Margins.bottom, topMargin );

		return (int)( lowestBox.Position.y + lowestBox.Size.y + marginCollapse );

	}

	private void addBlock( dfMarkupBox box )
	{

		if( currentLine != null )
		{
			currentLine.IsNewline = true;
			endCurrentLine( true );
		}

		var container = GetContainingBlock();

		// If a block box does not have a size specified, then by default it 
		// is the width of the containing block
		if( box.Size.sqrMagnitude <= float.Epsilon )
		{
			box.Size = new Vector2( container.Size.x - box.Margins.horizontal, Style.FontSize );
		}

		var boxTop = getVerticalPosition( box.Margins.top );
		box.Position = new Vector2( box.Margins.left, boxTop );

		this.Size = new Vector2( this.Size.x, Mathf.Max( this.Size.y, box.Position.y + box.Size.y ) );

		box.Parent = this;
		children.Add( box );

	}

	private void endCurrentLine()
	{
		endCurrentLine( false );
	}

	private void endCurrentLine( bool removeEmpty )
	{

		if( currentLine == null )
			return;

		if( currentLinePos == 0 )
		{
			if( removeEmpty )
			{
				children.Remove( currentLine );
			}
		}
		else
		{
			currentLine.doHorizontalAlignment();
			currentLine.doVerticalAlignment();
		}

		currentLine = null;
		currentLinePos = 0;

	}

	private void doVerticalAlignment()
	{

		if( children.Count == 0 )
			return;

		var lowestBottom = float.MinValue;
		var highestTop = float.MaxValue;
		var lowestBaseline = float.MinValue;

		#region Align control baselines first 

		this.Baseline = (int)( this.Size.y * 0.95f );

		for( int i = 0; i < children.Count; i++ )
		{
			var child = children[ i ];
			lowestBaseline = Mathf.Max( lowestBaseline, child.Position.y + child.Baseline );
		}

		for( int i = 0; i < children.Count; i++ )
		{

			var child = children[ i ];
			var align = child.Style.VerticalAlign;

			var pos = child.Position;
			if( align == dfMarkupVerticalAlign.Baseline )
			{
				pos.y = lowestBaseline - child.Baseline;
			}

			child.Position = pos;

		}

		#endregion

		for( int i = 0; i < children.Count; i++ )
		{

			var child = children[ i ];

			var pos = child.Position;
			var size = child.Size;

			highestTop = Mathf.Min( highestTop, pos.y );
			lowestBottom = Mathf.Max( lowestBottom, pos.y + size.y );

		}

		for( int i = 0; i < children.Count; i++ )
		{

			var child = children[ i ];
			var align = child.Style.VerticalAlign;

			var pos = child.Position;
			var size = child.Size;

			if( align == dfMarkupVerticalAlign.Top )
			{
				pos.y = highestTop;
			}
			else if( align == dfMarkupVerticalAlign.Bottom )
			{
				pos.y = lowestBottom - size.y;
			}
			else if( align == dfMarkupVerticalAlign.Middle )
			{
				pos.y = ( this.Size.y - size.y ) * 0.5f;
			}

			child.Position = pos;

		}

		#region Ensure alignment didn't make elements rise above box

		var minTop = int.MaxValue;
		for( int i = 0; i < children.Count; i++ )
		{
			minTop = Mathf.Min( minTop, (int)children[ i ].Position.y );
		}

		for( int i = 0; i < children.Count; i++ )
		{
			var pos = children[ i ].Position;
			pos.y -= minTop;
			children[ i ].Position = pos;
		}

		#endregion

	}

	private void doHorizontalAlignment()
	{

		if( this.Style.Align == dfMarkupTextAlign.Left || children.Count == 0 )
			return;

		var lastIndex = children.Count - 1;
		while( lastIndex > 0 )
		{

			var last = children[ lastIndex ] as dfMarkupBoxText;
			if( last == null || !last.IsWhitespace )
				break;

			lastIndex -= 1;

		}

		if( this.Style.Align == dfMarkupTextAlign.Center )
		{

			var childWidths = 0f;
			for( int i = 0; i <= lastIndex; i++ )
			{
				childWidths += children[ i ].Size.x;
			}

			var centerOffset = ( this.Size.x - Padding.horizontal - childWidths ) * 0.5f;
			for( int i = 0; i <= lastIndex; i++ )
			{
				var pos = children[ i ].Position;
				pos.x += centerOffset;
				children[ i ].Position = pos;
			}

		}
		else if( this.Style.Align == dfMarkupTextAlign.Right )
		{

			var right = this.Size.x - Padding.horizontal;
			for( int i = lastIndex; i >= 0; i-- )
			{

				var pos = children[ i ].Position;
				pos.x = right - children[ i ].Size.x;
				children[ i ].Position = pos;

				right -= children[ i ].Size.x;

			}

		}
		else if( this.Style.Align == dfMarkupTextAlign.Justify )
		{

			if( children.Count <= 1 )
				return;

			// Do not justify lines that end with newline
			if( this.IsNewline || children[ children.Count - 1 ].IsNewline )
			{
				return;
			}

			var childWidths = 0f;
			for( int i = 0; i <= lastIndex; i++ )
			{
				var child = children[ i ];
				childWidths = Mathf.Max( childWidths, child.Position.x + child.Size.x );
			}

			var spacing = ( this.Size.x - Padding.horizontal - childWidths ) / (float)children.Count;
			for( int i = 1; i <= lastIndex; i++ )
			{
				children[ i ].Position += new Vector2( i * spacing, 0 );
			}

			// Always ensure that the rightmost box is flush with right side,
			// even if spacing is rounded
			var rightmost = children[ lastIndex ];
			var rightPos = rightmost.Position;
			rightPos.x = this.Size.x - Padding.horizontal - rightmost.Size.x;
			rightmost.Position = rightPos;

		}
		else
		{
			throw new NotImplementedException( "text-align: " + Style.Align + " is not implemented" );
		}

	}

	#endregion


}

public class dfMarkupBoxSprite : dfMarkupBox
{

	#region Static variables and constants

	private static int[] TRIANGLE_INDICES = new int[] { 0, 1, 2, 0, 2, 3 };

	#endregion

	#region Public fields

	public dfAtlas Atlas { get; set; }
	public string Source { get; set; }

	#endregion

	#region Private variables

	private dfRenderData renderData = new dfRenderData();

	#endregion

	#region Constructor

	public dfMarkupBoxSprite( dfMarkupElement element, dfMarkupDisplayType display, dfMarkupStyle style )
		: base( element, display, style )
	{
	}

	#endregion

	#region Public methods

	internal void LoadImage( dfAtlas atlas, string source )
	{

		var spriteInfo = atlas[ source ];
		if( spriteInfo == null )
			throw new InvalidOperationException( "Sprite does not exist in atlas: " + source );

		this.Atlas = atlas;
		this.Source = source;

		this.Size = spriteInfo.sizeInPixels;
		this.Baseline = (int)Size.y;

	}

	#endregion

	#region dfMarkupBox overrides

	protected override dfRenderData OnRebuildRenderData()
	{

		this.renderData.Clear();

		if( Atlas != null && Atlas[ Source ] != null )
		{

			var options = new dfSprite.RenderOptions()
			{
				atlas = this.Atlas,
				spriteInfo = Atlas[ Source ],
				pixelsToUnits = 1,
				size = this.Size,
				color = Style.Color,
				baseIndex = 0,
				fillAmount = 1f,
				flip = dfSpriteFlip.None
			};

			dfSlicedSprite.renderSprite( renderData, options );

			renderData.Material = Atlas.Material;
			renderData.Transform = Matrix4x4.identity;

		}

		return renderData;

	}

	#endregion

	#region Private utility functions

	private static void addTriangleIndices( dfList<Vector3> verts, dfList<int> triangles )
	{

		var vcount = verts.Count;
		var indices = TRIANGLE_INDICES;

		for( int ii = 0; ii < indices.Length; ii++ )
		{
			triangles.Add( vcount + indices[ ii ] );
		}

	}

	#endregion

}

public class dfMarkupBoxTexture : dfMarkupBox
{

	#region Static variables and constants

	private static int[] TRIANGLE_INDICES = new int[] { 0, 1, 2, 0, 2, 3 };
	//private static int[] TRIANGLE_INDICES = new int[] { 0, 1, 3, 3, 1, 2 };

	#endregion

	#region Public fields

	public Texture Texture { get; set; }

	#endregion

	#region Private variables

	private dfRenderData renderData = new dfRenderData();
	private Material material = null;

	#endregion

	#region Constructor

	public dfMarkupBoxTexture( dfMarkupElement element, dfMarkupDisplayType display, dfMarkupStyle style )
		: base( element, display, style )
	{
	}

	#endregion

	#region Public methods

	internal void LoadTexture( Texture texture )
	{

		if( texture == null )
			throw new InvalidOperationException();

		this.Texture = texture;

		this.Size = new Vector2( texture.width, texture.height );
		this.Baseline = (int)Size.y;

	}

	#endregion

	#region dfMarkupBox overrides

	protected override dfRenderData OnRebuildRenderData()
	{

		this.renderData.Clear();

		ensureMaterial();
		renderData.Material = this.material;
		renderData.Material.mainTexture = this.Texture;

		var v1 = Vector3.zero;
		var v2 = v1 + Vector3.right * this.Size.x;
		var v3 = v2 + Vector3.down * this.Size.y;
		var v4 = v1 + Vector3.down * this.Size.y;

		renderData.Vertices.Add( v1 );
		renderData.Vertices.Add( v2 );
		renderData.Vertices.Add( v3 );
		renderData.Vertices.Add( v4 );

		renderData.Triangles.AddRange( TRIANGLE_INDICES );

		renderData.UV.Add( new Vector2( 0, 1 ) );
		renderData.UV.Add( new Vector2( 1, 1 ) );
		renderData.UV.Add( new Vector2( 1, 0 ) );
		renderData.UV.Add( new Vector2( 0, 0 ) );

		var color = Style.Color;
		renderData.Colors.Add( color );
		renderData.Colors.Add( color );
		renderData.Colors.Add( color );
		renderData.Colors.Add( color );

		return renderData;

	}

	#endregion

	#region Private utility functions

	/// <summary>
	/// Attempts to ensure that a Material is always available
	/// </summary>
	private void ensureMaterial()
	{

		if( material != null || Texture == null )
			return;

		var shader = Shader.Find( "Daikon Forge/Default UI Shader" );
		if( shader == null )
		{
			Debug.LogError( "Failed to find default shader" );
			return;
		}

		material = new Material( shader )
		{
			name = "Default Texture Shader",
			hideFlags = HideFlags.DontSave,
			mainTexture = Texture
		};

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

	#endregion

}

public class dfMarkupBoxText : dfMarkupBox
{

	#region Static variables and constants

	private static int[] TRIANGLE_INDICES = new int[] { 0, 1, 2, 0, 2, 3 };

	private static Queue<dfMarkupBoxText> objectPool = new Queue<dfMarkupBoxText>();
	private static Regex whitespacePattern = new Regex( "\\s+" );

	#endregion

	#region Public fields

	public string Text { get; private set; }

	public bool IsWhitespace
	{
		get { return isWhitespace; }
	}

	#endregion

	#region Private variables

	private dfRenderData renderData = new dfRenderData();
	private bool isWhitespace = false;

	#endregion

	#region Constructor

	public dfMarkupBoxText( dfMarkupElement element, dfMarkupDisplayType display, dfMarkupStyle style )
		: base( element, display, style )
	{
	}

	#endregion

	#region Public methods 

	public static dfMarkupBoxText Obtain( dfMarkupElement element, dfMarkupDisplayType display, dfMarkupStyle style )
	{

		if( objectPool.Count > 0 )
		{

			var instance = objectPool.Dequeue();

			instance.Element = element;
			instance.Display = display;
			instance.Style = style;

			instance.Position = Vector2.zero;
			instance.Size = Vector2.zero;

			instance.Baseline = (int)( style.FontSize * 1.1f );
			instance.Margins = new dfMarkupBorders();
			instance.Padding = new dfMarkupBorders();

			return instance;

		}

		return new dfMarkupBoxText( element, display, style );

	}

	public override void Release()
	{
		
		base.Release();

		Text = "";
		renderData.Clear();
		objectPool.Enqueue( this );

	}

	/// <summary>
	/// Sets the Text property of this element and measures the size
	/// of the text as it would be rendered with the current style.
	/// Designed for internal use by elements that need to create 
	/// text boxes dynamically, such as list items.
	/// </summary>
	/// <param name="text"></param>
	internal void SetText( string text )
	{

		this.Text = text;

		if( Style.Font == null )
			return;

		isWhitespace = whitespacePattern.IsMatch( this.Text );
		var effectiveText = Style.PreserveWhitespace || !isWhitespace ? this.Text : " ";

		var currentFontSize = Style.FontSize;
		var size = new Vector2( 0, Style.LineHeight );

		Style.Font.RequestCharacters( effectiveText, Style.FontSize, Style.FontStyle );

		var glyph = new UnityEngine.CharacterInfo();

		for( int i = 0; i < effectiveText.Length; i++ )
		{

			if( !Style.Font.BaseFont.GetCharacterInfo( effectiveText[ i ], out glyph, currentFontSize, Style.FontStyle ) )
				continue;

			var width = glyph.vert.x + glyph.vert.width;

			if( effectiveText[ i ] == ' ' )
			{
				width = Mathf.Max( width, currentFontSize * 0.33f );
			}
			else if( effectiveText[ i ] == '\t' )
			{
				width += currentFontSize * 3;
			}

			size.x += width;

		}

		this.Size = size;

		var font = Style.Font;
		var multiplier = (float)currentFontSize / (float)font.FontSize;
		this.Baseline = Mathf.CeilToInt( font.Baseline * multiplier );

	}

	#endregion

	#region dfMarkupBox overrides

	protected override dfRenderData OnRebuildRenderData()
	{

		this.renderData.Clear();

		if( Style.Font == null )
			return null;

		if( Style.TextDecoration == dfMarkupTextDecoration.Underline )
		{
			renderUnderline();
		}

		renderText( this.Text );

		return renderData;

	}

	#endregion

	#region Private utility functions

	private void renderUnderline()
	{

		// Underline doesn't work correctly when text alignment is Justified,
		// need to re-implement this functionality

#if FALSE
		var font = Style.Font;
		var fontSize = Style.FontSize;
		var fontStyle = Style.FontStyle;

		var verts = renderData.Vertices;
		var triangles = renderData.Triangles;
		var uvs = renderData.UV;
		var colors = renderData.Colors;

		var offset = Vector3.zero;
		var multiplier = (float)fontSize / (float)font.FontSize;
		var descent = font.Descent * multiplier;

		// Ensure that the baseFont's texture contains all characters before 
		// rendering any text.
		var glyph = font.RequestCharacters( "_", fontSize, fontStyle )[0];

		addTriangleIndices( verts, triangles );

		var yadjust = ( font.FontSize + glyph.vert.y ) - fontSize + descent;
		var quadLeft = 0;
		var quadTop = yadjust;
		var quadRight = Size.x;
		var quadBottom = ( quadTop + glyph.vert.height );

		var v0 = offset + new Vector3( quadLeft, quadTop );
		var v1 = offset + new Vector3( quadRight, quadTop );
		var v2 = offset + new Vector3( quadRight, quadBottom );
		var v3 = offset + new Vector3( quadLeft, quadBottom );

		verts.Add( v0 );
		verts.Add( v1 );
		verts.Add( v2 );
		verts.Add( v3 );

		var glyphColor = Style.Color;
		colors.Add( glyphColor );
		colors.Add( glyphColor );
		colors.Add( glyphColor );
		colors.Add( glyphColor );

		var region = glyph.uv;
		var uvLeft = region.x;
		var uvTop = region.y + region.height;
		var uvRight = uvLeft + region.width;
		var uvBottom = region.y;

		// Calculate width of texel. This is used when adding the UV 
		// coordinates for the quad so that resizing the glyph does
		// not result in stretched and faded edges.
		var uvx = 1f / font.Material.mainTexture.width;

		if( glyph.flipped )
		{
			uvs.Add( new Vector2( uvRight, uvBottom - uvx ) );
			uvs.Add( new Vector2( uvRight, uvTop + uvx ) );
			uvs.Add( new Vector2( uvLeft, uvTop + uvx ) );
			uvs.Add( new Vector2( uvLeft, uvBottom - uvx ) );
		}
		else
		{
			uvs.Add( new Vector2( uvLeft + uvx, uvTop ) );
			uvs.Add( new Vector2( uvRight - uvx, uvTop ) );
			uvs.Add( new Vector2( uvRight - uvx, uvBottom ) );
			uvs.Add( new Vector2( uvLeft + uvx, uvBottom ) );
		}
#endif

	}

	private void renderText( string text )
	{

		var font = Style.Font;
		var fontSize = Style.FontSize;
		var fontStyle = Style.FontStyle;
		var glyph = new UnityEngine.CharacterInfo();

		var verts = renderData.Vertices;
		var triangles = renderData.Triangles;
		var uvs = renderData.UV;
		var colors = renderData.Colors;

		var multiplier = (float)fontSize / (float)font.FontSize;
		var descent = font.Descent * multiplier;

		var x = 0f;

		// Ensure that the baseFont's texture contains all characters before 
		// rendering any text.
		font.RequestCharacters( text, fontSize, fontStyle );

		// Set the render material in the output buffer *after* requesting
		// glyph data, which may result in CharacterInfo in the dfDynamicFont's 
		// texture atlas being rebuilt.
		renderData.Material = font.Material;

		for( int i = 0; i < text.Length; i++ )
		{

			if( !font.BaseFont.GetCharacterInfo( text[ i ], out glyph, fontSize, fontStyle ) )
				continue;

			addTriangleIndices( verts, triangles );

			var yadjust = ( font.FontSize + glyph.vert.y ) - fontSize + descent;
			var quadLeft = ( x + glyph.vert.x );
			var quadTop = ( yadjust );
			var quadRight = ( quadLeft + glyph.vert.width );
			var quadBottom = ( quadTop + glyph.vert.height );

			var v0 = new Vector3( quadLeft, quadTop );
			var v1 = new Vector3( quadRight, quadTop );
			var v2 = new Vector3( quadRight, quadBottom );
			var v3 = new Vector3( quadLeft, quadBottom );

			verts.Add( v0 );
			verts.Add( v1 );
			verts.Add( v2 );
			verts.Add( v3 );

			var glyphColor = Style.Color;
			colors.Add( glyphColor );
			colors.Add( glyphColor );
			colors.Add( glyphColor );
			colors.Add( glyphColor );

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

			x += Mathf.CeilToInt( glyph.vert.x + glyph.vert.width );

		}

	}

	private static void addTriangleIndices( dfList<Vector3> verts, dfList<int> triangles )
	{

		var baseIndex = verts.Count;
		var indices = TRIANGLE_INDICES;

		for( int ii = 0; ii < indices.Length; ii++ )
		{
			triangles.Add( baseIndex + indices[ ii ] );
		}

	}

	#endregion

}
