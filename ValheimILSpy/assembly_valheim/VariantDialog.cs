using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VariantDialog : MonoBehaviour
{
	public Transform m_listRoot;

	public GameObject m_elementPrefab;

	public float m_spacing = 70f;

	public int m_gridWidth = 5;

	private List<GameObject> m_elements = new List<GameObject>();

	public Action<int> m_selected;

	public void Setup(ItemDrop.ItemData item)
	{
		base.gameObject.SetActive(value: true);
		foreach (GameObject element in m_elements)
		{
			UnityEngine.Object.Destroy(element);
		}
		m_elements.Clear();
		for (int i = 0; i < item.m_shared.m_variants; i++)
		{
			Sprite sprite = item.m_shared.m_icons[i];
			int num = i / m_gridWidth;
			int num2 = i % m_gridWidth;
			GameObject gameObject = UnityEngine.Object.Instantiate(m_elementPrefab, Vector3.zero, Quaternion.identity, m_listRoot);
			gameObject.SetActive(value: true);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2((float)num2 * m_spacing, (float)(-num) * m_spacing);
			Button component = gameObject.transform.Find("Button").GetComponent<Button>();
			int buttonIndex = i;
			component.onClick.AddListener(delegate
			{
				OnClicked(buttonIndex);
			});
			component.GetComponent<Image>().sprite = sprite;
			m_elements.Add(gameObject);
		}
	}

	public void OnClose()
	{
		base.gameObject.SetActive(value: false);
	}

	private void OnClicked(int index)
	{
		ZLog.Log("Clicked button " + index);
		base.gameObject.SetActive(value: false);
		m_selected(index);
	}
}
