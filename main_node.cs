using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

#region Agent Data Class
/// <summary>
/// Represents a single agent with needs and behaviors.
/// </summary>
public class Agent
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Color Color { get; set; }

    public float Energy { get; set; } = 100f;
    public float Health { get; set; } = 100f;
    public float Hunger { get; set; } = 0f;
    public float Horniness { get; set; } = 0f;
    public float Age { get; set; } = 0f;

    public bool IsHungry => Hunger >= 70f;
    public bool IsHorny => Horniness >= 70f;
    public bool IsTired => Energy <= 30f;

    public Agent(Vector2 position, Vector2 velocity, Color color)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
    }

    public void UpdateNeeds(float dt)
    {
        Hunger += 10f * dt;
        Horniness += 5f * dt;
        Energy -= 8f * dt;
        Age += dt;

        Hunger = MathF.Min(Hunger, 100f);
        Horniness = MathF.Min(Horniness, 100f);
        Energy = MathF.Max(Energy, 0f);
    }

    public void DecideBehavior()
    {
        if (IsHungry)
            Color = new Color(1f, 1f, 0f); // Yellow
        else if (IsTired)
            Color = new Color(0.5f, 0.5f, 1f); // Blue
        else if (IsHorny)
            Color = new Color(1f, 0f, 1f); // Magenta
        else
            Color = new Color(0f, 1f, 0f); // Green
    }

    public void Behave(List<Agent> agents, List<Food> foodList, float dt)
    {
        if (IsHungry)
        {
            Food nearestFood = FindNearestFood(foodList);
            if (nearestFood != null)
            {
                Seek(nearestFood.Position, dt);
                if (Position.DistanceTo(nearestFood.Position) < 10f)
                {
                    nearestFood.IsEaten = true;
                    Hunger -= 50f;
                    Energy += 10f;
                }
            }
        }
        else if (IsTired)
        {
            Velocity *= 0.95f;
            Energy += 20f * dt;
            Energy = MathF.Min(Energy, 100f);
        }
        else if (IsHorny)
        {
            Agent mate = FindMate(agents);
            if (mate != null)
            {
                Seek(mate.Position, dt);
                if (Position.DistanceTo(mate.Position) < 10f)
                {
                    Horniness -= 60f;
                    // Optionally: spawn new agent
                }
            }
        }
    }

    private void Seek(Vector2 target, float dt)
    {
        Vector2 direction = (target - Position).Normalized();
        Velocity = direction * Velocity.Length();
        Position += Velocity * dt;
    }

    private Food FindNearestFood(List<Food> foodList)
    {
        Food nearest = null;
        float closestDist = float.MaxValue;

        foreach (var food in foodList)
        {
            if (food.IsEaten) continue;
            float dist = Position.DistanceTo(food.Position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = food;
            }
        }
        return nearest;
    }

    private Agent FindMate(List<Agent> agents)
    {
        foreach (var other in agents)
        {
            if (other == this) continue;
            if (other.IsHorny && Position.DistanceTo(other.Position) < 100f)
            {
                return other;
            }
        }
        return null;
    }
}
#endregion

#region Food Class
public class Food
{
    public Vector2 Position { get; set; }
    public bool IsEaten { get; set; }

    public Food(Vector2 position)
    {
        Position = position;
        IsEaten = false;
    }
}
#endregion

public partial class main_node : Node2D
{
    private const int AGENT_COUNT = 20;
    private const float AGENT_RADIUS = 5f;
    private readonly Color BOUNCE_COLOR = new Color(1.0f, 0.2f, 0.2f);

    private Agent[] agents;
    private Vector2 screenSize;
    private Random rng = new Random();

    private const int MAX_FOOD = 100;
    private List<Food> foodItems = new List<Food>();
    private float foodSpawnTimer = 0f;
    private const float FOOD_SPAWN_INTERVAL = 2f;

    public override void _Ready()
    {
        screenSize = GetViewport().GetVisibleRect().Size;
        agents = new Agent[AGENT_COUNT];

        for (int i = 0; i < AGENT_COUNT; i++)
        {
            float x = AGENT_RADIUS + (float)rng.NextDouble() * (screenSize.X - 2 * AGENT_RADIUS);
            float y = AGENT_RADIUS + (float)rng.NextDouble() * (screenSize.Y - 2 * AGENT_RADIUS);
            Vector2 position = new Vector2(x, y);

            float angle = (float)(rng.NextDouble() * Math.PI * 2);
            float speed = 50f + (float)rng.NextDouble() * 100f;
            Vector2 velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

            Color color = new Color((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());

            agents[i] = new Agent(position, velocity, color);
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        foodSpawnTimer += dt;
        if (foodSpawnTimer >= FOOD_SPAWN_INTERVAL && foodItems.Count < MAX_FOOD)
        {
            foodSpawnTimer = 0f;
            float x = AGENT_RADIUS + (float)rng.NextDouble() * (screenSize.X - 2 * AGENT_RADIUS);
            float y = AGENT_RADIUS + (float)rng.NextDouble() * (screenSize.Y - 2 * AGENT_RADIUS);
            foodItems.Add(new Food(new Vector2(x, y)));
        }

        List<Agent> agentList = agents.ToList();

        for (int i = 0; i < agents.Length; i++)
        {
            Agent agent = agents[i];

            agent.UpdateNeeds(dt);
            agent.DecideBehavior();
            agent.Behave(agentList, foodItems, dt);

            // Bounce logic
            bool bounced = false;
            agent.Position += agent.Velocity * dt;

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

            if (bounced)
            {
                agent.Color = BOUNCE_COLOR;
            }
        }

        foodItems.RemoveAll(f => f.IsEaten);

        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (Agent agent in agents)
        {
            DrawCircle(agent.Position, AGENT_RADIUS, agent.Color);
        }

        foreach (Food food in foodItems)
        {
            DrawCircle(food.Position, 3f, new Color(0f, 1f, 0f));
        }
    }
}
