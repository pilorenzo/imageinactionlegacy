using Godot;
using System;


public partial class LoadButton : Button
{

	public override void _Ready()
	{
        ButtonUp += LoadProject;
	}


    void LoadProject()
    {
        var fileDialog = GetNode<FileDialog>("%LoadProjectDialog");


        bool correct = OS.RequestPermissions();
        GD.Print(OS.GetGrantedPermissions());

        if(!DirAccess.DirExistsAbsolute(AppManager.StartPath))
            DirAccess.MakeDirAbsolute(AppManager.StartPath);

        fileDialog.CurrentPath = AppManager.StartPath;
        // if(AppManager.IsManagerDebug)
        // {
        //     fileDialog.Access = FileDialog.AccessEnum.Resources;
        //     fileDialog.RootSubfolder = "ZZZ_test/Projects";
        // }
        fileDialog.Visible = true;

        fileDialog.DirSelected += Load;
        void Load(string dir)
        {
            AppManager.LoadExistingProject(dir);
            fileDialog.DirSelected -= Load;

        }
    }


	
}
