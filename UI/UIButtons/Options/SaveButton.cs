using Godot;
using System;

public partial class SaveButton : Panel
{
    TextureButton _button;
	public override void _Ready()
	{
        _button = GetChild<TextureButton>(0);
        _button.ButtonUp += SaveRoutine; 
	}

    async void SaveRoutine()
    {
        var art = Owner.GetNode<LayerManager>("%Art");
        AppManager.SaveProject(art);
        _button.Disabled = true;
        Modulate = new Color(1,1,1,0.5f);
        await ToSignal(GetTree().CreateTimer(2.0), Timer.SignalName.Timeout);
        _button.Disabled = false;
        Modulate = Colors.White;

    }

}
