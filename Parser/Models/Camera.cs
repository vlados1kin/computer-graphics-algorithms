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
    public float Zeta { get; set; } = (float)Math.PI / (float)2.3;
    public float Phi { get; set; } = (float)Math.PI / 2;

    public Matrix4x4 GetViewMatrix()
    {
        return Transformations.CreateViewMatrix(Eye, Target, Up);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Transformations.CreatePerspectiveProjection(Fov, Aspect, ZNear, ZFar);
    }

    public void ChangeEye()
    {
        Eye = new Vector3(
            Radius * MathF.Cos(Phi) * MathF.Sin(Zeta),
            Radius * MathF.Cos(Zeta),
            Radius * MathF.Sin(Phi) * MathF.Sin(Zeta));
    }
    
    public Vector3 GetCameraWorldPos()
    {
        var (sinX, cosX) = MathF.SinCos(DegreesToRadians(Zeta));
        var (sinY, cosY) = MathF.SinCos(DegreesToRadians(Phi));

        return new Vector3
        {
            X = cosX * cosY * Radius,
            Z = sinX * cosY * Radius,
            Y = sinY * Radius
        };
    }
    
    public Matrix4x4 GetCameraTransformation()
    {
        (float sinX, float cosX) = MathF.SinCos(DegreesToRadians(Zeta));
        (float sinY, float cosY) = MathF.SinCos(DegreesToRadians(Phi));

        var eye = new Vector3
        {
            X = cosX * cosY * Radius,
            Z = sinX * cosY * Radius,
            Y = sinY * Radius
        };

        var target = new Vector3(0, 0, 0);

        var zAxis = Vector3.Normalize(eye - target);
        var xAxis = Vector3.Normalize(new Vector3(-sinX, 0, cosX));
        var yAxis = Vector3.Normalize(Vector3.Cross(xAxis, zAxis));


        var view = new Matrix4x4(xAxis.X, yAxis.X, zAxis.X, 0,
            xAxis.Y, yAxis.Y, zAxis.Y, 0,
            xAxis.Z, yAxis.Z, zAxis.Z, 0,
            -Vector3.Dot(xAxis, eye),
            -Vector3.Dot(yAxis, eye),
            -Vector3.Dot(zAxis, eye),
            1);

        return view;
    }
    
    private float DegreesToRadians(float angle)
    {
        return (float)(angle / 180 * Math.PI);
    }
}