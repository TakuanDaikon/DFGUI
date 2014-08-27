using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

/// <summary>
/// Responsible for loading the language definition files and returning
/// localized versions of strings.
/// <h3>Data File Format</h3>
/// <p>All data files <b>must</b> conform to the following format in order
/// to be used by the dfLanguageManager class. Localization data is stored
/// as comma-seperated values (<a href="https://tools.ietf.org/html/rfc4180" target="_blank">CSV</a>) 
/// in a text file which must follow these rules:</p>
/// <ol>
///		<li>Each record is located on a separate line, delimited by a newline (LF) character.
///		<i>Note that CRLF-style line breaks will be converted internally during processing to
///		single-LF characters.</i></li>
///		<li>The last record in the file may or may not have an ending newline.</li>
///		<li>The first line of the file must contain a header record in the same format
///		as normal record lines, containing names corresponding to the fields in the 
///		file and should contain the same number of fields as the records in the rest
///		of the file. The name of the first field is not used, but is KEY by default.
///		The following fields <b>must</b> be an uppercase two-letter ISO 639-1 country code 
///		that indicates the language for that column.</li>
///		<li>Within the header and each record, there may be one or more fields, separated
///		by commas. Each line should contain	the same number of fields throughout the 
///		file.</li>
///		<li>Fields containing newline characters, double-quote characters, or comma 
///		characters must be enclosed in double-quotes.</li>
///		<li>If double-quotes are used to enclose fields, then a double-quote appearing 
///		inside a field must be escaped by preceding it with another double quote.</li>
/// </ol>
/// <h4>Example:</h4>
/// <pre style='margin-left: 4em'>
/// KEY,EN,ES,FR,DE
/// GREET,"Greetings, citizen!","Saludos, ciudadano!","Salutations, citoyens!","Grüße, Bürger!"
/// ENTER,Enter the door,Entra por la puerta,Entrez dans la porte,Geben Sie die Tür
/// QUOTE,"""Quickly now!"", he said","""¡Rápido!"", Dijo","""Vite!"", At-il dit","""Schnell jetzt!"", Sagte er"
/// </pre>
/// </summary>
public class dfLanguageManager : MonoBehaviour
{

	#region Serialized fields 

	[SerializeField]
	private dfLanguageCode currentLanguage = dfLanguageCode.None;

	[SerializeField]
	private TextAsset dataFile = null;

	#endregion

	#region Private variables

	private Dictionary<string, string> strings = new Dictionary<string, string>();

	#endregion

	#region Public properties 

	/// <summary>
	/// Returns the language code that represents the language currently
	/// used by the dfLanguageManager.
	/// </summary>
	public dfLanguageCode CurrentLanguage
	{
		get { return this.currentLanguage; }
	}

	/// <summary>
	/// Gets or sets the reference to the data file containing the localized
	/// string definitions.
	/// </summary>
	public TextAsset DataFile
	{
		get { return this.dataFile; }
		set
		{
			if( value != this.dataFile )
			{
				this.dataFile = value;
				LoadLanguage( this.currentLanguage );
			}
		}
	}

	#endregion

	#region Unity events 

	// @private
	public void Start()
	{

		var languageCode = this.currentLanguage;
		if( this.currentLanguage == dfLanguageCode.None ) 
			languageCode = SystemLanguageToLanguageCode( Application.systemLanguage );

		LoadLanguage( languageCode );

	}

	#endregion

	#region Public methods

	/// <summary>
	/// Loads the indicated language from the data file
	/// </summary>
	/// <param name="language">The language code representing the language data to be loaded</param>
	public void LoadLanguage( dfLanguageCode language )
	{

		// Keep track of the current language
		this.currentLanguage = language;

		// Start from a clean slate
		strings.Clear();

		if( dataFile != null )
		{
			parseDataFile();
		}
		
		// When the language is loaded, localize all controls
		var controls = GetComponentsInChildren<dfControl>();
		for( int i = 0; i < controls.Length; i++ )
		{
			controls[ i ].Localize();
		}

	}

	/// <summary>
	/// Returns the language-specific string specified by the key, if 
	/// one is defined in the current language data.
	/// </summary>
	/// <param name="key">A key specifying the string to be returned</param>
	/// <returns>If a localized string matching the key is found, that 
	/// value will be returned. If no matching string could be found, the <paramref name="key"/>
	/// value is returned unchanged.</returns>
	public string GetValue( string key )
	{

		string localizedValue = string.Empty;
		if( strings.TryGetValue( key, out localizedValue ) )
		{
			return localizedValue;
		}

		return key;

	}

	#endregion

	#region Private utility methods 

	private void parseDataFile()
	{

		var data = dataFile.text.Replace( "\r\n", "\n" ).Trim();
		
		var keys = new List<string>();
		int index = parseLine( data, keys, 0 );

		// Find the current language column position.
		int languageIndex = keys.IndexOf( this.currentLanguage.ToString() );
		if( languageIndex < 0 )
		{
			// Current language is not contained in the data file so 
			// exit without any further processing
			return;
		}

		var values = new List<string>();
		while( index < data.Length )
		{

			index = parseLine( data, values, index );
			if( values.Count == 0 )
				continue;

			var key = values[ 0 ];
			var value = ( languageIndex < values.Count ) ? values[ languageIndex ] : "";

			strings[ key ] = value;

		}

	}

	private int parseLine( string data, List<string> values, int index )
	{

		values.Clear();

		var quotedValue = false;
		var current = new StringBuilder( 256 );

		while( index < data.Length )
		{

			var ch = data[ index ];
			if( ch == '"' )
			{
				if( !quotedValue )
				{
					quotedValue = true;
				}
				else
				{
					if( index + 1 < data.Length && data[ index + 1 ] == ch )
					{
						index += 1;
						current.Append( ch );
					}
					else
					{
						quotedValue = false;
					}
				}
			}
			else if( ch == ',' )
			{
				if( quotedValue )
				{
					current.Append( ch );
				}
				else
				{
					values.Add( current.ToString() );
					current.Length = 0;
				}
			}
			else if( ch == '\n' )
			{
				if( quotedValue )
				{
					current.Append( ch );
				}
				else
				{
					index += 1;
					break;
				}
			}
			else
			{
				current.Append( ch );
			}

			index++;

		}

		if( current.Length > 0 )
		{
			values.Add( current.ToString() );
		}

		return index;

	}

	private dfLanguageCode SystemLanguageToLanguageCode( SystemLanguage language )
	{

		switch( language )
		{
			case SystemLanguage.Afrikaans:
				return dfLanguageCode.AF;
			case SystemLanguage.Arabic:
				return dfLanguageCode.AR;
			case SystemLanguage.Basque:
				return dfLanguageCode.EU;
			case SystemLanguage.Belarusian:
				return dfLanguageCode.BE;
			case SystemLanguage.Bulgarian:
				return dfLanguageCode.BG;
			case SystemLanguage.Catalan:
				return dfLanguageCode.CA;
			case SystemLanguage.Chinese:
				return dfLanguageCode.ZH;
			case SystemLanguage.Czech:
				return dfLanguageCode.CS;
			case SystemLanguage.Danish:
				return dfLanguageCode.DA;
			case SystemLanguage.Dutch:
				return dfLanguageCode.NL;
			case SystemLanguage.English:
				return dfLanguageCode.EN;
			case SystemLanguage.Estonian:
				return dfLanguageCode.ES;
			case SystemLanguage.Faroese:
				return dfLanguageCode.FO;
			case SystemLanguage.Finnish:
				return dfLanguageCode.FI;
			case SystemLanguage.French:
				return dfLanguageCode.FR;
			case SystemLanguage.German:
				return dfLanguageCode.DE;
			case SystemLanguage.Greek:
				return dfLanguageCode.EL;
			case SystemLanguage.Hebrew:
				return dfLanguageCode.HE;
			case SystemLanguage.Hungarian:
				return dfLanguageCode.HU;
			case SystemLanguage.Icelandic:
				return dfLanguageCode.IS;
			case SystemLanguage.Indonesian:
				return dfLanguageCode.ID;
			case SystemLanguage.Italian:
				return dfLanguageCode.IT;
			case SystemLanguage.Japanese:
				return dfLanguageCode.JA;
			case SystemLanguage.Korean:
				return dfLanguageCode.KO;
			case SystemLanguage.Latvian:
				return dfLanguageCode.LV;
			case SystemLanguage.Lithuanian:
				return dfLanguageCode.LT;
			case SystemLanguage.Norwegian:
				return dfLanguageCode.NO;
			case SystemLanguage.Polish:
				return dfLanguageCode.PL;
			case SystemLanguage.Portuguese:
				return dfLanguageCode.PT;
			case SystemLanguage.Romanian:
				return dfLanguageCode.RO;
			case SystemLanguage.Russian:
				return dfLanguageCode.RU;
			case SystemLanguage.SerboCroatian:
				return dfLanguageCode.SH;
			case SystemLanguage.Slovak:
				return dfLanguageCode.SK;
			case SystemLanguage.Slovenian:
				return dfLanguageCode.SL;
			case SystemLanguage.Spanish:
				return dfLanguageCode.ES;
			case SystemLanguage.Swedish:
				return dfLanguageCode.SV;
			case SystemLanguage.Thai:
				return dfLanguageCode.TH;
			case SystemLanguage.Turkish:
				return dfLanguageCode.TR;
			case SystemLanguage.Ukrainian:
				return dfLanguageCode.UK;
			case SystemLanguage.Unknown:
				return dfLanguageCode.EN;
			case SystemLanguage.Vietnamese:
				return dfLanguageCode.VI;
		}

		throw new ArgumentException( "Unknown system language: " + language );

	}

	#endregion

}
