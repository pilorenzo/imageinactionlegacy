using Godot;
using System;

public partial class OptionButton : Node
{

    Panel SavePanel => GetNode<Panel>("../SavePanel");
    Panel ExitPanel => GetNode<Panel>("../ExitPanel");
    Panel AnimationPanel => GetNode<Panel>("../AnimationPanel");
    Panel TouchPanel => GetNode<Panel>("../TouchPanel");


	public override void _Ready()
	{
        var optionButton = GetChild<TextureButton>(0);
        optionButton.ButtonUp += () => {
            SavePanel.Visible = !SavePanel.Visible;
            ExitPanel.Visible = !ExitPanel.Visible;
            AnimationPanel.Visible = !AnimationPanel.Visible;
            TouchPanel.Visible = !TouchPanel.Visible;
            optionButton.Modulate = optionButton.Modulate.R == 1f
                ? Colors.DimGray
                : Colors.White; 
        };
	}

}
