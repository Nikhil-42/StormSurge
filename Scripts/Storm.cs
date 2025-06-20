using Godot;
using System.Collections.Generic;

public partial class Storm : Node3D
{
	[Export]
	private Globe _globe;

	// Storm Particle Params
	[Export] public int NumParticles = 750;
	[Export] public float SpiralRadius = 25.0f;
	[Export] public float SpiralTurns = 18.0f;
	[Export] public float EyeRadius = 4.0f;
	[Export] public PackedScene ParticleScene;
	[Export] public float RotationSpeed = 2.0f;

	// Spacing Params for coordinate transformation
	[Export] public float RadPerUnitX = 0.1f;
	[Export] public float RadPerUnitY = 0.1f;

	// Movement Params
	[Export] public float MoveSpeedRadPerSec = 2.0f;
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

			var result = Geometry3D.SegmentIntersectsSphere(from, to, _globe.Position, _globe.Radius);
			if (result != null && result.Length > 0)
			{
				var hitPoint = result[0];
				var latlon = _globe.GetLatLon(hitPoint);
				var regionID = _globe.GetRegionID(latlon);
				if (regionID == 0)
				{
					SpawnStormAt(latlon.X, latlon.Y);
				}
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

			var moveDelta = MoveDirection * MoveSpeedRadPerSec * (float)delta;
			originLon += moveDelta.X;
			originLat += moveDelta.Y;

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
				float lat = originLat + offset.Y * RadPerUnitY;
				float lon = originLon + offset.X * RadPerUnitX / Mathf.Cos(lat);
				var globePoint = _globe.GetSurfacePoint(new(lat, lon));
				particles[i].GlobalPosition = 1.025f * (globePoint.Position - _globe.Position) + _globe.Position;
			}

			// --- Damage Application ---
			GD.Print($"Storm at ({originLat}, {originLon}) with strength {strength}, is affecting {_globe.GetRegionID(new(originLat, originLon))}");
		}

		foreach (var s in stormsToRemove)
			storms.Remove(s);
	}
}
