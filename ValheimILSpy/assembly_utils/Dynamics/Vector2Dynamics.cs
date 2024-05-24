using UnityEngine;

namespace Dynamics;

public class Vector2Dynamics : SecondOrderDynamicsBase<Vector2>
{
	public Vector2Dynamics(float f, float z, float r, Vector2 x0)
		: base(f, z, r, x0)
	{
	}

	public Vector2Dynamics(Vector2 x0)
		: base(x0)
	{
	}

	public Vector2Dynamics(DynamicsParameters parameters, Vector2 x0)
		: base(parameters, x0)
	{
	}

	public Vector2 Update(float dt, Vector2 target)
	{
		_velocity = (target - _previousInput) / dt;
		_previousInput = target;
		float num = Mathf.Max(k2, Mathf.Max(dt * dt / 2f + dt * k1 / 2f, dt * k1));
		_currentValue += dt * _velocity;
		_velocity += dt * (target + k3 * _velocity - _currentValue - k1 * _velocity) / num;
		return _currentValue;
	}
}
