using UnityEngine;
using UnityEngine.Rendering;

namespace LuxParticles;

[ExecuteInEditMode]
public class LuxParticles_AmbientLighting : MonoBehaviour
{
	public bool UpdatePerFrame = true;

	public bool AlwaysUseSH;

	private SphericalHarmonicsL2 probe;

	private Vector4[] SHLighting = new Vector4[7];

	private int Lux_SHAr;

	private int Lux_SHAg;

	private int Lux_SHAb;

	private int Lux_SHBr;

	private int Lux_SHBg;

	private int Lux_SHBb;

	private int Lux_SHC;

	private int Lux_L_SHAr;

	private int Lux_L_SHAg;

	private int Lux_L_SHAb;

	private int Lux_L_SHBr;

	private int Lux_L_SHBg;

	private int Lux_L_SHBb;

	private int Lux_L_SHC;

	private int Lux_AmbientMode;

	private const float k0 = 0.2820948f;

	private const float k1 = 0.48860252f;

	private const float k2 = 1.0925485f;

	private const float k3 = 0.31539157f;

	private const float k4 = 0.54627424f;

	private static float[] ks = new float[9] { 0.2820948f, -0.48860252f, 0.48860252f, -0.48860252f, 1.0925485f, -1.0925485f, 0.31539157f, -1.0925485f, 0.54627424f };

	private int managedParticleSystems;

	private void OnEnable()
	{
		Lux_SHAr = Shader.PropertyToID("_Lux_SHAr");
		Lux_SHAg = Shader.PropertyToID("_Lux_SHAg");
		Lux_SHAb = Shader.PropertyToID("_Lux_SHAb");
		Lux_SHBr = Shader.PropertyToID("_Lux_SHBr");
		Lux_SHBg = Shader.PropertyToID("_Lux_SHBg");
		Lux_SHBb = Shader.PropertyToID("_Lux_SHBb");
		Lux_SHC = Shader.PropertyToID("_Lux_SHC");
		Lux_L_SHAr = Shader.PropertyToID("_Lux_L_SHAr");
		Lux_L_SHAg = Shader.PropertyToID("_Lux_L_SHAg");
		Lux_L_SHAb = Shader.PropertyToID("_Lux_L_SHAb");
		Lux_L_SHBr = Shader.PropertyToID("_Lux_L_SHBr");
		Lux_L_SHBg = Shader.PropertyToID("_Lux_L_SHBg");
		Lux_L_SHBb = Shader.PropertyToID("_Lux_L_SHBb");
		Lux_L_SHC = Shader.PropertyToID("_Lux_L_SHC");
		Lux_AmbientMode = Shader.PropertyToID("_Lux_AmbientMode");
		Invoke("UpdateAmbientLighting", 0f);
	}

	private void LateUpdate()
	{
		if (UpdatePerFrame)
		{
			UpdateAmbientLighting();
		}
		else if (LuxParticles_LocalAmbientLighting.LocalProbes != null)
		{
			if (managedParticleSystems < LuxParticles_LocalAmbientLighting.LocalProbes.Count)
			{
				UpdateAmbientLightingForNewParticleSystems();
			}
			managedParticleSystems = LuxParticles_LocalAmbientLighting.LocalProbes.Count;
		}
	}

	public void UpdateAmbientLighting()
	{
		bool flag = false;
		if (LuxParticles_LocalAmbientLighting.LocalProbes != null && LuxParticles_LocalAmbientLighting.LocalProbes.Count > 0)
		{
			flag = true;
		}
		if (RenderSettings.ambientMode == AmbientMode.Flat && !flag && !AlwaysUseSH)
		{
			Shader.SetGlobalFloat(Lux_AmbientMode, 0f);
			return;
		}
		if (RenderSettings.ambientMode == AmbientMode.Trilight && !flag && !AlwaysUseSH)
		{
			Shader.SetGlobalFloat(Lux_AmbientMode, 1f);
			return;
		}
		Shader.SetGlobalFloat(Lux_AmbientMode, 2f);
		if (RenderSettings.ambientMode == AmbientMode.Skybox)
		{
			probe = RenderSettings.ambientProbe;
		}
		else
		{
			LightProbes.GetInterpolatedProbe(base.transform.position, null, out probe);
		}
		PremultiplyCoefficients(probe);
		GetShaderConstantsFromNormalizedSH(ref probe, IsSkyLighting: true);
		SetSHLighting();
		if (LuxParticles_LocalAmbientLighting.LocalProbes == null)
		{
			return;
		}
		for (int i = 0; i != LuxParticles_LocalAmbientLighting.LocalProbes.Count; i++)
		{
			LuxParticles_LocalAmbientLighting luxParticles_LocalAmbientLighting = LuxParticles_LocalAmbientLighting.LocalProbes[i];
			if (luxParticles_LocalAmbientLighting.IsVisible)
			{
				LightProbes.GetInterpolatedProbe(luxParticles_LocalAmbientLighting.trans.position + luxParticles_LocalAmbientLighting.SampleOffset, null, out probe);
				PremultiplyCoefficients(probe);
				GetShaderConstantsFromNormalizedSH(ref probe, IsSkyLighting: false);
				MaterialPropertyBlock block = LuxParticles_LocalAmbientLighting.LocalProbes[i].m_block;
				block.Clear();
				block.SetVector(Lux_L_SHAr, SHLighting[0]);
				block.SetVector(Lux_L_SHAg, SHLighting[1]);
				block.SetVector(Lux_L_SHAb, SHLighting[2]);
				block.SetVector(Lux_L_SHBr, SHLighting[3]);
				block.SetVector(Lux_L_SHBg, SHLighting[4]);
				block.SetVector(Lux_L_SHBb, SHLighting[5]);
				block.SetVector(Lux_L_SHC, SHLighting[6]);
				LuxParticles_LocalAmbientLighting.LocalProbes[i].rend.SetPropertyBlock(block);
			}
		}
	}

	public void UpdateAmbientLightingForNewParticleSystems()
	{
		int count = LuxParticles_LocalAmbientLighting.LocalProbes.Count;
		for (int i = managedParticleSystems; i != count; i++)
		{
			LuxParticles_LocalAmbientLighting luxParticles_LocalAmbientLighting = LuxParticles_LocalAmbientLighting.LocalProbes[i];
			LightProbes.GetInterpolatedProbe(luxParticles_LocalAmbientLighting.trans.position + luxParticles_LocalAmbientLighting.SampleOffset, null, out probe);
			PremultiplyCoefficients(probe);
			GetShaderConstantsFromNormalizedSH(ref probe, IsSkyLighting: false);
			MaterialPropertyBlock block = LuxParticles_LocalAmbientLighting.LocalProbes[i].m_block;
			block.Clear();
			block.SetVector(Lux_L_SHAr, SHLighting[0]);
			block.SetVector(Lux_L_SHAg, SHLighting[1]);
			block.SetVector(Lux_L_SHAb, SHLighting[2]);
			block.SetVector(Lux_L_SHBr, SHLighting[3]);
			block.SetVector(Lux_L_SHBg, SHLighting[4]);
			block.SetVector(Lux_L_SHBb, SHLighting[5]);
			block.SetVector(Lux_L_SHC, SHLighting[6]);
			LuxParticles_LocalAmbientLighting.LocalProbes[i].rend.SetPropertyBlock(block);
		}
	}

	private static SphericalHarmonicsL2 PremultiplyCoefficients(SphericalHarmonicsL2 sh)
	{
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				sh[i, j] *= ks[j];
			}
		}
		return sh;
	}

	private void GetShaderConstantsFromNormalizedSH(ref SphericalHarmonicsL2 ambientProbe, bool IsSkyLighting)
	{
		float num = 1f;
		if (IsSkyLighting)
		{
			num = RenderSettings.ambientIntensity;
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				num = Mathf.Pow(num, 2.2f);
			}
		}
		for (int i = 0; i < 3; i++)
		{
			SHLighting[i].x = ambientProbe[i, 3] * num;
			SHLighting[i].y = ambientProbe[i, 1] * num;
			SHLighting[i].z = ambientProbe[i, 2] * num;
			SHLighting[i].w = (ambientProbe[i, 0] - ambientProbe[i, 6]) * num;
			SHLighting[i + 3].x = ambientProbe[i, 4] * num;
			SHLighting[i + 3].y = ambientProbe[i, 5] * num;
			SHLighting[i + 3].z = ambientProbe[i, 6] * 3f * num;
			SHLighting[i + 3].w = ambientProbe[i, 7] * num;
		}
		SHLighting[6].x = ambientProbe[0, 8] * num;
		SHLighting[6].y = ambientProbe[1, 8] * num;
		SHLighting[6].z = ambientProbe[2, 8] * num;
		SHLighting[6].w = 1f;
	}

	private void SetSHLighting()
	{
		Shader.SetGlobalVector(Lux_SHAr, SHLighting[0]);
		Shader.SetGlobalVector(Lux_SHAg, SHLighting[1]);
		Shader.SetGlobalVector(Lux_SHAb, SHLighting[2]);
		Shader.SetGlobalVector(Lux_SHBr, SHLighting[3]);
		Shader.SetGlobalVector(Lux_SHBg, SHLighting[4]);
		Shader.SetGlobalVector(Lux_SHBb, SHLighting[5]);
		Shader.SetGlobalVector(Lux_SHC, SHLighting[6]);
	}
}
