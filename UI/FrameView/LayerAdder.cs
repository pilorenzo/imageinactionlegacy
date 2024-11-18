using Godot;
using System;

public partial class LayerAdder : TextureButton
{

    public LayerManager LManager => 
        GetTree().Root.GetChild(0).GetChild<LayerManager>(1);

	public override void _Ready()
	{
        Pressed += () => LManager.CreateLayer();
	}


}
