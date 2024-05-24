using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuxParticles;

[ExecuteInEditMode]
public class LuxParticles_LocalAmbientLighting : MonoBehaviour
{
	public static List<LuxParticles_LocalAmbientLighting> LocalProbes = new List<LuxParticles_LocalAmbientLighting>();

	public Vector3 SampleOffset = Vector3.zero;

	[NonSerialized]
	public Transform trans;

	[NonSerialized]
	public Renderer rend;

	[NonSerialized]
	public MaterialPropertyBlock m_block;

	[NonSerialized]
	public bool IsVisible;

	private void OnEnable()
	{
		trans = GetComponent<Transform>();
		rend = GetComponent<Renderer>();
		m_block = new MaterialPropertyBlock();
		IsVisible = true;
		Register();
	}

	private void Register()
	{
		LocalProbes.Add(this);
	}

	private void OnDisable()
	{
		LocalProbes.Remove(this);
		if (m_block != null)
		{
			m_block.Clear();
			rend.SetPropertyBlock(m_block);
			m_block = null;
		}
	}

	private void OnDestroy()
	{
		LocalProbes.Remove(this);
		if (m_block != null)
		{
			m_block.Clear();
			rend.SetPropertyBlock(m_block);
			m_block = null;
		}
	}

	private void OnBecameVisible()
	{
		IsVisible = true;
	}

	private void OnBecameInvisible()
	{
		IsVisible = false;
	}
}
