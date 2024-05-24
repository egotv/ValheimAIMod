using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GUIFramework;
using PlatformTools.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
	public enum MapMode
	{
		None,
		Small,
		Large
	}

	public enum PinType
	{
		Icon0,
		Icon1,
		Icon2,
		Icon3,
		Death,
		Bed,
		Icon4,
		Shout,
		None,
		Boss,
		Player,
		RandomEvent,
		Ping,
		EventArea,
		Hildir1,
		Hildir2,
		Hildir3
	}

	public class PinData
	{
		public string m_name;

		public PinType m_type;

		public Sprite m_icon;

		public Vector3 m_pos;

		public bool m_save;

		public long m_ownerID;

		public string m_author = "";

		public bool m_shouldDelete;

		public bool m_checked;

		public bool m_doubleSize;

		public bool m_animate;

		public float m_worldSize;

		public RectTransform m_uiElement;

		public GameObject m_checkedElement;

		public Image m_iconElement;

		public PinNameData m_NamePinData;
	}

	public class PinNameData
	{
		public readonly PinData ParentPin;

		public TMP_Text PinNameText { get; private set; }

		public GameObject PinNameGameObject { get; private set; }

		public RectTransform PinNameRectTransform { get; private set; }

		public PinNameData(PinData pin)
		{
			ParentPin = pin;
		}

		internal void SetTextAndGameObject(GameObject text)
		{
			PinNameGameObject = text;
			PinNameText = PinNameGameObject.GetComponentInChildren<TMP_Text>();
			if (string.IsNullOrEmpty(ParentPin.m_author) || ParentPin.m_author == PrivilegeManager.GetNetworkUserId())
			{
				PinNameText.text = Localization.instance.Localize(ParentPin.m_name);
			}
			else
			{
				PinNameText.text = CensorShittyWords.FilterUGC(Localization.instance.Localize(ParentPin.m_name), UGCType.Text, ParentPin.m_author, 0L);
			}
			PinNameRectTransform = text.GetComponent<RectTransform>();
		}

		internal void DestroyMapMarker()
		{
			UnityEngine.Object.Destroy(PinNameGameObject);
			PinNameGameObject = null;
		}
	}

	[Serializable]
	public struct SpriteData
	{
		public PinType m_name;

		public Sprite m_icon;
	}

	[Serializable]
	public struct LocationSpriteData
	{
		public string m_name;

		public Sprite m_icon;
	}

	private Color forest = new Color(1f, 0f, 0f, 0f);

	private Color noForest = new Color(0f, 0f, 0f, 0f);

	private static int MAPVERSION = 8;

	private float inputDelay;

	private const int sharedMapDataVersion = 3;

	private static Minimap m_instance;

	public GameObject m_smallRoot;

	public GameObject m_largeRoot;

	public RawImage m_mapImageSmall;

	public RawImage m_mapImageLarge;

	public RectTransform m_pinRootSmall;

	public RectTransform m_pinRootLarge;

	public RectTransform m_pinNameRootSmall;

	public RectTransform m_pinNameRootLarge;

	public TMP_Text m_biomeNameSmall;

	public TMP_Text m_biomeNameLarge;

	public RectTransform m_smallShipMarker;

	public RectTransform m_largeShipMarker;

	public RectTransform m_smallMarker;

	public RectTransform m_largeMarker;

	public RectTransform m_windMarker;

	public RectTransform m_gamepadCrosshair;

	public Toggle m_publicPosition;

	public Image m_selectedIcon0;

	public Image m_selectedIcon1;

	public Image m_selectedIcon2;

	public Image m_selectedIcon3;

	public Image m_selectedIcon4;

	public Image m_selectedIconDeath;

	public Image m_selectedIconBoss;

	private Dictionary<PinType, Image> m_selectedIcons = new Dictionary<PinType, Image>();

	private bool[] m_visibleIconTypes;

	private bool m_showSharedMapData = true;

	public float m_sharedMapDataFadeRate = 2f;

	private float m_sharedMapDataFade;

	public GameObject m_mapSmall;

	public GameObject m_mapLarge;

	private Material m_mapSmallShader;

	private Material m_mapLargeShader;

	public GameObject m_pinPrefab;

	[SerializeField]
	private GameObject m_pinNamePrefab;

	public GuiInputField m_nameInput;

	public int m_textureSize = 256;

	public float m_pixelSize = 64f;

	public float m_minZoom = 0.01f;

	public float m_maxZoom = 1f;

	public float m_showNamesZoom = 0.5f;

	public float m_exploreInterval = 2f;

	public float m_exploreRadius = 100f;

	public float m_removeRadius = 128f;

	public float m_pinSizeSmall = 32f;

	public float m_pinSizeLarge = 48f;

	public float m_clickDuration = 0.25f;

	public List<SpriteData> m_icons = new List<SpriteData>();

	public List<LocationSpriteData> m_locationIcons = new List<LocationSpriteData>();

	public Color m_meadowsColor = new Color(0.45f, 1f, 0.43f);

	public Color m_ashlandsColor = new Color(1f, 0.2f, 0.2f);

	public Color m_blackforestColor = new Color(0f, 0.7f, 0f);

	public Color m_deepnorthColor = new Color(1f, 1f, 1f);

	public Color m_heathColor = new Color(1f, 1f, 0.2f);

	public Color m_swampColor = new Color(0.6f, 0.5f, 0.5f);

	public Color m_mountainColor = new Color(1f, 1f, 1f);

	private Color m_mistlandsColor = new Color(0.2f, 0.2f, 0.2f);

	private PinData m_namePin;

	private PinType m_selectedType;

	private PinData m_deathPin;

	private PinData m_spawnPointPin;

	private Dictionary<Vector3, PinData> m_locationPins = new Dictionary<Vector3, PinData>();

	private float m_updateLocationsTimer;

	private List<PinData> m_pingPins = new List<PinData>();

	private List<PinData> m_shoutPins = new List<PinData>();

	private List<Chat.WorldTextInstance> m_tempShouts = new List<Chat.WorldTextInstance>();

	private List<PinData> m_playerPins = new List<PinData>();

	private List<ZNet.PlayerInfo> m_tempPlayerInfo = new List<ZNet.PlayerInfo>();

	private PinData m_randEventPin;

	private PinData m_randEventAreaPin;

	private float m_updateEventTime;

	private bool[] m_explored;

	private bool[] m_exploredOthers;

	public GameObject m_sharedMapHint;

	public List<GameObject> m_hints;

	private List<PinData> m_pins = new List<PinData>();

	private bool m_pinUpdateRequired;

	private Vector3 m_previousMapCenter = Vector3.zero;

	private float m_previousLargeZoom = 0.1f;

	private float m_previousSmallZoom = 0.01f;

	private Texture2D m_forestMaskTexture;

	private Texture2D m_mapTexture;

	private Texture2D m_heightTexture;

	private Texture2D m_fogTexture;

	private float m_largeZoom = 0.1f;

	private float m_smallZoom = 0.01f;

	private Heightmap.Biome m_biome;

	[HideInInspector]
	public MapMode m_mode;

	public float m_nomapPingDistance = 50f;

	private float m_exploreTimer;

	private bool m_hasGenerated;

	private bool m_dragView = true;

	private Vector3 m_mapOffset = Vector3.zero;

	private float m_leftDownTime;

	private float m_leftClickTime;

	private Vector3 m_dragWorldPos = Vector3.zero;

	private bool m_wasFocused;

	private float m_delayTextInput;

	private float m_pauseUpdate;

	private const bool m_enableLastDeathAutoPin = false;

	private int m_hiddenFrames;

	[SerializeField]
	private float m_gamepadMoveSpeed = 0.33f;

	private string m_forestMaskTexturePath;

	private const string c_forestMaskTextureName = "forestMaskTexCache";

	private string m_mapTexturePath;

	private const string c_mapTextureName = "mapTexCache";

	private string m_heightTexturePath;

	private const string c_heightTextureName = "heightTexCache";

	public static Minimap instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_largeRoot.SetActive(value: false);
		m_smallRoot.SetActive(value: false);
		Localization.OnLanguageChange = (Action)Delegate.Combine(Localization.OnLanguageChange, new Action(OnLanguageChange));
		PlatformManager.Instance.SuspendAndConstrainManager.OnConstrainedModeActivated += OnConstrainedModeActivated;
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChange = (Action)Delegate.Remove(Localization.OnLanguageChange, new Action(OnLanguageChange));
		GuiInputField nameInput = m_nameInput;
		nameInput.VirtualKeyboardStateChange = (Action<VirtualKeyboardState>)Delegate.Remove(nameInput.VirtualKeyboardStateChange, new Action<VirtualKeyboardState>(VirtualKeyboardStateChange));
		PlatformManager.Instance.SuspendAndConstrainManager.OnConstrainedModeActivated -= OnConstrainedModeActivated;
		m_instance = null;
	}

	private void OnConstrainedModeActivated(bool active)
	{
		m_pauseUpdate = 0.1f;
	}

	public static bool IsOpen()
	{
		if ((bool)m_instance)
		{
			if (!m_instance.m_largeRoot.activeSelf)
			{
				return m_instance.m_hiddenFrames <= 2;
			}
			return true;
		}
		return false;
	}

	public static bool InTextInput()
	{
		if ((bool)m_instance && m_instance.m_mode == MapMode.Large)
		{
			return m_instance.m_wasFocused;
		}
		return false;
	}

	private void OnLanguageChange()
	{
		Player localPlayer = Player.m_localPlayer;
		if (!(localPlayer == null))
		{
			m_biomeNameSmall.text = Localization.instance.Localize("$biome_" + localPlayer.GetCurrentBiome().ToString().ToLower());
		}
	}

	private void Start()
	{
		m_mapTexture = new Texture2D(m_textureSize, m_textureSize, TextureFormat.RGB24, mipChain: false);
		m_mapTexture.name = "_Minimap m_mapTexture";
		m_mapTexture.wrapMode = TextureWrapMode.Clamp;
		m_forestMaskTexture = new Texture2D(m_textureSize, m_textureSize, TextureFormat.RGBA32, mipChain: false);
		m_forestMaskTexture.name = "_Minimap m_forestMaskTexture";
		m_forestMaskTexture.wrapMode = TextureWrapMode.Clamp;
		m_heightTexture = new Texture2D(m_textureSize, m_textureSize, TextureFormat.RFloat, mipChain: false);
		m_heightTexture.name = "_Minimap m_heightTexture";
		m_heightTexture.wrapMode = TextureWrapMode.Clamp;
		m_fogTexture = new Texture2D(m_textureSize, m_textureSize, TextureFormat.RGBA32, mipChain: false);
		m_fogTexture.name = "_Minimap m_fogTexture";
		m_fogTexture.wrapMode = TextureWrapMode.Clamp;
		m_explored = new bool[m_textureSize * m_textureSize];
		m_exploredOthers = new bool[m_textureSize * m_textureSize];
		m_mapImageLarge.material = UnityEngine.Object.Instantiate(m_mapImageLarge.material);
		m_mapImageSmall.material = UnityEngine.Object.Instantiate(m_mapImageSmall.material);
		m_mapSmallShader = m_mapImageSmall.material;
		m_mapLargeShader = m_mapImageLarge.material;
		m_mapLargeShader.SetTexture("_MainTex", m_mapTexture);
		m_mapLargeShader.SetTexture("_MaskTex", m_forestMaskTexture);
		m_mapLargeShader.SetTexture("_HeightTex", m_heightTexture);
		m_mapLargeShader.SetTexture("_FogTex", m_fogTexture);
		m_mapSmallShader.SetTexture("_MainTex", m_mapTexture);
		m_mapSmallShader.SetTexture("_MaskTex", m_forestMaskTexture);
		m_mapSmallShader.SetTexture("_HeightTex", m_heightTexture);
		m_mapSmallShader.SetTexture("_FogTex", m_fogTexture);
		m_nameInput.gameObject.SetActive(value: false);
		UIInputHandler component = m_mapImageLarge.GetComponent<UIInputHandler>();
		component.m_onRightClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onRightClick, new Action<UIInputHandler>(OnMapRightClick));
		component.m_onMiddleClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onMiddleClick, new Action<UIInputHandler>(OnMapMiddleClick));
		component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(OnMapLeftDown));
		component.m_onLeftUp = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftUp, new Action<UIInputHandler>(OnMapLeftUp));
		m_visibleIconTypes = new bool[Enum.GetValues(typeof(PinType)).Length];
		for (int i = 0; i < m_visibleIconTypes.Length; i++)
		{
			m_visibleIconTypes[i] = true;
		}
		m_selectedIcons[PinType.Death] = m_selectedIconDeath;
		m_selectedIcons[PinType.Boss] = m_selectedIconBoss;
		m_selectedIcons[PinType.Icon0] = m_selectedIcon0;
		m_selectedIcons[PinType.Icon1] = m_selectedIcon1;
		m_selectedIcons[PinType.Icon2] = m_selectedIcon2;
		m_selectedIcons[PinType.Icon3] = m_selectedIcon3;
		m_selectedIcons[PinType.Icon4] = m_selectedIcon4;
		SelectIcon(PinType.Icon0);
		Reset();
		if (ZNet.World != null)
		{
			_ = ZNet.World;
			string rootPath = ZNet.World.GetRootPath(FileHelpers.FileSource.Local);
			Directory.CreateDirectory(World.GetWorldSavePath(FileHelpers.FileSource.Local));
			m_forestMaskTexturePath = GetCompleteTexturePath(rootPath, "forestMaskTexCache");
			m_mapTexturePath = GetCompleteTexturePath(rootPath, "mapTexCache");
			m_heightTexturePath = GetCompleteTexturePath(rootPath, "heightTexCache");
		}
	}

	public void Reset()
	{
		Color32[] array = new Color32[m_textureSize * m_textureSize];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		}
		m_fogTexture.SetPixels32(array);
		m_fogTexture.Apply();
		for (int j = 0; j < m_explored.Length; j++)
		{
			m_explored[j] = false;
			m_exploredOthers[j] = false;
		}
		m_sharedMapHint.gameObject.SetActive(value: false);
	}

	public void ResetSharedMapData()
	{
		Color[] pixels = m_fogTexture.GetPixels();
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i].g = 255f;
		}
		m_fogTexture.SetPixels(pixels);
		m_fogTexture.Apply();
		for (int j = 0; j < m_exploredOthers.Length; j++)
		{
			m_exploredOthers[j] = false;
		}
		for (int num = m_pins.Count - 1; num >= 0; num--)
		{
			PinData pinData = m_pins[num];
			if (pinData.m_ownerID != 0L)
			{
				DestroyPinMarker(pinData);
				m_pins.RemoveAt(num);
			}
		}
		m_sharedMapHint.gameObject.SetActive(value: false);
	}

	public void ForceRegen()
	{
		if (WorldGenerator.instance != null)
		{
			GenerateWorldMap();
		}
	}

	private void Update()
	{
		if (ZInput.VirtualKeyboardOpen)
		{
			return;
		}
		if (m_pauseUpdate > 0f)
		{
			m_pauseUpdate -= Time.deltaTime;
			return;
		}
		inputDelay = Mathf.Max(0f, inputDelay - Time.deltaTime);
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Utils.GetMainCamera() == null)
		{
			return;
		}
		if (!m_hasGenerated)
		{
			if (WorldGenerator.instance == null)
			{
				return;
			}
			if (!TryLoadMinimapTextureData())
			{
				GenerateWorldMap();
			}
			LoadMapData();
			m_hasGenerated = true;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		UpdateExplore(deltaTime, localPlayer);
		if (localPlayer.IsDead())
		{
			SetMapMode(MapMode.None);
			return;
		}
		if (m_mode == MapMode.None)
		{
			SetMapMode(MapMode.Small);
		}
		if (m_mode == MapMode.Large)
		{
			m_hiddenFrames = 0;
		}
		else
		{
			m_hiddenFrames++;
		}
		bool flag = (Chat.instance == null || !Chat.instance.HasFocus()) && !Console.IsVisible() && !TextInput.IsVisible() && !Menu.IsActive() && !InventoryGui.IsVisible();
		if (flag)
		{
			if (InTextInput())
			{
				if ((ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButton("JoyButtonB")) && m_namePin != null)
				{
					m_nameInput.text = "";
					OnPinTextEntered("");
				}
			}
			else if (ZInput.GetButtonDown("Map") || (ZInput.GetButtonDown("JoyMap") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")) && !ZInput.GetButton("JoyAltKeys")) || (m_mode == MapMode.Large && (ZInput.GetKeyDown(KeyCode.Escape) || (ZInput.GetButtonDown("JoyMap") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper"))) || ZInput.GetButtonDown("JoyButtonB"))))
			{
				switch (m_mode)
				{
				case MapMode.None:
					SetMapMode(MapMode.Small);
					break;
				case MapMode.Small:
					SetMapMode(MapMode.Large);
					break;
				case MapMode.Large:
					SetMapMode(MapMode.Small);
					break;
				}
			}
		}
		if (m_mode == MapMode.Large)
		{
			m_publicPosition.isOn = ZNet.instance.IsReferencePositionPublic();
			m_gamepadCrosshair.gameObject.SetActive(ZInput.IsGamepadActive());
		}
		if (m_showSharedMapData && m_sharedMapDataFade < 1f)
		{
			m_sharedMapDataFade = Mathf.Min(1f, m_sharedMapDataFade + m_sharedMapDataFadeRate * deltaTime);
			m_mapSmallShader.SetFloat("_SharedFade", m_sharedMapDataFade);
			m_mapLargeShader.SetFloat("_SharedFade", m_sharedMapDataFade);
			if (m_sharedMapDataFade == 1f)
			{
				m_pinUpdateRequired = true;
			}
		}
		else if (!m_showSharedMapData && m_sharedMapDataFade > 0f)
		{
			m_sharedMapDataFade = Mathf.Max(0f, m_sharedMapDataFade - m_sharedMapDataFadeRate * deltaTime);
			m_mapSmallShader.SetFloat("_SharedFade", m_sharedMapDataFade);
			m_mapLargeShader.SetFloat("_SharedFade", m_sharedMapDataFade);
			if (m_sharedMapDataFade == 0f)
			{
				m_pinUpdateRequired = true;
			}
		}
		UpdateMap(localPlayer, deltaTime, flag);
		UpdateDynamicPins(deltaTime);
		if (m_pinUpdateRequired)
		{
			m_pinUpdateRequired = false;
			UpdatePins();
		}
		UpdateBiome(localPlayer);
		UpdateNameInput();
	}

	private bool TryLoadMinimapTextureData()
	{
		if (string.IsNullOrEmpty(m_forestMaskTexturePath) || !File.Exists(m_forestMaskTexturePath) || !File.Exists(m_mapTexturePath) || !File.Exists(m_heightTexturePath) || 34 != ZNet.World.m_worldVersion)
		{
			return false;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		Texture2D texture2D = new Texture2D(m_forestMaskTexture.width, m_forestMaskTexture.height, TextureFormat.ARGB32, mipChain: false);
		if (!texture2D.LoadImage(File.ReadAllBytes(m_forestMaskTexturePath)))
		{
			return false;
		}
		m_forestMaskTexture.SetPixels(texture2D.GetPixels());
		m_forestMaskTexture.Apply();
		if (!texture2D.LoadImage(File.ReadAllBytes(m_mapTexturePath)))
		{
			return false;
		}
		m_mapTexture.SetPixels(texture2D.GetPixels());
		m_mapTexture.Apply();
		if (!texture2D.LoadImage(File.ReadAllBytes(m_heightTexturePath)))
		{
			return false;
		}
		Color[] pixels = texture2D.GetPixels();
		for (int i = 0; i < m_textureSize; i++)
		{
			for (int j = 0; j < m_textureSize; j++)
			{
				int num = i * m_textureSize + j;
				int num2 = (int)(pixels[num].r * 255f);
				int num3 = (int)(pixels[num].g * 255f);
				int num4 = (num2 << 8) + num3;
				float num5 = 127.5f;
				pixels[num].r = (float)num4 / num5;
			}
		}
		m_heightTexture.SetPixels(pixels);
		m_heightTexture.Apply();
		ZLog.Log("Loading minimap textures done [" + stopwatch.ElapsedMilliseconds + "ms]");
		return true;
	}

	private void ShowPinNameInput(Vector3 pos)
	{
		m_namePin = AddPin(pos, m_selectedType, "", save: true, isChecked: false, 0L);
		m_nameInput.text = "";
		m_nameInput.gameObject.SetActive(value: true);
		if (ZInput.IsGamepadActive())
		{
			m_nameInput.gameObject.transform.localPosition = new Vector3(0f, -30f, 0f);
		}
		else
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(m_nameInput.gameObject.transform.parent.GetComponent<RectTransform>(), ZInput.mousePosition, null, out var localPoint);
			m_nameInput.gameObject.transform.localPosition = new Vector3(localPoint.x, localPoint.y - 30f);
		}
		if (Application.isConsolePlatform && ZInput.IsGamepadActive())
		{
			m_nameInput.Select();
		}
		else
		{
			m_nameInput.ActivateInputField();
		}
		GuiInputField nameInput = m_nameInput;
		nameInput.VirtualKeyboardStateChange = (Action<VirtualKeyboardState>)Delegate.Remove(nameInput.VirtualKeyboardStateChange, new Action<VirtualKeyboardState>(VirtualKeyboardStateChange));
		GuiInputField nameInput2 = m_nameInput;
		nameInput2.VirtualKeyboardStateChange = (Action<VirtualKeyboardState>)Delegate.Combine(nameInput2.VirtualKeyboardStateChange, new Action<VirtualKeyboardState>(VirtualKeyboardStateChange));
		m_wasFocused = true;
	}

	private void UpdateNameInput()
	{
		if (!(m_delayTextInput < 0f))
		{
			m_delayTextInput -= Time.deltaTime;
			m_wasFocused = m_delayTextInput > 0f;
		}
	}

	private void VirtualKeyboardStateChange(VirtualKeyboardState state)
	{
		if (state == VirtualKeyboardState.Cancel)
		{
			HidePinTextInput(delayTextInput: true);
		}
	}

	private void CreateMapNamePin(PinData namePin, RectTransform root)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(m_pinNamePrefab, root);
		namePin.m_NamePinData.SetTextAndGameObject(gameObject);
		namePin.m_NamePinData.PinNameRectTransform.SetParent(root);
		m_pinUpdateRequired = true;
		StartCoroutine(DelayActivation(gameObject, 1f));
	}

	private IEnumerator DelayActivation(GameObject go, float delay)
	{
		go.SetActive(value: false);
		yield return new WaitForSeconds(delay);
		if (!(this == null) && !(go == null) && m_mode == MapMode.Large)
		{
			go.SetActive(value: true);
		}
	}

	public void OnPinTextEntered(string t)
	{
		string text = m_nameInput.text;
		if (text.Length > 0 && m_namePin != null)
		{
			text = text.Replace('$', ' ');
			text = text.Replace('<', ' ');
			text = text.Replace('>', ' ');
			m_namePin.m_name = text;
			if (!string.IsNullOrEmpty(text) && m_namePin.m_NamePinData == null)
			{
				m_namePin.m_NamePinData = new PinNameData(m_namePin);
				if (m_namePin.m_NamePinData.PinNameGameObject == null)
				{
					CreateMapNamePin(m_namePin, m_pinNameRootLarge);
				}
			}
		}
		HidePinTextInput(delayTextInput: true);
	}

	private void HidePinTextInput(bool delayTextInput = false)
	{
		GuiInputField nameInput = m_nameInput;
		nameInput.VirtualKeyboardStateChange = (Action<VirtualKeyboardState>)Delegate.Remove(nameInput.VirtualKeyboardStateChange, new Action<VirtualKeyboardState>(VirtualKeyboardStateChange));
		m_namePin = null;
		m_nameInput.text = "";
		m_nameInput.gameObject.SetActive(value: false);
		if (delayTextInput)
		{
			m_delayTextInput = 0.1f;
			m_wasFocused = true;
		}
		else
		{
			m_wasFocused = false;
		}
	}

	private void UpdateMap(Player player, float dt, bool takeInput)
	{
		if (takeInput)
		{
			if (m_mode == MapMode.Large)
			{
				float num = 0f;
				num += ZInput.GetMouseScrollWheel() * m_largeZoom * 2f;
				if (ZInput.GetButton("JoyButtonX") && inputDelay <= 0f)
				{
					Vector3 viewCenterWorldPoint = GetViewCenterWorldPoint();
					Chat.instance.SendPing(viewCenterWorldPoint);
				}
				if (ZInput.GetButton("JoyLTrigger") && !m_nameInput.gameObject.activeSelf)
				{
					num -= m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButton("JoyRTrigger") && !m_nameInput.gameObject.activeSelf)
				{
					num += m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButtonDown("JoyDPadUp"))
				{
					PinType pinType = PinType.None;
					foreach (KeyValuePair<PinType, Image> selectedIcon in m_selectedIcons)
					{
						if (selectedIcon.Key == m_selectedType && pinType != PinType.None)
						{
							SelectIcon(pinType);
							break;
						}
						pinType = selectedIcon.Key;
					}
				}
				else if (ZInput.GetButtonDown("JoyDPadDown"))
				{
					bool flag = false;
					foreach (KeyValuePair<PinType, Image> selectedIcon2 in m_selectedIcons)
					{
						if (flag)
						{
							SelectIcon(selectedIcon2.Key);
							break;
						}
						if (selectedIcon2.Key == m_selectedType)
						{
							flag = true;
						}
					}
				}
				if (ZInput.GetButtonDown("JoyDPadRight"))
				{
					ToggleIconFilter(m_selectedType);
				}
				if (ZInput.GetButtonUp("JoyButtonA") && !InTextInput() && inputDelay <= 0f)
				{
					ShowPinNameInput(ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2)));
				}
				if (ZInput.GetButtonDown("JoyTabRight"))
				{
					Vector3 pos = ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2));
					RemovePin(pos, m_removeRadius * (m_largeZoom * 2f));
					HidePinTextInput();
				}
				if (ZInput.GetButtonDown("JoyTabLeft"))
				{
					Vector3 pos2 = ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2));
					PinData closestPin = GetClosestPin(pos2, m_removeRadius * (m_largeZoom * 2f));
					if (closestPin != null)
					{
						if (closestPin.m_ownerID != 0L)
						{
							closestPin.m_ownerID = 0L;
						}
						else
						{
							closestPin.m_checked = !closestPin.m_checked;
						}
					}
					HidePinTextInput();
				}
				if (ZInput.GetButtonDown("MapZoomOut") && !InTextInput() && !m_nameInput.gameObject.activeSelf)
				{
					num -= m_largeZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn") && !InTextInput() && !m_nameInput.gameObject.activeSelf)
				{
					num += m_largeZoom * 0.5f;
				}
				if (!InTextInput())
				{
					m_largeZoom = Mathf.Clamp(m_largeZoom - num, m_minZoom, m_maxZoom);
				}
			}
			else
			{
				float num2 = 0f;
				if (ZInput.GetButtonDown("MapZoomOut") && !m_nameInput.gameObject.activeSelf)
				{
					num2 -= m_smallZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn") && !m_nameInput.gameObject.activeSelf)
				{
					num2 += m_smallZoom * 0.5f;
				}
				if (ZInput.GetButton("JoyAltKeys") && ZInput.GetButtonDown("JoyMapZoomOut") && !m_nameInput.gameObject.activeSelf)
				{
					num2 -= m_smallZoom * 0.5f;
				}
				if (ZInput.GetButton("JoyAltKeys") && ZInput.GetButtonDown("JoyMapZoomIn") && !m_nameInput.gameObject.activeSelf)
				{
					num2 += m_smallZoom * 0.5f;
				}
				if (!InTextInput())
				{
					m_smallZoom = Mathf.Clamp(m_smallZoom - num2, m_minZoom, m_maxZoom);
				}
			}
		}
		if (m_mode == MapMode.Large)
		{
			if (m_leftDownTime != 0f && m_leftDownTime > m_clickDuration && !m_dragView)
			{
				m_dragWorldPos = ScreenToWorldPoint(ZInput.mousePosition);
				m_dragView = true;
				HidePinTextInput();
			}
			if (!m_nameInput.gameObject.activeSelf)
			{
				m_mapOffset.x += ZInput.GetJoyLeftStickX(smooth: true) * dt * 50000f * m_largeZoom * m_gamepadMoveSpeed;
				m_mapOffset.z -= ZInput.GetJoyLeftStickY() * dt * 50000f * m_largeZoom * m_gamepadMoveSpeed;
			}
			if (m_dragView)
			{
				Vector3 vector = ScreenToWorldPoint(ZInput.mousePosition) - m_dragWorldPos;
				m_mapOffset -= vector;
				m_pinUpdateRequired = true;
				CenterMap(player.transform.position + m_mapOffset);
				m_dragWorldPos = ScreenToWorldPoint(ZInput.mousePosition);
			}
			else
			{
				CenterMap(player.transform.position + m_mapOffset);
			}
		}
		else
		{
			CenterMap(player.transform.position);
		}
		UpdateWindMarker();
		UpdatePlayerMarker(player, Utils.GetMainCamera().transform.rotation);
	}

	public void SetMapMode(MapMode mode)
	{
		if (Game.m_noMap)
		{
			mode = MapMode.None;
		}
		if (mode == m_mode)
		{
			return;
		}
		m_pinUpdateRequired = true;
		m_mode = mode;
		switch (mode)
		{
		case MapMode.None:
			m_largeRoot.SetActive(value: false);
			m_smallRoot.SetActive(value: false);
			HidePinTextInput();
			break;
		case MapMode.Small:
			m_largeRoot.SetActive(value: false);
			m_smallRoot.SetActive(value: true);
			HidePinTextInput();
			break;
		case MapMode.Large:
		{
			m_largeRoot.SetActive(value: true);
			m_smallRoot.SetActive(value: false);
			bool active = PlayerPrefs.GetInt("KeyHints", 1) == 1;
			foreach (GameObject hint in m_hints)
			{
				hint.SetActive(active);
			}
			m_dragView = false;
			m_mapOffset = Vector3.zero;
			m_namePin = null;
			break;
		}
		}
	}

	private void CenterMap(Vector3 centerPoint)
	{
		WorldToMapPoint(centerPoint, out var mx, out var my);
		Rect uvRect = m_mapImageSmall.uvRect;
		uvRect.width = m_smallZoom;
		uvRect.height = m_smallZoom;
		uvRect.center = new Vector2(mx, my);
		m_mapImageSmall.uvRect = uvRect;
		RectTransform rectTransform = m_mapImageLarge.transform as RectTransform;
		float num = rectTransform.rect.width / rectTransform.rect.height;
		Rect uvRect2 = m_mapImageSmall.uvRect;
		uvRect2.width = m_largeZoom * num;
		uvRect2.height = m_largeZoom;
		uvRect2.center = new Vector2(mx, my);
		m_mapImageLarge.uvRect = uvRect2;
		if (m_mode == MapMode.Large)
		{
			m_mapLargeShader.SetFloat("_zoom", m_largeZoom);
			m_mapLargeShader.SetFloat("_pixelSize", 200f / m_largeZoom);
			m_mapLargeShader.SetVector("_mapCenter", centerPoint);
		}
		else
		{
			m_mapSmallShader.SetFloat("_zoom", m_smallZoom);
			m_mapSmallShader.SetFloat("_pixelSize", 200f / m_smallZoom);
			m_mapSmallShader.SetVector("_mapCenter", centerPoint);
		}
		if (UpdatedMap(centerPoint))
		{
			m_pinUpdateRequired = true;
		}
	}

	private bool UpdatedMap(Vector3 centerPoint)
	{
		float num = m_previousMapCenter.magnitude - centerPoint.magnitude;
		if (num > 0.01f || num < -0.01f)
		{
			m_previousMapCenter = centerPoint;
			return true;
		}
		if (m_mode == MapMode.Large)
		{
			if (m_previousLargeZoom != m_largeZoom)
			{
				m_previousLargeZoom = m_largeZoom;
				return true;
			}
		}
		else if (m_previousSmallZoom != m_smallZoom)
		{
			m_previousSmallZoom = m_smallZoom;
			return true;
		}
		return false;
	}

	private void UpdateDynamicPins(float dt)
	{
		UpdateProfilePins();
		UpdateShoutPins();
		UpdatePingPins();
		UpdatePlayerPins(dt);
		UpdateLocationPins(dt);
		UpdateEventPin(dt);
	}

	private void UpdateProfilePins()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		playerProfile.HaveDeathPoint();
		if (m_deathPin != null)
		{
			RemovePin(m_deathPin);
			m_deathPin = null;
		}
		if (playerProfile.HaveCustomSpawnPoint())
		{
			if (m_spawnPointPin == null)
			{
				m_spawnPointPin = AddPin(playerProfile.GetCustomSpawnPoint(), PinType.Bed, "", save: false, isChecked: false, 0L);
			}
			m_spawnPointPin.m_pos = playerProfile.GetCustomSpawnPoint();
		}
		else if (m_spawnPointPin != null)
		{
			RemovePin(m_spawnPointPin);
			m_spawnPointPin = null;
		}
	}

	private void UpdateEventPin(float dt)
	{
		if (Time.time - m_updateEventTime < 1f)
		{
			return;
		}
		m_updateEventTime = Time.time;
		RandomEvent currentRandomEvent = RandEventSystem.instance.GetCurrentRandomEvent();
		if (currentRandomEvent != null)
		{
			if (m_randEventAreaPin == null)
			{
				m_randEventAreaPin = AddPin(currentRandomEvent.m_pos, PinType.EventArea, "", save: false, isChecked: false, 0L);
				m_randEventAreaPin.m_worldSize = currentRandomEvent.m_eventRange * 2f;
				m_randEventAreaPin.m_worldSize *= 0.9f;
			}
			if (m_randEventPin == null)
			{
				m_randEventPin = AddPin(currentRandomEvent.m_pos, PinType.RandomEvent, "", save: false, isChecked: false, 0L);
				m_randEventPin.m_animate = true;
				m_randEventPin.m_doubleSize = true;
			}
			m_randEventAreaPin.m_pos = currentRandomEvent.m_pos;
			m_randEventPin.m_pos = currentRandomEvent.m_pos;
			m_randEventPin.m_name = Localization.instance.Localize(currentRandomEvent.GetHudText());
		}
		else
		{
			if (m_randEventPin != null)
			{
				RemovePin(m_randEventPin);
				m_randEventPin = null;
			}
			if (m_randEventAreaPin != null)
			{
				RemovePin(m_randEventAreaPin);
				m_randEventAreaPin = null;
			}
		}
	}

	private void UpdateLocationPins(float dt)
	{
		m_updateLocationsTimer -= dt;
		if (!(m_updateLocationsTimer <= 0f))
		{
			return;
		}
		m_updateLocationsTimer = 5f;
		Dictionary<Vector3, string> dictionary = new Dictionary<Vector3, string>();
		ZoneSystem.instance.GetLocationIcons(dictionary);
		bool flag = false;
		while (!flag)
		{
			flag = true;
			foreach (KeyValuePair<Vector3, PinData> locationPin in m_locationPins)
			{
				if (!dictionary.ContainsKey(locationPin.Key))
				{
					ZLog.DevLog("Minimap: Removing location " + locationPin.Value.m_name);
					RemovePin(locationPin.Value);
					m_locationPins.Remove(locationPin.Key);
					flag = false;
					break;
				}
			}
		}
		foreach (KeyValuePair<Vector3, string> item in dictionary)
		{
			if (!m_locationPins.ContainsKey(item.Key))
			{
				Sprite locationIcon = GetLocationIcon(item.Value);
				if ((bool)locationIcon)
				{
					PinData pinData = AddPin(item.Key, PinType.None, "", save: false, isChecked: false, 0L);
					pinData.m_icon = locationIcon;
					pinData.m_doubleSize = true;
					m_locationPins.Add(item.Key, pinData);
					ZLog.Log("Minimap: Adding unique location " + item.Key.ToString());
				}
			}
		}
	}

	private Sprite GetLocationIcon(string name)
	{
		foreach (LocationSpriteData locationIcon in m_locationIcons)
		{
			if (locationIcon.m_name == name)
			{
				return locationIcon.m_icon;
			}
		}
		return null;
	}

	private void UpdatePlayerPins(float dt)
	{
		m_tempPlayerInfo.Clear();
		ZNet.instance.GetOtherPublicPlayers(m_tempPlayerInfo);
		if (m_playerPins.Count != m_tempPlayerInfo.Count)
		{
			foreach (PinData playerPin in m_playerPins)
			{
				RemovePin(playerPin);
			}
			m_playerPins.Clear();
			foreach (ZNet.PlayerInfo item2 in m_tempPlayerInfo)
			{
				_ = item2;
				PinData item = AddPin(Vector3.zero, PinType.Player, "", save: false, isChecked: false, 0L);
				m_playerPins.Add(item);
			}
		}
		for (int i = 0; i < m_tempPlayerInfo.Count; i++)
		{
			PinData pinData = m_playerPins[i];
			ZNet.PlayerInfo playerInfo = m_tempPlayerInfo[i];
			if (pinData.m_name == playerInfo.m_name)
			{
				Vector3 vector = Vector3.MoveTowards(pinData.m_pos, playerInfo.m_position, 200f * dt);
				if (vector != pinData.m_pos)
				{
					m_pinUpdateRequired = true;
				}
				pinData.m_pos = vector;
				continue;
			}
			pinData.m_name = CensorShittyWords.FilterUGC(playerInfo.m_name, UGCType.CharacterName, null, playerInfo.m_characterID.UserID);
			if (playerInfo.m_position != pinData.m_pos)
			{
				m_pinUpdateRequired = true;
			}
			pinData.m_pos = playerInfo.m_position;
			if (pinData.m_NamePinData == null)
			{
				pinData.m_NamePinData = new PinNameData(pinData);
				CreateMapNamePin(pinData, m_pinNameRootLarge);
			}
		}
	}

	private void UpdatePingPins()
	{
		m_tempShouts.Clear();
		Chat.instance.GetPingWorldTexts(m_tempShouts);
		if (m_pingPins.Count != m_tempShouts.Count)
		{
			foreach (PinData pingPin in m_pingPins)
			{
				RemovePin(pingPin);
			}
			m_pingPins.Clear();
			foreach (Chat.WorldTextInstance tempShout in m_tempShouts)
			{
				PinData pinData = AddPin(Vector3.zero, PinType.Ping, tempShout.m_name + ": " + tempShout.m_text, save: false, isChecked: false, 0L);
				pinData.m_doubleSize = true;
				pinData.m_animate = true;
				m_pingPins.Add(pinData);
			}
		}
		if (m_pingPins.Count > 0)
		{
			m_pinUpdateRequired = true;
		}
		for (int i = 0; i < m_tempShouts.Count; i++)
		{
			PinData pinData2 = m_pingPins[i];
			Chat.WorldTextInstance worldTextInstance = m_tempShouts[i];
			pinData2.m_pos = worldTextInstance.m_position;
			pinData2.m_name = worldTextInstance.m_name + ": " + worldTextInstance.m_text;
		}
	}

	private void UpdateShoutPins()
	{
		m_tempShouts.Clear();
		Chat.instance.GetShoutWorldTexts(m_tempShouts);
		if (m_shoutPins.Count != m_tempShouts.Count)
		{
			foreach (PinData shoutPin in m_shoutPins)
			{
				RemovePin(shoutPin);
			}
			m_shoutPins.Clear();
			foreach (Chat.WorldTextInstance tempShout in m_tempShouts)
			{
				PinData pinData = AddPin(Vector3.zero, PinType.Shout, tempShout.m_name + ": " + tempShout.m_text, save: false, isChecked: false, 0L);
				pinData.m_doubleSize = true;
				pinData.m_animate = true;
				m_shoutPins.Add(pinData);
			}
		}
		if (m_shoutPins.Count > 0)
		{
			m_pinUpdateRequired = true;
		}
		for (int i = 0; i < m_tempShouts.Count; i++)
		{
			PinData pinData2 = m_shoutPins[i];
			Chat.WorldTextInstance worldTextInstance = m_tempShouts[i];
			pinData2.m_pos = worldTextInstance.m_position;
			pinData2.m_name = worldTextInstance.m_name + ": " + worldTextInstance.m_text;
		}
	}

	private void UpdatePins()
	{
		RawImage rawImage = ((m_mode == MapMode.Large) ? m_mapImageLarge : m_mapImageSmall);
		Rect uvRect = rawImage.uvRect;
		Rect rect = rawImage.rectTransform.rect;
		float num = ((m_mode == MapMode.Large) ? m_pinSizeLarge : m_pinSizeSmall);
		if (m_mode != MapMode.Large)
		{
			_ = m_smallZoom;
		}
		else
		{
			_ = m_largeZoom;
		}
		Color color = new Color(0.7f, 0.7f, 0.7f, 0.8f * m_sharedMapDataFade);
		foreach (PinData pin in m_pins)
		{
			RectTransform rectTransform = null;
			RectTransform rectTransform2 = null;
			rectTransform = ((m_mode == MapMode.Large) ? m_pinRootLarge : m_pinRootSmall);
			rectTransform2 = ((m_mode == MapMode.Large) ? m_pinNameRootLarge : m_pinNameRootSmall);
			if (!IsPointVisible(pin.m_pos, rawImage) || !m_visibleIconTypes[(int)pin.m_type] || (!(m_sharedMapDataFade > 0f) && pin.m_ownerID != 0L))
			{
				DestroyPinMarker(pin);
				continue;
			}
			if (pin.m_uiElement == null || pin.m_uiElement.parent != rectTransform)
			{
				DestroyPinMarker(pin);
				GameObject gameObject = UnityEngine.Object.Instantiate(m_pinPrefab, rectTransform);
				pin.m_iconElement = gameObject.GetComponent<Image>();
				pin.m_iconElement.sprite = pin.m_icon;
				pin.m_uiElement = gameObject.transform as RectTransform;
				float size = (pin.m_doubleSize ? (num * 2f) : num);
				pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
				pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
				pin.m_checkedElement = gameObject.transform.Find("Checked").gameObject;
			}
			if (pin.m_NamePinData != null && pin.m_NamePinData.PinNameGameObject == null)
			{
				CreateMapNamePin(pin, rectTransform2);
			}
			if (pin.m_ownerID != 0L && m_sharedMapHint != null)
			{
				m_sharedMapHint.gameObject.SetActive(value: true);
			}
			Color color2 = ((pin.m_ownerID != 0L) ? color : Color.white);
			pin.m_iconElement.color = color2;
			if (pin.m_NamePinData != null && pin.m_NamePinData.PinNameText.color != color2)
			{
				pin.m_NamePinData.PinNameText.color = color2;
			}
			WorldToMapPoint(pin.m_pos, out var mx, out var my);
			Vector2 anchoredPosition = MapPointToLocalGuiPos(mx, my, uvRect, rect);
			pin.m_uiElement.anchoredPosition = anchoredPosition;
			if (pin.m_NamePinData != null)
			{
				pin.m_NamePinData.PinNameRectTransform.anchoredPosition = anchoredPosition;
			}
			if (pin.m_animate)
			{
				float num2 = (pin.m_doubleSize ? (num * 2f) : num);
				num2 *= 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
				pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num2);
				pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
			}
			if (pin.m_worldSize > 0f)
			{
				Vector2 size2 = new Vector2(pin.m_worldSize / m_pixelSize / (float)m_textureSize, pin.m_worldSize / m_pixelSize / (float)m_textureSize);
				Vector2 vector = MapSizeToLocalGuiSize(size2, rawImage);
				pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vector.x);
				pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, vector.y);
			}
			if (pin.m_checkedElement.activeInHierarchy != pin.m_checked)
			{
				pin.m_checkedElement.SetActive(pin.m_checked);
			}
			if (pin.m_name.Length > 0 && m_mode == MapMode.Large && m_largeZoom < m_showNamesZoom && pin.m_NamePinData != null)
			{
				pin.m_NamePinData.PinNameGameObject.SetActive(value: true);
			}
			else if (pin.m_NamePinData != null)
			{
				pin.m_NamePinData.PinNameGameObject.SetActive(value: false);
			}
		}
	}

	private void DestroyPinMarker(PinData pin)
	{
		if (pin.m_uiElement != null)
		{
			UnityEngine.Object.Destroy(pin.m_uiElement.gameObject);
			pin.m_uiElement = null;
		}
		if (pin.m_NamePinData != null)
		{
			pin.m_NamePinData.DestroyMapMarker();
		}
	}

	private void UpdateWindMarker()
	{
		Quaternion quaternion = Quaternion.LookRotation(EnvMan.instance.GetWindDir());
		m_windMarker.rotation = Quaternion.Euler(0f, 0f, 0f - quaternion.eulerAngles.y);
	}

	private void UpdatePlayerMarker(Player player, Quaternion playerRot)
	{
		Vector3 position = player.transform.position;
		Vector3 eulerAngles = playerRot.eulerAngles;
		m_smallMarker.rotation = Quaternion.Euler(0f, 0f, 0f - eulerAngles.y);
		if (m_mode == MapMode.Large && IsPointVisible(position, m_mapImageLarge))
		{
			m_largeMarker.gameObject.SetActive(value: true);
			m_largeMarker.rotation = m_smallMarker.rotation;
			WorldToMapPoint(position, out var mx, out var my);
			Vector2 anchoredPosition = MapPointToLocalGuiPos(mx, my, m_mapImageLarge);
			m_largeMarker.anchoredPosition = anchoredPosition;
		}
		else
		{
			m_largeMarker.gameObject.SetActive(value: false);
		}
		Ship controlledShip = player.GetControlledShip();
		if ((bool)controlledShip)
		{
			m_smallShipMarker.gameObject.SetActive(value: true);
			Vector3 eulerAngles2 = controlledShip.transform.rotation.eulerAngles;
			m_smallShipMarker.rotation = Quaternion.Euler(0f, 0f, 0f - eulerAngles2.y);
			if (m_mode == MapMode.Large)
			{
				m_largeShipMarker.gameObject.SetActive(value: true);
				Vector3 position2 = controlledShip.transform.position;
				WorldToMapPoint(position2, out var mx2, out var my2);
				Vector2 anchoredPosition2 = MapPointToLocalGuiPos(mx2, my2, m_mapImageLarge);
				m_largeShipMarker.anchoredPosition = anchoredPosition2;
				m_largeShipMarker.rotation = m_smallShipMarker.rotation;
			}
		}
		else
		{
			m_smallShipMarker.gameObject.SetActive(value: false);
			m_largeShipMarker.gameObject.SetActive(value: false);
		}
	}

	private Vector2 MapPointToLocalGuiPos(float mx, float my, RawImage img)
	{
		return MapPointToLocalGuiPos(mx, my, img.uvRect, img.rectTransform.rect);
	}

	private Vector2 MapPointToLocalGuiPos(float mx, float my, Rect uvRect, Rect transformRect)
	{
		Vector2 result = default(Vector2);
		result.x = (mx - uvRect.xMin) / uvRect.width;
		result.y = (my - uvRect.yMin) / uvRect.height;
		result.x *= transformRect.width;
		result.y *= transformRect.height;
		return result;
	}

	private Vector2 MapSizeToLocalGuiSize(Vector2 size, RawImage img)
	{
		size.x /= img.uvRect.width;
		size.y /= img.uvRect.height;
		return new Vector2(size.x * img.rectTransform.rect.width, size.y * img.rectTransform.rect.height);
	}

	private bool IsPointVisible(Vector3 p, RawImage map)
	{
		WorldToMapPoint(p, out var mx, out var my);
		if (mx > map.uvRect.xMin && mx < map.uvRect.xMax && my > map.uvRect.yMin)
		{
			return my < map.uvRect.yMax;
		}
		return false;
	}

	public void ExploreAll()
	{
		for (int i = 0; i < m_textureSize; i++)
		{
			for (int j = 0; j < m_textureSize; j++)
			{
				Explore(j, i);
			}
		}
		m_fogTexture.Apply();
	}

	private void WorldToMapPoint(Vector3 p, out float mx, out float my)
	{
		int num = m_textureSize / 2;
		mx = p.x / m_pixelSize + (float)num;
		my = p.z / m_pixelSize + (float)num;
		mx /= m_textureSize;
		my /= m_textureSize;
	}

	private Vector3 MapPointToWorld(float mx, float my)
	{
		int num = m_textureSize / 2;
		mx *= (float)m_textureSize;
		my *= (float)m_textureSize;
		mx -= (float)num;
		my -= (float)num;
		mx *= m_pixelSize;
		my *= m_pixelSize;
		return new Vector3(mx, 0f, my);
	}

	private void WorldToPixel(Vector3 p, out int px, out int py)
	{
		int num = m_textureSize / 2;
		px = Mathf.RoundToInt(p.x / m_pixelSize + (float)num);
		py = Mathf.RoundToInt(p.z / m_pixelSize + (float)num);
	}

	private void UpdateExplore(float dt, Player player)
	{
		m_exploreTimer += Time.deltaTime;
		if (m_exploreTimer > m_exploreInterval)
		{
			m_exploreTimer = 0f;
			Explore(player.transform.position, m_exploreRadius);
		}
	}

	private void Explore(Vector3 p, float radius)
	{
		int num = (int)Mathf.Ceil(radius / m_pixelSize);
		bool flag = false;
		WorldToPixel(p, out var px, out var py);
		for (int i = py - num; i <= py + num; i++)
		{
			for (int j = px - num; j <= px + num; j++)
			{
				if (j >= 0 && i >= 0 && j < m_textureSize && i < m_textureSize && !(new Vector2(j - px, i - py).magnitude > (float)num) && Explore(j, i))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			m_fogTexture.Apply();
		}
	}

	private bool Explore(int x, int y)
	{
		if (m_explored[y * m_textureSize + x])
		{
			return false;
		}
		Color pixel = m_fogTexture.GetPixel(x, y);
		pixel.r = 0f;
		m_fogTexture.SetPixel(x, y, pixel);
		m_explored[y * m_textureSize + x] = true;
		return true;
	}

	private void ResetAndExplore(byte[] explored, byte[] exploredOthers)
	{
		m_sharedMapHint.gameObject.SetActive(value: false);
		int num = explored.Length;
		Color[] pixels = m_fogTexture.GetPixels();
		if (num != pixels.Length || num != exploredOthers.Length)
		{
			ZLog.LogError("Dimension mismatch for exploring mipmap");
			return;
		}
		for (int i = 0; i < num; i++)
		{
			pixels[i] = Color.white;
			if (explored[i] != 0)
			{
				pixels[i].r = 0f;
				m_explored[i] = true;
			}
			else
			{
				m_explored[i] = false;
			}
			if (exploredOthers[i] != 0)
			{
				pixels[i].g = 0f;
				m_exploredOthers[i] = true;
			}
			else
			{
				m_exploredOthers[i] = false;
			}
		}
		m_fogTexture.SetPixels(pixels);
	}

	private bool ExploreOthers(int x, int y)
	{
		if (m_exploredOthers[y * m_textureSize + x])
		{
			return false;
		}
		Color pixel = m_fogTexture.GetPixel(x, y);
		pixel.g = 0f;
		m_fogTexture.SetPixel(x, y, pixel);
		m_exploredOthers[y * m_textureSize + x] = true;
		if (m_sharedMapHint != null)
		{
			m_sharedMapHint.gameObject.SetActive(value: true);
		}
		return true;
	}

	private bool IsExplored(Vector3 worldPos)
	{
		WorldToPixel(worldPos, out var px, out var py);
		if (px < 0 || px >= m_textureSize || py < 0 || py >= m_textureSize)
		{
			return false;
		}
		if (!m_explored[py * m_textureSize + px])
		{
			return m_exploredOthers[py * m_textureSize + px];
		}
		return true;
	}

	private float GetHeight(int x, int y)
	{
		return m_heightTexture.GetPixel(x, y).r;
	}

	private void GenerateWorldMap()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		DeleteMapTextureData(ZNet.World.m_name);
		int num = m_textureSize / 2;
		float num2 = m_pixelSize / 2f;
		Color32[] array = new Color32[m_textureSize * m_textureSize];
		Color32[] array2 = new Color32[m_textureSize * m_textureSize];
		Color[] array3 = new Color[m_textureSize * m_textureSize];
		Color32[] array4 = new Color32[m_textureSize * m_textureSize];
		float num3 = 127.5f;
		for (int i = 0; i < m_textureSize; i++)
		{
			for (int j = 0; j < m_textureSize; j++)
			{
				float wx = (float)(j - num) * m_pixelSize + num2;
				float wy = (float)(i - num) * m_pixelSize + num2;
				Heightmap.Biome biome = WorldGenerator.instance.GetBiome(wx, wy);
				Color mask;
				float biomeHeight = WorldGenerator.instance.GetBiomeHeight(biome, wx, wy, out mask);
				array[i * m_textureSize + j] = GetPixelColor(biome);
				array2[i * m_textureSize + j] = GetMaskColor(wx, wy, biomeHeight, biome);
				array3[i * m_textureSize + j].r = biomeHeight;
				int num4 = Mathf.Clamp((int)(biomeHeight * num3), 0, 65025);
				byte r = (byte)(num4 >> 8);
				byte g = (byte)((uint)num4 & 0xFFu);
				array4[i * m_textureSize + j] = new Color32(r, g, 0, byte.MaxValue);
			}
		}
		m_forestMaskTexture.SetPixels32(array2);
		m_forestMaskTexture.Apply();
		m_mapTexture.SetPixels32(array);
		m_mapTexture.Apply();
		m_heightTexture.SetPixels(array3);
		m_heightTexture.Apply();
		Texture2D texture2D = new Texture2D(m_textureSize, m_textureSize);
		texture2D.SetPixels32(array4);
		texture2D.Apply();
		ZLog.Log("Generating new world minimap done [" + stopwatch.ElapsedMilliseconds + "ms]");
		SaveMapTextureDataToDisk(m_forestMaskTexture, m_mapTexture, texture2D);
	}

	public static void DeleteMapTextureData(string worldName)
	{
		string rootPath = World.GetWorldSavePath(FileHelpers.FileSource.Local) + "/" + worldName;
		string completeTexturePath = GetCompleteTexturePath(rootPath, "forestMaskTexCache");
		string completeTexturePath2 = GetCompleteTexturePath(rootPath, "mapTexCache");
		string completeTexturePath3 = GetCompleteTexturePath(rootPath, "heightTexCache");
		if (File.Exists(completeTexturePath))
		{
			File.Delete(completeTexturePath);
		}
		if (File.Exists(completeTexturePath2))
		{
			File.Delete(completeTexturePath2);
		}
		if (File.Exists(completeTexturePath3))
		{
			File.Delete(completeTexturePath3);
		}
	}

	private static string GetCompleteTexturePath(string rootPath, string maskTextureName)
	{
		return rootPath + "_" + maskTextureName;
	}

	private void SaveMapTextureDataToDisk(Texture2D forestMaskTexture, Texture2D mapTexture, Texture2D heightTexture)
	{
		if (!string.IsNullOrEmpty(m_forestMaskTexturePath))
		{
			File.WriteAllBytes(m_forestMaskTexturePath, forestMaskTexture.EncodeToPNG());
			File.WriteAllBytes(m_mapTexturePath, mapTexture.EncodeToPNG());
			File.WriteAllBytes(m_heightTexturePath, heightTexture.EncodeToPNG());
		}
	}

	private Color GetMaskColor(float wx, float wy, float height, Heightmap.Biome biome)
	{
		Color result = new Color(0f, 0f, 0f, 0f);
		if (height < 30f)
		{
			result.b = Mathf.Clamp01(WorldGenerator.GetAshlandsOceanGradient(wx, wy));
			return result;
		}
		switch (biome)
		{
		case Heightmap.Biome.Meadows:
			result.r = (WorldGenerator.InForest(new Vector3(wx, 0f, wy)) ? 1 : 0);
			break;
		case Heightmap.Biome.Plains:
			result.r = ((WorldGenerator.GetForestFactor(new Vector3(wx, 0f, wy)) < 0.8f) ? 1 : 0);
			break;
		case Heightmap.Biome.BlackForest:
			result.r = 1f;
			break;
		case Heightmap.Biome.Mistlands:
		{
			float forestFactor = WorldGenerator.GetForestFactor(new Vector3(wx, 0f, wy));
			result.g = 1f - Utils.SmoothStep(1.1f, 1.3f, forestFactor);
			break;
		}
		case Heightmap.Biome.AshLands:
		{
			WorldGenerator.instance.GetAshlandsHeight(wx, wy, out var mask, cheap: true);
			result.b = mask.a;
			break;
		}
		}
		return result;
	}

	private Color GetPixelColor(Heightmap.Biome biome)
	{
		return biome switch
		{
			Heightmap.Biome.Meadows => m_meadowsColor, 
			Heightmap.Biome.AshLands => m_ashlandsColor, 
			Heightmap.Biome.BlackForest => m_blackforestColor, 
			Heightmap.Biome.DeepNorth => m_deepnorthColor, 
			Heightmap.Biome.Plains => m_heathColor, 
			Heightmap.Biome.Swamp => m_swampColor, 
			Heightmap.Biome.Mountain => m_mountainColor, 
			Heightmap.Biome.Mistlands => m_mistlandsColor, 
			Heightmap.Biome.Ocean => Color.white, 
			_ => Color.white, 
		};
	}

	private void LoadMapData()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		if (playerProfile.GetMapData() != null)
		{
			SetMapData(playerProfile.GetMapData());
		}
	}

	public void SaveMapData()
	{
		Game.instance.GetPlayerProfile().SetMapData(GetMapData());
	}

	private byte[] GetMapData()
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(MAPVERSION);
		ZPackage zPackage2 = new ZPackage();
		zPackage2.Write(m_textureSize);
		for (int i = 0; i < m_explored.Length; i++)
		{
			zPackage2.Write(m_explored[i]);
		}
		for (int j = 0; j < m_explored.Length; j++)
		{
			zPackage2.Write(m_exploredOthers[j]);
		}
		int num = 0;
		foreach (PinData pin in m_pins)
		{
			if (pin.m_save)
			{
				num++;
			}
		}
		zPackage2.Write(num);
		foreach (PinData pin2 in m_pins)
		{
			if (pin2.m_save)
			{
				zPackage2.Write(pin2.m_name);
				zPackage2.Write(pin2.m_pos);
				zPackage2.Write((int)pin2.m_type);
				zPackage2.Write(pin2.m_checked);
				zPackage2.Write(pin2.m_ownerID);
				zPackage2.Write(pin2.m_author);
			}
		}
		zPackage2.Write(ZNet.instance.IsReferencePositionPublic());
		int num2 = zPackage2.Size();
		zPackage.WriteCompressed(zPackage2);
		ZLog.Log("Minimap: compressed mapData " + num2 + " => " + zPackage.Size() + " bytes");
		return zPackage.GetArray();
	}

	private void SetMapData(byte[] data)
	{
		ZPackage zPackage = new ZPackage(data);
		int num = zPackage.ReadInt();
		if (num >= 7)
		{
			int num2 = zPackage.Size();
			zPackage = zPackage.ReadCompressedPackage();
			ZLog.Log("Minimap: unpacking compressed mapData " + num2 + " => " + zPackage.Size() + " bytes");
		}
		int num3 = zPackage.ReadInt();
		if (m_textureSize != num3)
		{
			ZLog.LogWarning("Missmatching mapsize " + m_mapTexture?.ToString() + " vs " + num3);
			return;
		}
		if (num >= 5)
		{
			byte[] explored = zPackage.ReadByteArray(m_explored.Length);
			byte[] exploredOthers = zPackage.ReadByteArray(m_exploredOthers.Length);
			ResetAndExplore(explored, exploredOthers);
		}
		else
		{
			Reset();
			for (int i = 0; i < m_explored.Length; i++)
			{
				if (zPackage.ReadBool())
				{
					int x = i % num3;
					int y = i / num3;
					Explore(x, y);
				}
			}
		}
		if (num >= 2)
		{
			int num4 = zPackage.ReadInt();
			ClearPins();
			for (int j = 0; j < num4; j++)
			{
				string text = zPackage.ReadString();
				Vector3 pos = zPackage.ReadVector3();
				PinType type = (PinType)zPackage.ReadInt();
				bool isChecked = num >= 3 && zPackage.ReadBool();
				long ownerID = ((num >= 6) ? zPackage.ReadLong() : 0);
				string author = ((num >= 8) ? zPackage.ReadString() : "");
				AddPin(pos, type, text, save: true, isChecked, ownerID, author);
			}
		}
		if (num >= 4)
		{
			bool publicReferencePosition = zPackage.ReadBool();
			ZNet.instance.SetPublicReferencePosition(publicReferencePosition);
		}
		m_fogTexture.Apply();
	}

	public bool RemovePin(Vector3 pos, float radius)
	{
		PinData closestPin = GetClosestPin(pos, radius);
		if (closestPin != null)
		{
			RemovePin(closestPin);
			return true;
		}
		return false;
	}

	private bool HavePinInRange(Vector3 pos, float radius)
	{
		foreach (PinData pin in m_pins)
		{
			if (pin.m_save && Utils.DistanceXZ(pos, pin.m_pos) < radius)
			{
				return true;
			}
		}
		return false;
	}

	private PinData GetClosestPin(Vector3 pos, float radius, bool mustBeVisible = true)
	{
		PinData pinData = null;
		float num = 999999f;
		foreach (PinData pin in m_pins)
		{
			if (pin.m_save && !((!pin.m_uiElement || !pin.m_uiElement.gameObject.activeInHierarchy) && mustBeVisible))
			{
				float num2 = Utils.DistanceXZ(pos, pin.m_pos);
				if (num2 < radius && (num2 < num || pinData == null))
				{
					pinData = pin;
					num = num2;
				}
			}
		}
		return pinData;
	}

	public void RemovePin(PinData pin)
	{
		m_pinUpdateRequired = true;
		DestroyPinMarker(pin);
		m_pins.Remove(pin);
	}

	public void ShowPointOnMap(Vector3 point)
	{
		inputDelay = 0.5f;
		if (!(Player.m_localPlayer == null))
		{
			SetMapMode(MapMode.Large);
			m_mapOffset = point - Player.m_localPlayer.transform.position;
		}
	}

	public bool DiscoverLocation(Vector3 pos, PinType type, string name, bool showMap)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		if (HaveSimilarPin(pos, type, name, save: true))
		{
			if (showMap)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_pin_exist");
				ShowPointOnMap(pos);
			}
			return false;
		}
		Sprite sprite = GetSprite(type);
		AddPin(pos, type, name, save: true, isChecked: false, 0L);
		if (showMap)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_pin_added: " + name, 0, sprite);
			ShowPointOnMap(pos);
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_pin_added: " + name, 0, sprite);
		}
		return true;
	}

	private bool HaveSimilarPin(Vector3 pos, PinType type, string name, bool save)
	{
		foreach (PinData pin in m_pins)
		{
			if (pin.m_name == name && pin.m_type == type && pin.m_save == save && Utils.DistanceXZ(pos, pin.m_pos) < 1f)
			{
				return true;
			}
		}
		return false;
	}

	public PinData AddPin(Vector3 pos, PinType type, string name, bool save, bool isChecked, long ownerID = 0L, string author = "")
	{
		if ((int)type >= m_visibleIconTypes.Length || type < PinType.Icon0)
		{
			ZLog.LogWarning($"Trying to add invalid pin type: {type}");
			type = PinType.Icon3;
		}
		if (name == null)
		{
			name = "";
		}
		PinData pinData = new PinData();
		pinData.m_type = type;
		pinData.m_name = name;
		pinData.m_pos = pos;
		pinData.m_icon = GetSprite(type);
		pinData.m_save = save;
		pinData.m_checked = isChecked;
		pinData.m_ownerID = ownerID;
		pinData.m_author = author;
		if (!string.IsNullOrEmpty(pinData.m_name))
		{
			pinData.m_NamePinData = new PinNameData(pinData);
		}
		m_pins.Add(pinData);
		if ((int)type < m_visibleIconTypes.Length && !m_visibleIconTypes[(int)type])
		{
			ToggleIconFilter(type);
		}
		m_pinUpdateRequired = true;
		return pinData;
	}

	private Sprite GetSprite(PinType type)
	{
		if (type == PinType.None)
		{
			return null;
		}
		return m_icons.Find((SpriteData x) => x.m_name == type).m_icon;
	}

	private Vector3 GetViewCenterWorldPoint()
	{
		Rect uvRect = m_mapImageLarge.uvRect;
		float mx = uvRect.xMin + 0.5f * uvRect.width;
		float my = uvRect.yMin + 0.5f * uvRect.height;
		return MapPointToWorld(mx, my);
	}

	private Vector3 ScreenToWorldPoint(Vector3 mousePos)
	{
		Vector2 screenPoint = mousePos;
		RectTransform rectTransform = m_mapImageLarge.transform as RectTransform;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out var localPoint))
		{
			Vector2 vector = Rect.PointToNormalized(rectTransform.rect, localPoint);
			Rect uvRect = m_mapImageLarge.uvRect;
			float mx = uvRect.xMin + vector.x * uvRect.width;
			float my = uvRect.yMin + vector.y * uvRect.height;
			return MapPointToWorld(mx, my);
		}
		return Vector3.zero;
	}

	private void OnMapLeftDown(UIInputHandler handler)
	{
		if (Time.time - m_leftClickTime < 0.3f)
		{
			OnMapDblClick();
			m_leftClickTime = 0f;
			m_leftDownTime = 0f;
		}
		else
		{
			m_leftClickTime = Time.time;
			m_leftDownTime = Time.time;
		}
	}

	private void OnMapLeftUp(UIInputHandler handler)
	{
		if (m_leftDownTime != 0f)
		{
			if (Time.time - m_leftDownTime < m_clickDuration)
			{
				OnMapLeftClick();
			}
			m_leftDownTime = 0f;
		}
		m_dragView = false;
	}

	public void OnMapDblClick()
	{
		if (m_selectedType != PinType.Death)
		{
			ShowPinNameInput(ScreenToWorldPoint(ZInput.mousePosition));
		}
	}

	public void OnMapLeftClick()
	{
		ZLog.Log("Left click");
		HidePinTextInput();
		Vector3 pos = ScreenToWorldPoint(ZInput.mousePosition);
		PinData closestPin = GetClosestPin(pos, m_removeRadius * (m_largeZoom * 2f));
		if (closestPin != null)
		{
			if (closestPin.m_ownerID != 0L)
			{
				closestPin.m_ownerID = 0L;
			}
			else
			{
				closestPin.m_checked = !closestPin.m_checked;
			}
		}
		m_pinUpdateRequired = true;
	}

	public void OnMapMiddleClick(UIInputHandler handler)
	{
		HidePinTextInput();
		Vector3 position = ScreenToWorldPoint(ZInput.mousePosition);
		Chat.instance.SendPing(position);
		if (Player.m_debugMode && Console.instance != null && Console.instance.IsCheatsEnabled() && ZInput.GetKey(KeyCode.LeftControl))
		{
			Vector3 vector = new Vector3(position.x, Player.m_localPlayer.transform.position.y, position.z);
			Heightmap.GetHeight(vector, out var height);
			vector.y = Math.Max(0f, height);
			Player.m_localPlayer.TeleportTo(vector, Player.m_localPlayer.transform.rotation, distantTeleport: true);
			instance.SetMapMode(MapMode.Small);
		}
	}

	public void OnMapRightClick(UIInputHandler handler)
	{
		ZLog.Log("Right click");
		HidePinTextInput();
		Vector3 pos = ScreenToWorldPoint(ZInput.mousePosition);
		RemovePin(pos, m_removeRadius * (m_largeZoom * 2f));
		m_namePin = null;
	}

	public void OnPressedIcon0()
	{
		SelectIcon(PinType.Icon0);
	}

	public void OnPressedIcon1()
	{
		SelectIcon(PinType.Icon1);
	}

	public void OnPressedIcon2()
	{
		SelectIcon(PinType.Icon2);
	}

	public void OnPressedIcon3()
	{
		SelectIcon(PinType.Icon3);
	}

	public void OnPressedIcon4()
	{
		SelectIcon(PinType.Icon4);
	}

	public void OnPressedIconDeath()
	{
	}

	public void OnPressedIconBoss()
	{
	}

	public void OnAltPressedIcon0()
	{
		ToggleIconFilter(PinType.Icon0);
	}

	public void OnAltPressedIcon1()
	{
		ToggleIconFilter(PinType.Icon1);
	}

	public void OnAltPressedIcon2()
	{
		ToggleIconFilter(PinType.Icon2);
	}

	public void OnAltPressedIcon3()
	{
		ToggleIconFilter(PinType.Icon3);
	}

	public void OnAltPressedIcon4()
	{
		ToggleIconFilter(PinType.Icon4);
	}

	public void OnAltPressedIconDeath()
	{
		ToggleIconFilter(PinType.Death);
	}

	public void OnAltPressedIconBoss()
	{
		ToggleIconFilter(PinType.Boss);
	}

	public void OnTogglePublicPosition()
	{
		if ((bool)ZNet.instance)
		{
			ZNet.instance.SetPublicReferencePosition(m_publicPosition.isOn);
		}
	}

	public void OnToggleSharedMapData()
	{
		m_showSharedMapData = !m_showSharedMapData;
	}

	private void SelectIcon(PinType type)
	{
		m_selectedType = type;
		m_pinUpdateRequired = true;
		foreach (KeyValuePair<PinType, Image> selectedIcon in m_selectedIcons)
		{
			selectedIcon.Value.enabled = selectedIcon.Key == type;
		}
	}

	private void ToggleIconFilter(PinType type)
	{
		m_visibleIconTypes[(int)type] = !m_visibleIconTypes[(int)type];
		m_pinUpdateRequired = true;
		foreach (KeyValuePair<PinType, Image> selectedIcon in m_selectedIcons)
		{
			selectedIcon.Value.transform.parent.GetComponent<Image>().color = (m_visibleIconTypes[(int)selectedIcon.Key] ? Color.white : Color.gray);
		}
	}

	private void ClearPins()
	{
		foreach (PinData pin in m_pins)
		{
			DestroyPinMarker(pin);
		}
		m_pins.Clear();
		m_deathPin = null;
	}

	private void UpdateBiome(Player player)
	{
		if (m_mode == MapMode.Large)
		{
			Vector3 vector = ScreenToWorldPoint(ZInput.IsMouseActive() ? ZInput.mousePosition : new Vector3(Screen.width / 2, Screen.height / 2));
			if (IsExplored(vector))
			{
				Heightmap.Biome biome = WorldGenerator.instance.GetBiome(vector);
				string text = Localization.instance.Localize("$biome_" + biome.ToString().ToLower());
				m_biomeNameLarge.text = text;
			}
			else
			{
				m_biomeNameLarge.text = "";
			}
			return;
		}
		Heightmap.Biome currentBiome = player.GetCurrentBiome();
		if (currentBiome != m_biome)
		{
			m_biome = currentBiome;
			string text2 = Localization.instance.Localize("$biome_" + currentBiome.ToString().ToLower());
			m_biomeNameSmall.text = text2;
			m_biomeNameLarge.text = text2;
			m_biomeNameSmall.GetComponent<Animator>().SetTrigger("pulse");
		}
	}

	public byte[] GetSharedMapData(byte[] oldMapData)
	{
		List<bool> list = null;
		if (oldMapData != null)
		{
			ZPackage zPackage = new ZPackage(oldMapData);
			int version = zPackage.ReadInt();
			list = ReadExploredArray(zPackage, version);
		}
		ZPackage zPackage2 = new ZPackage();
		zPackage2.Write(3);
		zPackage2.Write(m_explored.Length);
		for (int i = 0; i < m_explored.Length; i++)
		{
			bool flag = m_exploredOthers[i] || m_explored[i];
			if (list != null)
			{
				flag |= list[i];
			}
			zPackage2.Write(flag);
		}
		int num = 0;
		foreach (PinData pin in m_pins)
		{
			if (pin.m_save && pin.m_type != PinType.Death)
			{
				num++;
			}
		}
		long playerID = Player.m_localPlayer.GetPlayerID();
		string networkUserId = PrivilegeManager.GetNetworkUserId();
		zPackage2.Write(num);
		foreach (PinData pin2 in m_pins)
		{
			if (pin2.m_save && pin2.m_type != PinType.Death)
			{
				long num2 = ((pin2.m_ownerID != 0L) ? pin2.m_ownerID : playerID);
				string data = ((string.IsNullOrEmpty(pin2.m_author) && num2 == playerID) ? networkUserId : pin2.m_author);
				zPackage2.Write(num2);
				zPackage2.Write(pin2.m_name);
				zPackage2.Write(pin2.m_pos);
				zPackage2.Write((int)pin2.m_type);
				zPackage2.Write(pin2.m_checked);
				zPackage2.Write(data);
			}
		}
		return zPackage2.GetArray();
	}

	private List<bool> ReadExploredArray(ZPackage pkg, int version)
	{
		int num = pkg.ReadInt();
		if (num != m_explored.Length)
		{
			ZLog.LogWarning("Map exploration array size missmatch:" + num + " VS " + m_explored.Length);
			return null;
		}
		List<bool> list = new List<bool>();
		for (int i = 0; i < m_textureSize; i++)
		{
			for (int j = 0; j < m_textureSize; j++)
			{
				bool item = pkg.ReadBool();
				list.Add(item);
			}
		}
		return list;
	}

	public bool AddSharedMapData(byte[] dataArray)
	{
		ZPackage zPackage = new ZPackage(dataArray);
		int num = zPackage.ReadInt();
		List<bool> list = ReadExploredArray(zPackage, num);
		if (list == null)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < m_textureSize; i++)
		{
			for (int j = 0; j < m_textureSize; j++)
			{
				int num2 = i * m_textureSize + j;
				bool flag2 = list[num2];
				bool flag3 = m_exploredOthers[num2] || m_explored[num2];
				if (flag2 != flag3 && flag2 && ExploreOthers(j, i))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			m_fogTexture.Apply();
		}
		bool flag4 = false;
		if (num >= 2)
		{
			long playerID = Player.m_localPlayer.GetPlayerID();
			bool flag5 = false;
			for (int num3 = m_pins.Count - 1; num3 >= 0; num3--)
			{
				PinData pinData = m_pins[num3];
				if (pinData.m_ownerID != 0L && pinData.m_ownerID != playerID)
				{
					pinData.m_shouldDelete = true;
					flag5 = true;
				}
			}
			int num4 = zPackage.ReadInt();
			for (int k = 0; k < num4; k++)
			{
				long num5 = zPackage.ReadLong();
				string text = zPackage.ReadString();
				Vector3 pos = zPackage.ReadVector3();
				PinType type = (PinType)zPackage.ReadInt();
				bool isChecked = zPackage.ReadBool();
				string author = ((num >= 3) ? zPackage.ReadString() : "");
				if (HavePinInRange(pos, 1f))
				{
					GetClosestPin(pos, 1f, mustBeVisible: false).m_shouldDelete = false;
				}
				else if (num5 != playerID)
				{
					if (num5 == playerID)
					{
						num5 = 0L;
					}
					AddPin(pos, type, text, save: true, isChecked, num5, author);
					flag4 = true;
				}
			}
			if (flag5)
			{
				for (int num6 = m_pins.Count - 1; num6 >= 0; num6--)
				{
					PinData pinData2 = m_pins[num6];
					if (pinData2.m_ownerID != 0L && pinData2.m_ownerID != playerID && pinData2.m_shouldDelete)
					{
						RemovePin(pinData2);
						flag4 = true;
					}
				}
			}
		}
		return flag || flag4;
	}
}
