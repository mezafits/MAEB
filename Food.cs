using Godot;
#region Food Class
public class Food
{
	public Vector2 Position { get; set; }
	public bool IsEaten { get; set; }
	public float Age { get; set; } = 0f;
	public float MaxAge { get; set; } = 30f; // Food rots after 30 seconds

	public Food(Vector2 position)
	{
		Position = position;
		IsEaten = false;
	}

	public void Update(float dt)
	{
		Age += dt;
	}

	public bool IsRotten()
	{
		return Age >= MaxAge;
	}
}
