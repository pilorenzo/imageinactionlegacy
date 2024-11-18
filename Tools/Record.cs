using Godot;
using System;
using System.Threading.Tasks;

public partial class Record : Node2D
{

    public ImageTexture[] IText {get;set;} = new ImageTexture[2];


    public void FromImage(int index, Image image)
    {
        IText[index] = ImageTexture.CreateFromImage(image);
        image.Dispose();
    }

}
