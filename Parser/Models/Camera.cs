using System.Numerics;

namespace Parser.Models;

public class Camera
{
    public Vector3 Eye { get; set; } = Vector3.Zero;
    public Vector3 Target { get; set; } = Vector3.Zero;
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public float Fov { get; set; } = MathF.PI / 2.0f; // Поле зрения камеры по оси Y (в радианах) (90 градусов)
    public float Aspect { get; set; } = 16f / 9f; // Соотношение сторон обзора камеры
    public float ZNear { get; set; } = 0.01f;
    public float ZFar { get; set; } = 100f;
    public float Radius { get; set; } = 5;
    public float Zeta { get; set; } = (float)Math.PI / 2; // угол по вертикали 0..pi
    public float Phi { get; set; } = (float)Math.PI / 2; // угол по горизонтали 0..2pi

    public Matrix4x4 GetViewMatrix() =>
        Transformations.CreateViewMatrix(Eye, Target, Up);

    public Matrix4x4 GetProjectionMatrix() =>
        Transformations.CreatePerspectiveProjection(Fov, Aspect, ZNear, ZFar);

    public void ChangeEye()
    {
        Eye = new Vector3(
            Radius * (float)Math.Cos(Phi) * (float)Math.Sin(Zeta),
            Radius * (float)Math.Cos(Zeta),
            Radius * (float)Math.Sin(Phi) * (float)Math.Sin(Zeta));
    }
}