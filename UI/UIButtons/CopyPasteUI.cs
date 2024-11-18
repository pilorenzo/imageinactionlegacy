using Godot;
using System;


public partial class CopyPasteUI : Panel
{

    SelectionScript CurrentSelector =>
        Owner.GetNode<SelectionScript>($"%{ProjectManager.CurrentTool}"); 


    TextureButton Btn(ushort i) => GetChild(i).GetChild<TextureButton>(0);
    const ushort copy = 0, paste = 1, overwritten = 1;


    ToolsActionRecorder _record = new (2, "RectSel");
 
    public override void _Ready()
    {
        DisableCopy();
        DisablePaste();
        Btn(copy).ButtonUp += OnCopyClicked;
        Btn(paste).ButtonUp += OnPasteClicked;

        Owner.GetNode<SelectionScript>("%Lasso").SectionSelected += EnableCopy;
        Owner.GetNode<SelectionScript>("%RectSel").SectionSelected += EnableCopy;
        
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Btn(copy).Disabled && !Btn(copy).ButtonPressed &&
            Input.IsMouseButtonPressed(MouseButton.Left))
        {
            DisableCopy();
        }
    }

    void OnCopyClicked()
    {
        _record = CurrentSelector.CopySelection();
        DisableCopy();
        EnablePaste();

    }

    void OnPasteClicked()
    {
        GetOwner<ProjectManager>()
            .EmitSignal(ProjectManager.SignalName.ToolButton, "RectSel");
            
        if(CurrentSelector is RectangleSelScript rectSel)
            rectSel.PasteSelection(_record);

        DisablePaste();
        
    }

    void ChangeButtonStatus(ushort btn, bool enabled)
    {
        Btn(btn).Disabled = !enabled;
        Btn(btn).GetParent<Control>()
            .Modulate = enabled? Colors.White: new Color(1,1,1,.5f);

    }

    void EnablePaste() => ChangeButtonStatus(paste, true);
    void DisablePaste() => ChangeButtonStatus(paste, false);
    void EnableCopy() => ChangeButtonStatus(copy, true);
    void DisableCopy() => ChangeButtonStatus(copy, false);



    public override void _Notification(int what)
    {
        if(what == NotificationPredelete)
        {
            var owner = GetOwnerOrNull<Node>();
            if(owner != null)
            {
                var lasso = GetNodeOrNull<SelectionScript>("%Lasso");
                var rect = GetNodeOrNull<SelectionScript>("%RectSel");
                if(lasso != null) lasso.SectionSelected -= EnableCopy;
                if(rect != null)   rect.SectionSelected -= EnableCopy;
            }
        }
    }

}
