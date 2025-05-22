using System.Numerics;
using System.Windows.Media;
using Main.Extensions;

namespace Main.Models;

public class Material
{
    public static Material DefaultMaterial { get; } = new();
    public string Name { get; set; } = string.Empty;
    public string DiffuseMap { get; set; } = string.Empty;
    public string NormalMap { get; set; } = string.Empty;
    public string SpecularMap { get; set; } = string.Empty;
    public Vector3 Ka { get; set; } = new(0.1f);
    public Vector3 Kd { get; set; } = new(1.0f);
    public Vector3 Ks { get; set; } = new(0.2f);
    public float Shininess { get; set; } = 64f;
    public Vector3 AmbientColor { get; set; } = Colors.Black.ToVector3();
    public Vector3 DiffuseColor { get; set; } = Colors.Gray.ToVector3();
    public Vector3 SpecularColor { get; set; } = Colors.White.ToVector3();
}