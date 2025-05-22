namespace Main.Models;

public struct FaceVertex
{
    public int VertexIndex;
    public int TextureIndex;
    public int NormalIndex;

    public override string ToString()
    {
        return $"v:{VertexIndex} vt:{TextureIndex} vn:{NormalIndex}";
    }
}