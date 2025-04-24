using System.Globalization;
using System.IO;
using System.Numerics;
using Main.Models;

namespace Main.Parser;

public static class ObjParser
{
    public static ObjModel Parse(string filePath)
    {
        var model = new ObjModel();

        var culture = CultureInfo.InvariantCulture;

        var min = new Vector4(float.MaxValue, float.MaxValue, float.MaxValue, 1.0f);
        var max = new Vector4(float.MinValue, float.MinValue, float.MinValue, 1.0f);

        var lineIndex = 0;

        var currentMaterialName = string.Empty;
        var currentMtlFileName = string.Empty;

        foreach (var line in File.ReadLines(filePath))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            var tokens = trimmedLine.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                continue;

            switch (tokens[0])
            {
                case "v":
                {
                    if (tokens.Length < 4)
                        throw new ArgumentException($"Неправильный формат вершины на {lineIndex} строке");

                    var x = float.Parse(tokens[1], culture);
                    var y = float.Parse(tokens[2], culture);
                    var z = float.Parse(tokens[3], culture);
                    var w = tokens.Length >= 5 ? float.Parse(tokens[4], culture) : 1.0f;
                    var vertex = new Vector4(x, y, z, w);
                    model.OriginalVertices.Add(vertex);

                    if (vertex.X < min.X) min.X = vertex.X;
                    if (vertex.Y < min.Y) min.Y = vertex.Y;
                    if (vertex.Z < min.Z) min.Z = vertex.Z;

                    if (vertex.X > max.X) max.X = vertex.X;
                    if (vertex.Y > max.Y) max.Y = vertex.Y;
                    if (vertex.Z > max.Z) max.Z = vertex.Z;
                    break;
                }
                case "vt":
                {
                    if (tokens.Length < 2)
                        throw new ArgumentException($"Неверный формат текстурной координаты на ${lineIndex} строке");

                    var u = float.Parse(tokens[1], culture);
                    var v = tokens.Length >= 3 ? float.Parse(tokens[2], culture) : 0.0f;
                    var w = tokens.Length >= 4 ? float.Parse(tokens[3], culture) : 1.0f;
                    model.TextureCoords.Add(new Vector3(u, v, w));
                    break;
                }
                case "vn":
                {
                    if (tokens.Length < 4)
                        throw new ArgumentException($"Неверный формат нормали на ${lineIndex} строке");

                    var i = float.Parse(tokens[1], culture);
                    var j = float.Parse(tokens[2], culture);
                    var k = float.Parse(tokens[3], culture);
                    model.Normals.Add(new Vector3(i, j, k));
                    break;
                }
                case "usemtl":
                {
                    if (tokens.Length >= 2) currentMaterialName = tokens[1];
                    break;
                }
                case "mtllib":
                {
                    if (tokens.Length >= 2) currentMtlFileName = tokens[1];
                    break;
                }
                case "f":
                {
                    if (tokens.Length < 4)
                        throw new ArgumentException(
                            $"Неверный формат грани (требуется минимум 3 вершины) на ${lineIndex} строке");

                    var face = new Face
                    {
                        MaterialName = currentMaterialName
                    };

                    for (var i = 1; i < tokens.Length; i++)
                    {
                        var faceVertex = new FaceVertex();

                        if (tokens.Contains("//"))
                        {
                            var parts = tokens[i].Split("//");

                            if (int.TryParse(parts[0], out var vertexIndex))
                                faceVertex.VertexIndex = vertexIndex;
                            else
                                throw new ArgumentException($"Ошибка парсинга индекса вершины на ${lineIndex} строке");

                            if (parts.Length > 1 && int.TryParse(parts[1], out var normIndex))
                                faceVertex.NormalIndex = normIndex;
                            else
                                throw new ArgumentException($"Ошибка парсинга индекса нормали на ${lineIndex} строке");
                        }
                        else
                        {
                            var parts = tokens[i].Split('/');

                            if (int.TryParse(parts[0], out var vertexIndex))
                                faceVertex.VertexIndex = vertexIndex;
                            else
                                throw new ArgumentException($"Ошибка парсинга индекса вершины на ${lineIndex} строке");

                            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                            {
                                if (int.TryParse(parts[1], out var texIndex))
                                    faceVertex.TextureIndex = texIndex;
                                else
                                    throw new ArgumentException(
                                        $"Ошибка парсинга текстурного индекса на ${lineIndex} строке");
                            }

                            if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                            {
                                if (int.TryParse(parts[2], out var normIndex))
                                    faceVertex.NormalIndex = normIndex;
                                else
                                    throw new ArgumentException(
                                        $"Ошибка парсинга индекса нормали на ${lineIndex} строке");
                            }
                        }

                        face.Vertices.Add(faceVertex);
                    }

                    model.Faces.Add(face);
                    break;
                }
            }

            lineIndex++;
        }

        var diff = Vector4.Abs(max - min);

        var maxDiff = MathF.Max(diff.X, MathF.Max(diff.Y, diff.Z));
        var scale = 2.0f / (maxDiff == 0 ? 1 : maxDiff);
        var delta = scale / 10.0f;

        model.Min = min;
        model.Max = max;
        model.Scale = scale;
        model.Delta = delta;
        model.TransformedVertices = new Vector4[model.OriginalVertices.Count];
        model.Counters = new int[model.OriginalVertices.Count];
        model.VertexNormals = new Vector3[model.OriginalVertices.Count];
        model.ModelName = Path.GetFileName(filePath);
        model.WValues = new float[model.OriginalVertices.Count];

        var mtlPath = Path.ChangeExtension(filePath, ".mtl");
        if (File.Exists(mtlPath))
        {
            model.Materials = MtlParser.Parse(mtlPath);
        }
        else
        {
            var mtlDirectory = Path.GetDirectoryName(filePath)!;
            var mtlFilePath = Path.Combine(mtlDirectory, currentMtlFileName);
            if (File.Exists(mtlFilePath)) model.Materials = MtlParser.Parse(mtlFilePath);
        }

        return model;
    }
}