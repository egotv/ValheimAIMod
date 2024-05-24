using UnityEngine;

public class Talker : MonoBehaviour
{
	public enum Type
	{
		Whisper,
		Normal,
		Shout,
		Ping
	}

	public float m_visperDistance = 4f;

	public float m_normalDistance = 15f;

	public float m_shoutDistance = 70f;

	private ZNetView m_nview;

	private Character m_character;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_character = GetComponent<Character>();
		m_nview.Register<int, UserInfo, string, string>("Say", RPC_Say);
	}

	public void Say(Type type, string text)
	{
		ZLog.Log("Saying " + type.ToString() + "  " + text);
		m_nview.InvokeRPC(ZNetView.Everybody, "Say", (int)type, UserInfo.GetLocalUser(), text, PrivilegeManager.GetNetworkUserId());
	}

	private void RPC_Say(long sender, int ctype, UserInfo user, string text, string senderNetworkUserId)
	{
		if (!(Player.m_localPlayer == null))
		{
			float num = 0f;
			switch (ctype)
			{
			case 0:
				num = m_visperDistance;
				break;
			case 1:
				num = m_normalDistance;
				break;
			case 2:
				num = m_shoutDistance;
				break;
			}
			if (Vector3.Distance(base.transform.position, Player.m_localPlayer.transform.position) < num && (bool)Chat.instance)
			{
				Vector3 headPoint = m_character.GetHeadPoint();
				Chat.instance.OnNewChatMessage(base.gameObject, sender, headPoint, (Type)ctype, user, text, senderNetworkUserId);
			}
		}
	}
}
