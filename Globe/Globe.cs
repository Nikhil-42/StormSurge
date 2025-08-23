using Godot;

namespace StormSurge;
public partial class Globe : Node3D {
    public float WaterLevel {
        get => _waterPercent * Utils.EVEREST_HEIGHT;
        set {
            _waterPercent = value / Utils.EVEREST_HEIGHT;
            _globeMaterial.SetShaderParameter("water_level", Mathf.Max(0.01, _waterPercent));
        }
    } // Water level in meters

    [Export]
    private Texture2D _regionmap;

    public float Radius => ((SphereMesh)_globe.Mesh).Radius;

    private MeshInstance3D _globe;
    private ShaderMaterial _globeMaterial;
    private MeshInstance3D _clouds;
    private DirectionalLight3D _sun;
    private WorldEnvironment _environment;
    private Texture2D _heightmap;
    private Image _regionmapImage;

    private float _waterPercent;  // Percentange of the height of Everest that the water level is at

    private bool IsTutorialMode; // Tutorial mode

    internal struct SurfacePoint {
        public Vector2 LatLon;
        public Vector3 Position;
        public Vector3 Normal;
        public readonly Vector2 UV => new((LatLon.Y / Mathf.Tau) + 0.5f, (LatLon.X / Mathf.Pi) + 0.5f); // Placeholder for UV mapping, can be calculated based on LatLon)
        public readonly Vector3 Tangent => Normal.Cross(Vector3.Up).Normalized();
        public readonly Vector3 Bitangent => -Tangent.Cross(Normal);
    }

    public override void _Ready() {
        _globe = GetNode<MeshInstance3D>("GlobeMap");
        _globeMaterial = (ShaderMaterial)_globe.MaterialOverride;
        _clouds = GetNode<MeshInstance3D>("GlobeMap/Clouds");
        _sun = GetNode<DirectionalLight3D>("Sun");
        _environment = GetNode<WorldEnvironment>("WorldsUgliestSkyBox");
        _regionmapImage = _regionmap.GetImage();
    }

    public override void _Process(double delta) {
        Transform = Transform.Rotated(Vector3.Up, (float)delta * 0.1f);

        RegionAI[] regionAIs = GameManager.Instance.Game.RegionAIs;
        float[] health = new float[regionAIs.Length];
        for (int i = 0; i < regionAIs.Length; i++) {
            health[i] = regionAIs[i].Health;
        }
        _globeMaterial.SetShaderParameter("health", health);
    }


    public Vector2 GetLatLon(Vector3 worldPosition) {
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
    internal SurfacePoint GetSurfacePoint(Vector2 latLon) {
        Vector2 latLonNormalized = Utils.NormalizeLatLon(latLon);
        float lat = latLonNormalized.X; // Latitude in radians
        float lon = latLonNormalized.Y; // Longitude in radians

        // Calculate the position on the globe's surface
        float radius = ((SphereMesh)_globe.Mesh).Radius;
        float x = radius * Mathf.Cos(lat) * Mathf.Cos(-lon);
        float y = radius * Mathf.Sin(lat);
        float z = radius * Mathf.Cos(lat) * Mathf.Sin(-lon);

        Vector3 position = ToGlobal(new Vector3(x, y, z));

        SurfacePoint point = new() {
            LatLon = latLonNormalized,
            Position = position,
            Normal = (new Vector3(x, y, z) - Position).Normalized(),
        };
        return point;
    }

    public int GetRegionID(Vector2 latLon) {
        // Convert latitude and longitude to a point on the region map
        SurfacePoint point = GetSurfacePoint(latLon);
        Vector2I pointi = new((int)(point.UV.X * _regionmapImage.GetWidth()), (int)((1.0f - point.UV.Y) * _regionmapImage.GetHeight()));
        int id = Mathf.RoundToInt(_regionmapImage.GetPixelv(pointi).R * 256);
        return id - 1;
    }

    public override void _Input(InputEvent @event) {
        // Block Input in tutorial
        if (IsTutorialMode) {
            return;
        }

        if (@event is InputEventMouseButton mouseEvent
            && mouseEvent.Pressed
            && mouseEvent.ButtonIndex == MouseButton.Right) {
            Camera3D camera = GetViewport().GetCamera3D();
            if (camera == null) {
                return;
            }

            Vector2 mousePos = mouseEvent.Position;
            Vector3 from = camera.ProjectRayOrigin(mousePos);
            Vector3 dir = camera.ProjectRayNormal(mousePos);
            Vector3 to = from + (dir * 1000.0f);

            Vector3[] result = Geometry3D.SegmentIntersectsSphere(from, to, Position, Radius);
            if (result != null && result.Length > 0) {
                Vector3 hitPoint = result[0];
                int id = GetRegionID(GetLatLon(hitPoint));
                _globeMaterial.SetShaderParameter("selected_region", (uint)(id + 1));

                // Show region stats popup
                GameManager gameManager = GameManager.Instance;
                if (gameManager != null) {
                    RegionAI region = gameManager.GetRegion(id);

                    if (region != null) {
                        GameManager.Instance?.PlayRegionSelectSound();
                    }

                    RegionStatsPopup popup = GetNode<RegionStatsPopup>("../RegionStatsPopup");
                    popup?.ShowRegionStats(region);
                }
            }
        }
    }
}