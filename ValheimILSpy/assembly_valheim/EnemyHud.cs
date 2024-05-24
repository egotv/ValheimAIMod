using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyHud : MonoBehaviour
{
	private class HudData
	{
		public Character m_character;

		public BaseAI m_ai;

		public GameObject m_gui;

		public RectTransform m_level2;

		public RectTransform m_level3;

		public RectTransform m_alerted;

		public RectTransform m_aware;

		public GuiBar m_healthFast;

		public GuiBar m_healthFastFriendly;

		public GuiBar m_healthSlow;

		public TextMeshProUGUI m_healthText;

		public GuiBar m_stamina;

		public TextMeshProUGUI m_staminaText;

		public TextMeshProUGUI m_name;

		public float m_hoverTimer = 99999f;

		public bool m_isMount;
	}

	private static EnemyHud m_instance;

	public GameObject m_hudRoot;

	public GameObject m_baseHud;

	public GameObject m_baseHudBoss;

	public GameObject m_baseHudPlayer;

	public GameObject m_baseHudMount;

	public float m_maxShowDistance = 10f;

	public float m_maxShowDistanceBoss = 100f;

	public float m_hoverShowDuration = 60f;

	private Vector3 m_refPoint = Vector3.zero;

	private Dictionary<Character, HudData> m_huds = new Dictionary<Character, HudData>();

	public static EnemyHud instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_baseHud.SetActive(value: false);
		m_baseHudBoss.SetActive(value: false);
		m_baseHudPlayer.SetActive(value: false);
		m_baseHudMount.SetActive(value: false);
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void LateUpdate()
	{
		m_hudRoot.SetActive(!Hud.IsUserHidden());
		Sadle sadle = null;
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer != null)
		{
			m_refPoint = localPlayer.transform.position;
			sadle = localPlayer.GetDoodadController() as Sadle;
		}
		foreach (Character allCharacter in Character.GetAllCharacters())
		{
			if (!(allCharacter == localPlayer) && (!sadle || !(allCharacter == sadle.GetCharacter())) && TestShow(allCharacter, isVisible: false))
			{
				bool isMount = (bool)sadle && allCharacter == sadle.GetCharacter();
				ShowHud(allCharacter, isMount);
			}
		}
		UpdateHuds(localPlayer, sadle, Time.deltaTime);
	}

	private bool TestShow(Character c, bool isVisible)
	{
		float num = Vector3.SqrMagnitude(c.transform.position - m_refPoint);
		if (c.IsBoss() && num < m_maxShowDistanceBoss * m_maxShowDistanceBoss)
		{
			if (isVisible && c.m_dontHideBossHud)
			{
				return true;
			}
			if (c.GetComponent<BaseAI>().IsAlerted())
			{
				return true;
			}
		}
		else if (num < m_maxShowDistance * m_maxShowDistance)
		{
			if (c.IsPlayer() && c.IsCrouching())
			{
				return false;
			}
			return true;
		}
		return false;
	}

	private void ShowHud(Character c, bool isMount)
	{
		if (!m_huds.TryGetValue(c, out var value))
		{
			GameObject original = (isMount ? m_baseHudMount : (c.IsPlayer() ? m_baseHudPlayer : ((!c.IsBoss()) ? m_baseHud : m_baseHudBoss)));
			value = new HudData();
			value.m_character = c;
			value.m_ai = c.GetComponent<BaseAI>();
			value.m_gui = Object.Instantiate(original, m_hudRoot.transform);
			value.m_gui.SetActive(value: true);
			value.m_healthFast = value.m_gui.transform.Find("Health/health_fast").GetComponent<GuiBar>();
			value.m_healthSlow = value.m_gui.transform.Find("Health/health_slow").GetComponent<GuiBar>();
			Transform transform = value.m_gui.transform.Find("Health/health_fast_friendly");
			if ((bool)transform)
			{
				value.m_healthFastFriendly = transform.GetComponent<GuiBar>();
			}
			if (isMount)
			{
				value.m_stamina = value.m_gui.transform.Find("Stamina/stamina_fast").GetComponent<GuiBar>();
				value.m_staminaText = value.m_gui.transform.Find("Stamina/StaminaText").GetComponent<TextMeshProUGUI>();
				value.m_healthText = value.m_gui.transform.Find("Health/HealthText").GetComponent<TextMeshProUGUI>();
			}
			value.m_level2 = value.m_gui.transform.Find("level_2") as RectTransform;
			value.m_level3 = value.m_gui.transform.Find("level_3") as RectTransform;
			value.m_alerted = value.m_gui.transform.Find("Alerted") as RectTransform;
			value.m_aware = value.m_gui.transform.Find("Aware") as RectTransform;
			value.m_name = value.m_gui.transform.Find("Name").GetComponent<TextMeshProUGUI>();
			value.m_name.text = Localization.instance.Localize(c.GetHoverName());
			value.m_isMount = isMount;
			m_huds.Add(c, value);
		}
	}

	private void UpdateHuds(Player player, Sadle sadle, float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!mainCamera)
		{
			return;
		}
		Character character = (sadle ? sadle.GetCharacter() : null);
		Character character2 = (player ? player.GetHoverCreature() : null);
		Character character3 = null;
		foreach (KeyValuePair<Character, HudData> hud in m_huds)
		{
			HudData value = hud.Value;
			if (!value.m_character || !TestShow(value.m_character, isVisible: true) || value.m_character == character)
			{
				if (character3 == null)
				{
					character3 = value.m_character;
					Object.Destroy(value.m_gui);
				}
				continue;
			}
			if (value.m_character == character2)
			{
				value.m_hoverTimer = 0f;
			}
			value.m_hoverTimer += dt;
			float healthPercentage = value.m_character.GetHealthPercentage();
			if (value.m_character.IsPlayer() || value.m_character.IsBoss() || value.m_isMount || value.m_hoverTimer < m_hoverShowDuration)
			{
				value.m_gui.SetActive(value: true);
				int level = value.m_character.GetLevel();
				if ((bool)value.m_level2)
				{
					value.m_level2.gameObject.SetActive(level == 2);
				}
				if ((bool)value.m_level3)
				{
					value.m_level3.gameObject.SetActive(level == 3);
				}
				value.m_name.text = Localization.instance.Localize(value.m_character.GetHoverName());
				if (!value.m_character.IsBoss() && !value.m_character.IsPlayer())
				{
					bool flag = value.m_character.GetBaseAI().HaveTarget();
					bool flag2 = value.m_character.GetBaseAI().IsAlerted();
					value.m_alerted.gameObject.SetActive(flag2);
					value.m_aware.gameObject.SetActive(!flag2 && flag);
				}
			}
			else
			{
				value.m_gui.SetActive(value: false);
			}
			value.m_healthSlow.SetValue(healthPercentage);
			if ((bool)value.m_healthFastFriendly)
			{
				bool flag3 = !player || BaseAI.IsEnemy(player, value.m_character);
				value.m_healthFast.gameObject.SetActive(flag3);
				value.m_healthFastFriendly.gameObject.SetActive(!flag3);
				value.m_healthFast.SetValue(healthPercentage);
				value.m_healthFastFriendly.SetValue(healthPercentage);
			}
			else
			{
				value.m_healthFast.SetValue(healthPercentage);
			}
			if (value.m_isMount)
			{
				float stamina = sadle.GetStamina();
				float maxStamina = sadle.GetMaxStamina();
				value.m_stamina.SetValue(stamina / maxStamina);
				value.m_healthText.text = Mathf.CeilToInt(value.m_character.GetHealth()).ToString();
				value.m_staminaText.text = Mathf.CeilToInt(stamina).ToString();
			}
			if (!value.m_character.IsBoss() && value.m_gui.activeSelf)
			{
				Vector3 zero = Vector3.zero;
				zero = (value.m_character.IsPlayer() ? (value.m_character.GetHeadPoint() + Vector3.up * 0.3f) : ((!value.m_isMount) ? value.m_character.GetTopPoint() : (player.transform.position - player.transform.up * 0.5f)));
				Vector3 position = mainCamera.WorldToScreenPointScaled(zero);
				if (position.x < 0f || position.x > (float)Screen.width || position.y < 0f || position.y > (float)Screen.height || position.z > 0f)
				{
					value.m_gui.transform.position = position;
					value.m_gui.SetActive(value: true);
				}
				else
				{
					value.m_gui.SetActive(value: false);
				}
			}
		}
		if (character3 != null)
		{
			m_huds.Remove(character3);
		}
	}

	public bool ShowingBossHud()
	{
		foreach (KeyValuePair<Character, HudData> hud in m_huds)
		{
			if ((bool)hud.Value.m_character && hud.Value.m_character.IsBoss())
			{
				return true;
			}
		}
		return false;
	}

	public Character GetActiveBoss()
	{
		foreach (KeyValuePair<Character, HudData> hud in m_huds)
		{
			if ((bool)hud.Value.m_character && hud.Value.m_character.IsBoss())
			{
				return hud.Value.m_character;
			}
		}
		return null;
	}
}
