using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class MapRenderer: Node2D
{
	[Export] public int TileSize = 8;

	private List<char> decodedMap = new();
	private int mapWidth = 100;

	private Dictionary<char, Color> tileColors = new()
	{
		{ '#', new Color(0.2f, 0.2f, 0.2f) }, // Wall
		{ '.', new Color(0.8f, 0.8f, 0.8f) }, // Floor
	};

	public void LoadMapFromJson(string jsonString)
	{
		var doc = JsonDocument.Parse(jsonString);
		var root = doc.RootElement;

		string rle = root.GetProperty("map").GetString();
		mapWidth = root.GetProperty("width").GetInt32();

		// Optional: Load legend for future color extensions
		if (root.TryGetProperty("legend", out JsonElement legend))
		{
			foreach (var kv in legend.EnumerateObject())
			{
				char symbol = kv.Name[0];
				if (!tileColors.ContainsKey(symbol))
				{
					tileColors[symbol] = Colors.Gray; // Default for unknown
				}
			}
		}

		decodedMap = DecodeRLE(rle);
		QueueRedraw();
	}

	public override void _Draw()
	{
		for (int i = 0; i < decodedMap.Count; i++)
		{
			char tile = decodedMap[i];
			if (!tileColors.ContainsKey(tile)) continue;

			int x = i % mapWidth;
			int y = i / mapWidth;
			DrawRect(new Rect2(x * TileSize, y * TileSize, TileSize, TileSize), tileColors[tile]);
		}
	}

	private List<char> DecodeRLE(string data)
	{
		List<char> output = new();
		int i = 0;
		while (i < data.Length)
		{
			if (data[i] == '~' && i + 2 < data.Length)
			{
				char tile = data[i + 1];
				i += 2;
				string countStr = "";
				while (i < data.Length && char.IsDigit(data[i]))
				{
					countStr += data[i++];
				}
				int count = int.Parse(countStr);
				output.AddRange(new string(tile, count));
			}
			else
			{
				output.Add(data[i++]);
			}
		}
		return output;
	}
}
