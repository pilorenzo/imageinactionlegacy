using System;
using Godot;
using static Godot.Mathf;


public partial class DrawingCamera : Camera2D
{

    [Export] float _moveSpeed = 20f;
    [Export] float _zoomSpeed = 5f;
    [Export] float _dezoomCorrector = 0.1f;
    [Export] float _rotSpeed = 20f;
    [Export] float _rotStep = 15f;
    [Export] float _rotLerpSpeed = 10f;
    Vector2 _movement = new();
    Vector2 _limitMin = new();
    Vector2 _limitMax = new();
    float _zoom = 0f;
    Vector2 _startZoom = Vector2.One;
    float _startRot = 0f;
    float _newRot = 0f;
    float _targetRot = 0f;

    // Check if Sel is null before use
    SelectionScript Sel => ProjectManager.CurrentTool switch{
        "Lasso" or "RectSel" => GetNode<SelectionScript>($"%{ProjectManager.CurrentTool}"),
        _ => null
    };
 
    bool TouchAllowed =>
        Sel==null || !Sel.IsTouchingSelection(GetGlobalMousePosition());


    SpinBox _moveSpinBox;
    SpinBox _zoomSpinBox;
    SpinBox  _rotSpinBox;
    Button _centerCameraButton;




    public override void _Ready()
    {
        _moveSpinBox = GetNode<SpinBox>("%MoveSpinBox");
        _zoomSpinBox = GetNode<SpinBox>("%ZoomSpinBox");
        _rotSpinBox =  GetNode<SpinBox>("%RotSpinBox");
        _centerCameraButton =  GetNode<Button>("%CenterCameraButton");
        _centerCameraButton.ButtonUp += CenterCamera;

        void CenterCamera()
        {
            (int W, int H) = AppManager.Project.Size;
            Position = new Vector2(W,H)/2f;
            _startZoom = Zoom = AnimationButton.SetViewScale();
            Rotation = 0f;
            _targetRot = 0f;
        }

        CenterCamera();

    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if(MultiTouch.IsMobile)
        {
            _movement = -MultiTouch.Speed;
            _zoom = MultiTouch.Zoom;
            _newRot = MultiTouch.Rotation;
            return;
        }


        _movement = 10f*Input.GetVector("Move_Left", "Move_Right", "Move_Up", "Move_Down");
        _zoom = Input.GetAxis("Zoom_Out", "Zoom_In");
        _newRot = -Input.GetAxis("Rotate_CCW", "Rotate_CW");

    }



    public override void _Process(double delta)
    {

        if(!TouchAllowed) return;
        float fdelta = (float)delta;
        
        var moveSpeed = _moveSpeed * (float)_moveSpinBox.Value/10f;
        if(!IsZeroApprox(moveSpeed))
            Position += _movement.Rotated(Rotation)*0.1f*moveSpeed/Zoom;

        if(MultiTouch.Count != 2U && MultiTouch.IsMobile)
        {
            _startZoom = Zoom;
        }
        else if(MultiTouch.IsMobile)
        {
            var zoom = _zoom > 0f? _zoom : _dezoomCorrector*_zoom;
            Zoom = Vector2.One*zoom *_zoomSpeed*_startZoom + _startZoom;
            Zoom = Zoom.Clamp(Vector2.One*0.1f, Vector2.One*10f);
        }
        else
        {
            Zoom = Zoom.Lerp(Zoom + Vector2.One * _zoomSpeed*50f * _zoom, (float)delta);
            Zoom = Zoom.Clamp(Vector2.One*0.1f, Vector2.One*10f);
        }


        if(!MultiTouch.IsMobile)
        {
            Rotation = Lerp(Rotation, Rotation+_newRot, fdelta*_rotLerpSpeed);
            return;
        }
        float rot = _newRot * _rotSpeed * 0.1f * (float)_rotSpinBox.Value/10f;
        rot = DegToRad(_rotStep*Round(RadToDeg(rot)/_rotStep));
        if(rot != 0f || Rotation-_targetRot > Epsilon )
        {
            _targetRot = rot + _startRot;
            Rotation = Lerp(Rotation, _targetRot, fdelta*_rotLerpSpeed);
        }
        else 
            _startRot = Rotation; 

    }

}

