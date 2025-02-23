using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using lab1.model;

namespace lab1.transformation
{
    public static class Transformation
    {
        // матричное преобразования координат из
        // пространства модели в мировое пространство
        public static Matrix4x4 CreateWorldTransform(float scale, Matrix4x4 rotation, Vector3 translation)
        {
            var scaleMatrix = Matrix4x4.CreateScale(scale);
            var translationMatrix = Matrix4x4.CreateTranslation(translation);
            var worldMatrix = translationMatrix * rotation * scaleMatrix; // сначала T, потом R, затем S
            return worldMatrix;
        }
        public static Matrix4x4 CreateRotationMatrix(float angleX, float angleY, float angleZ, string order = "XYZ")
        {
            Matrix4x4 rotX = Matrix4x4.CreateRotationX(angleX);
            Matrix4x4 rotY = Matrix4x4.CreateRotationY(angleY);
            Matrix4x4 rotZ = Matrix4x4.CreateRotationZ(angleZ);

            Matrix4x4 result = Matrix4x4.Identity;

            foreach (char axis in order)
            {
                result = axis switch
                {
                    'X' => result * rotX,
                    'Y' => result * rotY,
                    'Z' => result * rotZ,
                    _ => throw new ArgumentException("Invalid rotation order.")
                };
            }

            return result;
        }

        //матричное преобразование координат из
        //мирового пространства в пространство наблюдателя
        public static Matrix4x4 CreateViewMatrix(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 zAxis = Vector3.Normalize(eye - target);
            Vector3 xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
            Vector3 yAxis = Vector3.Cross(zAxis, xAxis);

            Matrix4x4 viewMatrix = new Matrix4x4
            (
                xAxis.X, yAxis.X, zAxis.X, 0.0f,
                xAxis.Y, yAxis.Y, zAxis.Y, 0.0f,
                xAxis.Z, yAxis.Z, zAxis.Z, 0.0f,
                -Vector3.Dot(xAxis, eye), -Vector3.Dot(yAxis, eye), -Vector3.Dot(zAxis, eye), 1.0f
            );

            return viewMatrix;
        }
        public static void ApplyTransformation(this Model model, Matrix4x4 transform)
        {
            int count = model.OriginalVertices.Count;
            Parallel.For(0, count, i =>
            {
                model.TransformedVertices[i] = Vector4.Transform(model.OriginalVertices[i], transform);
            });
        }
    }


}

