/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Object = UnityEngine.Object;

[CanEditMultipleObjects]
[CustomEditor( typeof( dfScrollPanel ) )]
public class dfScrollPanelInspector : dfControlInspector
{

	private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

	protected override bool OnCustomInspector()
	{

		dfEditorUtil.DrawSeparator();

		if( !isFoldoutExpanded( foldouts, "Scroll Container Properties", true ) )
			return false;

		var control = target as dfScrollPanel;

		dfEditorUtil.LabelWidth = 110f;

		using( dfEditorUtil.BeginGroup( "Appearance" ) )
		{

			SelectTextureAtlas( "Atlas", control, "Atlas", false, true );
			if( control.GUIManager != null && !dfAtlas.Equals( control.Atlas, control.GUIManager.DefaultAtlas ) )
			{
				EditorGUILayout.HelpBox( "This control does not use the same Texture Atlas as the View, which will result in an additional draw call.", MessageType.Info );
			}

			SelectSprite( "Background", control.Atlas, control, "BackgroundSprite", false );

			var backgroundColor = EditorGUILayout.ColorField( "Back Color", control.BackgroundColor );
			if( backgroundColor != control.BackgroundColor )
			{
				dfEditorUtil.MarkUndo( control, "Change background color" );
				control.BackgroundColor = backgroundColor;
			}

			var scrollPadding = dfEditorUtil.EditPadding( "Padding", control.ScrollPadding );
			if( !RectOffset.Equals( scrollPadding, control.ScrollPadding ) )
			{
				dfEditorUtil.MarkUndo( control, "Change Layout Padding" );
				control.ScrollPadding = scrollPadding;
			}

		}

		using( dfEditorUtil.BeginGroup( "Layout" ) )
		{

			var scrollOffset = dfEditorUtil.EditInt2( "Scroll Pos.", "X", "Y", control.ScrollPosition );
			if( scrollOffset != control.ScrollPosition )
			{
				dfEditorUtil.MarkUndo( control, "Change Scroll Position" );
				control.ScrollPosition = scrollOffset;
			}

			// TODO: Review dfScrollPanel.Reset() - Why doesn't it reset scroll position when AutoLayout enabled? Seems intentional, but why?
			if( !control.AutoLayout )
			{

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space( dfEditorUtil.LabelWidth + 10 );
					if( GUILayout.Button( "Reset", "minibutton", GUILayout.Width( 100 ) ) )
					{
						control.Reset();
					}
					GUILayout.FlexibleSpace();
				}
				GUILayout.EndHorizontal();

			}

			var autoReset = EditorGUILayout.Toggle( "Auto Reset", control.AutoReset );
			if( autoReset != control.AutoReset )
			{
				dfEditorUtil.MarkUndo( control, "Change Auto Reset Property" );
				control.AutoReset = autoReset;
			}

			var autoLayout = EditorGUILayout.Toggle( "Auto Layout", control.AutoLayout );
			if( autoLayout != control.AutoLayout )
			{
				dfEditorUtil.MarkUndo( control, "Change Auto Layout Property" );
				control.AutoLayout = autoLayout;
			}

			if( autoLayout )
			{

				var wrap = EditorGUILayout.Toggle( "Wrap Layout", control.WrapLayout );
				if( wrap != control.WrapLayout )
				{
					dfEditorUtil.MarkUndo( control, "Change Wrap Layout Property" );
					control.WrapLayout = wrap;
				}

				var flowDirection = (dfScrollPanel.LayoutDirection)EditorGUILayout.EnumPopup( "Flow Direction", control.FlowDirection );
				if( flowDirection != control.FlowDirection )
				{
					dfEditorUtil.MarkUndo( control, "Change Flow Direction Property" );
					control.FlowDirection = flowDirection;
				}

				var flowPadding = dfEditorUtil.EditPadding( "Flow Padding", control.FlowPadding );
				if( !RectOffset.Equals( flowPadding, control.FlowPadding ) )
				{
					dfEditorUtil.MarkUndo( control, "Change Layout Padding" );
					control.FlowPadding = flowPadding;
				}

			}

		}

		using( dfEditorUtil.BeginGroup( "Scrolling" ) )
		{
			var useVirtualScrolling = EditorGUILayout.Toggle( "Virtual Scrolling", control.UseVirtualScrolling );
			if ( useVirtualScrolling != control.UseVirtualScrolling )
			{
				dfEditorUtil.MarkUndo( control, "Toggle Virtual Scrolling" );
				control.UseVirtualScrolling = useVirtualScrolling;
			}

			if ( useVirtualScrolling )
			{
				var virtualScrollingTile = (dfPanel) EditorGUILayout.ObjectField( "Virtual Tile", control.VirtualScrollingTile, typeof ( dfPanel ), true );
				if ( virtualScrollingTile != control.VirtualScrollingTile )
				{
					dfEditorUtil.MarkUndo( control, "Set Virtual Tile" );
					control.VirtualScrollingTile = virtualScrollingTile;
				}

				if ( virtualScrollingTile != null )
				{
					var inter = virtualScrollingTile.GetComponents<MonoBehaviour>()
													.FirstOrDefault( p => p is IDFVirtualScrollingTile );
					if ( !inter )
					{
						EditorGUILayout.HelpBox( "The tile you've chosen does not implement IDFVirtualScrollingTile!", MessageType.Error );
					}

				}

				var autoFitVirtualTiles = EditorGUILayout.Toggle( "Auto Fit Tile", control.AutoFitVirtualTiles );
				if ( autoFitVirtualTiles != control.AutoFitVirtualTiles )
				{
					dfEditorUtil.MarkUndo( control, "Set Auto-fit Tile" );
					control.AutoFitVirtualTiles = autoFitVirtualTiles;
				}
			}

			var scrollWithKeys = EditorGUILayout.Toggle( "Use Arrow Keys", control.ScrollWithArrowKeys );
			if( scrollWithKeys != control.ScrollWithArrowKeys )
			{
				dfEditorUtil.MarkUndo( control, "Toggle Arrow Keys" );
				control.ScrollWithArrowKeys = scrollWithKeys;
			}

			var useMomentum = EditorGUILayout.Toggle( "Add Momentum", control.UseScrollMomentum );
			if( useMomentum != control.UseScrollMomentum )
			{
				dfEditorUtil.MarkUndo( control, "Toggle Momentum" );
				control.UseScrollMomentum = useMomentum;
			}

			var wheelDir = (dfControlOrientation)EditorGUILayout.EnumPopup( "Wheel Dir", control.WheelScrollDirection );
			if( wheelDir != control.WheelScrollDirection )
			{
				dfEditorUtil.MarkUndo( control, "Set Wheel Scroll Direction" );
				control.WheelScrollDirection = wheelDir;
			}

			var wheelAmount = EditorGUILayout.IntField( "Wheel Amount", control.ScrollWheelAmount );
			if( wheelAmount != control.ScrollWheelAmount )
			{
				dfEditorUtil.MarkUndo( control, "Set Wheel Scroll Amount" );
				control.ScrollWheelAmount = wheelAmount;
			}

			var horzScroll = (dfScrollbar)EditorGUILayout.ObjectField( "Horz. Scrollbar", control.HorzScrollbar, typeof( dfScrollbar ), true );
			if( horzScroll != control.HorzScrollbar )
			{
				dfEditorUtil.MarkUndo( control, "Set Horizontal Scrollbar" );
				control.HorzScrollbar = horzScroll;
			}

			var vertScroll = (dfScrollbar)EditorGUILayout.ObjectField( "Vert. Scrollbar", control.VertScrollbar, typeof( dfScrollbar ), true );
			if( vertScroll != control.VertScrollbar )
			{
				dfEditorUtil.MarkUndo( control, "Set Vertical Scrollbar" );
				control.VertScrollbar = vertScroll;
			}

			if( wheelDir == dfControlOrientation.Horizontal && horzScroll == null )
			{
				EditorGUILayout.HelpBox( "Wheel Scroll Direction is set to Horizontal but there is no Horizontal Scrollbar assigned", MessageType.Info );
			}
			else if( wheelDir == dfControlOrientation.Vertical && vertScroll == null )
			{
				EditorGUILayout.HelpBox( "Wheel Scroll Direction is set to Vertical but there is no Vertical Scrollbar assigned", MessageType.Info );
			}

		}

		return true;

	}

	protected override void FillContextMenu( List<ContextMenuItem> menu )
	{

		if( Selection.gameObjects.Length == 1 )
		{

			menu.Add( new ContextMenuItem()
			{
				MenuText = "Fit to contents",
				Handler = ( control ) =>
				{

					dfEditorUtil.MarkUndo( control, "Fit to contents" );

					var panel = control as dfScrollPanel;
					panel.FitToContents();

				}
			} );

			menu.Add( new ContextMenuItem()
			{
				MenuText = "Center child controls",
				Handler = ( control ) =>
				{

					dfEditorUtil.MarkUndo( control, "Center child controls" );

					var panel = control as dfScrollPanel;
					panel.CenterChildControls();

				}
			} );

		}

		base.FillContextMenu( menu );

	}

}
