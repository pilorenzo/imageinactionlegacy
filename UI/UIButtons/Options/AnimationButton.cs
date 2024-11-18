using Godot;
using System;

public partial class AnimationButton : Panel
{

	public override void _Ready()
	{
        var lMan = Owner.GetNode<LayerManager>("%Art");
        GetChild<TextureButton>(0).ButtonUp += () => {
            ProjectManager.AnimUI.Visible = true;
            ProjectManager.DrawUI.Visible = false;
            lMan.Visible = false;

            AnimationController.AnimSprite.Scale = SetViewScale();
            AnimationController.AnimSprite.SpriteFrames.ClearAll();
            AnimationController.AnimSprite.SpriteFrames.SetAnimationSpeed(
                "default", 1.0 // FPS at SpeedScale 1 
            );
            
            foreach(BasicAddImage child in lMan.GetChildren())
            {
                AnimationController.AnimSprite.SpriteFrames.AddFrame(
                    "default",
                    child.Texture
                );
            }
        };

	}

    public static Vector2 SetViewScale()
    {
        float largestDim = 400f;
        var (width, height) = AppManager.Project.Size;
        var dim = width > height? width : height;
        return new Vector2(largestDim/dim, largestDim/dim);
    }

}
