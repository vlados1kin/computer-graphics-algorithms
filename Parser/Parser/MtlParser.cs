using System.Globalization;
using System.IO;
using System.Numerics;
using Main.Models;

namespace Main.Parser;

public static class MtlParser
{
    public static Dictionary<string, Material> Parse(string mtlFilePath)
    {
        var materials = new Dictionary<string, Material>();
        Material? current = null;
        var mtlDirectory = Path.GetDirectoryName(mtlFilePath)!;

        foreach (var line in File.ReadLines(mtlFilePath))
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            switch (parts[0].ToLowerInvariant())
            {
                case "newmtl":
                    if (parts.Length >= 2)
                    {
                        if (current != null)
                            materials[current.Name] = current;
                        current = new Material { Name = parts[1] };
                    }
                    break;
                case "map_kd":
                    if (current != null && parts.Length >= 2)
                        current.DiffuseMap = GetFullPath(mtlDirectory, parts[1]);
                    break;
                case "norm":
                case "map_norm":
                    if (current != null && parts.Length >= 2)
                        current.NormalMap = GetFullPath(mtlDirectory, parts[1]);
                    break;
                case "map_specular":
                    if (current != null && parts.Length >= 2)
                        current.SpecularMap = GetFullPath(mtlDirectory, parts[1]);
                    break;
                case "ka":
                    if (current != null && parts.Length >= 4)
                        current.Ka = ParseVector3(parts);
                    break;
                case "kd":
                    if (current != null && parts.Length >= 4)
                        current.Kd = ParseVector3(parts);
                    break;
                case "ks":
                    if (current != null && parts.Length >= 4)
                        current.Ks = ParseVector3(parts);
                    break;
                case "ns":
                    if (current != null && parts.Length >= 2)
                        current.Shininess = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    break;
            }
        }

        if (current != null)
            materials[current.Name] = current;

        return materials;
    }

    private static string GetFullPath(string baseDirectory, string relativePath)
    {
        return Path.Combine(baseDirectory, relativePath);
    }

    private static Vector3 ParseVector3(string[] parts)
    {
        return new Vector3(
            float.Parse(parts[1], CultureInfo.InvariantCulture),
            float.Parse(parts[2], CultureInfo.InvariantCulture),
            float.Parse(parts[3], CultureInfo.InvariantCulture));
    }
}