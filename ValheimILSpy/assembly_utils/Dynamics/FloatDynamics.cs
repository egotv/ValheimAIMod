using UnityEngine;

namespace Dynamics;

public class FloatDynamics : SecondOrderDynamicsBase<float>
{
	public FloatDynamics(float f, float z, float r, float x0)
		: base(f, z, r, x0)
	{
	}

	public FloatDynamics(float x0)
		: base(x0)
	{
	}

	public FloatDynamics(DynamicsParameters parameters, float x0)
		: base(parameters, x0)
	{
	}

	public float Update(float dt, float target, float velocity = float.NegativeInfinity, bool recalculateVelocity = false)
	{
		if (!float.IsInfinity(velocity))
		{
			_velocity = velocity;
		}
		else if (recalculateVelocity)
		{
			_velocity = (target - _previousInput) / dt;
			_previousInput = target;
		}
		float num = Mathf.Max(k2, Mathf.Max(dt * dt / 2f + dt * k1 / 2f, dt * k1));
		_currentValue += dt * _velocity;
		_velocity += dt * (target + k3 * _velocity - _currentValue - k1 * _velocity) / num;
		return _currentValue;
	}
}
