namespace Main.Models;

public class Face
{
    public List<FaceVertex> Vertices { get; } = [];
    public string MaterialName { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"Material – {MaterialName} : " + string.Join(" | ", Vertices);
    }
}