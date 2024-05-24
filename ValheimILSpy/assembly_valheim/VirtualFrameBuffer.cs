using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(Camera))]
public class VirtualFrameBuffer : MonoBehaviour
{
	public static bool m_autoRenderScale = false;

	public static float m_global3DRenderScale = 1f;

	private Camera m_camera;

	private Camera m_clearCamera;

	private RenderTexture m_renderTexture;

	private bool m_isUsingScaledRendering;

	private List<VirtualFrameBufferScaler> m_subscribers = new List<VirtualFrameBufferScaler>();

	public void Subscribe(VirtualFrameBufferScaler subscriber)
	{
		m_subscribers.Add(subscriber);
		if (m_renderTexture != null)
		{
			subscriber.OnBufferCreated(this, m_renderTexture);
		}
		UpdateCameraTarget();
	}

	public void Unsubscribe(VirtualFrameBufferScaler subscriber)
	{
		m_subscribers.Remove(subscriber);
		if (m_renderTexture != null)
		{
			subscriber.OnBufferDestroyed(this);
		}
		UpdateCameraTarget();
	}

	private void Update()
	{
		UpdateCameraTarget();
	}

	private void CreateClearCamera()
	{
		GameObject gameObject = new GameObject();
		gameObject.transform.parent = base.transform;
		m_clearCamera = gameObject.AddComponent<Camera>();
		m_clearCamera.cullingMask = 0;
		m_clearCamera.allowHDR = false;
		m_clearCamera.allowMSAA = false;
		m_clearCamera.renderingPath = RenderingPath.Forward;
		m_clearCamera.clearFlags = CameraClearFlags.Color;
		m_clearCamera.backgroundColor = Color.black;
	}

	private void DestroyClearCameraIfExists()
	{
		if (!(m_clearCamera == null))
		{
			Object.Destroy(m_clearCamera.gameObject);
		}
	}

	private void UpdateCurrentRenderScale()
	{
		if (m_autoRenderScale)
		{
			m_global3DRenderScale = Mathf.Clamp01(96f / Screen.dpi);
		}
	}

	private static Resolution GetHighestSupportedResolution()
	{
		Resolution[] resolutions = Screen.resolutions;
		int num = 0;
		int num2 = resolutions[num].width * resolutions[num].height;
		for (int i = 1; i < resolutions.Length; i++)
		{
			int num3 = resolutions[i].width * resolutions[i].height;
			if (num3 > num2)
			{
				num = i;
				num2 = num3;
			}
		}
		return resolutions[num];
	}

	private void UpdateCameraTarget()
	{
		if (m_camera == null)
		{
			m_camera = GetComponent<Camera>();
		}
		UpdateCurrentRenderScale();
		bool flag = m_global3DRenderScale < 1f && m_subscribers.Count > 0;
		if (flag)
		{
			if (!m_isUsingScaledRendering)
			{
				CreateClearCamera();
			}
			ReassignTextureIfNeeded();
		}
		else if (m_isUsingScaledRendering)
		{
			ReleaseTextureIfExists();
			DestroyClearCameraIfExists();
		}
		m_isUsingScaledRendering = flag;
	}

	private void ReassignTextureIfNeeded()
	{
		Vector2Int vector2Int = new Vector2Int(Mathf.RoundToInt((float)Screen.width * m_global3DRenderScale), Mathf.RoundToInt((float)Screen.height * m_global3DRenderScale));
		if (vector2Int.x < 8 || vector2Int.y < 8)
		{
			vector2Int = ((vector2Int.y >= vector2Int.x) ? new Vector2Int(8, Mathf.RoundToInt(8f * ((float)Screen.height / (float)Screen.width))) : new Vector2Int(Mathf.RoundToInt(8f * ((float)Screen.width / (float)Screen.height)), 8));
		}
		if (m_renderTexture == null || vector2Int != new Vector2Int(m_renderTexture.width, m_renderTexture.height))
		{
			RecreateAndAssignRenderTexture(vector2Int);
		}
	}

	private void RecreateAndAssignRenderTexture(Vector2Int viewportResolution)
	{
		ReleaseTextureIfExists();
		m_renderTexture = new RenderTexture(viewportResolution.x, viewportResolution.y, 24, DefaultFormat.HDR);
		m_renderTexture.Create();
		m_camera.targetTexture = m_renderTexture;
		for (int i = 0; i < m_subscribers.Count; i++)
		{
			m_subscribers[i].OnBufferCreated(this, m_renderTexture);
		}
	}

	private void ReleaseTextureIfExists()
	{
		if (!(m_renderTexture == null))
		{
			for (int i = 0; i < m_subscribers.Count; i++)
			{
				m_subscribers[i].OnBufferDestroyed(this);
			}
			m_camera.targetTexture = null;
			m_renderTexture.Release();
			m_renderTexture = null;
		}
	}

	private void OnDestroy()
	{
		ReleaseTextureIfExists();
		DestroyClearCameraIfExists();
	}
}
