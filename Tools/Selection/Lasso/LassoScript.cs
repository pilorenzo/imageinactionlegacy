using Godot;
using System;

public partial class LassoScript : SelectionScript
{

	protected override void ExtendSelector()
	{
		CurrentLine?.AddPoint(GetGlobalMousePosition());
	}

	protected override void GenerateSelector()
	{
		CurrentLine?.AddPoint(GetGlobalMousePosition());
	}



}
