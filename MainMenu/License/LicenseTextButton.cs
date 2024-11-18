using Godot;

public partial class LicenseTextButton : Button
{
    [Export] RichTextLabel _textLabel;
    [Export] string _licenseName;
    const string ResPath = "res://", Ext = ".md";

	public override void _Ready()
	{
        ButtonUp += () =>
        {
            _textLabel.Text = GetText(ResPath+_licenseName+Ext);
            _textLabel.Visible = true;
            GetParent().GetParent<Control>().Visible = false;
        };
	}

    static string GetText(string path) =>
        FileAccess.Open(path, FileAccess.ModeFlags.Read).GetAsText();
}
