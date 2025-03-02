using System.Numerics;
using System.Windows;

namespace Parser.Models;

public class Scene
{
    public List<ObjModel> Models { get; } = [];
    public Camera Camera { get; set; } = new();
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }
    public ObjModel? SelectedModel { get; set; }
    public void UpdateAllModels()
    {
        var view = Camera.GetViewMatrix();
        var projection = Camera.GetProjectionMatrix();
        var viewport = Transformations.CreateViewportMatrix(CanvasWidth, CanvasHeight);

        foreach (var model in Models)
        {
            UpdateModelTransform(model, view, projection, viewport);
        }
    }

    private void UpdateModelTransform(ObjModel model, Matrix4x4 view, Matrix4x4 projection, Matrix4x4 viewport)
    {
        var world = Transformations.CreateWorldTransform(
            model.Scale,
            Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z),
            model.Translation);

        // World * View * Projection * Viewport
        var finalTransform = world * view * projection * viewport;
        model.ApplyFinalTransformation(finalTransform, Camera);
    }

    public ObjModel? PickModel(Point clickPoint)
    {
        List<ObjModel> candidates = [];

        foreach (var model in Models)
        {
            Rect bb;
            if (model.TransformedVertices.Length == 0)
                bb = Rect.Empty;

            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            foreach (var v in model.TransformedVertices)
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
            }

            bb =new Rect(minX, minY, maxX - minX, maxY - minY);
            if (bb.Contains(clickPoint))
                candidates.Add(model);
        }

        if (candidates.Count == 0)
            return null;

        candidates.Sort((a, b) =>
        {
            var depthA = GetModelAverageDepth(a);
            var depthB = GetModelAverageDepth(b);
            return depthA.CompareTo(depthB);
        });

        return candidates[^1];

        float GetModelAverageDepth(ObjModel model) => model.TransformedVertices.Length == 0 ? float.MaxValue : model.TransformedVertices.Sum(v => v.Z) / model.TransformedVertices.Length;
    }
}