using UnityEngine;

public static class VectorExtensions
{
	public static Vector3 To(this Vector3 a, Vector3 b)
	{
		return b - a;
	}

	public static Vector3 DirTo(this Vector3 a, Vector3 b)
	{
		return Vector3.Normalize(b - a);
	}

	public static float DistanceTo(this Vector3 a, Vector3 b)
	{
		return Vector3.Distance(a, b);
	}

	public static Vector3 Horizontal(this Vector3 a)
	{
		a.y = 0f;
		return a;
	}

	public static Vector3 Vertical(this Vector3 a)
	{
		a.x = 0f;
		a.z = 0f;
		return a;
	}

	public static Color ToColor(this Vector3 c)
	{
		return new Color(c.x, c.y, c.z);
	}
}
