using Godot;
using System;

public partial class ExitButton : Panel
{

    const string MainMenuPath = "res://main_menu.tscn";
    const string ExitText =
        "Are you sure you want to exit? All unsaved changes will be lost.";


	public override void _Ready()
	{
        GetChild<TextureButton>(0).ButtonUp += () =>
        {
            PopUp.Make(
                this, true, ExitText, new Vector2I(500,80),
                ChangeScene, new Variant [] {MainMenuPath}
            );
        };
	}

    Action<Variant[]> ChangeScene => (str) => {
        GetTree().ChangeSceneToFile((string)str[0]);
    };

}
