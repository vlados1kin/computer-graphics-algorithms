using System.Numerics;
using System.Windows.Media;

namespace Main.Models;

public class Light
{
    public Vector3 Position { get; set; } = new(1, 1, 2);

    public Vector3 Color { get; set; } = Vector3.One;

    public float Intensity { get; set; } = 1.0f;

    public static Vector3 ApplyPhongShading(
        List<Light> lights,
        Vector3 normal,
        Vector3 viewDirection,
        Vector3 fragWorld, Vector3 ambientColor, Vector3 ka,
        Vector3 diffuseColor, Vector3 kd, Vector3 specularColor, float shininess)
    {
        var ambient = ambientColor * ka;
        var lighting = ambient;

        foreach (var light in lights)
        {
            var lightDir = Vector3.Normalize(light.Position - fragWorld);

            var diff = MathF.Max(Vector3.Dot(normal, lightDir), 0);
            var diffuse = light.Color * diffuseColor * diff * kd;

            var reflectDir = Vector3.Reflect(-lightDir, normal);
            var spec = MathF.Pow(MathF.Max(Vector3.Dot(viewDirection, reflectDir), 0), shininess);
            var specular = light.Color * specularColor * spec;

            lighting += (diffuse + specular) * light.Intensity;
        }

        lighting = Vector3.Clamp(lighting, Vector3.Zero, new Vector3(255, 255, 255));

        return lighting;
    }

    public static Color ApplyLambert(List<Light> lambertLights, Vector3 normal, Color baseColor)
    {
        var totalIntensity = 0f;
        foreach (var light in lambertLights)
        {
            var lightDir = Vector3.Normalize(light.Position);

            var intensity = MathF.Max(Vector3.Dot(normal, lightDir), 0);

            totalIntensity += intensity;
        }

        totalIntensity = MathF.Min(totalIntensity, 1.0f);

        return System.Windows.Media.Color.FromArgb(
            baseColor.A,
            (byte)(baseColor.R * totalIntensity),
            (byte)(baseColor.G * totalIntensity),
            (byte)(baseColor.B * totalIntensity));
    }
}