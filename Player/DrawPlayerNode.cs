using Godot;
using System;

public partial class DrawPlayerNode : Node2D
{

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 16, new Color(1, 0, 0)); // red circle
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
