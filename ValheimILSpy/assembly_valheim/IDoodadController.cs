using UnityEngine;

public interface IDoodadController
{
	void OnUseStop(Player player);

	void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block);

	Component GetControlledComponent();

	Vector3 GetPosition();

	bool IsValid();
}
