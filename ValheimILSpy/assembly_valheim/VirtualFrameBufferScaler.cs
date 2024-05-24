using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class VirtualFrameBufferScaler : MonoBehaviour
{
	private RawImage m_rawImage;

	private VirtualFrameBuffer m_virtualFrameBuffer;

	private void Start()
	{
		m_rawImage = GetComponent<RawImage>();
		m_rawImage.raycastTarget = false;
		m_rawImage.maskable = false;
		m_rawImage.color = new Color(1f, 1f, 1f, 0f);
		m_virtualFrameBuffer = Object.FindObjectOfType<VirtualFrameBuffer>();
		if (m_virtualFrameBuffer == null)
		{
			ZLog.LogError("Failed to find VirtualFrameBuffer");
		}
		else
		{
			m_virtualFrameBuffer.Subscribe(this);
		}
	}

	private void OnDestroy()
	{
		if (m_virtualFrameBuffer != null)
		{
			m_virtualFrameBuffer.Unsubscribe(this);
		}
	}

	public void OnBufferCreated(VirtualFrameBuffer virtualFrameBuffer, RenderTexture texture)
	{
		m_rawImage.texture = texture;
		m_rawImage.color = new Color(1f, 1f, 1f, 1f);
	}

	public void OnBufferDestroyed(VirtualFrameBuffer virtualFrameBuffer)
	{
		m_rawImage.texture = null;
		m_rawImage.color = new Color(1f, 1f, 1f, 0f);
	}
}
