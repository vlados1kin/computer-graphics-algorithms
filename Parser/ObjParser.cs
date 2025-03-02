using System.Globalization;
using System.IO;
using System.Numerics;
using Parser.Models;

namespace Parser;

public static class ObjParser
{
    public static ObjModel Parse(string filePath)
    {
        var model = new ObjModel();

        var min = new Vector4(float.MaxValue);
        var max = new Vector4(float.MinValue);

        foreach (var line in File.ReadLines(filePath))
        {
            var tokens = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0 || "#".StartsWith(tokens[0])) continue;

            switch (tokens[0])
            {
                case "v":
                    ParseVertex(tokens, model, ref min, ref max, CultureInfo.InvariantCulture);
                    break;
                case "vt":
                    ParseTextureCoord(tokens, model, CultureInfo.InvariantCulture);
                    break;
                case "vn":
                    ParseNormal(tokens, model, CultureInfo.InvariantCulture);
                    break;
                case "f":
                    ParseFace(tokens, model);
                    break;
            }
        }

        CalculateBoundingBox(model, min, max);
        return model;
    }

    private static void ParseVertex(string[] tokens, ObjModel model, ref Vector4 min, ref Vector4 max, CultureInfo culture)
    {
        var vertex = new Vector4(
            float.Parse(tokens[1], culture),
            float.Parse(tokens[2], culture),
            float.Parse(tokens[3], culture),
            tokens.Length >= 5 ? float.Parse(tokens[4], culture) : 1.0f
        );
        model.OriginalVertices.Add(vertex);
        min = Vector4.Min(min, vertex);
        max = Vector4.Max(max, vertex);
    }

    private static void ParseTextureCoord(string[] tokens, ObjModel model, CultureInfo culture)
    {
        model.TextureCoords.Add(new(
            float.Parse(tokens[1], culture),
            tokens.Length >= 3 ? float.Parse(tokens[2], culture) : 0,
            tokens.Length >= 4 ? float.Parse(tokens[3], culture) : 0
        ));
    }

    private static void ParseNormal(string[] tokens, ObjModel model, CultureInfo culture)
    {
        model.Normals.Add(new(
            float.Parse(tokens[1], culture),
            float.Parse(tokens[2], culture),
            float.Parse(tokens[3], culture)
        ));
    }

    private static void ParseFace(string[] tokens, ObjModel model)
    {
        var face = new Face();
        
        for (var i = 1; i < tokens.Length; i++)
        {
            var faceVertex = new FaceVertex();
            var parts = tokens[i].Split('/');

            faceVertex.VertexIndex = int.Parse(parts[0]);
            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) faceVertex.TextureIndex = int.Parse(parts[1]);
            if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2])) faceVertex.NormalIndex = int.Parse(parts[2]);
            
            face.Vertices.Add(faceVertex);
        }
        
        model.Faces.Add(face);
    }

    private static void CalculateBoundingBox(ObjModel model, Vector4 min, Vector4 max)
    {
        var diff = Vector4.Abs(max - min);
        float maxDiff = MathF.Max(diff.X, MathF.Max(diff.Y, diff.Z));
        float scale = 2.0f / (maxDiff == 0 ? 1 : maxDiff);
        model.Min = min;
        model.Max = max;
        model.Scale = scale;
        model.Delta = scale / 10.0f;
        model.TransformedVertices = new Vector4[model.OriginalVertices.Count];
        model.Counters = new int[model.OriginalVertices.Count];
        model.VertexNormals = new Vector3[model.OriginalVertices.Count];
    }
}
