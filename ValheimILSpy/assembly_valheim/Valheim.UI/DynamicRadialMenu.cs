using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.UI;

public class DynamicRadialMenu : MonoBehaviour
{
	[SerializeField]
	protected RectTransform m_elementContainer;

	[SerializeField]
	protected RadialLayout m_layout;

	[SerializeField]
	protected RectTransform m_marker;

	[SerializeField]
	protected float m_markerDistance = 150f;

	[SerializeField]
	[Range(0f, 1f)]
	protected float m_inertia;

	[SerializeField]
	protected float m_interactionDelay;

	[SerializeField]
	protected float m_noInteractionDelay;

	[SerializeField]
	protected float m_deadzone;

	[SerializeField]
	protected int m_maxElementsPerPage = 8;

	[SerializeField]
	protected GameObject m_pagination;

	[SerializeField]
	protected Image m_pageDot;

	[SerializeField]
	protected Color m_pageSelectedColor;

	[SerializeField]
	protected Color m_pageUnselectedColor;

	[SerializeField]
	protected BackElement m_backPrefab;

	[SerializeField]
	protected EmptyElement m_emptyPrefab;

	[SerializeField]
	protected ElementInfo m_elementInfo;

	private float m_lastX;

	private float m_lastY;

	protected IRadialConfig m_backConfig;

	protected IRadialConfig m_currentConfig;

	protected List<Image> m_pageDots;

	protected int m_currentPage;

	protected Stack<int> m_lastPages = new Stack<int>();

	protected RadialMenuElement[] m_elements;

	protected RadialMenuElement m_selected;

	private int currentLevel;

	public Func<Vector2, Vector2> GetXY { get; set; }

	public Func<bool> GetNext { get; set; }

	public Func<bool> GetPrevious { get; set; }

	public Func<bool> GetConfirm { get; set; }

	public Func<bool> GetSecondaryConfirm { get; set; }

	public Func<bool> GetBack { get; set; }

	public Func<bool> GetClose { get; set; }

	public Action<float> OnInteractionDelay { get; set; }

	public bool CanOpen { get; set; } = true;


	public float Inertia => m_inertia;

	public bool IsTopLevel => m_backConfig == null;

	public PageMode PageMode { get; set; }

	public int MaxElementsPerPage
	{
		get
		{
			return m_maxElementsPerPage;
		}
		set
		{
			m_maxElementsPerPage = value;
		}
	}

	public bool Active
	{
		get
		{
			return base.gameObject.activeSelf;
		}
		set
		{
			base.gameObject.SetActive(value);
			m_elementInfo.Clear();
			m_marker.gameObject.SetActive(value: false);
			m_lastX = (m_lastY = 0f);
		}
	}

	private int Pages
	{
		get
		{
			if (m_elements != null && m_elements.Length > m_maxElementsPerPage)
			{
				int num = m_elements.Length / m_maxElementsPerPage;
				if (m_elements.Length % m_maxElementsPerPage > 0)
				{
					num++;
				}
				return num;
			}
			return 1;
		}
	}

	public void Open(IRadialConfig config, IRadialConfig backConfig = null)
	{
		if (CanOpen)
		{
			if (backConfig != null)
			{
				m_lastPages.Push(m_currentPage);
			}
			if (m_elementContainer.childCount > 0)
			{
				ClearElements();
			}
			config?.SetRadial(this, 0);
			m_currentConfig = config;
			m_backConfig = backConfig;
			Active = true;
		}
	}

	public void SetElements(RadialMenuElement[] elements, int page, bool addBackButton, bool fillUp)
	{
		ClearElements();
		List<RadialMenuElement> list = new List<RadialMenuElement>();
		if (addBackButton)
		{
			BackElement backElement = UnityEngine.Object.Instantiate(m_backPrefab);
			backElement.Init(this);
			list.Add(backElement);
		}
		if (elements != null)
		{
			list.AddRange(elements);
		}
		if (list.Count < MaxElementsPerPage && fillUp && (list.Count == 0 || list.Count % MaxElementsPerPage > 0))
		{
			for (int i = list.Count % MaxElementsPerPage; i < MaxElementsPerPage; i++)
			{
				EmptyElement emptyElement = UnityEngine.Object.Instantiate(m_emptyPrefab);
				emptyElement.Init();
				list.Add(emptyElement);
			}
		}
		PageMode = ((list.Count <= MaxElementsPerPage) ? PageMode.Pages : PageMode.Default);
		m_elements = list.ToArray();
		for (int j = 0; j < m_elements.Length; j++)
		{
			m_elements[j].transform.SetParent(m_elementContainer);
			m_elements[j].transform.localScale = Vector3.one;
		}
		if (PageMode == PageMode.Pages)
		{
			while (m_pageDots.Count < Pages)
			{
				Image image = UnityEngine.Object.Instantiate(m_pageDot, m_pagination.transform);
				image.transform.SetSiblingIndex(image.transform.GetSiblingIndex() - 1);
				m_pageDots.Add(image);
			}
			SetPage((Pages > page) ? page : 0);
		}
		else
		{
			SetSpiral();
		}
	}

	protected void Awake()
	{
		Active = false;
		m_pageDots = new List<Image> { m_pageDot };
	}

	protected void Update()
	{
		if (m_elements != null && Active)
		{
			if (m_selected != null)
			{
				m_elementInfo.Set(m_selected);
			}
			else
			{
				m_elementInfo.Set(m_currentConfig);
			}
			HandleInput();
		}
	}

	private void HandleInput()
	{
		Func<bool> getNext = GetNext;
		if (getNext != null && getNext())
		{
			OnNextPage();
		}
		Func<bool> getPrevious = GetPrevious;
		if (getPrevious != null && getPrevious())
		{
			OnPreviousPage();
		}
		Func<bool> getSecondaryConfirm = GetSecondaryConfirm;
		if (getSecondaryConfirm != null && getSecondaryConfirm())
		{
			Interact(closeOnInteract: false);
		}
		else
		{
			Func<bool> getConfirm = GetConfirm;
			if (getConfirm != null && getConfirm())
			{
				Interact();
			}
		}
		Func<bool> getBack = GetBack;
		if (getBack != null && getBack())
		{
			Back();
		}
		Func<bool> getClose = GetClose;
		if (getClose != null && getClose())
		{
			Close();
			OnInteractionDelay?.Invoke(m_noInteractionDelay);
		}
		Vector2 vector = new Vector2(m_lastX, m_lastY);
		Vector2 vector2 = ((GetXY != null) ? Vector2.Lerp(vector, GetXY(vector), m_inertia) : Vector2.zero);
		m_lastX = vector2.x;
		m_lastY = vector2.y;
		if (Mathf.Abs(m_lastX) > m_deadzone || Mathf.Abs(m_lastY) > m_deadzone)
		{
			Vector2 normalized = new Vector2(m_lastX, m_lastY).normalized;
			(float, float) tuple = UIMath.CartToPolar(m_lastY, m_lastX);
			float item = tuple.Item1;
			float item2 = tuple.Item2;
			float deg = item * 57.29578f;
			UpdateMarker(normalized, deg);
			if (PageMode == PageMode.Pages)
			{
				UpdateSelection(item2, deg);
			}
			else
			{
				UpdateSpiralSelection(item2, deg);
			}
		}
		else
		{
			HideMarker();
			UnselectAll();
		}
	}

	private void UpdateSelection(float rad, float deg)
	{
		int startIndex = GetStartIndex(m_currentPage);
		int endIndex = GetEndIndex(m_currentPage);
		int num = endIndex - startIndex + 1;
		float num2 = 360f / (float)num;
		float num3 = num2 / 2f;
		float num4 = (m_layout.Clockwise ? (360f - (0f - deg + 450f - num3 - m_layout.StartAngle) % 360f) : ((0f - deg + 450f + num3 - m_layout.StartAngle) % 360f));
		for (int i = startIndex; i <= endIndex; i++)
		{
			float num5 = (float)(i - startIndex) * num2;
			float num6 = (float)(i - startIndex + 1) * num2;
			if (num4 >= num5 && num4 < num6 && (double)rad > 0.1)
			{
				m_elements[i].Selected = true;
				m_elementInfo.Set(m_elements[i]);
				m_selected = m_elements[i];
			}
			else
			{
				m_elements[i].Selected = false;
			}
		}
	}

	private void UpdateSpiralSelection(float rad, float deg)
	{
		float num = 360f / (float)m_maxElementsPerPage;
		float num2 = num / 2f;
		float num3 = (m_layout.Clockwise ? (360f - (0f - deg + 450f - num2 - m_layout.StartAngle) % 360f) : ((0f - deg + 450f + num2 - m_layout.StartAngle) % 360f));
		int num4 = -1;
		if ((double)rad > 0.1)
		{
			num4 = (int)(num3 / num);
		}
		for (int i = 0; i < m_elements.Length; i++)
		{
			int num5 = i / m_maxElementsPerPage;
			float num6 = (float)(i % m_maxElementsPerPage) * num;
			float num7 = (float)(i % m_maxElementsPerPage + 1) * num;
			if (currentLevel == num5)
			{
				if (num4 > -1)
				{
					float num8 = Vector3.Distance(m_elements[i].transform.position, m_marker.transform.position);
					m_elements[i].Alpha = (m_layout.Radius * 1.5f - num8) / (m_layout.Radius * 1.5f);
				}
				else
				{
					m_elements[i].Alpha = 1f;
				}
				if (i == num4)
				{
					m_elements[i].Selected = true;
					m_elementInfo.Set(m_elements[i]);
					m_selected = m_elements[i];
				}
				else
				{
					m_elements[i].Selected = false;
				}
			}
			else
			{
				m_elements[i].Selected = false;
				m_elements[i].Alpha = 0f;
			}
		}
	}

	private void UnselectAll()
	{
		m_selected = null;
		RadialMenuElement[] elements = m_elements;
		for (int i = 0; i < elements.Length; i++)
		{
			elements[i].Selected = false;
		}
	}

	private void HideMarker()
	{
		m_marker.localPosition = Vector3.zero;
		m_marker.localRotation = Quaternion.identity;
		m_marker.gameObject.SetActive(value: false);
	}

	private void UpdateMarker(Vector2 direction, float deg)
	{
		m_marker.gameObject.SetActive(value: true);
		m_marker.localPosition = direction * m_markerDistance;
		m_marker.localRotation = Quaternion.Euler(0f, 0f, 0f - deg);
	}

	private void SetSpiral()
	{
		m_pagination.SetActive(value: false);
		for (int i = 0; i < m_elements.Length; i++)
		{
			m_elements[i].Alpha = ((i >= currentLevel * MaxElementsPerPage && i < (currentLevel + 1) * MaxElementsPerPage) ? 1 : 0);
			if (m_layout.Clockwise)
			{
				float offset = (float)(i % MaxElementsPerPage) / (float)MaxElementsPerPage;
				m_elements[i].SetSegement(offset, MaxElementsPerPage);
			}
			else
			{
				float offset2 = (float)((m_elements.Length - i) % MaxElementsPerPage) / (float)MaxElementsPerPage;
				m_elements[i].SetSegement(offset2, MaxElementsPerPage);
			}
		}
		m_layout.MaxElements = MaxElementsPerPage;
		m_layout.CalculateRadial();
	}

	private void SetPage(int page)
	{
		m_pagination.SetActive(Pages > 1);
		m_currentPage = page;
		for (int i = 0; i < m_pageDots.Count; i++)
		{
			m_pageDots[i].color = ((page == i) ? m_pageSelectedColor : m_pageUnselectedColor);
			m_pageDots[i].gameObject.SetActive(i < Pages);
		}
		int startIndex = GetStartIndex(page);
		int endIndex = GetEndIndex(page);
		int num = endIndex - startIndex + 1;
		if (startIndex > 0)
		{
			for (int j = 0; j < startIndex; j++)
			{
				m_elements[j].gameObject.SetActive(value: false);
			}
		}
		if (endIndex < m_elements.Length - 1)
		{
			for (int k = endIndex + 1; k < m_elements.Length; k++)
			{
				m_elements[k].gameObject.SetActive(value: false);
			}
		}
		for (int l = startIndex; l <= endIndex; l++)
		{
			m_elements[l].gameObject.SetActive(value: true);
			if (m_layout.Clockwise)
			{
				m_elements[l].SetSegement(l - startIndex, num);
			}
			else
			{
				m_elements[l].SetSegement(num - (l - startIndex), num);
			}
		}
		m_layout.MaxElements = MaxElementsPerPage;
		m_layout.CalculateRadial();
	}

	private void OnNextPage()
	{
		int page = (m_currentPage + 1) % Pages;
		SetPage(page);
	}

	private void OnPreviousPage()
	{
		int page = (m_currentPage - 1 + Pages) % Pages;
		SetPage(page);
	}

	private int GetStartIndex(int page)
	{
		if (m_elements.Length <= m_maxElementsPerPage)
		{
			return 0;
		}
		return page * m_maxElementsPerPage;
	}

	private int GetEndIndex(int page)
	{
		if (m_elements.Length <= m_maxElementsPerPage)
		{
			return m_elements.Length - 1;
		}
		return Math.Min((page + 1) * m_maxElementsPerPage - 1, m_elements.Length - 1);
	}

	private void ClearElements()
	{
		foreach (Transform item in m_elementContainer)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
	}

	private void Interact(bool closeOnInteract = true)
	{
		if (m_selected != null && m_selected.Interact != null)
		{
			if (m_selected.Interact())
			{
				if (closeOnInteract)
				{
					Close();
				}
				else
				{
					m_currentConfig.SetRadial(this, m_currentPage);
				}
			}
			OnInteractionDelay?.Invoke(m_interactionDelay);
		}
		else
		{
			Close();
			OnInteractionDelay?.Invoke(m_noInteractionDelay);
		}
	}

	public void Back()
	{
		if (m_backConfig == null)
		{
			Close();
			return;
		}
		m_backConfig.SetRadial(this, m_lastPages.Pop());
		m_backConfig = null;
	}

	private void Close()
	{
		m_lastPages.Clear();
		m_backConfig = null;
		CanOpen = false;
		Active = false;
	}
}
