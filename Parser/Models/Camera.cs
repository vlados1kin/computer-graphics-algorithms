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
}