using Godot;
using System;


public partial class ToolsActionRecorder : Sprite2D
{

    public struct Part 
    {
        public Image Image;
        public Vector2I Center;
        public Vector2I Start;
    }

    public Part[] Parts;

    public ToolsActionRecorder(uint size, string action)
    {
        Parts = new Part[size];
        ActionName = action;
    } 

    public string ActionName {get;init;}

    public void AddPart(uint index, Image image, Vector2I start, Vector2I center)
    {
        if (index > Parts.Length - 1) return;

        Parts[index].Image = image;
        Parts[index].Center = center;
        Parts[index].Start = start;
    }

}
