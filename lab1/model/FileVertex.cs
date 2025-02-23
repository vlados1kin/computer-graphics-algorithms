using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab1.model
{
    public struct FaceVertex
    {
        public int VertexIndex, TextureIndex, NormalIndex;
        public FaceVertex(int v, int vt, int vn) => (VertexIndex, TextureIndex, NormalIndex) = (v, vt, vn);
    }
}
