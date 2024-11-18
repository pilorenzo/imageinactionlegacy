using Godot;
using System;

public partial class LayerSelector : TextureButton
{

    static LayerManager _lMan;

    static CanvasLayer _options;

    Vector2[] _pressedPos = new Vector2[2] {Vector2.Zero, Vector2.Zero};

    bool _isMousePressed = false;
    double _pressureTime = 0.0;
    int FrameIndex => GetParent()?.GetIndex() ?? -1;



	public override void _Ready()
	{
        var manager = GetTree().Root.GetChild<ProjectManager>(0);
        _lMan = manager.GetChild<LayerManager>(1);
        _options = manager.GetNode<CanvasLayer>("%LayerOptionCanvas");

        ButtonDown += () => {
            _pressedPos = new Vector2[2] {GlobalPosition, GetGlobalMousePosition()} ;
            _options.Visible = false;
        };

        ButtonUp += () => {
            if(GlobalPosition == _pressedPos[0] && GetGlobalMousePosition() == _pressedPos[1]) 
            {
                if(ProjectManager.InSelection) return;
                _lMan.SetLayerIndex(FrameIndex);
            }
        };
        _lMan.LayerSelected += ChangeBorder;

        Owner.GetParent().GetParent()
            .GetChild<HScrollBar>(1,true)
            .ValueChanged += (i) => _pressureTime = 0.0;

        var bg = GetNode<ColorRect>("%ColorRect");
        bg.Color = manager.BGColor;
        manager.BGColorChanged += (c) => bg.Color = c;

	}


    void ChangeBorder(int i)
    {
        var border = GetNode<ReferenceRect>("%ReferenceRect");
        bool isThis = i == FrameIndex;
        border.BorderColor = isThis? Colors.DarkGreen : Colors.Black;
        border.BorderWidth = isThis? 8f : 4f;
    }

    public override void _Process(double delta)
    {
        if(ProjectManager.InSelection) return;
        if(GetParent().IsQueuedForDeletion()) return;
        _isMousePressed = Input.IsMouseButtonPressed(MouseButton.Left);
        bool alreadyVisible = _options.Visible && LayerManager.LayerIndex == FrameIndex;

        if(!_isMousePressed || !Scroller.IsMouseInControlArea(this) || alreadyVisible)
        {
            _pressureTime = 0.0;
            return;
        }

        _pressureTime += delta;
        if(_pressureTime < 1) return;

        _pressureTime = 0.0;
        _lMan.SetLayerIndex(FrameIndex);

        if(_options.Visible) return;
        _options.Visible = true;

    }


    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if(_lMan != null)
            {
                _lMan.LayerSelected -= ChangeBorder;
            }

            base._Notification(what);
        }

    }

}
