using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LineAttach : MonoBehaviour, IMonoUpdater
{
	public List<Transform> m_attachments = new List<Transform>();

	private LineRenderer m_lineRenderer;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Start()
	{
		m_lineRenderer = GetComponent<LineRenderer>();
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public void CustomLateUpdate(float deltaTime)
	{
		for (int i = 0; i < m_attachments.Count; i++)
		{
			Transform transform = m_attachments[i];
			if ((bool)transform)
			{
				m_lineRenderer.SetPosition(i, base.transform.InverseTransformPoint(transform.position));
			}
		}
	}
}
