using System.Numerics;
using System.Windows.Media;

namespace Parser.Models;

public class Light
{
    public Vector3 Direction { get; set; } = new(1, 2, 1);
    
    public Color Ambient { get; set; } = Colors.White;

    public Color Diffuse { get; set; } = Colors.HotPink;

    public Color Specular { get; set; } = Colors.Purple;
    
    public float Ka { get; set; } = 0.1f;
    
    public float Kd { get; set; } = 1.0f;

    public float Ks { get; set; } = 0.2f;
    
    public float LikeADiamond { get; set; } = 32f;
    
    public static Color ApplyPhongShading(List<Light> lights, Vector3 normal, Vector3 viewDirection, Vector3 fragWorld)
    {
        var ambient = Vector3.Zero;
        var diffuse = Vector3.Zero;
        var specular = Vector3.Zero;
        
        foreach (var light in lights)
        {
            ambient += new Vector3(light.Ambient.R, light.Ambient.G, light.Ambient.B) * light.Ka;
            var lightDir = Vector3.Normalize(light.Direction - fragWorld);
            var ndotL = MathF.Max(Vector3.Dot(normal, lightDir), 0);
            diffuse += new Vector3(light.Diffuse.R, light.Diffuse.G, light.Diffuse.B) * ndotL * light.Kd;
            var reflection = Vector3.Reflect(-lightDir, normal); 
            var rdotV = MathF.Max(Vector3.Dot(reflection, viewDirection), 0);
            if (rdotV > 0) specular += new Vector3(light.Specular.R, light.Specular.G, light.Specular.B) * light.Ks * MathF.Pow(rdotV, light.LikeADiamond);
        }
        
        var phong = Vector3.Clamp(ambient + diffuse + specular, Vector3.Zero, new Vector3(255, 255, 255));
        return Color.FromArgb(255, (byte)phong.X, (byte)phong.Y, (byte)phong.Z);
    }
    
}