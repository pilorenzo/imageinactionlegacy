using Godot;
using System;

public partial class LicenseButton : Button
{
    [Export] LicenseMenu _licenseMenu;

	public override void _Ready()
	{
        ButtonUp += () => _licenseMenu.Visible = true;
	}
}
