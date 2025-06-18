using Godot;


public partial class Globe : Node3D
{
	private MeshInstance3D _globe;
	private MeshInstance3D _clouds;
	private DirectionalLight3D _sun;
	private WorldEnvironment _environment;
	private Texture2D _heightmap;

	public struct SurfacePoint
	{
		public Vector2 LatLon;
		public Vector3 Position;
		public Vector3 Normal;
		public readonly Vector3 Tangent => Normal.Cross(Vector3.Up).Normalized();
		public readonly Vector3 Bitangent => -Tangent.Cross(Normal);
	}

	public override void _Ready()
	{
		_globe = GetNode<MeshInstance3D>("GlobeMap");
		_clouds = GetNode<MeshInstance3D>("GlobeMap/Clouds");
		_sun = GetNode<DirectionalLight3D>("Sun");
		_environment = GetNode<WorldEnvironment>("WorldsUgliestSkyBox");
	}

	public SurfacePoint GetSurfacePoint(Vector2 latLon)
	{
		// Convert latitude and longitude to radians
		float latRad = Mathf.DegToRad(latLon.X);
		float lonRad = -Mathf.DegToRad(latLon.Y);

		// Calculate the position on the globe's surface
		float radius = ((SphereMesh)_globe.Mesh).Radius;
		float x = radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);
		float y = radius * Mathf.Sin(latRad);
		float z = radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);

		Vector3 position = ToGlobal(new Vector3(x, y, z));

		SurfacePoint point = new()
		{
			LatLon = latLon,
			Position = position,
			Normal = (new Vector3(x, y, z) - Position).Normalized(),
		};
		return point;
	}
	
	public float GetRadius()
	{
		if (_globe.Mesh is SphereMesh sphere)
			return sphere.Radius;
		return 1.0f;
	}

	public static Vector2 Vector3ToLatLon(Vector3 pos)
	{
		float r = pos.Length();
		if (r == 0)
			return Vector2.Zero;
		float lat = Mathf.Asin(pos.Y / r);
		float lon = Mathf.Atan2(pos.X, pos.Z);
		return new Vector2(Mathf.RadToDeg(lat), Mathf.RadToDeg(lon));
	}
}
