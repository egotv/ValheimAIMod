using System;

[Serializable]
public class EnvEntry
{
	public string m_environment = "";

	public float m_weight = 1f;

	public bool m_ashlandsOverride;

	public bool m_deepnorthOverride;

	[NonSerialized]
	public EnvSetup m_env;
}
