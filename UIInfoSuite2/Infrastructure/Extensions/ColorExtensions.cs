using System;
using Microsoft.Xna.Framework;

namespace UIInfoSuite2.Infrastructure.Extensions;

public static class ColorExtensions
{
  public static Color Desaturate(this Color color, float desaturationFactor)
  {
    float hue, saturation, lum;
    ColorToHsl(color, out hue, out saturation, out lum);
    float newSaturation = Math.Max(0, Math.Min(saturation * (1 - desaturationFactor), 1));
    Color newColor = HslToColor(hue, newSaturation, lum);
    newColor.A = color.A;
    return newColor;
  }


  // Help from https://gist.github.com/profexorgeek/a407c0c96f69a37a2f2554b43491e247
  private static void ColorToHsl(Color color, out float h, out float s, out float l)
  {
    h = 0;
    s = 0;
    l = 0;

    float r = color.R / 255f;
    float g = color.G / 255f;
    float b = color.B / 255f;
    float min = Math.Min(Math.Min(r, g), b);
    float max = Math.Max(Math.Max(r, g), b);
    float delta = max - min;

    // luminance is the ave of max and min
    l = (max + min) / 2f;


    if (delta > 0)
    {
      if (l < 0.5f)
      {
        s = delta / (max + min);
      }
      else
      {
        s = delta / (2 - max - min);
      }

      float deltaR = ((max - r) / 6f + delta / 2f) / delta;
      float deltaG = ((max - g) / 6f + delta / 2f) / delta;
      float deltaB = ((max - b) / 6f + delta / 2f) / delta;

      if (AlmostEqual(r, max))
      {
        h = deltaB - deltaG;
      }
      else if (AlmostEqual(g, max))
      {
        h = 1f / 3f + deltaR - deltaB;
      }
      else if (AlmostEqual(b, max))
      {
        h = 2f / 3f + deltaG - deltaR;
      }

      if (h < 0)
      {
        h += 1;
      }

      if (h > 1)
      {
        h -= 1;
      }
    }
  }

  private static Color HslToColor(float h, float s, float l)
  {
    var c = new Color();

    if (s == 0)
    {
      c.R = (byte)(l * 255f);
      c.G = (byte)(l * 255f);
      c.B = (byte)(l * 255f);
    }
    else
    {
      float v2 = l + s - s * l;
      if (l < 0.5f)
      {
        v2 = l * (1 + s);
      }

      float v1 = 2f * l - v2;

      c.R = (byte)(255f * HueToRgb(v1, v2, h + 1f / 3f));
      c.G = (byte)(255f * HueToRgb(v1, v2, h));
      c.B = (byte)(255f * HueToRgb(v1, v2, h - 1f / 3f));
    }

    return c;
  }

  private static float HueToRgb(float p, float q, float t)
  {
    t += t < 0 ? 1 : 0;
    t -= t > 1 ? 1 : 0;
    float ret = p;

    if (6 * t < 1)
    {
      ret = p + (q - p) * 6 * t;
    }

    else if (2 * t < 1)
    {
      ret = q;
    }

    else if (3 * t < 2)
    {
      ret = p + (q - p) * (2f / 3f - t) * 6f;
    }

    return Math.Clamp(ret, 0, 1);
  }

  // Comparing floats is hard
  // https://stackoverflow.com/a/67678949
  public static bool AlmostEqual(float a, float b, float? epsilon = null)
  {
    return AlmostEqual((double)a, b, epsilon);
  }

  public static bool AlmostEqual(double a, double b, double? epsilon = null)
  {
    epsilon ??= double.Epsilon;
    double absA = Math.Abs(a);
    double absB = Math.Abs(b);
    double diff = Math.Abs(a - b);

    if (a.Equals(b))
    {
      // shortcut, handles infinities
      return true;
    }

    if (a == 0 || b == 0 || absA + absB < double.MinValue)
    {
      // a or b is zero or both are extremely close to it
      // relative error is less meaningful here
      return diff < epsilon * double.MinValue;
    } // use relative error

    return diff / Math.Min(absA + absB, double.MaxValue) < epsilon;
  }
}
