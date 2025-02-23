using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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


    }


}
}
