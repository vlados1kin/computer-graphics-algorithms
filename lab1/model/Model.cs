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
    }
}
