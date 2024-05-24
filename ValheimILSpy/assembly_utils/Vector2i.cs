using System;
using UnityEngine;

public struct Vector2i : IEquatable<Vector2i>
{
	public static Vector2i zero = new Vector2i(0, 0);

	public int x;

	public int y;

	public Vector2i(Vector2 v)
	{
		x = (int)v.x;
		y = (int)v.y;
	}

	public Vector2i(Vector3 v)
	{
		x = (int)v.x;
		y = (int)v.y;
	}

	public Vector2i(Vector2i v)
	{
		x = v.x;
		y = v.y;
	}

	public Vector2i(int _x, int _y)
	{
		x = _x;
		y = _y;
	}

	public static Vector2i operator +(Vector2i v0, Vector2i v1)
	{
		return new Vector2i(v0.x + v1.x, v0.y + v1.y);
	}

	public static Vector2i operator -(Vector2i v0, Vector2i v1)
	{
		return new Vector2i(v0.x - v1.x, v0.y - v1.y);
	}

	public static bool operator ==(Vector2i v0, Vector2i v1)
	{
		if (v0.x == v1.x)
		{
			return v0.y == v1.y;
		}
		return false;
	}

	public static bool operator !=(Vector2i v0, Vector2i v1)
	{
		if (v0.x == v1.x)
		{
			return v0.y != v1.y;
		}
		return true;
	}

	public int Magnitude()
	{
		return Mathf.Abs(x) + Mathf.Abs(y);
	}

	public static int Distance(Vector2i a, Vector2i b)
	{
		return (a - b).Magnitude();
	}

	public Vector2 ToVector2()
	{
		return new Vector2(x, y);
	}

	public override string ToString()
	{
		return x + "," + y;
	}

	public override int GetHashCode()
	{
		return x.GetHashCode() ^ y.GetHashCode();
	}

	public bool Equals(Vector2i other)
	{
		if (other.x == x)
		{
			return other.y == y;
		}
		return false;
	}
}
