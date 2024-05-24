namespace LlamAcademy.Spring;

public abstract class BaseSpring<T>
{
	public virtual float Damping { get; set; } = 26f;


	public virtual float Mass { get; set; } = 1f;


	public virtual float Stiffness { get; set; } = 169f;


	public virtual T StartValue { get; set; }

	public virtual T EndValue { get; set; }

	public virtual T InitialVelocity { get; set; }

	public virtual T CurrentValue { get; set; }

	public virtual T CurrentVelocity { get; set; }

	public abstract void Reset();

	public virtual void UpdateEndValue(T Value)
	{
		UpdateEndValue(Value, CurrentVelocity);
	}

	public abstract void UpdateEndValue(T Value, T Velocity);

	public abstract T Evaluate(float DeltaTime);
}
