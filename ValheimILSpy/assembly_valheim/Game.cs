using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using SoftReferenceableAssets.SceneManagement;
using TMPro;
using UnityEngine;
using Valheim.SettingsGui;

public class Game : MonoBehaviour
{
	private struct ConnectingPortals
	{
		public ZDO PortalA;

		public ZDO PortalB;
	}

	[Serializable]
	public class ItemConversion
	{
		public HitData.DamageType m_damageType;

		public HitData.HitType m_hitType;

		public List<ItemDrop> m_items;

		public ItemDrop m_result;

		public int m_multiplier = 1;
	}

	public static readonly string messageForModders = "While we don't officially support mods in Valheim at this time. We ask that you please set the following isModded value to true in your mod. This will place a small text in the menu to inform the player that their game is modded and help us solving support issues. Thank you for your help!";

	public static bool isModded = false;

	public const int m_backgroundFPS = 30;

	public const int m_menuFPS = 60;

	public const int m_minimumFPSLimit = 30;

	public const int m_maximumFPSLimit = 360;

	public GameObject m_playerPrefab;

	public List<GameObject> m_portalPrefabs;

	public GameObject m_consolePrefab;

	public GameObject m_serverOptionPrefab;

	public SceneReference m_startScene;

	[Header("Player Startup")]
	public string m_devWorldName = "DevWorld";

	public string m_devWorldSeed = "";

	public string m_devProfileName = "Developer";

	public string m_devPlayerName = "Odev";

	public string m_StartLocation = "StartTemple";

	private static DateTime m_pauseStart;

	private static DateTime m_pauseEnd;

	private static float m_pauseFrom;

	private static float m_pauseTarget;

	private static float m_timeScale = 1f;

	private static float m_pauseRotateFade;

	private static float m_pauseTimer;

	private static float m_collectTimer;

	private static bool m_pause;

	private static string m_profileFilename = null;

	private static FileHelpers.FileSource m_profileFileSource = FileHelpers.FileSource.Local;

	private PlayerProfile m_playerProfile;

	private bool m_requestRespawn;

	private bool m_respawnAfterDeath;

	private float m_respawnWait;

	public float m_respawnLoadDuration = 8f;

	public float m_fadeTimeDeath = 9.5f;

	public float m_fadeTimeSleep = 3f;

	private bool m_haveSpawned;

	private bool m_firstSpawn = true;

	private bool m_shuttingDown;

	private Vector3 m_randomStartPoint = Vector3.zero;

	private UnityEngine.Random.State m_spawnRandomState;

	private List<ZoneSystem.LocationInstance> m_tempLocations = new List<ZoneSystem.LocationInstance>();

	private double m_lastSleepTime;

	private bool m_sleeping;

	private List<ConnectingPortals> m_currentlyConnectingPortals = new List<ConnectingPortals>();

	private const float m_collectResourcesInterval = 1200f;

	private const float m_collectResourcesIntervalPeriodic = 3600f;

	private DateTime m_lastCollectResources = DateTime.Now;

	[NonSerialized]
	public float m_saveTimer;

	public static float m_saveInterval = 1800f;

	private const float m_preSaveWarning = 30f;

	[Header("Diffuculty scaling")]
	public float m_difficultyScaleRange = 100f;

	public int m_difficultyScaleMaxPlayers = 5;

	public float m_damageScalePerPlayer = 0.04f;

	public float m_healthScalePerPlayer = 0.3f;

	private int m_forcePlayers;

	[Header("Misc")]
	public float m_ashDamage = 5f;

	public List<ItemConversion> m_damageTypeDropConversions = new List<ItemConversion>();

	[Header("World Level Rates")]
	public List<ItemDrop.ItemData.ItemType> m_nonScaledDropTypes = new List<ItemDrop.ItemData.ItemType>();

	public int m_worldLevelEnemyBaseAC = 100;

	public float m_worldLevelEnemyHPMultiplier = 2f;

	public int m_worldLevelEnemyBaseDamage = 85;

	public int m_worldLevelGearBaseAC = 38;

	public int m_worldLevelGearBaseDamage = 120;

	public float m_worldLevelEnemyLevelUpExponent = 1.15f;

	public float m_worldLevelEnemyMoveSpeedMultiplier = 0.2f;

	public int m_worldLevelPieceBaseDamage = 100;

	public float m_worldLevelPieceHPMultiplier = 1f;

	public float m_worldLevelMineHPMultiplier = 2f;

	public static float m_playerDamageRate = 1f;

	public static float m_enemyDamageRate = 1f;

	public static float m_enemyLevelUpRate = 1f;

	public static float m_localDamgeTakenRate = 1f;

	public static float m_resourceRate = 1f;

	public static float m_eventRate = 1f;

	public static float m_staminaRate = 1f;

	public static float m_moveStaminaRate = 1f;

	public static float m_staminaRegenRate = 1f;

	public static float m_skillGainRate = 1f;

	public static float m_skillReductionRate = 1f;

	public static float m_enemySpeedSize = 1f;

	public static int m_worldLevel = 0;

	public static string m_serverOptionsSummary = "";

	public static bool m_noMap = false;

	public const string m_keyDefaultString = "default";

	public static Game instance { get; private set; }

	public List<int> PortalPrefabHash { get; private set; } = new List<int>();


	private void Awake()
	{
		if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
		{
			Thread.CurrentThread.Name = "MainValheimThread";
		}
		instance = this;
		foreach (GameObject portalPrefab in m_portalPrefabs)
		{
			PortalPrefabHash.Add(portalPrefab.name.GetStableHashCode());
		}
		if (FejdStartup.AwakePlatforms())
		{
			FileHelpers.UpdateCloudEnabledStatus();
			PrivilegeManager.FlushCache();
			Settings.SetPlatformDefaultPrefs();
			GameplaySettings.SetControllerSpecificFirstTimeSettings();
			ZInput.Initialize();
			if (!Console.instance)
			{
				UnityEngine.Object.Instantiate(m_consolePrefab);
			}
			if (!ServerOptionsGUI.m_instance)
			{
				UnityEngine.Object.Instantiate(m_serverOptionPrefab);
			}
			Settings.ApplyStartupSettings();
			if (string.IsNullOrEmpty(m_profileFilename))
			{
				m_playerProfile = new PlayerProfile(m_devProfileName, FileHelpers.FileSource.Local);
				m_playerProfile.SetName(m_devPlayerName);
				m_playerProfile.Load();
			}
			else
			{
				ZLog.Log("Loading player profile " + m_profileFilename);
				m_playerProfile = new PlayerProfile(m_profileFilename, m_profileFileSource);
				m_playerProfile.Load();
			}
			InvokeRepeating("CollectResourcesCheckPeriodic", 3600f, 3600f);
			Gogan.LogEvent("Screen", "Enter", "InGame", 0L);
			Gogan.LogEvent("Game", "InputMode", ZInput.IsGamepadActive() ? "Gamepad" : "MK", 0L);
			ZLog.Log("isModded: " + isModded);
		}
	}

	private void OnDestroy()
	{
		instance = null;
	}

	private void Start()
	{
		Application.targetFrameRate = ((Settings.FPSLimit == 29 || Settings.FPSLimit > 360) ? (-1) : Settings.FPSLimit);
		ZRoutedRpc.instance.Register("SleepStart", SleepStart);
		ZRoutedRpc.instance.Register("SleepStop", SleepStop);
		ZRoutedRpc.instance.Register<float>("Ping", RPC_Ping);
		ZRoutedRpc.instance.Register<float>("Pong", RPC_Pong);
		ZRoutedRpc.instance.Register<ZDOID, ZDOID>("RPC_SetConnection", RPC_SetConnection);
		ZRoutedRpc.instance.Register<string, int, Vector3, bool>("RPC_DiscoverLocationResponse", RPC_DiscoverLocationResponse);
		if (ZNet.instance.IsServer())
		{
			ZRoutedRpc.instance.Register<string, Vector3, string, int, bool, bool>("RPC_DiscoverClosestLocation", RPC_DiscoverClosestLocation);
			InvokeRepeating("UpdateSleeping", 2f, 2f);
			StartCoroutine("ConnectPortalsCoroutine");
		}
	}

	private void ServerLog()
	{
		int peerConnections = ZNet.instance.GetPeerConnections();
		int num = ZDOMan.instance.NrOfObjects();
		int sentZDOs = ZDOMan.instance.GetSentZDOs();
		int recvZDOs = ZDOMan.instance.GetRecvZDOs();
		ZLog.Log(" Connections " + peerConnections + " ZDOS:" + num + "  sent:" + sentZDOs + " recv:" + recvZDOs);
	}

	public void CollectResources(bool displayMessage = false)
	{
		if (displayMessage && (bool)Player.m_localPlayer)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Unloading unused assets");
		}
		ZLog.Log("Unloading unused assets");
		Resources.UnloadUnusedAssets();
		m_lastCollectResources = DateTime.Now;
	}

	public void CollectResourcesCheckPeriodic()
	{
		if (DateTime.Now - TimeSpan.FromSeconds(3599.0) > m_lastCollectResources)
		{
			CollectResources(displayMessage: true);
		}
		else
		{
			ZLog.Log("Skipping unloading unused assets");
		}
	}

	public void CollectResourcesCheck()
	{
		if (DateTime.Now - TimeSpan.FromSeconds(1200.0) > m_lastCollectResources)
		{
			CollectResources(displayMessage: true);
		}
		else
		{
			ZLog.Log("Skipping unloading unused assets");
		}
	}

	public void Logout(bool save = true, bool changeToStartScene = true)
	{
		if (!m_shuttingDown)
		{
			bool shouldExit = false;
			save = ZNet.instance.EnoughDiskSpaceAvailable(out var exitGamePopupShown, exitGamePrompt: true, delegate(bool exit)
			{
				shouldExit = exit;
				ContinueLogout(save, shouldExit, changeToStartScene);
			});
			if (!exitGamePopupShown)
			{
				ContinueLogout(save, shouldExit, changeToStartScene);
			}
		}
	}

	private void ContinueLogout(bool save, bool shouldExit, bool changeToStartScene)
	{
		if (save || shouldExit)
		{
			Shutdown(save);
			if (changeToStartScene)
			{
				SceneManager.LoadScene(m_startScene);
			}
		}
	}

	public bool IsShuttingDown()
	{
		return m_shuttingDown;
	}

	private void OnApplicationQuit()
	{
		if (!m_shuttingDown)
		{
			ZLog.Log("Game - OnApplicationQuit");
			bool exitGamePopupShown;
			bool saveWorld = ZNet.instance.EnoughDiskSpaceAvailable(out exitGamePopupShown);
			Shutdown(saveWorld);
			HeightmapBuilder.instance.Dispose();
			FileHelpers.TerminateCloudStorage();
			Thread.Sleep(2000);
		}
	}

	private void Shutdown(bool saveWorld = true)
	{
		if (!m_shuttingDown)
		{
			ZLog.Log("Shutting down");
			m_shuttingDown = true;
			if (saveWorld)
			{
				SavePlayerProfile(setLogoutPoint: true);
			}
			ZNetScene.instance.Shutdown();
			ZNet.instance.Shutdown(saveWorld);
		}
	}

	public void SavePlayerProfile(bool setLogoutPoint)
	{
		m_saveTimer = 0f;
		if ((bool)Player.m_localPlayer)
		{
			m_playerProfile.SavePlayerData(Player.m_localPlayer);
			Minimap.instance.SaveMapData();
			if (setLogoutPoint)
			{
				m_playerProfile.SaveLogoutPoint();
			}
		}
		if (m_playerProfile.m_fileSource == FileHelpers.FileSource.Cloud)
		{
			ulong num = 1048576uL;
			if (FileHelpers.FileExistsCloud(m_playerProfile.GetPath()))
			{
				num += FileHelpers.GetFileSize(m_playerProfile.GetPath(), FileHelpers.FileSource.Cloud);
			}
			num *= 3;
			if (FileHelpers.OperationExceedsCloudCapacity(num))
			{
				string path = m_playerProfile.GetPath();
				m_playerProfile.m_fileSource = FileHelpers.FileSource.Local;
				string path2 = m_playerProfile.GetPath();
				if (FileHelpers.FileExistsCloud(path))
				{
					FileHelpers.FileCopyOutFromCloud(path, path2, deleteOnCloud: true);
				}
				SaveSystem.InvalidateCache();
				ZLog.LogWarning("The character save operation may exceed the cloud save quota and it has therefore been moved to local storage!");
			}
		}
		m_playerProfile.Save();
	}

	private Player SpawnPlayer(Vector3 spawnPoint)
	{
		ZLog.DevLog("Spawning player:" + Time.frameCount);
		Player component = UnityEngine.Object.Instantiate(m_playerPrefab, spawnPoint, Quaternion.identity).GetComponent<Player>();
		component.SetLocalPlayer();
		m_playerProfile.LoadPlayerData(component);
		ZNet.instance.SetCharacterID(component.GetZDOID());
		component.OnSpawned();
		return component;
	}

	private Bed FindBedNearby(Vector3 point, float maxDistance)
	{
		Bed[] array = UnityEngine.Object.FindObjectsOfType<Bed>();
		foreach (Bed bed in array)
		{
			if (bed.IsCurrent())
			{
				return bed;
			}
		}
		return null;
	}

	private bool FindSpawnPoint(out Vector3 point, out bool usedLogoutPoint, float dt)
	{
		m_respawnWait += dt;
		usedLogoutPoint = false;
		if (!m_respawnAfterDeath && m_playerProfile.HaveLogoutPoint())
		{
			Vector3 logoutPoint = m_playerProfile.GetLogoutPoint();
			ZNet.instance.SetReferencePosition(logoutPoint);
			if (m_respawnWait > m_respawnLoadDuration && ZNetScene.instance.IsAreaReady(logoutPoint))
			{
				if (!ZoneSystem.instance.GetGroundHeight(logoutPoint, out var height))
				{
					Vector3 vector = logoutPoint;
					ZLog.Log("Invalid spawn point, no ground " + vector.ToString());
					m_respawnWait = 0f;
					m_playerProfile.ClearLoguoutPoint();
					point = Vector3.zero;
					return false;
				}
				m_playerProfile.ClearLoguoutPoint();
				point = logoutPoint;
				if (point.y < height)
				{
					point.y = height;
				}
				point.y += 0.25f;
				usedLogoutPoint = true;
				ZLog.Log("Spawned after " + m_respawnWait);
				return true;
			}
			point = Vector3.zero;
			return false;
		}
		if (m_playerProfile.HaveCustomSpawnPoint())
		{
			Vector3 customSpawnPoint = m_playerProfile.GetCustomSpawnPoint();
			ZNet.instance.SetReferencePosition(customSpawnPoint);
			if (m_respawnWait > m_respawnLoadDuration && ZNetScene.instance.IsAreaReady(customSpawnPoint))
			{
				Bed bed = FindBedNearby(customSpawnPoint, 5f);
				if (bed != null)
				{
					ZLog.Log("Found bed at custom spawn point");
					point = bed.GetSpawnPoint();
					return true;
				}
				ZLog.Log("Failed to find bed at custom spawn point, using original");
				m_playerProfile.ClearCustomSpawnPoint();
				m_respawnWait = 0f;
				point = Vector3.zero;
				return false;
			}
			point = Vector3.zero;
			return false;
		}
		if (ZoneSystem.instance.GetLocationIcon(m_StartLocation, out var pos))
		{
			point = pos + Vector3.up * 2f;
			ZNet.instance.SetReferencePosition(point);
			return ZNetScene.instance.IsAreaReady(point);
		}
		ZNet.instance.SetReferencePosition(Vector3.zero);
		point = Vector3.zero;
		return false;
	}

	public void RemoveCustomSpawnPoint(Vector3 point)
	{
		if (m_playerProfile.HaveCustomSpawnPoint())
		{
			Vector3 customSpawnPoint = m_playerProfile.GetCustomSpawnPoint();
			if (point == customSpawnPoint)
			{
				m_playerProfile.ClearCustomSpawnPoint();
			}
		}
	}

	private static Vector3 GetPointOnCircle(float distance, float angle)
	{
		return new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
	}

	public void RequestRespawn(float delay, bool afterDeath = false)
	{
		m_respawnAfterDeath = afterDeath;
		CancelInvoke("_RequestRespawn");
		Invoke("_RequestRespawn", delay);
	}

	private void _RequestRespawn()
	{
		ZLog.Log("Starting respawn");
		if ((bool)Player.m_localPlayer)
		{
			m_playerProfile.SavePlayerData(Player.m_localPlayer);
		}
		if ((bool)Player.m_localPlayer)
		{
			ZNetScene.instance.Destroy(Player.m_localPlayer.gameObject);
			ZNet.instance.SetCharacterID(ZDOID.None);
		}
		m_respawnWait = 0f;
		m_requestRespawn = true;
		MusicMan.instance.TriggerMusic("respawn");
	}

	private void Update()
	{
		if (!m_shuttingDown)
		{
			bool flag = Settings.FPSLimit != 29 && Settings.FPSLimit <= 360;
			if (Settings.ReduceBackgroundUsage && !Application.isFocused)
			{
				Application.targetFrameRate = (flag ? Mathf.Min(30, Settings.FPSLimit) : 30);
			}
			else if (IsPaused())
			{
				Application.targetFrameRate = (flag ? Mathf.Min(60, Settings.FPSLimit) : 60);
			}
			else
			{
				Application.targetFrameRate = (flag ? Settings.FPSLimit : (-1));
			}
			if (Terminal.m_showTests)
			{
				Terminal.m_testList["fps limit"] = Application.targetFrameRate.ToString();
			}
			UpdatePause();
			ZInput.Update(Time.unscaledDeltaTime);
			UpdateSaving(Time.unscaledDeltaTime);
			LightLod.UpdateLights(Time.deltaTime);
		}
	}

	private void OnGUI()
	{
		ZInput.OnGUI();
	}

	private void FixedUpdate()
	{
		if (ZNet.m_loadError)
		{
			Logout();
			ZLog.LogError("World load failed, exiting without save. Check backups!");
		}
		if (!m_haveSpawned && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
		{
			m_haveSpawned = true;
			RequestRespawn(0f);
		}
		ZInput.FixedUpdate(Time.fixedDeltaTime);
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connecting && ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			ZLog.Log("Lost connection to server:" + ZNet.GetConnectionStatus());
			Logout();
		}
		else
		{
			UpdateRespawn(Time.fixedDeltaTime);
		}
	}

	private void UpdateSaving(float dt)
	{
		if (m_saveInterval - m_saveTimer > 30f && m_saveInterval - (m_saveTimer + dt) <= 30f && (bool)MessageHud.instance && ZNet.instance.IsServer())
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, "$msg_worldsavewarning " + 30f + "s");
		}
		m_saveTimer += dt;
		if (!(m_saveTimer > m_saveInterval))
		{
			return;
		}
		if (!ZNet.instance.EnoughDiskSpaceAvailable(out var _))
		{
			m_saveTimer -= 300f;
			return;
		}
		SavePlayerProfile(setLogoutPoint: false);
		if ((bool)ZNet.instance)
		{
			ZNet.instance.Save(sync: false, saveOtherPlayerProfiles: true);
		}
	}

	private void UpdateRespawn(float dt)
	{
		if (!m_requestRespawn || !FindSpawnPoint(out var point, out var usedLogoutPoint, dt))
		{
			return;
		}
		TextViewer.instance.HideIntro();
		if (!usedLogoutPoint)
		{
			m_playerProfile.SetHomePoint(point);
		}
		SpawnPlayer(point);
		m_requestRespawn = false;
		if (m_firstSpawn)
		{
			m_firstSpawn = false;
			Chat.instance.SendText(Talker.Type.Shout, Localization.instance.Localize("$text_player_arrived"));
			UpdateNoMap();
			JoinCode.Show(firstSpawn: true);
			if (ZNet.m_loadError)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "World load error, saving disabled! Recover your .old file or backups!");
				Hud.instance.m_betaText.GetComponent<TMP_Text>().text = "";
				Hud.instance.m_betaText.transform.GetChild(0).GetComponent<TMP_Text>().text = "WORLD SAVE DISABLED! (World load error)";
				Hud.instance.m_betaText.SetActive(value: true);
			}
		}
		instance.CollectResourcesCheck();
	}

	public bool WaitingForRespawn()
	{
		return m_requestRespawn;
	}

	public PlayerProfile GetPlayerProfile()
	{
		return m_playerProfile;
	}

	public void IncrementPlayerStat(PlayerStatType stat, float amount = 1f)
	{
		m_playerProfile.IncrementStat(stat, amount);
	}

	public static void SetProfile(string filename, FileHelpers.FileSource fileSource)
	{
		m_profileFilename = filename;
		m_profileFileSource = fileSource;
	}

	private IEnumerator ConnectPortalsCoroutine()
	{
		while (true)
		{
			ConnectPortals();
			yield return new WaitForSeconds(5f);
		}
	}

	public void ConnectPortals()
	{
		ClearCurrentlyConnectingPortals();
		List<ZDO> portals = ZDOMan.instance.GetPortals();
		int num = 0;
		foreach (ZDO item in portals)
		{
			ZDOID connectionZDOID = item.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
			string @string = item.GetString(ZDOVars.s_tag);
			if (!connectionZDOID.IsNone())
			{
				ZDO zDO = ZDOMan.instance.GetZDO(connectionZDOID);
				if (zDO == null || zDO.GetString(ZDOVars.s_tag) != @string)
				{
					SetConnection(item, ZDOID.None);
				}
			}
		}
		foreach (ZDO item2 in portals)
		{
			if (!IsCurrentlyConnectingPortal(item2) && item2.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal).IsNone())
			{
				string string2 = item2.GetString(ZDOVars.s_tag);
				ZDO zDO2 = FindRandomUnconnectedPortal(portals, item2, string2);
				if (zDO2 != null)
				{
					AddToCurrentlyConnectingPortals(item2, zDO2);
					SetConnection(item2, zDO2.m_uid);
					SetConnection(zDO2, item2.m_uid);
					num++;
					ZLog.Log("Connected portals " + item2?.ToString() + " <-> " + zDO2);
				}
			}
		}
		if (num > 0)
		{
			ZLog.Log("[ Connected " + num + " portals ]");
		}
	}

	private void ForceSetConnection(ZDO portal, ZDOID connection)
	{
		if (portal.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) != connection)
		{
			SetConnection(portal, connection, forceImmediateConnection: true);
		}
	}

	private void SetConnection(ZDO portal, ZDOID connection, bool forceImmediateConnection = false)
	{
		long owner = portal.GetOwner();
		bool flag = ZNet.instance.GetPeer(owner) != null;
		if (owner == 0L || !flag || forceImmediateConnection)
		{
			portal.SetOwner(ZDOMan.GetSessionID());
			portal.SetConnection(ZDOExtraData.ConnectionType.Portal, connection);
			ZDOMan.instance.ForceSendZDO(portal.m_uid);
		}
		else
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(owner, "RPC_SetConnection", portal.m_uid, connection);
		}
	}

	private void RPC_SetConnection(long sender, ZDOID portalID, ZDOID connectionID)
	{
		ZDO zDO = ZDOMan.instance.GetZDO(portalID);
		if (zDO != null)
		{
			zDO.SetOwner(ZDOMan.GetSessionID());
			zDO.SetConnection(ZDOExtraData.ConnectionType.Portal, connectionID);
			ZDOMan.instance.ForceSendZDO(portalID);
		}
	}

	private ZDO FindRandomUnconnectedPortal(List<ZDO> portals, ZDO skip, string tag)
	{
		List<ZDO> list = new List<ZDO>();
		foreach (ZDO portal in portals)
		{
			if (portal != skip && !(portal.GetString(ZDOVars.s_tag) != tag) && !(portal.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) != ZDOID.None) && !IsCurrentlyConnectingPortal(portal))
			{
				list.Add(portal);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private void AddToCurrentlyConnectingPortals(ZDO portalA, ZDO portalB)
	{
		m_currentlyConnectingPortals.Add(new ConnectingPortals
		{
			PortalA = portalA,
			PortalB = portalB
		});
	}

	private void ClearCurrentlyConnectingPortals()
	{
		foreach (ConnectingPortals currentlyConnectingPortal in m_currentlyConnectingPortals)
		{
			ForceSetConnection(currentlyConnectingPortal.PortalA, currentlyConnectingPortal.PortalB.m_uid);
			ForceSetConnection(currentlyConnectingPortal.PortalB, currentlyConnectingPortal.PortalA.m_uid);
		}
		m_currentlyConnectingPortals.Clear();
	}

	private bool IsCurrentlyConnectingPortal(ZDO zdo)
	{
		foreach (ConnectingPortals currentlyConnectingPortal in m_currentlyConnectingPortals)
		{
			if (zdo == currentlyConnectingPortal.PortalA || zdo == currentlyConnectingPortal.PortalB)
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateSleeping()
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		if (m_sleeping)
		{
			if (!EnvMan.instance.IsTimeSkipping())
			{
				m_lastSleepTime = ZNet.instance.GetTimeSeconds();
				m_sleeping = false;
				ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStop");
			}
		}
		else if (!EnvMan.instance.IsTimeSkipping() && (EnvMan.IsAfternoon() || EnvMan.IsNight()) && EverybodyIsTryingToSleep() && !(ZNet.instance.GetTimeSeconds() - m_lastSleepTime < 10.0))
		{
			EnvMan.instance.SkipToMorning();
			m_sleeping = true;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStart");
		}
	}

	private bool EverybodyIsTryingToSleep()
	{
		List<ZDO> allCharacterZDOS = ZNet.instance.GetAllCharacterZDOS();
		if (allCharacterZDOS.Count == 0)
		{
			return false;
		}
		foreach (ZDO item in allCharacterZDOS)
		{
			if (!item.GetBool(ZDOVars.s_inBed))
			{
				return false;
			}
		}
		return true;
	}

	private void SleepStart(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			localPlayer.SetSleeping(sleep: true);
		}
	}

	private void SleepStop(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			localPlayer.SetSleeping(sleep: false);
			localPlayer.AttachStop();
		}
		if (m_saveTimer > 60f)
		{
			if (!ZNet.instance.EnoughDiskSpaceAvailable(out var _))
			{
				m_saveTimer -= 300f;
				return;
			}
			SavePlayerProfile(setLogoutPoint: false);
			if ((bool)ZNet.instance)
			{
				ZNet.instance.Save(sync: false);
			}
		}
		else
		{
			ZLog.Log("Saved recently, skipping sleep save.");
		}
	}

	public void DiscoverClosestLocation(string name, Vector3 point, string pinName, int pinType, bool showMap = true, bool discoverAll = false)
	{
		ZLog.Log("DiscoverClosestLocation");
		ZRoutedRpc.instance.InvokeRoutedRPC("RPC_DiscoverClosestLocation", name, point, pinName, pinType, showMap, discoverAll);
	}

	private void RPC_DiscoverClosestLocation(long sender, string name, Vector3 point, string pinName, int pinType, bool showMap, bool discoverAll)
	{
		if (discoverAll && ZoneSystem.instance.FindLocations(name, ref m_tempLocations))
		{
			ZLog.Log($"Found {m_tempLocations.Count} locations of type {name}");
			{
				foreach (ZoneSystem.LocationInstance tempLocation in m_tempLocations)
				{
					ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_DiscoverLocationResponse", pinName, pinType, tempLocation.m_position, showMap);
				}
				return;
			}
		}
		if (!discoverAll && ZoneSystem.instance.FindClosestLocation(name, point, out var closest))
		{
			ZLog.Log("Found location of type " + name);
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_DiscoverLocationResponse", pinName, pinType, closest.m_position, showMap);
		}
		else
		{
			ZLog.LogWarning("Failed to find location of type " + name);
		}
	}

	private void RPC_DiscoverLocationResponse(long sender, string pinName, int pinType, Vector3 pos, bool showMap)
	{
		Minimap.instance.DiscoverLocation(pos, (Minimap.PinType)pinType, pinName, showMap);
		if ((bool)Player.m_localPlayer && Minimap.instance.m_mode == Minimap.MapMode.None)
		{
			Player.m_localPlayer.SetLookDir(pos - Player.m_localPlayer.transform.position, 3.5f);
		}
	}

	public void Ping()
	{
		if ((bool)Console.instance)
		{
			Console.instance.Print("Ping sent to server");
		}
		ZRoutedRpc.instance.InvokeRoutedRPC("Ping", Time.time);
	}

	private void RPC_Ping(long sender, float time)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(sender, "Pong", time);
	}

	private void RPC_Pong(long sender, float time)
	{
		float num = Time.time - time;
		string text = "Got ping reply from server: " + (int)(num * 1000f) + " ms";
		ZLog.Log(text);
		if ((bool)Console.instance)
		{
			Console.instance.Print(text);
		}
		if ((bool)Chat.instance)
		{
			Chat.instance.AddString(text);
		}
	}

	public void SetForcePlayerDifficulty(int players)
	{
		m_forcePlayers = players;
	}

	public int GetPlayerDifficulty(Vector3 pos)
	{
		if (m_forcePlayers > 0)
		{
			return m_forcePlayers;
		}
		int num = Player.GetPlayersInRangeXZ(pos, m_difficultyScaleRange);
		if (num < 1)
		{
			num = 1;
		}
		if (num > m_difficultyScaleMaxPlayers)
		{
			num = m_difficultyScaleMaxPlayers;
		}
		return num;
	}

	public float GetDifficultyDamageScalePlayer(Vector3 pos)
	{
		int playerDifficulty = GetPlayerDifficulty(pos);
		return 1f + (float)(playerDifficulty - 1) * m_damageScalePerPlayer;
	}

	public float GetDifficultyDamageScaleEnemy(Vector3 pos)
	{
		int playerDifficulty = GetPlayerDifficulty(pos);
		float num = 1f + (float)(playerDifficulty - 1) * m_healthScalePerPlayer;
		return 1f / num;
	}

	private static void UpdatePause()
	{
		if (m_pauseFrom != m_pauseTarget)
		{
			if (DateTime.Now >= m_pauseEnd)
			{
				m_pauseFrom = m_pauseTarget;
				m_timeScale = m_pauseTarget;
			}
			else
			{
				m_timeScale = Mathf.SmoothStep(m_pauseFrom, m_pauseTarget, (float)((DateTime.Now - m_pauseStart).TotalSeconds / (m_pauseEnd - m_pauseStart).TotalSeconds));
			}
		}
		if (Time.timeScale > 0f)
		{
			m_pauseRotateFade = 0f;
		}
		Time.timeScale = (IsPaused() ? 0f : ((ZNet.instance.GetPeerConnections() > 0) ? 1f : m_timeScale));
		if (IsPaused())
		{
			m_pauseTimer += Time.fixedUnscaledDeltaTime;
		}
		else if (m_pauseTimer > 0f)
		{
			m_pauseTimer = 0f;
		}
		if (IsPaused() && Menu.IsVisible() && (bool)Player.m_localPlayer)
		{
			if (m_pauseRotateFade < 1f)
			{
				Mathf.Min(1f, m_pauseRotateFade += 0.05f * Time.unscaledDeltaTime);
			}
			Transform eye = Player.m_localPlayer.m_eye;
			Vector3 forward = Player.m_localPlayer.m_eye.forward;
			float num = Vector3.Dot(forward, Vector3.up);
			float num2 = Vector3.Dot(forward, Vector3.down);
			float num3 = Mathf.Max(0.05f, 1f - ((num > num2) ? num : num2));
			eye.Rotate(Vector3.up, Time.unscaledDeltaTime * Mathf.Cos(Time.realtimeSinceStartup * 0.3f) * 5f * m_pauseRotateFade * num3);
			Player.m_localPlayer.SetLookDir(eye.forward);
			m_collectTimer += Time.fixedUnscaledDeltaTime;
			if (m_collectTimer > 5f && DateTime.Now > ZInput.instance.GetLastInputTimer() + TimeSpan.FromSeconds(5.0))
			{
				instance.CollectResourcesCheck();
				m_collectTimer = -1000f;
			}
		}
		else if (m_collectTimer != 0f)
		{
			m_collectTimer = 0f;
		}
	}

	public static bool IsPaused()
	{
		if (m_pause)
		{
			return CanPause();
		}
		return false;
	}

	public static void Pause()
	{
		m_pause = true;
	}

	public static void Unpause()
	{
		m_pause = false;
		m_timeScale = 1f;
	}

	public static void PauseToggle()
	{
		if (IsPaused())
		{
			Unpause();
		}
		else
		{
			Pause();
		}
	}

	private static bool CanPause()
	{
		if ((ZNet.instance.IsServer() && ZNet.instance.GetPeerConnections() > 0) || !Player.m_localPlayer || !ZNet.instance)
		{
			return false;
		}
		if (Player.m_debugMode && !ZNet.instance.IsServer() && (bool)Console.instance && Console.instance.IsCheatsEnabled())
		{
			return true;
		}
		if (ZNet.instance.IsServer() && ZNet.instance.GetPeerConnections() == 0)
		{
			return true;
		}
		return false;
	}

	public static void FadeTimeScale(float timeScale = 0f, float transitionSec = 0f)
	{
		if (timeScale == 1f || CanPause())
		{
			timeScale = Mathf.Clamp(timeScale, 0f, 100f);
			if (transitionSec == 0f)
			{
				m_timeScale = timeScale;
				return;
			}
			m_pauseFrom = Time.timeScale;
			m_pauseTarget = timeScale;
			m_pauseStart = DateTime.Now;
			m_pauseEnd = DateTime.Now + TimeSpan.FromSeconds(transitionSec);
		}
	}

	public int ScaleDrops(GameObject drop, int amount)
	{
		if (m_resourceRate != 1f)
		{
			ItemDrop component = drop.GetComponent<ItemDrop>();
			if ((object)component != null)
			{
				return ScaleDrops(component.m_itemData, amount);
			}
		}
		return amount;
	}

	public int ScaleDrops(ItemDrop.ItemData data, int amount)
	{
		if (m_resourceRate != 1f && !m_nonScaledDropTypes.Contains(data.m_shared.m_itemType))
		{
			amount = (int)Mathf.Clamp(Mathf.Round((float)amount * m_resourceRate), 1f, (data.m_shared.m_maxStackSize > 1) ? data.m_shared.m_maxStackSize : 1000);
		}
		return amount;
	}

	public int ScaleDrops(GameObject drop, int randomMin, int randomMax)
	{
		if (m_resourceRate != 1f)
		{
			ItemDrop component = drop.GetComponent<ItemDrop>();
			if ((object)component != null)
			{
				return ScaleDrops(component.m_itemData, randomMin, randomMax);
			}
		}
		return UnityEngine.Random.Range(randomMin, randomMax);
	}

	public int ScaleDrops(ItemDrop.ItemData data, int randomMin, int randomMax)
	{
		if (m_resourceRate != 1f && !m_nonScaledDropTypes.Contains(data.m_shared.m_itemType))
		{
			return Mathf.Min(ScaleDrops(randomMin, randomMax), (data.m_shared.m_maxStackSize > 1) ? data.m_shared.m_maxStackSize : 10000);
		}
		return UnityEngine.Random.Range(randomMin, randomMax);
	}

	public int ScaleDrops(int randomMin, int randomMax)
	{
		return (int)Mathf.Max(1f, Mathf.Round(UnityEngine.Random.Range((float)randomMin, (float)randomMax) * m_resourceRate));
	}

	public int ScaleDropsInverse(GameObject drop, int randomMin, int randomMax)
	{
		if (m_resourceRate != 1f)
		{
			ItemDrop component = drop.GetComponent<ItemDrop>();
			if ((object)component != null)
			{
				return ScaleDropsInverse(component.m_itemData, randomMin, randomMax);
			}
		}
		return UnityEngine.Random.Range(randomMin, randomMax);
	}

	public int ScaleDropsInverse(ItemDrop.ItemData data, int randomMin, int randomMax)
	{
		if (m_resourceRate != 1f && !m_nonScaledDropTypes.Contains(data.m_shared.m_itemType))
		{
			return ScaleDropsInverse(randomMin, Mathf.Min(randomMax, (data.m_shared.m_maxStackSize > 1) ? data.m_shared.m_maxStackSize : 1000));
		}
		return UnityEngine.Random.Range(randomMin, randomMax);
	}

	public int ScaleDropsInverse(int randomMin, int randomMax)
	{
		return (int)Mathf.Max(1f, Mathf.Round(UnityEngine.Random.Range((float)randomMin, (float)randomMax) / m_resourceRate));
	}

	public static void UpdateWorldRates(HashSet<string> globalKeys, Dictionary<string, string> globalKeysValues)
	{
		List<string> playerKeys = (Player.m_localPlayer ? Player.m_localPlayer.GetUniqueKeys() : null);
		trySetScalarKey(GlobalKeys.PlayerDamage, out m_playerDamageRate);
		trySetScalarKey(GlobalKeys.EnemyDamage, out m_enemyDamageRate);
		trySetScalarKey(GlobalKeys.ResourceRate, out m_resourceRate);
		trySetScalarKey(GlobalKeys.StaminaRate, out m_staminaRate);
		trySetScalarKey(GlobalKeys.MoveStaminaRate, out m_moveStaminaRate);
		trySetScalarKey(GlobalKeys.StaminaRegenRate, out m_staminaRegenRate);
		trySetScalarKey(GlobalKeys.SkillGainRate, out m_skillGainRate);
		trySetScalarKey(GlobalKeys.SkillReductionRate, out m_skillReductionRate);
		trySetScalarKey(GlobalKeys.EnemySpeedSize, out m_enemySpeedSize);
		trySetScalarKey(GlobalKeys.EnemyLevelUpRate, out m_enemyLevelUpRate);
		trySetIntKey(GlobalKeys.WorldLevel, out m_worldLevel, 0);
		trySetScalarKey(GlobalKeys.EventRate, out m_eventRate);
		trySetScalarKeyPlayer(PlayerKeys.DamageTaken, out m_localDamgeTakenRate);
		m_worldLevel = Mathf.Clamp(m_worldLevel, 0, 10);
		UpdateNoMap();
		m_serverOptionsSummary = ServerOptionsGUI.GetWorldModifierSummary(globalKeys);
		PlayerProfile playerProfile = instance.GetPlayerProfile();
		for (int i = 0; i < globalKeys.Count; i++)
		{
			GlobalKeys globalKeys2 = (GlobalKeys)i;
			if (!ZoneSystem.instance.GetGlobalKey(globalKeys2))
			{
				playerProfile.m_knownWorldKeys.IncrementOrSet(string.Format("{0} {1}", globalKeys2, "default"));
			}
		}
		void trySetIntKey(GlobalKeys key, out int value, int defaultValue = 1)
		{
			value = defaultValue;
			if (globalKeysValues.TryGetValue(key.ToString().ToLower(), out var value2) && int.TryParse(value2, NumberStyles.Any, CultureInfo.InvariantCulture, out var result2))
			{
				value = result2;
			}
		}
		void trySetScalarKey(GlobalKeys key, out float value, float defaultValue = 1f, float multiplier = 100f)
		{
			value = defaultValue;
			if (globalKeysValues.TryGetValue(key.ToString().ToLower(), out var value3) && float.TryParse(value3, NumberStyles.Any, CultureInfo.InvariantCulture, out var result3))
			{
				value = result3 / multiplier;
			}
		}
		void trySetScalarKeyPlayer(PlayerKeys key, out float value, float defaultValue = 1f, float multiplier = 100f)
		{
			if (playerKeys == null)
			{
				value = defaultValue;
				return;
			}
			value = defaultValue;
			foreach (string item in playerKeys)
			{
				string[] array = item.Split(' ', StringSplitOptions.None);
				if (array.Length >= 2 && array[0].ToLower() == key.ToString().ToLower() && float.TryParse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
				{
					value = result / multiplier;
					break;
				}
			}
		}
	}

	public static void UpdateNoMap()
	{
		m_noMap = ((bool)ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoMap)) || (Player.m_localPlayer != null && PlayerPrefs.GetFloat("mapenabled_" + Player.m_localPlayer.GetPlayerName(), 1f) == 0f);
		Minimap.instance.SetMapMode((!m_noMap) ? Minimap.MapMode.Small : Minimap.MapMode.None);
	}

	public GameObject CheckDropConversion(HitData hitData, ItemDrop itemDrop, GameObject dropPrefab, ref int dropCount)
	{
		if (hitData == null)
		{
			return dropPrefab;
		}
		HitData.DamageType majorityDamageType = hitData.m_damage.GetMajorityDamageType();
		HitData.HitType hitType = hitData.m_hitType;
		bool flag = majorityDamageType == HitData.DamageType.Fire && ZoneSystem.instance.GetGlobalKey(GlobalKeys.Fire);
		foreach (ItemConversion damageTypeDropConversion in m_damageTypeDropConversions)
		{
			if ((damageTypeDropConversion.m_hitType != 0 && damageTypeDropConversion.m_hitType != hitType && !flag) || damageTypeDropConversion.m_damageType != majorityDamageType)
			{
				continue;
			}
			foreach (ItemDrop item in damageTypeDropConversion.m_items)
			{
				if (!(item.m_itemData.m_shared.m_name != itemDrop.m_itemData.m_shared.m_name))
				{
					dropCount *= damageTypeDropConversion.m_multiplier;
					return damageTypeDropConversion.m_result.gameObject;
				}
			}
		}
		return dropPrefab;
	}
}
