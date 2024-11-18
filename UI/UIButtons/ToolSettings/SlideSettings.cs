using Godot;
using System;

public partial class SlideSettings : Node
{
	Label TextLabel => GetChild<Label>(0);
    HSlider Bar => GetChild<HSlider>(1);
    ProjectManager Manager => GetOwner<ProjectManager>();
    BasicDrawer Drawer => Owner.GetNode<BasicDrawer>("%Drawer");
    BasicDrawer Eraser => Owner.GetNode<BasicDrawer>("%Eraser");
    Bucket BucketNode => Owner.GetNode<Bucket>("%Bucket");


    void ChangeText(string text, int value, string unit) =>
        TextLabel.Text = $"{text}  ({value}{unit})";

    StringName _tool = "Pen"; // _tool prevent unwanted width/Thr changes
    public override void _Ready()
    {
        Manager.ToolButton += AcquireToolValue;
        Bar.ValueChanged += ChangeToolValue;
    }


    void ChangeToolValue(double value)
    {
        if(ProjectManager.CurrentTool != _tool) return;
        switch (ProjectManager.CurrentTool)
        {
            case "Pen":
                Drawer.Settings.Width = (int)value;
                ChangeText("Width", Drawer.Settings.Width, "pt");
                break;
            case "Eraser":
                Eraser.Settings.Width = (int)value * 5;
                ChangeText("Width", Eraser.Settings.Width, "pt");
                break;
            case "Bucket":
                BucketNode.Thr = (float)(value/Bar.MaxValue);
                ChangeText("Strength", (int)(BucketNode.Thr*100f), "%");
                break;
            default:
                break;
        }
    }

    void AcquireToolValue(StringName tool)
    {
        _tool = tool;
        switch (tool)
        {
            case "Pen":
                Bar.Value = Drawer.Settings.Width;
                ChangeText("Width", Drawer.Settings.Width, "pt");
                break;
            case "Eraser":
                Bar.Value = Eraser.Settings.Width/5;
                ChangeText("Width", Eraser.Settings.Width, "pt");
                break;
            case "Bucket":
                Bar.Value = BucketNode.Thr*Bar.MaxValue;
                ChangeText("Strength", (int)(BucketNode.Thr*100f), "%");
                break;
            default:
                break;
        }
    }
}
