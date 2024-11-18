using Godot;
using System;

public partial class RectangleSelScript : SelectionScript
{

    protected override void ExtendSelector()
    {
        MousePos = GetGlobalMousePosition();

        // the first point (index 0) is not moved
        CurrentLine?.SetPointPosition(1, new Vector2(MousePos.X, CurrentLine.Points[1].Y));
        CurrentLine?.SetPointPosition(2, MousePos);
        CurrentLine?.SetPointPosition(3, new Vector2(CurrentLine.Points[3].X, MousePos.Y));

    }

    protected override void GenerateSelector()
    {
        MousePos = GetGlobalMousePosition();

        CurrentLine?.AddPoint(MousePos);
        CurrentLine?.AddPoint(MousePos);
        CurrentLine?.AddPoint(MousePos);
        CurrentLine?.AddPoint(MousePos);

    }



    public void PasteSelection(ToolsActionRecorder rec)
    {
        SectionRecord = rec;
        SectionRecord.Texture = ImageTexture.CreateFromImage(rec.Parts[0].Image);
        CreateNewLine();

        var halfSize = SectionRecord.Parts[0].Image.GetSize()/2;
        var center = Owner.GetNode<Camera2D>("%UserCamera").Position;
        SectionRecord.Position = center;
        CurrentLine.SetPointPosition(0, new Vector2(center.X-halfSize.X, center.Y-halfSize.Y));
        CurrentLine.SetPointPosition(1, new Vector2(center.X+halfSize.X, center.Y-halfSize.Y));
        CurrentLine.SetPointPosition(2, new Vector2(center.X+halfSize.X, center.Y+halfSize.Y));
        CurrentLine.SetPointPosition(3, new Vector2(center.X-halfSize.X, center.Y+halfSize.Y));


        MainSprite.GetChild(0).AddChild(SectionRecord);
        CurrentLine.Reparent(SectionRecord);
        CurrentLine.ZIndex = 1;

        Phase = SelPhase.Selected;
    }


    bool IsCopyPasteRec =>  MainSprite.GetChild(0).GetChild<ToolsActionRecorder>(-1)
                            .Parts.Length == 2;



    void CopyPasteUndo()
    {
        var record = MainSprite.GetChild(0).GetChild<ToolsActionRecorder>(-1);
        using Image newImage = MainSprite.Texture.GetImage();
        const ushort overwritten = 1;

        newImage.BlitRect(
            record.Parts[overwritten].Image,
            new Rect2I(Vector2I.Zero, record.Parts[overwritten].Image.GetSize()),
            record.Parts[overwritten].Start
        );

        UpdateTexture(newImage);

    }

    void CopyPasteRedo()
    {
        var record = MainSprite.GetChild(0).GetChild<ToolsActionRecorder>(-1);
        using Image newImage = MainSprite.Texture.GetImage();
        const ushort copied = 0, overwritten = 1;

        using var partUpdater = new Sprite2D()
        {
            Position = record.Parts[overwritten].Center,
            Rotation = record.Rotation, // Parts[overwritten].Rotation,
            Scale = record.Scale,
            Texture = ImageTexture.CreateFromImage(record.Parts[copied].Image),
        };

    }



}
