using System.Windows.Media;
using System.Windows.Media.Imaging;
using Main.Models;

namespace Main.Renderer;

public static class Renderer
{
    public static void Render(Scene scene, CubeMap? cubeMap, WriteableBitmap? bitmap, Color backgroundColor, Color foregroundColor, RenderMode mode)
    {
        if (bitmap == null) return;

        Rasterizer.ClearBitmap(bitmap, backgroundColor);
        scene.Camera.ChangeEye();
        scene.UpdateAllModels();
        Rasterizer.ClearZBuffer(scene.CanvasWidth, scene.CanvasHeight, scene.Camera);

        switch (mode)
        {
            case RenderMode.Wireframe:
            {
                foreach (var model in scene.Models)
                {
                    Rasterizer.DrawWireframe(model, bitmap, foregroundColor, scene.Camera, 1);
                }
                
                break;
            }
            case RenderMode.FilledTrianglesLambert:
            {
                foreach (var model in scene.Models)
                {
                    Rasterizer.DrawFilledTriangleLambert(model, bitmap, foregroundColor, scene.Camera, scene.Lights);
                }

                break;
            }
            case RenderMode.FilledTrianglesPhong:
            {
                foreach (var model in scene.Models)
                {
                    Rasterizer.DrawFilledTrianglePhong(model, bitmap, scene.Camera, scene.Lights);
                }

                break;
            }
            case RenderMode.FilledTexture:
            {
                foreach (var model in scene.Models)
                {
                    Rasterizer.DrawTexturedTriangles(model, bitmap, scene.Camera, scene.Lights, cubeMap, scene);
                }

                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}