using System.Collections.Generic;
using UnityEngine;

public class SmokeRenderer : MonoBehaviour
{
	private InstanceRenderer m_instanceRenderer;

	private List<Vector4> tempTransforms = new List<Vector4>();

	private void Start()
	{
		m_instanceRenderer = GetComponent<InstanceRenderer>();
	}

	private void Update()
	{
		if (!(Utils.GetMainCamera() == null))
		{
			UpdateInstances();
		}
	}

	private void UpdateInstances()
	{
	}
}
