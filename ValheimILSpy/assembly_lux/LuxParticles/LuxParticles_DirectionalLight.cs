using UnityEngine;
using UnityEngine.Rendering;

namespace LuxParticles;

[ExecuteInEditMode]
[RequireComponent(typeof(Light))]
public class LuxParticles_DirectionalLight : MonoBehaviour
{
	private Light m_light;

	private CommandBuffer GetShadowCascades_CB;

	private void OnEnable()
	{
		m_light = GetComponent<Light>();
		if (GetShadowCascades_CB == null)
		{
			GetShadowCascades_CB = new CommandBuffer();
			GetShadowCascades_CB.name = "LuxParticles GetShadowCascades";
			GetShadowCascades_CB.SetGlobalTexture("_LuxParticles_CascadedShadowMap", BuiltinRenderTextureType.CurrentActive);
		}
		m_light.AddCommandBuffer(LightEvent.AfterShadowMap, GetShadowCascades_CB);
	}

	private void OnDisable()
	{
		if ((bool)GetComponent<Light>() && GetShadowCascades_CB != null)
		{
			GetComponent<Light>().RemoveCommandBuffer(LightEvent.AfterShadowMap, GetShadowCascades_CB);
		}
	}

	private void OnDestroy()
	{
		if (GetShadowCascades_CB != null)
		{
			GetShadowCascades_CB.Release();
			GetShadowCascades_CB = null;
		}
	}
}
