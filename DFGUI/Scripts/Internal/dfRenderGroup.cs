/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

// NOTES: This class is experimental. It allows me to configure specific
// points in the control hierarchy that will be rendered to a separate 
// mesh. This allows for flexibility in limiting the impact of changes
// to any specific control by allowing frequently-updated controls to 
// participate in a smaller and more targeted render phase.

[Serializable]
[ExecuteInEditMode]
[AddComponentMenu( "Daikon Forge/User Interface/Render Group" )]
// @private
internal class dfRenderGroup : MonoBehaviour
{

	#region Static variables 

	private static List<dfRenderGroup> activeInstances = new List<dfRenderGroup>();

	#endregion 

	#region Serialized fields

	[SerializeField]
	protected dfClippingMethod clipType;

	#endregion 

	#region Private runtime variables

	private Mesh renderMesh;
	private MeshFilter renderFilter;
	private MeshRenderer meshRenderer;
	private Camera renderCamera;

	private dfControl attachedControl;

	private static dfRenderData masterBuffer = new dfRenderData( 4096 );
	private dfList<dfRenderData> drawCallBuffers = new dfList<dfRenderData>();
	private List<int> submeshes = new List<int>();

	private Stack<dfTriangleClippingRegion> clipStack = new Stack<dfTriangleClippingRegion>();
	private dfList<Rect> groupOccluders = new dfList<Rect>();
	private dfList<dfControl> groupControls = new dfList<dfControl>();
	private dfList<dfRenderGroup> renderGroups = new dfList<dfRenderGroup>();

	private ClipRegionInfo clipInfo = new ClipRegionInfo();
	private Rect clipRect = new Rect();
	private Rect containerRect = new Rect();

	private int drawCallCount = 0;
	private bool isDirty = false;
		
	#endregion 

	#region Public properties 

	/// <summary>
	/// Gets or sets the type of clipping that will be applied to child controls
	/// </summary>
	public dfClippingMethod ClipType
	{
		get { return this.clipType; }
		set
		{
			if( value != this.clipType )
			{
				this.clipType = value;
				if( this.attachedControl != null )
				{
					this.attachedControl.Invalidate();
				}
			}
		}
	}

	#endregion 

	#region Unity events

#if UNITY_EDITOR

	[HideInInspector]
	public void OnValidate()
	{

		if( this.attachedControl != null )
		{
			this.isDirty = true;
			attachedControl.GetManager().Invalidate();
		}

	}

	[HideInInspector]
	public virtual void OnDrawGizmos()
	{

		if( attachedControl == null || meshRenderer == null || !this.enabled )
			return;

		var debugShowMesh = UnityEditor.EditorPrefs.GetBool( "dfGUIManager.ShowMesh", false );
		UnityEditor.EditorUtility.SetSelectedWireframeHidden( meshRenderer, !debugShowMesh );

		var controlCollider = attachedControl.collider as BoxCollider;
		if( controlCollider == null )
			return;
		else
			controlCollider.hideFlags = HideFlags.HideInInspector;

		if( !attachedControl.IsVisible || attachedControl.Opacity < 0.1f )
			return;

		var p2u = attachedControl.PixelsToUnits();
		var controlCenter = attachedControl.Pivot.TransformToCenter( attachedControl.Size );
		var controlSize = ( (Vector3)attachedControl.Size );

		var controlPadding = attachedControl.GetClipPadding();
		controlSize.x -= controlPadding.horizontal;
		controlSize.y -= controlPadding.vertical;

		var clipOffset = new Vector3( controlPadding.left - controlPadding.right, -( controlPadding.top - controlPadding.bottom ) ) * 0.5f;

		Gizmos.matrix = Matrix4x4.TRS( transform.position, transform.rotation, transform.localScale );
		Gizmos.color = new UnityEngine.Color( 1f, 0.5f, 0, UnityEditor.Selection.gameObjects.Contains( this.gameObject ) ? 1f : 0.5f );
		Gizmos.DrawWireCube( (controlCenter + clipOffset) * p2u, controlSize * p2u );

	}

#endif 
		
	public void OnEnable()
	{

		activeInstances.Add( this );

		isDirty = true;

		if( meshRenderer == null )
		{
			initialize();
		}

		meshRenderer.enabled = true;

		if( attachedControl != null )
		{
			attachedControl.Invalidate();
		}
		else
		{
			dfGUIManager.InvalidateAll();
		}

		this.attachedControl = GetComponent<dfControl>();

	}

	public void OnDisable()
	{

		activeInstances.Remove( this );

		if( meshRenderer != null )
		{
			meshRenderer.enabled = false;
		}

		if( attachedControl != null )
		{
			attachedControl.Invalidate();
		}

	}

	public void OnDestroy()
	{

		if( renderFilter != null )
		{
			renderFilter.sharedMesh = null;
		}

		renderFilter = null;
		meshRenderer = null;

		if( renderMesh != null )
		{
			renderMesh.Clear();
			DestroyImmediate( renderMesh );
			renderMesh = null;
		}

		dfGUIManager.InvalidateAll();

	}

	#endregion 

	#region Public static methods 

	/// <summary>
	/// Returns the dfRenderGroup, if any, that is responsible for rendering the indicated control
	/// </summary>
	internal static dfRenderGroup GetRenderGroupForControl( dfControl control )
	{
		return GetRenderGroupForControl( control, false );
	}

	/// <summary>
	/// Returns the dfRenderGroup, if any, that is responsible for rendering the indicated control
	/// </summary>
	internal static dfRenderGroup GetRenderGroupForControl( dfControl control, bool directlyAttachedOnly )
	{

		var transform = control.transform;

		for( int i = 0; i < activeInstances.Count; i++ )
		{

			var instance = activeInstances[ i ];
	
			if( instance.attachedControl == control )
				return instance;

			if( !directlyAttachedOnly && transform.IsChildOf( instance.transform ) )
				return instance;

		}

		return null;

	}

	/// <summary>
	/// Invalidates (marks as needing to be rendered again) the dfRenderGroup instance, if any,
	/// that is responsible for rendering the indicated control
	/// </summary>
	/// <param name="control"></param>
	internal static void InvalidateGroupForControl( dfControl control )
	{

		var controlTransform = control.transform;

		for( int i = 0; i < activeInstances.Count; i++ )
		{
			var instance = activeInstances[ i ];
			if( controlTransform.IsChildOf( instance.transform ) )
			{
				instance.isDirty = true;
			}
		}

	}

	#endregion

	#region Public instance methods

	internal void Render( Camera renderCamera, dfControl control, dfList<Rect> occluders, dfList<dfControl> controlsRendered, uint checksum, float opacity )
	{

		if( meshRenderer == null )
		{
			initialize();
		}

		this.renderCamera = renderCamera;
		this.attachedControl = control;

		if( !isDirty )
		{

			// Update the caller's lists
			occluders.AddRange( groupOccluders );
			controlsRendered.AddRange( groupControls );

			return;

		}

		// Clear lists that will contain the results of the rendering process
		groupOccluders.Clear();
		groupControls.Clear();
		renderGroups.Clear();
		resetDrawCalls();

		// Disable shader clipping by default
		this.clipInfo = new ClipRegionInfo();
		this.clipRect = new Rect();

		// Define the main draw call buffer, which will be assigned as needed
		// by the renderControl() method
		var buffer = (dfRenderData)null;

		using( var defaultClipRegion = dfTriangleClippingRegion.Obtain() )
		{

			// Initialize the clipping region stack
			clipStack.Clear();
			clipStack.Push( defaultClipRegion );

			// Render the control and all of its children
			renderControl( ref buffer, control, checksum, opacity );

			// The clip stack is reset after every frame as it's only needed during rendering
			clipStack.Pop();

		}

		// Remove any empty draw call buffers. There might be empty 
		// draw call buffers due to controls that were clipped.
		drawCallBuffers.RemoveAll( isEmptyBuffer );
		drawCallCount = drawCallBuffers.Count;

		// At this point, the drawCallCount variable contains the 
		// number of draw calls needed to render the user interface.
		if( drawCallBuffers.Count == 0 )
		{
			meshRenderer.enabled = false;
			return;
		}
		else
		{
			meshRenderer.enabled = true;
		}

		// Consolidate all draw call buffers into one master buffer 
		// that will be used to build the Mesh
		var masterBuffer = compileMasterBuffer();

		// Build the master mesh
		var mesh = renderMesh;
		mesh.Clear( true );
		mesh.vertices = masterBuffer.Vertices.Items;
		mesh.uv = masterBuffer.UV.Items;
		mesh.colors32 = masterBuffer.Colors.Items;

		#region Set sub-meshes

		mesh.subMeshCount = submeshes.Count;
		for( int i = 0; i < submeshes.Count; i++ )
		{

			// Calculate the start and length of the submesh array
			var startIndex = submeshes[ i ];
			var length = masterBuffer.Triangles.Count - startIndex;
			if( i < submeshes.Count - 1 )
			{
				length = submeshes[ i + 1 ] - startIndex;
			}

			var submeshTriangles = dfTempArray<int>.Obtain( length );
			masterBuffer.Triangles.CopyTo( startIndex, submeshTriangles, 0, length );

			// Set the submesh's triangle index array
			mesh.SetTriangles( submeshTriangles, i );

		}

		#endregion

		// This render group no longer requires updating
		isDirty = false;

		// Update the caller's lists
		occluders.AddRange( groupOccluders );
		controlsRendered.AddRange( groupControls );

	}

	/// <summary>
	/// Updates the materials used to render this group, so that draw call order is maintained
	/// </summary>
	/// <param name="renderQueueBase"></param>
	internal void UpdateRenderQueue( ref int renderQueueBase )
	{

		// Count the number of non-null materials 
		var materialCount = getMaterialCount();
		var index = 0;

		var renderMaterials = dfTempArray<Material>.Obtain( materialCount );
		for( int i = 0; i < drawCallBuffers.Count; i++ )
		{

			// Skip null Material instances (typically happens only during
			// initial control creation in the Unity Editor)
			if( drawCallBuffers[ i ].Material == null )
				continue;

			// Obtain a reference to the material that will be used to render
			// the buffer. In simple cases this will be the same instance that 
			// was passed in, but if a new draw call is required then it may
			// return a copy of the original in order to be able to set the
			// copy's [renderQueue] property so that render order is preserved.
			var drawCallMaterial = dfMaterialCache.Lookup( drawCallBuffers[ i ].Material );
			drawCallMaterial.mainTexture = drawCallBuffers[ i ].Material.mainTexture;
			drawCallMaterial.shader = drawCallBuffers[ i ].Shader ?? drawCallMaterial.shader;
			drawCallMaterial.mainTextureScale = Vector2.zero;
			drawCallMaterial.mainTextureOffset = Vector2.zero;
			drawCallMaterial.renderQueue = ++renderQueueBase;

			var setShaderClipInfo =
				Application.isPlaying &&
				clipType == dfClippingMethod.Shader &&
				!clipInfo.IsEmpty &&
				( i > 0 );

			if( setShaderClipInfo )
			{

				var offsetToCenter = attachedControl.Pivot.TransformToCenter( attachedControl.Size );
				var clipOffsetX = offsetToCenter.x + clipInfo.Offset.x;
				var clipOffsetY = offsetToCenter.y + clipInfo.Offset.y;
				var p2u = attachedControl.PixelsToUnits();

				drawCallMaterial.mainTextureScale = new Vector2( 1f / ( -clipInfo.Size.x * 0.5f * p2u ), 1f / ( -clipInfo.Size.y * 0.5f * p2u ) );
				drawCallMaterial.mainTextureOffset = new Vector2( clipOffsetX / clipInfo.Size.x * 2f, clipOffsetY / clipInfo.Size.y * 2f );

			}

			// Copy the material to the final buffer
			renderMaterials[ index++ ] = drawCallMaterial;	// Copy the material to the final buffer

		}

		// Assign the Materials to the mesh renderer
		meshRenderer.sharedMaterials = renderMaterials;

		var items = renderGroups.Items;
		var itemCount = renderGroups.Count;

		for( int i = 0; i < itemCount; i++ )
		{
			items[ i ].UpdateRenderQueue( ref renderQueueBase );
		}

	}

	#endregion

	#region Private utility methods 

	private void renderControl( ref dfRenderData buffer, dfControl control, uint checksum, float opacity )
	{

		// Don't render controls that are not currently active
		if( !control.enabled || !control.gameObject.activeSelf )
			return;

		// Don't render controls that are invisible. Keeping a running 
		// accumulator for opacity allows us to know a control's final
		// calculated opacity
		var effectiveOpacity = opacity * control.Opacity;
		if( effectiveOpacity <= 0.001f )
		{
			return;
		}

		// If this control has a dfRenderGroup component on it, then pass off all 
		// responsibility for rendering that control to the component.
		var renderGroup = GetRenderGroupForControl( control, true );
		if( renderGroup != null && renderGroup != this && renderGroup.enabled )
		{
			renderGroups.Add( renderGroup );
			renderGroup.Render( renderCamera, control, groupOccluders, groupControls, checksum, effectiveOpacity );
			return;
		}

		// Don't render controls that have the IsVisible flag set to FALSE. Note that this is tested
		// *after* checking for the presence of a dfRenderGroup component, since that component (if
		// present) will need to update its own internal state if the control's IsVisible property
		// has changed.
		if( !control.GetIsVisibleRaw() )
			return;

		// Grab the current clip region information off the stack
		var clipInfo = clipStack.Peek();

		// Update the checksum to include the current control
		checksum = dfChecksumUtil.Calculate( checksum, control.Version );

		// Retrieve the control's bounds, which will be used in intersection testing
		// and triangle clipping.
		var bounds = control.GetBounds();
		var screenRect = control.GetScreenRect();
		var occluder = getControlOccluder( ref screenRect, control );

		// Indicates whether the control was not rendered because it fell outside
		// of the currently-active clipping region
		var wasClipped = false;

		if( !( control is IDFMultiRender ) )
		{

			// Ask the control to render itself and return a buffer of the 
			// information needed to render it as a Mesh
			var controlData = control.Render();
			if( controlData != null )
			{
				processRenderData( ref buffer, controlData, ref bounds, ref screenRect, checksum, clipInfo, ref wasClipped );
			}

		}
		else
		{

			// Ask the control to render itself and return as many dfRenderData buffers
			// as needed to render all elements of the control as a Mesh
			var childBuffers = ( (IDFMultiRender)control ).RenderMultiple();

			if( childBuffers != null )
			{

				var buffers = childBuffers.Items;
				var bufferCount = childBuffers.Count;

				for( int i = 0; i < bufferCount; i++ )
				{

					var childBuffer = buffers[ i ];
					if( childBuffer != null )
					{
						processRenderData( ref buffer, childBuffer, ref bounds, ref screenRect, checksum, clipInfo, ref wasClipped );
					}

				}

			}

		}

		// Allow control to keep track of its clipping state
		control.setClippingState( wasClipped );

		// Keep track of which controls are rendered, and where they appear on-screen
		groupOccluders.Add( occluder );
		groupControls.Add( control );

		// If the control has the "Clip child controls" option set, push
		// its clip region information onto the stack so that all controls
		// lower in the hierarchy are clipped against that region.
		if( control.ClipChildren )
		{
			if( !Application.isPlaying || clipType == dfClippingMethod.Software )
			{
				clipInfo = dfTriangleClippingRegion.Obtain( clipInfo, control );
				clipStack.Push( clipInfo );
			}
			else if( this.clipInfo.IsEmpty )
			{
				setClipRegion( control, ref screenRect );
			}
		}

		// Dereference raw child control list for direct access
		var childControls = control.Controls.Items;
		var childCount = control.Controls.Count;

		// Ensure lists contain enough space for child controls
		groupControls.EnsureCapacity( groupControls.Count + childCount );
		groupOccluders.EnsureCapacity( groupOccluders.Count + childCount );

		// Render all child controls
		for( int i = 0; i < childCount; i++ )
		{
			renderControl( ref buffer, childControls[ i ], checksum, effectiveOpacity );
		}

		// No longer need the current control's clip region information
		if( control.ClipChildren )
		{
			if( !Application.isPlaying || clipType == dfClippingMethod.Software )
			{
				clipStack.Pop().Release();
			}
		}

	}

	private void setClipRegion( dfControl control, ref Rect screenRect )
	{

		var controlSize = control.Size;
		var padding = control.GetClipPadding();
		var horzPadding = Mathf.Min( Mathf.Max( 0, Mathf.Min( controlSize.x, padding.horizontal ) ), controlSize.x );
		var vertPadding = Mathf.Min( Mathf.Max( 0, Mathf.Min( controlSize.y, padding.vertical ) ), controlSize.y );

		this.clipInfo = new ClipRegionInfo();
		this.clipInfo.Size = Vector2.Max( new Vector2( controlSize.x - horzPadding, controlSize.y - vertPadding ), Vector3.zero );
		this.clipInfo.Offset = new Vector3( padding.left - padding.right, -( padding.top - padding.bottom ) ) * 0.5f;

		clipRect = containerRect.IsEmpty() ? screenRect : containerRect.Intersection( screenRect );

	}

	private bool processRenderData( ref dfRenderData buffer, dfRenderData controlData, ref Bounds bounds, ref Rect screenRect, uint checksum, dfTriangleClippingRegion clipInfo, ref bool wasClipped )
	{

		wasClipped = false;

		// This shouldn't happen in practice, but need to make certain
		if( controlData == null || controlData.Material == null || !controlData.IsValid() )
			return false;

		// A new draw call is needed every time the current Material, Texture, or Shader
		// changes. If the control returned a buffer that is not empty and uses a 
		// different Material, need to grab a new draw call buffer from the object pool.
		bool needNewDrawcall = false;
		if( buffer == null )
		{
			needNewDrawcall = true;
		}
		else
		{
			if( !Material.Equals( controlData.Material, buffer.Material ) )
			{
				needNewDrawcall = true;
			}
			else if( !textureEqual( controlData.Material.mainTexture, buffer.Material.mainTexture ) )
			{
				needNewDrawcall = true;
			}
			else if( !shaderEqual( buffer.Shader, controlData.Shader ) )
			{
				needNewDrawcall = true;
			}
			else if( !this.clipInfo.IsEmpty && drawCallBuffers.Count == 1 )
			{
				needNewDrawcall = true;
			}
		}

		if( needNewDrawcall )
		{
			buffer = getDrawCallBuffer( controlData.Material );
			buffer.Material = controlData.Material;
			buffer.Material.mainTexture = controlData.Material.mainTexture;
			buffer.Material.shader = controlData.Shader ?? controlData.Material.shader;
		}

		if( !Application.isPlaying || clipType == dfClippingMethod.Software )
		{
				
			// Ensure that the control's render data is properly clipped to the 
			// current clipping region
			if( clipInfo.PerformClipping( buffer, ref bounds, checksum, controlData ) )
			{
				return true;
			}

			// If PerformClipping() returns FALSE, then the control was outside of
			// the active clipping region
			wasClipped = true;

		}
		else
		{
			if( clipRect.IsEmpty() || screenRect.Intersects( clipRect ) )
			{
				buffer.Merge( controlData );
			}
			else
			{
				// Control was not inside of the active clipping rectangle
				wasClipped = true;
			}
		}

		return false;

	}

	private dfRenderData compileMasterBuffer()
	{

		submeshes.Clear();
		masterBuffer.Clear();

		var buffers = drawCallBuffers.Items;

		var masterBufferSize = 0;

		for( int i = 0; i < drawCallCount; i++ )
		{
			masterBufferSize += buffers[ i ].Vertices.Count;
		}

		masterBuffer.EnsureCapacity( masterBufferSize );

		for( int i = 0; i < drawCallCount; i++ )
		{

			submeshes.Add( masterBuffer.Triangles.Count );

			var buffer = buffers[ i ];
			masterBuffer.Merge( buffer, false );

		}

		// Translate the "world" coordinates in the buffer back into local 
		// coordinates relative to this GUIManager. This allows the GUIManager to be 
		// positioned anywhere in the scene without being distracting
		masterBuffer.ApplyTransform( transform.worldToLocalMatrix );

		return masterBuffer;

	}

	private bool isEmptyBuffer( dfRenderData buffer )
	{
		return buffer.Vertices.Count == 0;
	}

	private int getMaterialCount()
	{

		var materialCount = 0;

		for( int i = 0; i < drawCallCount; i++ )
		{
			if( drawCallBuffers[ i ] != null && drawCallBuffers[ i ].Material != null )
				materialCount += 1;
		}

		return materialCount;

	}

	private void resetDrawCalls()
	{

		drawCallCount = 0;

		for( int i = 0; i < drawCallBuffers.Count; i++ )
		{
			// Release the draw call buffer back to the RenderData pool
			drawCallBuffers[ i ].Release();
		}

		drawCallBuffers.Clear();

	}

	private dfRenderData getDrawCallBuffer( Material material )
	{

		var buffer = dfRenderData.Obtain();
		buffer.Material = material;

		drawCallBuffers.Add( buffer );
		drawCallCount++;

		return buffer;

	}

	private Rect getControlOccluder( ref Rect screenRect, dfControl control )
	{

		// Do not prevent "click through" on non-interactive controls
		if( !control.IsInteractive )
			return new Rect();

		var hotZoneSize = new Vector2(
			screenRect.width * control.HotZoneScale.x,
			screenRect.height * control.HotZoneScale.y
		);

		var difference = new Vector2(
			hotZoneSize.x - screenRect.width,
			hotZoneSize.y - screenRect.height
		) * 0.5f;

		return new Rect(
			screenRect.x - difference.x,
			screenRect.y - difference.y,
			hotZoneSize.x,
			hotZoneSize.y
		);

	}

	private bool textureEqual( Texture lhs, Texture rhs )
	{
		return Texture2D.Equals( lhs, rhs );
	}

	private bool shaderEqual( Shader lhs, Shader rhs )
	{

		if( lhs == null || rhs == null )
			return object.ReferenceEquals( lhs, rhs );

		return lhs.name.Equals( rhs.name );

	}

	private void initialize()
	{

		meshRenderer = GetComponent<MeshRenderer>();
		if( meshRenderer == null )
		{
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
		}
		meshRenderer.hideFlags = HideFlags.HideInInspector;

		renderFilter = GetComponent<MeshFilter>();
		if( renderFilter == null )
		{
			renderFilter = gameObject.AddComponent<MeshFilter>();
		}
		renderFilter.hideFlags = HideFlags.HideInInspector;

		renderMesh = new Mesh() { hideFlags = HideFlags.DontSave };
		renderMesh.MarkDynamic();

		renderFilter.sharedMesh = renderMesh;

	}

	#endregion 

	#region Nested classes

	private struct ClipRegionInfo
	{

		public Vector2 Offset;
		public Vector2 Size;

		public bool IsEmpty
		{
			get { return Offset == Vector2.zero && Size == Vector2.zero; }
		}

	}

	#endregion 

}
