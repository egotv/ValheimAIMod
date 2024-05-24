using System;
using System.Collections.Generic;
using UnityEngine;

public class Vine : MonoBehaviour
{
	[Flags]
	public enum VineState
	{
		None = 0,
		ClosedLeft = 1,
		ClosedRight = 2,
		ClosedTop = 4,
		ClosedBottom = 8,
		BranchLeft = 0x10,
		BranchRight = 0x20,
		BranchTop = 0x40,
		BranchBottom = 0x80
	}

	private enum VineType
	{
		Full,
		Left,
		Right,
		Top,
		Bottom
	}

	private static int s_pieceMask;

	private static int s_solidMask;

	[Header("Grow Settings")]
	[Tooltip("Chance that a grow check is run on each open branch, which can result in a chance to grow or chance to close.")]
	[Range(0f, 1f)]
	public float m_growCheckChance = 0.3f;

	[Tooltip("Grow check decreases by this amount for each near branch.")]
	[Range(-1f, 0f)]
	public float m_growCheckChancePerBranch = -0.75f;

	[Tooltip("Chance that a possible branch will actually grow")]
	[Range(0f, 1f)]
	public float m_growChance = 0.5f;

	[Tooltip("At what interval the GrowCheck function will repeat.")]
	public int m_growCheckTime = 5;

	[Tooltip("Seconds it will take between each attempt to grow.")]
	public float m_growTime = 3f;

	[Tooltip("Extra seconds it will take between each attempt to grow for each branch connected to it.")]
	public float m_growTimePerBranch = 2f;

	[Tooltip("Chance that a branch will close after during a grow check.")]
	[Range(0f, 1f)]
	public float m_closeEndChance;

	[Tooltip("Close chance increases by this amount for each near branch.")]
	[Range(0f, 1f)]
	public float m_closeEndChancePerBranch = 0.1f;

	[Tooltip("Close chance increases by this amount for each height.")]
	[Range(0f, 1f)]
	public float m_closeEndChancePerHeight = 0.2f;

	[Tooltip("Close chance will never go above this. (Also will never be closed unless there is atleast one grown branch.")]
	[Range(0f, 1f)]
	public float m_maxCloseEndChance = 0.9f;

	public float m_maxGrowUp = 1000f;

	public float m_maxGrowDown = -2f;

	[Tooltip("Grow width limitation")]
	public float m_maxGrowWidth = 3f;

	[Tooltip("Extra grow width limitation per height")]
	public float m_extraGrowWidthPerHeight = 0.5f;

	[Tooltip("Chance to ignore width limitaion.")]
	[Range(0f, 1f)]
	public float m_maxGrowEdgeIgnoreChance = 0.2f;

	[Tooltip("Vine will grow this many itterations upon placement. Used for locations for instant growing. Test with console 'test quickvine 300'")]
	public int m_initialGrowItterations;

	public int m_forceSeed;

	public float m_size = 1.5f;

	[Tooltip("At least this much % of the colliders must find support to be able to grow.")]
	[Range(0f, 1f)]
	public float m_growCollidersMinimum = 0.75f;

	public bool m_growSides = true;

	public bool m_growUp = true;

	public bool m_growDown;

	public float m_minScale = 1f;

	public float m_maxScale = 1f;

	public float m_randomOffset = 0.1f;

	[Header("Berries")]
	public int m_maxBerriesWithinBlocker = 2;

	public BoxCollider m_berryBlocker;

	[Header("Prefabs")]
	public GameObject m_vinePrefab;

	public GameObject m_vineFull;

	public GameObject m_vineTop;

	public GameObject m_vineBottom;

	public GameObject m_vineLeft;

	public GameObject m_vineRight;

	public BoxCollider m_sensorGrow;

	public BoxCollider m_supportCollider;

	public List<BoxCollider> m_sensorGrowColliders;

	public GameObject m_sensorBlock;

	public BoxCollider m_sensorBlockCollider;

	public BoxCollider m_placementCollider;

	[Header("Testing")]
	public int m_testItterations = 10;

	[NonSerialized]
	public VineState m_vineState;

	private Pickable m_pickable;

	private bool m_lastPickable;

	private long m_plantTime;

	private long m_lastGrow;

	private ZNetView m_nview;

	private VineType m_vineType;

	private int m_branches;

	private Vector3 m_originOffset;

	private System.Random m_rnd;

	private bool m_dupeCheck;

	private static Collider[] s_colliders = new Collider[20];

	private static List<Vine> s_vines = new List<Vine>();

	private static List<Vine> s_allVines = new List<Vine>();

	public bool IsDoneGrowing { get; private set; }

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_pickable = GetComponent<Pickable>();
		Pickable pickable = m_pickable;
		pickable.m_spawnCheck = (Pickable.SpawnCheck)Delegate.Combine(pickable.m_spawnCheck, new Pickable.SpawnCheck(CanSpawnPickable));
		GetRandom();
		s_allVines.Add(this);
		if (s_pieceMask == 0)
		{
			s_pieceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece");
		}
		if (s_solidMask == 0)
		{
			s_solidMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
		}
		InvokeRepeating("CheckSupport", 10 + UnityEngine.Random.Range(0, 7), 7f);
		InvokeRepeating("UpdateGrow", UnityEngine.Random.Range(5, 10 + m_growCheckTime), m_growCheckTime);
		IsDoneGrowing = IsDone();
		if (m_nview.IsOwner())
		{
			m_plantTime = m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, 0L);
			if (m_plantTime == 0L)
			{
				long ticks = ZNet.instance.GetTime().Ticks;
				m_nview.GetZDO().Set(ZDOVars.s_plantTime, m_plantTime = ticks);
			}
			m_vineType = (VineType)m_nview.GetZDO().GetInt(ZDOVars.s_type);
		}
		UpdateType();
		CheckBerryBlocker();
		if (Terminal.m_testList.TryGetValue("quickvine", out var value) && int.TryParse(value, out var result))
		{
			m_initialGrowItterations = result;
		}
	}

	private void GetRandom()
	{
		if (m_rnd == null)
		{
			m_rnd = new System.Random((m_forceSeed != 0) ? (m_forceSeed + GetOriginOffset().GetHashCode()) : UnityEngine.Random.Range(int.MinValue, int.MaxValue));
		}
	}

	public void Update()
	{
		if (m_initialGrowItterations > 0 && (bool)m_nview && m_nview.IsOwner())
		{
			for (int i = 0; i < Mathf.Min(m_initialGrowItterations, 25); i++)
			{
				if (UpdateGrow())
				{
					m_initialGrowItterations--;
					break;
				}
				m_initialGrowItterations--;
			}
		}
		bool flag = m_pickable.CanBePicked();
		if (flag != m_lastPickable)
		{
			BerryBlockNeighbours();
			m_lastPickable = flag;
		}
	}

	public bool UpdateGrow()
	{
		if (!m_nview || !m_nview.IsOwner())
		{
			return false;
		}
		if (!m_dupeCheck)
		{
			m_dupeCheck = true;
			foreach (Vine s_allVine in s_allVines)
			{
				if (s_allVine != this && (bool)s_allVine && Vector3.Distance(s_allVine.transform.position, base.transform.position) < 0.01f)
				{
					ZNetScene.instance.Destroy(base.gameObject);
					return false;
				}
			}
		}
		if ((IsDoneGrowing || IsDone()) && m_initialGrowItterations > 0)
		{
			m_initialGrowItterations = 0;
		}
		long num;
		long num2;
		if (m_initialGrowItterations <= 0)
		{
			num = (ZNet.instance ? ZNet.instance.GetTime().Ticks : 0);
			if ((bool)m_nview)
			{
				ZDO zDO = m_nview.GetZDO();
				if (zDO != null)
				{
					long @long = zDO.GetLong(ZDOVars.s_growStart, 0L);
					num2 = @long;
					goto IL_012c;
				}
			}
			num2 = 0L;
			goto IL_012c;
		}
		goto IL_01b6;
		IL_01b6:
		if ((bool)m_nview && m_nview.IsOwner() && m_nview.IsValid())
		{
			m_nview.GetZDO().Set(ZDOVars.s_growStart, ZNet.instance.GetTime().Ticks);
		}
		m_originOffset = GetOriginOffset();
		int num3 = 0;
		bool flag = m_rnd.NextDouble() < (double)m_maxGrowEdgeIgnoreChance;
		if (m_growSides)
		{
			float num4 = m_maxGrowWidth + m_originOffset.y * m_extraGrowWidthPerHeight;
			if (flag || m_originOffset.z + 1f <= num4)
			{
				num3 += CheckGrowChance(new Vector3(0f, 0f, 1f), VineType.Left, VineState.ClosedLeft, VineState.BranchLeft, VineState.BranchRight);
			}
			else
			{
				m_vineState |= VineState.ClosedLeft;
			}
			if (flag || Mathf.Abs(m_originOffset.z) + 1f <= num4)
			{
				num3 += CheckGrowChance(new Vector3(0f, 0f, -1f), VineType.Right, VineState.ClosedRight, VineState.BranchRight, VineState.BranchLeft);
			}
			else
			{
				m_vineState |= VineState.ClosedRight;
			}
		}
		if (m_growUp)
		{
			if (flag || m_originOffset.y + 1f <= m_maxGrowUp)
			{
				num3 += CheckGrowChance(new Vector3(0f, 1f, 0f), VineType.Top, VineState.ClosedTop, VineState.BranchTop, VineState.BranchBottom);
			}
			else
			{
				m_vineState |= VineState.ClosedTop;
			}
		}
		if (m_growDown)
		{
			if (flag || m_originOffset.y - 1f >= m_maxGrowDown)
			{
				num3 += CheckGrowChance(new Vector3(0f, -1f, 0f), VineType.Bottom, VineState.ClosedBottom, VineState.BranchBottom, VineState.BranchTop);
			}
			else
			{
				m_vineState |= VineState.ClosedBottom;
			}
		}
		IsDoneGrowing = IsDone();
		return num3 > 0;
		IL_012c:
		long num5 = num2;
		int num6 = (int)((float)((num - ((m_plantTime > num5) ? m_plantTime : num5)) / 10000000) / (m_growTime + m_growTimePerBranch * (float)GetBranches()));
		if (num6 < 1)
		{
			return false;
		}
		if (num6 >= 2 && num6 > m_initialGrowItterations)
		{
			m_initialGrowItterations = num6 - 1;
			if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
			{
				Terminal.Log($"Vine is queuing {m_initialGrowItterations} itterations to catch up!");
			}
		}
		goto IL_01b6;
	}

	private int CheckGrowChance(Vector3 offset, VineType type, VineState closed, VineState branch, VineState branchFrom)
	{
		if (m_rnd.NextDouble() < (double)(m_growCheckChance + m_growCheckChancePerBranch * (float)GetBranches()))
		{
			return CheckGrow(offset, type, closed, branch, branchFrom);
		}
		return 0;
	}

	private int CheckGrow(Vector3 offset, VineType type, VineState closed, VineState branch, VineState branchFrom)
	{
		if (m_vineState.HasFlag(branch))
		{
			return 0;
		}
		if (m_vineState.HasFlag(closed) && m_rnd.NextDouble() > 0.4000000059604645)
		{
			return 0;
		}
		if (m_rnd.NextDouble() < (double)Mathf.Min(m_maxCloseEndChance, m_closeEndChance + m_closeEndChancePerBranch * (float)GetBranches() + m_closeEndChancePerHeight * GetOriginOffset().y))
		{
			if (GetBranches() > 1)
			{
				m_vineState |= closed;
			}
			return 0;
		}
		Vector3 originOffset = offset;
		if (m_randomOffset != 0f)
		{
			switch (type)
			{
			case VineType.Left:
			case VineType.Right:
				offset.y += UnityEngine.Random.Range(0f - m_randomOffset, m_randomOffset);
				break;
			case VineType.Top:
			case VineType.Bottom:
				offset.z += UnityEngine.Random.Range(0f - m_randomOffset, m_randomOffset);
				break;
			}
		}
		m_sensorBlockCollider.transform.localPosition = Vector3.zero;
		m_sensorBlockCollider.transform.Translate(offset * m_size + m_sensorBlockCollider.center, base.transform);
		int num = Physics.OverlapBoxNonAlloc(m_sensorBlockCollider.transform.position, m_sensorBlockCollider.size / 2f, s_colliders, m_sensorBlockCollider.transform.rotation, s_solidMask);
		for (int i = 0; i < num; i++)
		{
			Vine componentInParent = s_colliders[i].GetComponentInParent<Vine>();
			if (componentInParent == this)
			{
				continue;
			}
			if (componentInParent != null)
			{
				if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
				{
					Terminal.Log("Blocked by vine, count it as a branch (green box)");
				}
				m_vineState |= branch;
				UpdateBranches();
				return 0;
			}
			if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
			{
				Terminal.Log("Blocked by piece (red box)");
			}
			m_vineState |= closed;
			UpdateBranches();
			return 0;
		}
		m_sensorGrow.transform.localPosition = Vector3.zero;
		m_sensorGrow.transform.Translate(offset * m_size + m_sensorGrow.center, base.transform);
		int num2 = 0;
		foreach (BoxCollider sensorGrowCollider in m_sensorGrowColliders)
		{
			num = Physics.OverlapBoxNonAlloc(m_sensorGrow.transform.position, sensorGrowCollider.size / 2f, s_colliders, m_sensorGrow.transform.rotation, s_pieceMask);
			if (num > 0)
			{
				num2++;
			}
		}
		if ((float)num2 >= (float)m_sensorGrowColliders.Count * m_growCollidersMinimum && m_rnd.NextDouble() < (double)m_growChance)
		{
			Grow(offset, originOffset, type, branch, branchFrom);
			return 1;
		}
		return 0;
	}

	private void CheckSupport()
	{
		if (!IsSupported())
		{
			Destructible component = GetComponent<Destructible>();
			if ((object)component != null)
			{
				component.m_destroyedEffect.Create(base.transform.position, base.transform.rotation);
			}
			else
			{
				GetComponent<WearNTear>()?.m_destroyedEffect.Create(base.transform.position, base.transform.rotation);
			}
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}

	private bool IsSupported()
	{
		return Physics.OverlapBoxNonAlloc(base.transform.TransformPoint(m_supportCollider.center), m_supportCollider.size / 2f, s_colliders, m_supportCollider.transform.rotation, s_pieceMask) > 0;
	}

	private int GetBranches()
	{
		return Mathf.Max(0, m_branches - 1);
	}

	private void Grow(Vector3 offset, Vector3 originOffset, VineType type, VineState branch, VineState growingFrom)
	{
		if (m_vineType != 0)
		{
			SetType(VineType.Full);
		}
		Vine component = UnityEngine.Object.Instantiate(m_vinePrefab, base.transform.position, base.transform.rotation).GetComponent<Vine>();
		component.transform.Translate(offset * m_size, Space.Self);
		component.SetType(type);
		component.name = "VineAsh";
		component.m_vineState |= growingFrom;
		component.m_initialGrowItterations = m_initialGrowItterations - 1;
		component.m_pickable.m_respawnTimeInitMin += m_initialGrowItterations;
		component.m_pickable.m_respawnTimeInitMax += m_initialGrowItterations;
		m_originOffset = GetOriginOffset();
		component.SetOriginOffset(m_originOffset + originOffset);
		if (m_maxScale != 1f || m_minScale != 1f)
		{
			float num = m_minScale + (float)(m_rnd.NextDouble() * (double)(m_maxScale - m_minScale));
			component.transform.localScale = new Vector3(num, num, num);
		}
		if ((bool)component.m_nview && component.m_nview.IsOwner() && component.m_nview.IsValid())
		{
			component.m_nview.GetZDO().Set(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks);
		}
	}

	private void SetType(VineType type)
	{
		m_vineType = type;
		if ((bool)m_nview && m_nview.IsOwner() && m_nview.IsValid())
		{
			m_nview.GetZDO().Set(ZDOVars.s_type, (int)type);
		}
		UpdateType();
	}

	public void SetOriginOffset(Vector3 offset)
	{
		m_originOffset = offset;
		if ((bool)m_nview && m_nview.IsValid())
		{
			m_nview.GetZDO().Set(ZDOVars.s_offset, m_originOffset);
		}
	}

	public Vector3 GetOriginOffset()
	{
		if (m_originOffset == Vector3.zero && (bool)m_nview && m_nview.IsValid())
		{
			m_originOffset = m_nview.GetZDO().GetVec3(ZDOVars.s_offset, Vector3.zero);
		}
		return m_originOffset;
	}

	private void UpdateType()
	{
		m_vineFull.SetActive(m_vineType == VineType.Full);
		m_vineLeft.SetActive(m_vineType == VineType.Left);
		m_vineRight.SetActive(m_vineType == VineType.Right);
		m_vineTop.SetActive(m_vineType == VineType.Top);
		m_vineBottom.SetActive(m_vineType == VineType.Bottom);
	}

	private void UpdateBranches()
	{
		m_branches = 0;
		if (m_vineState.HasFlag(VineState.BranchLeft))
		{
			m_branches++;
		}
		if (m_vineState.HasFlag(VineState.BranchRight))
		{
			m_branches++;
		}
		if (m_vineState.HasFlag(VineState.BranchTop))
		{
			m_branches++;
		}
		if (m_vineState.HasFlag(VineState.BranchBottom))
		{
			m_branches++;
		}
	}

	private void BerryBlockNeighbours()
	{
		if (!m_nview || !m_nview.IsOwner())
		{
			return;
		}
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
		{
			Terminal.Log("Vine pickable changed, blocking neighbors");
		}
		int num = Physics.OverlapBoxNonAlloc(base.transform.TransformPoint(m_berryBlocker.center), m_berryBlocker.size / 2f, s_colliders, m_berryBlocker.transform.rotation, s_solidMask);
		s_vines.Clear();
		for (int i = 0; i < num; i++)
		{
			Vine componentInParent = s_colliders[i].GetComponentInParent<Vine>();
			if ((object)componentInParent != null)
			{
				s_vines.Add(componentInParent);
			}
		}
		foreach (Vine s_vine in s_vines)
		{
			s_vine.CheckBerryBlocker();
		}
	}

	private bool CanSpawnPickable(Pickable p)
	{
		return CheckBerryBlocker();
	}

	private bool CheckBerryBlocker()
	{
		if (!m_nview || !m_nview.IsOwner())
		{
			return true;
		}
		if (m_pickable.CanBePicked())
		{
			return true;
		}
		int num = Physics.OverlapBoxNonAlloc(base.transform.TransformPoint(m_berryBlocker.center), m_berryBlocker.size / 2f, s_colliders, m_berryBlocker.transform.rotation, s_solidMask);
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			Vine componentInParent = s_colliders[i].GetComponentInParent<Vine>();
			if ((object)componentInParent != null && componentInParent.m_pickable.CanBePicked())
			{
				num2++;
			}
		}
		UpdateBranches();
		bool flag = num2 < m_maxBerriesWithinBlocker && GetBranches() > 0;
		if (!flag)
		{
			m_pickable.SetPicked(picked: true);
		}
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("vine"))
		{
			Terminal.Log($"Vine checking berry blockers. Berries: {num2}, Pickable: {flag}");
		}
		return flag;
	}

	private bool IsDone()
	{
		if ((m_vineState.HasFlag(VineState.ClosedLeft) || m_vineState.HasFlag(VineState.BranchLeft)) && (m_vineState.HasFlag(VineState.ClosedRight) || m_vineState.HasFlag(VineState.BranchRight)))
		{
			if (!m_vineState.HasFlag(VineState.ClosedTop))
			{
				return m_vineState.HasFlag(VineState.BranchTop);
			}
			return true;
		}
		return false;
	}

	private void OnDestroy()
	{
		s_allVines.Remove(this);
	}
}
