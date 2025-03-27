using System.Numerics;
using System.Windows.Media;
using Parser.Models;

namespace Parser.Raster.Extensions;

public static class ColorExtensions
{
    public static int ColorToIntBgra(this Color color)
    {
        return (color.B << 0) | (color.G << 8) | (color.R << 16) | (color.A << 24);
    }
    
    public static Color ApplyLambert(this Color baseColor, Vector3 normal, List<Light> lambertLights)
    {
        var totalIntensity = 0f;
        foreach (var light in lambertLights)
        {
            var lightDir = Vector3.Normalize(light.Direction);
            var intensity = MathF.Max(Vector3.Dot(normal, lightDir), 0);
            totalIntensity += intensity;
        }

        totalIntensity = MathF.Min(totalIntensity, 1.0f);

        return Color.FromArgb(baseColor.A, (byte)(baseColor.R * totalIntensity), (byte)(baseColor.G * totalIntensity), (byte)(baseColor.B * totalIntensity));
    }
}