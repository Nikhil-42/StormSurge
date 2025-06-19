using Godot;
using System.Collections.Generic;

public partial class Storm : Node3D
{
	// Storm Particle Params
	[Export] public int NumParticles = 750;
	[Export] public float SpiralRadius = 25.0f;
	[Export] public float SpiralTurns = 18.0f;
	[Export] public float EyeRadius = 4.0f;
	[Export] public PackedScene ParticleScene;
	[Export] public float RotationSpeed = 2.0f;

	// Globe Params
	[Export] public float EarthRadius = 10.25f;
	[Export] public Vector3 GlobeCenter = Vector3.Zero;

	// Spacing Params for coordinate transformation
	[Export] public float DegreesPerUnitX = 0.1f;
	[Export] public float DegreesPerUnitY = 0.1f;

	// Movement Params
	[Export] public float MoveSpeedDegPerSec = 2.0f;
	[Export] public Vector2 MoveDirection = new Vector2(1, 0);

	// Internal state
	private List<Dictionary<string, object>> storms = new List<Dictionary<string, object>>();

	public override void _Ready()
	{
		// Currently has a hardcoded move direction (needs to be updated to be dynamic later)
		MoveDirection = MoveDirection.Normalized();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent
			&& mouseEvent.Pressed
			&& mouseEvent.ButtonIndex == MouseButton.Right)
		{
			var camera = GetViewport().GetCamera3D();
			if (camera == null)
				return;

			var mousePos = mouseEvent.Position;
			var from = camera.ProjectRayOrigin(mousePos);
			var dir = camera.ProjectRayNormal(mousePos);
			var to = from + dir * 1000.0f;

			var result = Geometry3D.SegmentIntersectsSphere(from, to, GlobeCenter, EarthRadius);
			if (result != null && result.Length > 0)
			{
				var hitPoint = result[0];
				var latlon = Vector3ToLatLon(hitPoint - GlobeCenter);
				SpawnStormAt(latlon.X, latlon.Y);
			}
		}
	}

	private void SpawnStormAt(float lat, float lon)
	{
		var stormParticles = new List<Node3D>();
		var initialOffsets = new List<Vector2>();
		float rotationAngle = 0.0f;

		int spawned = 0;
		int attempts = 0;
		while (spawned < NumParticles && attempts < NumParticles * 3)
		{
			float t = (float)attempts / NumParticles;
			float angle = t * SpiralTurns * Mathf.Tau;
			float radius = t * SpiralRadius;
			float x = Mathf.Cos(angle) * radius;
			float z = Mathf.Sin(angle) * radius;
			attempts++;

			if (Mathf.Sqrt(x * x + z * z) < EyeRadius)
				continue;

			var instance = ParticleScene.Instantiate<Node3D>();
			instance.Visible = true;
			instance.Position = Vector3.Zero;
			AddChild(instance);

			stormParticles.Add(instance);
			initialOffsets.Add(new Vector2(x, z));
			spawned++;
		}

		var storm = new Dictionary<string, object>
		{
			{ "origin_lat", lat },
			{ "origin_lon", lon },
			{ "rotation_angle", rotationAngle },
			{ "particles", stormParticles },
			{ "offsets", initialOffsets },
			{ "strength", 1.0f }, // 1.0 = full strength, 0.0 = dead
			{ "fade_timer", 0.0f } // time since fade started
		};

		storms.Add(storm);
		GD.Print($"Spawned storm with {spawned} particles in {attempts} attempts");
	}

	public override void _Process(double delta)
	{
		var FPS = Engine.GetFramesPerSecond();
		GetTree().Root.Title = $"Storms: {storms.Count} | FPS: {FPS}";

		var stormsToRemove = new List<Dictionary<string, object>>();

		foreach (var storm in storms)
		{
			float rotationAngle = (float)storm["rotation_angle"] + RotationSpeed * (float)delta;
			storm["rotation_angle"] = rotationAngle;

			float originLat = (float)storm["origin_lat"];
			float originLon = (float)storm["origin_lon"];

			var moveDelta = MoveDirection * MoveSpeedDegPerSec * (float)delta;
			originLon += moveDelta.X;
			originLat += moveDelta.Y;
			originLat = Mathf.Clamp(originLat, -89.9f, 89.9f);

			storm["origin_lat"] = originLat;
			storm["origin_lon"] = originLon;

			var particles = (List<Node3D>)storm["particles"];
			var offsets = (List<Vector2>)storm["offsets"];

			// --- Storm strength and fading ---
			float strength = (float)storm["strength"];
			if (strength > 0.0f)
			{
				strength -= 0.05f * (float)delta; // Deplete rate (This whole block will be replaced with a more complex system later)
				if (strength < 0.0f)
					strength = 0.0f;
				storm["strength"] = strength;
			}

			if (strength < 0.2f)
			{
				// Some fade effect will need to be applied/created

				float fade_timer = (float)storm["fade_timer"];
				fade_timer += (float)delta;
				storm["fade_timer"] = fade_timer;
				float fade = Mathf.Clamp(1.0f - fade_timer / 2.0f, 0.0f, 1.0f); // 2 seconds fade

				// Delete particles if fade is complete
				if (fade <= 0.0f)
				{
					foreach (var p in particles)
						if (IsInstanceValid(p)) p.QueueFree();
					stormsToRemove.Add(storm);
					GD.Print($"Removed storm");
					continue;
				}
			}

			// --- Particle positioning ---
			for (int i = 0; i < particles.Count; i++)
			{
				var offset = offsets[i].Rotated(rotationAngle);
				float lat = originLat + offset.Y * DegreesPerUnitY;
				float lon = originLon + (offset.X * DegreesPerUnitX) / Mathf.Cos(Mathf.DegToRad(lat));
				var globePos = LatLonToVector3(lat, lon, EarthRadius);
				particles[i].GlobalPosition = GlobeCenter + globePos;
			}
		}

		foreach (var s in stormsToRemove)
			storms.Remove(s);
}


	// Helpers for coordinate transformations
	private Vector3 LatLonToVector3(float latDeg, float lonDeg, float radius)
	{
		float lat = Mathf.DegToRad(latDeg);
		float lon = Mathf.DegToRad(lonDeg);
		float x = radius * Mathf.Cos(lat) * Mathf.Sin(lon);
		float y = radius * Mathf.Sin(lat);
		float z = radius * Mathf.Cos(lat) * Mathf.Cos(lon);
		return new Vector3(x, y, z);
	}

	private Vector2 Vector3ToLatLon(Vector3 pos)
	{
		float r = pos.Length();
		if (r == 0)
			return Vector2.Zero;
		float lat = Mathf.Asin(pos.Y / r);
		float lon = Mathf.Atan2(pos.X, pos.Z);
		return new Vector2(Mathf.RadToDeg(lat), Mathf.RadToDeg(lon));
	}
}
