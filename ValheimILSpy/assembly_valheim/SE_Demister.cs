using UnityEngine;

public class SE_Demister : StatusEffect
{
	[Header("SE_Demister")]
	public GameObject m_ballPrefab;

	public Vector3 m_offset = new Vector3(0f, 2f, 0f);

	public Vector3 m_offsetInterior = new Vector3(0.5f, 1.8f, 0f);

	public float m_maxDistance = 50f;

	public float m_ballAcceleration = 4f;

	public float m_ballMaxSpeed = 10f;

	public float m_ballFriction = 0.1f;

	public float m_noiseDistance = 1f;

	public float m_noiseDistanceInterior = 0.2f;

	public float m_noiseDistanceYScale = 1f;

	public float m_noiseSpeed = 1f;

	public float m_characterVelocityFactor = 1f;

	public float m_rotationSpeed = 1f;

	private int m_coverRayMask;

	private GameObject m_ballInstance;

	private Vector3 m_ballVel = new Vector3(0f, 0f, 0f);

	public override void Setup(Character character)
	{
		base.Setup(character);
		if (m_coverRayMask == 0)
		{
			m_coverRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
		}
	}

	private bool IsUnderRoof()
	{
		if (Physics.Raycast(m_character.GetCenterPoint(), Vector3.up, out var _, 4f, m_coverRayMask))
		{
			return true;
		}
		return false;
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!m_ballInstance)
		{
			Vector3 position = m_character.GetCenterPoint() + m_character.transform.forward * 0.5f;
			m_ballInstance = Object.Instantiate(m_ballPrefab, position, Quaternion.identity);
			return;
		}
		_ = m_character;
		bool num = IsUnderRoof();
		Vector3 position2 = m_character.transform.position;
		Vector3 vector = m_ballInstance.transform.position;
		Vector3 vector2 = (num ? m_offsetInterior : m_offset);
		float num2 = (num ? m_noiseDistanceInterior : m_noiseDistance);
		Vector3 vector3 = position2 + m_character.transform.TransformVector(vector2);
		float num3 = Time.time * m_noiseSpeed;
		vector3 += new Vector3(Mathf.Sin(num3 * 4f), Mathf.Sin(num3 * 2f) * m_noiseDistanceYScale, Mathf.Cos(num3 * 5f)) * num2;
		float num4 = Vector3.Distance(vector3, vector);
		if (num4 > m_maxDistance * 2f)
		{
			vector = vector3;
		}
		else if (num4 > m_maxDistance)
		{
			Vector3 normalized = (vector - vector3).normalized;
			vector = vector3 + normalized * m_maxDistance;
		}
		Vector3 normalized2 = (vector3 - vector).normalized;
		m_ballVel += normalized2 * m_ballAcceleration * dt;
		if (m_ballVel.magnitude > m_ballMaxSpeed)
		{
			m_ballVel = m_ballVel.normalized * m_ballMaxSpeed;
		}
		if (!num)
		{
			Vector3 velocity = m_character.GetVelocity();
			m_ballVel += velocity * m_characterVelocityFactor * dt;
		}
		m_ballVel -= m_ballVel * m_ballFriction;
		Vector3 position3 = vector + m_ballVel * dt;
		m_ballInstance.transform.position = position3;
		Quaternion rotation = m_ballInstance.transform.rotation;
		rotation *= Quaternion.Euler(m_rotationSpeed, 0f, m_rotationSpeed * 0.5321f);
		m_ballInstance.transform.rotation = rotation;
	}

	private void RemoveEffects()
	{
		if (m_ballInstance != null)
		{
			ZNetView component = m_ballInstance.GetComponent<ZNetView>();
			if (component.IsValid())
			{
				component.ClaimOwnership();
				component.Destroy();
			}
		}
	}

	protected override void OnApplicationQuit()
	{
		base.OnApplicationQuit();
		m_ballInstance = null;
	}

	public override void Stop()
	{
		base.Stop();
		RemoveEffects();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		RemoveEffects();
	}
}
