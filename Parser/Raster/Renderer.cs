using System.Windows.Media;
using System.Windows.Media.Imaging;
using Parser.Models;

namespace Parser.Raster;

public static class Renderer
{
    public static void Render(Scene scene, WriteableBitmap? bitmap, Color backgroundColor, Color foregroundColor, RenderMode mode)
    {
        if (bitmap == null)
        {
            return;
        }

        Rasterizer.ClearBitmap(bitmap, backgroundColor);
        
        scene.Camera.ChangeEye();
        scene.UpdateAllModels();
        
        switch (mode)
        {
            case RenderMode.Wireframe:
                foreach (var model in scene.Models)
                {
                    Rasterizer.DrawWireframe(model, bitmap, foregroundColor, scene.Camera);
                }
                break;
            case RenderMode.FilledTrianglesLambert:
                Rasterizer.ClearZBuffer(scene.CanvasWidth, scene.CanvasHeight, scene.Camera);
                foreach (var model in scene.Models)
                {
                    Rasterizer.Lambert(model, bitmap, foregroundColor, scene.Camera, scene.Lights);
                }
                break;
            case RenderMode.FilledTrianglesPhong:
                Rasterizer.ClearZBuffer(scene.CanvasWidth, scene.CanvasHeight, scene.Camera);
                foreach (var model in scene.Models)
                {
                    Rasterizer.Phong(model, bitmap, scene.Camera, scene.Lights);
                }
                break;
        }
    }
}