using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class main_node : Node2D
{
	public const int INITIAL_AGENT_COUNT = 20;
	public const float AGENT_RADIUS = 5f;
	public readonly Color BOUNCE_COLOR = new Color(1.0f, 0.2f, 0.2f);
	public List<Agent> agents = new List<Agent>();
	public Vector2 screenSize;
	public Random rng = new Random();
	public const int MAX_FOOD = 100;
	public List<Food> foodItems = new List<Food>();
	public float foodSpawnTimer = 0f;
	public const float FOOD_SPAWN_INTERVAL = 1f;
	public const int MAX_AGENTS = 100;

	public override void _Ready()
	{
		screenSize = GetViewport().GetVisibleRect().Size;

		// Create initial agents
		for (int i = 0; i < INITIAL_AGENT_COUNT; i++)
		{
			float x = AGENT_RADIUS + (float)rng.NextDouble() * (screenSize.X - 2 * AGENT_RADIUS);
			float y = AGENT_RADIUS + (float)rng.NextDouble() * (screenSize.Y - 2 * AGENT_RADIUS);
			Vector2 position = new Vector2(x, y);
			float angle = (float)(rng.NextDouble() * Math.PI * 2);
			float speed = 50f + (float)rng.NextDouble() * 100f;
			Vector2 velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
			Color color = new Color((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
			agents.Add(new Agent(position, velocity, color, rng));
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// Spawn food
		foodSpawnTimer += dt;
		if (foodSpawnTimer >= FOOD_SPAWN_INTERVAL && foodItems.Count < MAX_FOOD)
		{
			foodSpawnTimer = 0f;
			float x = 20f + (float)rng.NextDouble() * (screenSize.X - 40f);
			float y = 20f + (float)rng.NextDouble() * (screenSize.Y - 40f);
			foodItems.Add(new Food(new Vector2(x, y)));
		}

		// Update food
		for (int i = foodItems.Count - 1; i >= 0; i--)
		{
			foodItems[i].Update(dt);
			if (foodItems[i].IsRotten() || foodItems[i].IsEaten)
			{
				foodItems.RemoveAt(i);
			}
		}

		// Process agents and collect new offspring
		List<Agent> newAgents = new List<Agent>();

		for (int i = 0; i < agents.Count; i++)
		{
			Agent agent = agents[i];

			// Skip dead agents
			if (agent.IsDead)
				continue;

			agent.UpdateNeeds(dt);
			agent.DecideBehavior();

			// Process behavior and check for reproduction
			Agent offspring = agent.Behave(agents, foodItems, dt, rng);
			if (offspring != null && agents.Count + newAgents.Count < MAX_AGENTS)
			{
				newAgents.Add(offspring);
			}

			// Update position and handle bouncing
			agent.Position += agent.Velocity * dt;
			bool bounced = HandleBouncing(agent);

			if (bounced)
			{
				agent.Color = BOUNCE_COLOR;
			}
		}

		// Add new agents
		agents.AddRange(newAgents);

		// Remove dead agents (after a while to show them)
		for (int i = agents.Count - 1; i >= 0; i--)
		{
			if (agents[i].IsDead && agents[i].Age > agents[i].MaxAge + 5f)
			{
				agents.RemoveAt(i);
			}
		}

		QueueRedraw();
	}

	public bool HandleBouncing(Agent agent)
	{
		bool bounced = false;

		if (agent.Position.X < AGENT_RADIUS)
		{
			agent.Position = new Vector2(AGENT_RADIUS, agent.Position.Y);
			agent.Velocity = new Vector2(-agent.Velocity.X, agent.Velocity.Y);
			bounced = true;
		}
		else if (agent.Position.X > screenSize.X - AGENT_RADIUS)
		{
			agent.Position = new Vector2(screenSize.X - AGENT_RADIUS, agent.Position.Y);
			agent.Velocity = new Vector2(-agent.Velocity.X, agent.Velocity.Y);
			bounced = true;
		}

		if (agent.Position.Y < AGENT_RADIUS)
		{
			agent.Position = new Vector2(agent.Position.X, AGENT_RADIUS);
			agent.Velocity = new Vector2(agent.Velocity.X, -agent.Velocity.Y);
			bounced = true;
		}
		else if (agent.Position.Y > screenSize.Y - AGENT_RADIUS)
		{
			agent.Position = new Vector2(agent.Position.X, screenSize.Y - AGENT_RADIUS);
			agent.Velocity = new Vector2(agent.Velocity.X, -agent.Velocity.Y);
			bounced = true;
		}

		return bounced;
	}

	public override void _Draw()
	{
		// Draw food
		foreach (Food food in foodItems)
		{
			float ageRatio = food.Age / food.MaxAge;
			Color foodColor = new Color(0f, 1f - ageRatio, 0f); // Gets darker as it ages
			DrawCircle(food.Position, 3f, foodColor);
		}

		// Draw agents
		foreach (Agent agent in agents)
		{
			float size = AGENT_RADIUS;

			// Make young agents smaller
			if (agent.Age < agent.MaxAge * 0.3f)
			{
				size = AGENT_RADIUS * (0.5f + 0.5f * (agent.Age / (agent.MaxAge * 0.3f)));
			}

			DrawCircle(agent.Position, size, agent.Color);
		}
	}
}
