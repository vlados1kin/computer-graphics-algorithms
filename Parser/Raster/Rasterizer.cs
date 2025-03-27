using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Parser.Models;
using Parser.Raster.Extensions;

namespace Parser.Raster;

public static class Rasterizer
{
    // Z-буфер: хранит глубину для каждого пикселя; 
    private static float[,]? _zBuffer;

    public static void ClearZBuffer(int width, int height, Camera camera)
    {
        _zBuffer ??= new float[width, height];
        var initDepth = camera.ZFar;
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                _zBuffer[x, y] = initDepth;
            }
        }
    }

    #region Lambert

    /// <summary>
    /// Растеризует (заполняет) треугольники для каждой грани модели.
    /// Для каждой грани, состоящей из 3+ вершин, применяется фан‑трайангуляция.
    /// Для каждой треугольной части производится backface culling (с использованием нормали)
    /// и рассчитывается интенсивность освещения по модели Ламберта.
    /// Затем вызывается метод, который заполняет треугольник с использованием Z-буфера.
    /// </summary>
    public static unsafe void DrawFilledTriangleLambert(ObjModel model, WriteableBitmap bitmap, Color color, Camera camera, List<Light> lights)
    {
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;

        var world = Transformations.CreateWorldTransform(model.Scale, Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z), model.Translation);

        bitmap.Lock();

        var buffer = (int*)bitmap.BackBuffer;

        // Для каждой грани модели
        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3) return;

            //Если грань содержит больше 3 вершин, выполняем трайангуляцию
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

                //Вычисляем нормаль треугольника в мировых координатах
                var worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                var worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                var worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                var edge1 = worldV1 - worldV0;
                var edge2 = worldV2 - worldV0;

                // Эту нормаль бы сохранять где-то на будущее
                var normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                // Backface culling: если треугольник обращён от камеры, отбраковываем грань
                var viewDirection = worldV0 - camera.Eye; // Вектор взгляда от камеры к вершине
                if (Vector3.Dot(normal, viewDirection) > 0)
                    continue; // Если скалярное произведение положительное, грань отвернута

                // Расчет интенсивности освещения по модели Ламберта
                var shadedColor = color.ApplyLambert(normal, lights);

                // Получаем экранные координаты (после всех преобразований)
                var screenV0 = model.TransformedVertices[idx0].AsVector3();
                var screenV1 = model.TransformedVertices[idx1].AsVector3();
                var screenV2 = model.TransformedVertices[idx2].AsVector3();
                
                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width) 
                    || (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0) 
                    || (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height) 
                    || (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0)
                    || screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear
                    || screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar)
                {
                    continue;
                }

                // Растеризуем треугольник с заливкой и Z-тестом
                DrawFilledTriangleLambert(screenV0, screenV1, screenV2, shadedColor, buffer, width, height);
            }
        });

        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        bitmap.Unlock();
    }

    /// <summary>
    /// Растеризует (заполняет) один треугольник, заданный тремя вершинами в экранном пространстве.
    /// Метод использует сканирующую линию с вычислением барицентрических координат для интерполяции глубины.
    /// Отбраковка невидимых фрагментов осуществляется с помощью Z-буфера.
    /// TODO: что такое интерполяция глубины, барицентрические координаты
    /// </summary>
    private static unsafe void DrawFilledTriangleLambert(Vector3 v0, Vector3 v1, Vector3 v2, Color color, int* buffer, int width, int height)
    {
        // Определяем ограничивающий прямоугольник (обрамлены Math.Max и Math.Min, чтобы не уходили за экран)
        var minX = Math.Max(0, (int)Math.Floor(Math.Min(v0.X, Math.Min(v1.X, v2.X))));
        var maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(v0.X, Math.Max(v1.X, v2.X))));
        var minY = Math.Max(0, (int)Math.Floor(Math.Min(v0.Y, Math.Min(v1.Y, v2.Y))));
        var maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(v0.Y, Math.Max(v1.Y, v2.Y))));

        // Вычисляем знаменатель барицентрических координат
        var denominator = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
        if (Math.Abs(denominator) < float.Epsilon)
        {
            return; // Вырожденный треугольник
        }

        var invDenominator = 1.0f / denominator;

        for (var y = minY; y <= maxY; y++)
        {
            if (y < 0 || y >= height)
            {
                return;
            }

            for (var x = minX; x <= maxX; x++)
            {
                if (x < 0 || x >= width)
                {
                    continue;
                }

                // Вычисляем барицентрические координаты: alpha, beta, gamma
                var alpha = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) * invDenominator;
                var beta = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) * invDenominator;
                var gamma = 1 - alpha - beta;

                // Если точка внутри треугольника (включая границы)
                if (alpha >= 0 && beta >= 0 && gamma >= 0)
                {
                    // Интерполируем глубину по барицентрическим координатам
                    var depth = alpha * v0.Z + beta * v1.Z + gamma * v2.Z;
                    // Если новый фрагмент ближе (меньшее значение depth) – обновляем Z-буфер и рисуем пиксель
                    if (depth < _zBuffer![x, y])
                    {
                        _zBuffer[x, y] = depth;
                        buffer[y * width + x] = color.ColorToIntBgra();
                    }
                }
            }
        }
    }

    #endregion


    // Поддерживаются два режима:
    // FilledTrianglesPhong – вычисление цвета на уровне пикселя (обычное Фонговое затенение)
    // FilledTrianglesAverageFaceNormalPhong – использование усреднённых нормалей вершин (Гуравское затенение)
    #region FilledTrianglesPhong
    
    /// <summary>
    /// Растеризует треугольники для каждой грани модели с применением TODO: фан-трайангуляции, backface culling и модели Фонга.
    /// Для каждой треугольной части вычисляются экранные координаты и, если треугольник видим (с учетом нормали),
    /// происходит заполнение с использованием Z-буфера и вычислением цвета по модели Фонга.
    /// </summary>
    public static unsafe void DrawFilledTrianglePhong(ObjModel model, WriteableBitmap bitmap, Camera camera, List<Light> lights)
    {
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;

        // Вычисляем мировую матрицу на основе масштабирования, вращения и трансляции модели
        var world = Transformations.CreateWorldTransform(model.Scale, Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z), model.Translation);

        bitmap.Lock();
        var buffer = (int*)bitmap.BackBuffer;

        // Для каждой грани модели (фан-трайангуляция)
        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3)
            {
                return;
            }

            // Для каждой треугольной части грани
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

                // Вычисляем мировые координаты вершин (для backface culling)
                var worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                var worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                var worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                // Вычисляем нормаль треугольника (в мировых координатах)
                var edge1 = worldV1 - worldV0;
                var edge2 = worldV2 - worldV0;
                var faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                // Backface culling: если треугольник обращён от камеры, отбраковываем грань
                var viewDirection = worldV0 - camera.Eye; // Вектор взгляда от камеры к вершине
                if (Vector3.Dot(faceNormal, viewDirection) > 0)
                {
                    continue; // Если скалярное произведение положительное, грань отвернута
                }

                // Получаем экранные координаты (уже после всех преобразований)
                var screenV0 = model.TransformedVertices[idx0].AsVector3();
                var screenV1 = model.TransformedVertices[idx1].AsVector3();
                var screenV2 = model.TransformedVertices[idx2].AsVector3();
                
                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width) 
                    || (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0) 
                    || (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height) 
                    || (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0)
                    || screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear
                    || screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar)
                {
                    continue;
                }

                // Определяем нормали для затенения:
                // Если в модели заданы нормали для вершин, используем их; иначе – используем нормаль грани.
                var n0 = (face.Vertices[0].NormalIndex > 0)
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[0].NormalIndex - 1], world)
                    : faceNormal;
                var n1 = (face.Vertices[j].NormalIndex > 0)
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j].NormalIndex - 1], world)
                    : faceNormal;
                var n2 = (face.Vertices[j + 1].NormalIndex > 0)
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j + 1].NormalIndex - 1], world)
                    : faceNormal;

                // Отрисовываем треугольник с Фонговым затенением.
                DrawFilledTrianglePhong(screenV0, screenV1, screenV2,
                    n0, n1, n2, worldV0, worldV1, worldV2,
                    buffer, width, height, lights, camera);
            }
        });

        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        bitmap.Unlock();
    }

    #endregion

    #region FilledTrianglesAverageFaceNormalPhong

    /// <summary>
    /// Растеризует треугольники для каждой грани модели с применением фан-трайангуляции, backface culling и модели Фонга.
    /// Для каждой треугольной части вычисляются экранные координаты и, если треугольник видим (с учетом нормали),
    /// происходит заполнение с использованием Z-буфера и вычислением цвета по модели Фонга.
    /// </summary>
    public static unsafe void FilledTrianglesAverageFaceNormalPhong(ObjModel model, WriteableBitmap bitmap, Camera camera, List<Light> lights)
    {
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;

        // Вычисляем мировую матрицу на основе масштабирования, вращения и трансляции модели
        var world = Transformations.CreateWorldTransform(model.Scale, Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z), model.Translation);

        bitmap.Lock();
        var buffer = (int*)bitmap.BackBuffer;

        model.CalculateVertexNormals(world);
        
        // Для каждой грани модели (фан-трайангуляция)
        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3) return;

            // Для каждой треугольной части грани
            for (int j = 1; j < face.Vertices.Count - 1; j++)
            {
                var idx0 = face.Vertices[0].VertexIndex - 1;
                var idx1 = face.Vertices[j].VertexIndex - 1;
                var idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= model.TransformedVertices.Length ||
                    idx1 >= model.TransformedVertices.Length ||
                    idx2 >= model.TransformedVertices.Length)
                    continue;

                // Вычисляем мировые координаты вершин (для backface culling)
                var worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                var worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                var worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                // Вычисляем нормаль треугольника (в мировых координатах)
                var edge1 = worldV1 - worldV0;
                var edge2 = worldV2 - worldV0;
                var faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                // Backface culling: если треугольник обращён от камеры, отбраковываем грань
                var viewDirection = worldV0 - camera.Eye; // Вектор взгляда от камеры к вершине
                if (Vector3.Dot(faceNormal, viewDirection) > 0)
                    continue; // Если скалярное произведение положительное, грань отвернута

                // Получаем экранные координаты (уже после всех преобразований)
                var screenV0 = model.TransformedVertices[idx0].AsVector3();
                var screenV1 = model.TransformedVertices[idx1].AsVector3();
                var screenV2 = model.TransformedVertices[idx2].AsVector3();
                
                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width) 
                    || (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0) 
                    || (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height) 
                    || (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0)
                    || screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear
                    || screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar)
                {
                    continue;
                }
                
                var n0 = model.VertexNormals[idx0];
                var n1 = model.VertexNormals[idx1];
                var n2 = model.VertexNormals[idx2];
                
                DrawFilledTrianglePhong(screenV0, screenV1, screenV2,
                    n0, n1, n2, worldV0, worldV1, worldV2,
                    buffer, width, height, lights, camera);
            }
        });

        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        bitmap.Unlock();
    }

    #endregion

    /// <summary>
    /// Растеризует один треугольник с Фонговым затенением.
    /// Для каждого пикселя внутри ограничивающего прямоугольника вычисляются барицентрические координаты,
    /// интерполируется глубина, а также мировая позиция и нормаль, после чего вычисляется итоговый цвет фрагмента по модели Фонга.
    /// Отбраковка невидимых фрагментов производится с помощью Z-буфера.
    /// </summary>
    private static unsafe void DrawFilledTrianglePhong(Vector3 v0, Vector3 v1, Vector3 v2, 
        Vector3 n0, Vector3 n1, Vector3 n2, Vector3 w0, Vector3 w1, Vector3 w2,
        int* buffer, int width, int height, List<Light> lights, Camera camera)
    {
        // Ограничивающий прямоугольник (не выходит за пределы экрана)
        var minX = Math.Max(0, (int)Math.Floor(Math.Min(v0.X, Math.Min(v1.X, v2.X))));
        var maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(v0.X, Math.Max(v1.X, v2.X))));
        var minY = Math.Max(0, (int)Math.Floor(Math.Min(v0.Y, Math.Min(v1.Y, v2.Y))));
        var maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(v0.Y, Math.Max(v1.Y, v2.Y))));

        // Вычисляем знаменатель барицентрических координат
        var denominator = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
        if (Math.Abs(denominator) < float.Epsilon) return; // Вырожденный треугольник
        var invDenominator = 1.0f / denominator;

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                // Вычисляем барицентрические координаты: alpha, beta, gamma
                var alpha = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) * invDenominator;
                var beta = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) * invDenominator;
                var gamma = 1 - alpha - beta;

                // Если точка внутри треугольника (включая границы)
                if (alpha >= 0 && beta >= 0 && gamma >= 0)
                {
                    // Интерполируем глубину
                    var depth = alpha * v0.Z + beta * v1.Z + gamma * v2.Z;
                    // Z-тест: если новый фрагмент ближе, обновляем Z-буфер и цвет пикселя
                    if (depth < _zBuffer![x, y])
                    {
                        _zBuffer[x, y] = depth;

                        // Интерполируем нормаль: линейная интерполяция нормалей вершин
                        var interpNormal = Vector3.Normalize(alpha * n0 + beta * n1 + gamma * n2);

                        // Интерполируем мировую позицию фрагмента (для расчёта вектора взгляда)
                        var fragWorld = alpha * w0 + beta * w1 + gamma * w2;

                        // Вектор от фрагмента к камере.
                        // Нормализация нужна для расчета зеркальной составляющей.
                        var viewDirection = Vector3.Normalize(camera.Eye - fragWorld);

                        buffer[y * width + x] = Light.ApplyPhongShading(lights, interpNormal, viewDirection, fragWorld).ColorToIntBgra();
                    }
                }
            }
        }
    }
    
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

    private static unsafe void DrawLineBresenham(int* buffer, int width, int height, int x0, int y0, int x1, int y1, int color)
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

    public static void ClearBitmap(WriteableBitmap bitmap, Color clearColor)
    {
        var intColor = (clearColor.B << 0) | (clearColor.G << 8) | (clearColor.R << 16) | (clearColor.A << 24);

        bitmap.Lock();

        try
        {
            unsafe
            {
                var pBackBuffer = (int*)bitmap.BackBuffer;

                for (var i = 0; i < bitmap.PixelHeight; i++)
                {
                    for (var j = 0; j < bitmap.PixelWidth; j++)
                    {
                        *pBackBuffer++ = intColor;
                    }
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        }
        finally
        {
            bitmap.Unlock();
        }
    }
}