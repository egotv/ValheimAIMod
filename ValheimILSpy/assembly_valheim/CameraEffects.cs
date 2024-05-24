using UnityEngine;
using UnityEngine.PostProcessing;
using UnityStandardAssets.ImageEffects;

public class CameraEffects : MonoBehaviour
{
	private static CameraEffects m_instance;

	public bool m_forceDof;

	public LayerMask m_dofRayMask;

	public bool m_dofAutoFocus;

	public float m_dofMinDistance = 50f;

	public float m_dofMinDistanceShip = 50f;

	public float m_dofMaxDistance = 3000f;

	private PostProcessingBehaviour m_postProcessing;

	private DepthOfField m_dof;

	public static CameraEffects instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_postProcessing = GetComponent<PostProcessingBehaviour>();
		m_dof = GetComponent<DepthOfField>();
		ApplySettings();
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public void ApplySettings()
	{
		SetDof(PlatformPrefs.GetInt("DOF", 1) == 1);
		SetBloom(PlatformPrefs.GetInt("Bloom", 1) == 1);
		SetSSAO(PlatformPrefs.GetInt("SSAO", 1) == 1);
		SetSunShafts(PlatformPrefs.GetInt("SunShafts", 1) == 1);
		SetAntiAliasing(PlatformPrefs.GetInt("AntiAliasing", 1) == 1);
		SetCA(PlatformPrefs.GetInt("ChromaticAberration", 1) == 1);
		SetMotionBlur(PlatformPrefs.GetInt("MotionBlur", 1) == 1);
	}

	public void SetSunShafts(bool enabled)
	{
		SunShafts component = GetComponent<SunShafts>();
		if (component != null)
		{
			component.enabled = enabled;
		}
	}

	private void SetBloom(bool enabled)
	{
		m_postProcessing.profile.bloom.enabled = enabled;
	}

	private void SetSSAO(bool enabled)
	{
		m_postProcessing.profile.ambientOcclusion.enabled = enabled;
	}

	private void SetMotionBlur(bool enabled)
	{
		m_postProcessing.profile.motionBlur.enabled = enabled;
	}

	private void SetAntiAliasing(bool enabled)
	{
		m_postProcessing.profile.antialiasing.enabled = enabled;
	}

	private void SetCA(bool enabled)
	{
		m_postProcessing.profile.chromaticAberration.enabled = enabled;
	}

	private void SetDof(bool enabled)
	{
		m_dof.enabled = enabled || m_forceDof;
	}

	private void LateUpdate()
	{
		UpdateDOF();
	}

	private bool ControllingShip()
	{
		if (Player.m_localPlayer == null || Player.m_localPlayer.GetControlledShip() != null)
		{
			return true;
		}
		return false;
	}

	private void UpdateDOF()
	{
		if (m_dof.enabled && m_dofAutoFocus)
		{
			float num = m_dofMaxDistance;
			if (Physics.Raycast(base.transform.position, base.transform.forward, out var hitInfo, m_dofMaxDistance, m_dofRayMask))
			{
				num = hitInfo.distance;
			}
			if (ControllingShip() && num < m_dofMinDistanceShip)
			{
				num = m_dofMinDistanceShip;
			}
			if (num < m_dofMinDistance)
			{
				num = m_dofMinDistance;
			}
			m_dof.focalLength = Mathf.Lerp(m_dof.focalLength, num, 0.2f);
		}
	}
}
