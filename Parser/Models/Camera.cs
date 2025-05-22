using System.Numerics;

namespace Main.Models;

public class Camera
{
    public Vector3 Eye { get; set; } = Vector3.Zero;
    public Vector3 Target { get; set; } = Vector3.Zero;
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public float Fov { get; set; } = MathF.PI / 2.0f;
    public float Aspect { get; set; } = 16f / 9f;
    public float ZNear { get; set; } = 0.01f;
    public float ZFar { get; set; } = 100f;
    public float Radius { get; set; } = 5;
    
    // Исправляем углы для горизонтального взгляда вперед:
    public float Zeta { get; set; } = 0f;  // Азимутальный угол (поворот вокруг Y)
    public float Phi { get; set; } = 0f;   // Полярный угол (0 = горизонтально, π/2 = вверх, -π/2 = вниз)

    public Matrix4x4 GetViewMatrix()
    {
        return Transformations.CreateViewMatrix(Eye, Target, Up);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Transformations.CreatePerspectiveProjection(Fov, Aspect, ZNear, ZFar);
    }

    public Vector3 GetCameraWorldPos()
    {
        var (sinZeta, cosZeta) = MathF.SinCos(Zeta);
        var (sinPhi, cosPhi) = MathF.SinCos(Phi);
        return new Vector3
        {
            X = cosZeta * cosPhi * Radius,
            Z = sinZeta * cosPhi * Radius,
            Y = sinPhi * Radius
        };
    }

    public Matrix4x4 GetCameraTransformation()
    {
        (float sinZeta, float cosZeta) = MathF.SinCos(Zeta);
        (float sinPhi, float cosPhi) = MathF.SinCos(Phi);
        var eye = new Vector3
        {
            X = cosZeta * cosPhi * Radius,
            Z = sinZeta * cosPhi * Radius,
            Y = sinPhi * Radius
        };
        var target = Target;
        var zAxis = Vector3.Normalize(eye - target);
        var xAxis = Vector3.Normalize(new Vector3(-sinZeta, 0, cosZeta));
        var yAxis = Vector3.Normalize(Vector3.Cross(xAxis, zAxis));
        var view = new Matrix4x4(
            xAxis.X, yAxis.X, zAxis.X, 0,
            xAxis.Y, yAxis.Y, zAxis.Y, 0,
            xAxis.Z, yAxis.Z, zAxis.Z, 0,
            -Vector3.Dot(xAxis, eye),
            -Vector3.Dot(yAxis, eye),
            -Vector3.Dot(zAxis, eye),
            1);
        return view;
    }

    public void ChangeEye()
    {
        var (sinZeta, cosZeta) = MathF.SinCos(Zeta);
        var (sinPhi, cosPhi) = MathF.SinCos(Phi);
        
        Eye = new Vector3(
            Radius * cosPhi * cosZeta + Target.X,
            Radius * sinPhi + Target.Y,
            Radius * cosPhi * sinZeta + Target.Z);
    }

    private float DegreesToRadians(float angle)
    {
        return (float)(angle / 180 * Math.PI);
    }
}