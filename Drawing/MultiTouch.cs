using Godot;
using System;


public partial class MultiTouch : Node
{
    struct F
    {
        public bool Pressed;
        public bool Moving;
        public Vector2 StartPos;
        public Vector2 OldPos;
        public Vector2 NewPos;

        public F()
        {
            Pressed = false;
            Moving = false;
            StartPos = Vector2.Zero;
            OldPos = Vector2.Zero;
            NewPos = Vector2.Zero;
        }
    }


    static readonly F[] Fingers = new F [2]; 
    public static Vector2 Speed => GetSpeedValue();
    public static float Zoom => GetZoomValue();
    public static float Rotation => GetRotationValue();

    public static bool IsMobile => OS.GetName().Equals("Android");
    public static uint Count => 
        Fingers[0].Pressed? 1U + (Fingers[1].Pressed? 1U : 0U): 
                            0U + (Fingers[1].Pressed? 1U : 0U);

    public static bool IsMoving => Fingers[0].Moving || Fingers[1].Moving;



    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventScreenTouch eTouch && eTouch.Index < 2)
        {
            int i = eTouch.Index;
            Fingers[i].OldPos = Fingers[i].NewPos = 
            Fingers[i].StartPos = eTouch.Position;
            Fingers[i].Pressed = eTouch.Pressed;
            Fingers[i].Moving = false;
        }
        else if(@event is InputEventScreenDrag eDrag && eDrag.Index < 2)
        {
            int i = eDrag.Index;
            Fingers[i].OldPos = Fingers[i].NewPos;
            Fingers[i].NewPos = eDrag.Position;
            Fingers[i].Moving = true;
        }
        else
        {
            for (int i = 0; i < Count; i++)
                Fingers[i].OldPos = Fingers[i].NewPos;
        }
    }

    Vector2 _prevPos = Vector2.Zero;



    static Vector2 GetSpeedValue()
    {
        if(Count != 2U || !MovingAngle(true) || !IsMoving) 
            return Vector2.Zero;

        var moveSpeed = GetSpeedFinger(1);
        return moveSpeed;

    }
    static Vector2 GetSpeedFinger(int i) => Fingers[i].NewPos - Fingers[i].OldPos;


    const float maxSpeed = 6000f;
    static float GetZoomValue()
    {
        if(Count != 2U) 
            return 0f;

        var startDist = Fingers[1].StartPos.DistanceTo(Fingers[0].StartPos);
        var newDist = Fingers[1].NewPos.DistanceTo(Fingers[0].NewPos);
        return newDist-startDist;
        
    }


    static float GetRotationValue()
    {
        if(Count != 2U || !IsMoving)
            return 0f;

        float oldAngle = Fingers[0].StartPos.AngleToPoint(Fingers[1].StartPos);
        float angle = Fingers[0].NewPos.AngleToPoint(Fingers[1].NewPos);
        return oldAngle-angle;
    }


    static bool MovingAngle(bool parallelNotOpposite)
    {
        static Vector2 Dir(F finger) => (finger.NewPos-finger.OldPos).Normalized();
        float cosAngle = Dir(Fingers[0]).Dot(Dir(Fingers[1]));

        if(parallelNotOpposite) return cosAngle > 0.9f;
        else  return cosAngle < -0.8f;

    }
    
}
