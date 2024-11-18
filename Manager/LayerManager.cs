using Godot;
using System;


public partial class LayerManager : Node2D
{
    [Signal] public delegate void LayerSelectedEventHandler(int index);
    static uint s_spriteIndex = 0U;
    public static int LayerIndex => (int) s_spriteIndex;
    public static BasicAddImage ActiveSprite {get;set;} 
    public static BasicAddImage ShadowSprite {get;set;}
    public SpriteUpdater Updater => ActiveSprite.GetNode<SpriteUpdater>("%Updater");

    static readonly SceneTree Tree = (SceneTree)Engine.GetMainLoop();
    static Node FrameList => Tree.Root.GetChild(0).GetNode("%FrameUI");
    public static Node FrameView => FrameList.GetChild(LayerIndex);
    public static Node GetFrameViewAtIndex(int i) => FrameList.GetChild(i);
    public static int FrameSide => 128;


	public async override void _Ready()
	{
        if(AppManager.IsNewProject)
        {
            CreateLayer();
        }
        else
        {
            using var dir = DirAccess.Open(AppManager.FullPath);
            foreach (string filename in dir.GetFiles())
            {
                char lastChar = filename.TrimSuffix(".png")[^1];
                if (char.IsDigit(lastChar))
                {
                    CreateLayer();
                }
            }
        }

        ActiveSprite = GetChild<BasicAddImage>(-1);
        ShadowSprite = null;
        await ToSignal(Updater, SpriteUpdater.SignalName.RenderingUpdated);
        await ToSignal(Updater, SpriteUpdater.SignalName.RenderingUpdated);
        AppManager.IsSceneJustInstantiated = false;
        SetLayerIndex(GetChildCount()-1);
    }


    public void CreateLayer(int index = -1)
    {
        bool notLastLayer = index >= 0 && index <= GetChildCount();
        var frameView = GD.Load<PackedScene>("res://UI/FrameView/layer_selector.tscn") 
                        .Instantiate() as Control;
        
        FrameList.AddChild(frameView);
        int frameIndex = notLastLayer ? index+1 : -2;
        FrameList.MoveChild(frameView, frameIndex);

        var layer = GD.Load<PackedScene>("res://layer.tscn").Instantiate();
        AddChild(layer);
        if(notLastLayer) MoveChild(layer, index+1);
        layer.Owner = Owner;

        if(!AppManager.IsSceneJustInstantiated)
            SetLayerIndex(notLastLayer? index+1 : GetChildCount()-1);

    }


    public async void DuplicateLayer()
    {
        var img = ActiveSprite.Img;
        CreateLayer(LayerIndex);
        await ToSignal(Updater, SpriteUpdater.SignalName.RenderingUpdated);
        await ToSignal(Updater, SpriteUpdater.SignalName.RenderingUpdated);
        ActiveSprite.SetAndUpdate(ImageTexture.CreateFromImage(img));
    }


    // Called by LayerSelector
    public void SetLayerIndex(int value)
    {
        if(value > GetChildCount()) return;

        ActiveSprite = GetChild<BasicAddImage>(value);
        s_spriteIndex = (uint) value;

        foreach (BasicAddImage child in GetChildren())
            child.Visible = child.GetIndex() == s_spriteIndex ||
                            child.GetIndex() == ShadowSprite?.GetIndex();

        if(ShadowSprite != null)
            ShadowSprite.IsShadow = ShadowSprite.GetIndex() != value;
        // used to modify border in frameview
        EmitSignal(SignalName.LayerSelected, value);
    }


    public void DeleteLayer(int index)
    {
        if(index == s_spriteIndex)
            SetLayerIndex((index+1)%GetChildCount());

        var sprite = GetChild<BasicAddImage>(index);
        if(sprite.IsShadow) ShadowSprite = null;
        RemoveChild(sprite);
        sprite.QueueFree();

        var frame = FrameList.GetChild(index);
        FrameList.RemoveChild(frame);
        frame.QueueFree();

    }


    public void SetShadowLayer(int index)
    {
        bool isValidIndex = index >= 0 && index < GetChildCount();
        if(!isValidIndex) return;

        if(ShadowSprite?.GetIndex() == index)
        {
            ShadowSprite.IsShadow = false;
            ShadowSprite = null;
        }
        else
        {
            var sprite = GetChild<BasicAddImage>(index);
            int oldShadIndex = int.MaxValue;
            if(ShadowSprite != null)
            {
                oldShadIndex = ShadowSprite.GetIndex();
                ShadowSprite.IsShadow = false;
            }
            ShadowSprite = sprite;

            // does nothing if oldShadIndex == MaxValue
            // (i.e. if ShadowSprite was null)
            SetLayerIndex(oldShadIndex);
        }
 
    }

}
