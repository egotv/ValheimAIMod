using UnityEngine;

public class PlatformEnable : MonoBehaviour
{
	public Platforms m_enabledPlatforms = Platforms.All;

	private void Awake()
	{
		base.gameObject.SetActive(m_enabledPlatforms.HasFlag(Version.GetPlatform()));
	}
}
