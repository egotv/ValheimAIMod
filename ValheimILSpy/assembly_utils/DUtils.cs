using System;
using UnityEngine;

public static class DUtils
{
	public static float Length(float x, float y)
	{
		return (float)Math.Sqrt((double)x * (double)x + (double)y * (double)y);
	}

	public static double Length(double x, double y)
	{
		return Math.Sqrt(x * x + y * y);
	}

	public static double BlendOverlay(double a, double b)
	{
		double result = 2.0 * a * b;
		double result2 = 1.0 - 2.0 * (1.0 - a) * (1.0 - b);
		if (!(a < 0.5))
		{
			return result2;
		}
		return result;
	}

	public static float Lerp(float a, float b, float t)
	{
		if (t <= 0f)
		{
			return a;
		}
		if (t >= 1f)
		{
			return b;
		}
		return (float)((double)a * (1.0 - (double)t) + (double)b * (double)t);
	}

	public static double Lerp(double a, double b, double t)
	{
		if (t <= 0.0)
		{
			return a;
		}
		if (t >= 1.0)
		{
			return b;
		}
		return a * (1.0 - t) + b * t;
	}

	public static float LerpStep(float l, float h, float v)
	{
		return (float)Clamp01(((double)v - (double)l) / ((double)h - (double)l));
	}

	public static double LerpStep(double l, double h, double v)
	{
		return Clamp01((v - l) / (h - l));
	}

	public static float SmoothStep(float p_Min, float p_Max, float p_X)
	{
		float num = (float)Clamp01(((double)p_X - (double)p_Min) / ((double)p_Max - (double)p_Min));
		return (float)((double)num * (double)num * (3.0 - 2.0 * (double)num));
	}

	public static double MathfLikeSmoothStep(double from, double to, double t)
	{
		t = Clamp01(t);
		t = -2.0 * t * t * t + 3.0 * t * t;
		return (float)(to * t + from * (1.0 - t));
	}

	public static double Clamp01(double v)
	{
		if (v > 1.0)
		{
			return 1.0;
		}
		if (v < 0.0)
		{
			return 0.0;
		}
		return v;
	}

	public static float Fbm(Vector3 p, int octaves, float lacunarity, float gain)
	{
		return Fbm(new Vector2(p.x, p.z), octaves, lacunarity, gain);
	}

	public static float Fbm(Vector2 p, int octaves, float lacunarity, float gain)
	{
		float num = 0f;
		float num2 = 1f;
		Vector2 vector = p;
		for (int i = 0; i < octaves; i++)
		{
			num = (float)((double)num + (double)num2 * (double)PerlinNoise(vector.x, vector.y));
			num2 = (float)((double)num2 * (double)gain);
			vector *= lacunarity;
		}
		return num;
	}

	public static double Fbm(Vector2 p, int octaves, double lacunarity, double gain)
	{
		double num = 0.0;
		double num2 = 1.0;
		double num3 = p.x;
		double num4 = p.y;
		for (int i = 0; i < octaves; i++)
		{
			num += num2 * (double)PerlinNoise(num3, num4);
			num2 *= gain;
			num3 *= lacunarity;
			num4 *= lacunarity;
		}
		return num;
	}

	public static double Remap(double value, double inLow, double inHigh, double outLow, double outHigh)
	{
		return Lerp(outLow, outHigh, InverseLerp(inLow, inHigh, value));
	}

	public static double InverseLerp(double a, double b, double value)
	{
		if (a == b)
		{
			return 0.0;
		}
		return Clamp01((value - a) / (b - a));
	}

	public static float PerlinNoise(double x, double y)
	{
		return Mathf.PerlinNoise((float)x, (float)y);
	}
}
