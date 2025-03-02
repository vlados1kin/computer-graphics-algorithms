namespace Parser;

public class Face
{
    public List<FaceVertex> Vertices { get; } = [];

    public override string ToString() => string.Join(" | ", Vertices);
}

public struct FaceVertex(int vertexIndex, int textureIndex = 0, int normalIndex = 0)
{
    public int VertexIndex { get; set; } = vertexIndex;
    public int TextureIndex { get; set; } = textureIndex;
    public int NormalIndex { get; set; } = normalIndex;

    public override string ToString() => $"v:{VertexIndex} vt:{TextureIndex} vn:{NormalIndex}";
}