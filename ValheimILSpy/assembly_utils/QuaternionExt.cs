using UnityEngine;

public static class QuaternionExt
{
	public static Quaternion GetNormalized(this Quaternion q)
	{
		float num = 1f / Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
		return new Quaternion(q.x * num, q.y * num, q.z * num, q.w * num);
	}
}
