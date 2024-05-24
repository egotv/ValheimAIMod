using System;
using UnityEngine;

public struct Vector2s : IEquatable<Vector2s>
{
	public short x;

	public short y;

	public static Vector2s zero { get; } = new Vector2s(0, 0);


	public Vector2s(Vector2 v)
	{
		x = (short)v.x;
		y = (short)v.y;
	}

	public Vector2s(Vector3 v)
	{
		x = (short)v.x;
		y = (short)v.y;
	}

	public Vector2s(Vector2s v)
	{
		x = v.x;
		y = v.y;
	}

	public Vector2s(Vector2i v)
	{
		x = (short)v.x;
		y = (short)v.y;
	}

	public Vector2s(short _x, short _y)
	{
		x = _x;
		y = _y;
	}

	public Vector2s(int _x, int _y)
	{
		x = (short)_x;
		y = (short)_y;
	}

	public static Vector2s operator +(Vector2s v0, Vector2s v1)
	{
		return new Vector2s((short)(v0.x + v1.x), (short)(v0.y + v1.y));
	}

	public static Vector2s operator -(Vector2s v0, Vector2s v1)
	{
		return new Vector2s((short)(v0.x - v1.x), (short)(v0.y - v1.y));
	}

	public static bool operator ==(Vector2s v0, Vector2s v1)
	{
		if (v0.x == v1.x)
		{
			return v0.y == v1.y;
		}
		return false;
	}

	public static bool operator !=(Vector2s v0, Vector2s v1)
	{
		if (v0.x == v1.x)
		{
			return v0.y != v1.y;
		}
		return true;
	}

	public static bool operator ==(Vector2s v0, Vector2i v1)
	{
		if (v0.x == v1.x)
		{
			return v0.y == v1.y;
		}
		return false;
	}

	public static bool operator !=(Vector2s v0, Vector2i v1)
	{
		if (v0.x == v1.x)
		{
			return v0.y == v1.y;
		}
		return false;
	}

	public Vector2i ToVector2i()
	{
		return new Vector2i(x, y);
	}

	public int Magnitude()
	{
		return Mathf.Abs(x) + Mathf.Abs(y);
	}

	public static int Distance(Vector2s a, Vector2s b)
	{
		return (a - b).Magnitude();
	}

	public override string ToString()
	{
		return x + "," + y;
	}

	public override int GetHashCode()
	{
		return x.GetHashCode() ^ y.GetHashCode();
	}

	public bool Equals(Vector2s other)
	{
		if (other.x == x)
		{
			return other.y == y;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is Vector2s vector2s)
		{
			if (vector2s.x == x)
			{
				return vector2s.y == y;
			}
			return false;
		}
		return false;
	}
}
