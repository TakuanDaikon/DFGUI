// @cond DOXY_IGNORE
/* Copyright 2013-2014 Daikon Forge */

/// <summary>
/// This is a marker interface used by the Editor components to 
/// identify Component types that can be displayed in a context menu.
/// </summary>
public interface IDataBindingComponent
{

	bool IsBound { get; }

	void Bind();
	void Unbind();

}

// @endcond DOXY_IGNORE
