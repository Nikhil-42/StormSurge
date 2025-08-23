using Godot;

namespace StormSurge;
// Utils.cs
// Contains utility functions for the game, such as distance calculations and normalization.
internal static class Utils {
    public const float EARTH_RADIUS = 6378.137f; // Earth's radius in kilometers
    public const float EVEREST_HEIGHT = 8848.86f; // Height of Mount Everest in meters

    public static float HaversineDistance(Vector2 start, Vector2 end, float radius = EARTH_RADIUS) {
        // Convert degrees to radians
        float lat1 = start.X;
        float lon1 = start.Y;
        float lat2 = end.X;
        float lon2 = end.Y;

        // Haversine formula
        float dlon = lon2 - lon1;
        float dlat = lat2 - lat1;
        float a = Mathf.Pow(Mathf.Sin(dlat / 2), 2) +
                  Mathf.Cos(lat1) * Mathf.Cos(lat2) * Mathf.Pow(Mathf.Sin(dlon / 2), 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return radius * c; // Distance in kilometers
    }



    public static Vector2 NormalizeLatLon(Vector2 latLon) {
        float lat = latLon.X;
        float lon = latLon.Y;

        // Wrap longitude to [-π, π]
        lon = ((lon + Mathf.Pi) % Mathf.Tau + Mathf.Tau) % Mathf.Tau - Mathf.Pi;

        // Normalize latitude to [-π/2, π/2], flipping over-pole values
        if (lat > Mathf.Pi / 2) {
            lat = Mathf.Pi - lat;
            lon += Mathf.Pi;
        } else if (lat < -Mathf.Pi / 2) {
            lat = -Mathf.Pi - lat;
            lon += Mathf.Pi;
        }

        // Wrap longitude again in case it was flipped
        lon = ((lon + Mathf.Pi) % Mathf.Tau + Mathf.Tau) % Mathf.Tau - Mathf.Pi;

        return new Vector2(lat, lon);
    }
}