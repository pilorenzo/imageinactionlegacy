using Godot;
using System;

public partial class PopUp
{

    public static Vector2I DefaultSize => new (500,80);


	public static void Make(
        Node caller, bool hasCancel, string dialogText, Vector2I size,
        Action<Variant[]> action = null, Variant[] inputs = null
    )
    {
        var popup = hasCancel? new ConfirmationDialog() : new AcceptDialog();
        popup.Size = size;
        popup.InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen;
        popup.DialogText = dialogText;
        popup.Visible = true;

        popup.Canceled += popup.QueueFree;
        popup.Confirmed += () => {
            action?.Invoke(inputs);
            popup.QueueFree();
        };
        caller.AddChild(popup);
    }


    public static void Error(string text)
    {
        var root = (Engine.GetMainLoop() as SceneTree).Root;
        Make(root, false, text, DefaultSize);
    }
}
