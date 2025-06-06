using UnityEngine;

namespace UnityStandardAssets.ImageEffects;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Rendering/Global Fog")]
internal class GlobalFog : PostEffectsBase
{
	[Tooltip("Apply distance-based fog?")]
	public bool distanceFog = true;

	[Tooltip("Exclude far plane pixels from distance-based fog? (Skybox or clear color)")]
	public bool excludeFarPixels = true;

	[Tooltip("Distance fog is based on radial distance from camera when checked")]
	public bool useRadialDistance;

	[Tooltip("Apply height-based fog?")]
	public bool heightFog = true;

	[Tooltip("Fog top Y coordinate")]
	public float height = 1f;

	[Range(0.001f, 10f)]
	public float heightDensity = 2f;

	[Tooltip("Push fog away from the camera by this amount")]
	public float startDistance;

	public Shader fogShader;

	private Material fogMaterial;

	public override bool CheckResources()
	{
		CheckSupport(needDepth: true);
		fogMaterial = CheckShaderAndCreateMaterial(fogShader, fogMaterial);
		if (!isSupported)
		{
			ReportAutoDisable();
		}
		return isSupported;
	}

	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!CheckResources() || (!distanceFog && !heightFog))
		{
			Graphics.Blit(source, destination);
			return;
		}
		Camera component = GetComponent<Camera>();
		Transform obj = component.transform;
		Vector3[] array = new Vector3[4];
		component.CalculateFrustumCorners(new Rect(0f, 0f, 1f, 1f), component.farClipPlane, component.stereoActiveEye, array);
		Vector3 vector = obj.TransformVector(array[0]);
		Vector3 vector2 = obj.TransformVector(array[1]);
		Vector3 vector3 = obj.TransformVector(array[2]);
		Vector3 vector4 = obj.TransformVector(array[3]);
		Matrix4x4 identity = Matrix4x4.identity;
		identity.SetRow(0, vector);
		identity.SetRow(1, vector4);
		identity.SetRow(2, vector2);
		identity.SetRow(3, vector3);
		Vector3 position = obj.position;
		float num = position.y - height;
		float z = ((num <= 0f) ? 1f : 0f);
		float y = (excludeFarPixels ? 1f : 2f);
		fogMaterial.SetMatrix("_FrustumCornersWS", identity);
		fogMaterial.SetVector("_CameraWS", position);
		fogMaterial.SetVector("_HeightParams", new Vector4(height, num, z, heightDensity * 0.5f));
		fogMaterial.SetVector("_DistanceParams", new Vector4(0f - Mathf.Max(startDistance, 0f), y, 0f, 0f));
		FogMode fogMode = RenderSettings.fogMode;
		float fogDensity = RenderSettings.fogDensity;
		float fogStartDistance = RenderSettings.fogStartDistance;
		float fogEndDistance = RenderSettings.fogEndDistance;
		bool flag = fogMode == FogMode.Linear;
		float num2 = (flag ? (fogEndDistance - fogStartDistance) : 0f);
		float num3 = ((Mathf.Abs(num2) > 0.0001f) ? (1f / num2) : 0f);
		Vector4 value = default(Vector4);
		value.x = fogDensity * 1.2011224f;
		value.y = fogDensity * 1.442695f;
		value.z = (flag ? (0f - num3) : 0f);
		value.w = (flag ? (fogEndDistance * num3) : 0f);
		fogMaterial.SetVector("_SceneFogParams", value);
		fogMaterial.SetVector("_SceneFogMode", new Vector4((float)fogMode, useRadialDistance ? 1 : 0, 0f, 0f));
		int num4 = 0;
		Graphics.Blit(pass: (!distanceFog || !heightFog) ? (distanceFog ? 1 : 2) : 0, source: source, dest: destination, mat: fogMaterial);
	}
}
