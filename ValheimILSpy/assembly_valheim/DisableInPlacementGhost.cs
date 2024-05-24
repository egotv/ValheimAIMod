using System.Collections.Generic;
using UnityEngine;

public class DisableInPlacementGhost : MonoBehaviour
{
	public List<Behaviour> m_components;

	public List<GameObject> m_objects;

	private void Start()
	{
		if (!Player.IsPlacementGhost(base.gameObject))
		{
			return;
		}
		foreach (Behaviour component in m_components)
		{
			component.enabled = false;
		}
		foreach (GameObject @object in m_objects)
		{
			@object.SetActive(value: false);
		}
	}
}
