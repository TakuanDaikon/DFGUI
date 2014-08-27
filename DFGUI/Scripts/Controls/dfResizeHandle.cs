/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Allows the user to use the mouse to resize the dfControl that contains 
/// this dfResizeHandle instance.
/// </summary>
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Resize Handle" )]
public class dfResizeHandle : dfControl
{

	#region Public enums

	/// <summary>
	/// Defines which edges of the parent control are moved
	/// by dragging the <see cref="dfResizeHandle"/>
	/// </summary>
	[Flags]
	public enum ResizeEdge : int
	{
		/// <summary>
		/// The parent control is not resized
		/// </summary>
		None = 0,
		/// <summary>
		/// Dragging the <see cref="dfResizeHandle"/> object horizontally
		/// moves the parent control's Left edge
		/// </summary>
		Left = 1,
		/// <summary>
		/// Dragging the <see cref="dfResizeHandle"/> object horizontally
		/// moves the parent control's Right edge
		/// </summary>
		Right = 2,
		/// <summary>
		/// Dragging the <see cref="dfResizeHandle"/> object horizontally
		/// moves the parent control's Top edge
		/// </summary>
		Top = 4,
		/// <summary>
		/// Dragging the <see cref="dfResizeHandle"/> object horizontally
		/// moves the parent control's Bottom edge
		/// </summary>
		Bottom = 8
	}

	#endregion

	#region Serialized protected fields

	[SerializeField]
	protected dfAtlas atlas;

	[SerializeField]
	protected string backgroundSprite = "";

	[SerializeField]
	protected ResizeEdge edges = ResizeEdge.Right | ResizeEdge.Bottom;

	#endregion

	#region Private instance variables

	private Vector3 mouseAnchorPos;
	private Vector3 startPosition;
	private Vector2 startSize;
	private Vector2 minEdgePos;
	private Vector2 maxEdgePos;

	#endregion

	#region Public properties

	/// <summary>
	/// The <see cref="dfAtlas">Texture Atlas</see> containing the images used by this control
	/// </summary>
	public dfAtlas Atlas
	{
		get
		{
			if( this.atlas == null )
			{
				var view = GetManager();
				if( view != null )
				{
					return this.atlas = view.DefaultAtlas;
				}
			}
			return this.atlas;
		}
		set
		{
			if( !dfAtlas.Equals( value, atlas ) )
			{
				this.atlas = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// The name of the image in the <see cref="Atlas"/> that will be used to 
	/// render the background of this control
	/// </summary>
	public string BackgroundSprite
	{
		get { return backgroundSprite; }
		set
		{
			if( value != backgroundSprite )
			{
				backgroundSprite = value;
				Invalidate();
			}
		}
	}

	/// <summary>
	/// Defines which edges of the parent control are moved
	/// by dragging the <see cref="dfResizeHandle"/>
	/// </summary>
	public ResizeEdge Edges
	{
		get { return this.edges; }
		set { this.edges = value; }
	}

	#endregion

	#region dfControl overrides

	public override void Start()
	{

		base.Start();

		if( Size.magnitude <= float.Epsilon )
		{

			Size = new Vector2( 25, 25 );

			if( this.Parent != null )
			{
				this.RelativePosition = Parent.Size - this.Size;
				this.Anchor = dfAnchorStyle.Right | dfAnchorStyle.Bottom;
			}

		}

	}

	protected override void OnRebuildRenderData()
	{

		if( Atlas == null || string.IsNullOrEmpty( backgroundSprite ) )
			return;

		var spriteInfo = Atlas[ backgroundSprite ];
		if( spriteInfo == null )
		{
			return;
		}

		renderData.Material = Atlas.Material;

		var color = ApplyOpacity( IsEnabled ? this.color : this.disabledColor );
		var options = new dfSprite.RenderOptions()
		{
			atlas = atlas,
			color = color,
			fillAmount = 1,
			flip = dfSpriteFlip.None,
			offset = pivot.TransformToUpperLeft( Size ),
			pixelsToUnits = PixelsToUnits(),
			size = Size,
			spriteInfo = spriteInfo
		};

		if( spriteInfo.border.horizontal == 0 && spriteInfo.border.vertical == 0 )
			dfSprite.renderSprite( renderData, options );
		else
			dfSlicedSprite.renderSprite( renderData, options );

	}

	protected internal override void OnMouseDown( dfMouseEventArgs args )
	{

		args.Use();

		var plane = new Plane( parent.transform.TransformDirection( Vector3.back ), parent.transform.position );
		var ray = args.Ray;

		var distance = 0f;
		plane.Raycast( args.Ray, out distance );

		mouseAnchorPos = ( ray.origin + ray.direction * distance );

		startSize = parent.Size;
		startPosition = parent.RelativePosition;

		#region Calculate the min and max edge positions

		minEdgePos = startPosition;
		maxEdgePos = (Vector2)startPosition + startSize;

		var minSize = parent.CalculateMinimumSize();

		var maxSize = parent.MaximumSize;
		if( maxSize.magnitude <= float.Epsilon )
		{
			maxSize = Vector2.one * 2048;
		}

		if( ( Edges & ResizeEdge.Left ) == ResizeEdge.Left )
		{
			minEdgePos.x = maxEdgePos.x - maxSize.x;
			maxEdgePos.x = maxEdgePos.x - minSize.x;
		}
		else if( ( Edges & ResizeEdge.Right ) == ResizeEdge.Right )
		{
			minEdgePos.x = startPosition.x + minSize.x;
			maxEdgePos.x = startPosition.x + maxSize.x;
		}

		if( ( Edges & ResizeEdge.Top ) == ResizeEdge.Top )
		{
			minEdgePos.y = maxEdgePos.y - maxSize.y;
			maxEdgePos.y = maxEdgePos.y - minSize.y;
		}
		else if( ( Edges & ResizeEdge.Bottom ) == ResizeEdge.Bottom )
		{
			minEdgePos.y = startPosition.y + minSize.y;
			maxEdgePos.y = startPosition.y + maxSize.y;
		}

		#endregion

		base.OnMouseDown( args );

	}

	protected internal override void OnMouseMove( dfMouseEventArgs args )
	{

		if( !args.Buttons.IsSet( dfMouseButtons.Left ) || Edges == ResizeEdge.None )
			return;

		args.Use();

		var ray = args.Ray;
		var distance = 0f;
		var direction = this.GetCamera().transform.TransformDirection( Vector3.back );
		var plane = new Plane( direction, mouseAnchorPos );
		plane.Raycast( ray, out distance );

		var p2u = this.PixelsToUnits();

		var pos = ( ray.origin + ray.direction * distance );
		var offset = ( ( pos - mouseAnchorPos ) / p2u );

		// Flip Y axis for screen coordinates
		offset.y *= -1;

		var left = startPosition.x;
		var top = startPosition.y;
		var right = left + startSize.x;
		var bottom = top + startSize.y;

		if( ( Edges & ResizeEdge.Left ) == ResizeEdge.Left )
		{
			left = Mathf.Min( maxEdgePos.x, Mathf.Max( minEdgePos.x, left + offset.x ) );
		}
		else if( ( Edges & ResizeEdge.Right ) == ResizeEdge.Right )
		{
			right = Mathf.Min( maxEdgePos.x, Mathf.Max( minEdgePos.x, right + offset.x ) );
		}

		if( ( Edges & ResizeEdge.Top ) == ResizeEdge.Top )
		{
			top = Mathf.Min( maxEdgePos.y, Mathf.Max( minEdgePos.y, top + offset.y ) );
		}
		else if( ( Edges & ResizeEdge.Bottom ) == ResizeEdge.Bottom )
		{
			bottom = Mathf.Min( maxEdgePos.y, Mathf.Max( minEdgePos.y, bottom + offset.y ) );
		}

		parent.Size = new Vector2( right - left, bottom - top );
		parent.RelativePosition = new Vector3( left, top, 0 );

		if( parent.GetManager().PixelPerfectMode )
		{
			parent.MakePixelPerfect();
		}

	}

	protected internal override void OnMouseUp( dfMouseEventArgs args )
	{

		//Parent.PerformLayout();
		Parent.MakePixelPerfect();

		args.Use();

		base.OnMouseUp( args );

	}

	#endregion

}
