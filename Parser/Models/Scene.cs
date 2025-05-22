using System.Numerics;
using System.Windows;

namespace Main.Models;

public class Scene
{
    public List<ObjModel> Models { get; } = [];
    public List<Light> Lights { get; } = [];
    public Camera Camera { get; set; } = new();
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }
    public ObjModel? SelectedModel { get; set; }

    public void UpdateAllModels()
    {
        var view = Camera.GetViewMatrix();
        var projection = Camera.GetProjectionMatrix();
        var viewport = Transformations.CreateViewportMatrix(CanvasWidth, CanvasHeight);

        foreach (var model in Models) UpdateModelTransform(model, view, projection, viewport);
    }

    private void UpdateModelTransform(ObjModel model, Matrix4x4 view, Matrix4x4 projection, Matrix4x4 viewport)
    {
        var world = Transformations.CreateWorldTransform(model.Scale, Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z), model.Translation);
        var finalTransform = world * view * projection * viewport;
        model.ApplyFinalTransformation(finalTransform, Camera);
    }

    public ObjModel? PickModel(Point clickPoint)
    {
        ObjModel? pickedModel = null;
        var bestDepth = float.MaxValue;

        foreach (var model in Models)
        {
            if (model.TransformedVertices.Length == 0)
                continue;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            var modelDepth = float.MaxValue;

            foreach (var v in model.TransformedVertices)
            {
                minX = MathF.Min(minX, v.X);
                minY = MathF.Min(minY, v.Y);
                maxX = MathF.Max(maxX, v.X);
                maxY = MathF.Max(maxY, v.Y);
                modelDepth = MathF.Min(modelDepth, v.Z);
            }

            if (clickPoint.X >= minX && clickPoint.X <= maxX &&
                clickPoint.Y >= minY && clickPoint.Y <= maxY)
                if (modelDepth < bestDepth)
                {
                    bestDepth = modelDepth;
                    pickedModel = model;
                }
        }

        return pickedModel;
    }
}