public interface IDestructible
{
	void Damage(HitData hit);

	DestructibleType GetDestructibleType();
}
