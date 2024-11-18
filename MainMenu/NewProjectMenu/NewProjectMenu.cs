using Godot;
using System;

namespace MainMenu;

public partial class NewProjectMenu : Control
{

    [Signal] public delegate void CanceledEventHandler();
    [Signal] public delegate void OkedEventHandler();

    public string ProjectName => GetNode<LineEdit>("%NameLine").Text;
    public Color ProjectBGColor => GetNode<MyPicker>("%Color").Color;

    public bool IsFromImage => GetNode<CheckButton>("%FromFileButton").ButtonPressed;
    public (int W, int H) ProjectSize =>
    (
        (int)GetNode<SpinBox>("%Width").Value,
        (int)GetNode<SpinBox>("%Height").Value
    );
    public string StartImagePath => GetNode<LineEdit>("%FilePathLine").Text;



	public override void _Ready()
	{
        var cancelButton = GetNode<Button>("%CancelButton");
        var okButton = GetNode<Button>("%OkButton");
        cancelButton.ButtonUp += () => EmitSignal(SignalName.Canceled);
        okButton.ButtonUp += () => EmitSignal(SignalName.Oked);

        var browseButton = GetNode<Button>("%BrowseButton");
        browseButton.ButtonUp += ChooseImage;

        VisibilityChanged += SelectNameLine;
	}


    void ChooseImage()
    {
        var fileDialog = GetChild<FileDialog>(1);
        fileDialog.CurrentPath = AppManager.StartPath;
        fileDialog.Visible = true;
        fileDialog.FileSelected += SavePath;
        void SavePath(string path)
        {
            var line = GetNode<LineEdit>("%FilePathLine"); 
            line.Text = path;
            fileDialog.FileSelected -= SavePath;
        }
    }


    void SelectNameLine()
    {
        if(!Visible) return;

        GetNode<LineEdit>("%NameLine").GrabFocus();
    }

}
