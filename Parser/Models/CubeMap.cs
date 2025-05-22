using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Main.Models;

public class CubeMap
{
    public ImageData PositiveX { get; private set; }
    public ImageData NegativeX { get; private set; }
    public ImageData PositiveY { get; private set; }
    public ImageData NegativeY { get; private set; }
    public ImageData PositiveZ { get; private set; }
    public ImageData NegativeZ { get; private set; }

    public CubeMap(string filePath)
    {
        using Image<Bgra32> image = Image.Load<Bgra32>(filePath);

        int faceWidth = image.Width / 4;
        int faceHeight = image.Height / 3;

        PositiveX = ExtractFace(image, 2 * faceWidth, 1 * faceHeight, faceWidth, faceHeight);
        NegativeX = ExtractFace(image, 0 * faceWidth, 1 * faceHeight, faceWidth, faceHeight);
        PositiveY = ExtractFace(image, 1 * faceWidth, 0 * faceHeight, faceWidth, faceHeight);
        NegativeY = ExtractFace(image, 1 * faceWidth, 2 * faceHeight, faceWidth, faceHeight);
        PositiveZ = ExtractFace(image, 3 * faceWidth, 1 * faceHeight, faceWidth, faceHeight);
        NegativeZ = ExtractFace(image, 1 * faceWidth, 1 * faceHeight, faceWidth, faceHeight);
    }

    private ImageData ExtractFace(Image<Bgra32> source, int x, int y, int width, int height)
    {
        Bgra32[] pixels = new Bgra32[(width * height)];

        source.Clone(ctx => ctx
                .Crop(new Rectangle(x, y, width, height)))
            .CopyPixelDataTo(pixels);

        return new ImageData(pixels, width, height);
    }

    public Vector4 SampleBackground(Vector3 direction)
    {
        direction = Vector3.Normalize(direction);

        float absX = MathF.Abs(direction.X);
        float absY = MathF.Abs(direction.Y);
        float absZ = MathF.Abs(direction.Z);

        ImageData face;
        Vector2 uv;

        if (absX >= absY && absX >= absZ)
        {
            if (direction.X > 0)
            {
                face = PositiveX;
                uv = new Vector2(direction.Z, direction.Y) / absX;
            }
            else
            {
                face = NegativeX;
                uv = new Vector2(-direction.Z, direction.Y) / absX;
            }
        }
        else if (absY >= absX && absY >= absZ)
        {
            if (direction.Y > 0)
            {
                face = PositiveY;
                uv = new Vector2(direction.X, direction.Z) / absY;
            }
            else
            {
                face = NegativeY;
                uv = new Vector2(direction.X, -direction.Z) / absY;
            }
        }
        else
        {
            if (direction.Z > 0)
            {
                face = PositiveZ;
                uv = new Vector2(-direction.X, direction.Y) / absZ;
            }
            else
            {
                face = NegativeZ;
                uv = new Vector2(direction.X, direction.Y) / absZ;
            }
        }

        uv = (uv + Vector2.One) * 0.5f;

        return face.SampleNearest(uv);
    }
}