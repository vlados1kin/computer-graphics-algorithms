using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace lab1.model
{
    public class Model
    {
        private float _scale;
        private Vector3 _translation = Vector3.Zero;
        private Vector3 _rotation = Vector3.Zero;

        public List<Vector4> OriginalVertices { get; } = [];

        public Vector4[] TransformedVertices { get; set; } = [];

        // vt u [v] [w]
        public List<Vector3> TextureCoords { get; } = [];
        // vn i j k
        public List<Vector3> Normals { get; } = [];

        public List<Face> Faces { get; } = [];

        public void writeToFile(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var v in OriginalVertices)
                {
                    writer.WriteLine($"v {v.X} {v.Y} {v.Z} {v.W}");
                }
               /* foreach (var vt in TextureCoords)
                {
                    writer.WriteLine($"vt {vt.X} {vt.Y} {vt.Z}");
                }
                foreach (var vn in Normals)
                {
                    writer.WriteLine($"vn {vn.X} {vn.Y} {vn.Z}");
                }
                foreach (var face in Faces)
                {
                    writer.Write("f");
                    foreach (var fv in face.Vertices)
                    {
                        writer.Write($" {fv.VertexIndex}");
                        if (fv.TextureIndex > 0 || fv.NormalIndex > 0)
                        {
                            writer.Write("/");
                            if (fv.TextureIndex > 0) writer.Write($"{fv.TextureIndex}");
                            if (fv.NormalIndex > 0) writer.Write($"/{fv.NormalIndex}");
                        }
                    }
                    writer.WriteLine();
                }*/
            }
        }

    }
}
