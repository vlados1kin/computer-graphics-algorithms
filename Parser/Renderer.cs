using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Parser.Models;

namespace Parser;

public static class Renderer
{
    public static void DrawWireframe(ObjModel model, WriteableBitmap bitmap, Color color, Camera camera)
    {
        var intColor = (color.B << 0) | (color.G << 8) | (color.R << 16) | (color.A << 24);

        bitmap.Lock();

        unsafe
        {
            var pBackBuffer = (int*)bitmap.BackBuffer;
            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;

            Parallel.ForEach(model.Faces, face =>
            {
                var count = face.Vertices.Count;
                if (count < 2)
                    return;

                for (var i = 0; i < count; i++)
                {
                    var index1 = face.Vertices[i].VertexIndex - 1;
                    var index2 = face.Vertices[(i + 1) % count].VertexIndex - 1;

                    if (index1 < 0 || index1 >= model.TransformedVertices.Length ||
                        index2 < 0 || index2 >= model.TransformedVertices.Length)
                        continue;

                    var x0 = (int)Math.Round(model.TransformedVertices[index1].X);
                    var y0 = (int)Math.Round(model.TransformedVertices[index1].Y);
                    var x1 = (int)Math.Round(model.TransformedVertices[index2].X);
                    var y1 = (int)Math.Round(model.TransformedVertices[index2].Y);
                    var z0 = model.TransformedVertices[index1].Z;

                    var z1 = model.TransformedVertices[index2].Z;

                    if ((x0 >= width && x1 >= width) || (x0 <= 0 && x1 <= 0) ||
                        (y0 >= height && y1 >= height) || (y0 <= 0 && y1 <= 0) ||
                        (z0 < camera.ZNear || z1 < camera.ZNear) || (z0 > camera.ZFar || z1 > camera.ZFar))
                    {
                        continue;
                    }

                    DrawLineBresenham(pBackBuffer, width, height, x0, y0, x1, y1, intColor);
                }
            });
        }

        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        bitmap.Unlock();
    }

    private static unsafe void DrawLineBresenham(int* buffer, int width, int height, int x0, int y0, int x1, int y1,
        int color)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                buffer[y0 * width + x0] = color;
            }

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public static void ClearBitmap(WriteableBitmap wb, Color clearColor)
    {
        var intColor = (clearColor.B << 0) | (clearColor.G << 8) | (clearColor.R << 16) | (clearColor.A << 24);

        wb.Lock();

        try
        {
            unsafe
            {
                var pBackBuffer = (int*)wb.BackBuffer;

                for (var i = 0; i < wb.PixelHeight; i++)
                {
                    for (var j = 0; j < wb.PixelWidth; j++)
                    {
                        *pBackBuffer++ = intColor;
                    }
                }
            }

            wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        }
        finally
        {
            wb.Unlock();
        }
    }
}