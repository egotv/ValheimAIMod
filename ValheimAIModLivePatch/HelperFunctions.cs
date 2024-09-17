using BepInEx;
using Jotunn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimpleJson;
using UnityEngine;

namespace ValheimAIModLoader
{
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        private static int AllGOInstancesRefreshRate = 3;

        private static bool CanAccessAllGameInstances()
        {
            if (Time.time > AllGOInstancesLastRefresh + AllGOInstancesRefreshRate || AllGOInstancesLastRefresh == 0)
            {
                RefreshAllGameObjectInstances();
            }

            if (AllGOInstances.Length > 0)
            {
                return true;
            }

            return false;
        }

        private static GameObject[] GetAllGameObjectInstances()
        {
            if (CanAccessAllGameInstances())
            {
                return AllGOInstances;
            }

            return null;
        }

        private static void RefreshAllGameObjectInstances()
        {
            if (!PlayerNPC && !Player.m_localPlayer)
            {
                LogError("RefreshAllGameObjectInstances failed! Local player and PlayerNPC was null");
                return;
            }

            Vector3 p = PlayerNPC != null ? PlayerNPC.transform.position : Player.m_localPlayer.transform.position;

            AllGOInstances = GameObject.FindObjectsOfType<GameObject>(false)
                    .Where(go => go != null &&
                    go.transform.position.DistanceTo(p) < 300 &&
                    !blacklistedItems.Contains(go) &&
                    (HasAnyChildComponent(go, new List<Type> { typeof(Character), typeof(BaseAI) }) ||
                    go.HasAnyComponent("ItemDrop", "CharacterDrop", "DropOnDestroyed", "Pickable", "Destructible", "TreeBase", "TreeLog", "MineRock", "MineRock5")))
                    .ToArray();
            AllGOInstancesLastRefresh = Time.time;

            LogInfo($"Refresh nearby objects, len {AllGOInstances.Count()}, 300 units from {(PlayerNPC != null ? "thrall" : "player")}");
        }

        private GameObject[] FindEnemies()
        {
            if (Time.time - AllEnemiesInstancesLastRefresh < 1f)
            {
                return instance.AllEnemiesInstances;
            }

            List<Type> compsList = new List<Type>();
            compsList.Add(typeof(Character));

            instance.AllEnemiesInstances = GameObject.FindObjectsOfType<GameObject>(true)
                    .Where(go => go != null && HasAnyChildComponent(go, compsList) && !GetCharacterFromGameObject(go).m_tamed)
                    .ToArray();
            AllEnemiesInstancesLastRefresh = Time.time;
            return instance.AllEnemiesInstances;
        }

        private static Character FindClosestEnemy(GameObject character, string EnemyName = "")
        {
            GameObject res = null;

            if (EnemyName == "")
            {
                res = instance.FindEnemies()
                .Where(go => go != null)
                .ToArray().OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                .FirstOrDefault();
            }

            res = instance.FindEnemies()
                .Where(go => go != null && IsStringStartingWith(go.name, EnemyName, true))
                .ToArray().OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                .FirstOrDefault();

            return GetCharacterFromGameObject(res);
        }

        private static GameObject FindPlayerNPC()
        {
            HumanoidNPC[] humanoidNPCs = GameObject.FindObjectsOfType<HumanoidNPC>(true)
                    .Where(go => go != null && go.name.Contains(NPCPrefabName))
                    .ToArray();

            if (humanoidNPCs.Length > 0)
            {
                PlayerNPC = humanoidNPCs[0].gameObject;
                humanoid_PlayerNPC = PlayerNPC.GetComponent<HumanoidNPC>();
                monster_PlayerNPC = PlayerNPC.GetComponent<MonsterAI>();
            }
            if (humanoidNPCs.Length > 1)
            {
                for (int i = 0; i < humanoidNPCs.Length; i++)
                {
                    Destroy(humanoidNPCs[i]);
                }
            }
            return PlayerNPC;
        }

        private static GameObject FindClosestResource(GameObject character, string ResourceName, bool UnderwaterAllowed = true)
        {

            if (CanAccessAllGameInstances())
            {
                return AllGOInstances
                    .Where(go => go != null && IsStringEqual(go.name, ResourceName, true) && (UnderwaterAllowed || !IsUnderwater(go.transform.position) || go.HasAnyComponent("ItemDrop", "Pickable")))
                    .ToArray().OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                    .FirstOrDefault();
            }

            LogError($"FindClosestResource returning null for {ResourceName}");
            return null;
        }

        private static GameObject FindClosestResourceWithComponents(Vector3 position, float radius, string ResourceName, string[] componentNames, bool sortByDistance = true, bool UnderwaterAllowed = true)
        {
            if (CanAccessAllGameInstances())
            {
                IEnumerable<GameObject> res = AllGOInstances
                    .Where(go => go != null &&
                                 IsStringEqual(go.name, ResourceName, true) &&
                                 (UnderwaterAllowed || !IsUnderwater(go.transform.position) || go.HasAnyComponent(componentNames)));
                if (sortByDistance)
                    res.OrderBy(t => Vector3.Distance(position, t.transform.position));

                return res.FirstOrDefault();
            }

            LogError($"FindClosestResource returning null for {ResourceName}");
            return null;
        }

        private static bool IsUnderwater(Vector3 position)
        {
            GameObject go = ZNetScene.instance.GetPrefab("Fish1");
            if (go)
            {
                Fish fish = go.GetComponent<Fish>();

                if (fish)
                {
                    return position.y < fish.GetWaterLevel(position);
                }
            }

            return false;
        }

        private static float nearbyResourcesLastRefresh = 0f;
        private static Dictionary<string, int> nearbyResources = new Dictionary<string, int>();
        private static Dictionary<string, float> nearbyResourcesDistance = new Dictionary<string, float>();
        Dictionary<string, int> nearbyEnemies = new Dictionary<string, int>();
        Dictionary<string, float> nearbyEnemiesDistance = new Dictionary<string, float>();

        private static Dictionary<string, int> GetNearbyResources(GameObject source)
        {
            nearbyResources.Clear();

            void ProcessResource(GameObject resource, string key)
            {
                key = CleanKey(key);

                if (nearbyResources.ContainsKey(key))
                    nearbyResources[key]++;
                else
                    nearbyResources[key] = 1;

                float distance = resource.transform.position.DistanceTo(source.transform.position);
                if (nearbyResourcesDistance.ContainsKey(key))
                {
                    nearbyResourcesDistance[key] = Mathf.Min(nearbyResourcesDistance[key], distance);
                }
                else
                    nearbyResourcesDistance[key] = distance;
            }

            foreach (GameObject co in GetAllGameObjectInstances())
                if (co != null)
                    ProcessResource(co, co.name);

            return nearbyResources;
        }

        private string GetNearbyResourcesJSON(GameObject source)
        {
            GetNearbyResources(source);

            var jarray = new JsonArray();

            foreach (var kvp in nearbyResources)
            {
                JsonObject thisJobject = new JsonObject();
                thisJobject["name"] = kvp.Key;
                thisJobject["quantity"] = kvp.Value;
                thisJobject["nearestDistance"] = nearbyResourcesDistance[kvp.Key];

                jarray.Add(thisJobject);
            }

            int totalResources = nearbyResources.Values.Sum();


            string json = SimpleJson.SimpleJson.SerializeObject(jarray);
            //LogInfo(json);
            LogInfo($"Total nearby resources count: {totalResources}");
            return IndentJson(json);
        }

        private string GetNearbyEnemies(GameObject source)
        {
            Character[] characters = GameObject.FindObjectsOfType<Character>(true);
            Humanoid[] humanoids = GameObject.FindObjectsOfType<Humanoid>(true);

            /*Debug.Log("characters len " + characters.Length);
            Debug.Log("humanoids len " + humanoids.Length);*/



            void ProcessResource(Component resource, string key)
            {
                key = CleanKey(key);

                if (nearbyEnemies.ContainsKey(key))
                    nearbyEnemies[key]++;
                else
                    nearbyEnemies[key] = 1;

                float distance = resource.transform.position.DistanceTo(source.transform.position);
                if (nearbyEnemiesDistance.ContainsKey(key))
                    nearbyEnemiesDistance[key] = Mathf.Min(nearbyEnemiesDistance[key], distance);
                else
                    nearbyEnemiesDistance[key] = distance;
            }

            foreach (Character character in characters)
            {
                if (character.name.Contains("Player") || character.name.Contains("HumanoidNPC"))
                    continue;
                ProcessResource(character, character.name);
            }

            /*foreach (Humanoid humanoid in humanoids)
                ProcessResource(humanoid, humanoid.name);*/

            var jarray = new JsonArray();

            foreach (var kvp in nearbyEnemies)
            {
                JsonObject thisJobject = new JsonObject();
                thisJobject["name"] = kvp.Key;
                thisJobject["quantity"] = kvp.Value;
                thisJobject["nearestDistance"] = nearbyEnemiesDistance[kvp.Key];

                jarray.Add(thisJobject);
            }

            int totalEnemies = nearbyEnemies.Values.Sum();


            //string json = jarray.ToString();
            string json = SimpleJson.SimpleJson.SerializeObject(jarray);
            //Debug.Log(json);
            LogInfo($"Total nearby enemies: {totalEnemies}");
            return IndentJson(json);
        }

        

        public static void SaveNPCData(GameObject character)
        {
            HumanoidNPC humanoidNPC = character.GetComponent<HumanoidNPC>();
            MonsterAI monsterAI = character.GetComponent<MonsterAI>();

            JsonObject data = new JsonObject();

            data["name"] = humanoidNPC.m_name;
            data["personality"] = instance.npcPersonality;
            data["voice"] = instance.npcVoice;
            data["volume"] = (int)instance.npcVolume;
            data["gender"] = instance.npcGender;
            data["MicrophoneIndex"] = instance.MicrophoneIndex;
            //data["NPCCurrentMode"] = (int)NPCCurrentMode;


            // inventory
            var inventoryItems = new JsonArray();
            foreach (ItemDrop.ItemData item in humanoidNPC.m_inventory.m_inventory)
            {
                var itemData = new JsonObject
                {
                    //["name"] = item.m_shared.m_name,
                    ["name"] = item.m_dropPrefab.name,
                    ["stack"] = item.m_stack,
                    ["equipped"] = item.m_equipped ? 1 : 0,
                    ["durability"] = item.m_durability,
                    ["quality"] = item.m_quality
                };
                inventoryItems.Add(itemData);
            }
            data["inventory"] = inventoryItems;


            JsonArray skinColorArray = new JsonArray();
            skinColorArray.Add(humanoidNPC.m_visEquipment.m_skinColor.x);
            skinColorArray.Add(humanoidNPC.m_visEquipment.m_skinColor.y);
            skinColorArray.Add(humanoidNPC.m_visEquipment.m_skinColor.z);
            data["skinColor"] = skinColorArray;

            JsonArray hairColorArray = new JsonArray();
            hairColorArray.Add(humanoidNPC.m_visEquipment.m_hairColor.x);
            hairColorArray.Add(humanoidNPC.m_visEquipment.m_hairColor.y);
            hairColorArray.Add(humanoidNPC.m_visEquipment.m_hairColor.z);
            data["hairColor"] = hairColorArray;

            string json = SimpleJson.SimpleJson.SerializeObject(data);
            json = IndentJson(json);

            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "thrallmod.json");

            File.WriteAllText(filePath, json);
            //LogInfo("Saved NPC data to " + filePath);
        }

        public static void LoadNPCData(HumanoidNPC npc)
        {
            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "thrallmod.json");
            LogInfo("Loading NPC data from " + filePath);

            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                JsonObject data = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);

                if (data.ContainsKey("name"))
                    instance.npcName = data["name"].ToString();

                if (data.ContainsKey("personality"))
                    instance.npcPersonality = data["personality"].ToString();

                if (data.ContainsKey("voice"))
                    instance.npcVoice = int.Parse(data["voice"].ToString());

                if (data.ContainsKey("volume"))
                    instance.npcVolume = int.Parse(data["volume"].ToString());

                if (data.ContainsKey("gender"))
                    instance.npcGender = int.Parse(data["gender"].ToString());

                if (data.ContainsKey("MicrophoneIndex"))
                    instance.MicrophoneIndex = int.Parse(data["MicrophoneIndex"].ToString());

                /*if (data.ContainsKey("NPCCurrentMode"))
                    NPCCurrentMode = (NPCMode)((int.Parse(data["NPCCurrentMode"].ToString()) % Enum.GetValues(typeof(NPCMode)).Length));*/

                // Load skin color
                JsonArray skinColorArray = data["skinColor"] as JsonArray;
                if (skinColorArray.Count == 3)
                {
                    instance.skinColor = new Color(
                        float.Parse(skinColorArray[0].ToString()),
                        float.Parse(skinColorArray[1].ToString()),
                        float.Parse(skinColorArray[2].ToString())
                    );

                }

                // Load skin color
                JsonArray hairColorArray = data["hairColor"] as JsonArray;
                if (hairColorArray.Count == 3)
                {
                    instance.hairColor = new Color(
                        float.Parse(hairColorArray[0].ToString()),
                        float.Parse(hairColorArray[1].ToString()),
                        float.Parse(hairColorArray[2].ToString())
                    );
                }

                ApplyNPCData(npc);


                // Load inventory
                JsonArray inventoryArray = data["inventory"] as JsonArray;
                npc.m_inventory.RemoveAll();
                npc.GetInventory().RemoveAll();
                npc.m_inventory.m_inventory.Clear();
                LogMessage($"Loading {inventoryArray.Count} items to {npc.m_name}'s inventory");
                foreach (JsonObject itemData in inventoryArray)
                {
                    string itemName = itemData["name"].ToString();
                    int stack = int.Parse(itemData["stack"].ToString());
                    int equipped = 0;
                    if (itemData.ContainsKey("equipped"))
                        equipped = int.Parse(itemData["equipped"].ToString());
                    float durability = 0;
                    if (itemData.ContainsKey("durability"))
                        durability = float.Parse(itemData["durability"].ToString());
                    int quality = 0;
                    if (itemData.ContainsKey("quality"))
                        quality = int.Parse(itemData["quality"].ToString());


                    //LogInfo($"{itemName} x{stack} {(equipped == 1 ? "(equipping)" : "")}");


                    GameObject itemPrefab = ZNetScene.instance.GetPrefab(itemName);
                    if (itemPrefab != null)
                    {
                        ItemDrop.ItemData itemdata = npc.PickupPrefab(itemPrefab, stack);
                        if (equipped != 0)
                        {
                            npc.EquipItem(itemdata);
                        }
                        if (durability > 0)
                            itemdata.m_durability = durability;
                        if (quality > 0)
                            itemdata.m_quality = quality;
                    }
                    else if (itemPrefab == null)
                    {
                        LogError($"itemPrefab {itemName} was null");
                    }
                }

                npc.EquipBestWeapon(Player.m_localPlayer, null, Player.m_localPlayer, Player.m_localPlayer);

                LogMessage($"{npc.m_name} data loaded successfully!");
            }
            else
            {
                LogWarning("No saved NPC data found.");
                LogMessage("Applying default NPC personality");

                /*instance.npcName = "The Truth";
                instance.npcPersonality = "He always lies and brags about stuff he doesn't have or has never seen. His lies are extremely obvious. Always brings up random stuff out of nowhere";
                instance.personalityDropdownComp.SetValueWithoutNotify(npcPersonalities.Count - 1);*/

                instance.OnNPCPersonalityDropdownChanged(0);

                ApplyNPCData(npc);
            }
        }

        static int FindNPCPersonalityKeyIndexForValue(string value)
        {
            var keyValuePair = npcPersonalitiesMap.FirstOrDefault(kvp => kvp.Value == value);
            if (!keyValuePair.Equals(default(KeyValuePair<string, string>)))
            {
                return Array.IndexOf(npcPersonalities.ToArray(), keyValuePair.Key);
            }
            return -1; // Return -1 if the value is not found in the map
        }

        public static void ApplyNPCData(HumanoidNPC npc)
        {
            npc.m_name = instance.npcName;
            instance.nameInputField.SetTextWithoutNotify(instance.npcName);
            instance.personalityInputField.SetTextWithoutNotify(instance.npcPersonality);
            int personalityIndex = FindNPCPersonalityKeyIndexForValue(instance.npcPersonality);
            instance.personalityDropdownComp.SetValueWithoutNotify(personalityIndex == -1 ? npcPersonalities.Count - 1 : personalityIndex);
            instance.voiceDropdownComp.SetValueWithoutNotify(instance.npcVoice);
            instance.micDropdownComp.SetValueWithoutNotify(instance.MicrophoneIndex);
            instance.volumeSliderComp.SetValueWithoutNotify(instance.npcVolume);
            if (instance.npcGender == 0)
            {
                instance.toggleMasculine.isOn = true;
                instance.toggleFeminine.isOn = false;
            }
            else
            {
                instance.toggleMasculine.isOn = false;
                instance.toggleFeminine.isOn = true;
            }

            npc.m_visEquipment.SetHairColor(new Vector3(
                instance.hairColor.r,
                instance.hairColor.g,
                instance.hairColor.b
            ));

            npc.m_visEquipment.SetSkinColor(new Vector3(
                instance.skinColor.r,
                instance.skinColor.g,
                instance.skinColor.b
            ));


        }



        public static ItemDrop.ItemData GetBestHarvestingTool(List<ItemDrop.ItemData> tools, HitData.DamageModifiers resourceDamageModifiers)
        {
            float CalculateEffectiveDamage(ItemDrop.ItemData tool)
            {
                HitData.DamageTypes weaponDamages = tool.m_shared.m_damages;
                float totalDamage = 0f;

                if (tool.m_shared.m_skillType == Skills.SkillType.Bows)
                    return 0f;

                totalDamage += ApplyDamageModifier(weaponDamages.m_blunt, resourceDamageModifiers.m_blunt);
                totalDamage += ApplyDamageModifier(weaponDamages.m_slash, resourceDamageModifiers.m_slash);
                totalDamage += ApplyDamageModifier(weaponDamages.m_pierce, resourceDamageModifiers.m_pierce);
                totalDamage += ApplyDamageModifier(weaponDamages.m_chop, resourceDamageModifiers.m_chop);
                totalDamage += ApplyDamageModifier(weaponDamages.m_pickaxe, resourceDamageModifiers.m_pickaxe);
                totalDamage += ApplyDamageModifier(weaponDamages.m_fire, resourceDamageModifiers.m_fire);
                totalDamage += ApplyDamageModifier(weaponDamages.m_frost, resourceDamageModifiers.m_frost);
                totalDamage += ApplyDamageModifier(weaponDamages.m_lightning, resourceDamageModifiers.m_lightning);
                totalDamage += ApplyDamageModifier(weaponDamages.m_poison, resourceDamageModifiers.m_poison);
                totalDamage += ApplyDamageModifier(weaponDamages.m_spirit, resourceDamageModifiers.m_spirit);

                return totalDamage;
            }

            float ApplyDamageModifier(float baseDamage, HitData.DamageModifier modifier)
            {
                switch (modifier)
                {
                    case HitData.DamageModifier.Normal:
                        return baseDamage;
                    case HitData.DamageModifier.Resistant:
                        return baseDamage * 0.5f;
                    case HitData.DamageModifier.Weak:
                        return baseDamage * 1.5f;
                    case HitData.DamageModifier.Immune:
                    case HitData.DamageModifier.Ignore:
                        return 0f;
                    case HitData.DamageModifier.VeryResistant:
                        return baseDamage * 0.25f;
                    case HitData.DamageModifier.VeryWeak:
                        return baseDamage * 2f;
                    default:
                        return baseDamage;
                }
            }

            return tools.OrderByDescending(CalculateEffectiveDamage).FirstOrDefault();
        }

        private float GetDamageModifierValue(HitData.DamageModifier modifier)
        {
            switch (modifier)
            {
                case HitData.DamageModifier.Normal: return 1.0f;
                case HitData.DamageModifier.Resistant: return 0.5f;
                case HitData.DamageModifier.Weak: return 1.5f;
                case HitData.DamageModifier.Immune: return 0.0f;
                case HitData.DamageModifier.Ignore: return 1.0f;
                case HitData.DamageModifier.VeryResistant: return 0.25f;
                case HitData.DamageModifier.VeryWeak: return 2.0f;
                default: return 1.0f;
            }
        }

        private float CalculateWeaponEffectiveness(ItemDrop.ItemData weapon, HitData.DamageModifiers resourceModifiers)
        {
            float effectiveness = 0f;
            HitData.DamageTypes damages = weapon.m_shared.m_damages;

            if (damages.m_blunt > 0)
                effectiveness += damages.m_blunt * GetDamageModifierValue(resourceModifiers.m_blunt);

            if (damages.m_slash > 0)
                effectiveness += damages.m_slash * GetDamageModifierValue(resourceModifiers.m_slash);

            if (damages.m_pierce > 0)
                effectiveness += damages.m_pierce * GetDamageModifierValue(resourceModifiers.m_pierce);

            if (damages.m_chop > 0)
                effectiveness += damages.m_chop * GetDamageModifierValue(resourceModifiers.m_chop);

            if (damages.m_pickaxe > 0)
                effectiveness += damages.m_pickaxe * GetDamageModifierValue(resourceModifiers.m_pickaxe);

            if (damages.m_fire > 0)
                effectiveness += damages.m_fire * GetDamageModifierValue(resourceModifiers.m_fire);

            if (damages.m_frost > 0)
                effectiveness += damages.m_frost * GetDamageModifierValue(resourceModifiers.m_frost);

            if (damages.m_lightning > 0)
                effectiveness += damages.m_lightning * GetDamageModifierValue(resourceModifiers.m_lightning);

            if (damages.m_poison > 0)
                effectiveness += damages.m_poison * GetDamageModifierValue(resourceModifiers.m_poison);

            if (damages.m_spirit > 0)
                effectiveness += damages.m_spirit * GetDamageModifierValue(resourceModifiers.m_spirit);

            return effectiveness;
        }

        private float CalculateEfficiency(string name, string category, int minAmount, int maxAmount, float health, float weaponEffectiveness, float distance)
        {
            float avgAmount = (minAmount + maxAmount) / 2f;

            if (weaponEffectiveness <= 5)
            {
                if (category == "ItemDrop" || category == "Pickable")
                    return avgAmount / (1 + distance / 50);
                else
                    return 0;
            }

            float baseEfficiency = avgAmount / (health / weaponEffectiveness);

            if (category == "ItemDrop" || category == "Pickable")
            {
                baseEfficiency = 1;
            }
            else if (category == "DropOnDestroyed")
            {
                baseEfficiency *= avgAmount;
            }
            else if (category == "TreeLog" || category == "TreeBase" || category == "MineRock" || category == "MineRock5" || category == "Destructible")
            {
                if (weaponEffectiveness <= 5)
                    return 0;
                baseEfficiency *= avgAmount;

                if (name.ToLower().Contains("log"))
                    baseEfficiency *= 1.1f;
                if (name.ToLower().Contains("half"))
                    baseEfficiency *= 1.1f;
            }
            else if (category == "CharacterDrop")
            {
                baseEfficiency *= 0.5f;
            }

            // Adjust efficiency based on distance
            float distanceFactor = 1 / (1 + distance / 10);

            return baseEfficiency * distanceFactor;
        }

        void EquipBestClothes(Humanoid humanoid)
        {
            Dictionary<ItemDrop.ItemData.ItemType, ItemDrop.ItemData> bestClothes = new Dictionary<ItemDrop.ItemData.ItemType, ItemDrop.ItemData>();

            foreach (ItemDrop.ItemData item in humanoid.m_inventory.GetAllItems())
            {
                if (IsClothing(item.m_shared.m_itemType))
                {
                    if (!bestClothes.ContainsKey(item.m_shared.m_itemType) ||
                        item.GetArmor() > bestClothes[item.m_shared.m_itemType].GetArmor())
                    {
                        bestClothes[item.m_shared.m_itemType] = item;
                    }
                }
            }

            foreach (var kvp in bestClothes)
            {
                humanoid.EquipItem(kvp.Value);
            }
        }

        bool IsClothing(ItemDrop.ItemData.ItemType itemType)
        {
            return itemType == ItemDrop.ItemData.ItemType.Chest ||
                   itemType == ItemDrop.ItemData.ItemType.Legs ||
                   itemType == ItemDrop.ItemData.ItemType.Helmet ||
                   itemType == ItemDrop.ItemData.ItemType.Shoulder;
        }

        

        static int CountItemsInInventory(Inventory inventory, string itemName)
        {
            if (inventory == null)
            {
                return 0;
            }

            return inventory.GetAllItems()
                .Where(item => item.m_dropPrefab.name.ToLower() == itemName.ToLower())
                .Sum(item => item.m_stack);
        }

        private static void PrintInventoryItems(Inventory inventory)
        {
            LogMessage("Character Inventory Items:");

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            foreach (ItemDrop.ItemData item in items)
            {
                LogMessage($"- {item.m_shared.m_name} (Quantity: {item.m_stack} | {item.m_dropPrefab.name})");
            }
        }

        private static bool IsRangedWeapon(ItemDrop.ItemData item)
        {
            if (item == null) return false;

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch ||
                   item.m_shared.m_attack.m_attackType == Attack.AttackType.Projectile;
        }

        private static bool IsMeleeWeapon(ItemDrop.ItemData item)
        {
            if (item == null) return false;

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft;
        }

        private static int CheckArrows(Inventory inventory)
        {
            var arrows = inventory.GetAllItems()
                .Where(item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo)
                .ToList();

            if (arrows.Any())
            {
                //Debug.Log($"Player has {arrows.Count} types of arrows:");
                foreach (var arrow in arrows)
                {
                    //Debug.Log($"- {arrow.m_shared.m_name}: {arrow.m_stack}");
                }
                return arrows.Count;
            }
            else
            {
                //Debug.Log("Player has no arrows!");
                return 0;
            }
        }

        private void DropAllItems(HumanoidNPC humanoidNPC)
        {
            List<ItemDrop.ItemData> allItems = humanoidNPC.m_inventory.GetAllItems();
            int num = 1;
            LogInfo($"{humanoidNPC.m_name} dropping {humanoidNPC.m_inventory.GetAllItems().Count} items: ");
            foreach (ItemDrop.ItemData item in allItems)
            {
                LogInfo(item.m_shared.m_name);
                //Vector3 position = humanoidNPC.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                Vector3 position = humanoidNPC.transform.position + Vector3.up * 2f + UnityEngine.Random.insideUnitSphere * 0.3f + humanoidNPC.transform.forward * 2.5f;
                Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                ItemDrop.DropItem(item, item.m_stack, position, rotation);
                num++;
            }
            humanoidNPC.m_inventory.RemoveAll();
        }

        private void DropItem(string ItemName, HumanoidNPC humanoidNPC)
        {
            List<ItemDrop.ItemData> allItems = humanoidNPC.m_inventory.GetAllItems();
            int num = 1;
            foreach (ItemDrop.ItemData item in allItems)
            {
                if ((item.m_dropPrefab != null && ItemName == item.m_dropPrefab.name) || ItemName == item.m_shared.m_name)
                {
                    LogInfo($"{humanoidNPC.m_name} dropping item: " + item.m_shared.m_name);
                    //Vector3 position = humanoidNPC.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                    Vector3 position = humanoidNPC.transform.position + Vector3.up * 2f + UnityEngine.Random.insideUnitSphere * 0.3f + humanoidNPC.transform.forward * 5.5f;
                    Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                    ItemDrop.DropItem(item, item.m_stack, position, rotation);
                    num++;
                    //humanoidNPC.m_inventory.RemoveOneItem(item);
                    humanoidNPC.m_inventory.RemoveItem(item, item.m_stack);
                    return;
                }
            }
            LogInfo($"{humanoidNPC.m_name} couldn't drop item: {ItemName}");
        }

        private void EquipItem(string ItemName, HumanoidNPC humanoidNPC)
        {
            List<ItemDrop.ItemData> allItems = humanoidNPC.m_inventory.GetAllItems();
            foreach (ItemDrop.ItemData item in allItems)
            {
                if (ItemName == item.m_shared.m_name)
                {
                    LogInfo($"{humanoidNPC.m_name} equipping  " + item.m_shared.m_name);
                    humanoidNPC.EquipItem(item);
                    return;
                }
            }

        }

        public void AddChatTalk(Character character, string name, string text, bool addToChat = true)
        {
            UserInfo userInfo = new UserInfo();
            if (character is Player)
            {
                Player player = (Player)character;
                userInfo.Name = player.GetPlayerName();
            }
            else
                userInfo.Name = character.m_name;
            Vector3 headPoint = character.GetEyePoint() + (Vector3.up * -100f);
            long senderID = character is Player ? 99991 : 99992;
            Chat.WorldTextInstance oldtext = Chat.instance.FindExistingWorldText(senderID);
            if (oldtext != null && oldtext.m_gui)
            {
                UnityEngine.Object.Destroy(oldtext.m_gui);
                Chat.instance.m_worldTexts.Remove(oldtext);
            }
            Chat.instance.AddInworldText(character.gameObject, senderID, headPoint, Talker.Type.Shout, userInfo, text + "\n\n\n\n\n");
            if (text != "..." && addToChat)
            {
                Chat.instance.AddString(character is Player ? Player.m_localPlayer.GetPlayerName() : character.m_name, text, Talker.Type.Normal);
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.m_chatWindow.gameObject.SetActive(value: true);
            }
        }



        public static Vector3 GetRandomReachableLocationInRadius(Vector3 center, float radius)
        {
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
            Vector3 randomLocation = center + randomDirection;

            int maxAttempts = 10;
            int attempts = 0;

            while (!IsLocationReachable(randomLocation) && attempts < maxAttempts)
            {
                randomDirection = UnityEngine.Random.insideUnitSphere * radius;
                randomLocation = center + randomDirection;
                attempts++;
            }

            return randomLocation;
        }

        private static bool IsLocationReachable(Vector3 location)
        {
            // Perform a sphere cast to check if the location is reachable
            RaycastHit hit;
            if (Physics.SphereCast(location + Vector3.up * 500f, 0.5f, Vector3.down, out hit, 1000f, LayerMask.GetMask("Default", "static_solid", "Default_small", "Terrain")))
            {
                // Check if the hit point is close enough to the desired location
                float distance = Vector3.Distance(hit.point, location);
                if (distance <= 1f)
                {
                    return true;
                }
            }
            return false;
        }

        private Vector3 GetRandomSpawnPosition(float radius)
        {
            //Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
            Vector3 randomDirection = Player.m_localPlayer.transform.position + (Vector3.up * 20f) + (Vector3.forward * 20f);

            RaycastHit hit;
            if (Physics.Raycast(randomDirection, Vector3.down, out hit, 1000f, LayerMask.GetMask("Default", "static_solid", "Default_small", "Terrain")))
            {
                return hit.point;
            }

            //return Player.m_localPlayer.transform.position;
            return randomDirection;
        }

        public static string GetPlayerSteamID()
        {
            List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();

            for (int j = 0; j < playerList.Count; j++)
            {
                ZNet.PlayerInfo playerInfo = playerList[j];
                //Debug.LogError($"{playerInfo.m_name} {playerInfo.m_host}");

                return playerInfo.m_host;
            }

            return "NullID";
        }

        public static void ToggleNPCCurrentCommandType()
        {
            NPCCurrentMode = (NPCMode)(((int)NPCCurrentMode + 1) % Enum.GetValues(typeof(NPCMode)).Length);

            if (NPCCurrentMode == NPCMode.Passive)
            {
                instance.commandManager.RemoveCommandsOfType<AttackAction>();
                enemyList.Clear();
            }
        }



        private static string GetBrainAPIAddress()
        {
            return Decrypt(encryptedBrainBaseURL);
        }

        public static bool IsInAWorld()
        {
            return ZNetScene.instance != null && Player.m_localPlayer != null;
        }

        public static bool IsLocalSingleplayer()
        {
            // Check if ZNet instance exists
            if (ZNet.instance == null)
            {
                Debug.LogWarning("ZNet instance is null. Unable to determine world type.");
                return false;
            }

            // Check if it's a dedicated server
            if (ZNet.instance.IsDedicated())
            {
                return false; // Dedicated server is always multiplayer
            }

            // Check if it's a local server (which could be singleplayer or non-dedicated multiplayer)
            if (ZNet.instance.IsServer())
            {
                // If it's a server and there's only one peer (the host), it's singleplayer
                return ZNet.instance.GetPeers().Count <= 1;
            }

            // If we're not the server, it's multiplayer
            return false;
        }

        private static void SetMonsterAIAggravated(MonsterAI monsterAIcomponent, bool Aggravated)
        {
            if (Aggravated)
            {
                monsterAIcomponent.m_aggravatable = true;
            }
            else
            {
                monsterAIcomponent.m_aggravated = false;
                monsterAIcomponent.m_aggravatable = false;
                monsterAIcomponent.m_alerted = false;

                monsterAIcomponent.m_eventCreature = false;
                monsterAIcomponent.m_targetCreature = null;
                monsterAIcomponent.m_targetStatic = null;
                //monsterAIcomponent.m_viewRange = 0f;
                monsterAIcomponent.SetHuntPlayer(false);
            }
        }

        // TO READ NAMES OF ATTACK ANIMS
        /*[HarmonyPostfix]
        [HarmonyPatch(typeof(Attack), "Start")]
        private static void Attack_Start_Postfix(Attack __instance)
        {
            // TO FIND OUT ANIMATIONS NAMES
            Debug.Log("Attack anim " + __instance.m_attackAnimation);
        }*/
    }
}
