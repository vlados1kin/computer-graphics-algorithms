using System.Numerics;
using System.Windows.Media;
using Parser.Models;

namespace Parser.Raster.Extensions;

public static class ColorExtensions
{
    public static int ColorToIntBgra(this Color color)
    {
        return (color.B << 0) | (color.G << 8) | (color.R << 16) | (color.A << 24);
    }

    /// <summary>
    /// Применяет модель Ламберта к базовому цвету на основе нормали.
    /// Вычисляется интенсивность освещения как абсолютное значение скалярного произведения нормали и
    /// направления света (после нормализации). Итоговый цвет – базовый цвет, умноженный на интенсивность.
    /// </summary>
    public static Color ApplyLambert(this Color baseColor, Vector3 normal, List<Light> lambertLights)
    {
        var totalIntensity = 0f;
        foreach (var light in lambertLights)
        {
            // Нормализуем направление света
            var lightDir = Vector3.Normalize(light.Direction);
            // Вычисляем интенсивность для данного источника
            var intensity = MathF.Max(Vector3.Dot(normal, lightDir), 0);
            // Если требуется учитывать коэффициент диффузного отражения (Kd) из настроек источника, можно умножить:
            // intensity *= light.Kd;
            totalIntensity += intensity;
        }

        // Ограничиваем суммарную интенсивность значением 1
        totalIntensity = MathF.Min(totalIntensity, 1.0f);

        return Color.FromArgb(baseColor.A, (byte)(baseColor.R * totalIntensity), (byte)(baseColor.G * totalIntensity), (byte)(baseColor.B * totalIntensity));
    }
}