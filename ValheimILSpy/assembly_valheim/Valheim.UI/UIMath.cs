using UnityEngine;

namespace Valheim.UI;

public static class UIMath
{
	public static (float, float) CartToPolar(float x, float y)
	{
		float item = Mathf.Atan2(y, x);
		float item2 = Mathf.Sqrt(x * x + y * y);
		return (item, item2);
	}
}
