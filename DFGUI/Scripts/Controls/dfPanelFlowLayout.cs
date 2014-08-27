using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Panel Addon/Flow Layout" )]
public class dfPanelFlowLayout : MonoBehaviour
{

	#region Serialized properties

	[SerializeField]
	protected RectOffset borderPadding = new RectOffset();

	[SerializeField]
	protected Vector2 itemSpacing = new Vector2();

	[SerializeField]
	protected dfControlOrientation flowDirection = dfControlOrientation.Horizontal;

	[SerializeField]
	protected bool hideClippedControls = false;

	[SerializeField]
	protected int maxLayoutSize = 0;

	[SerializeField]
	protected List<dfControl> excludedControls = new List<dfControl>();

	#endregion

	#region Public properties

	/// <summary>
	/// Gets or sets the direction in which child controls will be arranged
	/// </summary>
	public dfControlOrientation Direction
	{
		get { return this.flowDirection; }
		set
		{
			if( value != this.flowDirection )
			{
				this.flowDirection = value;
				PerformLayout();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding that will be applied to each control
	/// when arranging child controls
	/// </summary>
	public Vector2 ItemSpacing
	{
		get
		{
			return this.itemSpacing;
		}
		set
		{
			value = Vector2.Max( value, Vector2.zero );
			if( !Vector2.Equals( value, this.itemSpacing ) )
			{
				this.itemSpacing = value;
				PerformLayout();
			}
		}
	}

	/// <summary>
	/// Gets or sets the amount of padding that will be applied to the
	/// borders of the Panel
	/// </summary>
	public RectOffset BorderPadding
	{
		get
		{
			if( this.borderPadding == null ) this.borderPadding = new RectOffset();
			return this.borderPadding;
		}
		set
		{
			value = value.ConstrainPadding();
			if( !RectOffset.Equals( value, this.borderPadding ) )
			{
				this.borderPadding = value;
				PerformLayout();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether controls which would be clipped by the 
	/// panel's border should be hidden.
	/// </summary>
	public bool HideClippedControls
	{
		get { return this.hideClippedControls; }
		set
		{
			if( value != this.hideClippedControls )
			{
				this.hideClippedControls = value;
				PerformLayout();
			}
		}
	}

	/// <summary>
	/// Gets or sets the maximum size a row or column can grow to before 
	/// the layout is wrapped to the next row or column
	/// </summary>
	public int MaxLayoutSize
	{
		get { return maxLayoutSize; }
		set
		{
			if( value != maxLayoutSize )
			{
				maxLayoutSize = value;
				PerformLayout();
			}
		}
	}

	/// <summary>
	/// Contains a list of controls that will be excluded from the flow layout.
	/// </summary>
	public List<dfControl> ExcludedControls
	{
		get { return this.excludedControls; }
	}

	#endregion

	#region Private runtime variables

	private dfPanel panel;

	#endregion

	#region Unity events

	public void OnEnable()
	{

		this.panel = GetComponent<dfPanel>();
		if( panel == null )
		{
			Debug.LogError( "The " + this.GetType().Name + " component requires a dfPanel component.", gameObject );
			this.enabled = false;
			return;
		}

		panel.SizeChanged += OnSizeChanged;

	}

	public void OnDisable()
	{

		if( panel != null )
		{
			panel.SizeChanged -= OnSizeChanged;
			panel = null;
		}

	}

	#endregion

	#region dfPanel events

	public void OnControlAdded( dfControl container, dfControl child )
	{
		child.ZOrderChanged += child_ZOrderChanged;
		child.SizeChanged += child_SizeChanged;
		PerformLayout();
	}

	public void OnControlRemoved( dfControl container, dfControl child )
	{
		child.ZOrderChanged -= child_ZOrderChanged;
		child.SizeChanged -= child_SizeChanged;
		PerformLayout();
	}

	public void OnSizeChanged( dfControl control, Vector2 value )
	{
		PerformLayout();
	}

	void child_SizeChanged( dfControl control, Vector2 value )
	{
		PerformLayout();
	}

	void child_ZOrderChanged( dfControl control, int value )
	{
		PerformLayout();
	}

	#endregion

	#region Public methods

	public void PerformLayout()
	{

		if( panel == null )
		{
			this.panel = GetComponent<dfPanel>();
		}

		var position = new Vector3( borderPadding.left, borderPadding.top );


		var firstInLine = true;

		var maxX = ( flowDirection == dfControlOrientation.Horizontal && maxLayoutSize > 0 ) ? maxLayoutSize : panel.Width - borderPadding.right;
		var maxY = ( flowDirection == dfControlOrientation.Vertical && maxLayoutSize > 0 ) ? maxLayoutSize : panel.Height - borderPadding.bottom;

		var maxSize = 0;

		var controls = panel.Controls;
		for( int i = 0; i < controls.Count; i++, firstInLine = false )
		{

			var control = controls[ i ];
			if( !control.enabled || !control.gameObject.activeSelf || excludedControls.Contains( control ) )
				continue;

			if( !firstInLine )
			{
				if( flowDirection == dfControlOrientation.Horizontal )
					position.x += itemSpacing.x;
				else
					position.y += itemSpacing.y;
			}

			if( flowDirection == dfControlOrientation.Horizontal )
			{
				if( !firstInLine && position.x + control.Width > maxX + float.Epsilon )
				{

					position.x = borderPadding.left;
					position.y += maxSize;

					maxSize = 0;
					firstInLine = true;

				}
			}
			else
			{
				if( !firstInLine && position.y + control.Height > maxY + float.Epsilon )
				{

					position.y = borderPadding.top;
					position.x += maxSize;

					maxSize = 0;
					firstInLine = true;

				}
			}

			control.RelativePosition = position;

			if( flowDirection == dfControlOrientation.Horizontal )
			{
				position.x += control.Width;
				maxSize = Mathf.Max( Mathf.CeilToInt( control.Height + itemSpacing.y ), maxSize );
			}
			else
			{
				position.y += control.Height;
				maxSize = Mathf.Max( Mathf.CeilToInt( control.Width + itemSpacing.x ), maxSize );
			}

			control.IsVisible = canShowControlUnclipped( control );

		}

	}

	#endregion

	#region Private utility methods

	private bool canShowControlUnclipped( dfControl control )
	{

		if( !hideClippedControls )
			return true;

		var position = control.RelativePosition;

		if( position.x + control.Width >= panel.Width - borderPadding.right )
			return false;

		if( position.y + control.Height >= panel.Height - borderPadding.bottom )
			return false;

		return true;

	}

	#endregion

}
