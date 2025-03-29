using System.Numerics;
using System.Windows.Media;
using Main.Extensions;

namespace Main.Models;

/// <summary>
///     Класс, описывающий свойства материала.
/// </summary>
public class Material
{
    public static Material DefaultMaterial { get; } = new();

    /// <summary>
    ///     Имя материала.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Путь к текстуре диффузного цвета (map_Kd).
    /// </summary>
    public string DiffuseMap { get; set; } = string.Empty;

    /// <summary>
    ///     Путь к эмиссивной текстуре (map_Ke), если есть.
    /// </summary>
    public string EmissiveMap { get; set; } = string.Empty;

    /// <summary>
    ///     Путь к нормальной карте (norm).
    /// </summary>
    public string NormalMap { get; set; } = string.Empty;

    /// <summary>
    ///     Путь к текстуре AO (ambient occlusion), если задан отдельно.
    /// </summary>
    public string AoMap { get; set; } = string.Empty;

    /// <summary>
    ///     Путь к текстуре карты MRAO (map_MRAO) (metallic-roughness-ambient occlusion).
    /// </summary>
    public string MraoMap { get; set; } = string.Empty;

    /// <summary>
    ///     Путь к текстуре карты Specular Map
    /// </summary>
    public string SpecularMap { get; set; } = string.Empty;

    /// <summary>
    ///     Отдельная текстура для metallic.
    /// </summary>
    public string MetallicMap { get; set; } = string.Empty;

    /// <summary>
    ///     Отдельная текстура для roughness.
    /// </summary>
    public string RoughnessMap { get; set; } = string.Empty;

    /// <summary>
    ///     Путь к bump-карте (map_bump или bump).
    ///     Bump-карта используется для имитации рельефа поверхности путем изменения нормалей на основе градаций яркости.
    /// </summary>
    public string BumpMap { get; set; } = string.Empty;

    // Коэффициенты (берутся из .mtl)

    /// <summary>
    ///     Коэффициент масштабирования bump‑карты, задающий интенсивность рельефа.
    ///     Если строка содержит параметр “-bm”, его значение записывается сюда.
    ///     По умолчанию 1.0 (без усиления).
    /// </summary>
    public float BumpScale { get; set; } = 1.0f;

    /// <summary>
    ///     Коэффициент фонового (амбиентного) освещения
    /// </summary>
    public Vector3 Ka { get; set; } = new(0.1f);

    /// <summary>
    ///     Коэффициент рассеянного (диффузного) освещения
    /// </summary>
    public Vector3 Kd { get; set; } = new(1.0f);

    /// <summary>
    ///     Коэффициент зеркального освещения
    /// </summary>
    public Vector3 Ks { get; set; } = new(0.2f);

    /// <summary>
    ///     Эмиссивная компонента
    /// </summary>
    public Vector3 Ke { get; set; } = new(1.0f);

    /// <summary>
    ///     Показатель блеска поверхности. (NS)
    /// </summary>
    public float Shininess { get; set; } = 64f;

    /// <summary>
    ///     Параметр металличности материала (от 0 до 1).
    ///     Если задан, влияет на отражательные свойства поверхности.
    /// </summary>
    /// metallic
    public float Pm { get; set; } = 0.0f;

    /// <summary>
    ///     Параметр шероховатости материала (от 0 до 1).
    ///     Определяет рассеянность бликов: больше значение – менее резкий блеск.
    /// </summary>
    /// roughness
    public float Pr { get; set; } = 0.0f;

    // Эти должны изменяться для каждого пикселя грани (face)

    /// <summary>
    ///     Амбиентная компонента освещения.
    /// </summary>
    public Vector3 AmbientColor { get; set; } = Colors.Black.ToVector3();

    /// <summary>
    ///     Диффузная компонента освещения.
    /// </summary>
    public Vector3 DiffuseColor { get; set; } = Colors.Gray.ToVector3();

    /// <summary>
    ///     Зеркальная компонента освещения.
    /// </summary>
    public Vector3 SpecularColor { get; set; } = Colors.White.ToVector3();
}