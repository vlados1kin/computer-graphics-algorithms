using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Main.Extensions;
using Main.ImageHelpers;
using Main.Models;

namespace Main.Renderer;

public static class Rasterizer
{
    private static float[,]? _buffer;

    public static void ClearZBuffer(int width, int height, Camera camera)
    {
        _buffer ??= new float[width, height];
        var initDepth = camera.ZFar;
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            _buffer[x, y] = initDepth;
    }
    
    public static unsafe void DrawFilledTriangleLambert(ObjModel model, WriteableBitmap wb, Color color, Camera camera, List<Light> lights)
    {
        var width = wb.PixelWidth;
        var height = wb.PixelHeight;

        var world = Transformations.CreateWorldTransform(model.Scale, Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z), model.Translation);

        wb.Lock();

        var buffer = (int*)wb.BackBuffer;

        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3) return;

            for (var j = 1; j < face.Vertices.Count - 1; j++)
            {
                var idx0 = face.Vertices[0].VertexIndex - 1;
                var idx1 = face.Vertices[j].VertexIndex - 1;
                var idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= model.TransformedVertices.Length ||
                    idx1 >= model.TransformedVertices.Length ||
                    idx2 >= model.TransformedVertices.Length)
                    continue;

                var worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                var worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                var worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                var edge1 = worldV1 - worldV0;
                var edge2 = worldV2 - worldV0;

                var normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                var viewDirection = worldV0 - camera.Eye;
                if (Vector3.Dot(normal, viewDirection) > 0)
                    continue;

                var shadedColor = Light.ApplyLambert(lights, normal, color);

                var screenV0 = model.TransformedVertices[idx0].AsVector3();
                var screenV1 = model.TransformedVertices[idx1].AsVector3();
                var screenV2 = model.TransformedVertices[idx2].AsVector3();

                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width)
                    || (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0)
                    || (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height)
                    || (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0)
                    || screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear
                    || screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar)
                    continue;

                DrawFilledTriangleLambert(screenV0, screenV1, screenV2, shadedColor, buffer, width, height);
            }
        });

        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }
    
    private static unsafe void DrawFilledTriangleLambert(Vector3 v0, Vector3 v1, Vector3 v2, Color color, int* buffer, int width, int height)
    {
        var minX = Math.Max(0, (int)Math.Floor(Math.Min(v0.X, Math.Min(v1.X, v2.X))));
        var maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(v0.X, Math.Max(v1.X, v2.X))));
        var minY = Math.Max(0, (int)Math.Floor(Math.Min(v0.Y, Math.Min(v1.Y, v2.Y))));
        var maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(v0.Y, Math.Max(v1.Y, v2.Y))));

        var denominator = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
        if (Math.Abs(denominator) < 0.001) return;
        
        for (var y = minY; y <= maxY; y++)
        {
            if (y < 0 || y >= height)
                return;

            for (var x = minX; x <= maxX; x++)
            {
                if (x < 0 || x >= width)
                    continue;

                var alpha = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) / denominator;
                var beta = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) / denominator;
                var gamma = 1 - alpha - beta;

                if (alpha >= 0 && beta >= 0 && gamma >= 0)
                {
                    var depth = alpha * v0.Z + beta * v1.Z + gamma * v2.Z;
                    if (depth < _buffer![x, y])
                    {
                        _buffer[x, y] = depth;
                        buffer[y * width + x] = color.ColorToIntBgra();
                    }
                }
            }
        }
    }
    
    public static unsafe void DrawFilledTrianglePhong(ObjModel model, WriteableBitmap bitmap, Camera camera, List<Light> lights)
    {
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;

        var world = Transformations.CreateWorldTransform(model.Scale, Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z), model.Translation);

        bitmap.Lock();
        var buffer = (int*)bitmap.BackBuffer;

        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3) return;

            for (var j = 1; j < face.Vertices.Count - 1; j++)
            {
                var idx0 = face.Vertices[0].VertexIndex - 1;
                var idx1 = face.Vertices[j].VertexIndex - 1;
                var idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= model.TransformedVertices.Length ||
                    idx1 >= model.TransformedVertices.Length ||
                    idx2 >= model.TransformedVertices.Length)
                    continue;

                var worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                var worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                var worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                var edge1 = worldV1 - worldV0;
                var edge2 = worldV2 - worldV0;
                var faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                var viewDirection = worldV0 - camera.Eye;
                if (Vector3.Dot(faceNormal, viewDirection) > 0)
                    continue;

                var screenV0 = model.TransformedVertices[idx0].AsVector3();
                var screenV1 = model.TransformedVertices[idx1].AsVector3();
                var screenV2 = model.TransformedVertices[idx2].AsVector3();

                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width)
                    || (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0)
                    || (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height)
                    || (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0)
                    || screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear
                    || screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar)
                    continue;

                var n0 = face.Vertices[0].NormalIndex > 0
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[0].NormalIndex - 1], world)
                    : faceNormal;
                var n1 = face.Vertices[j].NormalIndex > 0
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j].NormalIndex - 1], world)
                    : faceNormal;
                var n2 = face.Vertices[j + 1].NormalIndex > 0
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j + 1].NormalIndex - 1], world)
                    : faceNormal;

                DrawFilledTrianglePhong(screenV0, screenV1, screenV2,
                    n0, n1, n2, worldV0, worldV1, worldV2,
                    buffer, width, height, lights, camera);
            }
        });

        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        bitmap.Unlock();
    }
    
    private static unsafe void DrawFilledTrianglePhong(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 n0, Vector3 n1, Vector3 n2, Vector3 w0, Vector3 w1, Vector3 w2, int* buffer, int width, int height, List<Light> lights, Camera camera)
    {
        var minX = Math.Max(0, (int)Math.Floor(Math.Min(v0.X, Math.Min(v1.X, v2.X))));
        var maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(v0.X, Math.Max(v1.X, v2.X))));
        var minY = Math.Max(0, (int)Math.Floor(Math.Min(v0.Y, Math.Min(v1.Y, v2.Y))));
        var maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(v0.Y, Math.Max(v1.Y, v2.Y))));

        var denominator = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
        if (Math.Abs(denominator) < 0.001) return;

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var alpha = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) / denominator;
                var beta = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) / denominator;
                var gamma = 1 - alpha - beta;

                if (alpha >= 0 && beta >= 0 && gamma >= 0)
                {
                    var depth = alpha * v0.Z + beta * v1.Z + gamma * v2.Z;
                    if (depth < _buffer![x, y])
                    {
                        _buffer[x, y] = depth;
                        var interpNormal = Vector3.Normalize(alpha * n0 + beta * n1 + gamma * n2);
                        var fragWorld = alpha * w0 + beta * w1 + gamma * w2;
                        var viewDirection = Vector3.Normalize(camera.Eye - fragWorld);
                        var material = Material.DefaultMaterial;
                        buffer[y * width + x] = Light.ApplyPhongShading(lights, interpNormal, viewDirection, fragWorld,
                                material.AmbientColor, material.Ka, material.DiffuseColor, material.Kd,
                                material.SpecularColor, material.Shininess).ToColor().ColorToIntBgra();
                    }
                }
            }
        }
    }
    
    public static unsafe void DrawTexturedTriangles(ObjModel model, WriteableBitmap bitmap, Camera camera, List<Light> lights)
    {
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;

        var world = Transformations.CreateWorldTransform(model.Scale, Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z), model.Translation);

        bitmap.Lock();
        var buffer = (int*)bitmap.BackBuffer;

        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3) return;
            
            for (var j = 1; j < face.Vertices.Count - 1; j++)
            {
                var idx0 = face.Vertices[0].VertexIndex - 1;
                var idx1 = face.Vertices[j].VertexIndex - 1;
                var idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= model.TransformedVertices.Length ||
                    idx1 >= model.TransformedVertices.Length ||
                    idx2 >= model.TransformedVertices.Length)
                    continue;

                var worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                var worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                var worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                var edge1 = worldV1 - worldV0;
                var edge2 = worldV2 - worldV0;
                var faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                var viewDir = worldV0 - camera.Eye;
                if (Vector3.Dot(faceNormal, viewDir) > 0)
                    continue;

                var screenV0 = model.TransformedVertices[idx0].AsVector3();
                var screenV1 = model.TransformedVertices[idx1].AsVector3();
                var screenV2 = model.TransformedVertices[idx2].AsVector3();

                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width) ||
                    (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0) ||
                    (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height) ||
                    (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0) ||
                    screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear ||
                    screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar)
                    continue;

                var uv0 = model.TextureCoords[face.Vertices[0].TextureIndex - 1]
                          / model.WValues[face.Vertices[0].VertexIndex - 1]; 
                var uv1 = model.TextureCoords[face.Vertices[j].TextureIndex - 1]
                          / model.WValues[face.Vertices[j].VertexIndex - 1];
                var uv2 = model.TextureCoords[face.Vertices[j + 1].TextureIndex - 1]
                          / model.WValues[face.Vertices[j + 1].VertexIndex - 1];

                var n0 = face.Vertices[0].NormalIndex > 0
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[0].NormalIndex - 1], world)
                    : faceNormal;
                var n1 = face.Vertices[j].NormalIndex > 0
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j].NormalIndex - 1], world)
                    : faceNormal;
                var n2 = face.Vertices[j + 1].NormalIndex > 0
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j + 1].NormalIndex - 1], world)
                    : faceNormal;
                
                DrawFilledTriangleTexture(screenV0, screenV1, screenV2, n0, n1, n2, worldV0, worldV1, worldV2, uv0, uv1, uv2, buffer, width, height, lights, camera, GetFaceMaterial(model, face), model);
            }
        });

        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        bitmap.Unlock();
    }

    private static Material GetFaceMaterial(ObjModel model, Face face)
    {
        if (model.Materials != null &&
            model.Materials.TryGetValue(face.MaterialName, out var mat))
            return mat;
        return Material.DefaultMaterial;
    }
    
    private static unsafe void DrawFilledTriangleTexture(Vector3 v0, Vector3 v1, Vector3 v2,
        Vector3 n0, Vector3 n1, Vector3 n2, Vector3 w0, Vector3 w1, Vector3 w2,
        Vector3 uv0, Vector3 uv1, Vector3 uv2, int* buffer, int width, int height, List<Light> lights,
        Camera camera, Material material, ObjModel model)
    {
        var diffuseMap = !string.IsNullOrEmpty(material.DiffuseMap) ? TextureLoader.Load(material.DiffuseMap) : null;
        var normalMap = !string.IsNullOrEmpty(material.NormalMap) ? TextureLoader.Load(material.NormalMap) : null;
        var specularMap = !string.IsNullOrEmpty(material.SpecularMap) ? TextureLoader.Load(material.SpecularMap) : null;

        var minX = Math.Max(0, (int)Math.Floor(Math.Min(v0.X, Math.Min(v1.X, v2.X))));
        var maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(v0.X, Math.Max(v1.X, v2.X))));
        var minY = Math.Max(0, (int)Math.Floor(Math.Min(v0.Y, Math.Min(v1.Y, v2.Y))));
        var maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(v0.Y, Math.Max(v1.Y, v2.Y))));

        var rotation = Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z);

        var s = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
        if (Math.Abs(s) < float.Epsilon) return;

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var alpha = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) / s;
            var beta = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) / s;
            var gamma = 1 - alpha - beta;

            if (alpha >= 0 && beta >= 0 && gamma >= 0)
            {
                var depth = alpha * v0.Z + beta * v1.Z + gamma * v2.Z;
                if (depth < _buffer![x, y])
                {
                    _buffer[x, y] = depth;
                    var uv = alpha * uv0 / uv0.Z + beta * uv1 / uv1.Z + gamma * uv2 / uv2.Z;
                    var fragWorld = alpha * w0 + beta * w1 + gamma * w2;
                    var interpNormal = Vector3.Normalize(alpha * n0 + beta * n1 + gamma * n2);
                    if (normalMap != null)
                    {
                        var normColor = TextureUtil.Apply(normalMap, uv.X, uv.Y);
                        var mapNormal = new Vector3(
                            normColor.R / 255f * 2f - 1f,
                            normColor.G / 255f * 2f - 1f,
                            normColor.B / 255f * 2f - 1f);
                        mapNormal = Vector3.Normalize(mapNormal);
                        interpNormal = Vector3.TransformNormal(mapNormal, rotation);
                    }

                    var diffuseColor = material.DiffuseColor;
                    var ambientColor = material.AmbientColor;
                    if (diffuseMap != null)
                    {
                        var texColor = TextureUtil.Apply(diffuseMap, uv.X, uv.Y);
                        diffuseColor = texColor.ToVector3();
                        ambientColor = texColor.ToVector3();
                    }

                    var specularColor = material.SpecularColor;
                    if (specularMap != null)
                    {
                        var specColor = TextureUtil.Apply(specularMap, uv.X, uv.Y);
                        specularColor = specColor.ToVector3();
                    }

                    var viewDir = Vector3.Normalize(camera.Eye - fragWorld);
                    var lighting = Light.ApplyPhongShading(lights, interpNormal, viewDir, fragWorld, ambientColor, material.Ka, diffuseColor, material.Kd, specularColor, material.Shininess);
                    lighting = Vector3.Clamp(lighting, Vector3.Zero, new Vector3(255, 255, 255));
                    buffer[y * width + x] = lighting.ToColor().ColorToIntBgra();
                }
            }
        }
    }
    
    public static void DrawWireframe(ObjModel model, WriteableBitmap bitmap, Color color, Camera camera, int thickness)
    {
        var intColor = color.ColorToIntBgra();

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
                        z0 < camera.ZNear || z1 < camera.ZNear || z0 > camera.ZFar || z1 > camera.ZFar)
                        continue;

                    DrawLineBresenham(pBackBuffer, width, height, x0, y0, x1, y1, intColor, thickness);
                }
            });
        }

        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        bitmap.Unlock();
    }

    private static unsafe void DrawLineBresenham(int* buffer, int width, int height, int x0, int y0, int x1, int y1, int color, int thickness)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            DrawThickPixel(buffer, width, height, x0, y0, color, thickness);

            if (x0 == x1 && y0 == y1)
                break;

            var e2 = 2 * err;
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

    private static unsafe void DrawThickPixel(int* buffer, int width, int height, int x, int y, int color, int thickness)
    {
        var radius = thickness / 2;
        for (var i = -radius; i <= radius; i++)
        for (var j = -radius; j <= radius; j++)
        {
            var px = x + i;
            var py = y + j;
            if (px >= 0 && px < width && py >= 0 && py < height) buffer[py * width + px] = color;
        }
    }

    public static void ClearBitmap(WriteableBitmap wb, Color clearColor)
    {
        var intColor = clearColor.ColorToIntBgra();

        wb.Lock();

        try
        {
            unsafe
            {
                var pBackBuffer = (int*)wb.BackBuffer;

                for (var i = 0; i < wb.PixelHeight; i++)
                for (var j = 0; j < wb.PixelWidth; j++)
                    *pBackBuffer++ = intColor;
            }

            wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        }
        finally
        {
            wb.Unlock();
        }
    }
}