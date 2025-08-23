using Godot;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace StormSurge;
internal readonly struct City(string region, string name, int population, float latitude, float longitude, int elevation) {
    public readonly string region = region;
    public readonly string name = name;
    public readonly int population = population;
    public readonly int elevation = elevation;
    public readonly float latitude = latitude;
    public readonly float longitude = longitude;
}

internal partial class CityMarkers {
    private const string DATA_PATH = "res://Library/citydata.txt";
    private static readonly Regex LINE_PATTERN = CitiesRegex();
    public readonly List<City> cities = [];


    public CityMarkers() {
        if (!FileAccess.FileExists(DATA_PATH)) {
            GD.PrintErr($"File not found: {DATA_PATH}");
            return;
        }

        using FileAccess cityFile = FileAccess.Open(DATA_PATH, FileAccess.ModeFlags.Read);

        while (!cityFile.EofReached()) {
            string line = cityFile.GetLine().Trim();

            if (string.IsNullOrEmpty(line)) {
                continue;
            }

            Match match = LINE_PATTERN.Match(line);
            if (match.Success) {
                City current = new(
                    match.Groups[1].Value,
                    match.Groups[2].Value,
                    int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                    float.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
                    float.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture)
                );
                cities.Add(current);
            } else {
                GD.PrintErr($"Could not parse line: {line}");
            }
        }
        cityFile.Close();
    }


    [GeneratedRegex("^\"([^\"]+)\"\\s+(\\S+(?:\\s+\\S+)*)\\s+(\\d+)\\s+([-+]?[0-9]*\\.?[0-9]+)\\s+([-+]?[0-9]*\\.?[0-9]+)\\s+(\\d+)$")]
    private static partial Regex CitiesRegex();
}

public partial class Map3dFeatures : Node3D {
    [Export]
    private float _pin_scale = 0.5f; // Scale for the pin asset

    [Export]
    private PackedScene _pinPrefab;

    public override void _Ready() {
        CityMarkers cityData = new();

        foreach (City c in cityData.cities) {
            Node3D pinInstance = _pinPrefab.Instantiate<Node3D>();
            AddChild(pinInstance);

            pinInstance.Scale = new Vector3(_pin_scale, _pin_scale, _pin_scale);
            Globe.SurfacePoint point = GameManager.Instance.Globe.GetSurfacePoint(new Vector2(Mathf.DegToRad(c.latitude), Mathf.DegToRad(c.longitude)));
            pinInstance.LookAtFromPosition(point.Position, point.Position + point.Tangent, point.Normal);
        }
    }

    public override void _Process(double delta) {
    }
}