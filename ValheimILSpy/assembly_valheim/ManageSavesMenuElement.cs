using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManageSavesMenuElement : MonoBehaviour
{
	public delegate void BackupElementClickedHandler();

	private class BackupElement
	{
		public SaveFile File { get; private set; }

		public GameObject GuiInstance { get; private set; }

		public Button Button { get; private set; }

		public RectTransform rectTransform => GuiInstance.transform as RectTransform;

		public BackupElement(GameObject guiInstance, SaveFile backup, BackupElementClickedHandler clickedCallback)
		{
			GuiInstance = guiInstance;
			GuiInstance.SetActive(value: true);
			Button = GuiInstance.GetComponent<Button>();
			UpdateElement(backup, clickedCallback);
		}

		public void UpdateElement(SaveFile backup, BackupElementClickedHandler clickedCallback)
		{
			File = backup;
			Button.onClick.RemoveAllListeners();
			Button.onClick.AddListener(delegate
			{
				clickedCallback?.Invoke();
			});
			string text = backup.FileName;
			if (SaveSystem.IsCorrupt(backup))
			{
				text += " [CORRUPT]";
			}
			if (SaveSystem.IsWorldWithMissingMetaFile(backup))
			{
				text += " [MISSING META FILE]";
			}
			rectTransform.Find("name").GetComponent<TMP_Text>().text = text;
			rectTransform.Find("size").GetComponent<TMP_Text>().text = FileHelpers.BytesAsNumberString(backup.Size, 1u);
			rectTransform.Find("date").GetComponent<TMP_Text>().text = backup.LastModified.ToShortDateString() + " " + backup.LastModified.ToShortTimeString();
			Transform transform = rectTransform.Find("source");
			transform.Find("source_cloud")?.gameObject.SetActive(backup.m_source == FileHelpers.FileSource.Cloud);
			transform.Find("source_local")?.gameObject.SetActive(backup.m_source == FileHelpers.FileSource.Local);
			transform.Find("source_legacy")?.gameObject.SetActive(backup.m_source == FileHelpers.FileSource.Legacy);
		}
	}

	public delegate void HeightChangedHandler();

	public delegate void ElementClickedHandler(ManageSavesMenuElement element, int backupElementIndex);

	public delegate void ElementExpandedChangedHandler(ManageSavesMenuElement element, bool isExpanded);

	[SerializeField]
	private Button primaryElement;

	[SerializeField]
	private Button backupElement;

	[SerializeField]
	private GameObject selectedBackground;

	[SerializeField]
	private Button arrow;

	[SerializeField]
	private TMP_Text nameText;

	[SerializeField]
	private TMP_Text sizeText;

	[SerializeField]
	private TMP_Text backupCountText;

	[SerializeField]
	private TMP_Text dateText;

	[SerializeField]
	private RectTransform sourceParent;

	private float elementHeight = 32f;

	private List<BackupElement> backupElements = new List<BackupElement>();

	private Coroutine arrowAnimationCoroutine;

	private Coroutine listAnimationCoroutine;

	public RectTransform rectTransform => base.transform as RectTransform;

	private RectTransform arrowRectTransform => arrow.transform as RectTransform;

	public bool IsExpanded { get; private set; }

	public int BackupCount => backupElements.Count;

	public SaveWithBackups Save { get; private set; }

	public event HeightChangedHandler HeightChanged;

	public event ElementClickedHandler ElementClicked;

	public event ElementExpandedChangedHandler ElementExpandedChanged;

	public void SetUp(SaveWithBackups save)
	{
		UpdatePrimaryElement();
		for (int i = 0; i < Save.BackupFiles.Length; i++)
		{
			BackupElement item = CreateBackupElement(Save.BackupFiles[i], i);
			backupElements.Add(item);
		}
		UpdateElementPositions();
	}

	public IEnumerator SetUpEnumerator(SaveWithBackups save)
	{
		Save = save;
		UpdatePrimaryElement();
		yield return null;
		for (int i = 0; i < Save.BackupFiles.Length; i++)
		{
			BackupElement item = CreateBackupElement(Save.BackupFiles[i], i);
			backupElements.Add(item);
			yield return null;
		}
		IEnumerator updateElementPositions = UpdateElementPositionsEnumerator();
		while (updateElementPositions.MoveNext())
		{
			yield return null;
		}
	}

	public void UpdateElement(SaveWithBackups save)
	{
		Save = save;
		UpdatePrimaryElement();
		List<BackupElement> list = new List<BackupElement>();
		Dictionary<string, Dictionary<FileHelpers.FileSource, BackupElement>> dictionary = new Dictionary<string, Dictionary<FileHelpers.FileSource, BackupElement>>();
		for (int i = 0; i < backupElements.Count; i++)
		{
			if (!dictionary.ContainsKey(backupElements[i].File.FileName))
			{
				dictionary.Add(backupElements[i].File.FileName, new Dictionary<FileHelpers.FileSource, BackupElement>());
			}
			dictionary[backupElements[i].File.FileName].Add(backupElements[i].File.m_source, backupElements[i]);
		}
		for (int j = 0; j < Save.BackupFiles.Length; j++)
		{
			SaveFile saveFile = Save.BackupFiles[j];
			if (dictionary.ContainsKey(saveFile.FileName) && dictionary[saveFile.FileName].ContainsKey(saveFile.m_source))
			{
				int currentIndex = j;
				dictionary[saveFile.FileName][saveFile.m_source].UpdateElement(saveFile, delegate
				{
					OnBackupElementClicked(currentIndex);
				});
				list.Add(dictionary[saveFile.FileName][saveFile.m_source]);
				dictionary[saveFile.FileName].Remove(saveFile.m_source);
				if (dictionary.Count <= 0)
				{
					dictionary.Remove(saveFile.FileName);
				}
			}
			else
			{
				BackupElement item = CreateBackupElement(saveFile, j);
				list.Add(item);
			}
		}
		foreach (KeyValuePair<string, Dictionary<FileHelpers.FileSource, BackupElement>> item2 in dictionary)
		{
			foreach (KeyValuePair<FileHelpers.FileSource, BackupElement> item3 in item2.Value)
			{
				Object.Destroy(item3.Value.GuiInstance);
			}
		}
		backupElements = list;
		float num = UpdateElementPositions();
		rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, IsExpanded ? num : elementHeight);
	}

	public IEnumerator UpdateElementEnumerator(SaveWithBackups save)
	{
		Save = save;
		UpdatePrimaryElement();
		List<BackupElement> newBackupElementsList = new List<BackupElement>();
		Dictionary<string, Dictionary<FileHelpers.FileSource, BackupElement>> backupNameToElementMap = new Dictionary<string, Dictionary<FileHelpers.FileSource, BackupElement>>();
		for (int j = 0; j < backupElements.Count; j++)
		{
			if (!backupNameToElementMap.ContainsKey(backupElements[j].File.FileName))
			{
				backupNameToElementMap.Add(backupElements[j].File.FileName, new Dictionary<FileHelpers.FileSource, BackupElement>());
			}
			backupNameToElementMap[backupElements[j].File.FileName].Add(backupElements[j].File.m_source, backupElements[j]);
			yield return null;
		}
		for (int j = 0; j < Save.BackupFiles.Length; j++)
		{
			SaveFile saveFile = Save.BackupFiles[j];
			if (backupNameToElementMap.ContainsKey(saveFile.FileName) && backupNameToElementMap[saveFile.FileName].ContainsKey(saveFile.m_source))
			{
				int currentIndex = j;
				backupNameToElementMap[saveFile.FileName][saveFile.m_source].UpdateElement(saveFile, delegate
				{
					OnBackupElementClicked(currentIndex);
				});
				newBackupElementsList.Add(backupNameToElementMap[saveFile.FileName][saveFile.m_source]);
				backupNameToElementMap[saveFile.FileName].Remove(saveFile.m_source);
				if (backupNameToElementMap.Count <= 0)
				{
					backupNameToElementMap.Remove(saveFile.FileName);
				}
			}
			else
			{
				BackupElement item = CreateBackupElement(saveFile, j);
				newBackupElementsList.Add(item);
			}
			yield return null;
		}
		foreach (KeyValuePair<string, Dictionary<FileHelpers.FileSource, BackupElement>> item2 in backupNameToElementMap)
		{
			foreach (KeyValuePair<FileHelpers.FileSource, BackupElement> item3 in item2.Value)
			{
				Object.Destroy(item3.Value.GuiInstance);
				yield return null;
			}
		}
		backupElements = newBackupElementsList;
		float num = UpdateElementPositions();
		rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, IsExpanded ? num : elementHeight);
	}

	private BackupElement CreateBackupElement(SaveFile backup, int index)
	{
		return new BackupElement(Object.Instantiate(backupElement.gameObject, rectTransform), backup, delegate
		{
			OnBackupElementClicked(index);
		});
	}

	private float UpdateElementPositions()
	{
		float num = elementHeight;
		for (int i = 0; i < backupElements.Count; i++)
		{
			backupElements[i].rectTransform.anchoredPosition = new Vector2(backupElements[i].rectTransform.anchoredPosition.x, 0f - num);
			num += backupElements[i].rectTransform.sizeDelta.y;
		}
		return num;
	}

	private IEnumerator UpdateElementPositionsEnumerator()
	{
		float pos = elementHeight;
		for (int i = 0; i < backupElements.Count; i++)
		{
			backupElements[i].rectTransform.anchoredPosition = new Vector2(backupElements[i].rectTransform.anchoredPosition.x, 0f - pos);
			pos += backupElements[i].rectTransform.sizeDelta.y;
			yield return null;
		}
	}

	private void UpdatePrimaryElement()
	{
		arrow.gameObject.SetActive(Save.BackupFiles.Length != 0);
		string text = Save.m_name;
		if (!Save.IsDeleted)
		{
			text = Save.PrimaryFile.FileName;
			if (SaveSystem.IsCorrupt(Save.PrimaryFile))
			{
				text += " [CORRUPT]";
			}
			if (SaveSystem.IsWorldWithMissingMetaFile(Save.PrimaryFile))
			{
				text += " [MISSING META]";
			}
		}
		nameText.text = text;
		sizeText.text = FileHelpers.BytesAsNumberString(Save.IsDeleted ? 0 : Save.PrimaryFile.Size, 1u) + "/" + FileHelpers.BytesAsNumberString(Save.SizeWithBackups, 1u);
		backupCountText.text = Localization.instance.Localize("$menu_backupcount", Save.BackupFiles.Length.ToString());
		dateText.text = (Save.IsDeleted ? Localization.instance.Localize("$menu_deleted") : (Save.PrimaryFile.LastModified.ToShortDateString() + " " + Save.PrimaryFile.LastModified.ToShortTimeString()));
		sourceParent.Find("source_cloud")?.gameObject.SetActive(!Save.IsDeleted && Save.PrimaryFile.m_source == FileHelpers.FileSource.Cloud);
		sourceParent.Find("source_local")?.gameObject.SetActive(!Save.IsDeleted && Save.PrimaryFile.m_source == FileHelpers.FileSource.Local);
		sourceParent.Find("source_legacy")?.gameObject.SetActive(!Save.IsDeleted && Save.PrimaryFile.m_source == FileHelpers.FileSource.Legacy);
		if (IsExpanded && Save.BackupFiles.Length == 0)
		{
			SetExpanded(value: false, animated: false);
		}
	}

	private void OnDestroy()
	{
		foreach (BackupElement backupElement in backupElements)
		{
			Object.Destroy(backupElement.GuiInstance);
		}
		backupElements.Clear();
	}

	private void Start()
	{
		elementHeight = rectTransform.sizeDelta.y;
	}

	private void OnEnable()
	{
		primaryElement.onClick.AddListener(OnElementClicked);
		arrow.onClick.AddListener(OnArrowClicked);
	}

	private void OnDisable()
	{
		primaryElement.onClick.RemoveListener(OnElementClicked);
		arrow.onClick.RemoveListener(OnArrowClicked);
	}

	private void OnElementClicked()
	{
		this.ElementClicked?.Invoke(this, -1);
	}

	private void OnBackupElementClicked(int index)
	{
		this.ElementClicked?.Invoke(this, index);
	}

	private void OnArrowClicked()
	{
		SetExpanded(!IsExpanded);
	}

	public void SetExpanded(bool value, bool animated = true)
	{
		if (IsExpanded != value)
		{
			IsExpanded = value;
			this.ElementExpandedChanged?.Invoke(this, IsExpanded);
			if (arrowAnimationCoroutine != null)
			{
				StopCoroutine(arrowAnimationCoroutine);
			}
			if (listAnimationCoroutine != null)
			{
				StopCoroutine(listAnimationCoroutine);
			}
			if (animated)
			{
				arrowAnimationCoroutine = StartCoroutine(AnimateArrow());
				listAnimationCoroutine = StartCoroutine(AnimateList());
				return;
			}
			float z = ((!IsExpanded) ? 90 : 0);
			arrowRectTransform.rotation = Quaternion.Euler(0f, 0f, z);
			float y = (IsExpanded ? (elementHeight * (float)(backupElements.Count + 1)) : elementHeight);
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, y);
			this.HeightChanged?.Invoke();
		}
	}

	public void Select(ref int backupIndex)
	{
		if (backupIndex < 0 || BackupCount <= 0)
		{
			selectedBackground.gameObject.SetActive(value: true);
			backupIndex = -1;
		}
		else
		{
			backupIndex = Mathf.Clamp(backupIndex, 0, BackupCount - 1);
			backupElements[backupIndex].rectTransform.Find("selected").gameObject.SetActive(value: true);
		}
	}

	public void Deselect(int backupIndex = -1)
	{
		if (backupIndex < 0)
		{
			selectedBackground.gameObject.SetActive(value: false);
		}
		else if (backupIndex > backupElements.Count - 1)
		{
			ZLog.LogWarning("Failed to deselect backup: Index " + backupIndex + " was outside of the valid range -1-" + (backupElements.Count - 1) + ". Ignoring.");
		}
		else
		{
			backupElements[backupIndex].rectTransform.Find("selected").gameObject.SetActive(value: false);
		}
	}

	public RectTransform GetTransform(int backupIndex = -1)
	{
		if (backupIndex < 0)
		{
			return primaryElement.transform as RectTransform;
		}
		return backupElements[backupIndex].rectTransform;
	}

	private IEnumerator AnimateArrow()
	{
		float currentRotation = arrowRectTransform.rotation.eulerAngles.z;
		float targetRotation = ((!IsExpanded) ? 90 : 0);
		float sign = Mathf.Sign(targetRotation - currentRotation);
		while (true)
		{
			currentRotation += sign * 90f * 10f * Time.deltaTime;
			if (currentRotation * sign > targetRotation * sign)
			{
				currentRotation = targetRotation;
			}
			arrowRectTransform.rotation = Quaternion.Euler(0f, 0f, currentRotation);
			if (currentRotation == targetRotation)
			{
				break;
			}
			yield return null;
		}
		arrowAnimationCoroutine = null;
	}

	private IEnumerator AnimateList()
	{
		float currentSize = rectTransform.sizeDelta.y;
		float targetSize = (IsExpanded ? (elementHeight * (float)(backupElements.Count + 1)) : elementHeight);
		float sign = Mathf.Sign(targetSize - currentSize);
		float velocity = 0f;
		while (true)
		{
			currentSize = Mathf.SmoothDamp(currentSize, targetSize, ref velocity, 0.06f);
			if (currentSize * sign + 0.1f > targetSize * sign)
			{
				currentSize = targetSize;
			}
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, currentSize);
			this.HeightChanged?.Invoke();
			if (currentSize == targetSize)
			{
				break;
			}
			yield return null;
		}
		listAnimationCoroutine = null;
	}
}
