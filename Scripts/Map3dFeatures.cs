using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Asset {
	public string path;
	public Vector3 scale;
	public Asset(string p, float s) {
		path = p;
		scale = new Vector3(s, s, s);
	}
}

public struct City {
	public string region;
	public string name;
	public int population;
	public int elevation;
	public float latitude;
	public float longitude;
	
	public City(string r, string n, int p, int e, float lat, float lon) {
		region = r;
		name = n;
		population = p;
		elevation = e;
		latitude = lat;
		longitude = lon;
	}
	
	public void printInfo() {
		GD.Print($"Name: {name}, Population: {population}, Latitude: {latitude}, Longitude: {longitude}, Elevation: {elevation}");
	}
}

public class CityMarkers {
	public List<City> cities = new List<City>();
	public string dataPath = "res://Library/citydata.txt";
	
	private static Regex linePattern = new Regex("^\"([^\"]+)\"\\s+(\\S+(?:\\s+\\S+)*)\\s+(\\d+)\\s+([-+]?[0-9]*\\.?[0-9]+)\\s+([-+]?[0-9]*\\.?[0-9]+)\\s+(\\d+)$");
	
	public CityMarkers() {
		if (!FileAccess.FileExists(dataPath))
		{
			GD.PrintErr($"File not found: {dataPath}");
			return;
		}

		using FileAccess cityFile = FileAccess.Open(dataPath, FileAccess.ModeFlags.Read);

		while (!cityFile.EofReached()) {
			var line = cityFile.GetLine().Trim();
			if (string.IsNullOrEmpty(line)) 
				continue;

			var match = linePattern.Match(line);
			if (match.Success) {
				var current = new City
				{
					name = match.Groups[1].Value,
					region = match.Groups[2].Value,
					population = int.Parse(match.Groups[3].Value),
					latitude = float.Parse(match.Groups[4].Value),
					longitude = float.Parse(match.Groups[5].Value),
					elevation = int.Parse(match.Groups[6].Value)
				};
				cities.Add(current);
			}
			else {
				GD.PrintErr($"Could not parse line: {line}");
			}
		}
		GD.Print("Finished parsing city data.");
		cityFile.Close();
	}
}

public partial class Map3dFeatures : Node3D
{
	float mapWidth = 28.9f;
	float mapHeight = 14.04f;
	Asset pin = new Asset("res://Assets/pin.gltf", 0.5f);
	
	public override void _Ready() {
		// ===== LOAD MAP DATA =====
		GD.Print("Running map 3D features script.");
		CityMarkers cityData = new CityMarkers();

		// ===== MAP PIN ASSET =====
		var pinPrefab = ResourceLoader.Load<PackedScene>(pin.path);
		if (pinPrefab == null) {
			GD.PrintErr($"Failed to load asset at: {pin.path}");
			return;
		}

		foreach (City c in cityData.cities) {
			Node3D pinInstance = pinPrefab.Instantiate<Node3D>();
			AddChild(pinInstance);
			
			pinInstance.Scale = pin.scale;
			Vector3 position = new Vector3((c.longitude*mapWidth)/360f, 0, (c.latitude)*mapHeight/-180f);
			pinInstance.Position = position;
		}
	}
	
	public override void _Process(double delta) {
	}
}
