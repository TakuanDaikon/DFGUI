using UnityEngine;
using System.Collections;

[AddComponentMenu( "Daikon Forge/Examples/Containers/Auto-Arrange Item" )]
public class AutoArrangeDemoItem : MonoBehaviour
{

	#region Private variables 

	private dfButton control;
	private dfAnimatedVector2 size;
	private bool isExpanded = false;

	#endregion

	#region Unity events 

	void Start()
	{

		this.control = GetComponent<dfButton>();
		this.size = new dfAnimatedVector2( control.Size, control.Size, 0.33f );

		this.control.Text = "#" + ( control.ZOrder + 1 );

	}

	void Update()
	{
		control.Size = this.size.Value.RoundToInt();
	}

	#endregion

	#region Control events 

	void OnClick()
	{
		Toggle();
	}

	#endregion

	#region Public methods

	public void Expand()
	{
		size.StartValue = size.EndValue;
		this.size.EndValue = new Vector2( 128, 96 );
		isExpanded = true;
	}

	public void Collapse()
	{
		size.StartValue = size.EndValue;
		this.size.EndValue = new Vector2( 48, 48 );
		isExpanded = false;
	}

	public void Toggle()
	{
		if( isExpanded )
			Collapse();
		else
			Expand();
	}

	#endregion

}
