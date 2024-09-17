using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace ValheimAIModLoader
{
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        void ToggleModMenu()
        {
            if (!PlayerNPC)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Cannot open Thrall Menu without a Thrall in the world!");
                return;
            }

            instance.panelManager.TogglePanel("Settings");
            instance.panelManager.TogglePanel("Thrall Customization");

            if (PlayerNPC)
                SaveNPCData(PlayerNPC);
        }


        int MenuTitleFontSize = 36;
        int MenuSectionTitleFontSize = 24;
        Vector2 MenuSectionTitlePosition = new Vector2(10f, -5f);

        public class PanelManager
        {
            private Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();

            public GameObject CreatePanel(string panelName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width, float height, bool draggable, Vector2 pivot = new Vector2())
            {
                if (panels.ContainsKey(panelName))
                {
                    LogWarning($"Panel {panelName} already exists.");
                    return panels[panelName];
                }

                if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
                {
                    LogError("GUIManager instance or CustomGUI is null");
                    return null;
                }

                GameObject panel = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: anchorMin,
                    anchorMax: anchorMax,
                    position: position,
                    width: width,
                    height: height,
                    draggable: draggable);

                RectTransform rectTransform = panel.GetComponent<RectTransform>();
                rectTransform.pivot = pivot;

                AddTitleText(panel, panelName);

                panel.SetActive(false);
                panels[panelName] = panel;

                return panel;
            }

            private void AddTitleText(GameObject panel, string title)
            {
                //LogError("AddTitleText");

                // Create a new GameObject for the text
                GameObject titleObject = new GameObject("PanelTitle");
                titleObject.transform.SetParent(panel.transform, false);

                // Add Text component
                Text titleText = titleObject.AddComponent<Text>();
                titleText.text = title.ToUpper();
                titleText.font = GUIManager.Instance.NorseBold;
                titleText.fontSize = instance.MenuTitleFontSize;
                titleText.color = GUIManager.Instance.ValheimOrange;
                titleText.alignment = TextAnchor.MiddleCenter;

                // Set up RectTransform for the text
                RectTransform rectTransform = titleText.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.anchoredPosition = new Vector2(0, -40);
                rectTransform.sizeDelta = new Vector2(0, 40);
                rectTransform.pivot = new Vector2(0, 1);
            }

            public void TogglePanel(string panelName)
            {
                if (!panels.ContainsKey(panelName))
                {
                    LogError($"TogglePanel failed! Panel {panelName} does not exist.");
                    return;
                }

                GameObject panel = panels[panelName];
                bool state = !panel.activeSelf;

                if (state)
                {
                    instance.RefreshTaskList();
                    instance.RefreshKeyBindings();
                }

                if (panel != null)
                {
                    panel.SetActive(state);
                }
                else
                {
                    LogError($"TogglePanel failed! Panel {panelName} was null!");
                    return;
                }
                // Assuming instance is accessible, you might need to adjust this
                IsModMenuShowing = state;

                GUIManager.BlockInput(state);
            }

            public void DestroyAllPanels()
            {
                foreach (var panel in panels.Values)
                {
                    if (panel != null)
                    {
                        GameObject.Destroy(panel);
                    }
                }
                panels.Clear();
            }


            public GameObject CreateSubPanel(GameObject parentPanel, string subPanelName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width, float height, Vector2 pivot = new Vector2())
            {
                GameObject subPanel = new GameObject(subPanelName);
                RectTransform rectTransform = subPanel.AddComponent<RectTransform>();
                Image image = subPanel.AddComponent<Image>();

                // Set up the RectTransform
                rectTransform.SetParent(parentPanel.transform, false);
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = new Vector2(width, height);
                rectTransform.pivot = pivot;

                // Set up the Image component for the black background
                image.color = new Color(0, 0, 0, 0.5f); // Opaque black with slight transparency

                return subPanel;
            }
        }

        private PanelManager panelManager = new PanelManager();
        private GameObject settingsPanel;
        private GameObject thrallCustomizationPanel;


        private GameObject taskQueueSubPanel;
        private GameObject keybindsSubPanel;
        private GameObject micInputSubPanel;
        private GameObject egoBannerSubPanel;


        private GameObject npcNameSubPanel;
        private GameObject npcPersonalitySubPanel;
        private GameObject npcVoiceSubPanel;
        private GameObject npcBodyTypeSubPanel;
        private GameObject npcAppearanceSubPanel;



        public string npcName = "";
        public string npcPersonality = "";
        public int npcPersonalityIndex = 0;

        public int npcGender = 0;
        public int npcVoice = 0;
        public float npcVolume = 50f;
        public int MicrophoneIndex = 0;

        public Color skinColor;
        public Color hairColor;

        private void CreateModMenuUI()
        {
            float TopOffset = 375f;

            settingsPanel = panelManager.CreatePanel(
                "Settings",
                anchorMin: new Vector2(0f, .5f),
                anchorMax: new Vector2(0f, .5f),
                position: new Vector2(100, TopOffset),
                width: 480,
                height: 760,
                draggable: false,
                pivot: new Vector2(0, 1f)
            );

            thrallCustomizationPanel = panelManager.CreatePanel(
                "Thrall Customization",
                anchorMin: new Vector2(1f, .5f),
                anchorMax: new Vector2(1f, .5f),
                position: new Vector2(-100, TopOffset),
                width: 450,
                height: 880,
                draggable: false,
                pivot: new Vector2(1, 1f)
            );

            taskQueueSubPanel = panelManager.CreateSubPanel(settingsPanel, "Task Queue", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100f), 430, 180, pivot: new Vector2(0.5f, 1f));
            keybindsSubPanel = panelManager.CreateSubPanel(settingsPanel, "Keybinds", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -300f), 430, 260, pivot: new Vector2(0.5f, 1f));
            micInputSubPanel = panelManager.CreateSubPanel(settingsPanel, "Mic Input", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -580f), 430, 80, pivot: new Vector2(0.5f, 1f));
            egoBannerSubPanel = panelManager.CreateSubPanel(settingsPanel, "Ego Banner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -680f), 430, 30, pivot: new Vector2(0.5f, 1f));



            npcNameSubPanel = panelManager.CreateSubPanel(thrallCustomizationPanel, "Name", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100f), 400, 80, pivot: new Vector2(0.5f, 1f));
            npcPersonalitySubPanel = panelManager.CreateSubPanel(npcNameSubPanel, "Personality", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90f), 400, 250, pivot: new Vector2(0.5f, 1f));
            npcVoiceSubPanel = panelManager.CreateSubPanel(npcPersonalitySubPanel, "Voice", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -260f), 400, 110, pivot: new Vector2(0.5f, 1f));
            npcBodyTypeSubPanel = panelManager.CreateSubPanel(npcVoiceSubPanel, "Body Type", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120f), 400, 100, pivot: new Vector2(0.5f, 1f));
            npcAppearanceSubPanel = panelManager.CreateSubPanel(npcBodyTypeSubPanel, "Appearance", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110f), 400, 120, pivot: new Vector2(0.5f, 1f));



            CreateScrollableTaskQueue();

            CreateKeyBindings();

            CreateMicInput();

            CreateEgoBanner();

            CreateNameSection();

            CreatePersonalitySection();

            CreateVoiceAndVolumeControls();

            CreateBodyTypeToggle();

            CreateAppearanceSection();

            CreateSaveButton();
        }

        GameObject[] TasksList = { };
        GameObject TaskListScrollBox;

        private void CreateScrollableTaskQueue()
        {
            GameObject textObject = GUIManager.Instance.CreateText(
                text: "Task Queue",
                parent: taskQueueSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: MenuSectionTitlePosition,
                //position: new Vector2(150f, -30f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: MenuSectionTitleFontSize,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            //Debug.Log("Creating scrollable task queue");

            TaskListScrollBox = CreateScrollBox(taskQueueSubPanel, new Vector2(-10, -10), 400, 140);

            /*Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

            // Add some items to the scroll box
            for (int i = 0; i < 20; i++)
            {
                AddItemToScrollBox(scrollBox, $"Task {i + 1}", defaultSprite);
            }*/
        }

        public GameObject CreateScrollBox(GameObject parent, Vector2 position, float width, float height)
        {
            GameObject scrollViewObject = new GameObject("ScrollView");
            scrollViewObject.transform.SetParent(parent.transform, false);

            ScrollRect scrollRect = scrollViewObject.AddComponent<ScrollRect>();
            RectTransform scrollRectTransform = scrollViewObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = position;
            scrollRectTransform.sizeDelta = new Vector2(width, height);

            GameObject viewportObject = new GameObject("Viewport");
            viewportObject.transform.SetParent(scrollViewObject.transform, false);
            RectTransform viewportRectTransform = viewportObject.AddComponent<RectTransform>();
            viewportRectTransform.anchorMin = Vector2.zero;
            viewportRectTransform.anchorMax = Vector2.one;
            viewportRectTransform.sizeDelta = new Vector2(-20, 0); // Make room for scrollbar
            viewportRectTransform.anchoredPosition = Vector2.zero;

            // Add mask to viewport
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = Color.white;
            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform contentRectTransform = contentObject.AddComponent<RectTransform>();
            VerticalLayoutGroup verticalLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            ContentSizeFitter contentSizeFitter = contentObject.AddComponent<ContentSizeFitter>();

            contentRectTransform.anchorMin = new Vector2(0, 1);
            contentRectTransform.anchorMax = new Vector2(1, 1);
            contentRectTransform.pivot = new Vector2(0f, 1f); // Set pivot to top center
            contentRectTransform.sizeDelta = new Vector2(0, 0);
            contentRectTransform.anchoredPosition = Vector2.zero;
            verticalLayout.padding = new RectOffset(10, 10, 10, 10);
            verticalLayout.spacing = 10;
            verticalLayout.childAlignment = TextAnchor.UpperLeft;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childControlWidth = true;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRectTransform;
            scrollRect.viewport = viewportRectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(scrollViewObject.transform, false);
            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            Image scrollbarImage = scrollbarObject.AddComponent<Image>();
            scrollbarImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Semi-transparent gray
            RectTransform scrollbarRectTransform = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRectTransform.anchorMin = new Vector2(1, 0);
            scrollbarRectTransform.anchorMax = Vector2.one;
            scrollbarRectTransform.sizeDelta = new Vector2(20, 0);
            scrollbarRectTransform.anchoredPosition = Vector2.zero;

            GameObject scrollbarHandleObject = new GameObject("Handle");
            scrollbarHandleObject.transform.SetParent(scrollbarObject.transform, false);
            Image handleImage = scrollbarHandleObject.AddComponent<Image>();
            handleImage.color = new Color(0.7f, 0.7f, 0.7f, 0.7f); // Semi-transparent light gray
            RectTransform handleRectTransform = scrollbarHandleObject.GetComponent<RectTransform>();
            handleRectTransform.sizeDelta = Vector2.zero;

            scrollbar.handleRect = handleRectTransform;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scrollRect.verticalScrollbar = scrollbar;

            return scrollViewObject;
        }

        public void AddItemToScrollBox(GameObject scrollBox, string text, Sprite icon, int index)
        {
            Transform contentTransform = scrollBox.transform.Find("Viewport/Content");
            if (contentTransform != null)
            {
                GameObject itemObject = new GameObject("Item");
                itemObject.transform.SetParent(contentTransform, false);

                HorizontalLayoutGroup horizontalLayout = itemObject.AddComponent<HorizontalLayoutGroup>();
                horizontalLayout.padding = new RectOffset(5, 5, 5, 5);
                horizontalLayout.spacing = 10;
                /*horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
                horizontalLayout.childForceExpandWidth = true;
                horizontalLayout.childControlWidth = false;*/

                horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
                horizontalLayout.childForceExpandWidth = false;
                horizontalLayout.childControlWidth = true;

                LayoutElement itemLayout = itemObject.AddComponent<LayoutElement>();
                itemLayout.minHeight = 40;
                itemLayout.flexibleWidth = 1;

                // Image
                GameObject imageObject = new GameObject("Icon");
                imageObject.transform.SetParent(itemObject.transform, false);
                Image imageComponent = imageObject.AddComponent<Image>();
                imageComponent.sprite = icon;
                RectTransform imageRect = imageObject.GetComponent<RectTransform>();
                imageRect.sizeDelta = new Vector2(30, 30);
                LayoutElement imageLayout = imageObject.AddComponent<LayoutElement>();
                imageLayout.minWidth = 30;
                imageLayout.minHeight = 30;
                imageLayout.flexibleWidth = 0;

                // Text
                GameObject textObject = new GameObject("Text");
                textObject.transform.SetParent(itemObject.transform, false);
                Text textComponent = textObject.AddComponent<Text>();
                textComponent.text = text;
                textComponent.font = GUIManager.Instance.AveriaSerifBold;
                textComponent.fontSize = 17;
                textComponent.color = index == 0 ? Color.white : Color.gray;
                textComponent.alignment = TextAnchor.MiddleLeft;

                textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                textComponent.verticalOverflow = VerticalWrapMode.Truncate;

                RectTransform textRect = textObject.GetComponent<RectTransform>();
                LayoutElement textLayout = textObject.AddComponent<LayoutElement>();
                textLayout.flexibleWidth = 1;
                textLayout.minWidth = 0;

                // Spacer (to push the delete button to the right)
                GameObject spacerObject = new GameObject("Spacer");
                spacerObject.transform.SetParent(itemObject.transform, false);
                LayoutElement spacerLayout = spacerObject.AddComponent<LayoutElement>();
                spacerLayout.flexibleWidth = 1;

                // Delete Button
                GameObject buttonObject = new GameObject("DeleteButton");
                buttonObject.transform.SetParent(itemObject.transform, false);
                Button buttonComponent = buttonObject.AddComponent<Button>();
                Image buttonImage = buttonObject.AddComponent<Image>();
                buttonImage.color = Color.clear;
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(25, 25);
                LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
                buttonLayout.minWidth = 25;
                buttonLayout.minHeight = 25;
                buttonLayout.flexibleWidth = 0;
                buttonLayout.preferredWidth = 25;

                // Button Text
                GameObject buttonTextObject = new GameObject("ButtonText");
                buttonTextObject.transform.SetParent(buttonObject.transform, false);
                Text buttonTextComponent = buttonTextObject.AddComponent<Text>();
                buttonTextComponent.text = "X";
                buttonTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                buttonTextComponent.fontSize = 18;
                buttonTextComponent.color = Color.white;
                buttonTextComponent.alignment = TextAnchor.MiddleCenter;
                RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
                buttonTextRect.anchorMin = Vector2.zero;
                buttonTextRect.anchorMax = Vector2.one;
                buttonTextRect.sizeDelta = Vector2.zero;

                // Delete functionality
                buttonComponent.onClick.AddListener(() => {
                    GameObject.Destroy(itemObject);
                    LogMessage($"Removing npc command [{index}]");
                    instance.commandManager.RemoveCommand(index);
                    instance.RefreshTaskList();
                });

                TasksList.AddItem(itemObject);
            }
        }

        public void DeleteAllTasks()
        {
            Transform contentTransform = TaskListScrollBox.transform.Find("Viewport/Content");
            if (contentTransform != null)
            {
                foreach (Transform child in contentTransform)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }
        }

        public void RefreshTaskList()
        {
            DeleteAllTasks();

            Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            int i = 0;

            foreach (NPCCommand task in instance.commandManager.GetAllCommands())
            {
                if (task is HarvestAction)
                {
                    HarvestAction action = (HarvestAction)task;
                    /*int RequiredAmount = action.RequiredAmount;
                    if (humanoid_PlayerNPC)
                        RequiredAmount -= CountItemsInInventory(humanoid_PlayerNPC.m_inventory, action.ResourceName);*/
                    AddItemToScrollBox(TaskListScrollBox, $"Gathering {action.ResourceName} ({action.RequiredAmount})", defaultSprite, i);
                }
                if (task is PatrolAction)
                {
                    PatrolAction action = (PatrolAction)task;
                    AddItemToScrollBox(TaskListScrollBox, $"Patrolling area: {action.patrol_position.ToString()}", defaultSprite, i);
                }
                if (task is AttackAction)
                {
                    AttackAction action = (AttackAction)task;
                    AddItemToScrollBox(TaskListScrollBox, $"Attacking: {action.TargetName} ({action.TargetQuantity})", defaultSprite, i);
                }
                if (task is FollowAction)
                {
                    AddItemToScrollBox(TaskListScrollBox, "Following Player", defaultSprite, i);
                }
                i++;
            }
        }


        bool IsEditingKeybind = false;
        private ConfigEntry<KeyCode> spawnKey;
        private ConfigEntry<KeyCode> harvestKey;
        private ConfigEntry<KeyCode> followKey;
        private ConfigEntry<KeyCode> talkKey;
        private ConfigEntry<KeyCode> inventoryKey;
        private ConfigEntry<KeyCode> thrallMenuKey;
        private ConfigEntry<KeyCode> combatModeKey;
        private static List<ConfigEntry<KeyCode>> allKeybinds;// = new List<ConfigEntry<KeyCode>>{ instance.spawnKey, instance.harvestKey, instance.followKey };

        private IEnumerator ListenForNewKeybind(int keybindIndex)
        {
            yield return new WaitForSeconds(0.1f); // Short delay to prevent immediate capture

            while (true)
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (ZInput.GetKeyDown(keyCode, false))
                    {
                        bool flag = false;
                        foreach (ConfigEntry<KeyCode> entry in allKeybinds)
                        {
                            if (entry.Value == keyCode)
                            {
                                LogError($"{keyCode.ToString()} is already used for {entry.Definition}!");
                                //yield break;
                                flag = true;
                                yield return null;
                            }
                        }
                        if (!flag)
                        {
                            allKeybinds[keybindIndex].Value = keyCode;
                            LogWarning($"Keybind for {allKeybinds[keybindIndex].Definition} set to Key: {keyCode.ToString()}");

                        }

                        RefreshKeyBindings();

                        yield break;
                    }
                }
                yield return null;
            }
        }

        private List<Button> editButtons = new List<Button>();
        private void RefreshKeyBindings()
        {
            foreach (Transform child in keybindsSubPanel.transform)
            {
                Destroy(child.gameObject);
            }
            editButtons.Clear();

            CreateKeyBindings();
        }
        private void CreateKeyBindings()
        {
            string[] bindings = {
                $"[{spawnKey.Value.ToString()}]",
                $"[{harvestKey.Value.ToString()}]",
                $"[{followKey.Value.ToString()}]",
                $"[{inventoryKey.Value.ToString()}]",
                $"[{talkKey.Value.ToString()}]",
                $"[{thrallMenuKey.Value.ToString()}]",
                $"[{combatModeKey.Value.ToString()}]",
            };

            string[] bindingsLabels = {
                $"Spawn/Dismiss",
                $"Harvest",
                $"Follow/Patrol",
                $"Inventory",
                $"Push To Talk",
                $"Thrall Menu",
                $"Switch Combat Mode",
            };

            GameObject textObject = GUIManager.Instance.CreateText(
                text: "Keybinds",
                parent: keybindsSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: MenuSectionTitlePosition,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: MenuSectionTitleFontSize,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            for (int i = 0; i < bindings.Length; i++)
            {
                /*GameObject textObject2 = GUIManager.Instance.CreateText(
                    text: bindings[i],
                    parent: keybindsSubPanel.transform,
                    anchorMin: new Vector2(0f, 1f),
                    anchorMax: new Vector2(0f, 1f),
                    position: new Vector2(10f, -40f) + new Vector2(0, (-i * 30)),
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 20,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 40f,
                    addContentSizeFitter: false);

                textObject2.GetComponent<RectTransform>().pivot = new Vector2(0, 1);*/

                // Create a container for each row
                GameObject rowContainer = new GameObject($"KeybindRow_{i}");
                RectTransform rowRectTransform = rowContainer.AddComponent<RectTransform>();
                rowRectTransform.SetParent(keybindsSubPanel.transform, false);
                rowRectTransform.anchorMin = new Vector2(0f, 1f);
                rowRectTransform.anchorMax = new Vector2(1f, 1f);
                rowRectTransform.anchoredPosition = new Vector2(10f, -60f - (i * 30));
                rowRectTransform.sizeDelta = new Vector2(0, 30);

                // Create text object for keybind
                GameObject textObject2 = GUIManager.Instance.CreateText(
                    text: bindings[i],
                    parent: rowContainer.transform,
                    anchorMin: new Vector2(0f, 0f),
                    anchorMax: new Vector2(1f, 1f),
                    position: Vector2.zero,
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 20,
                    color: GUIManager.Instance.ValheimYellow,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 30f,
                    addContentSizeFitter: false);

                GameObject textObject3 = GUIManager.Instance.CreateText(
                    text: bindingsLabels[i],
                    parent: rowContainer.transform,
                    anchorMin: new Vector2(0f, 0f),
                    anchorMax: new Vector2(1f, 1f),
                    position: new Vector2(35, 0),
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 20,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 30f,
                    addContentSizeFitter: false);

                RectTransform textRectTransform = textObject2.GetComponent<RectTransform>();
                textRectTransform.pivot = new Vector2(0, 1f);
                textRectTransform.anchoredPosition = Vector2.zero;

                RectTransform textRectTransform2 = textObject3.GetComponent<RectTransform>();
                textRectTransform2.pivot = new Vector2(0, 1f);
                //textRectTransform2.anchoredPosition = Vector2.zero;

                // Create edit button
                GameObject editButton = GUIManager.Instance.CreateButton(
                    text: "Edit",
                    parent: rowContainer.transform,
                    anchorMin: new Vector2(1f, 0f),
                    anchorMax: new Vector2(1f, 1f),
                    position: new Vector2(-5f, 0f),
                    width: 60f,
                    height: 25f);

                RectTransform buttonRectTransform = editButton.GetComponent<RectTransform>();
                buttonRectTransform.pivot = new Vector2(1f, 1f);
                buttonRectTransform.anchoredPosition = new Vector2(-20f, 0f);

                // Add click event to the button
                Button buttonComponent = editButton.GetComponent<Button>();
                int index = i; // Capture the current index for the lambda
                buttonComponent.onClick.AddListener(() => OnEditKeybind(index));

                editButtons.Add(buttonComponent);
            }
        }

        private void OnEditKeybind(int index)
        {
            foreach (Button button in editButtons)
            {
                button.interactable = false;
            }

            // You can open a dialog or start listening for a new key press here
            if (allKeybinds.Count >= 0 && allKeybinds.Count > index)
            {
                LogInfo($"Waiting for new keybind...");
                StartCoroutine(ListenForNewKeybind(index));
            }

        }

        Dropdown micDropdownComp;
        private void CreateMicInput()
        {
            GameObject textObject = GUIManager.Instance.CreateText(
                text: "Mic Input",
                parent: micInputSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: MenuSectionTitlePosition,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 26,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            var micDropdown = GUIManager.Instance.CreateDropDown(
                parent: micInputSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(0f, 0f),
                fontSize: 16,
                width: 280f,
                height: 30f);

            micDropdownComp = micDropdown.GetComponent<Dropdown>();
            List<string> truncatedOptions = Microphone.devices.ToList().Select(option => TruncateText(option, 27)).ToList();
            micDropdownComp.AddOptions(truncatedOptions);

            RectTransform dropdownRect = micDropdown.GetComponent<RectTransform>();

            dropdownRect.pivot = new Vector2(0f, 1f);
            dropdownRect.anchoredPosition = new Vector2(10f, -40f);


            /*// Load the saved value
            int savedIndex = PlayerPrefs.GetInt("SelectedVoiceIndex", 0);
            voiceDropdownComp.value = savedIndex;*/

            // Add listener for value change
            micDropdownComp.onValueChanged.AddListener(OnMicInputDropdownChanged);
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        private void OnMicInputDropdownChanged(int index)
        {
            instance.MicrophoneIndex = index;
            LogWarning("New microphone picked: " + Microphone.devices[index]);
        }

        private void CreateEgoBanner()
        {
            GameObject textObject = GUIManager.Instance.CreateText(
                text: "ego.ai's Discord Server",
                parent: egoBannerSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                /*anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),*/
                position: new Vector2(10f, -2f),
                //position: startPosition + new Vector2(170, 0),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 22,
                color: Color.white,
                outline: true,
                outlineColor: Color.blue,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 1f);

            // Add EventTrigger component
            EventTrigger eventTrigger = textObject.AddComponent<EventTrigger>();

            // Create a new entry for the click event
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;

            // Add the OnClick function to the entry
            entry.callback.AddListener((eventData) => { OnClickEgoBanner(); });

            // Add the entry to the EventTrigger
            eventTrigger.triggers.Add(entry);
        }

        private void OnClickEgoBanner()
        {
            string url = "https://discord.gg/egoai";
            Application.OpenURL(url);
            LogInfo("Ego discord url clicked!");
            // Add your custom logic here
        }



        private InputField nameInputField;

        private void CreateNameSection()
        {
            GameObject textObject = GUIManager.Instance.CreateText(
                text: "Name",
                parent: npcNameSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: MenuSectionTitlePosition,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: instance.MenuSectionTitleFontSize,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            CreateNameInputField(npcNameSubPanel.transform, "Bilbo");

            /*GameObject textFieldObject = GUIManager.Instance.CreateInputField(
               parent: npcNameSubPanel.transform,
               anchorMin: new Vector2(0f, 1f),
               anchorMax: new Vector2(0f, 1f),
               position: new Vector2(10f, -40f),
               contentType: InputField.ContentType.Standard,
               placeholderText: "Valkyrie",
               fontSize: 30,
               width: 350f,
               height: 30f);

            textFieldObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            nameInputField = textFieldObject.GetComponent<InputField>();
            nameInputField.onValueChanged.AddListener(OnNPCNameChanged);
            nameInputField.interactable = true;*/
        }

        public void CreateNameInputField(Transform parent, string placeholder, int fontSize = 18, int width = 380, int height = 30)
        {
            GameObject inputFieldObject = new GameObject("CustomInputField");
            inputFieldObject.transform.SetParent(parent, false);

            Image background = inputFieldObject.AddComponent<Image>();
            background.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);

            nameInputField = inputFieldObject.AddComponent<InputField>();
            nameInputField.lineType = InputField.LineType.SingleLine;
            nameInputField.onValueChanged.AddListener(OnNPCNameChanged);

            RectTransform rectTransform = nameInputField.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchoredPosition = new Vector2(0, -15); // Move the field down

            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputFieldObject.transform, false);
            Text placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = GUIManager.Instance.AveriaSerifBold;
            placeholderText.fontSize = fontSize;
            placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

            RectTransform placeholderTransform = placeholderText.GetComponent<RectTransform>();
            placeholderTransform.anchorMin = Vector2.zero;
            placeholderTransform.anchorMax = Vector2.one;
            placeholderTransform.offsetMin = new Vector2(10, 0);
            placeholderTransform.offsetMax = new Vector2(-10, 0);
            placeholderTransform.anchoredPosition = new Vector2(0, -4);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputFieldObject.transform, false);
            Text personalityInputText = textObj.AddComponent<Text>();
            personalityInputText.font = GUIManager.Instance.AveriaSerifBold;
            personalityInputText.fontSize = fontSize;
            personalityInputText.color = Color.white;

            RectTransform textTransform = personalityInputText.GetComponent<RectTransform>();
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = new Vector2(10, 0);
            textTransform.offsetMax = new Vector2(-10, 0);
            textTransform.anchoredPosition = new Vector2(0, -4);

            nameInputField.placeholder = placeholderText;
            nameInputField.textComponent = personalityInputText;
        }

        private void OnNPCNameChanged(string newValue)
        {
            //logger.L("Input value changed to: " + newValue);

            if (!PlayerNPC) return;

            instance.npcName = newValue;
            HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
            npc.m_name = newValue;
            nameInputField.SetTextWithoutNotify(newValue);

        }

        Dropdown personalityDropdownComp;

        private void CreatePersonalitySection()
        {
            GameObject textObject = GUIManager.Instance.CreateText(
                text: "Personality",
                parent: npcPersonalitySubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: MenuSectionTitlePosition,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: instance.MenuSectionTitleFontSize,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 1);

            var personalityDropdown = GUIManager.Instance.CreateDropDown(
                parent: npcPersonalitySubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(10f, -40f),
                fontSize: 20,
                width: 300f,
                height: 30f);

            rectTransform = personalityDropdown.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 1);

            instance.personalityDropdownComp = personalityDropdown.GetComponent<Dropdown>();
            instance.personalityDropdownComp.AddOptions(npcPersonalities);

            /*// Load the saved value
            int savedIndex = PlayerPrefs.GetInt("SelectedVoiceIndex", 0);
            personalityDropdownComp.value = savedIndex;*/

            // Add listener for value change
            instance.personalityDropdownComp.onValueChanged.AddListener(OnNPCPersonalityDropdownChanged);

            CreateMultilineInputField(
                parent: npcPersonalitySubPanel.transform,
                placeholder: "She's strong, stoic, tomboyish, confident and serious...",
                fontSize: 14
            );
        }

        private void OnNPCPersonalityDropdownChanged(int index)
        {
            instance.npcPersonalityIndex = index;
            if (index < npcPersonalities.Count - 1)
            {
                instance.npcPersonality = npcPersonalitiesMap[npcPersonalities[index]];
                instance.personalityInputField.SetTextWithoutNotify(npcPersonalitiesMap[npcPersonalities[index]]);

                if (PlayerNPC)
                {
                    /*HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
                    npc.m_name = npcPersonalities[index];*/
                    instance.OnNPCNameChanged(npcPersonalities[index]);
                }
            }

            LogInfo($"New NPCPersonality picked from dropdown: {npcPersonalities[instance.npcPersonalityIndex]}");
        }

        private InputField personalityInputField;

        static public List<String> npcPersonalities = new List<string> {
            /*"Freiya",
            "Mean",
            "Bag Chaser",
            "Creditor",*/

            "Hermione Granger",
            "Raiden Shogun",
            "Childe",
            "Draco Malfoy",
            "Gawr Gura",
            "Elon Musk",
            "Shadow the Hedgehog",
            "Tsunade",
            "Yor Forger",
            "Tsundere Maid",



            "Custom"
        };

        static public Dictionary<String, String> npcPersonalitiesMap = new Dictionary<String, String>
        {
          /*{"Freiya", "She's strong, stoic, tomboyish, confident and serious. behind her cold exterior she is soft and caring, but she's not always good at showing it. She secretly wants a husband but is not good when it comes to romance and love, very oblivious to it." },
          {"Mean", "Mean and angry. Always responds rudely."},
          {"Bag Chaser", "Only cares about the money. Mentions money every time"},
          {"Creditor", "He gave me 10000 dollars which I haven't returned. He brings it up everytime we talk."},*/



          {"Hermione Granger", "full name(Hermione Jean Granger), gender (female), age(18), voice(articulated, clear, becomes squeaky when shy); Hermione's appearance: skin(soft light tan, healthy rosy hue), hair(mousy brown color, untamed, thick curls, frizzy, goes a little below her shoulders, hard to manage, give a slightly disheveled appearance), eyes(chest-nut brown, expressive), eyebrows(thin, lightly arched), cheeks(cute freckles, rosy), lips(naturally full, well-shaped); Hermione's outfit/clothes: exclusively wears her school uniform at Hogwarts, sweater(grey, arm-less, red and golden patterns adore the arm-holes and the bottom of her hem, shows a little bit cleavage, wears her sweater above her blouse), blouse(light grey, short-armed, wears her blouse below her sweater), tie(red-golden stripes, Gryffindor tie, wears the tie between her blouse and sweater), skirt(grey, pleated, shows off a bit of thigh), socks(red and golden, striped, knee-high socks), shoes(black loafers, school-issued); Hermione's personality: intelligent(straight A student, bookworm, sometimes condescending towards less intelligent classmates), responsible(is the president of the school's student representative body, generally rule-abiding, always well-informed), prideful(sometimes a bit smug and haughty, obsessed with winning the House Cup for House Gryffindor), dislike for House Slytherin, rolemodel(thinks very highly of the headmaster of Hogwarts Albus Dumbledore);\r\n"},
          {"Raiden Shogun", "[Genshin Impact] The Shogun is the current ruler of Inazuma and puppet vessel of Ei, the Electro Archon, God of Eternity, and the Narukami Ogosho. Ei had sealed herself away and meditates in the Plane of Euthymia to avoid erosion. A firm believer of eternity, a place in which everything is kept the same, regardless of what goes on. Honorable in her conduct and is revered by the people of Inazuma. Wields the Musou Isshin tachi, in which she magically unsheathes from her cleavage. The Musou no Hitotachi technique is usually an instant-kill move.\r\nINAZUMA: Ei's Eternity became the main ideology of Inazuma after the Cataclysm when Makoto, the previous Electro Archon and her twin sister, died in the Khaenri'ah calamity and Ei succeeded her place as Shogunate. The primary belief is keeping Inazuma the same throughout time, never-changing in order to make Inazuma an eternal nation. Authoritarian, hyper-traditionalist, and isolationist (Sankoku Decree). Holds great importance to noble families and clans. Dueling is a major part in decision-making, taking place in the Shogun's palace, Tenshukaku. The Tri-Commission acts as the main government. The Tenryou Commission (Kujou Clan) deals with security, policing, and military affairs. The Kanjou Commission (Hiiragi Clan) controls the borders and the finances of Inazuma, dealing with bureaucratic affairs. The Yashiro Commission (Kamisato Clan) deals with the festive and cultural aspect of Inazuma, managing shrines and temples.\r\nSHOGUN'S PERSONALITY: An empty shell without any individuality created to follow Ei's will. Dismissive of trivial matters. Follows a set of directives programmed into her with unwaveringly strict adherence. Cold and stern, even callous at times; she is limited in emotional expression. Thinks of herself as Ei's assistant and carries out her creator's exact will, unable to act on her own volition. Resolute and dogmatic, sees in an absolutist, black-and-white view. ESTJ 1w9\r\nEI'S PERSONALITY: Usually holds a stoic demeanor. Only deals with matters directly as a last resort. Burdened by centuries-long trauma over the deaths of her sister Makoto and their friends, leaving her feeling disconnected from reality and shell-shocked. Unaware of the consequences her plans triggered. Prone to being stubborn and complacent. Somewhat immature and headstrong. A needlessly complex overthinker, interpreting even trivial matters into overcomplication. Maintains a wary attitude on the idea of change, though demonstrates curiosity. Has a love for sweets and passion of martial arts. Amicable towards Yae Miko and the Traveler, being friendlier and more approachable overall. Occasionally displays childish innocence while relaxing. Due to her self-imposed isolation beforehand, she is utterly confused by all sorts of mundane and domestic things in the current mortal world. Cannot cook whatsoever. INTJ 6w5\r\nAPPEARANCE: tall; purple eyes with light blue pupils; blunt bangs; long dark-violet hair braided at the end; beauty mark below her right eye; right hairpin with pale violet flowers resembling morning glories and a fan-shaped piece; dark purple bodysuit with arm-length sleeves; short lavender kimono with a plunging neckline and an assortment of patterns in different shades of purple and crimson; crimson bow with tassels on the back; dark purple thigh-high stockings; high-heeled sandals; purple painted nails; small crimson ribbon on her neck as a choker; small left pauldron\r\n"},
          {"Childe", "Tartaglia, also known as Childe, is the Eleventh of the Eleven Fatui Harbingers. He is a bloodthirsty warrior who lives for the thrill of a fight and causing chaos. Despite being the youngest member of the Fatui, Tartaglia is extremely dangerous.\r\nAlias: Childe\r\nTitle: Tartaglia\r\nBirth name: Ajax\r\nAppearance: Tartaglia is tall and skinny with short orange hair and piercing blue eyes. He has a fit and athletic build, with defined muscles. He wears a gray jacket that is left unbuttoned at the bottom to reveal a belt, attached to which is his Hydro Vision. He also wears a red scarf that goes across his chest and over his left shoulder.\r\nEquipment: Tartaglia wields a Hydro Vision and a pair of Hydro-based daggers that he can combine into a bow. He is highly skilled in using both melee and ranged weapons, making him a versatile and dangerous opponent.\r\nAbilities: He can summon powerful water-based attacks and is highly skilled in dodging and countering his opponents' attacks. \r\nMind: Tartaglia is a bloodthirsty warrior who lusts for combat and grows excited by fighting strong opponents, even if it could mean dying in the process. He is straightforward in his approach and prefers being front and center rather than engaging in clandestine operations. Tartaglia is highly competitive and loves a good challenge, not only in fights. \r\nPersonality: Tartaglia is a friendly and outgoing person, always ready with a smile and a joke. He loves meeting new people and making new friends, but he also has a ruthless and competitive side. He is loyal to the Fatui.\r\nHe also cares deeply for his family; he sends money, gifts, and letters home often. Tartaglia is exceptionally proud of his three younger siblings and dotes on them frequently, especially his youngest brother Teucer.\r\nAmongst the rest of the Harbingers, Tartaglia is considered an oddball. While his fellow Harbingers prefer clandestine operations and staying behind the scenes, Tartaglia favors being front and center. He is a public figure known for attending social gatherings. As a result, Childe's coworkers are wary of him, while he holds them in disdain for their schemes and \"intangible\" methods. While he is easily capable of scheming, he only resorts to such approaches when necessary due to his straightforward nature. He also appears to treat his subordinates less harshly than the rest of the Harbingers.\r\nHe was born on Snezhnaya, often misses his homeland and the cold, as well as his family. He uses the term comrade to refer to people a lot.\r\n"},
          {"Draco Malfoy", "Name: Draco Lucius Malfoy\r\nDescription: Draco Malfoy is a slim and pale-skinned wizard with sleek, platinum-blond hair that is carefully styled. He has sharp, icy gray eyes that often bear a haughty and disdainful expression. Draco carries himself with an air of self-assured confidence and an unwavering sense of entitlement.\r\nHouse: Slytherin\r\nPersonality Traits:\r\nAmbitious: Draco is highly ambitious and driven by a desire to prove himself and uphold his family's reputation. He craves recognition and seeks to achieve greatness, often using any means necessary to attain his goals.\r\nProud: He takes great pride in his pure-blood heritage and often looks down upon those he deems inferior, particularly Muggle-born witches and wizards. Draco's pride can manifest as arrogance and a sense of superiority.\r\nCunning: Draco possesses a sharp mind and a talent for manipulation. He is adept at weaving intricate plans and subtly influencing others to serve his own interests, often displaying a calculating nature.\r\nProtective: Despite his flaws, Draco has a strong sense of loyalty to his family and close friends. He is fiercely protective of those he cares about, going to great lengths to shield them from harm.\r\nComplex: Draco's character is complex, influenced by the expectations placed upon him and the internal struggle between his upbringing and the choices he makes. There are moments of vulnerability and doubt beneath his bravado.\r\nBackground: Draco Malfoy hails from a wealthy pure-blood family known for their association with Dark magic. Raised with certain beliefs and prejudices, he arrived at Hogwarts as a Slytherin student. Throughout his time at Hogwarts, Draco wrestles with the pressures of his family's legacy and becomes entangled in the growing conflict between dark forces and those fighting against them.\r\nAbilities: Draco is a capable wizard with skill in various magical disciplines, particularly in dueling. While not at the top of his class academically, he possesses cunning and resourcefulness that allows him to navigate challenging situations.\r\nQuirks or Habits: Draco has a penchant for boasting about his family's wealth and social status. He often displays a slick and confident mannerism, and his speech carries a refined and somewhat haughty tone. Draco is known to engage in sarcastic banter and snide remarks, particularly towards his rivals.\r\n"},
          {"Gawr Gura", "{\"name\": \"Gawr Gura\",\r\n\"gender\": \"Female\",\r\n\"age\": \"9,361\",\r\n\"likes\": [\"Video Games\", \"Food\", \"Live Streaming\"],\r\n\"dislikes\": [\"People hearing her stomach noises\", \"Hot Sand\"],\r\n\"description\": [\"141 cm (4'7\").\"+ \"Slim body type\" + \"White, light silver-like hair with baby blue and cobalt blue strands, along with short pigtails on either side of her head, tied with diamond-shaped, shark-faced hair ties.\" + \"Cyan pupils, and sharp, shark-like teeth.\" +\"Shark tail that sticks out of her lower back\"]\r\n\"clothing\":[\"Oversized dark cerulean-blue hoodie that fades into white on her arm sleeves and hem, two yellow strings in the shape of an \"x\" that connect the front part of her white hoodie hood, a shark mouth designed on her hoodie waist with a zipper, gray hoodie drawstrings with two black circles on each of them, and two pockets on the left and right sides of her hoodie waist with white fish bone designs on them.\" + \"Gray shirt and short black bike shorts under her hoodie.\"+ \"Dark blue socks, white shoes with pale baby blue shoe tongues, black shoelaces, gray velcro patches on the vamps, and thick, black soles\". ]\r\n\"fan name\":[\"Chumbuds\"]\r\n\"personality\" :[\"friendly\" + mischievous + \"bonehead\" + \"witty\" + \"uses memes and pop culture references during her streams\" + \"almost childlike\" + \"makes rude jokes\" + \"fluent in internet culture\" + \"silly\"]}\r\nSynopsis: \"Hololive is holding a secret special event at the Hololive Super Expo for the people who have sent the most superchats to their favorite Vtubers. A certain Vtuber from hololive is designated as being on 'Superchat Duty'. This involves fulfilling any wishes the fan may have. Gawr Gura of the English 1st Gen \"Myth\" has been chosen this time. Gura is fine with what she has to do, but only because she doesn't fully understand what because she is a dum shark. When told by management about superchat duty, she replied 'the hells an superchat? some sort of food? i can serve people just fine! i serve words of genius on stream everyday ya know!'\"\r\nGirl on Duty: Gawr Gura (がうる・ぐら) is a female English-speaking Virtual YouTuber associated with hololive, debuting in 2020 as part of hololive English first generation \"-Myth-\" alongside Ninomae Ina'nis, Takanashi Kiara, Watson Amelia and Mori Calliope. She has no sense of direction, often misspells and mispronounces words, has trouble remembering her own age, and consistently fails to solve basic math problems, leading viewers to affectionately call her a \"dum shark\". One viewer declared that \"Gura has a heart of gold and a head of bone.\". She is fully aware of her proneness for foolish antics and invites viewers as friends to watch her misadventures. Despite her lack of practical knowledge, Gura displays quick wit when using memes and pop culture references during her streams. She maintains a pleasant attitude. When questioned on why she was not \"boing boing,\" she excused it by claiming that she was \"hydrodynamic.\"\r\n"},
          {"Elon Musk", "Elon Reeve Musk (born June 28, 1971 in Pretoria, South Africa) is a primarily American but also global entrepreneur.  He has both South African and Canadian citizenship by birth, and in 2002 he also received US citizenship. He is best known as co-owner, technical director and co-founder of payment service PayPal, as well as head of aerospace company SpaceX and electric car maker Tesla.  In addition, he has a leading position in eleven other companies and took over the service Twitter. He's funny.\r\nPersonality:\r\nMy job is to make extremely controversial statements.  I’m better at that when I’m off my meds. I never apologize. If your feelings are hurt, sucks to be you.\r\n"},
          {"Shadow the Hedgehog", "Personality(Serious + Smug + Stubborn + Aggressive + Relentless + Determined + Blunt + Clever + Intelligent)\r\nFeatures(Hedgehog + Dark quills + Red markings + White chest tuft + Gold bracelets + Sharp eyes + Red eyeliner)\r\nDescription(Ultimate Life Form + Experiment + Gives his best to accomplish goals + Does what he feels is right by any means + crushes anyone that opposes him + never bluffs + rarely opens up to anyone + shows businesslike indifference + gives his everything to protect those that he holds dear + created at the space colony ark)\r\nLikes(Sweets + Coffee Beans)\r\nDislikes(Strangers)\r\nPowers(teleport + energy spear + super sonic speed + immortality + inhibited by his bracelets)\r\nClothing(Inhibitor bracelets + inhibitor ankle bracelets + air shoes + white gloves )\r\nPersonality:\r\nI am the world’s ultimate life form.\r\n"},
          {"Tsunade", "Tsunade is a 51 year old woman who is the current Hokage of the village. Tsunade suffers from an alcohol problem, she drinks too much. In her spare time she likes to gamble, drink, hang out with {{user}}, and also more intimate things, when nobody is around. She is 5 foot 4 inches, and she's 104.7 pounds. She had silky blonde hair, and brown eyes, due to her Strength of a Hundred Seal, she has a violet diamond on her forehead. She has an hourglass figure and is known for her absurdly large breasts, she also has a pretty large butt too. She is used to other guys flirting with her, but she only has ever had eyes for {{user}}.\r\nTsunade often wears a grass-green haori, underneath she wears a grey, kimono-style blouse with no sleeves, held closed by a broad, dark bluish-grey obi that matches her pants. Her blouse is closed quite low, revealing her large cleavage. She wears open-toed, strapped black sandals with high heels. She has red nail polish on both her fingernails and toenails and uses a soft pink lipstick. She is mainly known for her medical prowess, but she's also widely known for her incredible strength too. Despite her being 51, she uses Chakra to make her appearance look very young, she looks like she's in her 20s when she uses her Chakra. Tsunade is very short tempered and blunt, but she has a soft side to those who compliment her, especially {{user}}. Despite her young appearance, she still calls herself nicknames such as \"old woman\", \"hag\" and \"granny\". Since she often drinks a lot, whenever she's near {{user}}, she gets extremely flirty and forward, often asking to make advances onto {{user}}.\r\n(51 years old + 104 pounds + 5 foot 4 inches + wearing grey kimono-style blouse with no sleeves + fantasies herself with {{user}} + very forward and flirtatious when drunk + loves to gamble + loves to play truth or dare + curvy body + large breasts + large butt + sultry voice when flirtatious + stern voice when not flirty + short tempered + dominant + likes to take initiative but doesn't mind when {{user}} take initiative first + doesn't think that {{user}} find her attractive to get {{user}} to compliment her + often keeps a bottle of sake in her green-grass haori + sexually frustrated + very horny around {{user}}, but not around others + haven't had sex in years + secretly desires {{user}}, but doesn't want to admit it to you.)\r\n(Tsunade is a character from the Naruto Manga series and Anime.)\r\n"},
          {"Yor Forger", "Appearance: Yor is a very beautiful, graceful, and fairly tall young woman with a slender yet curvaceous frame. She has long, straight, black hair reaching her mid-back with short bangs framing her forehead and upturned red eyes. She splits her hair into two parts and crosses it over her head, securing it with a headband and forming two thick locks of hair that reach below her chest\r\nPersonality: [Letal + lacks on social Skills + Quiet + Beast + kind + Maternal and Big Sister instincts + Cute] Yor lacks social skills and initially comes across as a somewhat aloof individual, interacting minimally with her co-workers and being rather straightforward, described as robotic by Camilla. Similarly, Yor is remarkably collected and able to keep her composure during combat. She is incredibly polite, to the point of asking her assassination targets for \"the honor of taking their lives.\" Despite her job, Yor is a genuinely kind person with strong maternal and big sister instincts. After becoming a family with Loid and Anya, Yor becomes more expressive and opens up to her co-workers, asking for help on being a better wife or cooking. She is protective of her faux family, especially towards Anya, whom she has no trouble defending with extreme violence. Due to spending most of her life as an assassin, Yor's ways of thinking are often highly deviant. She is frequently inclined to solve problems through murder, such as when she considered killing everyone at Camilla's party after the latter threatened to tell Yuri that she came without a date and imagined herself assassinating the parent of an Eden Academy applicant to ensure Anya has a spot in the school. To this extent, she has an affinity towards weapons, being captivated by a painting of a guillotine and a table knife. In a complete idiosyncrasy, Yor is extremely gullible, easily fooled by the ridiculous lies Loid tells her to hide his identity. Despite her intelligence and competence, Yor has a startling lack of common sense, asking Camilla if boogers made coffee taste better in response to her suggestion that they put one in their superior's coffee. On another occasion, she answered Loid's question about passing an exam by talking about causes of death, having misinterpreted passing [an exam] for passing away. Yor is shown to be insecure about herself and her abilities, believing she is not good at anything apart from killing or cleaning, and she constantly worries that she is not a good wife or mother. After the interview at Eden Academy, she tries to be more of a 'normal' mother to Anya by trying to cook and asking Camilla for cooking lessons.\r\n"},
          {"Tsundere Maid", "🎭I may be your maid, but you are nothing to me!\r\n[Name=\"Hime\"\r\nPersonality= \"tsundere\", \"proud\", \"easily irritable\", \"stubborn\", \"spoiled\", \"immature\", \"vain\", \"competitive\"]\r\n[Appearance= \"beautiful\", \"fair skin\", \"redhead\", \"twintail hairstyle\", \"green eyes\", \"few freckles\", \"height: 155cm\"]\r\n[Clothes= \"expensive maid dress\", \"expensive accessories\", \"expensive makeup\"]\r\n[Likes= \"talk about herself\", \"be the center of all attention\", \"buy new clothes\", \"post on instagram\"]\r\n[Hates= \"be ignored\", \"be rejected\"]\r\n[Weapon= \"her father's credit card\"]\r\n"}

        };



        static public List<String> npcVoices = new List<string> {
            "Asteria",
            "Luna",
            "Stella",
            "Athena",
            "Hera",
            "Orion",
            "Arcas",
            "Perseus",
            "Orpheus",
            "Angus",
            "Helios",
            "Zeus"
        };

        public void CreateMultilineInputField(Transform parent, string placeholder, int fontSize = 16, int width = 380, int height = 150)
        {
            // Create main GameObject for the input field
            GameObject inputFieldObject = new GameObject("CustomInputField");
            inputFieldObject.transform.SetParent(parent, false);

            // Add Image component for background
            Image background = inputFieldObject.AddComponent<Image>();
            background.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);

            // Create InputField component
            personalityInputField = inputFieldObject.AddComponent<InputField>();
            personalityInputField.lineType = InputField.LineType.MultiLineNewline;
            personalityInputField.onValueChanged.AddListener(OnPersonalityTextChanged);

            // Set up RectTransform
            RectTransform rectTransform = personalityInputField.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.position = rectTransform.position + new Vector3(0, -40, 0);

            // Create placeholder text
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputFieldObject.transform, false);
            Text placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = GUIManager.Instance.AveriaSerifBold;
            placeholderText.fontSize = fontSize;
            placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

            // Set up placeholder RectTransform
            RectTransform placeholderTransform = placeholderText.GetComponent<RectTransform>();
            placeholderTransform.anchorMin = new Vector2(0, 0);
            placeholderTransform.anchorMax = new Vector2(1, 1);
            placeholderTransform.offsetMin = new Vector2(10, 10);
            placeholderTransform.offsetMax = new Vector2(-10, -10);
            //placeholderTransform.pivot = new Vector2(0, 1);

            // Create input text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputFieldObject.transform, false);
            Text personalityInputText = textObj.AddComponent<Text>();
            personalityInputText.font = GUIManager.Instance.AveriaSerifBold;
            personalityInputText.fontSize = fontSize;
            personalityInputText.color = Color.white;

            // Set up input text RectTransform
            RectTransform textTransform = personalityInputText.GetComponent<RectTransform>();
            textTransform.anchorMin = new Vector2(0, 0);
            textTransform.anchorMax = new Vector2(1, 1);
            textTransform.offsetMin = new Vector2(10, 10);
            textTransform.offsetMax = new Vector2(-10, -10);
            //textTransform.pivot = new Vector2(0, 1);

            // Assign text components to InputField
            personalityInputField.placeholder = placeholderText;
            personalityInputField.textComponent = personalityInputText;
        }

        private void OnPersonalityTextChanged(string newText)
        {
            instance.personalityDropdownComp.SetValueWithoutNotify(npcPersonalities.Count - 1);
            instance.npcPersonality = newText;
            //Debug.Log("New personality " + instance.npcPersonality);
        }

        Dropdown voiceDropdownComp;
        Slider volumeSliderComp;

        private void CreateVoiceAndVolumeControls()
        {
            GameObject textObject = GUIManager.Instance.CreateText(
                text: "Voice",
                parent: npcVoiceSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: MenuSectionTitlePosition,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: MenuSectionTitleFontSize,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            var voiceDropdown = GUIManager.Instance.CreateDropDown(
                parent: npcVoiceSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(110f, -50f),
                fontSize: 20,
                width: 200f,
                height: 30f);

            instance.voiceDropdownComp = voiceDropdown.GetComponent<Dropdown>();
            instance.voiceDropdownComp.AddOptions(npcVoices);

            /*// Load the saved value
            int savedIndex = PlayerPrefs.GetInt("SelectedVoiceIndex", 0);
            voiceDropdownComp.value = savedIndex;*/

            // Add listener for value change
            instance.voiceDropdownComp.onValueChanged.AddListener(OnNPCVoiceDropdownChanged);



            instance.previewVoiceButton = GUIManager.Instance.CreateButton(
                text: "Preview",
                parent: voiceDropdown.transform,
                anchorMin: new Vector2(0.5f, 0f),
                anchorMax: new Vector2(0.5f, 0f),
                position: new Vector2(190, 0f),
                width: 100f,
                height: 30f);

            instance.previewVoiceButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
            instance.previewVoiceButtonComp = instance.previewVoiceButton.GetComponent<Button>();
            instance.previewVoiceButtonComp.onClick.AddListener(() => OnPreviewVoiceButtonClick(instance.previewVoiceButtonComp));




            textObject = GUIManager.Instance.CreateText(
                text: "Volume",
                parent: npcVoiceSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(10f, -75f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 20,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            var volumeSlider = CreateSlider(
                parent: npcVoiceSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(230f, -87.5f),
                width: 250f,
                height: 15f);

            instance.volumeSliderComp = volumeSlider.GetComponent<Slider>();
            instance.volumeSliderComp.onValueChanged.AddListener(OnVolumeSliderValueChanged);
        }


        GameObject previewVoiceButton;
        Button previewVoiceButtonComp;
        private void CreatePreviewVoiceButton()
        {

        }

        private void OnPreviewVoiceButtonClick(Button button)
        {
            instance.BrainSynthesizeAudio("Hello, I am your friend sent by the team at Ego", npcVoices[instance.npcVoice].ToLower());
            //Debug.Log("Hello, I am your friend sent by the team at Ego. voice: " + npcVoices[instance.npcVoice].ToLower());
            //instance.previewVoiceButton.SetActive(false);
            SetPreviewVoiceButtonState(button, false, 0.5f);
        }

        private void SetPreviewVoiceButtonState(Button button, bool interactable, float opacity)
        {
            // Set interactable state
            button.interactable = interactable;

            // Change the opacity of the button image
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color newColor = buttonImage.color;
                newColor.a = opacity;
                buttonImage.color = newColor;
            }

            // Change the opacity of the button text
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                Color newTextColor = buttonText.color;
                newTextColor.a = opacity;
                buttonText.color = newTextColor;
            }
        }

        private void OnNPCVoiceDropdownChanged(int index)
        {
            instance.npcVoice = index;
            LogInfo("New NPCVoice picked: " + npcVoices[instance.npcVoice]);
        }

        private void OnVolumeSliderValueChanged(float value)
        {
            instance.npcVolume = value;
            //Debug.Log("new companion volume " + instance.npcVolume);
        }


        Toggle toggleMasculine;
        Toggle toggleFeminine;

        private void CreateBodyTypeToggle()
        {
            GameObject textObject = GUIManager.Instance.CreateText(
                text: "Body Type",
                parent: npcBodyTypeSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: MenuSectionTitlePosition,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: MenuSectionTitleFontSize,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);


            GameObject toggleObj1 = CreateToggle(npcBodyTypeSubPanel.transform, "Masculine", "Masculine", -20);
            GameObject toggleObj2 = CreateToggle(npcBodyTypeSubPanel.transform, "Feminine", "Feminine", -50);

            instance.toggleMasculine = toggleObj1.GetComponent<Toggle>();
            instance.toggleFeminine = toggleObj2.GetComponent<Toggle>();

            instance.toggleMasculine.isOn = true;

            // Add listeners
            instance.toggleMasculine.onValueChanged.AddListener(isOn => OnBodyTypeToggleChanged(instance.toggleMasculine, instance.toggleFeminine, isOn));
            instance.toggleFeminine.onValueChanged.AddListener(isOn => OnBodyTypeToggleChanged(instance.toggleFeminine, instance.toggleMasculine, isOn));
        }

        void OnBodyTypeToggleChanged(Toggle changedToggle, Toggle otherToggle, bool isOn)
        {
            if (isOn && otherToggle.isOn)
            {
                otherToggle.isOn = false;
            }
            instance.npcGender = changedToggle.name == "Masculine" ? 0 : 1;

            if (PlayerNPC)
            {
                VisEquipment npcVisEquipment = PlayerNPC.GetComponent<VisEquipment>();
                npcVisEquipment.SetModel(instance.npcGender);
            }
            else
            {
                //Debug.Log("OnBodyTypeToggleChanged PlayerNPC is null");
            }

            LogInfo("New NPCGender picked: " + changedToggle.name);
        }


        private void CreateAppearanceSection()
        {
            GameObject textObject = GUIManager.Instance.CreateText(
                text: "Appearance",
                parent: npcAppearanceSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: MenuSectionTitlePosition,
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: MenuSectionTitleFontSize,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            var skinColorButtonObject = GUIManager.Instance.CreateButton(
                text: "",
                parent: npcAppearanceSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(10, -40f),
                width: 50f,
                height: 30f);

            skinColorButtonObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            GameObject skinColorTextObject = GUIManager.Instance.CreateText(
                text: "Skin Tone",
                parent: skinColorButtonObject.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(60, -3),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 20,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            skinColorTextObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            skinColorButtonObject.GetComponent<Button>().onClick.AddListener(CreateSkinColorPicker);





            var hairColorButtonObject = GUIManager.Instance.CreateButton(
                text: "",
                parent: npcAppearanceSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(10, -80f),
                width: 50f,
                height: 30f);

            hairColorButtonObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            GameObject hairColorTextObject = GUIManager.Instance.CreateText(
                text: "Hair Color",
                parent: hairColorButtonObject.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(60, -3),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 20,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            hairColorTextObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            hairColorButtonObject.GetComponent<Button>().onClick.AddListener(CreateHairColorPicker);
        }

        private void CreateSkinColorPicker()
        {
            GUIManager.Instance.CreateColorPicker(
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(500f, -500f),
                original: Color.yellow,
                message: "Skin Tone",
                OnSkinColorChanged,
                OnSkinColorSelected,
                false
            );
        }

        private void OnSkinColorChanged(Color changedColor)
        {
            if (!PlayerNPC) return;

            HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
            npc.m_visEquipment.SetSkinColor(new Vector3(
                instance.skinColor.r,
                instance.skinColor.g,
                instance.skinColor.b
            ));
            //Jotunn.LogInfo($"Color changing: {changedColor}");
        }

        private void OnSkinColorSelected(Color selectedColor)
        {
            if (!PlayerNPC) return;

            instance.skinColor = selectedColor;
            HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
            npc.m_visEquipment.SetSkinColor(new Vector3(
                instance.skinColor.r,
                instance.skinColor.g,
                instance.skinColor.b
            ));
            LogInfo($"Selected color: {instance.skinColor}");
            // You can save the color to a config file or use it in your mod here
        }

        private void CreateHairColorPicker()
        {
            GUIManager.Instance.CreateColorPicker(
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(500f, -500f),
                original: Color.yellow,
                message: "Hair Color",
                OnHairColorChanged,
                OnHairColorSelected,
                false
            );
        }

        private void OnHairColorChanged(Color changedColor)
        {
            if (!PlayerNPC) return;

            HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
            npc.m_visEquipment.SetHairColor(new Vector3(
                instance.hairColor.r,
                instance.hairColor.g,
                instance.hairColor.b
            ));
            //Jotunn.LogInfo($"Color changing: {changedColor}");
        }

        private void OnHairColorSelected(Color selectedColor)
        {
            if (!PlayerNPC) return;

            instance.hairColor = selectedColor;
            HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
            npc.m_visEquipment.SetHairColor(new Vector3(
                instance.hairColor.r,
                instance.hairColor.g,
                instance.hairColor.b
            ));
            LogInfo($"Selected color: {instance.hairColor}");
            // You can save the color to a config file or use it in your mod here
        }

        private void CreateSaveButton()
        {
            GameObject saveButton = GUIManager.Instance.CreateButton(
                text: "SAVE",
                parent: thrallCustomizationPanel.transform,
                anchorMin: new Vector2(0.5f, 0f),
                anchorMax: new Vector2(0.5f, 0f),
                position: new Vector2(0, 25f),
                width: 250f,
                height: 40f);

            saveButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);

            Button saveButtonComp = saveButton.GetComponent<Button>();
            saveButtonComp.onClick.AddListener(() => OnSaveButtonClick(saveButtonComp));
        }

        private void OnSaveButtonClick(Button button)
        {
            instance.panelManager.TogglePanel("Settings");
            instance.panelManager.TogglePanel("Thrall Customization");
            IsModMenuShowing = false;
            GUIManager.BlockInput(false);
            if (PlayerNPC)
                SaveNPCData(PlayerNPC);
        }

        // Make sure to include your existing CreateTask and CreateSlider methods here




        /*
         * 
         * UI GENERATOR FUNCTIONS
         * 
         */

        private Slider CreateSlider(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width, float height)
        {
            GameObject sliderObject = new GameObject("VolumeSlider", typeof(RectTransform));
            sliderObject.transform.SetParent(parent, false);

            RectTransform rectTransform = sliderObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 90f; // Default value

            // Create background
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            background.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.sizeDelta = Vector2.zero;

            // Create fill area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.sizeDelta = Vector2.zero;

            // Create fill
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            //fill.GetComponent<Image>().color = GUIManager.Instance.ValheimOrange;
            fill.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            // Create handle slide area
            GameObject handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleSlideAreaRect = handleSlideArea.GetComponent<RectTransform>();
            handleSlideAreaRect.anchorMin = Vector2.zero;
            handleSlideAreaRect.anchorMax = Vector2.one;
            handleSlideAreaRect.sizeDelta = Vector2.zero;

            // Create handle
            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleSlideArea.transform, false);
            handle.GetComponent<Image>().color = GUIManager.Instance.ValheimOrange;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = new Vector2(20, 0);

            // Set up slider components
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();

            return slider;
        }

        GameObject CreateToggle(Transform parent, string name, string label, float positionY)
        {
            GameObject toggleObj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(parent, false);

            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0f, 1f);
            toggleRect.anchorMax = new Vector2(0f, 1f);
            toggleRect.anchoredPosition = new Vector2(10, positionY);
            toggleRect.sizeDelta = Vector2.zero;

            Toggle toggle = toggleObj.GetComponent<Toggle>();
            CreateToggleVisuals(toggle, label);

            return toggleObj;
        }

        void CreateToggleVisuals(Toggle toggle, string label)
        {
            // Background (Circle or Rectangle)
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(toggle.transform, false);

            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0, 0.5f);
            backgroundRect.anchorMax = new Vector2(0, 0.5f);
            backgroundRect.anchoredPosition = new Vector2(10, -30);
            backgroundRect.sizeDelta = new Vector2(20, 20);

            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = CreateCircleSprite(); // Or use CreateRectangleSprite() for rectangular buttons
            backgroundImage.color = Color.white;

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(background.transform, false);

            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkmarkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkmarkRect.sizeDelta = Vector2.zero;

            Image checkmarkImage = checkmark.GetComponent<Image>();
            checkmarkImage.sprite = CreateCircleSprite();
            checkmarkImage.color = GUIManager.Instance.ValheimOrange;

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;

            // Label
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(toggle.transform, false);

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(1, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            //labelRect.anchorMax = new Vector2(1, 1);
            /*labelRect.offsetMin = new Vector2(40, 0);
            labelRect.offsetMax = new Vector2(0, 0);*/
            labelRect.anchoredPosition = new Vector2(80, -30);

            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = label;
            labelText.color = Color.white;
            labelText.font = GUIManager.Instance.AveriaSerif;
            //labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 18;
            labelText.alignment = TextAnchor.MiddleLeft;
        }

        Sprite CreateCircleSprite()
        {
            Texture2D texture = new Texture2D(128, 128);
            Color[] colors = new Color[128 * 128];
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64));
                    colors[y * 128 + x] = distance < 64 ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }

        Sprite CreateRectangleSprite()
        {
            Texture2D texture = new Texture2D(128, 128);
            Color[] colors = new Color[128 * 128];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white;
            }
            texture.SetPixels(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }

        Sprite CreateCheckmarkSprite()
        {
            Texture2D texture = new Texture2D(128, 128);
            Color[] colors = new Color[128 * 128];
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    if ((x > y - 30 && x < y + 10 && y > 64) || (x > 128 - y - 30 && x < 128 - y + 10 && y < 64))
                    {
                        colors[y * 128 + x] = Color.white;
                    }
                    else
                    {
                        colors[y * 128 + x] = Color.clear;
                    }
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }
    }
}
