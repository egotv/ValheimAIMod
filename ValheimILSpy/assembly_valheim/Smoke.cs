using System;
using System.Collections.Generic;
using UnityEngine;

public class Smoke : MonoBehaviour, IMonoUpdater
{
	public Vector3 m_vel = Vector3.up;

	public float m_randomVel = 0.1f;

	public float m_force = 0.1f;

	public float m_ttl = 10f;

	public float m_fadetime = 3f;

	private Rigidbody m_body;

	private float m_time;

	private float m_fadeTimer = -1f;

	private bool m_added;

	private float m_alpha = 1f;

	private MeshRenderer m_mr;

	private MaterialPropertyBlock m_propertyBlock;

	private static readonly List<Smoke> s_smoke = new List<Smoke>();

	private static readonly int m_colorProp = Shader.PropertyToID("_Color");

	private static readonly int m_randomAngleProp = Shader.PropertyToID("_RandomAngle");

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		s_smoke.Add(this);
		m_added = true;
		m_body = GetComponent<Rigidbody>();
		m_mr = GetComponent<MeshRenderer>();
		m_propertyBlock = new MaterialPropertyBlock();
		Color color = m_mr.material.color;
		color.a = 0f;
		m_propertyBlock.SetColor(m_colorProp, color);
		m_propertyBlock.SetFloat(m_randomAngleProp, UnityEngine.Random.Range(0f, (float)Math.PI * 2f));
		m_mr.SetPropertyBlock(m_propertyBlock);
		m_body.maxDepenetrationVelocity = 1f;
		m_vel = Vector3.up + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * m_randomVel;
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	private void OnDestroy()
	{
		if (m_added)
		{
			s_smoke.Remove(this);
			m_added = false;
		}
	}

	public void StartFadeOut()
	{
		if (!(m_fadeTimer >= 0f))
		{
			if (m_added)
			{
				s_smoke.Remove(this);
				m_added = false;
			}
			m_fadeTimer = 0f;
		}
	}

	public static int GetTotalSmoke()
	{
		return s_smoke.Count;
	}

	public static void FadeOldest()
	{
		if (s_smoke.Count != 0)
		{
			s_smoke[0].StartFadeOut();
		}
	}

	public static void FadeMostDistant()
	{
		if (s_smoke.Count == 0)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 position = mainCamera.transform.position;
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < s_smoke.Count; i++)
		{
			float num3 = Vector3.Distance(s_smoke[i].transform.position, position);
			if (num3 > num2)
			{
				num = i;
				num2 = num3;
			}
		}
		if (num != -1)
		{
			s_smoke[num].StartFadeOut();
		}
	}

	public void CustomUpdate(float deltaTime, float time)
	{
		m_alpha = Mathf.Clamp01(m_time);
		m_time += deltaTime;
		if (m_time > m_ttl && m_fadeTimer < 0f)
		{
			StartFadeOut();
		}
		float num = 1f - Mathf.Clamp01(m_time / m_ttl);
		m_body.mass = num * num;
		Vector3 velocity = m_body.velocity;
		Vector3 vel = m_vel;
		vel.y *= num;
		Vector3 vector = vel - velocity;
		m_body.AddForce(vector * (m_force * deltaTime), ForceMode.VelocityChange);
		if (m_fadeTimer >= 0f)
		{
			m_fadeTimer += deltaTime;
			float num2 = 1f - Mathf.Clamp01(m_fadeTimer / m_fadetime);
			m_alpha *= num2;
			if (m_fadeTimer >= m_fadetime)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
		if (m_time <= 1f || m_fadeTimer > 0f)
		{
			Color color = m_propertyBlock.GetColor(m_colorProp);
			color.a = m_alpha;
			m_propertyBlock.SetColor(m_colorProp, color);
			m_mr.SetPropertyBlock(m_propertyBlock);
		}
	}
}
