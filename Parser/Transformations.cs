using System.Numerics;

namespace Parser;

public static class Transformations
{
    // M = T * R * S.
    public static Matrix4x4 CreateWorldTransform(float scale, Matrix4x4 rotation, Vector3 translation)
    {
        var scaleMatrix = Matrix4x4.CreateScale(scale);
        var translationMatrix = Matrix4x4.CreateTranslation(translation);
        var worldMatrix = translationMatrix * rotation * scaleMatrix;
        
        return worldMatrix;
    }
    
    // model -> view
    public static Matrix4x4 CreateViewMatrix(Vector3 eye, Vector3 target, Vector3 up)
    {
        var zAxis = Vector3.Normalize(eye - target);
        var xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
        var yAxis = Vector3.Cross(zAxis, xAxis);

        var tx = -Vector3.Dot(xAxis, eye);
        var ty = -Vector3.Dot(yAxis, eye);
        var tz = -Vector3.Dot(zAxis, eye);

        var view = new Matrix4x4(
            xAxis.X, xAxis.Y, xAxis.Z, tx,
            yAxis.X, yAxis.Y, yAxis.Z, ty,
            zAxis.X, zAxis.Y, zAxis.Z, tz,
            0.0f,    0.0f,    0.0f,    1.0f);

        view = Matrix4x4.Transpose(view);

        return view;
    }

    // view -> projection
    public static Matrix4x4 CreatePerspectiveProjection(float fov, float aspect, float znear, float zfar)
    {
        var tanHalfFov = MathF.Tan(fov / 2);
        var m00 = 1 / (aspect * tanHalfFov);
        var m11 = 1 / tanHalfFov;
        var m22 = zfar / (znear - zfar);
        var m32 = (znear * zfar) / (znear - zfar);

        var perspective = new Matrix4x4(
            m00, 0,    0,   0,
            0,   m11,  0,   0,
            0,   0,    m22, m32,
            0,   0,   -1,   0
        );

        perspective = Matrix4x4.Transpose(perspective);

        return perspective;
    }
    
    // projection -> viewport
    public static Matrix4x4 CreateViewportMatrix(float width, float height, float xMin = 0.0f, float yMin = 0.0f)
    {
        var viewportMatrix = new Matrix4x4(
            width / 2,  0,            0,  xMin + width / 2,
            0,         -height / 2,   0,  yMin + height / 2,
            0,          0,            1,  0,
            0,          0,            0,  1
        );

        viewportMatrix = Matrix4x4.Transpose(viewportMatrix);

        return viewportMatrix;
    }
}