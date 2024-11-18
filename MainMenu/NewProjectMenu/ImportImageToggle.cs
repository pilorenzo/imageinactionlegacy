using Godot;
using System;


public partial class ImportImageToggle : CheckButton
{
    [Export] Control _sizeLabel;
    [Export] Control _sizeContainer;
    [Export] Control _filePathLabel;
    [Export] Control _filePathContainer;


	public override void _Ready()
	{
        ButtonUp += () => {
            _sizeLabel.Visible = _sizeContainer.Visible = !ButtonPressed;
            _filePathLabel.Visible = _filePathContainer.Visible = ButtonPressed;
        };
	}

}
