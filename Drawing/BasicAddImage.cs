using Godot;
using System;

public partial class BasicAddImage : Sprite2D
{

    public (int W, int H) Size => AppManager.Project.Size;

    Vector2I _centeringPosition;
    Vector2I _mousePos;
    public Node ActionList => GetChild(0);

    public SpriteUpdater Updater => GetChild<SpriteUpdater>(-1);

    public Image Img => Texture.GetImage();

    float shadowAlpha = 0.2f;
    public bool IsShadow
    {
        get => Mathf.IsEqualApprox(SelfModulate.A, shadowAlpha);
        set => SelfModulate = new Color(SelfModulate, value? shadowAlpha:1f);
    }

    public void SetAndUpdate(Texture2D texture2D)
    {
        // GD.Print("Updating");
        Updater.ChildSprite.Texture = texture2D;
        Updater.Bg.Texture = texture2D;
        Updater.SetUpdatedImage();

    }


    public async override void _Ready()
    {
        if(AppManager.IsSceneJustInstantiated)
        {
            if(AppManager.IsNewProject) NewImage();
            else LoadImage();
            await ToSignal(Updater, SpriteUpdater.SignalName.RenderingUpdated);
            SetAndUpdate(Texture);
            return;
        }
        else
        {
            var (width, height) = Size;
            var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
            image.Fill(Colors.Transparent);
            Texture = ImageTexture.CreateFromImage(image);
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(Updater, SpriteUpdater.SignalName.RenderingUpdated);
        Updater.UpdateImage();

    }


    void NewImage()
    {
        if(GetIndex() == 0 && AppManager.Project.IsFromImage)
        {
            string path = AppManager.Project.StartImagePath;
            var error = SetImageFromFile(path);
            if(error == Error.Ok) 
                return;
        }

    }


    void LoadImage()
    {
        using var dir = DirAccess.Open(AppManager.FullPath);
        foreach (string filename in dir.GetFiles())
        {
            char lastChar = filename.TrimSuffix(".png")[^1];
            if (char.IsDigit(lastChar) && lastChar-'0' == GetIndex())
            {
                // GD.Print("Loading " + filename);
                SetImageFromFile(AppManager.FullPath+filename);
                return;
            }
        }

    }

    Error SetImageFromFile(string path)
    {
        if(!string.IsNullOrEmpty(path))
        {
            using var fileImage = Image.LoadFromFile(path);
            if(fileImage != null)
            {
                // GD.Print("Copying image from path\n" + path);
                Texture = ImageTexture.CreateFromImage(fileImage);
                Updater.ChildSprite.Texture = Texture;
                Updater.Bg.Texture = Texture;
                LayerManager.GetFrameViewAtIndex(GetIndex())
                    .GetChild<TextureButton>(0)
                    .TextureNormal = Texture;
                return Error.Ok;
            }
        }
        PopUp.Error($"Unable to load image from path {path}");
        return Error.Failed;

    }

}
