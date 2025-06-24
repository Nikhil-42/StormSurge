using Godot;
using Godot.Collections;

public partial class Globe : Node3D
{
	[Export]
	private Json _regions;

	[Export]
	private Texture2D _regionmap;

	public float Radius => ((SphereMesh)_globe.Mesh).Radius;

	private MeshInstance3D _globe;
	private MeshInstance3D _clouds;
	private DirectionalLight3D _sun;
	private WorldEnvironment _environment;
	private Texture2D _heightmap;
	private Image _regionmapImage;

	public struct SurfacePoint
	{
		public Vector2 LatLon;
		public Vector3 Position;
		public Vector3 Normal;
		public readonly Vector2 UV => new(LatLon.Y / Mathf.Tau + 0.5f, LatLon.X / Mathf.Pi + 0.5f); // Placeholder for UV mapping, can be calculated based on LatLon)
		public readonly Vector3 Tangent => Normal.Cross(Vector3.Up).Normalized();
		public readonly Vector3 Bitangent => -Tangent.Cross(Normal);
	}

	public override void _Ready()
	{
		_globe = GetNode<MeshInstance3D>("GlobeMap");
		_clouds = GetNode<MeshInstance3D>("GlobeMap/Clouds");
		_sun = GetNode<DirectionalLight3D>("Sun");
		_environment = GetNode<WorldEnvironment>("WorldsUgliestSkyBox");
		_regionmapImage = _regionmap.GetImage();
	}

	public override void _Process(double delta)
	{
		Transform.Rotated(Vector3.Up, (float)delta * 0.1f);
	}

	Vector2 NormalizeLatLon(Vector2 latLon)
	{
		float lat = latLon.X;
		float lon = latLon.Y;

		// Wrap longitude to [-π, π]
		lon = ((lon + Mathf.Pi) % Mathf.Tau + Mathf.Tau) % Mathf.Tau - Mathf.Pi;

		// Normalize latitude to [-π/2, π/2], flipping over-pole values
		if (lat > Mathf.Pi / 2)
		{
			lat = Mathf.Pi - lat;
			lon += Mathf.Pi;
		}
		else if (lat < -Mathf.Pi / 2)
		{
			lat = -Mathf.Pi - lat;
			lon += Mathf.Pi;
		}

		// Wrap longitude again in case it was flipped
		lon = ((lon + Mathf.Pi) % Mathf.Tau + Mathf.Tau) % Mathf.Tau - Mathf.Pi;

		return new Vector2(lat, lon);
	}


	public Vector2 GetLatLon(Vector3 worldPosition)
	{
		// Convert world position to latitude and longitude
		Vector3 localPosition = ToLocal(worldPosition).Normalized();

		float lat = Mathf.Asin(localPosition.Y);
		float lon = -Mathf.Atan2(localPosition.Z, localPosition.X);

		Vector2 latLon = new(lat, lon);

		return latLon;
	}

	/// <summary>
	/// Converts latitude and longitude to a point on the globe's surface.
	/// </summary>
	/// <param name="latLon">[latitdude, longitude] (rad)</param>
	/// <returns>A SurfacePoint containing position, and normal information</returns>
	public SurfacePoint GetSurfacePoint(Vector2 latLon)
	{
		var latLonNormalized = NormalizeLatLon(latLon);
		var lat = latLonNormalized.X; // Latitude in radians
		var lon = latLonNormalized.Y; // Longitude in radians

		// Calculate the position on the globe's surface
		float radius = ((SphereMesh)_globe.Mesh).Radius;
		float x = radius * Mathf.Cos(lat) * Mathf.Cos(-lon);
		float y = radius * Mathf.Sin(lat);
		float z = radius * Mathf.Cos(lat) * Mathf.Sin(-lon);

		Vector3 position = ToGlobal(new Vector3(x, y, z));

		SurfacePoint point = new()
		{
			LatLon = latLonNormalized,
			Position = position,
			Normal = (new Vector3(x, y, z) - Position).Normalized(),
		};
		return point;
	}

	public int GetRegionID(Vector2 latLon)
	{
		// Convert latitude and longitude to a point on the region map
		var point = GetSurfacePoint(latLon);
		Vector2I pointi = new((int)(point.UV.X * _regionmapImage.GetWidth()), (int)((1.0f - point.UV.Y) * _regionmapImage.GetHeight()));
		var id = Mathf.RoundToInt(_regionmapImage.GetPixelv(pointi).R * 256);
		return id;
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
			var to = from + (dir * 1000.0f);

			var result = Geometry3D.SegmentIntersectsSphere(from, to, Position, Radius);
			if (result != null && result.Length > 0)
			{
				var hitPoint = result[0];
				var id = GetRegionID(GetLatLon(hitPoint));
				((ShaderMaterial)_globe.MaterialOverride).SetShaderParameter("selected_region", (uint)id);
				GD.Print("Region: ", ((Array)((Dictionary)_regions.Data)["names"])[id]);
			}
		}
	}
}
