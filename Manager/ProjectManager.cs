using Godot;
using System;


public partial class ProjectManager : Node
{
    static ProjectManager Self;
    [Signal] public delegate void ToolButtonEventHandler(StringName tName);
    [Signal] public delegate void BGColorChangedEventHandler(Color newColor);
    public static string CurrentTool {get;set;} = "Pen";
    public UndoRedoScript UndoRedo => 
        LayerManager.ActiveSprite.GetNode<UndoRedoScript>("%UndoRedo");

    public (int W, int H) Size => AppManager.Project.Size;
    [Export] public Color BGColor 
    {
        get => AppManager.Project.BGColor;
        set { AppManager.Project.BGColor = value; EmitSignal(SignalName.BGColorChanged, value); }
    }
    
    public static bool InSelection => 
        SelectionScript.Phase != SelectionScript.SelPhase.NotSelecting;


    public static CanvasLayer DrawUI => Self.GetNode<CanvasLayer>("%DrawCanvas");
    public static CanvasLayer AnimUI => Self.GetNode<CanvasLayer>("%AnimCanvas");
    public static LayerManager LMan => Self.GetNode<LayerManager>("%Art");


    public override void _Ready()
    {
        if(Self != null && Self != this) Self.QueueFree();
        Self = this;

        RenderingServer.SetDefaultClearColor(Colors.Gray);
        PhysicsServer2D.SetActive(false);
        PhysicsServer3D.SetActive(false);

        CurrentTool = "Pen";
        ToolButton += (str) => CurrentTool = str;

        BGColorChanged += (c) => {
            var bg = GetChild<Sprite2D>(0);
            using var bgImage = Image.CreateEmpty(Size.W, Size.H, false, Image.Format.Rgba8);
            bgImage.Fill(c);
            bg.Texture = ImageTexture.CreateFromImage(bgImage);
        };

        EmitSignal(SignalName.BGColorChanged, BGColor);

        // GetNode<Timer>("%SaveTimer").Timeout += () => 
        //     AppManager.SaveProject(LMan);

    }
   

    public static bool CheckDrag(InputEvent @event) =>
        AppManager.IsManagerDebug
            ? @event is InputEventMouseMotion
            : @event is InputEventScreenDrag;




    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            Self = null;
        }
    }
}
