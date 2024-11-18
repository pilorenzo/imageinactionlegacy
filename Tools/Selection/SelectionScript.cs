using Godot;
using System;
using System.Threading.Tasks;
using static System.MathF;


public partial class SelectionScript : Node2D, IUndoRedo
{
    [Signal] public delegate void SelectionEndedEventHandler(Node action);
    [Signal] public delegate void SectionSelectedEventHandler();

    public static Line2D CurrentLine { get; protected set; }
    [Export] protected Color lineColor = Colors.Gold;
    [Export] float _moveSpeed = 1f;
    [Export] float _zoomSpeed = 600f;
    [Export] float _rotSpeed = 50f;

    Border _border = new();
    Vector2I _currentPixel;


    protected BasicAddImage MainSprite => LayerManager.ActiveSprite;
    Node ActionList => MainSprite.GetChild(0);

    SpriteUpdater MainUpdater => MainSprite.Updater;

    public enum SelPhase { NotSelecting, Selecting, Selected }
    public static SelPhase Phase { get; protected set; } = SelPhase.NotSelecting;
    const ushort moved = 0, overwritten = 1, remained = 2;
    const ushort before = 0, after = 1;


    public Vector2 MousePos { get; protected set; }
    bool _mouseDrag = false;
    bool _mouseInImage = false;
    protected static ToolsActionRecorder SectionRecord { get; set; }
    protected bool isCopied = false;

    // Used when rotating selection
    Vector2 _rotationPivot = Vector2.Zero;
    float _initialRotation = 0f;
    float _initialZoom = 1f;

    public StringName ToolName => Name;



    public async override void _UnhandledInput(InputEvent @event)
    {
        if (!ProjectManager.CurrentTool.Equals(ToolName)) return;

        if(!UndoRedoScript.Recorded) return;

        bool drawPressed = @event.IsActionPressed("Finger1");
        bool drawReleased = @event.IsActionReleased("Finger1");

        switch (Phase)
        {
            case SelPhase.NotSelecting:
                if (MultiTouch.Count == 1U)
                {
                    Phase = SelPhase.Selecting;
                    CreateNewLine();
                    await RecordImage(before);
                }
                break;

            case SelPhase.Selecting:
                if (MultiTouch.Count == 2U)
                {
                    CurrentLine.Visible = false;
                    CurrentLine.ClearPoints();
                    ActionList.GetChildOrNull<Record>(-1)?.QueueFree();
                    Phase = SelPhase.NotSelecting;
                }
                else if (ProjectManager.CheckDrag(@event))
                {
                    ExtendSelector();
                }
                else if (drawReleased)
                {
                    bool recorded = await RecordSelection();
                    if (recorded) EmitSignal(SignalName.SectionSelected);
                    Phase = SelPhase.Selected;
                }
                break;

            case SelPhase.Selected:
                _mouseDrag = ProjectManager.CheckDrag(@event);
                _mouseInImage = GetSquaredBorder(true).IsInsideFromCenter(
                    GetGlobalMousePosition(), SectionRecord.GlobalPosition
                );

                if (drawPressed && !_mouseInImage)
                {
                    await ToSignal(GetTree().CreateTimer(0.3f), Timer.SignalName.Timeout);
                    if(MultiTouch.Count == 2U) return;

                    CurrentLine.Visible = false;
                    CurrentLine.ClearPoints();
                    CurrentLine.GetParent()?.RemoveChild(CurrentLine);
                    bool validSelection = RecordOverwritten();
                    if(!validSelection)
                    {
                        ActionList.GetChildOrNull<Record>(-1)?.QueueFree();
                        Phase = SelPhase.NotSelecting;
                        return;
                    }

                    var partUpdater = new Sprite2D()
                    {
                        Position = SectionRecord.Position,
                        Rotation = SectionRecord.Rotation,
                        Scale = SectionRecord.Scale,
                        Texture = ImageTexture.CreateFromImage(SectionRecord.Parts[moved].Image),
                    };
                    MainUpdater.ChildSprite.AddChild(partUpdater);
                    
                    SectionRecord.GetParent()?.RemoveChild(SectionRecord);
                    await ToSignal(MainUpdater, SpriteUpdater.SignalName.RenderingUpdated);
                    partUpdater?.QueueFree();
                    UndoRedoScript.StartRecord();
                    await RecordImage(after);
                    EmitSignal(SignalName.SelectionEnded, this);

                    Phase = SelPhase.NotSelecting;
                }
                break;

            default:
                Phase = SelPhase.NotSelecting;
                break;
        }

    }

    async Task RecordImage(ushort index)
    {
        var record = index == 0? new() : ActionList.GetChildOrNull<Record>(-1);
        if(record == null)
        {
            GD.PrintErr("Unable to found record in child -1");
            return;
        }
        if(index == 0) ActionList.AddChild(record);
        await Task.Run(() => record.FromImage(index, MainSprite.Texture.GetImage())); 
    }


    protected void CreateNewLine()
    {
        CurrentLine = new()
        {
            DefaultColor = lineColor,
            Width = 8,
            Closed = true,
            Visible = true,
        };
        if(CurrentLine.Owner != null)
            CurrentLine.Reparent(this);
        else
            AddChild(CurrentLine);
        GenerateSelector();

    }

    // manage selection movement, rotation and scale
    public override void _Process(double delta)
    {
        if (Phase != SelPhase.Selected) return;
        if (!CurrentLine.Visible) return;

        if(MultiTouch.IsMobile && MultiTouch.Count != 2U)
        {
            _initialRotation = SectionRecord.GlobalRotation;
            _initialZoom = SectionRecord.Scale.X;
        }

        bool isDraggingSelection = MultiTouch.IsMoving && _mouseInImage;
        if(!isDraggingSelection) 
        {
            MousePos = GetGlobalMousePosition();
            return;
        }

        float fdelta = (float)delta;
        float zoom;
        float angle;
        if(MultiTouch.IsMobile)
        {
            zoom  = -MultiTouch.Zoom*0.0001f;
            angle = -MultiTouch.Rotation;
            SectionRecord.GlobalRotation = _initialRotation + angle * _rotSpeed * fdelta;
        }
        else
        {
            zoom = Input.GetAxis("Zoom_In", "Zoom_Out");
        }

        float scaleValue = Max(-zoom*_zoomSpeed*fdelta+_initialZoom, 0.1f);
        SectionRecord.Scale = Vector2.One*scaleValue;
        SectionRecord.Position += GetGlobalMousePosition() - MousePos;
        MousePos = GetGlobalMousePosition();

    }

    async Task<bool> RecordSelection()
    {

        _border = GetSquaredBorder().Trim(AppManager.Project.Size);
        if (_border.Width <= 0 || _border.Height <= 0)
        {
            RemoveChild(CurrentLine);
            CurrentLine.ClearPoints();
            Phase = SelPhase.NotSelecting;
            return false;
        }

        using Image mainImage = MainSprite.Texture.GetImage();
        var selection = Image.CreateEmpty(
            (int)Ceiling(_border.Width),
            (int)Ceiling(_border.Height),
            false, Image.Format.Rgba8
        );
        var emptyImage = selection.Duplicate() as Image;

        bool anyPointInPolygon = false;
        await Task.Run(() => {
            for (int x = _border.MinX; x < _border.MaxX; x++)
            {
                for (int y = _border.MinY; y < _border.MaxY; y++)
                {
                    _currentPixel = new Vector2I(x, y);
                    (int pX, int pY) = (x - _border.MinX, y - _border.MinY);
                    if (IsPointInPolygon(_currentPixel, CurrentLine.Points))
                    {
                        anyPointInPolygon = true;
                        selection.SetPixel(pX, pY, mainImage.GetPixel(x, y));
                    }
                }
            }
        });
        if (!anyPointInPolygon)
        {
            RemoveChild(CurrentLine);
            CurrentLine.ClearPoints();
            Phase = SelPhase.NotSelecting;
            return false;
        }

        mainImage.BlitRectMask(
            emptyImage, selection,
            new Rect2I(Vector2I.Zero, emptyImage.GetSize()),
            new Vector2I(_border.MinX, _border.MinY)
        );
        UpdateTexture(mainImage);

        SectionRecord = new(3, ToolName)
        {
            Position = new Vector2(
                Ceiling((_border.MaxX + _border.MinX) / 2f),
                Ceiling((_border.MaxY + _border.MinY) / 2f)
            ),
            Texture = ImageTexture.CreateFromImage(selection),
        };


        var sectRecord = SectionRecord;
        await Task.Run(() => {
            sectRecord.AddPart(
                index: moved,
                image: selection,
                start: new Vector2I(_border.MinX, _border.MinY),
                center: (Vector2I)sectRecord.Position
            );

            sectRecord.AddPart(
                remained,
                emptyImage,
                start: sectRecord.Parts[moved].Start,
                center: sectRecord.Parts[moved].Center
            );
        });

        ActionList.AddChild(SectionRecord);
        CurrentLine.Reparent(SectionRecord);
        CurrentLine.Position = -SectionRecord.Position;
        CurrentLine.ZIndex = 1;
        isCopied = false;

        return true;

    }

    public ToolsActionRecorder CopySelection()
    {
        using var mainCopy = MainSprite.Texture.GetImage();
        mainCopy.BlitRect(
            SectionRecord.Parts[moved].Image,
            new Rect2I(Vector2I.Zero, SectionRecord.Parts[moved].Image.GetSize()),
            SectionRecord.Parts[moved].Start
        );
        UpdateTexture(mainCopy);

        var copyRecord = new ToolsActionRecorder(2, "RectSel");
        copyRecord.Parts[0].Image = SectionRecord.Parts[moved].Image.Duplicate() as Image;

        SectionRecord.GetParent()?.RemoveChild(SectionRecord);
        CurrentLine.ClearPoints();
        Phase = SelPhase.NotSelecting;

        return copyRecord;

    }

    bool RecordOverwritten()
    {
        var border = GetSquaredBorder(rotated: true).Trim(AppManager.Project.Size);

        _border = Border.GetEnvelopingBorder(_border, border)
                        .Scale(SectionRecord.Scale).Trim(AppManager.Project.Size);

        if(_border.Width <= 0 || _border.Height <= 0) return false;

        var sectionImage = Image.CreateEmpty(
            (int)_border.Width + 1, (int)_border.Height + 1,
            false, Image.Format.Rgba8
        );

        using (var mainCopy = MainSprite.Texture.GetImage())
        {
            sectionImage.BlitRect(
                mainCopy,
                new Rect2I(
                    (Vector2I)SectionRecord.Position - sectionImage.GetSize() / 2,
                    sectionImage.GetSize()
                ),
                Vector2I.Zero
            );
        }

        SectionRecord.AddPart(
            overwritten,
            image: sectionImage,
            start: (Vector2I)SectionRecord.Position - sectionImage.GetSize() / 2,
            center: (Vector2I)SectionRecord.Position
        );

        return true;

    }

    public void Undo() => ModifySourceSprite(before);
    public void Redo() => ModifySourceSprite(after);

    void ModifySourceSprite(ushort index)
    {
        var recorder = ActionList.GetChildOrNull<Record>(-1);
        if(recorder == null)
        {
            GD.PrintErr("recorder not found when undo or redo");
            return;
        }

        // if(AppManager.IsManagerDebug)
        // {
        //     string path = "ZZZ_test/test" + (index > 0 ? "after" : "before") + ".png";
        //     recorder.IText[index].GetImage().SavePng(path);
        // }

        MainSprite.SetAndUpdate(recorder.IText[index]);

    }


    Border GetSquaredBorder(bool rotated = false)
    {
        Border border = new();
        foreach (Vector2 q in CurrentLine.Points)
        {
            if (rotated == false) border.Update(q);
            else border.Update(RotatedLine(q));
        }
        return border;
    }



    Vector2 RotatedLine(Vector2 p)
    {
        /*
            from https://math.stackexchange.com/questions/3935956/calculate-the-new-position-of-a-point-after-rotating-it-around-another-point-2d
            (cos(θ)(x−α)−sin(θ)(y−β)+α, sin(θ)(x−α)+cos(θ)(y−β)+β)
        */

        float th = SectionRecord.Rotation;
        (float a, float b) = SectionRecord.Position;
        return new Vector2(
            Cos(th) * (p.X - a) - Sin(th) * (p.Y - b) + a,
            Sin(th) * (p.X - a) - Cos(th) * (p.Y - b) + b
        );

    }


    bool IsPointInPolygon(Vector2I p, Vector2[] polygon)
    {

        if (!_border.IsInside(p))
        {
            return false;
        }

        // https://wrfranklin.org/Research/Short_Notes/pnpoly.html
        /*
        Copyright (c) 1970-2003, Wm. Randolph Franklin

        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

            1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimers.
            2. Redistributions in binary form must reproduce the above copyright notice in the documentation and/or other materials provided with the distribution.
            3. The name of W. Randolph Franklin may not be used to endorse or promote products derived from this Software without specific prior written permission. 

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
        */
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) &&
               p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    public bool IsTouchingSelection(Vector2 point) // for touch inputs during selection
    {
        if (Phase != SelPhase.Selected || SectionRecord is null )
            return false;

        var border = GetSquaredBorder(rotated: true);

        // center of image
        float cX = SectionRecord.Position.X;
        float cY = SectionRecord.Position.Y;

        // offset from center to obtain min and max position
        float oX = border.Width / 2f * SectionRecord.Scale.X;
        float oY = border.Height / 2f * SectionRecord.Scale.Y;

        return  (point.X > cX - oX) && (point.X < cX + oX) &&
                (point.Y > cY - oY) && (point.Y < cY + oY);

    }

    protected virtual void GenerateSelector() { }

    protected virtual void ExtendSelector() { }


    ImageTexture imageTexture;
    public override void _Ready() =>
        imageTexture = ImageTexture.CreateFromImage(
            Image.CreateEmpty(
                AppManager.Project.Size.W,
                AppManager.Project.Size.H,
                false,
                Image.Format.Rgba8
            )
        );
    protected void UpdateTexture(Image image)
    {
        imageTexture.Update(image);
        MainSprite.SetAndUpdate(imageTexture);
    }


}



public class Border
{
    public int MinX { get; set; }
    public int MaxX { get; set; }
    public int MinY { get; set; }
    public int MaxY { get; set; }

    public int Width => MaxX - MinX;
    public int Height => MaxY - MinY;
    public Vector2 Size => new(Width, Height);

    public override string ToString() => $"[({MinX},{MinY}); ({MaxX},{MaxY})]";
    public Border(int minX = int.MaxValue, int maxX = 0, int minY = int.MaxValue, int maxY = 0)
    {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
    }


    public void Update(Vector2 point)
    {
        MinX = (int)Min(point.X, MinX);
        MaxX = (int)Max(point.X, MaxX);

        MinY = (int)Min(point.Y, MinY);
        MaxY = (int)Max(point.Y, MaxY);

    }

    public Border Trim((int, int) Size)
    {
        (int W, int H) = Size;
        MinX = (int)Max(0, MinX);
        MaxX = (int)Min(W, MaxX);
        MinY = (int)Max(0, MinY);
        MaxY = (int)Min(H, MaxY);

        return this;
    }

    public Border Scale(Vector2 scale)
    {
        MinX = (int)(MinX * scale.X);
        MaxX = (int)(MaxX * scale.X);
        MinY = (int)(MinY * scale.Y);
        MaxY = (int)(MaxY * scale.Y);

        return this;
    }

    // returns the minimum border that contains both border1 and border2
    public static Border GetEnvelopingBorder(Border border1, Border border2)
    {
        return new Border()
        {
            MinX = (int)Min(border1.MinX, border2.MinX),
            MaxX = (int)Max(border1.MaxX, border2.MaxX),
            MinY = (int)Min(border1.MinY, border2.MinY),
            MaxY = (int)Max(border1.MaxY, border2.MaxY),
        };
    }

    public bool IsInside(Vector2 p)
    {
        return p.X >= MinX && p.X <= MaxX &&
                p.Y >= MinY && p.Y <= MaxY;
    }

    public bool IsInsideFromCenter(Vector2 p, Vector2 center)
    {
        return Abs(p.X - center.X) <= Width / 2 &&
                Abs(p.Y - center.Y) <= Height / 2;

    }
}
