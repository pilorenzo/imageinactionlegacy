using Godot;
using System;

public partial class ColorChanger : ColorPicker
{

	[Export] private PanelContainer _panelContainer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetParent<Button>().Pressed += OpenColorPicker;
	}


	private void OpenColorPicker()
	{
		// Visible = !Visible;
		// if (!Visible)
		// {
		// 	GetNode<BasicDrawer_v3>("%Drawer").LineColor = Color;
		// 	_panelContainer.Size = GetParent<Button>().Size;
		// }
		// else
		// {
		// 	_panelContainer.Size = GetViewport().GetVisibleRect().Size;
		// }
	}




}
