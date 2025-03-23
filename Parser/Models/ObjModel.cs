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
    
    /// <summary>
    /// Рассчитывает нормали вершин на основе нормалей граней.
    /// </summary>
    public void CalculateVertexNormals(Matrix4x4 world)
    {
        // Инициализируем нормали и счетчики нулями
        for (int i = 0; i < OriginalVertices.Count; i++)
        {
            VertexNormals[i] = Vector3.Zero;
            Counters[i] = 0;
        }

        // Для каждой грани выполняем фан-трайангуляцию
        Parallel.ForEach(Faces, face =>
        {
            if (face.Vertices.Count < 3)
                return;

            for (int j = 1; j < face.Vertices.Count - 1; j++)
            {
                int idx0 = face.Vertices[0].VertexIndex - 1;
                int idx1 = face.Vertices[j].VertexIndex - 1;
                int idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= OriginalVertices.Count || idx1 >= OriginalVertices.Count || idx2 >= OriginalVertices.Count)
                    continue;

                // Преобразуем исходные вершины с учетом текущей мировой матрицы
                var worldV0 = Vector4.Transform(OriginalVertices[idx0], world).AsVector3();
                var worldV1 = Vector4.Transform(OriginalVertices[idx1], world).AsVector3();
                var worldV2 = Vector4.Transform(OriginalVertices[idx2], world).AsVector3();

                // Проверяем на вырожденность треугольника
                if (worldV0 == worldV1 || worldV1 == worldV2 || worldV0 == worldV2)
                    continue;

                // Вычисляем нормаль данного треугольника
                var edge1 = worldV1 - worldV0;
                var edge2 = worldV2 - worldV0;
                var triNormal = Vector3.Cross(edge1, edge2);

                // Проверяем, что нормаль не является нулевой
                if (triNormal.LengthSquared() > float.Epsilon)
                {
                    triNormal = Vector3.Normalize(triNormal);

                    // Добавляем нормаль треугольника к каждой из вершин
                    AddFaceNormalToVertex(idx0, triNormal);
                    AddFaceNormalToVertex(idx1, triNormal);
                    AddFaceNormalToVertex(idx2, triNormal);
                }
            }
        });

        // Усредняем нормали для каждой вершины
        Parallel.For(0, VertexNormals.Length, i =>
        {
            if (Counters[i] > 0)
            {
                VertexNormals[i] = Vector3.Normalize(VertexNormals[i] / Counters[i]);
            }
        });

        void AddFaceNormalToVertex(int idx, Vector3 normal)
        {
            VertexNormals[idx] += normal;
            Counters[idx]++;
        }
    }
}