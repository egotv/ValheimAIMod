using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PostEffectsBase : MonoBehaviour
{
	protected bool supportHDRTextures = true;

	protected bool supportDX11;

	protected bool isSupported = true;

	private bool s_supportsARGBHalf;

	private bool s_supportsDepth;

	private List<Material> createdMaterials = new List<Material>();

	protected Material CheckShaderAndCreateMaterial(Shader s, Material m2Create)
	{
		if (!s)
		{
			Debug.Log("Missing shader in " + ToString());
			base.enabled = false;
			return null;
		}
		if (s.isSupported && (bool)m2Create && m2Create.shader == s)
		{
			return m2Create;
		}
		if (!s.isSupported)
		{
			NotSupported();
			Debug.Log("The shader " + s.ToString() + " on effect " + ToString() + " is not supported on this platform!");
			return null;
		}
		m2Create = new Material(s);
		createdMaterials.Add(m2Create);
		m2Create.hideFlags = HideFlags.DontSave;
		return m2Create;
	}

	protected Material CreateMaterial(Shader s, Material m2Create)
	{
		if (!s)
		{
			Debug.Log("Missing shader in " + ToString());
			return null;
		}
		if ((bool)m2Create && m2Create.shader == s && s.isSupported)
		{
			return m2Create;
		}
		if (!s.isSupported)
		{
			return null;
		}
		m2Create = new Material(s);
		createdMaterials.Add(m2Create);
		m2Create.hideFlags = HideFlags.DontSave;
		return m2Create;
	}

	private void OnEnable()
	{
		isSupported = true;
	}

	private void OnDestroy()
	{
		RemoveCreatedMaterials();
	}

	private void RemoveCreatedMaterials()
	{
		while (createdMaterials.Count > 0)
		{
			Material obj = createdMaterials[0];
			createdMaterials.RemoveAt(0);
			Object.Destroy(obj);
		}
	}

	protected bool CheckSupport()
	{
		return CheckSupport(needDepth: false);
	}

	public virtual bool CheckResources()
	{
		Debug.LogWarning("CheckResources () for " + ToString() + " should be overwritten.");
		return isSupported;
	}

	protected void Start()
	{
		s_supportsARGBHalf = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
		s_supportsDepth = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
		CheckResources();
	}

	protected bool CheckSupport(bool needDepth)
	{
		isSupported = true;
		supportHDRTextures = s_supportsARGBHalf;
		supportDX11 = SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders;
		if (needDepth && !s_supportsDepth)
		{
			NotSupported();
			return false;
		}
		if (needDepth)
		{
			GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
		}
		return true;
	}

	protected bool CheckSupport(bool needDepth, bool needHdr)
	{
		if (!CheckSupport(needDepth))
		{
			return false;
		}
		if (needHdr && !supportHDRTextures)
		{
			NotSupported();
			return false;
		}
		return true;
	}

	public bool Dx11Support()
	{
		return supportDX11;
	}

	protected void ReportAutoDisable()
	{
		Debug.LogWarning("The image effect " + ToString() + " has been disabled as it's not supported on the current platform.");
	}

	private bool CheckShader(Shader s)
	{
		Debug.Log("The shader " + s.ToString() + " on effect " + ToString() + " is not part of the Unity 3.2+ effects suite anymore. For best performance and quality, please ensure you are using the latest Standard Assets Image Effects (Pro only) package.");
		if (!s.isSupported)
		{
			NotSupported();
			return false;
		}
		return false;
	}

	protected void NotSupported()
	{
		base.enabled = false;
		isSupported = false;
	}

	protected void DrawBorder(RenderTexture dest, Material material)
	{
		RenderTexture.active = dest;
		bool flag = true;
		GL.PushMatrix();
		GL.LoadOrtho();
		for (int i = 0; i < material.passCount; i++)
		{
			material.SetPass(i);
			float y;
			float y2;
			if (flag)
			{
				y = 1f;
				y2 = 0f;
			}
			else
			{
				y = 0f;
				y2 = 1f;
			}
			float x = 0f + 1f / ((float)dest.width * 1f);
			float y3 = 0f;
			float y4 = 1f;
			GL.Begin(7);
			GL.TexCoord2(0f, y);
			GL.Vertex3(0f, y3, 0.1f);
			GL.TexCoord2(1f, y);
			GL.Vertex3(x, y3, 0.1f);
			GL.TexCoord2(1f, y2);
			GL.Vertex3(x, y4, 0.1f);
			GL.TexCoord2(0f, y2);
			GL.Vertex3(0f, y4, 0.1f);
			float x2 = 1f - 1f / ((float)dest.width * 1f);
			x = 1f;
			y3 = 0f;
			y4 = 1f;
			GL.TexCoord2(0f, y);
			GL.Vertex3(x2, y3, 0.1f);
			GL.TexCoord2(1f, y);
			GL.Vertex3(x, y3, 0.1f);
			GL.TexCoord2(1f, y2);
			GL.Vertex3(x, y4, 0.1f);
			GL.TexCoord2(0f, y2);
			GL.Vertex3(x2, y4, 0.1f);
			x = 1f;
			y3 = 0f;
			y4 = 0f + 1f / ((float)dest.height * 1f);
			GL.TexCoord2(0f, y);
			GL.Vertex3(0f, y3, 0.1f);
			GL.TexCoord2(1f, y);
			GL.Vertex3(x, y3, 0.1f);
			GL.TexCoord2(1f, y2);
			GL.Vertex3(x, y4, 0.1f);
			GL.TexCoord2(0f, y2);
			GL.Vertex3(0f, y4, 0.1f);
			x = 1f;
			y3 = 1f - 1f / ((float)dest.height * 1f);
			y4 = 1f;
			GL.TexCoord2(0f, y);
			GL.Vertex3(0f, y3, 0.1f);
			GL.TexCoord2(1f, y);
			GL.Vertex3(x, y3, 0.1f);
			GL.TexCoord2(1f, y2);
			GL.Vertex3(x, y4, 0.1f);
			GL.TexCoord2(0f, y2);
			GL.Vertex3(0f, y4, 0.1f);
			GL.End();
		}
		GL.PopMatrix();
	}
}
