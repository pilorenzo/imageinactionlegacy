using Godot;
using System;
using System.Collections.Generic;

public partial class UndoRedoScript : Node, IUndoRedo
{

    private readonly Stack<Node> _undoActionsParents = new();
    private readonly Stack<Node> _redoActionsParents = new();
    private Node _redoableNode;

    bool IsThisLayer => Owner.GetIndex() == LayerManager.LayerIndex;
    Node ActionList => LayerManager.ActiveSprite.GetChild(0);

    public static bool Recorded {get; private set;} = true;
    public static void StartRecord() => Recorded = false;

    Node _main;

    public override void _Ready()
    {
        _main = GetTree().Root.GetChild(0);
        _redoableNode = GetChild(0);
        (_main.FindChild("*Bucket") as Bucket).BucketEnded += RegisterAction;
        (_main.FindChild("*Drawer") as BasicDrawer).CopiesCreated += RegisterAction;
        (_main.FindChild("*Eraser") as BasicDrawer).CopiesCreated += RegisterAction;
        (_main.FindChild("*Lasso") as SelectionScript).SelectionEnded += RegisterAction;
        (_main.FindChild("*RectSel") as SelectionScript).SelectionEnded += RegisterAction;
    }


    public void RegisterAction(Node action)
    {
        if (action == null || !IsThisLayer) return;
        _undoActionsParents.Push(action);
        _redoActionsParents.Clear();
        foreach (Node node in _redoableNode.GetChildren())
            node.QueueFree();
        Recorded = true;

    }


    // Keyboard input, but Undo() and Redo() can be called also via button
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("Undo"))
        {
            Undo();
        }
        else if (@event.IsActionPressed("Redo"))
        {
            Redo();
        }
    }



    public void Undo()
    {
        if(!Recorded) return;
        if (_undoActionsParents.Count > 0 && IsThisLayer)
        {
            (_undoActionsParents.Peek() as IUndoRedo).Undo();
            _redoActionsParents.Push(_undoActionsParents.Pop());

            ActionList.GetChild(-1).Reparent(_redoableNode);
        }
    }

    public void Redo()
    {
        if(!Recorded) return;
        if (_redoActionsParents.Count > 0 && IsThisLayer)
        {
            _undoActionsParents.Push(_redoActionsParents.Pop());

            _redoableNode.GetChild(-1).Reparent(ActionList);

            (_undoActionsParents.Peek() as IUndoRedo).Redo();
        }
    }


    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if(_main?.FindChild("*Bucket") is Bucket b)
                b.BucketEnded -= RegisterAction;

            if(_main?.FindChild("*Drawer") is BasicDrawer d)
                d.CopiesCreated -= RegisterAction;

            if(_main?.FindChild("*Eraser") is BasicDrawer e)
                e.CopiesCreated -= RegisterAction;

            if(_main?.FindChild("*Lasso") is SelectionScript l)
                l.SelectionEnded -= RegisterAction;

            if(_main?.FindChild("*RectSel") is SelectionScript r)
                r.SelectionEnded -= RegisterAction;
        }

    }
}
