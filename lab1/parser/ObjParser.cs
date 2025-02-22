using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using lab1.model;


namespace lab1.parser
{
    public static class ObjParser
    {

        public static Model Load(string filePath)
        {
            var model = new Model();
            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0 || parts[0].StartsWith("#")) continue;

                switch (parts[0])
                {
                    //Вершина
                    case "v":
                        model.OriginalVertices.Add(ParseVector4(parts.Skip(1).ToArray()));
                        break;
                    // Текстурная координата
                    case "vt":
                        model.TextureCoords.Add(ParseVector3(parts.Skip(1).ToArray()));
                        break;
                    // Нормаль
                    case "vn":
                        model.Normals.Add(ParseVector3(parts.Skip(1).ToArray()));
                        break;
                    // Грань
                    case "f":
                        model.Faces.Add(ParseFace(parts.Skip(1).ToArray()));
                        break;
                }
            }
            return model;
        }

        private static Vector4 ParseVector4(string[] parts)
        {
            float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
            float y = parts.Length > 1 ? float.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
            float z = parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : 0;
            float w = parts.Length > 3 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : 1;
            return new Vector4(x, y, z, w);
        }

        private static Vector3 ParseVector3(string[] parts)
        {
            float u = float.Parse(parts[0], CultureInfo.InvariantCulture);
            float v = parts.Length > 1 ? float.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
            float w = parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : 0;
            return new Vector3(u, v, w);
        }
        private static Face ParseFace(string[] parts)
        {
            var face = new Face();
            foreach (var part in parts)
            {
                var indices = part.Split('/');
                int v = int.Parse(indices[0]);
                int vt = indices.Length > 1 && indices[1] != "" ? int.Parse(indices[1]) : -1;
                int vn = indices.Length > 2 ? int.Parse(indices[2]) : -1;
                face.Vertices.Add(new FaceVertex(v, vt, vn));
            }
            return face;
        }
    }

}
