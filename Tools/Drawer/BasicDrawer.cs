using Godot;
using System;
using System.Threading.Tasks;
using static System.MathF;


public partial class BasicDrawer : Node2D, IUndoRedo
{
    [Signal] public delegate void CopiesCreatedEventHandler(Node action);

    [Export] public DrawerSettings Settings {get;set;}
    Line2D _childLine = new();
    ProjectManager Main => GetTree().Root.GetChild<ProjectManager>(0);
    ColorPickerButton Picker => GetNode<ColorPickerButton>("%ColorPickerButton");
    public BasicAddImage Sprite => LayerManager.ActiveSprite;
    Node ActionList => Sprite.GetChild(0);
    const ushort before = 0, after = 1;
    Record _fullRecord;

    SpriteUpdater Updater => Sprite.Updater;

    bool _isMousePressed = false;
    bool _isMouseMotion = false;
    double _pressureTime = 0.0;
    
    // a rectangle that contains the line to create the image
    readonly float[] _border = new float[4];

    readonly float[,] _bezier = new float [6,4]{
        {0.6297376f,  0.31486884f, 0.05247814f, 0.00291545f},
        {0.3644315f,  0.43731782f, 0.17492713f, 0.02332361f},
        {0.18658888f, 0.41982502f, 0.31486878f, 0.0787172f},
        {0.07871719f, 0.31486878f, 0.41982508f, 0.18658894f},
        {0.02332361f, 0.17492709f, 0.43731776f, 0.3644315f},
        {0.00291545f, 0.05247813f, 0.31486878f, 0.6297376f}
    };

    Vector2[] _pointBuffer = new Vector2[3];


    public override void _UnhandledInput(InputEvent @event)
    {
        // if using two fingers don't draw
        if (MultiTouch.Count > 1U) return; 
        if (!ProjectManager.CurrentTool.Equals(Settings.ResourceName)) return;
        if (!UndoRedoScript.Recorded) return;

        if (@event is InputEventScreenTouch ev)
        {
            _isMousePressed = ev.Pressed;
            if (_isMousePressed) CreateNewLine();
            else OnMouseReleased();
        }

        if (@event is InputEventScreenDrag && _isMousePressed)
        {
            _childLine.Visible = true;
            if(!Settings.IsPen)
            {
                var circle = GetChild<Sprite2D>(0);
                circle.GlobalPosition = GetGlobalMousePosition();
                float scaleValue = Settings.Width/256f;
                circle.Scale = Vector2.One*scaleValue;
                circle.Visible = true;
            }
            _pressureTime = 0.0;
            if(_childLine != null && IsInsideSprite())
            {
                AddPoints(6, _childLine);
                UpdateSquareBorders();

            }
        }

    }

    void AddPoints(int quantity, Line2D line)
    {
        var P1 = _pointBuffer[0];
        var P2 = _pointBuffer[1];
        var P3 = _pointBuffer[2];
        var P4 = GetGlobalMousePosition();
        if(line.GetPointCount() > 0)
        {
            var index = line.GetPointCount() - 1;
            var lastPoint = line.GetPointPosition(index);
            line.ClearPoints();
            line.AddPoint(lastPoint);
        }
        for (int i = 1; i <= quantity; i++)
        {
            Vector2 P =
                _bezier[i-1,0] * P1 +
                _bezier[i-1,1] * P2 +
                _bezier[i-1,2] * P3 +
                _bezier[i-1,3] * P4;

            if(quantity-i > 2)
                line.AddPoint(P);
            else
                _pointBuffer[2-(quantity-i)] = P;
        }
    }

    public override void _Process(double delta)
    {
        if(!(_isMousePressed && Settings.IsPen) || MultiTouch.Count > 1)
        {
            _pressureTime = 0.0;
            return;
        }

        _pressureTime += delta;
        if(_pressureTime < 1) return;

        _pressureTime = 0.0;   
        if(!IsInsideSprite()) return;

        var pos = GetGlobalMousePosition();
        var imgColor = Sprite.Img.GetPixel((int)pos.X, (int)pos.Y); 
        Picker.Color = Mathf.IsZeroApprox(imgColor.A)? Main.BGColor : imgColor;
    }


    public void CreateNewLine()
    {
        _fullRecord?.QueueFree();
        _fullRecord = new Record();
        _fullRecord.FromImage(before, Sprite.Img);

        GC.Collect(1);

        if(_childLine != null && !_childLine.IsQueuedForDeletion())
            _childLine.QueueFree();

        _childLine = new Line2D
        {
            Width = Settings.Width,
            DefaultColor = Settings.IsPen? Picker.Color: Main.BGColor,
            EndCapMode = Settings.EndCapMode,
            BeginCapMode = Settings.BeginCapMode,
            JointMode = Line2D.LineJointMode.Round,
            Visible = AppManager.IsManagerDebug,
        };
        if(!Settings.IsPen)
        {
            _childLine.Material = new CanvasItemMaterial()
            {
                BlendMode = CanvasItemMaterial.BlendModeEnum.Sub
            };
        }
        // else
        // { 
        //     _childLine.Texture = GD.Load<Texture2D>("res://Tools/Drawer/Textures/Texture.png");
        //     _childLine.TextureMode = Line2D.LineTextureMode.Stretch;
        //     _childLine.Material = new CanvasItemMaterial()
        //     {
        //         BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha
        //     };
        // }


        Updater.ChildSprite.AddChild(_childLine);
        
        _childLine.Visible = false;
        
        _border[0] = _border[1] = float.MaxValue;
        _border[2] = _border[3] = float.MinValue;

        _pointBuffer[0] = _pointBuffer[1] =
        _pointBuffer[2] = GetGlobalMousePosition();
        AddPoints(6, _childLine);
        UpdateSquareBorders();

    }
 

    public async void OnMouseReleased()
    {
        if(!Settings.IsPen) GetChild<Sprite2D>(0).Visible = false;
        _isMouseMotion = false;
        if(_childLine.Visible)
        { 
            _childLine.Visible = false;
            var recorder = new ToolsActionRecorder(2, "Drawer");
            ActionList.AddChild(recorder);
            UndoRedoScript.StartRecord();
            await Task.Run(async() => await RecordChanges(Sprite.Img, recorder));
        }
        if(_childLine.GetParent() == Updater.ChildSprite)
            Updater.ChildSprite.RemoveChild(_childLine);

    }

    async Task RecordChanges(Image spriteImg, ToolsActionRecorder recorder)
    {
        _fullRecord.FromImage(after, spriteImg);
        int startX = (int)Floor(_border[0]-Settings.Width);
        int startY = (int)Floor(_border[1]-Settings.Width);
        int width  = (int)Ceiling(_border[2]-_border[0]+ 2f*Settings.Width);
        int height = (int)Ceiling(_border[3]-_border[1]+ 2f*Settings.Width);
        if(width <= 0 || height <= 0) return;

        void SavePart(ushort index)
        {
            Image sectionImage = Image.CreateEmpty(
                width, height,
                false, Image.Format.Rgba8
            );
            sectionImage.BlitRect(
                _fullRecord.IText[index].GetImage(),
                new Rect2I(startX, startY, sectionImage.GetSize()),
                Vector2I.Zero
            );

            recorder.AddPart(
                index,
                image: sectionImage,
                start: new Vector2I(startX, startY),
                center: new Vector2I((startX+width)/2, (startY+height)/2)
            );
        }
        var bef = Task.Run(() => SavePart(before));
        var aft = Task.Run(() => SavePart(after));
        await Task.WhenAll(bef,aft);
        CallDeferred(MethodName.EmitSignal, SignalName.CopiesCreated, this);

    }



    bool IsInsideSprite()
    {
        Vector2 pos = GetGlobalMousePosition();
        (int X, int Y) = Sprite.Size;
        return
            pos.X >= 0 && pos.X < X &&
            pos.Y >= 0 && pos.Y < Y;
        
    }

    void UpdateSquareBorders()
    {
        Vector2 p = _childLine.Points[^1];
        _border[0] = Min(p.X, _border[0]);
        _border[1] = Min(p.Y, _border[1]);
        _border[2] = Max(p.X, _border[2]);
        _border[3] = Max(p.Y, _border[3]);

    }



    public void Undo() => ModifySourceSprite(before);
    public void Redo() => ModifySourceSprite(after);

    void ModifySourceSprite(ushort index)
    {
        var recorder = Sprite.GetChild(0).GetChild<ToolsActionRecorder>(-1);

        // if(AppManager.IsManagerDebug)
        // {
        //     string path = "ZZZ_test/test" + (index > 0 ? "after" : "before") + ".png";
        //     recorder.Parts[index].Image.SavePng(path);
        // }

        using Image newImage = Sprite.Img;
        newImage.BlitRect(
            recorder.Parts[index].Image,
            new Rect2I(Vector2I.Zero, recorder.Parts[index].Image.GetSize()),
            recorder.Parts[index].Start
        );
        Sprite.SetAndUpdate(ImageTexture.CreateFromImage(newImage));

    }

}
