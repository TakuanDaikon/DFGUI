using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu( "Daikon Forge/Examples/General/Textbox Prompt" )]
[ExecuteInEditMode]
public class TextboxPrompt : MonoBehaviour 
{

	public Color32 promptColor = Color.gray;
	public Color32 textColor = Color.white;

	public string promptText = "(enter some text)";

	private dfTextbox _textbox;

	public void OnEnable()
	{

		this._textbox = GetComponent<dfTextbox>();

		if( string.IsNullOrEmpty( _textbox.Text ) || _textbox.Text == promptText )
		{
			_textbox.Text = promptText;
			_textbox.TextColor = promptColor;
		}

	}

	public void OnDisable()
	{
		if( _textbox != null && _textbox.Text == promptText )
		{
			_textbox.Text = "";
		}
	}

	public void OnEnterFocus( dfControl control, dfFocusEventArgs args )
	{
		if( _textbox.Text == promptText )
		{
			_textbox.Text = "";
		}
		_textbox.TextColor = textColor;
	}

	public void OnLeaveFocus( dfControl control, dfFocusEventArgs args )
	{
		if( string.IsNullOrEmpty( _textbox.Text ) )
		{
			_textbox.Text = promptText;
			_textbox.TextColor = promptColor;
		}
	}

	public void OnTextChanged( dfControl control, string value )
	{
		if( value != promptText )
		{
			_textbox.TextColor = textColor;
		}
	}

}
