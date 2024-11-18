using Godot;
using System;

namespace MainMenu;


public partial class NewButton : Button
{

    FileDialog FileDialog => Owner.GetNode<FileDialog>("%FileDialog");
    NewProjectMenu NewOption => Owner.GetNode<NewProjectMenu>("%NewProjectOptions");
 

	public override void _Ready()
	{
        ButtonUp += () => {
            bool correct = OS.RequestPermissions();
            GD.Print(OS.GetGrantedPermissions());

            if(!DirAccess.DirExistsAbsolute(AppManager.StartPath))
                DirAccess.MakeDirAbsolute(AppManager.StartPath);

            NewOption.Visible = true;
        };
        NewOption.VisibilityChanged += AddOrRemoveSignals;
	}


    void AddOrRemoveSignals()
    {
        if(NewOption.Visible)
        {
            NewOption.Canceled += HideNewOptionMenu;
            NewOption.Oked += CreateProject;
        }
        else
        {
            NewOption.Canceled -= HideNewOptionMenu;
            NewOption.Oked -= CreateProject;
        }
    }

    void HideNewOptionMenu() => NewOption.Visible = false;

    void CreateProject() => AppManager.CreateNewProject(NewOption);
    
}
