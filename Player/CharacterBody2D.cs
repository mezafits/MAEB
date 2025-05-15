using Godot;
using System;

public partial class CharacterBody2D : Godot.CharacterBody2D
{
    public const float Speed = 300.0f;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Vector2.Zero;

        if (Input.IsActionPressed("move_right"))
            velocity.X += 1;
        if (Input.IsActionPressed("move_left"))
            velocity.X -= 1;
        if (Input.IsActionPressed("move_down"))
            velocity.Y += 1;
        if (Input.IsActionPressed("move_up"))
            velocity.Y -= 1;

        if (velocity != Vector2.Zero)
        {
            velocity = velocity.Normalized() * Speed;
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
