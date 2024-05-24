using UnityEngine;

namespace LlamAcademy.Spring;

public class FloatSpring : BaseSpring<float>
{
	protected float SpringTime;

	public override void Reset()
	{
		SpringTime = 0f;
		CurrentValue = 0f;
		CurrentVelocity = 0f;
		InitialVelocity = 0f;
	}

	public override void UpdateEndValue(float Value, float Velocity)
	{
		StartValue = CurrentValue;
		EndValue = Value;
		InitialVelocity = Velocity;
		SpringTime = 0f;
	}

	public override float Evaluate(float deltaTime)
	{
		SpringTime += deltaTime;
		float damping = Damping;
		float mass = Mass;
		float stiffness = Stiffness;
		float num = 0f - InitialVelocity;
		float springTime = SpringTime;
		float num2 = damping / (2f * Mathf.Sqrt(stiffness * mass));
		float num3 = Mathf.Sqrt(stiffness / mass);
		float num4 = EndValue - StartValue;
		float num5 = num3 * num2;
		float num12;
		float currentVelocity;
		if (num2 < 1f)
		{
			float num6 = num3 * Mathf.Sqrt(1f - num2 * num2);
			float num7 = Mathf.Exp((0f - num5) * springTime);
			float num8 = num4;
			float num9 = (num + num5 * num4) / num6;
			float num10 = Mathf.Cos(num6 * springTime);
			float num11 = Mathf.Sin(num6 * springTime);
			num12 = num7 * (num8 * num10 + num9 * num11);
			currentVelocity = (0f - num7) * ((num4 * num5 - num9 * num6) * num10 + (num4 * num6 + num9 * num5) * num11);
		}
		else if (num2 > 1f)
		{
			float num13 = num3 * Mathf.Sqrt(num2 * num2 - 1f);
			float num14 = 0f - num5 - num13;
			float num15 = 0f - num5 + num13;
			float num16 = Mathf.Exp(num14 * springTime);
			float num17 = Mathf.Exp(num15 * springTime);
			float num18 = (num - num4 * num15) / (-2f * num13);
			float num19 = num4 - num18;
			num12 = num18 * num16 + num19 * num17;
			currentVelocity = num18 * num14 * num16 + num19 * num15 * num17;
		}
		else
		{
			float num20 = Mathf.Exp((0f - num3) * springTime);
			num12 = num20 * (num4 + (num + num3 * num4) * springTime);
			currentVelocity = num20 * (num * (1f - springTime * num3) + springTime * num4 * (num3 * num3));
		}
		CurrentValue = EndValue - num12;
		CurrentVelocity = currentVelocity;
		return CurrentValue;
	}
}
