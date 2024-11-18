using Godot;
using System;

public partial class AnimationController : Node
{
	static AnimatedSprite2D _animSprite;
    public static AnimatedSprite2D AnimSprite => _animSprite;

    TextureButton _pauseButton;
    TextureButton _playButton;
    TextureButton _exitButton;
    SpinBox _frameBox;
    SpinBox _speedBox;


    public override void _Ready()
    {
        _animSprite = GetNode<AnimatedSprite2D>("%AnimSprite");

        var container = GetChild(0);
        _pauseButton = container.GetNode<TextureButton>("PauseButton");
        _playButton = container.GetNode<TextureButton>("PlayButton");
        _exitButton = container.GetNode<TextureButton>("ExitButton");
        _frameBox = container.GetNode<SpinBox>("FrameBox");
        _speedBox = container.GetNode<SpinBox>("SpeedBox");

        _pauseButton.ButtonUp += () => _animSprite.Pause();
        _playButton.ButtonUp += () => _animSprite.Play();

        _frameBox.ValueChanged += (v) => {
            if(!_animSprite.IsPlaying())
            {
                _frameBox.MaxValue = (double) _animSprite.SpriteFrames.GetFrameCount("default")-1;
                _animSprite.Frame = (int)v;
            }
        };
        _speedBox.ValueChanged += (v) => _animSprite.SpeedScale = (int)v;

        _animSprite.FrameChanged += () => {
            if(_animSprite.IsPlaying()) _frameBox.Value = _animSprite.Frame;
        };

        _exitButton.ButtonUp += () => {
            ProjectManager.AnimUI.Visible = false;
            ProjectManager.DrawUI.Visible = true;
            ProjectManager.LMan.Visible = true;
            _animSprite.Stop();
        };

        Owner.GetOwner<ProjectManager>().BGColorChanged += (c) =>
            Owner.GetChild<ColorRect>(0).Color = c;

    }
}
