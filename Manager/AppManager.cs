using Godot;
using static Godot.DirAccess;
using static Godot.FileAccess;
using System;
using MainMenu;
using Godot.Collections;
using System.Threading.Tasks;



public partial class AppManager : Node
{
#if DEBUG
    public static bool IsManagerDebug => !OS.GetName().Equals("Android");
#else
    public static bool IsManagerDebug => false;
#endif
    static ProjectConfig NewProject = new();
    static ProjectConfig LoadProject = new();

    public static ProjectConfig Project => IsNewProject? NewProject : LoadProject; 

    public static string StartPath => IsManagerDebug? 
        OS.GetSystemDir(OS.SystemDir.Pictures) + "/ImageInAction/":
        OS.GetSystemDir(OS.SystemDir.Pictures) + "/ImageInAction/";

    public static string FullPath => StartPath + Project.Name + "/";
    static string ConfigPath => FullPath + ConfigName;
    static string ConfigName => "settings.stp";

    public static bool IsNewProject {get; private set;} = true;
    public static bool IsSceneJustInstantiated {get;set;} = false;

    static SceneTree MainTree => (SceneTree)Engine.GetMainLoop();


    public async static void CreateNewProject(NewProjectMenu setup)
    {
        if(string.IsNullOrEmpty(setup.ProjectName))
        {
            PopUp.Error($"You should define a name for the project");
            return;
        }
        NewProject = new(){
            Name = setup.ProjectName,
            BGColor = setup.ProjectBGColor,
            IsFromImage = setup.IsFromImage,
            Size = setup.ProjectSize,
            StartImagePath = setup.StartImagePath
        };

        if(NewProject.IsFromImage)
        {
            using var image = Image.LoadFromFile(NewProject.StartImagePath);
            if(image == null)
            {
                PopUp.Error($"Can't get image from path {NewProject.StartImagePath}");
                return;
            }
            
            NewProject.Size = (image.GetSize().X, image.GetSize().Y);
        }
        setup.Visible = false;


        IsNewProject = true;

        if(IsManagerDebug) GD.Print("FullPath: " + FullPath);

        if (!DirExistsAbsolute(StartPath))
        {
            if(CreateDir(StartPath) != Error.Ok) return;
            if(CreateDir(FullPath)  != Error.Ok) return;
        }
        else
        {
            if(CreateDir(FullPath)  != Error.Ok) return;
        }

        var formattedData = Json.Stringify(NewProject.SerializeData(), " ");
        if(IsManagerDebug) GD.Print(formattedData);
        using var configFile = Open(ConfigPath, ModeFlags.Write);
        configFile.StoreString(formattedData);

        IsSceneJustInstantiated = true;
        MainTree.ChangeSceneToFile("res://basic.tscn");
        await MainTree.ToSignal(MainTree.CreateTimer(2), Timer.SignalName.Timeout);
        SaveProject(MainTree.Root.GetChild(0).GetNode<LayerManager>("%Art"));

    }


    static Error CreateDir(string path)
    {
        var error = MakeDirAbsolute(path);
        if(error != Error.Ok)
        {
            PopUp.Error($"Unable to create folder: {error}");
        }
        return error;
        
    }



    public static void SaveProject(LayerManager art)
    {
        // GD.Print("Saving project: " + Project.Name);
        if(string.IsNullOrEmpty(Project.Name))
        {
            PopUp.Error("Project has no name!!!");
            return;
        }

        if(!DirExistsAbsolute(FullPath))
        {
            PopUp.Error("Project Directory does not exist!!");
            return;
        }

        using (var dir = Open(FullPath))
        {
            foreach (string fileName in dir.GetFiles())
            {
                if(!fileName.Equals(ConfigName))
                    dir.Remove(fileName); // Or maybe better OS.MoveToTrash
            }
        }

        foreach (Sprite2D sprite in art.GetChildren())
        {
            Task.Run(() => sprite.Texture
                .GetImage()
                .SavePng(FullPath + Project.Name+"_"+sprite.GetIndex()+".png")
            );
        }

    }

	public static void LoadExistingProject(string dirPath)
    {
        IsNewProject = false;
        using (var dir = Open(dirPath))
        {
            if(dir == null)
            {
                PopUp.Error($"Unable to access folder {dirPath}:\n{DirAccess.GetOpenError()}"); 
                return;
            }
        }

        string filePath = dirPath+"/"+ConfigName;
        using (var configFile = Open(filePath, ModeFlags.Read))
        {
            if(configFile == null)
            {
                PopUp.Error($"Unable to access file {filePath}:\n{FileAccess.GetOpenError()}"); 
                return;
            }
            var json = new Json();
            var parseCheck = json.Parse(configFile.GetAsText());
            if(parseCheck != Error.Ok)
            {
                PopUp.Error($"Unable to load data: {parseCheck}");
            }

            LoadProject = ProjectConfig.FromDictionary((Dictionary<string, Variant>)json.Data);
        }

        IsSceneJustInstantiated = true;
        MainTree.ChangeSceneToFile("res://basic.tscn");

    }

}


public class ProjectConfig
{
    public ProjectConfig() {}

    public Color BGColor {get;set;} = Colors.White;
    public string Name {get;set;} = "";

    public bool IsFromImage {get;set;} = false;

    int _w = 256;
    int _h = 256;
    public (int W, int H) Size
    {
        get { return (_w, _h); }
        set { (_w,_h) = value; }
    }
    public string StartImagePath {get;set;} = "";


    public Dictionary<string, Variant> SerializeData()
    {
        return new Dictionary<string, Variant>()
        {
            {"Name", Name},
            {"BGColor", BGColor.ToHtml()},
            {"Width", Size.W},
            {"Height", Size.H},
        };

    }

    static public ProjectConfig FromDictionary(Dictionary<string, Variant> data)
    {

        return new ProjectConfig()
        {
            Name = data.TryGetValue("Name", out Variant name)? (string)name : "",

            BGColor = data.TryGetValue("BGColor", out Variant color)?
                Color.FromHtml((string)color) : 
                Colors.White,

            _w = data.TryGetValue("Width", out Variant width)? (int)width : 512,
            _h = data.TryGetValue("Height", out Variant height)? (int)height : 512,
        };

    }

}
