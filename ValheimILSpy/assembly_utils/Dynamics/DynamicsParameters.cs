using System;

namespace Dynamics;

[Serializable]
public class DynamicsParameters
{
	public float Frequency = 3f;

	public float Damping = 0.5f;

	public float Response = 1f;
}
