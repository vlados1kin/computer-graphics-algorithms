using System.Windows.Media;
using System.Windows.Media.Imaging;
using Main.Models;

namespace Main.Renderer;

public static class Renderer
{
    public static void Render(Scene scene, WriteableBitmap? wb, Color backgroundColor, Color foregroundColor, RenderMode mode)
    {
        if (wb == null) return;

        Rasterizer.ClearBitmap(wb, backgroundColor);

        BackgroundRenderer.RenderBackground(scene, wb);

        scene.Camera.ChangeEye();
        scene.UpdateAllModels();
        
        Rasterizer.ClearZBuffer(scene.CanvasWidth, scene.CanvasHeight, scene.Camera);

        switch (mode)
        {
            case RenderMode.Wireframe:
                foreach (var model in scene.Models)
                    Rasterizer.DrawWireframe(model, wb, foregroundColor, scene.Camera, 1);
                break;
            case RenderMode.FilledTrianglesLambert:
                foreach (var model in scene.Models)
                    Rasterizer.DrawFilledTriangleLambert(model, wb, foregroundColor, scene.Camera, scene.Lights);
                break;
            case RenderMode.FilledTrianglesPhong:
                foreach (var model in scene.Models)
                    Rasterizer.DrawFilledTrianglePhong(model, wb, scene.Camera, scene.Lights);
                break;
            case RenderMode.Texture:
                foreach (var model in scene.Models)
                    Rasterizer.DrawTexturedTriangles(model, wb, scene.Camera, scene.Lights);
                break;
        }
        
        LightRenderer.DrawLights(scene, wb);
    }
}