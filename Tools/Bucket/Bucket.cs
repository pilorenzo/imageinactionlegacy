using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


public partial class Bucket : Node2D, IUndoRedo
{

    readonly Queue<Vector4I> _bucketArray = new();
    readonly List<Rect2I> _drawLines = new();
    Image ImageToFill { get; set; }
    int _width;
    int _height;

    Color _oldColor;
    Color _replacementColor;

    Vector4I _pixel = new();

    Vector2I _mousePos;
    Rect2I _drawLine;

    BasicAddImage Sprite => LayerManager.ActiveSprite;
    Node ActionList => Sprite.GetChild(0);
    [Signal] public delegate void BucketEndedEventHandler(Node action);

    [Export(PropertyHint.Range, "0.0, 1.0")] float _threshold = 0.5f;
    public float Thr { get { return _threshold; } set { _threshold = value; } }



    public async override void _UnhandledInput(InputEvent @event)
    {
        if (MultiTouch.Count > 1U) return;
        if (!ProjectManager.CurrentTool.Equals("Bucket")) return;

        if(!UndoRedoScript.Recorded) return;
        if (@event.IsActionPressed("Finger1"))
        {
            await ToSignal(GetTree().CreateTimer(0.4f), Timer.SignalName.Timeout);
            if(MultiTouch.Count == 2U) return;
            // Select sprite and make a copy of the image
            ImageToFill = Sprite.Img; // Disposed by record later
            var oldImg = Sprite.Img;

            (_width, _height) = AppManager.Project.Size;
            _replacementColor = GetNode<ColorPickerButton>("%ColorPickerButton").Color;
            _mousePos = (Vector2I)GetGlobalMousePosition();
            if (IsOutsideImage(_mousePos.X, _mousePos.Y)) return;
            _oldColor = ImageToFill.GetPixelv(_mousePos);

            var filled = await Task.Run(() => BucketFill(_mousePos));

            if(!filled)
            {
                ImageToFill.Dispose();
                oldImg.Dispose();
                return;
            }

            var list = ActionList;
            UndoRedoScript.StartRecord();
            await Task.Run(() => RecordChanges(list, oldImg, ImageToFill));
        }

    }

    async Task RecordChanges(Node list, Image oldImage, Image newImage)
    {
        var fillRect = GetFillRectangle(_drawLines);
        var recorder = new ToolsActionRecorder(2, "Bucket");

        void SavePart(ushort i, Image image, Rect2I rect)
        {
            var section = Image.CreateEmpty(
                rect.Size.X, 
                rect.Size.Y,
                false,
                Image.Format.Rgba8
            );
            section.BlitRect(
                image,
                rect,
                Vector2I.Zero
            );
            recorder.AddPart(
                i, section,
                rect.Position,
                (rect.Position+rect.Size)/2
            );
            image.Dispose();
        }
        var before = Task.Run(() => SavePart(0, oldImage, fillRect));
        var after = Task.Run(() => SavePart(1, newImage.Duplicate() as Image, fillRect));
        await Task.WhenAll(before, after);
        _drawLines.Clear();
        list.CallDeferred(MethodName.AddChild, recorder);
        CallDeferred(MethodName.EmitSignal, SignalName.BucketEnded, this);
        Sprite.CallDeferred(
                BasicAddImage.MethodName.SetAndUpdate, 
                ImageTexture.CreateFromImage(newImage)
        );

    }


    // Same as Drawer one
    public void Undo() => ModifySourceSprite(0);
    public void Redo() => ModifySourceSprite(1);
    
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



    /*
        Algorithm adapted from:
        https://en.wikipedia.org/wiki/Flood_fill span filling algorithm
    */
    public bool BucketFill(Vector2I vpos)
    {
        if (AreColorsEqual(_oldColor, _replacementColor)) return false;
        if (!HasToBeFilled(vpos.X, vpos.Y)) return false;
        int x;

        _bucketArray.Enqueue(new Vector4I(vpos.X, vpos.X, vpos.Y, 1));
        _bucketArray.Enqueue(new Vector4I(vpos.X, vpos.X, vpos.Y - 1, -1));
        while (_bucketArray.Count > 0)
        {
            _pixel = _bucketArray.Dequeue();
            x = _pixel.X;
            if (HasToBeFilled(x, _pixel.Z))
            {
                while (HasToBeFilled(x - 1, _pixel.Z)) { x--; }

                if (x < _pixel.X)
                {
                    _drawLine = new Rect2I(x, _pixel.Z, _pixel.X - x, 1);
                    _drawLines.Add(_drawLine);
                    ImageToFill.FillRect(_drawLine, _replacementColor);
                    _bucketArray.Enqueue(new Vector4I(x, _pixel.X - 1, _pixel.Z - _pixel.W, -_pixel.W));
                }
            }
            while (_pixel.X <= _pixel.Y)
            {
                int minX = _pixel.X;
                while (HasToBeFilled(_pixel.X, _pixel.Z)) { _pixel.X++; }

                if (_pixel.X > minX)
                {
                    _drawLine = new Rect2I(minX, _pixel.Z, _pixel.X - minX, 1);
                    _drawLines.Add(_drawLine);
                    ImageToFill.FillRect(_drawLine, _replacementColor);
                }

                if (_pixel.X > x)
                {
                    _bucketArray.Enqueue(new Vector4I(x, _pixel.X - 1, _pixel.Z + _pixel.W, _pixel.W));
                }
                if (_pixel.X - 1 > _pixel.Y)
                {
                    _bucketArray.Enqueue(new Vector4I(_pixel.Y + 1, _pixel.X - 1, _pixel.Z - _pixel.W, -_pixel.W));
                }
                _pixel.X++;
                while (_pixel.X < _pixel.Y && !HasToBeFilled(_pixel.X, _pixel.Z))
                {
                    _pixel.X++;
                }
                x = _pixel.X;
            }

        }
        return true;

    }





    public bool HasToBeFilled(int x, int y)
    {
        if (IsOutsideImage(x, y)) return false;
        Color c = ImageToFill.GetPixel(x, y);
        // higher threshold means more image "eaten"
        return ColorDistance(c, _oldColor) < _threshold &&
                !AreColorsEqual(c, _replacementColor);
    }


    bool IsOutsideImage(int x, int y) => x < 0 || x >= _width || y < 0 || y >= _height;

    static float ColorDistance(Color c1, Color c2)
    {
        float rmean = (c1.R + c2.R) / 2.0f;
        float r = c1.R - c2.R;
        float g = c1.G - c2.G;
        float b = c1.B - c2.B;
        float alphaDist = Mathf.Abs(c1.A - c2.A);
        /*
            Readmean metric for color distance
            from https://en.wikipedia.org/wiki/Color_difference
        */
        float rgbMean = Mathf.Sqrt(((2f + rmean) * r * r) + 4f * g * g + ((3f - rmean) * b * b)) / 3f;

        return alphaDist > 0.75f ? alphaDist ://1f:
                ApproxEqual(alphaDist, 0f) ? rgbMean : (rgbMean + alphaDist) / 2f;

    }


    static bool AreColorsEqual(Color c1, Color c2)
    {
        return ApproxEqual(c1.R, c2.R) && ApproxEqual(c1.G, c2.G) &&
                ApproxEqual(c1.B, c2.B) && ApproxEqual(c1.A, c2.A);
    }


    static bool ApproxEqual(float a, float b) => Mathf.Abs(a - b) < 1f / 255f;


    static Rect2I GetFillRectangle(List<Rect2I> fillLines)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        foreach (var lines in fillLines)
        {
            minX = Mathf.Min(minX, lines.Position.X);
            minY = Mathf.Min(minY, lines.Position.Y);
            maxX = Mathf.Max(maxX, lines.Position.X + lines.Size.X);
            maxY = Mathf.Max(maxY, lines.Position.Y + lines.Size.Y);
        }
        return new Rect2I(
            minX, minY, // position
            maxX-minX, maxY-minY // size
        );
    }


}
