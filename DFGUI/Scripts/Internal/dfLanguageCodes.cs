using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// http://www.loc.gov/standards/iso639-2/php/code_list.php
/// </summary>
public enum dfLanguageCode : int
{
	///<summary>None selected</summary>
	None,
	///<summary>Abkhazian</summary>
	AB,
	///<summary>Afar</summary>
	AA,
	///<summary>Afrikaans</summary>
	AF,
	///<summary>Albanian</summary>
	SQ,
	///<summary>Amharic</summary>
	AM,
	///<summary>Arabic</summary>
	AR,
	///<summary>Aragonese</summary>
	AN,
	///<summary>Armenian</summary>
	HY,
	///<summary>Assamese</summary>
	AS,
	///<summary>Avestan</summary>
	AE,
	///<summary>Aymara</summary>
	AY,
	///<summary>Azerbaijani</summary>
	AZ,
	///<summary>Bashkir</summary>
	BA,
	///<summary>Basque</summary>
	EU,
	///<summary>Belarusian</summary>
	BE,
	///<summary>Bengali</summary>
	BN,
	///<summary>Bihari</summary>
	BH,
	///<summary>Bislama</summary>
	BI,
	///<summary>Bosnian</summary>
	BS,
	///<summary>Breton</summary>
	BR,
	///<summary>Bulgarian</summary>
	BG,
	///<summary>Burmese</summary>
	MY,
	///<summary>Catalan</summary>
	CA,
	///<summary>Chamorro</summary>
	CH,
	///<summary>Chechen</summary>
	CE,
	///<summary>Chinese</summary>
	ZH,
	///<summary>Church Slavic; Slavonic; Old Bulgarian</summary>
	CU,
	///<summary>Chuvash</summary>
	CV,
	///<summary>Cornish</summary>
	KW,
	///<summary>Corsican</summary>
	CO,
	///<summary>Croatian</summary>
	HR,
	///<summary>Czech</summary>
	CS,
	///<summary>Danish</summary>
	DA,
	///<summary>Divehi; Dhivehi; Maldivian</summary>
	DV,
	///<summary>Dutch</summary>
	NL,
	///<summary>Dzongkha</summary>
	DZ,
	///<summary>English</summary>
	EN,
	///<summary>Esperanto</summary>
	EO,
	///<summary>Estonian</summary>
	ET,
	///<summary>Faroese</summary>
	FO,
	///<summary>Fijian</summary>
	FJ,
	///<summary>Finnish</summary>
	FI,
	///<summary>French</summary>
	FR,
	///<summary>Gaelic; Scottish Gaelic</summary>
	GD,
	///<summary>Galician</summary>
	GL,
	///<summary>Georgian</summary>
	KA,
	///<summary>German</summary>
	DE,
	///<summary>Greek, Modern (1453-)</summary>
	EL,
	///<summary>Guarani</summary>
	GN,
	///<summary>Gujarati</summary>
	GU,
	///<summary>Haitian; Haitian Creole</summary>
	HT,
	///<summary>Hausa</summary>
	HA,
	///<summary>Hebrew</summary>
	HE,
	///<summary>Herero</summary>
	HZ,
	///<summary>Hindi</summary>
	HI,
	///<summary>Hiri Motu</summary>
	HO,
	///<summary>Hungarian</summary>
	HU,
	///<summary>Icelandic</summary>
	IS,
	///<summary>Ido</summary>
	IO,
	///<summary>Indonesian</summary>
	ID,
	///<summary>Interlingua (International Auxiliary Language Association)</summary>
	IA,
	///<summary>Interlingue</summary>
	IE,
	///<summary>Inuktitut</summary>
	IU,
	///<summary>Inupiaq</summary>
	IK,
	///<summary>Irish</summary>
	GA,
	///<summary>Italian</summary>
	IT,
	///<summary>Japanese</summary>
	JA,
	///<summary>Javanese</summary>
	JV,
	///<summary>Kalaallisut</summary>
	KL,
	///<summary>Kannada</summary>
	KN,
	///<summary>Kashmiri</summary>
	KS,
	///<summary>Kazakh</summary>
	KK,
	///<summary>Khmer</summary>
	KM,
	///<summary>Kikuyu; Gikuyu</summary>
	KI,
	///<summary>Kinyarwanda</summary>
	RW,
	///<summary>Kirghiz</summary>
	KY,
	///<summary>Komi</summary>
	KV,
	///<summary>Korean</summary>
	KO,
	///<summary>Kuanyama; Kwanyama</summary>
	KJ,
	///<summary>Kurdish</summary>
	KU,
	///<summary>Lao</summary>
	LO,
	///<summary>Latin</summary>
	LA,
	///<summary>Latvian</summary>
	LV,
	///<summary>Limburgan; Limburger; Limburgish</summary>
	LI,
	///<summary>Lingala</summary>
	LN,
	///<summary>Lithuanian</summary>
	LT,
	///<summary>Luxembourgish; Letzeburgesch</summary>
	LB,
	///<summary>Macedonian</summary>
	MK,
	///<summary>Malagasy</summary>
	MG,
	///<summary>Malay</summary>
	MS,
	///<summary>Malayalam</summary>
	ML,
	///<summary>Maltese</summary>
	MT,
	///<summary>Manx</summary>
	GV,
	///<summary>Maori</summary>
	MI,
	///<summary>Marathi</summary>
	MR,
	///<summary>Marshallese</summary>
	MH,
	///<summary>Moldavian</summary>
	MO,
	///<summary>Mongolian</summary>
	MN,
	///<summary>Nauru</summary>
	NA,
	///<summary>Navaho, Navajo</summary>
	NV,
	///<summary>Ndebele, North</summary>
	ND,
	///<summary>Ndebele, South</summary>
	NR,
	///<summary>Ndonga</summary>
	NG,
	///<summary>Nepali</summary>
	NE,
	///<summary>Northern Sami</summary>
	SE,
	///<summary>Norwegian</summary>
	NO,
	///<summary>Norwegian Bokmal</summary>
	NB,
	///<summary>Norwegian Nynorsk</summary>
	NN,
	///<summary>Nyanja; Chichewa; Chewa</summary>
	NY,
	///<summary>Occitan (post 1500); Provencal</summary>
	OC,
	///<summary>Oriya</summary>
	OR,
	///<summary>Oromo</summary>
	OM,
	///<summary>Ossetian; Ossetic</summary>
	OS,
	///<summary>Pali</summary>
	PI,
	///<summary>Panjabi</summary>
	PA,
	///<summary>Persian</summary>
	FA,
	///<summary>Polish</summary>
	PL,
	///<summary>Portuguese</summary>
	PT,
	///<summary>Pushto</summary>
	PS,
	///<summary>Quechua</summary>
	QU,
	///<summary>Raeto-Romance</summary>
	RM,
	///<summary>Romanian</summary>
	RO,
	///<summary>Rundi</summary>
	RN,
	///<summary>Russian</summary>
	RU,
	///<summary>Samoan</summary>
	SM,
	///<summary>Sango</summary>
	SG,
	///<summary>Sanskrit</summary>
	SA,
	///<summary>Sardinian</summary>
	SC,
	///<summary>Serbo-Croatian</summary>
	SH,
	///<summary>Serbian</summary>
	SR,
	///<summary>Shona</summary>
	SN,
	///<summary>Sichuan Yi</summary>
	II,
	///<summary>Sindhi</summary>
	SD,
	///<summary>Sinhala; Sinhalese</summary>
	SI,
	///<summary>Slovak</summary>
	SK,
	///<summary>Slovenian</summary>
	SL,
	///<summary>Somali</summary>
	SO,
	///<summary>Sotho, Southern</summary>
	ST,
	///<summary>Spanish; Castilian</summary>
	ES,
	///<summary>Sundanese</summary>
	SU,
	///<summary>Swahili</summary>
	SW,
	///<summary>Swati</summary>
	SS,
	///<summary>Swedish</summary>
	SV,
	///<summary>Tagalog</summary>
	TL,
	///<summary>Tahitian</summary>
	TY,
	///<summary>Tajik</summary>
	TG,
	///<summary>Tamil</summary>
	TA,
	///<summary>Tatar</summary>
	TT,
	///<summary>Telugu</summary>
	TE,
	///<summary>Thai</summary>
	TH,
	///<summary>Tibetan</summary>
	BO,
	///<summary>Tigrinya</summary>
	TI,
	///<summary>Tonga (Tonga Islands)</summary>
	TO,
	///<summary>Tsonga</summary>
	TS,
	///<summary>Tswana</summary>
	TN,
	///<summary>Turkish</summary>
	TR,
	///<summary>Turkmen</summary>
	TK,
	///<summary>Twi</summary>
	TW,
	///<summary>Uighur</summary>
	UG,
	///<summary>Ukrainian</summary>
	UK,
	///<summary>Urdu</summary>
	UR,
	///<summary>Uzbek</summary>
	UZ,
	///<summary>Vietnamese</summary>
	VI,
	///<summary>Volapuk</summary>
	VO,
	///<summary>Walloon</summary>
	WA,
	///<summary>Welsh</summary>
	CY,
	///<summary>Western Frisian</summary>
	FY,
	///<summary>Wolof</summary>
	WO,
	///<summary>Xhosa</summary>
	XH,
	///<summary>Yiddish</summary>
	YI,
	///<summary>Yoruba</summary>
	YO,
	///<summary>Zhuang; Chuang</summary>
	ZA,
	///<summary>Zulu</summary>
	ZU,
}