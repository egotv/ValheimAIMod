using System.Collections.Generic;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectPanel : MonoBehaviour
{
	private static ConnectPanel m_instance;

	public Transform m_root;

	public TMP_Text m_serverField;

	public TMP_Text m_worldField;

	public TMP_Text m_statusField;

	public TMP_Text m_connections;

	public RectTransform m_playerList;

	public Scrollbar m_playerListScroll;

	public GameObject m_playerElement;

	public GuiInputField m_hostName;

	public GuiInputField m_hostPort;

	public Button m_connectButton;

	public TMP_Text m_myPort;

	public TMP_Text m_myUID;

	public TMP_Text m_knownHosts;

	public TMP_Text m_nrOfConnections;

	public TMP_Text m_pendingConnections;

	public Toggle m_autoConnect;

	public TMP_Text m_zdos;

	public TMP_Text m_zdosPool;

	public TMP_Text m_zdosSent;

	public TMP_Text m_zdosRecv;

	public TMP_Text m_zdosInstances;

	public TMP_Text m_activePeers;

	public TMP_Text m_ntp;

	public TMP_Text m_upnp;

	public TMP_Text m_dataSent;

	public TMP_Text m_dataRecv;

	public TMP_Text m_clientSendQueue;

	public TMP_Text m_fps;

	public TMP_Text m_frameTime;

	public TMP_Text m_ping;

	public TMP_Text m_quality;

	private float m_playerListBaseSize;

	private List<GameObject> m_playerListElements = new List<GameObject>();

	public TMP_Text m_serverOptions;

	private int m_frameSamples;

	private float m_frameTimer;

	public static ConnectPanel instance => m_instance;

	private void Start()
	{
		m_instance = this;
		m_root.gameObject.SetActive(value: false);
		m_playerListBaseSize = m_playerList.rect.height;
	}

	public static bool IsVisible()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_root.gameObject.activeSelf;
		}
		return false;
	}

	private void Update()
	{
		if (ZInput.GetKeyDown(KeyCode.F2) || (ZInput.GetButton("JoyLTrigger") && ZInput.GetButton("JoyLBumper") && ZInput.GetButtonDown("JoyBack")))
		{
			m_root.gameObject.SetActive(!m_root.gameObject.activeSelf);
		}
		if (!m_root.gameObject.activeInHierarchy)
		{
			return;
		}
		if (!ZNet.instance.IsServer() && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
		{
			m_serverField.gameObject.SetActive(value: true);
			m_serverField.text = ZNet.GetServerString();
		}
		else
		{
			m_serverField.gameObject.SetActive(value: false);
		}
		m_worldField.text = ZNet.instance.GetWorldName();
		UpdateFps();
		m_serverOptions.text = Localization.instance.Localize(Game.m_serverOptionsSummary);
		m_myPort.gameObject.SetActive(ZNet.instance.IsServer());
		m_myPort.text = ZNet.instance.GetHostPort().ToString();
		m_myUID.text = ZNet.GetUID().ToString();
		if (ZDOMan.instance != null)
		{
			m_zdos.text = ZDOMan.instance.NrOfObjects().ToString();
			ZDOMan.instance.GetAverageStats(out var sentZdos, out var recvZdos);
			m_zdosSent.text = sentZdos.ToString("0.0");
			m_zdosRecv.text = recvZdos.ToString("0.0");
			m_activePeers.text = ZNet.instance.GetNrOfPlayers().ToString();
		}
		m_zdosPool.text = ZDOPool.GetPoolActive() + " / " + ZDOPool.GetPoolSize() + " / " + ZDOPool.GetPoolTotal();
		if ((bool)ZNetScene.instance)
		{
			m_zdosInstances.text = ZNetScene.instance.NrOfInstances().ToString();
		}
		ZNet.instance.GetNetStats(out var localQuality, out var remoteQuality, out var ping, out var outByteSec, out var inByteSec);
		m_dataSent.text = (outByteSec / 1024f).ToString("0.0") + "kb/s";
		m_dataRecv.text = (inByteSec / 1024f).ToString("0.0") + "kb/s";
		m_ping.text = ping.ToString("0") + "ms";
		m_quality.text = (int)(localQuality * 100f) + "% / " + (int)(remoteQuality * 100f) + "%";
		m_clientSendQueue.text = ZDOMan.instance.GetClientChangeQueue().ToString();
		m_nrOfConnections.text = ZNet.instance.GetPeerConnections().ToString();
		string text = "";
		foreach (ZNetPeer connectedPeer in ZNet.instance.GetConnectedPeers())
		{
			text = ((!connectedPeer.IsReady()) ? (text + connectedPeer.m_socket.GetEndPointString() + " connecting \n") : (text + connectedPeer.m_socket.GetEndPointString() + " UID: " + connectedPeer.m_uid + "\n"));
		}
		m_connections.text = text;
		List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
		float num = 16f;
		if (playerList.Count != m_playerListElements.Count)
		{
			foreach (GameObject playerListElement in m_playerListElements)
			{
				Object.Destroy(playerListElement);
			}
			m_playerListElements.Clear();
			for (int i = 0; i < playerList.Count; i++)
			{
				GameObject gameObject = Object.Instantiate(m_playerElement, m_playerList);
				(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * (0f - num));
				m_playerListElements.Add(gameObject);
			}
			float b = (float)playerList.Count * num;
			b = Mathf.Max(m_playerListBaseSize, b);
			m_playerList.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b);
			m_playerListScroll.value = 1f;
		}
		for (int j = 0; j < playerList.Count; j++)
		{
			ZNet.PlayerInfo playerInfo = playerList[j];
			TMP_Text component = m_playerListElements[j].transform.Find("name").GetComponent<TMP_Text>();
			TMP_Text component2 = m_playerListElements[j].transform.Find("hostname").GetComponent<TMP_Text>();
			Button component3 = m_playerListElements[j].transform.Find("KickButton").GetComponent<Button>();
			component.text = CensorShittyWords.FilterUGC(playerInfo.m_name, UGCType.CharacterName, playerInfo.m_host, 0L);
			component2.text = playerInfo.m_host;
			component3.gameObject.SetActive(value: false);
		}
		m_connectButton.interactable = ValidHost();
	}

	private void UpdateFps()
	{
		m_frameTimer += Time.deltaTime;
		m_frameSamples++;
		if (m_frameTimer > 1f)
		{
			float num = m_frameTimer / (float)m_frameSamples;
			m_fps.text = (1f / num).ToString("0.0");
			m_frameTime.text = "( " + (num * 1000f).ToString("00.0") + "ms )";
			m_frameSamples = 0;
			m_frameTimer = 0f;
		}
	}

	private bool ValidHost()
	{
		int num = 0;
		try
		{
			num = int.Parse(m_hostPort.text);
		}
		catch
		{
			return false;
		}
		if (string.IsNullOrEmpty(m_hostName.text) || num == 0)
		{
			return false;
		}
		return true;
	}
}
