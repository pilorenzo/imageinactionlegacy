using Godot;
using System;

public partial class MyPicker : ColorPickerButton
{
    public override void _Ready()
    {
        ChildEnteredTree += (child) => {
            HexEditor.Visible = false;
            SwatchButton.Visible = false;
        };

        ButtonUp += async () => {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            HexEditor.GetChild<LineEdit>(-1, true).ReleaseFocus();
        };

    }


    Control HexEditor =>
             GetChild(0, includeInternal: true)
            .GetChild(1, includeInternal: true)
            .GetChild(0, includeInternal: true)
            .GetChild(0, includeInternal: true)
            .GetChild(4, includeInternal: true)
            .GetChild<Control>(1, includeInternal: true); 

    Control SwatchButton =>
             GetChild(0, includeInternal: true)
            .GetChild(1, includeInternal: true)
            .GetChild(0, includeInternal: true)
            .GetChild(0, includeInternal: true)
            .GetChild<Control>(5, includeInternal: true);

}
