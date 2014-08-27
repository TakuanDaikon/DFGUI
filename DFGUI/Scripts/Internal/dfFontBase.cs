/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityMaterial = UnityEngine.Material;
using UnityFont = UnityEngine.Font;

[Serializable]
public abstract class dfFontBase : MonoBehaviour
{

	#region Properties 

	/// <summary>
	/// Returns a reference to the material that will be used to 
	/// render text
	/// </summary>
	public abstract UnityMaterial Material { get; set; }

	/// <summary>
	/// Returns a reference to the texture which contains the
	/// glyph images that will be used to render text
	/// </summary>
	public abstract Texture Texture { get; }

	/// <summary>
	/// Returns a value indicating whether the font configuration is valid
	/// </summary>
	public abstract bool IsValid { get; }

	/// <summary>
	/// Returns the default font size, in pixels
	/// </summary>
	public abstract int FontSize { get; set; }

	/// <summary>
	/// Returns the minimimum height (in pixels) of each line of rendered text
	/// </summary>
	public abstract int LineHeight { get; set; }

	#endregion 

	#region Public methods 

	public abstract dfFontRendererBase ObtainRenderer();

	#endregion

}

/// <summary>
/// Defines the base requirements for a font renderer
/// </summary>
public abstract class dfFontRendererBase : IDisposable
{

	#region Public properties

	public dfFontBase Font { get; protected set; }
	public Vector2 MaxSize { get; set; }
	public float PixelRatio { get; set; }
	public float TextScale { get; set; }
	public int CharacterSpacing { get; set; }
	public Vector3 VectorOffset { get; set; }
	public bool ProcessMarkup { get; set; }
	public bool WordWrap { get; set; }
	public bool MultiLine { get; set; }
	public bool OverrideMarkupColors { get; set; }
	public bool ColorizeSymbols { get; set; }
	public TextAlignment TextAlign { get; set; }
	public Color32 DefaultColor { get; set; }
	public Color32? BottomColor { get; set; }
	public float Opacity { get; set; }
	public bool Outline { get; set; }
	public int OutlineSize { get; set; }
	public Color32 OutlineColor { get; set; }
	public bool Shadow { get; set; }
	public Color32 ShadowColor { get; set; }
	public Vector2 ShadowOffset { get; set; }
	public int TabSize { get; set; }
	public List<int> TabStops { get; set; }

	public Vector2 RenderedSize { get; internal set; }
	public int LinesRendered { get; internal set; }

	#endregion

	#region Public methods

	public abstract void Release();

	/// <summary>
	/// Returns an array of float values, each one corresponding 
	/// to the width of the character at the same position of the 
	/// source text. NOTE: Does not do any markup processing, and
	/// must only be used on single-line plaintext.
	/// </summary>
	public abstract float[] GetCharacterWidths( string text );

	/// <summary>
	/// Measures the given text and returns the size (in pixels) required to render the text.
	/// </summary>
	/// <param name="text">The text to be measured</param>
	/// <returns>The size required to render the text</returns>
	public abstract Vector2 MeasureString( string text );

	/// <summary>
	/// Render the given text as mesh data to the given destination buffer
	/// </summary>
	/// <param name="text">The text to be rendered</param>
	/// <param name="destination">The dfRenderData buffer that will hold the 
	/// text mesh information</param>
	public abstract void Render( string text, dfRenderData destination );

	#endregion

	#region Protected methods 

	protected virtual void Reset()
	{
	
		this.Font = null;
		this.PixelRatio = 0;
		this.TextScale = 1;
		this.CharacterSpacing = 0;
		this.VectorOffset = Vector3.zero;
		this.ProcessMarkup = false;
		this.WordWrap = false;
		this.MultiLine = false;
		this.OverrideMarkupColors = false;
		this.ColorizeSymbols = false;
		this.TextAlign = TextAlignment.Left;
		this.DefaultColor = Color.white;
		this.BottomColor = (Color32?)null;
		this.Opacity = 1f;
		this.Outline = false;
		this.Shadow = false;

	}

	#endregion

	#region IDisposable Members

	public void Dispose()
	{
		this.Release();
	}

	#endregion

}
