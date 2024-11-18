using Godot;
using System;

public partial class Scroller : ScrollContainer
{

    [Export(PropertyHint.Range, "10, 100")] float _speed = 50f;
    
    Vector2 _prevPos;
    bool _isMousePressed = false;
    bool _scrolling = false;
 


    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventMouseButton e)
            _isMousePressed = e.Pressed;

        if(_isMousePressed && IsMouseInControlArea(this))
        {
            if(_scrolling == false)
            {
                _prevPos = GetGlobalMousePosition();
                _scrolling = true;
            }
        }

        else _scrolling = false;

    }


    public override void _Process(double delta)
	{
        if(_scrolling == false) return;

        var mousePos = GetGlobalMousePosition();
        int hValue = Mathf.RoundToInt((mousePos.X-_prevPos.X)*delta*_speed);
        ScrollHorizontal -= hValue;
        _prevPos = mousePos;

	}


    public static bool IsMouseInControlArea(Control c)
    {
        var mousePos = c.GetGlobalMousePosition();
        var (x,y) = (mousePos.X,mousePos.Y);

        return
            x > c.GlobalPosition.X && x < c.GlobalPosition.X + c.Size.X &&
            y > c.GlobalPosition.Y && y < c.GlobalPosition.Y + c.Size.Y;

    }
}
