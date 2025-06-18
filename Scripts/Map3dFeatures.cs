using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
	public string dataPath = "res://Scripts/citydata.txt";
	
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
	[Export]
	Globe _globe;

	[Export]
	float _pin_scale = 0.5f; // Scale for the pin asset

	[Export]
	PackedScene _pinPrefab;
	
	public override void _Ready() {
		// ===== LOAD MAP DATA =====
		GD.Print("Running map 3D features script.");
		CityMarkers cityData = new CityMarkers();

		foreach (City c in cityData.cities) {
			Node3D pinInstance = _pinPrefab.Instantiate<Node3D>();
			AddChild(pinInstance);
			
			pinInstance.Scale = new Vector3(_pin_scale, _pin_scale, _pin_scale);
			Globe.SurfacePoint point = _globe.GetSurfacePoint(new Vector2(c.latitude, c.longitude));
			GD.Print($"Placing pin for {c.name} at {point.Position}");
			pinInstance.LookAtFromPosition(point.Position, point.Position + point.Tangent, point.Normal);
		}
	}
	
	public override void _Process(double delta) {
	}
}
