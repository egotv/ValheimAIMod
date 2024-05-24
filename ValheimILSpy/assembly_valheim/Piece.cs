using System;
using System.Collections.Generic;
using UnityEngine;

public class Piece : StaticTarget
{
	public enum PieceCategory
	{
		Misc = 0,
		Crafting = 1,
		BuildingWorkbench = 2,
		BuildingStonecutter = 3,
		Furniture = 4,
		Max = 5,
		All = 100
	}

	public enum ComfortGroup
	{
		None,
		Fire,
		Bed,
		Banner,
		Chair,
		Table,
		Carpet
	}

	[Serializable]
	public class Requirement
	{
		[Header("Resource")]
		public ItemDrop m_resItem;

		public int m_amount = 1;

		public int m_extraAmountOnlyOneIngredient;

		[Header("Item")]
		public int m_amountPerLevel = 1;

		[Header("Piece")]
		public bool m_recover = true;

		public int GetAmount(int qualityLevel)
		{
			if (qualityLevel <= 1)
			{
				return m_amount;
			}
			return (qualityLevel - 1) * m_amountPerLevel;
		}
	}

	public bool m_targetNonPlayerBuilt = true;

	[Header("Basic stuffs")]
	public Sprite m_icon;

	public string m_name = "";

	public string m_description = "";

	public bool m_enabled = true;

	public PieceCategory m_category;

	public bool m_isUpgrade;

	[Header("Comfort")]
	public int m_comfort;

	public ComfortGroup m_comfortGroup;

	public GameObject m_comfortObject;

	[Header("Placement rules")]
	public bool m_groundPiece;

	public bool m_allowAltGroundPlacement;

	public bool m_groundOnly;

	public bool m_cultivatedGroundOnly;

	public bool m_waterPiece;

	public bool m_clipGround;

	public bool m_clipEverything;

	public bool m_noInWater;

	public bool m_notOnWood;

	public bool m_notOnTiltingSurface;

	public bool m_inCeilingOnly;

	public bool m_notOnFloor;

	public bool m_noClipping;

	public bool m_onlyInTeleportArea;

	public bool m_allowedInDungeons;

	public float m_spaceRequirement;

	public bool m_repairPiece;

	public bool m_canRotate = true;

	public bool m_randomInitBuildRotation;

	public bool m_canBeRemoved = true;

	public bool m_allowRotatedOverlap;

	public bool m_vegetationGroundOnly;

	public List<Piece> m_blockingPieces = new List<Piece>();

	public float m_blockRadius;

	public ZNetView m_mustConnectTo;

	public float m_connectRadius;

	public bool m_mustBeAboveConnected;

	public bool m_noVines;

	public int m_extraPlacementDistance;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_onlyInBiome;

	[Header("Effects")]
	public EffectList m_placeEffect = new EffectList();

	[Header("Requirements")]
	public string m_dlc = "";

	public CraftingStation m_craftingStation;

	public Requirement[] m_resources = Array.Empty<Requirement>();

	public GameObject m_destroyedLootPrefab;

	private ZNetView m_nview;

	private List<KeyValuePair<Renderer, Material[]>> m_invalidPlacementMaterials;

	private long m_creator;

	private int m_myListIndex = -1;

	private static int s_ghostLayer = 0;

	private static int s_pieceRayMask = 0;

	private static readonly Collider[] s_pieceColliders = new Collider[2000];

	private static readonly List<Piece> s_allPieces = new List<Piece>();

	private static readonly HashSet<Piece> s_allComfortPieces = new HashSet<Piece>();

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		s_allPieces.Add(this);
		m_myListIndex = s_allPieces.Count - 1;
		if (m_comfort > 0)
		{
			s_allComfortPieces.Add(this);
		}
		if ((bool)m_nview && m_nview.IsValid())
		{
			m_creator = m_nview.GetZDO().GetLong(ZDOVars.s_creator, 0L);
		}
		if (s_ghostLayer == 0)
		{
			s_ghostLayer = LayerMask.NameToLayer("ghost");
		}
		if (s_pieceRayMask == 0)
		{
			s_pieceRayMask = LayerMask.GetMask("piece", "piece_nonsolid");
		}
	}

	private void OnDestroy()
	{
		if (m_myListIndex >= 0)
		{
			s_allPieces[m_myListIndex] = s_allPieces[s_allPieces.Count - 1];
			s_allPieces[m_myListIndex].m_myListIndex = m_myListIndex;
			s_allPieces.RemoveAt(s_allPieces.Count - 1);
			m_myListIndex = -1;
		}
		if (m_comfort > 0)
		{
			s_allComfortPieces.Remove(this);
		}
	}

	public bool CanBeRemoved()
	{
		Container componentInChildren = GetComponentInChildren<Container>();
		if (componentInChildren != null)
		{
			return componentInChildren.CanBeRemoved();
		}
		Ship componentInChildren2 = GetComponentInChildren<Ship>();
		if (componentInChildren2 != null)
		{
			return componentInChildren2.CanBeRemoved();
		}
		return true;
	}

	public void DropResources(HitData hitData = null)
	{
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost))
		{
			return;
		}
		Container container = null;
		Requirement[] resources = m_resources;
		foreach (Requirement requirement in resources)
		{
			if (requirement.m_resItem == null || !requirement.m_recover)
			{
				continue;
			}
			GameObject dropPrefab = requirement.m_resItem.gameObject;
			int dropCount = requirement.m_amount;
			dropPrefab = Game.instance.CheckDropConversion(hitData, requirement.m_resItem, dropPrefab, ref dropCount);
			if (!IsPlacedByPlayer())
			{
				dropCount = Mathf.Max(1, dropCount / 3);
			}
			if ((bool)m_destroyedLootPrefab)
			{
				while (dropCount > 0)
				{
					ItemDrop.ItemData itemData = dropPrefab.GetComponent<ItemDrop>().m_itemData.Clone();
					itemData.m_dropPrefab = dropPrefab;
					itemData.m_stack = Mathf.Min(dropCount, itemData.m_shared.m_maxStackSize);
					dropCount -= itemData.m_stack;
					if (container == null || !container.GetInventory().HaveEmptySlot())
					{
						container = UnityEngine.Object.Instantiate(m_destroyedLootPrefab, base.transform.position + Vector3.up, Quaternion.identity).GetComponent<Container>();
					}
					container.GetInventory().AddItem(itemData);
				}
			}
			else
			{
				while (dropCount > 0)
				{
					ItemDrop component = UnityEngine.Object.Instantiate(dropPrefab, base.transform.position + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
					component.SetStack(Mathf.Min(dropCount, component.m_itemData.m_shared.m_maxStackSize));
					ItemDrop.OnCreateNew(component);
					dropCount -= component.m_itemData.m_stack;
				}
			}
		}
	}

	public override bool IsPriorityTarget()
	{
		if (!base.IsPriorityTarget())
		{
			return false;
		}
		if (!m_targetNonPlayerBuilt)
		{
			return IsPlacedByPlayer();
		}
		return true;
	}

	public override bool IsRandomTarget()
	{
		if (!base.IsRandomTarget())
		{
			return false;
		}
		if (!m_targetNonPlayerBuilt)
		{
			return IsPlacedByPlayer();
		}
		return true;
	}

	public void SetCreator(long uid)
	{
		if (!(m_nview == null) && m_nview.IsOwner() && GetCreator() == 0L)
		{
			m_creator = uid;
			m_nview.GetZDO().Set(ZDOVars.s_creator, uid);
		}
	}

	public long GetCreator()
	{
		return m_creator;
	}

	public bool IsCreator()
	{
		long creator = GetCreator();
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		return creator == playerID;
	}

	public bool IsPlacedByPlayer()
	{
		return GetCreator() != 0;
	}

	public void SetInvalidPlacementHeightlight(bool enabled)
	{
		if ((enabled && m_invalidPlacementMaterials != null) || (!enabled && m_invalidPlacementMaterials == null))
		{
			return;
		}
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		if (enabled)
		{
			m_invalidPlacementMaterials = new List<KeyValuePair<Renderer, Material[]>>();
			Renderer[] array = componentsInChildren;
			foreach (Renderer renderer in array)
			{
				Material[] sharedMaterials = renderer.sharedMaterials;
				m_invalidPlacementMaterials.Add(new KeyValuePair<Renderer, Material[]>(renderer, sharedMaterials));
			}
			array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				Material[] materials = array[i].materials;
				foreach (Material material in materials)
				{
					if (material.HasProperty("_EmissionColor"))
					{
						material.SetColor("_EmissionColor", Color.red * 0.7f);
					}
					material.color = Color.red;
				}
			}
			return;
		}
		foreach (KeyValuePair<Renderer, Material[]> invalidPlacementMaterial in m_invalidPlacementMaterials)
		{
			if ((bool)invalidPlacementMaterial.Key)
			{
				invalidPlacementMaterial.Key.materials = invalidPlacementMaterial.Value;
			}
		}
		m_invalidPlacementMaterials = null;
	}

	public static void GetSnapPoints(Vector3 point, float radius, List<Transform> points, List<Piece> pieces)
	{
		int num = Physics.OverlapSphereNonAlloc(point, radius, s_pieceColliders, s_pieceRayMask);
		for (int i = 0; i < num; i++)
		{
			Piece componentInParent = s_pieceColliders[i].GetComponentInParent<Piece>();
			if (componentInParent != null)
			{
				componentInParent.GetSnapPoints(points);
				pieces.Add(componentInParent);
			}
		}
	}

	public static void GetAllPiecesInRadius(Vector3 p, float radius, List<Piece> pieces)
	{
		foreach (Piece s_allPiece in s_allPieces)
		{
			if (s_allPiece.gameObject.layer != s_ghostLayer && Vector3.Distance(p, s_allPiece.transform.position) < radius)
			{
				pieces.Add(s_allPiece);
			}
		}
	}

	public static void GetAllComfortPiecesInRadius(Vector3 p, float radius, List<Piece> pieces)
	{
		foreach (Piece s_allComfortPiece in s_allComfortPieces)
		{
			if (s_allComfortPiece.gameObject.layer != s_ghostLayer && Vector3.Distance(p, s_allComfortPiece.transform.position) < radius)
			{
				pieces.Add(s_allComfortPiece);
			}
		}
	}

	public void GetSnapPoints(List<Transform> points)
	{
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			if (child.CompareTag("snappoint"))
			{
				points.Add(child);
			}
		}
	}

	public int GetComfort()
	{
		if (m_comfortObject != null && !m_comfortObject.activeInHierarchy)
		{
			return 0;
		}
		return m_comfort;
	}
}
