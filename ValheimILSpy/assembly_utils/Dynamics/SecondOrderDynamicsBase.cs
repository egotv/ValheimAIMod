using System;

namespace Dynamics;

public abstract class SecondOrderDynamicsBase<T>
{
	protected T _previousInput;

	protected T _currentValue;

	protected T _velocity;

	protected float k1;

	protected float k2;

	protected float k3;

	protected SecondOrderDynamicsBase(float f, float z, float r, T x0)
	{
		k1 = z / ((float)Math.PI * f);
		k2 = 1f / ((float)Math.PI * 2f * f * ((float)Math.PI * 2f * f));
		k3 = r * z / ((float)Math.PI * 2f * f);
		_previousInput = x0;
		_currentValue = x0;
		_velocity = default(T);
	}

	protected SecondOrderDynamicsBase(T x0)
		: this(3f, 0.5f, 1f, x0)
	{
	}

	protected SecondOrderDynamicsBase(DynamicsParameters parameters, T x0)
		: this(parameters.Frequency, parameters.Damping, parameters.Response, x0)
	{
	}
}
