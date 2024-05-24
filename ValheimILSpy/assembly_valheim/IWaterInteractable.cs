using UnityEngine;

public interface IWaterInteractable
{
	void SetLiquidLevel(float level, LiquidType type, Component liquidObj);

	Transform GetTransform();

	int Increment(LiquidType type);

	int Decrement(LiquidType type);
}
