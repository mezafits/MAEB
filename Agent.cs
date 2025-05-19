using Godot;
using System;
using System.Collections.Generic;
#region Agent Data Class
/// <summary>
/// Represents a single agent with needs, behaviors, and personality traits.
/// </summary>
public class Agent
{
	public Vector2 Position { get; set; }
	public Vector2 Velocity { get; set; }
	public Color Color { get; set; }
	public float Energy { get; set; } = 100f;
	public float Health { get; set; } = 100f;
	public float Hunger { get; set; }
	public float Horniness { get; set; }
	public float Age { get; set; } = 0f;
	public bool IsDead { get;  set; } = false;
	public float ReproductionCooldown { get; set; } = 0f;

	// Personality traits (0-1 scale)
	public float HungerResistance { get;  set; } // How well they resist hunger
	public float LibidoStrength { get;  set; }   // How strong their sex drive is
	public float EnergyEfficiency { get;  set; } // How efficiently they use energy
	public float Longevity { get;  set; }        // How long they can live

	// Need thresholds - different for each agent based on personality
	public float HungerThreshold;
	public float HorninessThreshold;
	public float TirednessThreshold;

	// Hunger/horniness increase rates
	public float HungerRate;
	public float HorninessRate;
	public float EnergyDrainRate;

	// Maximum age based on longevity
	public float MaxAge;

	// Base movement speed
	public float BaseSpeed;

	// Wandering behavior
	public Vector2 WanderTarget;
	public float WanderTimer = 0f;
	public const float WANDER_TIME = 5f;

	public bool IsHungry => Hunger >= HungerThreshold;
	public bool IsHorny => Horniness >= HorninessThreshold && ReproductionCooldown <= 0f;
	public bool IsTired => Energy <= TirednessThreshold;
	public bool IsStarving => Hunger >= 95f;

	public Agent(Vector2 position, Vector2 velocity, Color color, Random rng)
	{
		Position = position;
		Velocity = velocity;
		Color = color;

		InitializeTraits(rng);
		SetRandomWanderTarget(rng);
	}

	// Constructor for offspring
	public Agent(Vector2 position, Agent parent1, Agent parent2, Random rng)
	{
		Position = position;

		// Inherit traits with some mutation
		HungerResistance = MutateValue((parent1.HungerResistance + parent2.HungerResistance) / 2, 0.1f, rng);
		LibidoStrength = MutateValue((parent1.LibidoStrength + parent2.LibidoStrength) / 2, 0.1f, rng);
		EnergyEfficiency = MutateValue((parent1.EnergyEfficiency + parent2.EnergyEfficiency) / 2, 0.1f, rng);
		Longevity = MutateValue((parent1.Longevity + parent2.Longevity) / 2, 0.1f, rng);

		// Set derived values
		SetDerivedValues();

		// Initialize as a baby
		Age = 0f;
		Hunger = 30f;
		Horniness = 0f;
		Energy = 80f;
		Health = 100f;
		ReproductionCooldown = 30f; // Can't reproduce immediately

		// Create velocity
		float angle = (float)(rng.NextDouble() * Math.PI * 2);
		Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * BaseSpeed;

		// Mix colors from parents with some variation
		float r = MutateValue((parent1.Color.R + parent2.Color.R) / 2, 0.1f, rng);
		float g = MutateValue((parent1.Color.G + parent2.Color.G) / 2, 0.1f, rng);
		float b = MutateValue((parent1.Color.B + parent2.Color.B) / 2, 0.1f, rng);
		Color = new Color(r, g, b);

		SetRandomWanderTarget(rng);
	}

	public void InitializeTraits(Random rng)
	{
		// Initialize personality traits
		HungerResistance = 0.3f + (float)rng.NextDouble() * 0.7f;  // 0.3-1.0
		LibidoStrength = 0.2f + (float)rng.NextDouble() * 0.8f;    // 0.2-1.0
		EnergyEfficiency = 0.4f + (float)rng.NextDouble() * 0.6f;  // 0.4-1.0
		Longevity = 0.5f + (float)rng.NextDouble() * 0.5f;         // 0.5-1.0

		SetDerivedValues();

		// Randomize initial need levels
		Hunger = (float)rng.NextDouble() * 50f;
		Horniness = (float)rng.NextDouble() * 40f;
		Energy = 50f + (float)rng.NextDouble() * 50f;
		ReproductionCooldown = 0f;
	}

	public void SetDerivedValues()
	{
		// Set thresholds based on personality
		HungerThreshold = 50f + (HungerResistance * 40f);       // 50-90
		HorninessThreshold = 60f + ((1 - LibidoStrength) * 30f);  // 60-90 (inverse of libido)
		TirednessThreshold = 20f + (EnergyEfficiency * 20f);    // 20-40

		// Set rates based on personality
		HungerRate = 5f + (1 - HungerResistance) * 10f;          // 5-15
		HorninessRate = 2f + (LibidoStrength * 8f);            // 2-10
		EnergyDrainRate = 5f + (1 - EnergyEfficiency) * 8f;      // 5-13

		// Set max age based on longevity (between 60 and 120 seconds)
		MaxAge = 60f + (Longevity * 60f);

		// Set base speed (agents with higher energy efficiency move faster)
		BaseSpeed = 70f + (EnergyEfficiency * 80f);
	}

	public float MutateValue(float value, float mutationRate, Random rng)
	{
		float mutation = ((float)rng.NextDouble() * 2 - 1) * mutationRate;
		return Mathf.Clamp(value + mutation, 0f, 1f);
	}

	public void UpdateNeeds(float dt)
	{
		if (IsDead) return;

		// Update age and check for natural death
		Age += dt;
		if (Age >= MaxAge)
		{
			Die();
			return;
		}

		// Decrease reproduction cooldown
		if (ReproductionCooldown > 0)
			ReproductionCooldown -= dt;

		// Increase needs
		Hunger += HungerRate * dt;

		// Only increase horniness after maturity (30% of max age)
		if (Age > MaxAge * 0.3f)
			Horniness += HorninessRate * dt;

		Energy -= EnergyDrainRate * dt;

		// Cap values
		Hunger = MathF.Min(Hunger, 100f);
		Horniness = MathF.Min(Horniness, 100f);
		Energy = MathF.Max(Energy, 0f);

		// Health effects
		if (IsStarving)
		{
			Health -= 5f * dt;
			if (Health <= 0)
			{
				Die();
				return;
			}
		}
		else if (Health < 100f && !IsHungry)
		{
			// Recover health when not hungry
			Health += 2f * dt;
			Health = MathF.Min(Health, 100f);
		}

		// Wander timer
		WanderTimer -= dt;
		if (WanderTimer <= 0)
		{
			SetRandomWanderTarget(new Random());
		}
	}

	public void SetRandomWanderTarget(Random rng)
	{
		// Get a random point on screen (this will need to be adjusted based on screen size)
		float x = 50f + (float)rng.NextDouble() * 700f;
		float y = 50f + (float)rng.NextDouble() * 500f;
		WanderTarget = new Vector2(x, y);
		WanderTimer = WANDER_TIME + (float)rng.NextDouble() * 5f; // 5-10 seconds
	}

	public void Die()
	{
		IsDead = true;
		Color = new Color(0.3f, 0.3f, 0.3f); // Gray when dead
	}

	public void DecideBehavior()
	{
		if (IsDead) return;

		// Priority order: Hunger > Tiredness > Horniness > Wandering
		if (IsStarving)
			Color = new Color(1f, 0f, 0f); // Red when starving
		else if (IsHungry)
			Color = new Color(1f, 1f, 0f); // Yellow
		else if (IsTired)
			Color = new Color(0.5f, 0.5f, 1f); // Blue
		else if (IsHorny)
			Color = new Color(1f, 0f, 1f); // Magenta
		else
			Color = new Color(0f, 1f, 0f); // Green
	}

	public Agent Behave(List<Agent> agents, List<Food> foodList, float dt, Random rng)
	{
		if (IsDead) return null;

		Agent newAgent = null;

		// Behavior priority follows the same natural order
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
					Energy += 20f;
				}
			}
			else
			{
				// If no food is found, wander to look for food
				Wander(dt);
			}
		}
		else if (IsTired)
		{
			// Slow down when tired
			Velocity *= 0.95f;
			Energy += 30f * dt;
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
					// Reproduce
					Horniness -= 60f;
					mate.Horniness -= 60f;

					// Set cooldown
					ReproductionCooldown = 20f;
					mate.ReproductionCooldown = 20f;

					// Create new agent
					Vector2 birthPos = new Vector2(
						(Position.X + mate.Position.X) / 2 + ((float)rng.NextDouble() * 20 - 10),
						(Position.Y + mate.Position.Y) / 2 + ((float)rng.NextDouble() * 20 - 10)
					);

					newAgent = new Agent(birthPos, this, mate, rng);

					// Reproduction costs energy
					Energy -= 10f;
					mate.Energy -= 10f;
				}
			}
			else
			{
				// If no mate is found, wander to look for mates
				Wander(dt);
			}
		}
		else
		{
			// Wander when no pressing needs
			Wander(dt);
		}

		return newAgent;
	}

	public void Wander(float dt)
	{
		if (Position.DistanceTo(WanderTarget) < 20f)
		{
			// Reached target, get a new one
			SetRandomWanderTarget(new Random());
		}
		else
		{
			// Move toward wander target
			Seek(WanderTarget, dt);
		}
	}

	public void Seek(Vector2 target, float dt)
	{
		Vector2 direction = (target - Position).Normalized();
		float speed = BaseSpeed;

		// Adjust speed based on state
		if (IsTired) speed *= 0.6f;
		if (IsStarving) speed *= 0.4f;

		Velocity = direction * speed;
	}

	public Food FindNearestFood(List<Food> foodList)
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

	public Agent FindMate(List<Agent> agents)
	{
		foreach (var other in agents)
		{
			if (other == this || other.IsDead) continue;

			// Check if other agent is mature, horny and close enough
			if (other.IsHorny &&
				other.Age > other.MaxAge * 0.3f &&
				Position.DistanceTo(other.Position) < 150f)
			{
				return other;
			}
		}
		return null;
	}
}
