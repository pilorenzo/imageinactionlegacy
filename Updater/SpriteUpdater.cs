using Godot;
using System;



public partial class SpriteUpdater : SubViewportContainer
{
    [Signal] public delegate void RenderingUpdatedEventHandler();
    [Signal] public delegate void ImageUpdatedEventHandler();
    [Export] Shader _shader;
    Sprite2D Sprite => GetParent<Sprite2D>();
    public Sprite2D ChildSprite {get; private set;}
    SubViewport _subViewport;
    public TextureRect Bg => _subViewport.GetNode<TextureRect>("Bg");



    public async override void _Ready()
    {
        RenderingServer.FramePostDraw += OnRenderingEnded;
        _subViewport = GetNode<SubViewport>("SubViewport");

        ChildSprite = GetNode<Sprite2D>("%ChildSprite");
        ChildSprite.Centered = false;
        ChildSprite.Offset = Vector2.Zero;

        await ToSignal(this, SignalName.RenderingUpdated);
        Size = new Vector2(AppManager.Project.Size.W, AppManager.Project.Size.H);

    }

    public void SetUpdatedImage()
    {
        _subViewport.RenderTargetClearMode = SubViewport.ClearMode.Once;
        RenderingServer.FramePostDraw += UpdateAndDisconnect;
        void UpdateAndDisconnect()
        {
            UpdateImage();
            RenderingServer.FramePostDraw -= UpdateAndDisconnect;
        }

    }

    public void UpdateImage()
    {
        Sprite.Texture = _subViewport.GetTexture();
        ChildSprite.Texture = null;
        Bg.Texture = null;
        var frameView = LayerManager.GetFrameViewAtIndex(Sprite.GetIndex());
        frameView.GetChild<TextureButton>(0).TextureNormal = Sprite.Texture;

        EmitSignal(SignalName.ImageUpdated);

    }


    void OnRenderingEnded() => EmitSignal(SignalName.RenderingUpdated);

}
