using System.Numerics;

namespace Parser.Models;

public class ObjModel
{
    private float _scale;
    public List<Vector4> OriginalVertices { get; } = [];
    public List<Vector3> TextureCoords { get; } = [];
    public List<Vector3> Normals { get; } = [];
    public Vector4[] TransformedVertices { get; set; } = [];
    public int[] Counters { get; set; } = [];
    public Vector3[] VertexNormals { get; set; } = [];
    public List<Face> Faces { get; } = [];
    public Vector4 Min { get; set; }
    public Vector4 Max { get; set; }

    public float Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            Delta = _scale / 10.0f;
        }
    }

    public Vector3 Translation { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public float Delta { get; set; }

    public Vector3 GetOptimalTranslationStep()
    {
        var dx = Max.X - Min.X;
        var dy = Max.Y - Min.Y;
        var dz = Max.Z - Min.Z;

        var stepX = dx / 50.0f;
        var stepY = dy / 50.0f;
        var stepZ = dz / 50.0f;

        return new Vector3(stepX, stepY, stepZ);
    }
    
    public void ApplyFinalTransformation(Matrix4x4 finalTransform, Camera camera)
    {
        var count = OriginalVertices.Count;
        Parallel.For(0, count, i =>
        {
            var v = Vector4.Transform(OriginalVertices[i], finalTransform);
            if (v.W > camera.ZNear && v.W < camera.ZFar) v /= v.W;
            TransformedVertices[i] = v;
        });
    }
}