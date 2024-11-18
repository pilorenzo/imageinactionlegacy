using Godot;
using System;

public partial class LicenseExitButton : Button
{
    [Export] Control _buttonControl;
    [Export] RichTextLabel _textLabel;


	public override void _Ready()
	{
        ButtonUp += () =>
        {
            if(_textLabel.Visible)
            {
                _textLabel.Visible = false;
                _buttonControl.Visible = true;
            }
            else
            {
                GetOwner<Control>().Visible = false;
            }
        };
	}

}
