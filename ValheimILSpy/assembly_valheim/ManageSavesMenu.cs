using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManageSavesMenu : MonoBehaviour
{
	public delegate void ClosedCallback();

	public delegate void SavesModifiedCallback(SaveDataType list);

	private delegate void UpdateCloudUsageFinishedCallback();

	private delegate void ReloadSavesFinishedCallback(bool success);

	private delegate void UpdateGuiListFinishedCallback();

	[SerializeField]
	private Button backButton;

	[SerializeField]
	private Button removeButton;

	[SerializeField]
	private Button moveButton;

	[SerializeField]
	private Button actionButton;

	[SerializeField]
	private GameObject saveElement;

	[SerializeField]
	private TMP_Text storageUsed;

	[SerializeField]
	private TabHandler tabHandler;

	[SerializeField]
	private RectTransform storageBar;

	[SerializeField]
	private RectTransform listRoot;

	[SerializeField]
	private ScrollRectEnsureVisible scrollRectEnsureVisible;

	[SerializeField]
	private UIGamePad blockerInfo;

	[SerializeField]
	private GameObject pleaseWait;

	private SaveWithBackups[] currentList;

	private SaveDataType currentListType;

	private DateTime mostRecentBackupCreatedTime = DateTime.MinValue;

	private List<ManageSavesMenuElement> listElements = new List<ManageSavesMenuElement>();

	private bool elementHeightChanged;

	private ClosedCallback closedCallback;

	private SavesModifiedCallback savesModifiedCallback;

	private string m_queuedNameToSelect;

	private int selectedSaveIndex = -1;

	private int selectedBackupIndex = -1;

	private float timeClicked;

	private const float doubleClickTime = 0.5f;

	private int pleaseWaitCount;

	private void Update()
	{
		bool flag = false;
		if (!blockerInfo.IsBlocked())
		{
			bool flag2 = true;
			if (ZInput.GetKeyDown(KeyCode.LeftArrow) && IsSelectedExpanded())
			{
				CollapseSelected();
				flag = true;
				flag2 = false;
			}
			if (ZInput.GetKeyDown(KeyCode.RightArrow) && !IsSelectedExpanded())
			{
				ExpandSelected();
				flag = true;
			}
			if (flag2)
			{
				if (ZInput.GetKeyDown(KeyCode.DownArrow))
				{
					SelectRelative(1);
					flag = true;
				}
				if (ZInput.GetKeyDown(KeyCode.UpArrow))
				{
					SelectRelative(-1);
					flag = true;
				}
			}
			if (ZInput.IsGamepadActive())
			{
				if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
				{
					SelectRelative(1);
					flag = true;
				}
				if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
				{
					SelectRelative(-1);
					flag = true;
				}
			}
		}
		if (flag)
		{
			UpdateButtons();
			CenterSelected();
		}
		else
		{
			UpdateButtonsInteractable();
		}
	}

	private void LateUpdate()
	{
		if (elementHeightChanged)
		{
			elementHeightChanged = false;
			UpdateElementPositions();
		}
	}

	private void UpdateButtons()
	{
		moveButton.gameObject.SetActive(FileHelpers.m_cloudEnabled && !FileHelpers.m_cloudOnly);
		if (selectedSaveIndex < 0)
		{
			actionButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize("$menu_expand");
		}
		else
		{
			if (selectedBackupIndex < 0)
			{
				if (listElements[selectedSaveIndex].BackupCount > 0)
				{
					actionButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize(listElements[selectedSaveIndex].IsExpanded ? "$menu_collapse" : "$menu_expand");
				}
			}
			else
			{
				actionButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize("$menu_restorebackup");
			}
			if (selectedBackupIndex < 0)
			{
				if (!currentList[selectedSaveIndex].IsDeleted)
				{
					moveButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize((currentList[selectedSaveIndex].PrimaryFile.m_source != FileHelpers.FileSource.Cloud) ? "$menu_movetocloud" : "$menu_movetolocal");
				}
			}
			else
			{
				moveButton.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize((currentList[selectedSaveIndex].BackupFiles[selectedBackupIndex].m_source != FileHelpers.FileSource.Cloud) ? "$menu_movetocloud" : "$menu_movetolocal");
			}
		}
		UpdateButtonsInteractable();
	}

	private void UpdateButtonsInteractable()
	{
		bool flag = (DateTime.Now - mostRecentBackupCreatedTime).TotalSeconds >= 1.0;
		int num;
		int num2;
		if (selectedSaveIndex >= 0)
		{
			num = ((selectedSaveIndex < listElements.Count) ? 1 : 0);
			if (num != 0)
			{
				num2 = ((selectedBackupIndex >= 0) ? 1 : 0);
				goto IL_0059;
			}
		}
		else
		{
			num = 0;
		}
		num2 = 0;
		goto IL_0059;
		IL_0059:
		bool flag2 = (byte)num2 != 0;
		bool flag3 = num != 0 && listElements[selectedSaveIndex].BackupCount > 0 && selectedBackupIndex < 0;
		actionButton.interactable = flag3 || (flag2 && flag);
		bool flag4 = num != 0 && (selectedBackupIndex >= 0 || !currentList[selectedSaveIndex].IsDeleted);
		removeButton.interactable = flag4;
		moveButton.interactable = flag4 && flag;
	}

	private void OnSaveElementHeighChanged()
	{
		elementHeightChanged = true;
	}

	private void UpdateCloudUsageAsync(UpdateCloudUsageFinishedCallback callback = null)
	{
		if (FileHelpers.m_cloudEnabled)
		{
			PushPleaseWait();
			BackgroundWorker backgroundWorker = new BackgroundWorker();
			ulong usedBytes = 0uL;
			ulong capacityBytes = 0uL;
			backgroundWorker.DoWork += delegate
			{
				usedBytes = FileHelpers.GetTotalCloudUsage();
				capacityBytes = FileHelpers.GetTotalCloudCapacity();
			};
			backgroundWorker.RunWorkerCompleted += delegate
			{
				storageUsed.gameObject.SetActive(value: true);
				storageBar.parent.gameObject.SetActive(value: true);
				storageUsed.text = Localization.instance.Localize("$menu_cloudstorageused", FileHelpers.BytesAsNumberString(usedBytes, 1u), FileHelpers.BytesAsNumberString(capacityBytes, 1u));
				storageBar.localScale = new Vector3((float)usedBytes / (float)capacityBytes, storageBar.localScale.y, storageBar.localScale.z);
				PopPleaseWait();
				callback?.Invoke();
			};
			backgroundWorker.RunWorkerAsync();
		}
		else
		{
			storageUsed.gameObject.SetActive(value: false);
			storageBar.parent.gameObject.SetActive(value: false);
			callback?.Invoke();
		}
	}

	private void OnBackButton()
	{
		Close();
	}

	private void OnRemoveButton()
	{
		if (selectedSaveIndex >= 0)
		{
			bool isBackup = selectedBackupIndex >= 0;
			string text = (isBackup ? "$menu_removebackup" : (tabHandler.GetActiveTab() switch
			{
				0 => "$menu_removeworld", 
				1 => "$menu_removecharacter", 
				_ => "Remove?", 
			}));
			SaveFile toDelete = (isBackup ? currentList[selectedSaveIndex].BackupFiles[selectedBackupIndex] : currentList[selectedSaveIndex].PrimaryFile);
			UnifiedPopup.Push(new YesNoPopup(Localization.instance.Localize(text), isBackup ? toDelete.FileName : currentList[selectedSaveIndex].m_name, delegate
			{
				UnifiedPopup.Pop();
				DeleteSaveFile(toDelete, isBackup);
			}, delegate
			{
				UnifiedPopup.Pop();
			}, localizeText: false));
		}
	}

	private void OnMoveButton()
	{
		if (selectedSaveIndex < 0)
		{
			return;
		}
		bool flag = selectedBackupIndex >= 0;
		SaveFile saveFile = (flag ? currentList[selectedSaveIndex].BackupFiles[selectedBackupIndex] : currentList[selectedSaveIndex].PrimaryFile);
		FileHelpers.FileSource fileSource = ((saveFile.m_source == FileHelpers.FileSource.Cloud) ? FileHelpers.FileSource.Local : FileHelpers.FileSource.Cloud);
		SaveFile saveFile2 = null;
		for (int i = 0; i < currentList[selectedSaveIndex].BackupFiles.Length; i++)
		{
			if (i != selectedBackupIndex && currentList[selectedSaveIndex].BackupFiles[i].m_source == fileSource && currentList[selectedSaveIndex].BackupFiles[i].FileName == saveFile.FileName)
			{
				saveFile2 = currentList[selectedSaveIndex].BackupFiles[i];
				break;
			}
		}
		if (saveFile2 == null && flag && !currentList[selectedSaveIndex].IsDeleted && currentList[selectedSaveIndex].PrimaryFile.m_source == fileSource && currentList[selectedSaveIndex].PrimaryFile.FileName == saveFile.FileName)
		{
			saveFile2 = currentList[selectedSaveIndex].PrimaryFile;
		}
		if (saveFile2 != null)
		{
			UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$menu_cantmovesave"), Localization.instance.Localize("$menu_duplicatefileprompttext", saveFile.FileName), delegate
			{
				UnifiedPopup.Pop();
			}, localizeText: false));
		}
		else if (SaveSystem.IsCorrupt(saveFile))
		{
			UnifiedPopup.Push(new WarningPopup("$menu_cantmovesave", "$menu_savefilecorrupt", delegate
			{
				UnifiedPopup.Pop();
			}));
		}
		else
		{
			MoveSource(saveFile, flag, fileSource);
		}
	}

	private void OnPrimaryActionButton()
	{
		if (selectedSaveIndex >= 0)
		{
			if (selectedBackupIndex >= 0)
			{
				RestoreBackup();
			}
			else if (listElements[selectedSaveIndex].BackupCount > 0)
			{
				listElements[selectedSaveIndex].SetExpanded(!listElements[selectedSaveIndex].IsExpanded);
				UpdateButtons();
			}
		}
	}

	private void RestoreBackup()
	{
		SaveWithBackups saveWithBackups = currentList[selectedSaveIndex];
		SaveFile backup = currentList[selectedSaveIndex].BackupFiles[selectedBackupIndex];
		UnifiedPopup.Push(new YesNoPopup(Localization.instance.Localize("$menu_backuprestorepromptheader"), saveWithBackups.IsDeleted ? Localization.instance.Localize("$menu_backuprestorepromptrecover", saveWithBackups.m_name, backup.FileName) : Localization.instance.Localize("$menu_backuprestorepromptreplace", saveWithBackups.m_name, backup.FileName), delegate
		{
			UnifiedPopup.Pop();
			RestoreBackupAsync();
		}, delegate
		{
			UnifiedPopup.Pop();
		}, localizeText: false));
		void RestoreBackupAsync()
		{
			PushPleaseWait();
			SaveSystem.RestoreBackupResult result = SaveSystem.RestoreBackupResult.UnknownError;
			BackgroundWorker backgroundWorker = new BackgroundWorker();
			backgroundWorker.DoWork += delegate
			{
				result = SaveSystem.RestoreBackup(backup);
			};
			backgroundWorker.RunWorkerCompleted += delegate
			{
				PopPleaseWait();
				if (result != 0)
				{
					ZLog.LogError($"Failed to restore backup: {result}");
					RestoreBackupFailed();
				}
				mostRecentBackupCreatedTime = DateTime.Now;
				savesModifiedCallback?.Invoke(GetCurrentListType());
				UpdateGuiAfterFileModification();
			};
			backgroundWorker.RunWorkerAsync();
		}
		static void RestoreBackupFailed()
		{
			UnifiedPopup.Push(new WarningPopup("$menu_backuprestorefailedheader", "$menu_tryagainorrestart", delegate
			{
				UnifiedPopup.Pop();
			}));
		}
	}

	private void UpdateGuiAfterFileModification(bool alwaysSelectSave = false)
	{
		string saveName = ((selectedSaveIndex >= 0) ? listElements[selectedSaveIndex].Save.m_name : "");
		string backupName = ((selectedSaveIndex >= 0 && selectedBackupIndex >= 0 && selectedBackupIndex < listElements[selectedSaveIndex].Save.BackupFiles.Length) ? listElements[selectedSaveIndex].Save.BackupFiles[selectedBackupIndex].FileName : "");
		int saveIndex = selectedSaveIndex;
		int backupIndex = selectedBackupIndex;
		DeselectCurrent();
		UpdateCloudUsageAsync();
		ReloadSavesAsync(delegate(bool success)
		{
			if (success)
			{
				UpdateGuiAsync();
			}
			else
			{
				ShowReloadError();
			}
		});
		void UpdateGuiAsync()
		{
			UpdateSavesListGuiAsync(delegate
			{
				int num = -1;
				for (int i = 0; i < listElements.Count; i++)
				{
					if (listElements[i].Save.m_name == saveName)
					{
						num = i;
						break;
					}
				}
				if (num >= 0)
				{
					if (backupIndex < 0 || alwaysSelectSave)
					{
						SelectByIndex(num);
					}
					else
					{
						int num2 = -1;
						for (int j = 0; j < listElements[num].Save.BackupFiles.Length; j++)
						{
							if (listElements[num].Save.BackupFiles[j].FileName == backupName)
							{
								num2 = j;
								break;
							}
						}
						if (num2 >= 0)
						{
							SelectByIndex(num, num2);
						}
						else
						{
							SelectByIndex(num, backupIndex);
						}
					}
					CenterSelected();
				}
				else
				{
					SelectByIndex(saveIndex);
					CenterSelected();
				}
				UpdateButtons();
			});
		}
	}

	public void OnWorldTab()
	{
		if (pleaseWaitCount <= 0)
		{
			ChangeList(SaveDataType.World);
		}
	}

	public void OnCharacterTab()
	{
		if (pleaseWaitCount <= 0)
		{
			ChangeList(SaveDataType.Character);
		}
	}

	private void ChangeList(SaveDataType dataType)
	{
		DeselectCurrent();
		currentList = SaveSystem.GetSavesByType(dataType);
		currentListType = dataType;
		UpdateSavesListGuiAsync(delegate
		{
			bool flag = false;
			if (!string.IsNullOrEmpty(m_queuedNameToSelect))
			{
				for (int i = 0; i < currentList.Length; i++)
				{
					if (!currentList[i].IsDeleted && currentList[i].PrimaryFile.FileName == m_queuedNameToSelect)
					{
						SelectByIndex(i);
						flag = true;
						break;
					}
				}
				m_queuedNameToSelect = null;
			}
			if (!flag || listElements.Count <= 0)
			{
				SelectByIndex(0);
			}
			if (selectedSaveIndex >= 0)
			{
				CenterSelected();
			}
			UpdateButtons();
		});
	}

	private void DeleteSaveFile(SaveFile file, bool isBackup)
	{
		PushPleaseWait();
		bool success = false;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate
		{
			success = SaveSystem.Delete(file);
		};
		backgroundWorker.RunWorkerCompleted += delegate
		{
			PopPleaseWait();
			if (!success)
			{
				DeleteSaveFailed();
				ZLog.LogError("Failed to delete save " + file.FileName);
			}
			mostRecentBackupCreatedTime = DateTime.Now;
			savesModifiedCallback?.Invoke(GetCurrentListType());
			UpdateGuiAfterFileModification();
		};
		backgroundWorker.RunWorkerAsync();
		static void DeleteSaveFailed()
		{
			UnifiedPopup.Push(new WarningPopup("$menu_deletefailedheader", "$menu_tryagainorrestart", delegate
			{
				UnifiedPopup.Pop();
			}));
		}
	}

	private void MoveSource(SaveFile file, bool isBackup, FileHelpers.FileSource destinationSource)
	{
		PushPleaseWait();
		bool cloudQuotaExceeded = false;
		bool success = false;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate
		{
			success = SaveSystem.MoveSource(file, isBackup, destinationSource, out cloudQuotaExceeded);
		};
		backgroundWorker.RunWorkerCompleted += delegate
		{
			PopPleaseWait();
			if (cloudQuotaExceeded)
			{
				ShowCloudQuotaWarning();
			}
			else if (!success)
			{
				MoveSourceFailed();
			}
			mostRecentBackupCreatedTime = DateTime.Now;
			savesModifiedCallback?.Invoke(GetCurrentListType());
			UpdateGuiAfterFileModification();
		};
		backgroundWorker.RunWorkerAsync();
		static void MoveSourceFailed()
		{
			UnifiedPopup.Push(new WarningPopup("$menu_movefailedheader", "$menu_tryagainorrestart", delegate
			{
				UnifiedPopup.Pop();
			}));
		}
	}

	private SaveDataType GetCurrentListType()
	{
		return currentListType;
	}

	private void ReloadSavesAsync(ReloadSavesFinishedCallback callback)
	{
		PushPleaseWait();
		Exception reloadException = null;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate
		{
			try
			{
				SaveSystem.ForceRefreshCache();
			}
			catch (Exception ex)
			{
				reloadException = ex;
			}
		};
		backgroundWorker.RunWorkerCompleted += delegate
		{
			currentList = SaveSystem.GetSavesByType(currentListType);
			PopPleaseWait();
			if (reloadException != null)
			{
				ZLog.LogError(reloadException.ToString());
			}
			callback?.Invoke(reloadException == null);
		};
		backgroundWorker.RunWorkerAsync();
	}

	private void UpdateElementPositions()
	{
		float num = 0f;
		for (int i = 0; i < listElements.Count; i++)
		{
			listElements[i].rectTransform.anchoredPosition = new Vector2(listElements[i].rectTransform.anchoredPosition.x, 0f - num);
			num += listElements[i].rectTransform.sizeDelta.y;
		}
		listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
	}

	private IEnumerator UpdateElementPositionsEnumerator()
	{
		float pos = 0f;
		for (int i = 0; i < listElements.Count; i++)
		{
			listElements[i].rectTransform.anchoredPosition = new Vector2(listElements[i].rectTransform.anchoredPosition.x, 0f - pos);
			pos += listElements[i].rectTransform.sizeDelta.y;
			yield return null;
		}
		listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pos);
	}

	private ManageSavesMenuElement CreateElement()
	{
		GameObject obj = UnityEngine.Object.Instantiate(saveElement, listRoot);
		ManageSavesMenuElement component = obj.GetComponent<ManageSavesMenuElement>();
		obj.SetActive(value: true);
		component.HeightChanged += OnSaveElementHeighChanged;
		component.ElementClicked += OnElementClicked;
		component.ElementExpandedChanged += OnElementExpandedChanged;
		return component;
	}

	private void UpdateSavesListGui()
	{
		List<ManageSavesMenuElement> list = new List<ManageSavesMenuElement>();
		Dictionary<string, ManageSavesMenuElement> dictionary = new Dictionary<string, ManageSavesMenuElement>();
		for (int i = 0; i < listElements.Count; i++)
		{
			dictionary.Add(listElements[i].Save.m_name, listElements[i]);
		}
		for (int j = 0; j < currentList.Length; j++)
		{
			if (dictionary.ContainsKey(currentList[j].m_name))
			{
				dictionary[currentList[j].m_name].UpdateElement(currentList[j]);
				list.Add(dictionary[currentList[j].m_name]);
				dictionary.Remove(currentList[j].m_name);
			}
			else
			{
				ManageSavesMenuElement manageSavesMenuElement = CreateElement();
				manageSavesMenuElement.SetUp(manageSavesMenuElement.Save);
				list.Add(manageSavesMenuElement);
			}
		}
		foreach (KeyValuePair<string, ManageSavesMenuElement> item in dictionary)
		{
			UnityEngine.Object.Destroy(item.Value.gameObject);
		}
		listElements = list;
		UpdateElementPositions();
	}

	private IEnumerator UpdateSaveListGuiAsyncCoroutine(UpdateGuiListFinishedCallback callback)
	{
		PushPleaseWait();
		float timeBudget = 0.25f / (float)Application.targetFrameRate;
		DateTime now = DateTime.Now;
		for (int i = listElements.Count - 1; i >= 0; i--)
		{
			listElements[i].rectTransform.anchoredPosition = new Vector2(listElements[i].rectTransform.anchoredPosition.x, 1000000f);
			if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
		}
		if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
		{
			yield return null;
			now = DateTime.Now;
		}
		List<ManageSavesMenuElement> newSaveElementsList = new List<ManageSavesMenuElement>();
		Dictionary<string, ManageSavesMenuElement> saveNameToElementMap = new Dictionary<string, ManageSavesMenuElement>();
		for (int i = 0; i < listElements.Count; i++)
		{
			saveNameToElementMap.Add(listElements[i].Save.m_name, listElements[i]);
			if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
		}
		for (int i = 0; i < currentList.Length; i++)
		{
			if (saveNameToElementMap.ContainsKey(currentList[i].m_name))
			{
				IEnumerator updateElementEnumerator = saveNameToElementMap[currentList[i].m_name].UpdateElementEnumerator(currentList[i]);
				while (updateElementEnumerator.MoveNext())
				{
					if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
					{
						yield return null;
						now = DateTime.Now;
					}
				}
				newSaveElementsList.Add(saveNameToElementMap[currentList[i].m_name]);
				saveNameToElementMap.Remove(currentList[i].m_name);
			}
			else
			{
				ManageSavesMenuElement manageSavesMenuElement = CreateElement();
				newSaveElementsList.Add(manageSavesMenuElement);
				newSaveElementsList[newSaveElementsList.Count - 1].rectTransform.anchoredPosition = new Vector2(newSaveElementsList[newSaveElementsList.Count - 1].rectTransform.anchoredPosition.x, 1000000f);
				IEnumerator updateElementEnumerator = manageSavesMenuElement.SetUpEnumerator(currentList[i]);
				while (updateElementEnumerator.MoveNext())
				{
					if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
					{
						yield return null;
						now = DateTime.Now;
					}
				}
			}
			if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
		}
		foreach (KeyValuePair<string, ManageSavesMenuElement> item in saveNameToElementMap)
		{
			UnityEngine.Object.Destroy(item.Value.gameObject);
			if ((DateTime.Now - now).TotalSeconds > (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
		}
		listElements = newSaveElementsList;
		if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
		{
			yield return null;
			now = DateTime.Now;
		}
		IEnumerator updateElementPositionsEnumerator = UpdateElementPositionsEnumerator();
		while (updateElementPositionsEnumerator.MoveNext())
		{
			if ((DateTime.Now - now).TotalSeconds >= (double)timeBudget)
			{
				yield return null;
				now = DateTime.Now;
			}
		}
		PopPleaseWait();
		callback?.Invoke();
	}

	private void UpdateSavesListGuiAsync(UpdateGuiListFinishedCallback callback)
	{
		StartCoroutine(UpdateSaveListGuiAsyncCoroutine(callback));
	}

	private void DestroyGui()
	{
		for (int i = 0; i < listElements.Count; i++)
		{
			UnityEngine.Object.Destroy(listElements[i].gameObject);
		}
		listElements.Clear();
	}

	public void Open(SaveDataType dataType, string selectedSaveName, ClosedCallback closedCallback, SavesModifiedCallback savesModifiedCallback)
	{
		QueueSelectByName(selectedSaveName);
		Open(dataType, closedCallback, savesModifiedCallback);
	}

	public void Open(SaveDataType dataType, ClosedCallback closedCallback, SavesModifiedCallback savesModifiedCallback)
	{
		this.closedCallback = closedCallback;
		this.savesModifiedCallback = savesModifiedCallback;
		if (base.gameObject.activeSelf && tabHandler.GetActiveTab() == GetTabIndexFromSaveDataType(dataType))
		{
			return;
		}
		backButton.onClick.AddListener(OnBackButton);
		removeButton.onClick.AddListener(OnRemoveButton);
		moveButton.onClick.AddListener(OnMoveButton);
		actionButton.onClick.AddListener(OnPrimaryActionButton);
		storageUsed.gameObject.SetActive(value: false);
		storageBar.parent.gameObject.SetActive(value: false);
		base.gameObject.SetActive(value: true);
		UpdateCloudUsageAsync();
		ReloadSavesAsync(delegate(bool success)
		{
			if (!success)
			{
				ShowReloadError();
			}
			tabHandler.SetActiveTabWithoutInvokingOnClick(GetTabIndexFromSaveDataType(dataType));
			ChangeList(dataType);
		});
	}

	private void QueueSelectByName(string name)
	{
		m_queuedNameToSelect = name;
	}

	private int GetTabIndexFromSaveDataType(SaveDataType dataType)
	{
		return dataType switch
		{
			SaveDataType.World => 0, 
			SaveDataType.Character => 1, 
			_ => throw new ArgumentException($"{dataType} does not have a tab!"), 
		};
	}

	public void Close()
	{
		DestroyGui();
		backButton.onClick.RemoveListener(OnBackButton);
		removeButton.onClick.RemoveListener(OnRemoveButton);
		moveButton.onClick.RemoveListener(OnMoveButton);
		actionButton.onClick.RemoveListener(OnPrimaryActionButton);
		base.gameObject.SetActive(value: false);
		closedCallback?.Invoke();
	}

	public bool IsVisible()
	{
		return base.gameObject.activeInHierarchy;
	}

	private void SelectByIndex(int saveIndex, int backupIndex = -1)
	{
		DeselectCurrent();
		selectedSaveIndex = saveIndex;
		selectedBackupIndex = backupIndex;
		if (listElements.Count <= 0)
		{
			selectedSaveIndex = -1;
			selectedBackupIndex = -1;
		}
		else
		{
			selectedSaveIndex = Mathf.Clamp(selectedSaveIndex, 0, listElements.Count - 1);
			listElements[selectedSaveIndex].Select(ref selectedBackupIndex);
		}
	}

	private void SelectRelative(int offset)
	{
		int num = selectedSaveIndex;
		int num2 = selectedBackupIndex;
		DeselectCurrent();
		if (listElements.Count <= 0)
		{
			selectedSaveIndex = -1;
			selectedBackupIndex = -1;
			return;
		}
		if (num < 0)
		{
			num = 0;
			num2 = -1;
		}
		else if (num > listElements.Count - 1)
		{
			num = listElements.Count - 1;
			num2 = (listElements[num].IsExpanded ? listElements[num].BackupCount : (-1));
		}
		int num3 = offset;
		while (num3 != 0)
		{
			int num4 = Math.Sign(num3);
			if (listElements[num].IsExpanded)
			{
				if (num2 + num4 < -1 || num2 + num4 > listElements[num].BackupCount - 1)
				{
					if (num + num4 >= 0 && num + num4 <= listElements.Count - 1)
					{
						num += num4;
						num2 = ((num4 < 0 && listElements[num].IsExpanded) ? (listElements[num].BackupCount - 1) : (-1));
					}
				}
				else
				{
					num2 += num4;
				}
			}
			else if (num2 >= 0)
			{
				if (num + num4 >= 0 && num + num4 <= listElements.Count - 1 && num4 > 0)
				{
					num += num4;
				}
				num2 = -1;
			}
			else if (num + num4 >= 0 && num + num4 <= listElements.Count - 1)
			{
				num += num4;
				num2 = ((num4 < 0 && listElements[num].IsExpanded) ? (listElements[num].BackupCount - 1) : (-1));
			}
			num3 -= num4;
		}
		SelectByIndex(num, num2);
	}

	private void DeselectCurrent()
	{
		if (selectedSaveIndex >= 0 && selectedSaveIndex <= listElements.Count - 1)
		{
			listElements[selectedSaveIndex].Deselect(selectedBackupIndex);
		}
		selectedSaveIndex = -1;
		selectedBackupIndex = -1;
	}

	private bool IsSelectedExpanded()
	{
		if (selectedSaveIndex < 0 || selectedSaveIndex > listElements.Count - 1)
		{
			ZLog.LogError("Failed to expand save: Index " + selectedSaveIndex + " was outside of the valid range 0-" + (listElements.Count - 1) + ".");
			return false;
		}
		return listElements[selectedSaveIndex].IsExpanded;
	}

	private void ExpandSelected()
	{
		if (selectedSaveIndex < 0 || selectedSaveIndex > listElements.Count - 1)
		{
			ZLog.LogWarning("Failed to expand save: Index " + selectedSaveIndex + " was outside of the valid range 0-" + (listElements.Count - 1) + ". Ignoring.");
		}
		else
		{
			listElements[selectedSaveIndex].SetExpanded(value: true);
		}
	}

	private void CollapseSelected()
	{
		if (selectedSaveIndex < 0 || selectedSaveIndex > listElements.Count - 1)
		{
			ZLog.LogWarning("Failed to collapse save: Index " + selectedSaveIndex + " was outside of the valid range 0-" + (listElements.Count - 1) + ". Ignoring.");
		}
		else
		{
			listElements[selectedSaveIndex].SetExpanded(value: false);
		}
	}

	private void CenterSelected()
	{
		if (selectedSaveIndex < 0 || selectedSaveIndex > listElements.Count - 1)
		{
			ZLog.LogWarning("Failed to center save: Index " + selectedSaveIndex + " was outside of the valid range 0-" + (listElements.Count - 1) + ". Ignoring.");
		}
		else
		{
			scrollRectEnsureVisible.CenterOnItem(listElements[selectedSaveIndex].GetTransform(selectedBackupIndex));
		}
	}

	private void OnElementClicked(ManageSavesMenuElement element, int backupElementIndex)
	{
		int num = selectedSaveIndex;
		int num2 = selectedBackupIndex;
		int saveIndex = listElements.IndexOf(element);
		DeselectCurrent();
		SelectByIndex(saveIndex, backupElementIndex);
		if (selectedSaveIndex == num && selectedBackupIndex == num2 && Time.time < timeClicked + 0.5f)
		{
			OnPrimaryActionButton();
			timeClicked = Time.time - 0.5f;
		}
		else
		{
			timeClicked = Time.time;
		}
		UpdateButtons();
	}

	private void OnElementExpandedChanged(ManageSavesMenuElement element, bool isExpanded)
	{
		int num = listElements.IndexOf(element);
		if (selectedSaveIndex == num)
		{
			if (!isExpanded && selectedBackupIndex >= 0)
			{
				DeselectCurrent();
				SelectByIndex(num);
			}
			UpdateButtons();
		}
	}

	public void ShowCloudQuotaWarning()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_cloudstoragefull", "$menu_cloudstoragefulloperationfailed", delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	public void ShowReloadError()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_reloadfailed", "$menu_checklogfile", delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	private void PushPleaseWait()
	{
		if (pleaseWaitCount == 0)
		{
			pleaseWait.SetActive(value: true);
		}
		pleaseWaitCount++;
	}

	private void PopPleaseWait()
	{
		pleaseWaitCount--;
		if (pleaseWaitCount == 0)
		{
			pleaseWait.SetActive(value: false);
		}
	}
}
