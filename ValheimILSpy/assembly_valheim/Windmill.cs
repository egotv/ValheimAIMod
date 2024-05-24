using UnityEngine;

public class Windmill : MonoBehaviour
{
	public Transform m_propeller;

	public Transform m_grindstone;

	public Transform m_bom;

	public AudioSource[] m_sfxLoops;

	public GameObject m_propellerAOE;

	public float m_minAOEPropellerSpeed = 5f;

	public float m_bomRotationSpeed = 10f;

	public float m_propellerRotationSpeed = 10f;

	public float m_grindstoneRotationSpeed = 10f;

	public float m_minWindSpeed = 0.1f;

	public float m_minPitch = 1f;

	public float m_maxPitch = 1.5f;

	public float m_maxPitchVel = 10f;

	public float m_maxVol = 1f;

	public float m_maxVolVel = 10f;

	public float m_audioChangeSpeed = 2f;

	private float m_cover;

	private float m_propAngle;

	private float m_grindStoneAngle;

	private Smelter m_smelter;

	private void Start()
	{
		m_smelter = GetComponent<Smelter>();
		InvokeRepeating("CheckCover", 0.1f, 5f);
	}

	private void Update()
	{
		Quaternion to = Quaternion.LookRotation(-EnvMan.instance.GetWindDir());
		float powerOutput = GetPowerOutput();
		m_bom.rotation = Quaternion.RotateTowards(m_bom.rotation, to, m_bomRotationSpeed * powerOutput * Time.deltaTime);
		float num = powerOutput * m_propellerRotationSpeed;
		m_propAngle += num * Time.deltaTime;
		m_propeller.localRotation = Quaternion.Euler(0f, 0f, m_propAngle);
		if (m_smelter == null || m_smelter.IsActive())
		{
			m_grindStoneAngle += powerOutput * m_grindstoneRotationSpeed * Time.deltaTime;
		}
		m_grindstone.localRotation = Quaternion.Euler(0f, m_grindStoneAngle, 0f);
		m_propellerAOE.SetActive(Mathf.Abs(num) > m_minAOEPropellerSpeed);
		UpdateAudio(Time.deltaTime);
	}

	public float GetPowerOutput()
	{
		float num = Utils.LerpStep(m_minWindSpeed, 1f, EnvMan.instance.GetWindIntensity());
		return (1f - m_cover) * num;
	}

	private void CheckCover()
	{
		Cover.GetCoverForPoint(m_propeller.transform.position, out m_cover, out var _);
	}

	private void UpdateAudio(float dt)
	{
		float powerOutput = GetPowerOutput();
		float target = Mathf.Lerp(m_minPitch, m_maxPitch, Mathf.Clamp01(powerOutput / m_maxPitchVel));
		float target2 = m_maxVol * Mathf.Clamp01(powerOutput / m_maxVolVel);
		AudioSource[] sfxLoops = m_sfxLoops;
		foreach (AudioSource obj in sfxLoops)
		{
			obj.volume = Mathf.MoveTowards(obj.volume, target2, m_audioChangeSpeed * dt);
			obj.pitch = Mathf.MoveTowards(obj.pitch, target, m_audioChangeSpeed * dt);
		}
	}
}
