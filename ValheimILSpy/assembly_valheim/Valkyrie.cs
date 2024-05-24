using System;
using UnityEngine;

public class Valkyrie : MonoBehaviour
{
	public static Valkyrie m_instance;

	public float m_startPause = 10f;

	public float m_speed = 10f;

	public float m_turnRate = 5f;

	public float m_dropHeight = 10f;

	public float m_startAltitude = 500f;

	public float m_descentAltitude = 100f;

	public float m_startDistance = 500f;

	public float m_startDescentDistance = 200f;

	public Vector3 m_attachOffset = new Vector3(0f, 0f, 1f);

	public float m_textDuration = 5f;

	public string m_introTopic = "";

	[TextArea]
	public string m_introText = "";

	public Transform m_attachPoint;

	private Vector3 m_targetPoint;

	private Vector3 m_descentStart;

	private Vector3 m_flyAwayPoint;

	private bool m_descent;

	private bool m_droppedPlayer;

	private Animator m_animator;

	private ZNetView m_nview;

	private float m_timer;

	private void Awake()
	{
		m_instance = this;
		m_nview = GetComponent<ZNetView>();
		m_animator = GetComponentInChildren<Animator>();
		if (!m_nview.IsOwner())
		{
			base.enabled = false;
			return;
		}
		ZLog.Log("Setting up valkyrie ");
		float f = UnityEngine.Random.value * (float)Math.PI * 2f;
		Vector3 vector = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f));
		Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
		Player.m_localPlayer.SetIntro(intro: true);
		m_targetPoint = Player.m_localPlayer.transform.position + new Vector3(0f, m_dropHeight, 0f);
		Vector3 position = m_targetPoint + vector * m_startDistance;
		position.y = m_startAltitude;
		base.transform.position = position;
		m_descentStart = m_targetPoint + vector * m_startDescentDistance + vector2 * 200f;
		m_descentStart.y = m_descentAltitude;
		Vector3 vector3 = m_targetPoint - m_descentStart;
		vector3.y = 0f;
		vector3.Normalize();
		m_flyAwayPoint = m_targetPoint + vector3 * m_startDescentDistance;
		m_flyAwayPoint.y = m_startAltitude;
		ShowText();
		SyncPlayer(doNetworkSync: true);
		ZLog.Log("World pos " + base.transform.position.ToString() + "   " + ZNet.instance.GetReferencePosition().ToString());
	}

	private void ShowText()
	{
		TextViewer.instance.ShowText(TextViewer.Style.Intro, m_introTopic, m_introText, autoHide: false);
	}

	private void HideText()
	{
	}

	private void OnDestroy()
	{
		ZLog.Log("Destroying valkyrie");
	}

	private void FixedUpdate()
	{
		UpdateValkyrie(Time.fixedDeltaTime);
		if (!m_droppedPlayer)
		{
			SyncPlayer(doNetworkSync: true);
		}
	}

	private void LateUpdate()
	{
		if (!m_droppedPlayer)
		{
			SyncPlayer(doNetworkSync: false);
		}
	}

	private void UpdateValkyrie(float dt)
	{
		m_timer += dt;
		if (m_timer < m_startPause)
		{
			return;
		}
		Vector3 vector = (m_droppedPlayer ? m_flyAwayPoint : ((!m_descent) ? m_descentStart : m_targetPoint));
		if (Utils.DistanceXZ(vector, base.transform.position) < 0.5f)
		{
			if (!m_descent)
			{
				m_descent = true;
				ZLog.Log("Starting descent");
			}
			else if (!m_droppedPlayer)
			{
				ZLog.Log("We are here");
				DropPlayer();
			}
			else
			{
				m_nview.Destroy();
			}
		}
		Vector3 normalized = (vector - base.transform.position).normalized;
		Vector3 vector2 = base.transform.position + normalized * 25f;
		if (ZoneSystem.instance.GetGroundHeight(vector2, out var height))
		{
			vector2.y = Mathf.Max(vector2.y, height + m_dropHeight);
		}
		Vector3 normalized2 = (vector2 - base.transform.position).normalized;
		Quaternion quaternion = Quaternion.LookRotation(normalized2);
		Vector3 to = normalized2;
		to.y = 0f;
		to.Normalize();
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		float num = Mathf.Clamp(Vector3.SignedAngle(forward, to, Vector3.up), -30f, 30f) / 30f;
		quaternion = Quaternion.Euler(0f, 0f, num * 45f) * quaternion;
		float num2 = (m_droppedPlayer ? (m_turnRate * 4f) : m_turnRate);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, num2 * dt);
		Vector3 vector3 = base.transform.forward * m_speed;
		Vector3 vector4 = base.transform.position + vector3 * dt;
		if (ZoneSystem.instance.GetGroundHeight(vector4, out var height2))
		{
			vector4.y = Mathf.Max(vector4.y, height2 + m_dropHeight);
		}
		base.transform.position = vector4;
	}

	public void DropPlayer(bool destroy = false)
	{
		ZLog.Log("We are here");
		m_droppedPlayer = true;
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Player.m_localPlayer.transform.rotation = Quaternion.LookRotation(forward);
		Player.m_localPlayer.SetIntro(intro: false);
		m_animator.SetBool("dropped", value: true);
		if (destroy)
		{
			m_nview.Destroy();
		}
	}

	private void SyncPlayer(bool doNetworkSync)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			ZLog.LogWarning("No local player");
			return;
		}
		localPlayer.transform.rotation = m_attachPoint.rotation;
		localPlayer.transform.position = m_attachPoint.position - localPlayer.transform.TransformVector(m_attachOffset);
		localPlayer.GetComponent<Rigidbody>().position = localPlayer.transform.position;
		if (doNetworkSync)
		{
			ZNet.instance.SetReferencePosition(localPlayer.transform.position);
			localPlayer.GetComponent<ZSyncTransform>().SyncNow();
			GetComponent<ZSyncTransform>().SyncNow();
		}
	}
}
