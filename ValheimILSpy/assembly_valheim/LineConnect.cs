using System.Collections.Generic;
using UnityEngine;

public class LineConnect : MonoBehaviour
{
	public bool m_centerOfCharacter;

	public string m_childObject = "";

	public bool m_hideIfNoConnection = true;

	public Vector3 m_noConnectionWorldOffset = new Vector3(0f, -1f, 0f);

	[Header("Dynamic slack")]
	public bool m_dynamicSlack;

	public float m_slack = 0.5f;

	[Header("Thickness")]
	public bool m_dynamicThickness = true;

	public float m_minDistance = 6f;

	public float m_maxDistance = 30f;

	public float m_minThickness = 0.2f;

	public float m_maxThickness = 0.8f;

	public float m_thicknessPower = 0.2f;

	public string m_netViewPrefix = "";

	private LineRenderer m_lineRenderer;

	private ZNetView m_nview;

	private KeyValuePair<int, int> m_linePeerID;

	private int m_slackHash;

	private void Awake()
	{
		m_lineRenderer = GetComponent<LineRenderer>();
		m_nview = GetComponentInParent<ZNetView>();
		m_linePeerID = ZDO.GetHashZDOID(m_netViewPrefix + "line_peer");
		m_slackHash = (m_netViewPrefix + "line_slack").GetStableHashCode();
	}

	private void LateUpdate()
	{
		if (!m_nview.IsValid())
		{
			m_lineRenderer.enabled = false;
			return;
		}
		ZDOID zDOID = m_nview.GetZDO().GetZDOID(m_linePeerID);
		GameObject gameObject = ZNetScene.instance.FindInstance(zDOID);
		if ((bool)gameObject && !string.IsNullOrEmpty(m_childObject))
		{
			Transform transform = Utils.FindChild(gameObject.transform, m_childObject);
			if ((bool)transform)
			{
				gameObject = transform.gameObject;
			}
		}
		if (gameObject != null)
		{
			Vector3 endpoint = gameObject.transform.position;
			if (m_centerOfCharacter)
			{
				Character component = gameObject.GetComponent<Character>();
				if ((bool)component)
				{
					endpoint = component.GetCenterPoint();
				}
			}
			SetEndpoint(endpoint);
			m_lineRenderer.enabled = true;
		}
		else if (m_hideIfNoConnection)
		{
			m_lineRenderer.enabled = false;
		}
		else
		{
			m_lineRenderer.enabled = true;
			SetEndpoint(base.transform.position + m_noConnectionWorldOffset);
		}
	}

	private void SetEndpoint(Vector3 pos)
	{
		Vector3 vector = base.transform.InverseTransformPoint(pos);
		Vector3 vector2 = base.transform.InverseTransformDirection(Vector3.down);
		if (m_dynamicSlack)
		{
			float @float = m_nview.GetZDO().GetFloat(m_slackHash, m_slack);
			Vector3 position = m_lineRenderer.GetPosition(0);
			Vector3 b = vector;
			float num = Vector3.Distance(position, b) / 2f;
			for (int i = 1; i < m_lineRenderer.positionCount; i++)
			{
				float num2 = (float)i / (float)(m_lineRenderer.positionCount - 1);
				float num3 = Mathf.Abs(0.5f - num2) * 2f;
				num3 *= num3;
				num3 = 1f - num3;
				Vector3 position2 = Vector3.Lerp(position, b, num2);
				position2 += vector2 * num * @float * num3;
				m_lineRenderer.SetPosition(i, position2);
			}
		}
		else
		{
			m_lineRenderer.SetPosition(1, vector);
		}
		if (m_dynamicThickness)
		{
			float v = Vector3.Distance(base.transform.position, pos);
			float f = Utils.LerpStep(m_minDistance, m_maxDistance, v);
			f = Mathf.Pow(f, m_thicknessPower);
			m_lineRenderer.widthMultiplier = Mathf.Lerp(m_maxThickness, m_minThickness, f);
		}
	}

	public void SetPeer(ZNetView other)
	{
		if ((bool)other)
		{
			SetPeer(other.GetZDO().m_uid);
		}
		else
		{
			SetPeer(ZDOID.None);
		}
	}

	public void SetPeer(ZDOID zdoid)
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(m_linePeerID, zdoid);
		}
	}

	public void SetSlack(float slack)
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(m_slackHash, slack);
		}
	}
}
