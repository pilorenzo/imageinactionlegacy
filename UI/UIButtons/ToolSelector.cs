using Godot;
using System;


[Tool]
public partial class ToolSelector : Panel
{

    [Export] Texture2D _icon;
    [Export] StringName _toolName = "Pen"; 
    [Signal] public delegate void ToolButtonEventHandler(StringName tName);
    TextureButton TButton => GetChild<TextureButton>(0);
    ProjectManager Manager => GetOwner<ProjectManager>();

    void CallUndo() => Manager?.UndoRedo?.Undo();
    void CallRedo() => Manager?.UndoRedo?.Redo();
 
    void Highlight(bool b) => Modulate = b? Colors.DimGray : Colors.White; 

	public override void _Ready()
	{
        TButton.TextureNormal = _icon;
        if(Engine.IsEditorHint()) return;

        TButton.ButtonDown += ButtonPressed; // Called only for the button pressed
        TButton.ButtonUp += ButtonReleased;  // Called only for the button released
        Manager.ToolButton += ChangeHighlightedTool; // Called for all button simultaneously

	}


    public void ButtonPressed()
    {
        if(SelectionScript.Phase == SelectionScript.SelPhase.Selected)
            return;

        switch (_toolName)
        {
            case "Undo": Highlight(true); CallUndo(); break;
            case "Redo": Highlight(true); CallRedo(); break;
            default:
                Manager.EmitSignal(SignalName.ToolButton, _toolName);
                break;
        }

    }


    public async void ButtonReleased()
    {
        if(SelectionScript.Phase == SelectionScript.SelPhase.Selected)
            return;

        await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
        switch (_toolName)
        {
            case "Undo": Highlight(false); return;
            case "Redo": Highlight(false); return;
            default: return;
        }
                
    }


    void ChangeHighlightedTool(StringName currentTool)
    {
        Highlight(currentTool.Equals(_toolName));
    }




    public override string[] _GetConfigurationWarnings()
    {
        if(_icon is null)
        {
            return new string[] {"You must add an icon!!"};
        }
        return base._GetConfigurationWarnings();
    }
}
