using System;
using UnityEngine;

/// <summary>
/// Describes the functionality available to all components which can 
/// host dfControl instances
/// </summary>
public interface IDFControlHost
{
	T AddControl<T>() where T : dfControl;
	dfControl AddControl( Type controlType );
	void AddControl( dfControl child );
	dfControl AddPrefab( GameObject prefab );
}
