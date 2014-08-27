using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field )]
public class dfCategoryAttribute : System.Attribute
{

	public string Category { get; private set; }

	public dfCategoryAttribute( string category )
	{
		this.Category = category;
	}

}

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field )]
public class dfTooltipAttribute : System.Attribute
{

	public string Tooltip { get; private set; }

	public dfTooltipAttribute( string tooltip )
	{
		this.Tooltip = tooltip;
	}

}

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field )]
public class dfHelpAttribute : System.Attribute
{

	public string HelpURL { get; private set; }

	public dfHelpAttribute( string url )
	{
		this.HelpURL = url;
	}

}

