using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VisEquipment : MonoBehaviour, IMonoUpdater
{
	[Serializable]
	public class PlayerModel
	{
		public Mesh m_mesh;

		public Material m_baseMaterial;
	}

	public SkinnedMeshRenderer m_bodyModel;

	public ZNetView m_nViewOverride;

	[Header("Attachment points")]
	public Transform m_leftHand;

	public Transform m_rightHand;

	public Transform m_helmet;

	public Transform m_backShield;

	public Transform m_backMelee;

	public Transform m_backTwohandedMelee;

	public Transform m_backBow;

	public Transform m_backTool;

	public Transform m_backAtgeir;

	public CapsuleCollider[] m_clothColliders = Array.Empty<CapsuleCollider>();

	public PlayerModel[] m_models = Array.Empty<PlayerModel>();

	public bool m_isPlayer;

	public bool m_useAllTrails;

	private string m_leftItem = "";

	private string m_rightItem = "";

	private string m_chestItem = "";

	private string m_legItem = "";

	private string m_helmetItem = "";

	private string m_shoulderItem = "";

	private string m_beardItem = "";

	private string m_hairItem = "";

	private string m_utilityItem = "";

	private string m_leftBackItem = "";

	private string m_rightBackItem = "";

	private int m_shoulderItemVariant;

	private int m_leftItemVariant;

	private int m_leftBackItemVariant;

	private GameObject m_leftItemInstance;

	private GameObject m_rightItemInstance;

	private GameObject m_helmetItemInstance;

	private List<GameObject> m_chestItemInstances;

	private List<GameObject> m_legItemInstances;

	private List<GameObject> m_shoulderItemInstances;

	private List<GameObject> m_utilityItemInstances;

	private GameObject m_beardItemInstance;

	private GameObject m_hairItemInstance;

	private GameObject m_leftBackItemInstance;

	private GameObject m_rightBackItemInstance;

	private int m_currentLeftItemHash;

	private int m_currentRightItemHash;

	private int m_currentChestItemHash;

	private int m_currentLegItemHash;

	private int m_currentHelmetItemHash;

	private int m_currentShoulderItemHash;

	private int m_currentBeardItemHash;

	private int m_currentHairItemHash;

	private int m_currentUtilityItemHash;

	private int m_currentLeftBackItemHash;

	private int m_currentRightBackItemHash;

	private int m_currentShoulderItemVariant;

	private int m_currentLeftItemVariant;

	private int m_currentLeftBackItemVariant;

	private ItemDrop.ItemData.HelmetHairType m_helmetHideHair;

	private ItemDrop.ItemData.HelmetHairType m_helmetHideBeard;

	private Texture m_emptyBodyTexture;

	private Texture m_emptyLegsTexture;

	private int m_modelIndex;

	private Vector3 m_skinColor = Vector3.one;

	private Vector3 m_hairColor = Vector3.one;

	private int m_currentModelIndex;

	private ZNetView m_nview;

	private GameObject m_visual;

	private LODGroup m_lodGroup;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_nview = ((m_nViewOverride != null) ? m_nViewOverride : GetComponent<ZNetView>());
		Transform transform = base.transform.Find("Visual");
		if (transform == null)
		{
			transform = base.transform;
		}
		m_visual = transform.gameObject;
		m_lodGroup = m_visual.GetComponentInChildren<LODGroup>();
		if (m_bodyModel != null && m_bodyModel.material.HasProperty("_ChestTex"))
		{
			m_emptyBodyTexture = m_bodyModel.material.GetTexture("_ChestTex");
		}
		if (m_bodyModel != null && m_bodyModel.material.HasProperty("_LegsTex"))
		{
			m_emptyLegsTexture = m_bodyModel.material.GetTexture("_LegsTex");
		}
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	private void Start()
	{
		UpdateVisuals();
	}

	public void SetWeaponTrails(bool enabled)
	{
		if (m_useAllTrails)
		{
			MeleeWeaponTrail[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Emit = enabled;
			}
		}
		else if ((bool)m_rightItemInstance)
		{
			MeleeWeaponTrail[] componentsInChildren = m_rightItemInstance.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Emit = enabled;
			}
		}
	}

	public void SetModel(int index)
	{
		if (m_modelIndex != index && index >= 0 && index < m_models.Length)
		{
			ZLog.Log("Vis equip model set to " + index);
			m_modelIndex = index;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_modelIndex, m_modelIndex);
			}
		}
	}

	public void SetSkinColor(Vector3 color)
	{
		if (!(color == m_skinColor))
		{
			m_skinColor = color;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_skinColor, m_skinColor);
			}
		}
	}

	public void SetHairColor(Vector3 color)
	{
		if (!(m_hairColor == color))
		{
			m_hairColor = color;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_hairColor, m_hairColor);
			}
		}
	}

	public void SetItem(VisSlot slot, string name, int variant = 0)
	{
		switch (slot)
		{
		case VisSlot.HandLeft:
			SetLeftItem(name, variant);
			break;
		case VisSlot.HandRight:
			SetRightItem(name);
			break;
		case VisSlot.BackLeft:
			SetLeftBackItem(name, variant);
			break;
		case VisSlot.BackRight:
			SetRightBackItem(name);
			break;
		case VisSlot.Chest:
			SetChestItem(name);
			break;
		case VisSlot.Legs:
			SetLegItem(name);
			break;
		case VisSlot.Helmet:
			SetHelmetItem(name);
			break;
		case VisSlot.Shoulder:
			SetShoulderItem(name, variant);
			break;
		case VisSlot.Utility:
			SetUtilityItem(name);
			break;
		case VisSlot.Beard:
			SetBeardItem(name);
			break;
		case VisSlot.Hair:
			SetHairItem(name);
			break;
		default:
			throw new NotImplementedException("Unknown slot: " + slot);
		}
	}

	public void SetLeftItem(string name, int variant)
	{
		if (!(m_leftItem == name) || m_leftItemVariant != variant)
		{
			m_leftItem = name;
			m_leftItemVariant = variant;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_leftItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
				m_nview.GetZDO().Set(ZDOVars.s_leftItemVariant, variant);
			}
		}
	}

	public void SetRightItem(string name)
	{
		if (!(m_rightItem == name))
		{
			m_rightItem = name;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_rightItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetLeftBackItem(string name, int variant)
	{
		if (!(m_leftBackItem == name) || m_leftBackItemVariant != variant)
		{
			m_leftBackItem = name;
			m_leftBackItemVariant = variant;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_leftBackItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
				m_nview.GetZDO().Set(ZDOVars.s_leftBackItemVariant, variant);
			}
		}
	}

	public void SetRightBackItem(string name)
	{
		if (!(m_rightBackItem == name))
		{
			m_rightBackItem = name;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_rightBackItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetChestItem(string name)
	{
		if (!(m_chestItem == name))
		{
			m_chestItem = name;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_chestItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetLegItem(string name)
	{
		if (!(m_legItem == name))
		{
			m_legItem = name;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_legItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetHelmetItem(string name)
	{
		if (!(m_helmetItem == name))
		{
			m_helmetItem = name;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_helmetItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetShoulderItem(string name, int variant)
	{
		if (!(m_shoulderItem == name) || m_shoulderItemVariant != variant)
		{
			m_shoulderItem = name;
			m_shoulderItemVariant = variant;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_shoulderItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
				m_nview.GetZDO().Set(ZDOVars.s_shoulderItemVariant, variant);
			}
		}
	}

	public void SetBeardItem(string name)
	{
		if (!(m_beardItem == name))
		{
			m_beardItem = name;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_beardItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetHairItem(string name)
	{
		if (!(m_hairItem == name))
		{
			m_hairItem = name;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_hairItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetUtilityItem(string name)
	{
		if (!(m_utilityItem == name))
		{
			m_utilityItem = name;
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_utilityItem, (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void CustomUpdate(float deltaTime, float time)
	{
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		UpdateEquipmentVisuals();
		if (m_isPlayer)
		{
			UpdateBaseModel();
			UpdateColors();
		}
	}

	private void UpdateColors()
	{
		Color value = Utils.Vec3ToColor(m_skinColor);
		Color value2 = Utils.Vec3ToColor(m_hairColor);
		if (m_nview.GetZDO() != null)
		{
			value = Utils.Vec3ToColor(m_nview.GetZDO().GetVec3(ZDOVars.s_skinColor, Vector3.one));
			value2 = Utils.Vec3ToColor(m_nview.GetZDO().GetVec3(ZDOVars.s_hairColor, Vector3.one));
		}
		m_bodyModel.materials[0].SetColor("_SkinColor", value);
		m_bodyModel.materials[1].SetColor("_SkinColor", value2);
		if ((bool)m_beardItemInstance)
		{
			Renderer[] componentsInChildren = m_beardItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.SetColor("_SkinColor", value2);
			}
		}
		if ((bool)m_hairItemInstance)
		{
			Renderer[] componentsInChildren = m_hairItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.SetColor("_SkinColor", value2);
			}
		}
	}

	private void UpdateBaseModel()
	{
		if (m_models.Length != 0)
		{
			int num = m_modelIndex;
			if (m_nview.GetZDO() != null)
			{
				num = m_nview.GetZDO().GetInt(ZDOVars.s_modelIndex);
			}
			if (m_currentModelIndex != num || m_bodyModel.sharedMesh != m_models[num].m_mesh)
			{
				m_currentModelIndex = num;
				m_bodyModel.sharedMesh = m_models[num].m_mesh;
				m_bodyModel.materials[0].SetTexture("_MainTex", m_models[num].m_baseMaterial.GetTexture("_MainTex"));
				m_bodyModel.materials[0].SetTexture("_SkinBumpMap", m_models[num].m_baseMaterial.GetTexture("_SkinBumpMap"));
			}
		}
	}

	private void UpdateEquipmentVisuals()
	{
		int hash = 0;
		int rightHandEquipped = 0;
		int chestEquipped = 0;
		int legEquipped = 0;
		int hash2 = 0;
		int itemHash = 0;
		int num = 0;
		int hash3 = 0;
		int utilityEquipped = 0;
		int leftItem = 0;
		int rightItem = 0;
		int variant = m_shoulderItemVariant;
		int variant2 = m_leftItemVariant;
		int leftVariant = m_leftBackItemVariant;
		ZDO zDO = m_nview.GetZDO();
		if (zDO != null)
		{
			hash = zDO.GetInt(ZDOVars.s_leftItem);
			rightHandEquipped = zDO.GetInt(ZDOVars.s_rightItem);
			chestEquipped = zDO.GetInt(ZDOVars.s_chestItem);
			legEquipped = zDO.GetInt(ZDOVars.s_legItem);
			hash2 = zDO.GetInt(ZDOVars.s_helmetItem);
			hash3 = zDO.GetInt(ZDOVars.s_shoulderItem);
			utilityEquipped = zDO.GetInt(ZDOVars.s_utilityItem);
			if (m_isPlayer)
			{
				itemHash = zDO.GetInt(ZDOVars.s_beardItem);
				num = zDO.GetInt(ZDOVars.s_hairItem);
				leftItem = zDO.GetInt(ZDOVars.s_leftBackItem);
				rightItem = zDO.GetInt(ZDOVars.s_rightBackItem);
				variant = zDO.GetInt(ZDOVars.s_shoulderItemVariant);
				variant2 = zDO.GetInt(ZDOVars.s_leftItemVariant);
				leftVariant = zDO.GetInt(ZDOVars.s_leftBackItemVariant);
			}
		}
		else
		{
			if (!string.IsNullOrEmpty(m_leftItem))
			{
				hash = m_leftItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_rightItem))
			{
				rightHandEquipped = m_rightItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_chestItem))
			{
				chestEquipped = m_chestItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_legItem))
			{
				legEquipped = m_legItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_helmetItem))
			{
				hash2 = m_helmetItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_shoulderItem))
			{
				hash3 = m_shoulderItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_utilityItem))
			{
				utilityEquipped = m_utilityItem.GetStableHashCode();
			}
			if (m_isPlayer)
			{
				if (!string.IsNullOrEmpty(m_beardItem))
				{
					itemHash = m_beardItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(m_hairItem))
				{
					num = m_hairItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(m_leftBackItem))
				{
					leftItem = m_leftBackItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(m_rightBackItem))
				{
					rightItem = m_rightBackItem.GetStableHashCode();
				}
			}
		}
		bool flag = false;
		flag = SetRightHandEquipped(rightHandEquipped) || flag;
		flag = SetLeftHandEquipped(hash, variant2) || flag;
		flag = SetChestEquipped(chestEquipped) || flag;
		flag = SetLegEquipped(legEquipped) || flag;
		flag = SetHelmetEquipped(hash2, num) || flag;
		flag = SetShoulderEquipped(hash3, variant) || flag;
		flag = SetUtilityEquipped(utilityEquipped) || flag;
		if (m_isPlayer)
		{
			itemHash = GetHairItem(m_helmetHideBeard, itemHash, ItemDrop.ItemData.AccessoryType.Beard);
			flag = SetBeardEquipped(itemHash) || flag;
			flag = SetBackEquipped(leftItem, rightItem, leftVariant) || flag;
			num = GetHairItem(m_helmetHideHair, num, ItemDrop.ItemData.AccessoryType.Hair);
			flag = SetHairEquipped(num) || flag;
		}
		if (flag)
		{
			UpdateLodgroup();
		}
	}

	private int GetHairItem(ItemDrop.ItemData.HelmetHairType type, int itemHash, ItemDrop.ItemData.AccessoryType accessory)
	{
		if (type == ItemDrop.ItemData.HelmetHairType.Hidden)
		{
			return 0;
		}
		if (type == ItemDrop.ItemData.HelmetHairType.Default)
		{
			return itemHash;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if ((bool)itemPrefab)
		{
			ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
			if ((object)component != null)
			{
				ItemDrop.ItemData.HelmetHairSettings helmetHairSettings = (accessory switch
				{
					ItemDrop.ItemData.AccessoryType.Hair => component.m_itemData.m_shared.m_helmetHairSettings, 
					ItemDrop.ItemData.AccessoryType.Beard => component.m_itemData.m_shared.m_helmetBeardSettings, 
					_ => throw new Exception("Acecssory type not implemented"), 
				}).FirstOrDefault((ItemDrop.ItemData.HelmetHairSettings x) => x.m_setting == type);
				if (helmetHairSettings != null)
				{
					return helmetHairSettings.m_hairPrefab.name.GetStableHashCode();
				}
			}
		}
		return 0;
	}

	private void UpdateLodgroup()
	{
		if (m_lodGroup == null)
		{
			return;
		}
		List<Renderer> list = new List<Renderer>(m_visual.GetComponentsInChildren<Renderer>());
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Renderer renderer = list[num];
			LODGroup componentInParent = renderer.GetComponentInParent<LODGroup>();
			if ((object)componentInParent != null && componentInParent != m_lodGroup)
			{
				LOD[] lODs = componentInParent.GetLODs();
				for (int i = 0; i < lODs.Length; i++)
				{
					if (Array.IndexOf(lODs[i].renderers, renderer) >= 0)
					{
						list.RemoveAt(num);
						break;
					}
				}
			}
		}
		LOD[] lODs2 = m_lodGroup.GetLODs();
		lODs2[0].renderers = list.ToArray();
		m_lodGroup.SetLODs(lODs2);
	}

	private bool SetRightHandEquipped(int hash)
	{
		if (m_currentRightItemHash == hash)
		{
			return false;
		}
		if ((bool)m_rightItemInstance)
		{
			UnityEngine.Object.Destroy(m_rightItemInstance);
			m_rightItemInstance = null;
		}
		m_currentRightItemHash = hash;
		if (hash != 0)
		{
			m_rightItemInstance = AttachItem(hash, 0, m_rightHand);
		}
		return true;
	}

	private bool SetLeftHandEquipped(int hash, int variant)
	{
		if (m_currentLeftItemHash == hash && m_currentLeftItemVariant == variant)
		{
			return false;
		}
		if ((bool)m_leftItemInstance)
		{
			UnityEngine.Object.Destroy(m_leftItemInstance);
			m_leftItemInstance = null;
		}
		m_currentLeftItemHash = hash;
		m_currentLeftItemVariant = variant;
		if (hash != 0)
		{
			m_leftItemInstance = AttachItem(hash, variant, m_leftHand);
		}
		return true;
	}

	private bool SetBackEquipped(int leftItem, int rightItem, int leftVariant)
	{
		if (m_currentLeftBackItemHash == leftItem && m_currentRightBackItemHash == rightItem && m_currentLeftBackItemVariant == leftVariant)
		{
			return false;
		}
		if ((bool)m_leftBackItemInstance)
		{
			UnityEngine.Object.Destroy(m_leftBackItemInstance);
			m_leftBackItemInstance = null;
		}
		if ((bool)m_rightBackItemInstance)
		{
			UnityEngine.Object.Destroy(m_rightBackItemInstance);
			m_rightBackItemInstance = null;
		}
		m_currentLeftBackItemHash = leftItem;
		m_currentRightBackItemHash = rightItem;
		m_currentLeftBackItemVariant = leftVariant;
		if (m_currentLeftBackItemHash != 0)
		{
			m_leftBackItemInstance = AttachBackItem(leftItem, leftVariant, rightHand: false);
		}
		if (m_currentRightBackItemHash != 0)
		{
			m_rightBackItemInstance = AttachBackItem(rightItem, 0, rightHand: true);
		}
		return true;
	}

	private GameObject AttachBackItem(int hash, int variant, bool rightHand)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing back attach item prefab: " + hash);
			return null;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		switch ((component.m_itemData.m_shared.m_attachOverride != 0) ? component.m_itemData.m_shared.m_attachOverride : component.m_itemData.m_shared.m_itemType)
		{
		case ItemDrop.ItemData.ItemType.Torch:
			if (rightHand)
			{
				return AttachItem(hash, variant, m_backMelee, enableEquipEffects: false, backAttach: true);
			}
			return AttachItem(hash, variant, m_backTool, enableEquipEffects: false, backAttach: true);
		case ItemDrop.ItemData.ItemType.Bow:
			return AttachItem(hash, variant, m_backBow, enableEquipEffects: false, backAttach: true);
		case ItemDrop.ItemData.ItemType.Tool:
			return AttachItem(hash, variant, m_backTool, enableEquipEffects: false, backAttach: true);
		case ItemDrop.ItemData.ItemType.Attach_Atgeir:
			return AttachItem(hash, variant, m_backAtgeir, enableEquipEffects: false, backAttach: true);
		case ItemDrop.ItemData.ItemType.OneHandedWeapon:
			return AttachItem(hash, variant, m_backMelee, enableEquipEffects: false, backAttach: true);
		case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
		case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
			return AttachItem(hash, variant, m_backTwohandedMelee, enableEquipEffects: false, backAttach: true);
		case ItemDrop.ItemData.ItemType.Shield:
			return AttachItem(hash, variant, m_backShield, enableEquipEffects: false, backAttach: true);
		default:
			return null;
		}
	}

	private bool SetChestEquipped(int hash)
	{
		if (m_currentChestItemHash == hash)
		{
			return false;
		}
		m_currentChestItemHash = hash;
		if (m_bodyModel == null)
		{
			return true;
		}
		if (m_chestItemInstances != null)
		{
			foreach (GameObject chestItemInstance in m_chestItemInstances)
			{
				if ((bool)m_lodGroup)
				{
					Utils.RemoveFromLodgroup(m_lodGroup, chestItemInstance);
				}
				UnityEngine.Object.Destroy(chestItemInstance);
			}
			m_chestItemInstances = null;
			m_bodyModel.material.SetTexture("_ChestTex", m_emptyBodyTexture);
			m_bodyModel.material.SetTexture("_ChestBumpMap", null);
			m_bodyModel.material.SetTexture("_ChestMetal", null);
		}
		if (m_currentChestItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing chest item " + hash);
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if ((bool)component.m_itemData.m_shared.m_armorMaterial)
		{
			m_bodyModel.material.SetTexture("_ChestTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestTex"));
			m_bodyModel.material.SetTexture("_ChestBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestBumpMap"));
			m_bodyModel.material.SetTexture("_ChestMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestMetal"));
		}
		m_chestItemInstances = AttachArmor(hash);
		return true;
	}

	private bool SetShoulderEquipped(int hash, int variant)
	{
		if (m_currentShoulderItemHash == hash && m_currentShoulderItemVariant == variant)
		{
			return false;
		}
		m_currentShoulderItemHash = hash;
		m_currentShoulderItemVariant = variant;
		if (m_bodyModel == null)
		{
			return true;
		}
		if (m_shoulderItemInstances != null)
		{
			foreach (GameObject shoulderItemInstance in m_shoulderItemInstances)
			{
				if ((bool)m_lodGroup)
				{
					Utils.RemoveFromLodgroup(m_lodGroup, shoulderItemInstance);
				}
				UnityEngine.Object.Destroy(shoulderItemInstance);
			}
			m_shoulderItemInstances = null;
		}
		if (m_currentShoulderItemHash == 0)
		{
			return true;
		}
		if (ObjectDB.instance.GetItemPrefab(hash) == null)
		{
			ZLog.Log("Missing shoulder item " + hash);
			return true;
		}
		m_shoulderItemInstances = AttachArmor(hash, variant);
		return true;
	}

	private bool SetLegEquipped(int hash)
	{
		if (m_currentLegItemHash == hash)
		{
			return false;
		}
		m_currentLegItemHash = hash;
		if (m_bodyModel == null)
		{
			return true;
		}
		if (m_legItemInstances != null)
		{
			foreach (GameObject legItemInstance in m_legItemInstances)
			{
				UnityEngine.Object.Destroy(legItemInstance);
			}
			m_legItemInstances = null;
			m_bodyModel.material.SetTexture("_LegsTex", m_emptyLegsTexture);
			m_bodyModel.material.SetTexture("_LegsBumpMap", null);
			m_bodyModel.material.SetTexture("_LegsMetal", null);
		}
		if (m_currentLegItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing legs item " + hash);
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if ((bool)component.m_itemData.m_shared.m_armorMaterial)
		{
			m_bodyModel.material.SetTexture("_LegsTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsTex"));
			m_bodyModel.material.SetTexture("_LegsBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsBumpMap"));
			m_bodyModel.material.SetTexture("_LegsMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsMetal"));
		}
		m_legItemInstances = AttachArmor(hash);
		return true;
	}

	private bool SetBeardEquipped(int hash)
	{
		if (m_currentBeardItemHash == hash)
		{
			return false;
		}
		if ((bool)m_beardItemInstance)
		{
			UnityEngine.Object.Destroy(m_beardItemInstance);
			m_beardItemInstance = null;
		}
		m_currentBeardItemHash = hash;
		if (hash != 0)
		{
			m_beardItemInstance = AttachItem(hash, 0, m_helmet);
		}
		return true;
	}

	private bool SetHairEquipped(int hash)
	{
		if (m_currentHairItemHash == hash)
		{
			return false;
		}
		if ((bool)m_hairItemInstance)
		{
			UnityEngine.Object.Destroy(m_hairItemInstance);
			m_hairItemInstance = null;
		}
		m_currentHairItemHash = hash;
		if (hash != 0)
		{
			m_hairItemInstance = AttachItem(hash, 0, m_helmet);
		}
		return true;
	}

	private bool SetHelmetEquipped(int hash, int hairHash)
	{
		if (m_currentHelmetItemHash == hash)
		{
			return false;
		}
		if ((bool)m_helmetItemInstance)
		{
			UnityEngine.Object.Destroy(m_helmetItemInstance);
			m_helmetItemInstance = null;
		}
		m_currentHelmetItemHash = hash;
		HelmetHides(hash, out m_helmetHideHair, out m_helmetHideBeard);
		if (hash != 0)
		{
			m_helmetItemInstance = AttachItem(hash, 0, m_helmet);
		}
		return true;
	}

	private bool SetUtilityEquipped(int hash)
	{
		if (m_currentUtilityItemHash == hash)
		{
			return false;
		}
		if (m_utilityItemInstances != null)
		{
			foreach (GameObject utilityItemInstance in m_utilityItemInstances)
			{
				if ((bool)m_lodGroup)
				{
					Utils.RemoveFromLodgroup(m_lodGroup, utilityItemInstance);
				}
				UnityEngine.Object.Destroy(utilityItemInstance);
			}
			m_utilityItemInstances = null;
		}
		m_currentUtilityItemHash = hash;
		if (hash != 0)
		{
			m_utilityItemInstances = AttachArmor(hash);
		}
		return true;
	}

	private static void HelmetHides(int itemHash, out ItemDrop.ItemData.HelmetHairType hideHair, out ItemDrop.ItemData.HelmetHairType hideBeard)
	{
		hideHair = ItemDrop.ItemData.HelmetHairType.Default;
		hideBeard = ItemDrop.ItemData.HelmetHairType.Default;
		if (itemHash != 0)
		{
			GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
			if (!(itemPrefab == null))
			{
				ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
				hideHair = component.m_itemData.m_shared.m_helmetHideHair;
				hideBeard = component.m_itemData.m_shared.m_helmetHideBeard;
			}
		}
	}

	private List<GameObject> AttachArmor(int itemHash, int variant = -1)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing attach item: " + itemHash + "  ob:" + base.gameObject.name);
			return null;
		}
		List<GameObject> list = new List<GameObject>();
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (!child.gameObject.name.CustomStartsWith("attach_"))
			{
				continue;
			}
			string text = child.gameObject.name.Substring(7);
			GameObject gameObject;
			if (text == "skin")
			{
				gameObject = UnityEngine.Object.Instantiate(child.gameObject, m_bodyModel.transform.position, m_bodyModel.transform.parent.rotation, m_bodyModel.transform.parent);
				gameObject.SetActive(value: true);
				SkinnedMeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
				foreach (SkinnedMeshRenderer obj in componentsInChildren)
				{
					obj.rootBone = m_bodyModel.rootBone;
					obj.bones = m_bodyModel.bones;
				}
				Cloth[] componentsInChildren2 = gameObject.GetComponentsInChildren<Cloth>();
				foreach (Cloth cloth in componentsInChildren2)
				{
					if (m_clothColliders.Length != 0)
					{
						if (cloth.capsuleColliders.Length != 0)
						{
							List<CapsuleCollider> list2 = new List<CapsuleCollider>(m_clothColliders);
							list2.AddRange(cloth.capsuleColliders);
							cloth.capsuleColliders = list2.ToArray();
						}
						else
						{
							cloth.capsuleColliders = m_clothColliders;
						}
					}
				}
			}
			else
			{
				Transform transform = Utils.FindChild(m_visual.transform, text);
				if (transform == null)
				{
					ZLog.LogWarning("Missing joint " + text + " in item " + itemPrefab.name);
					continue;
				}
				gameObject = UnityEngine.Object.Instantiate(child.gameObject);
				gameObject.SetActive(value: true);
				gameObject.transform.SetParent(transform);
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
			}
			if (variant >= 0)
			{
				gameObject.GetComponentInChildren<IEquipmentVisual>()?.Setup(variant);
			}
			CleanupInstance(gameObject);
			EnableEquippedEffects(gameObject);
			list.Add(gameObject);
		}
		return list;
	}

	private GameObject AttachItem(int itemHash, int variant, Transform joint, bool enableEquipEffects = true, bool backAttach = false)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing attach item: " + itemHash + "  ob:" + base.gameObject.name + "  joint:" + (joint ? joint.name : "none"));
			return null;
		}
		GameObject gameObject = null;
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (backAttach && child.gameObject.name == "attach_back")
			{
				gameObject = child.gameObject;
				break;
			}
			if (child.gameObject.name == "attach" || (!backAttach && child.gameObject.name == "attach_skin"))
			{
				gameObject = child.gameObject;
				break;
			}
		}
		if (gameObject == null)
		{
			return null;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
		gameObject2.SetActive(value: true);
		CleanupInstance(gameObject2);
		if (enableEquipEffects)
		{
			EnableEquippedEffects(gameObject2);
		}
		if (gameObject.name == "attach_skin")
		{
			gameObject2.transform.SetParent(m_bodyModel.transform.parent);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			SkinnedMeshRenderer[] componentsInChildren = gameObject2.GetComponentsInChildren<SkinnedMeshRenderer>();
			foreach (SkinnedMeshRenderer obj in componentsInChildren)
			{
				obj.rootBone = m_bodyModel.rootBone;
				obj.bones = m_bodyModel.bones;
			}
		}
		else
		{
			gameObject2.transform.SetParent(joint);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
		}
		gameObject2.GetComponentInChildren<IEquipmentVisual>()?.Setup(variant);
		return gameObject2;
	}

	private static void CleanupInstance(GameObject instance)
	{
		Collider[] componentsInChildren = instance.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	private static void EnableEquippedEffects(GameObject instance)
	{
		Transform transform = instance.transform.Find("equiped");
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: true);
		}
	}

	public int GetModelIndex()
	{
		int result = m_modelIndex;
		if (m_nview.IsValid())
		{
			result = m_nview.GetZDO().GetInt(ZDOVars.s_modelIndex);
		}
		return result;
	}
}
