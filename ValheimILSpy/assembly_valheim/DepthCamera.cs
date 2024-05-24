using UnityEngine;

public class DepthCamera : MonoBehaviour
{
	public Shader m_depthShader;

	public float m_offset = 50f;

	public RenderTexture m_texture;

	public float m_updateInterval = 1f;

	private Camera m_camera;

	private void Start()
	{
		m_camera = GetComponent<Camera>();
		InvokeRepeating("RenderDepth", m_updateInterval, m_updateInterval);
	}

	private void RenderDepth()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!(mainCamera == null))
		{
			Vector3 position = (Player.m_localPlayer ? Player.m_localPlayer.transform.position : mainCamera.transform.position) + Vector3.up * m_offset;
			position.x = Mathf.Round(position.x);
			position.y = Mathf.Round(position.y);
			position.z = Mathf.Round(position.z);
			base.transform.position = position;
			float lodBias = QualitySettings.lodBias;
			QualitySettings.lodBias = 10f;
			m_camera.RenderWithShader(m_depthShader, "RenderType");
			QualitySettings.lodBias = lodBias;
			Shader.SetGlobalTexture("_SkyAlphaTexture", m_texture);
			Shader.SetGlobalVector("_SkyAlphaPosition", base.transform.position);
		}
	}
}
