/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Allows the user to use the mouse to move the dfControl that contains 
/// this dfDragHandle instance
/// </summary>
[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Drag Handle" )]
public class dfDragHandle : dfControl
{

	private Vector3 lastPosition;

	#region Overrides

	public override void Start()
	{

		base.Start();

		if( Size.magnitude <= float.Epsilon )
		{

			if( this.Parent != null )
			{
				this.Size = new Vector2( Parent.Width, 30 );
				this.Anchor = dfAnchorStyle.Top | dfAnchorStyle.Right | dfAnchorStyle.Left;
				this.RelativePosition = Vector2.zero;
			}
			else
			{
				this.Size = new Vector2( 200, 25 );
			}

		}

	}

	protected internal override void OnMouseDown( dfMouseEventArgs args )
	{

		GetRootContainer().BringToFront();
		Parent.BringToFront();

		args.Use();

		var plane = new Plane( parent.transform.TransformDirection( Vector3.back ), parent.transform.position );
		var ray = args.Ray;

		var distance = 0f;
		plane.Raycast( args.Ray, out distance );

		lastPosition = ( ray.origin + ray.direction * distance );

		base.OnMouseDown( args );

	}

	protected internal override void OnMouseMove( dfMouseEventArgs args )
	{

		args.Use();

		if( args.Buttons.IsSet( dfMouseButtons.Left ) )
		{

			var ray = args.Ray;
			var distance = 0f;
			var direction = GetCamera().transform.TransformDirection( Vector3.back );
			var plane = new Plane( direction, lastPosition );
			plane.Raycast( ray, out distance );

			var pos = ( ray.origin + ray.direction * distance ).Quantize( parent.PixelsToUnits() );
			var offset = pos - lastPosition;

			var transformPos = ( parent.transform.position + offset ).Quantize( parent.PixelsToUnits() );
			parent.transform.position = transformPos;

			lastPosition = pos;

		}

		base.OnMouseMove( args );

	}

	protected internal override void OnMouseUp( dfMouseEventArgs args )
	{
		base.OnMouseUp( args );
		Parent.MakePixelPerfect();
	}

	#endregion

}
