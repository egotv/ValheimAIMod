using System.Collections.Generic;
using UnityEngine;

public class StationExtension : MonoBehaviour, Hoverable
{
	public CraftingStation m_craftingStation;

	public float m_maxStationDistance = 5f;

	public bool m_stack;

	public GameObject m_connectionPrefab;

	public Vector3 m_connectionOffset = new Vector3(0f, 0f, 0f);

	public bool m_continousConnection;

	private GameObject m_connection;

	private Piece m_piece;

	private static List<StationExtension> m_allExtensions = new List<StationExtension>();

	private void Awake()
	{
		if (GetComponent<ZNetView>().GetZDO() != null)
		{
			m_piece = GetComponent<Piece>();
			m_allExtensions.Add(this);
			if (m_continousConnection)
			{
				InvokeRepeating("UpdateConnection", 1f, 4f);
			}
		}
	}

	private void OnDestroy()
	{
		if ((bool)m_connection)
		{
			Object.Destroy(m_connection);
			m_connection = null;
		}
		m_allExtensions.Remove(this);
	}

	public string GetHoverText()
	{
		if (!m_continousConnection)
		{
			PokeEffect();
		}
		return Localization.instance.Localize(m_piece.m_name);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(m_piece.m_name);
	}

	private string GetExtensionName()
	{
		return m_piece.m_name;
	}

	public static void FindExtensions(CraftingStation station, Vector3 pos, List<StationExtension> extensions)
	{
		foreach (StationExtension allExtension in m_allExtensions)
		{
			if (Vector3.Distance(allExtension.transform.position, pos) < allExtension.m_maxStationDistance && allExtension.m_craftingStation.m_name == station.m_name && (allExtension.m_stack || !ExtensionInList(extensions, allExtension)))
			{
				extensions.Add(allExtension);
			}
		}
	}

	private static bool ExtensionInList(List<StationExtension> extensions, StationExtension extension)
	{
		foreach (StationExtension extension2 in extensions)
		{
			if (extension2.GetExtensionName() == extension.GetExtensionName())
			{
				return true;
			}
		}
		return false;
	}

	public bool OtherExtensionInRange(float radius)
	{
		foreach (StationExtension allExtension in m_allExtensions)
		{
			if (!(allExtension == this) && Vector3.Distance(allExtension.transform.position, base.transform.position) < radius)
			{
				return true;
			}
		}
		return false;
	}

	public List<CraftingStation> FindStationsInRange(Vector3 center)
	{
		List<CraftingStation> list = new List<CraftingStation>();
		CraftingStation.FindStationsInRange(m_craftingStation.m_name, center, m_maxStationDistance, list);
		return list;
	}

	public CraftingStation FindClosestStationInRange(Vector3 center)
	{
		return CraftingStation.FindClosestStationInRange(m_craftingStation.m_name, center, m_maxStationDistance);
	}

	private void UpdateConnection()
	{
		PokeEffect(5f);
	}

	private void PokeEffect(float timeout = 1f)
	{
		CraftingStation craftingStation = FindClosestStationInRange(base.transform.position);
		if ((bool)craftingStation)
		{
			StartConnectionEffect(craftingStation, timeout);
		}
	}

	public void StartConnectionEffect(CraftingStation station, float timeout = 1f)
	{
		StartConnectionEffect(station.GetConnectionEffectPoint(), timeout);
	}

	public void StartConnectionEffect(Vector3 targetPos, float timeout = 1f)
	{
		Vector3 connectionPoint = GetConnectionPoint();
		if (m_connection == null)
		{
			m_connection = Object.Instantiate(m_connectionPrefab, connectionPoint, Quaternion.identity);
		}
		Vector3 vector = targetPos - connectionPoint;
		Quaternion rotation = Quaternion.LookRotation(vector.normalized);
		m_connection.transform.position = connectionPoint;
		m_connection.transform.rotation = rotation;
		m_connection.transform.localScale = new Vector3(1f, 1f, vector.magnitude);
		CancelInvoke("StopConnectionEffect");
		Invoke("StopConnectionEffect", timeout);
	}

	public void StopConnectionEffect()
	{
		if ((bool)m_connection)
		{
			Object.Destroy(m_connection);
			m_connection = null;
		}
	}

	private Vector3 GetConnectionPoint()
	{
		return base.transform.TransformPoint(m_connectionOffset);
	}

	private void OnDrawGizmos()
	{
	}
}
