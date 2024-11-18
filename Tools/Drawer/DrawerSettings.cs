using Godot;
using static Godot.Line2D;
using System;

public partial class DrawerSettings : Resource
{

    [Export] public bool IsPen {get;set;}
    [Export] public int Width {get;set;}
    [Export] public Color DefaultColor {get;set;}
    [Export] public LineCapMode EndCapMode {get;set;}
    [Export] public LineCapMode BeginCapMode {get;set;}
    [Export] public bool Antialiased {get;set;}


    public DrawerSettings() : this (true, 0, Colors.Transparent, 0, 0, false) {}
    
    public DrawerSettings(bool ispen, int w, Color color, LineCapMode end, LineCapMode begin, bool aa)
    {
        IsPen = ispen;
        Width = w;
        DefaultColor = color;
        EndCapMode = end;
        BeginCapMode = begin;
        Antialiased = aa;
    }


}
