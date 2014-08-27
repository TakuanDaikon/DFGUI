// @cond DOXY_IGNORE
/* Copyright 2013-2014 Daikon Forge */

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Provides the ability to bind a property on one object to the value of 
/// another property on another object.
/// </summary>
[Serializable]
#if !UNITY_IPHONE
//[AddComponentMenu( "Daikon Forge/Data Binding/Expression Binding" )]
[Obsolete( "The expression binding functionality is no longer supported and may be removed in future versions of DFGUI" )]
#endif
public class dfExpressionPropertyBinding : MonoBehaviour, IDataBindingComponent
{

	#region Public fields

	/// <summary>
	/// Specifies the source data object
	/// </summary>
	public Component DataSource;

	/// <summary>
	/// Specifies which field or property to bind to on the target component
	/// </summary>
	public dfComponentMemberInfo DataTarget;

	#endregion

	#region Serialized protected members 

	[SerializeField]
	protected string expression;

	#endregion

	#region Private fields

#if !UNITY_IPHONE
	private Delegate compiledExpression = null;
	private dfObservableProperty targetProperty;
#endif

	private bool isBound = false;

	#endregion

	#region Public properties 

	public bool IsBound { get { return this.isBound; } }

	public string Expression
	{
		get { return this.expression; }
		set
		{
			if( !string.Equals( value, expression ) )
			{
				Unbind();
				this.expression = value;
			}
		}
	}

	#endregion

	#region Unity events

#if UNITY_IPHONE
	public void Start()
	{
		Debug.LogError( "Dynamic expression evaluation is not supported on iOS targets", this.gameObject );
	}
#elif UNITY_METRO
	private static bool _WACKWarningIssued = false;
	public void Start()
	{
		if( !_WACKWarningIssued )
		{
			_WACKWarningIssued = true;
			Debug.LogWarning( "WARNING: Dynamic expression evaluation does not pass WACK certification due to use of unapproved API", this.gameObject );
		}
	}
#endif

	public void OnDisable()
	{
		Unbind();
	}

	public void Update()
	{

#if !UNITY_IPHONE
		if( isBound )
		{
			evaluate();
		}
		else
		{

			var canBind =
				DataSource != null &&
				!string.IsNullOrEmpty( expression ) &&
				DataTarget.IsValid;

			if( canBind )
			{
				Bind();
			}

		}
#endif

	}

	#endregion

	#region Public methods

	/// <summary>
	/// Unbind the source and target properties 
	/// </summary>
	public void Unbind()
	{

		if( !isBound )
			return;

#if !UNITY_IPHONE
		compiledExpression = null;
		targetProperty = null;
#endif

		isBound = false;

	}

	/// <summary>
	/// Bind the source and target properties 
	/// </summary>
	public void Bind()
	{
#if !UNITY_IPHONE

		if( isBound )
			return;

		// A dfProxyDataObject might legitimately have a NULL value at scene
		// startup, so just skip binding for now if that's the case. This assumes
		// that the proxy object will have its value set when it's initialized
		// but that control startup order is not consistent.
		if( DataSource is dfDataObjectProxy && ( (dfDataObjectProxy)DataSource ).Data == null )
			return;

		// Define the constants and types that will be available to the script expression
		var settings = new dfScriptEngineSettings()
		{
			Constants = new Dictionary<string, object>()
			{
				// Add any other types whose static members you wish 
				// to be available to the script expression
				{ "Application", typeof( UnityEngine.Application ) },
				{ "Color", typeof( UnityEngine.Color ) },
				{ "Color32", typeof( UnityEngine.Color32 ) },
				{ "Random", typeof( UnityEngine.Random ) },
				{ "Time", typeof( UnityEngine.Time ) },
				{ "ScriptableObject", typeof( UnityEngine.ScriptableObject ) },
				{ "Vector2", typeof( UnityEngine.Vector2 ) },
				{ "Vector3", typeof( UnityEngine.Vector3 ) },
				{ "Vector4", typeof( UnityEngine.Vector4 ) },
				{ "Quaternion", typeof( UnityEngine.Quaternion ) },
				{ "Matrix", typeof( UnityEngine.Matrix4x4 ) },
				{ "Mathf", typeof( UnityEngine.Mathf ) }
			}
		};
		
		// Add any variables you want the script expression to have access to
		if( DataSource is dfDataObjectProxy )
		{
			var proxy = DataSource as dfDataObjectProxy;
			settings.AddVariable( new dfScriptVariable( "source", null, proxy.DataType ) );
		}
		else
		{
			settings.AddVariable( new dfScriptVariable( "source", DataSource ) );
		}

		// Compile the script expression and store the resulting Delegate.
		// Note that any syntax errors or compile errors will throw an 
		// exception here, which is why we don't init the target property
		// or set the isBound variable until after this step.
		compiledExpression = dfScriptEngine.CompileExpression( expression, settings );

		// Initialize our target property
		targetProperty = DataTarget.GetProperty();

		// Keep track of whether the binding was successful
		isBound = ( compiledExpression != null ) && ( targetProperty != null );
#endif
	}

	#endregion

	#region Private utility methods 

#if !UNITY_IPHONE
	private void evaluate()
	{

		try
		{

			object sourceObject = DataSource;
			if( sourceObject is dfDataObjectProxy )
			{
				sourceObject = ( (dfDataObjectProxy)sourceObject ).Data;
			}

			var value = compiledExpression.DynamicInvoke( sourceObject );
			targetProperty.Value = value;

		}
		catch( Exception err )
		{
			Debug.LogError( err );
		}

	}
#endif

	#endregion

	#region System.Object overrides

	/// <summary>
	/// Returns a formatted string summarizing this object's state
	/// </summary>
	public override string ToString()
	{

		string targetType = DataTarget != null && DataTarget.Component != null ? DataTarget.Component.GetType().Name : "[null]";
		string targetMember = DataTarget != null && !string.IsNullOrEmpty( DataTarget.MemberName ) ? DataTarget.MemberName : "[null]";

		return string.Format( "Bind [expression] -> {0}.{1}", targetType, targetMember );

	}

	#endregion

}

// @endcond DOXY_IGNORE
