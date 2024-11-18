using Godot;
using System;

public partial class FrameOptionsMenu : Node
{
    LayerManager _lMan;
    string _deleteText = "Are you sure you want to delete the image?";

	public override void _Ready()
	{
        _lMan = GetTree().Root.GetChild(0).GetChild<LayerManager>(1);

        var delButton = GetNode<Button>("%DeleteLayerButton");
        delButton.ButtonUp += () => {
            PopUp.Make(
                this, true, _deleteText, new Vector2I(500,80),
                CallDelete, new Variant [] {LayerManager.LayerIndex}
            );
        };

        var duplButton = GetNode<Button>("%DuplicateLayerButton");
        duplButton.ButtonUp += () => {
            _lMan.DuplicateLayer();
            GetOwner<CanvasLayer>().Visible=false;
        };

        var shadButton = GetNode<Button>("%ShadowLayerButton");
        shadButton.ButtonUp += () => {
            _lMan.SetShadowLayer(LayerManager.LayerIndex);
            GetOwner<CanvasLayer>().Visible=false;
        };

	}



    Action<Variant[]> CallDelete => (index) => {
        _lMan.DeleteLayer((int)index[0]);
        GetOwner<CanvasLayer>().Visible=false;
    };


}
