using System.Numerics;
using SixLabors.ImageSharp.PixelFormats;

namespace Main.Models;

public struct ImageData
{
    public Bgra32[] Pixels { get; }
    public int Width { get; }
    public int Height { get; }

    public ImageData(Bgra32[] pixels, int width, int height)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
    }

    public readonly Vector4 SampleNearest(Vector2 uv)
    {
        uv.X = float.Clamp(uv.X, 0, 1);
        uv.Y = float.Clamp(1 - uv.Y, 0, 1);
        int x = (int)MathF.Round((uv.X * (Width - 1)));
        int y = (int)MathF.Round((uv.Y * (Height - 1)));
        return Pixels[y * Width + x].ToScaledVector4();
    }
}
