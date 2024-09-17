using BepInEx;
using Jotunn.Managers;
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
        public class Resource
        {
            public string Name { get; set; }
            public int MinAmount { get; set; }
            public int MaxAmount { get; set; }
            public float Health { get; set; }
            public HitData.DamageModifiers DamageModifiers { get; set; }

            public Resource(string name, int minAmount, int maxAmount, float health, HitData.DamageModifiers damageModifiers = new HitData.DamageModifiers())
            {
                Name = name;
                MinAmount = minAmount;
                MaxAmount = maxAmount;
                Health = health;
                DamageModifiers = damageModifiers;
            }

            // to be removed / not being used
            public float CalculateEaseScore(float distance, bool HasWeapon)
            {
                // Constants for weighting different factors
                float AMOUNT_WEIGHT = HasWeapon ? 0.3f : 0.0f;
                float HEALTH_WEIGHT = HasWeapon ? 0.3f : 0.9f;
                float DISTANCE_WEIGHT = HasWeapon ? 0.4f : 0.1f;

                // Calculate sub-scores
                float amountScore = ((MinAmount + MaxAmount) / 2.0f) * 10; // Assuming max possible amount is 10
                float healthScore = 100 / Health; // Inverse relationship: lower health is better
                float distanceScore = 100 / (1 + distance); // Inverse relationship: closer is better

                // Combine sub-scores with weights
                float totalScore = (amountScore * AMOUNT_WEIGHT) +
                                   (healthScore * HEALTH_WEIGHT) +
                                   (distanceScore * DISTANCE_WEIGHT);

                return totalScore;
            }
        }

        private static Dictionary<string, Dictionary<string, List<Resource>>> resourceDatabase = new Dictionary<string, Dictionary<string, List<Resource>>>();
        private static Dictionary<string, float> resourceHealthMap = new Dictionary<string, float>();
        private static Dictionary<string, float> resourceQuantityMap = new Dictionary<string, float>();

        private Dictionary<string, List<Resource>> logToTreeMap = new Dictionary<string, List<Resource>>();
        private Dictionary<string, List<Resource>> logToLogMap = new Dictionary<string, List<Resource>>();
        private Dictionary<string, List<Resource>> destructibleToSpawnMap = new Dictionary<string, List<Resource>>();

        private static List<string> priorityOrderUnarmed = new List<string>
        {
            "ItemDrop",
            "Pickable",

            "TreeLog",
            "TreeBase",
            "MineRock",
            "MineRock5",

            "CharacterDrop",
            "DropOnDestroyed",
            "Destructible"
        };

        private static List<string> priorityOrder = new List<string>
        {
            "TreeLog",
            "TreeBase",
            "MineRock",
            "MineRock5",

            "DropOnDestroyed",
            "Destructible",

            "ItemDrop",
            "Pickable",

            "CharacterDrop",
        };

        
        private void PopulateDatabase()
        {
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab.HasAnyComponent("TreeBase"))
                    CheckTreeBase(prefab);
                if (prefab.HasAnyComponent("TreeLog"))
                    CheckTreeLog(prefab);

                if (prefab.HasAnyComponent("Pickable"))
                    CheckPickables(prefab);
                if (prefab.HasAnyComponent("ItemDrop"))
                    CheckItemDrop(prefab);
                if (prefab.HasAnyComponent("MineRock"))
                    CheckMineRock(prefab);
                if (prefab.HasAnyComponent("MineRock5"))
                    CheckMineRock5(prefab);
                if (prefab.HasAnyComponent("Destructible"))
                    CheckDestructibles(prefab);
                else if (prefab.HasAnyComponent("DropOnDestroyed"))
                    CheckDropOnDestroyed(prefab);

                if (prefab.HasAnyComponent("CharacterDrop"))
                    CheckCharacterDrop(prefab);

            }

            AddResourceRelationships();

            //SortDatabase();


            //SaveDatabaseToJson();
        }

        private void AddResourceRelationships()
        {
            foreach (var kvp in resourceDatabase)
            {
                Dictionary<string, List<Resource>> resources = kvp.Value;

                // Add logs that drop sub logs that drop this resource
                List<Resource> newLogs = new List<Resource>();
                newLogs.AddRange(resources["TreeLog"]);
                foreach (Resource r in resources["TreeLog"])
                {
                    if (logToLogMap.ContainsKey(r.Name))
                    {
                        newLogs.AddRange(logToLogMap[r.Name]);
                        //Debug.Log($"adding {logToLogMap[r].Count} tree logs to {kvp.Key}");
                    }
                }
                resourceDatabase[kvp.Key]["TreeLog"] = newLogs.ToList();

                newLogs.Clear();
                newLogs.AddRange(resources["TreeBase"]);
                // add trees that drop logs that drop this resource
                foreach (Resource r in resources["TreeLog"])
                {
                    if (logToTreeMap.ContainsKey(r.Name))
                    {
                        newLogs.AddRange(logToTreeMap[r.Name]);
                    }
                }
                resourceDatabase[kvp.Key]["TreeBase"] = newLogs.ToList();

                newLogs.Clear();
                newLogs.AddRange(resources["MineRock5"]);
                foreach (Resource r in resources["MineRock5"])
                {
                    if (destructibleToSpawnMap.ContainsKey(r.Name))
                    {
                        newLogs.AddRange(destructibleToSpawnMap[r.Name]);
                        //LogError($"newlogs destructibleToSpawnMap {destructibleToSpawnMap[r.Name]}");
                    }
                }

                resourceDatabase[kvp.Key]["MineRock5"] = newLogs.ToList();
            }
        }

        private void CheckTreeBase(GameObject prefab)
        {
            TreeBase treeBase = prefab.GetComponent<TreeBase>();
            if (treeBase != null && treeBase.m_dropWhenDestroyed != null && treeBase.m_dropWhenDestroyed.m_drops != null)
            {
                foreach (DropTable.DropData drop in treeBase.m_dropWhenDestroyed.m_drops)
                {
                    float health = -1;
                    if (resourceHealthMap.TryGetValue(prefab.name, out health))
                    {
                        resourceHealthMap[prefab.name] = Math.Max(treeBase.m_health, health);
                    }
                    else
                    {
                        resourceHealthMap[prefab.name] = treeBase.m_health;
                    }

                    resourceQuantityMap[prefab.name] = treeBase.m_dropWhenDestroyed.m_dropMax;

                    Resource sourceResource = new Resource(prefab.name, treeBase.m_dropWhenDestroyed.m_dropMin, treeBase.m_dropWhenDestroyed.m_dropMax, resourceHealthMap[prefab.name], treeBase.m_damageModifiers);
                    AddToDatabase(drop.m_item.name, "TreeBase", sourceResource);
                }

                // Store the relationship between the tree and its log prefab
                if (treeBase.m_logPrefab != null)
                {
                    /*if (!logToTreeMap.ContainsKey(treeBase.m_logPrefab.name))
                        logToTreeMap[treeBase.m_logPrefab.name] = new List<string>();
                    logToTreeMap[treeBase.m_logPrefab.name].Add(prefab.name);*/

                    TreeLog treelog = treeBase.m_logPrefab.GetComponent<TreeLog>();
                    TreeLog subLog = null;

                    if (treelog)
                    {
                        if (treelog.m_subLogPrefab)
                        {
                            subLog = treelog.m_subLogPrefab.GetComponent<TreeLog>();
                        }
                        else
                        {
                            //LogError($"sublogprefab is null for {treelog.name}");
                        }
                    }

                    int min = subLog ? (int)(subLog.m_dropWhenDestroyed.m_dropMin * .6f) : 1;
                    int max = subLog ? (int)(subLog.m_dropWhenDestroyed.m_dropMax * .6f) : 1;

                    Resource sourceResource = new Resource(prefab.name, min, max, treeBase.m_health, treeBase.m_damageModifiers);
                    AddToDatabase(treeBase.m_logPrefab.name, "TreeBase", sourceResource);

                    if (!logToTreeMap.ContainsKey(treeBase.m_logPrefab.name))
                        logToTreeMap[treeBase.m_logPrefab.name] = new List<Resource>();
                    logToTreeMap[treeBase.m_logPrefab.name].Add(sourceResource);
                }
            }
        }

        private void CheckTreeLog(GameObject prefab)
        {
            TreeLog treeBase = prefab.GetComponent<TreeLog>();
            if (treeBase != null && treeBase.m_dropWhenDestroyed != null && treeBase.m_dropWhenDestroyed.m_drops != null)
            {
                foreach (DropTable.DropData drop in treeBase.m_dropWhenDestroyed.m_drops)
                {
                    float health = -1;
                    if (resourceHealthMap.TryGetValue(prefab.name, out health))
                    {
                        resourceHealthMap[prefab.name] = Math.Max(treeBase.m_health, health);
                    }
                    else
                    {
                        resourceHealthMap[prefab.name] = treeBase.m_health;
                    }

                    resourceQuantityMap[prefab.name] = treeBase.m_dropWhenDestroyed.m_dropMax;
                    Resource sourceResource = new Resource(prefab.name, treeBase.m_dropWhenDestroyed.m_dropMin, treeBase.m_dropWhenDestroyed.m_dropMax, resourceHealthMap[prefab.name], treeBase.m_damages);

                    AddToDatabase(drop.m_item.name, "TreeLog", sourceResource);
                }

                if (treeBase.m_subLogPrefab != null)
                {
                    TreeLog subLog = treeBase.m_subLogPrefab.GetComponent<TreeLog>();
                    int min = subLog ? (int)(subLog.m_dropWhenDestroyed.m_dropMin * .8f) : 1;
                    int max = subLog ? (int)(subLog.m_dropWhenDestroyed.m_dropMax * .8f) : 1;

                    Resource sourceResource = new Resource(prefab.name, min, max, treeBase.m_health, treeBase.m_damages);
                    AddToDatabase(treeBase.m_subLogPrefab.name, "TreeLog", sourceResource);

                    if (!logToLogMap.ContainsKey(treeBase.m_subLogPrefab.name))
                        logToLogMap[treeBase.m_subLogPrefab.name] = new List<Resource>();
                    logToLogMap[treeBase.m_subLogPrefab.name].Add(sourceResource);
                }
            }
        }

        private void CheckCharacterDrop(GameObject prefab)
        {
            CharacterDrop characterDrop = prefab.GetComponent<CharacterDrop>();
            HitData.DamageModifiers damageModifiers = new HitData.DamageModifiers();

            if (characterDrop != null && characterDrop.m_drops != null)
            {
                float health = -1;

                if (prefab.HasAnyComponent("Humanoid"))
                {
                    Humanoid humanoid = prefab.GetComponent<Humanoid>();
                    damageModifiers = humanoid.m_damageModifiers;
                    if (resourceHealthMap.TryGetValue(prefab.name, out health))
                    {
                        resourceHealthMap[prefab.name] = Math.Max(humanoid.m_health, health);
                    }
                    else
                    {
                        resourceHealthMap[prefab.name] = humanoid.m_health;
                    }
                }
                else if (prefab.HasAnyComponent("Character"))
                {
                    Character humanoid = prefab.GetComponent<Character>();
                    damageModifiers = humanoid.m_damageModifiers;
                    if (resourceHealthMap.TryGetValue(prefab.name, out health))
                    {
                        resourceHealthMap[prefab.name] = Math.Max(humanoid.m_health, health);
                    }
                    else
                    {
                        resourceHealthMap[prefab.name] = humanoid.m_health;
                    }
                }
                foreach (CharacterDrop.Drop drop in characterDrop.m_drops)
                {
                    resourceQuantityMap[prefab.name] = characterDrop.m_drops.Count;
                    Resource sourceResource = new Resource(prefab.name, drop.m_amountMin, drop.m_amountMax, resourceHealthMap[prefab.name], damageModifiers);
                    AddToDatabase(drop.m_prefab.name, "CharacterDrop", sourceResource);
                }
            }
        }

        private void CheckDropOnDestroyed(GameObject prefab)
        {
            DropOnDestroyed dropOnDestroyed = prefab.GetComponent<DropOnDestroyed>();
            HitData.DamageModifiers damageModifiers = new HitData.DamageModifiers();

            if (dropOnDestroyed != null && dropOnDestroyed.m_dropWhenDestroyed != null && dropOnDestroyed.m_dropWhenDestroyed.m_drops != null)
            {
                float health = -1;
                if (resourceHealthMap.TryGetValue(prefab.name, out health))
                {
                    resourceHealthMap[prefab.name] = health;
                }
                else
                {
                    resourceHealthMap[prefab.name] = health;
                    if (resourceHealthMap[prefab.name] <= 0 && prefab.HasAnyComponent("WearNTear"))
                    {
                        WearNTear wnt = prefab.GetComponent<WearNTear>();
                        if (wnt != null)
                        {
                            resourceHealthMap[prefab.name] = wnt.m_health;
                            damageModifiers = wnt.m_damages;
                        }
                    }
                }



                foreach (DropTable.DropData drop in dropOnDestroyed.m_dropWhenDestroyed.m_drops)
                {
                    resourceQuantityMap[prefab.name] = dropOnDestroyed.m_dropWhenDestroyed.m_dropMax;
                    Resource sourceResource = new Resource(prefab.name, dropOnDestroyed.m_dropWhenDestroyed.m_dropMin, dropOnDestroyed.m_dropWhenDestroyed.m_dropMax, resourceHealthMap[prefab.name], damageModifiers);
                    if (drop.m_item)
                        AddToDatabase(drop.m_item.name, "DropOnDestroyed", sourceResource);
                }
            }
        }

        private void CheckItemDrop(GameObject prefab)
        {
            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            //if (itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_dropPrefab != null)
            //if (itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_dropPrefab != null)
            if (itemDrop != null && itemDrop.m_itemData != null)
            {
                resourceQuantityMap[prefab.name] = itemDrop.m_itemData.m_stack;
                //AddToDatabase(itemDrop.m_itemData.m_shared.m_name, "ItemDrop", prefab.name);
                Resource sourceResource = new Resource(prefab.name, 1, itemDrop.m_itemData.m_stack, 2.5f);
                AddToDatabase(prefab.name, "ItemDrop", sourceResource);
            }
        }

        private void CheckDestructibles(GameObject prefab)
        {
            Destructible destructible = prefab.GetComponent<Destructible>();

            if (destructible != null)
            {
                float health = -1;
                if (resourceHealthMap.TryGetValue(prefab.name, out health))
                {
                    resourceHealthMap[prefab.name] = Math.Max(destructible.m_health, health);
                }
                else
                {
                    resourceHealthMap[prefab.name] = destructible.m_health;
                }

                resourceQuantityMap[prefab.name] = 1;

                int min = 1;
                int max = 1;

                DropOnDestroyed dropOnDestroyed = prefab.GetComponent<DropOnDestroyed>();
                if (dropOnDestroyed != null && dropOnDestroyed.m_dropWhenDestroyed != null)
                {
                    foreach (DropTable.DropData drop in dropOnDestroyed.m_dropWhenDestroyed.m_drops)
                    {
                        resourceQuantityMap[prefab.name] = dropOnDestroyed.m_dropWhenDestroyed.m_dropMax;

                        min = dropOnDestroyed.m_dropWhenDestroyed.m_dropMin;
                        max = dropOnDestroyed.m_dropWhenDestroyed.m_dropMax;

                        Resource sourceResource = new Resource(prefab.name, min, max, resourceHealthMap[prefab.name], destructible.m_damages);
                        if (drop.m_item)
                            AddToDatabase(drop.m_item.name, "Destructible", sourceResource);

                        /*if (destructible.m_spawnWhenDestroyed)
                            AddToDatabase(destructible.m_spawnWhenDestroyed.name, "Destructible", sourceResource);*/
                    }
                }

                if (destructible.m_spawnWhenDestroyed != null)
                {
                    Resource sourceResource = new Resource(prefab.name, min, max, destructible.m_health, destructible.m_damages);
                    //AddToDatabase(treeBase.m_logPrefab.name, "TreeBase", sourceResource);

                    if (!destructibleToSpawnMap.ContainsKey(destructible.m_spawnWhenDestroyed.name))
                        destructibleToSpawnMap[destructible.m_spawnWhenDestroyed.name] = new List<Resource>();
                    destructibleToSpawnMap[destructible.m_spawnWhenDestroyed.name].Add(sourceResource);
                }
            }
        }

        private void CheckPickables(GameObject prefab)
        {
            Pickable pickable = prefab.GetComponent<Pickable>();
            if (pickable != null && pickable.m_itemPrefab != null)
            {
                resourceQuantityMap[prefab.name] = pickable.m_amount;
                Resource sourceResource = new Resource(prefab.name, pickable.m_amount, pickable.m_amount, 2.5f);
                AddToDatabase(pickable.m_itemPrefab.name, "Pickable", sourceResource);
            }
        }

        private void CheckMineRock(GameObject prefab)
        {
            MineRock minerock = prefab.GetComponent<MineRock>();
            if (minerock != null && minerock.m_dropItems != null && minerock.m_dropItems.m_drops != null)
            {
                float health = -1;
                if (resourceHealthMap.TryGetValue(prefab.name, out health))
                {
                    resourceHealthMap[prefab.name] = Math.Max(minerock.m_health, health);
                }
                else
                {
                    resourceHealthMap[prefab.name] = minerock.m_health;
                }

                resourceQuantityMap[prefab.name] = minerock.m_dropItems.m_dropMax;

                foreach (DropTable.DropData drop in minerock.m_dropItems.m_drops)
                {
                    //if (drop.m_item != null)
                    Resource sourceResource = new Resource(prefab.name, drop.m_stackMin, drop.m_stackMax, resourceHealthMap[prefab.name], minerock.m_damageModifiers);
                    AddToDatabase(drop.m_item.name, "MineRock", sourceResource);
                }
            }
        }

        private void CheckMineRock5(GameObject prefab)
        {
            MineRock5 minerock = prefab.GetComponent<MineRock5>();
            if (minerock != null && minerock.m_dropItems != null && minerock.m_dropItems.m_drops != null)
            {
                float health = -1;
                if (resourceHealthMap.TryGetValue(prefab.name, out health))
                {
                    resourceHealthMap[prefab.name] = Math.Max(minerock.m_health, health);
                }
                else
                {
                    resourceHealthMap[prefab.name] = minerock.m_health;
                }

                resourceQuantityMap[prefab.name] = minerock.m_dropItems.m_dropMax;


                foreach (DropTable.DropData drop in minerock.m_dropItems.m_drops)
                {
                    //if (drop.m_item != null)
                    Resource sourceResource = new Resource(prefab.name, drop.m_stackMin, drop.m_stackMax, resourceHealthMap[prefab.name], minerock.m_damageModifiers);
                    AddToDatabase(drop.m_item.name, "MineRock5", sourceResource);
                }
            }
        }

        

        private void AddToDatabase(string resourceName, string sourceType, Resource sourceResource)
        {
            if (!resourceDatabase.ContainsKey(resourceName))
            {
                resourceDatabase[resourceName] = new Dictionary<string, List<Resource>>
                    {
                        { "TreeLog", new List<Resource>() },
                        { "TreeBase", new List<Resource>() },
                        { "MineRock", new List<Resource>() },
                        { "MineRock5", new List<Resource>() },

                        { "DropOnDestroyed", new List<Resource>() },
                        { "Destructible", new List<Resource>() },

                        { "ItemDrop", new List<Resource>() },
                        { "Pickable", new List<Resource>() },

                        { "CharacterDrop", new List<Resource>() },
                    };
            }

            resourceDatabase[resourceName][sourceType].Add(sourceResource);
        }

        private void SaveDatabaseToJson()
        {
            JsonObject jsonObject = new JsonObject();

            foreach (var resourcePair in resourceDatabase)
            {
                JsonObject resourceObject = new JsonObject();

                foreach (var sourcePair in resourcePair.Value)
                {
                    JsonArray sourceArray = new JsonArray();

                    foreach (var sourceName in sourcePair.Value)
                    {
                        JsonObject source = new JsonObject();
                        source["Name"] = sourceName.Name;
                        source["MinAmount"] = sourceName.MinAmount;
                        source["MaxAmount"] = sourceName.MaxAmount;
                        source["Health"] = sourceName.Health;
                        source["DamageModifiers"] = sourceName.DamageModifiers;
                        sourceArray.Add(source);
                    }

                    resourceObject[sourcePair.Key] = sourceArray;
                }

                jsonObject[resourcePair.Key] = resourceObject;
            }

            string jsonFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "resource_database.json");
            File.WriteAllText(jsonFilePath, IndentJson(jsonObject.ToString())); // '2' is for indentation
            LogInfo($"Saved resource database to {jsonFilePath}");
        }

        private List<(string Category, string Name, float Efficiency, float Distance, int Depth)> FindResourceSourcesRecursive(string resource, ItemDrop.ItemData weapon, int depth = 0, int maxDepth = 3)
        {
            var sources = new List<(string, string, float, float, int)>();
            if (!resourceDatabase.ContainsKey(resource))
                return sources;

            GetNearbyResources(Player.m_localPlayer.gameObject);

            foreach (var category in resourceDatabase[resource])
            {
                foreach (var item in category.Value)
                {
                    if (nearbyResourcesDistance.TryGetValue(item.Name, out float distance))
                    {
                        //float distance = Vector3.Distance(Player.m_localPlayer.transform.position, position);

                        float weaponEffectiveness = CalculateWeaponEffectiveness(weapon, item.DamageModifiers);
                        //LogError($"CalculateWeaponEffectiveness for {weapon.m_shared.m_name} {weaponEffectiveness} {resource}");
                        float efficiency = CalculateEfficiency(item.Name, category.Key, item.MinAmount, item.MaxAmount, item.Health, weaponEffectiveness, distance);
                        sources.Add((category.Key, item.Name, efficiency, distance, depth));
                    }

                    // Recursively search for parent resources
                    /*var parentSources = FindResourceSourcesRecursive(item.Name, weapon, depth + 1, maxDepth);
                    sources.AddRange(parentSources);*/
                }
            }

            return sources;
        }

        public static Dictionary<string, List<Resource>> QueryResourceComplete(string resourceName, bool HasWeapon = true)
        {
            if (!resourceDatabase.ContainsKey(resourceName))
            {
                return new Dictionary<string, List<Resource>>(); // Return an empty array if resource is not found
            }

            var results = resourceDatabase[resourceName];
            var resourceList = new Dictionary<string, List<Resource>>();

            // Add all resources to the set without labels
            foreach (var sourceType in HasWeapon ? priorityOrder : priorityOrderUnarmed)
            {
                if (results.ContainsKey(sourceType))
                {
                    resourceList[sourceType] = results[sourceType];
                }
            }

            return resourceList;
        }

        private static List<List<string>> ConvertResourcesToNames(List<List<Resource>> resourceLists)
        {
            return resourceLists.Select(innerList =>
                innerList.Select(resource => resource.Name).ToList()
            ).ToList();
        }

        private static List<string> FlattenListOfLists(List<List<string>> nestedList)
        {
            return nestedList.SelectMany(innerList => innerList).ToList();
        }




        private void PopulateCraftingRequirements()
        {
            var jsonObject = new JsonObject();
            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    var thisJsonObject = new JsonObject();

                    thisJsonObject["name"] = itemDrop.name;
                    thisJsonObject["itemName"] = itemDrop.m_itemData.m_shared.m_name;

                    JsonObject itemDropCustomData = new JsonObject();
                    foreach (var s in itemDrop.m_itemData.m_customData)
                    {
                        itemDropCustomData[s.Key] = s.Value;
                    }
                    if (itemDropCustomData.Count > 0)
                        thisJsonObject["customData"] = itemDropCustomData;

                    if (itemDrop.m_itemData.m_shared.m_description != "")
                    {
                        string description = LocalizationManager.Instance.TryTranslate(itemDrop.m_itemData.m_shared.m_description);

                        // If the description is the same as the key, it means no translation was found
                        if (description != "")
                        {
                            thisJsonObject["description"] = description;
                        }
                    }


                    thisJsonObject["armor"] = itemDrop.m_itemData.m_shared.m_armor;
                    thisJsonObject["maxDurability"] = itemDrop.m_itemData.m_shared.m_maxDurability;
                    thisJsonObject["weight"] = itemDrop.m_itemData.m_shared.m_weight;

                    Recipe recipe = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);
                    if (recipe != null)
                    {
                        craftingRequirements[itemDrop.m_itemData.m_shared.m_name] = recipe.m_resources;
                        JsonArray requirementsArray = new JsonArray();
                        foreach (var req in recipe.m_resources)
                        {
                            JsonObject reqObject = new JsonObject();

                            reqObject["name"] = req.m_resItem.name;
                            reqObject["itemName"] = req.m_resItem.m_itemData.m_shared.m_name;

                            if (req.m_resItem.m_itemData.m_shared.m_description != "")
                            {
                                string description = LocalizationManager.Instance.TryTranslate(req.m_resItem.m_itemData.m_shared.m_description);

                                // If the description is the same as the key, it means no translation was found
                                if (description != "")
                                {
                                    reqObject["description"] = description;
                                }
                            }

                            reqObject["amount"] = req.m_amount;
                            /*reqObject["amountPerLevel"] = req.m_amountPerLevel;
                            reqObject["m_recover"] = req.m_recover;
                            reqObject["m_extraAmountOnlyOneIngredient"] = req.m_extraAmountOnlyOneIngredient;*/

                            requirementsArray.Add(reqObject);
                        }
                        thisJsonObject["m_resources"] = requirementsArray;
                    }

                    jsonObject[itemDrop.m_itemData.m_shared.m_name] = thisJsonObject;
                }
            }

            string json = IndentJson(jsonObject.ToString());

            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "crafting_requirements.json");

            File.WriteAllText(filePath, json);
            LogError($"Crafting requirements exported to {filePath}");
        }

        private void PopulateBuildingRequirements()
        {
            var jsonObject = new JsonObject();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                Piece piece = prefab.GetComponent<Piece>();
                if (piece != null)
                {
                    string pieceName = piece.m_name;
                    buildingRequirements[pieceName] = piece.m_resources;

                    JsonObject thisJsonObject = new JsonObject();
                    thisJsonObject["name"] = piece.name;
                    thisJsonObject["itemName"] = piece.m_name;

                    if (piece.m_description != "")
                    {
                        string description = LocalizationManager.Instance.TryTranslate(piece.m_description);

                        // If the description is the same as the key, it means no translation was found
                        if (description != "")
                        {
                            thisJsonObject["description"] = description;
                        }
                    }

                    thisJsonObject["category"] = piece.m_category.ToString();
                    thisJsonObject["comfort"] = piece.m_comfort;
                    thisJsonObject["groundPiece"] = piece.m_groundPiece;
                    thisJsonObject["allowedInDungeons"] = piece.m_allowedInDungeons;
                    thisJsonObject["spaceRequirement"] = piece.m_spaceRequirement;

                    JsonArray requirementsArray = new JsonArray();
                    foreach (var req in piece.m_resources)
                    {
                        JsonObject reqObject = new JsonObject();
                        reqObject["name"] = req.m_resItem.name;
                        reqObject["itemName"] = req.m_resItem.m_itemData.m_shared.m_name;

                        if (req.m_resItem.m_itemData.m_shared.m_description != "")
                        {
                            string description = LocalizationManager.Instance.TryTranslate(req.m_resItem.m_itemData.m_shared.m_description);

                            // If the description is the same as the key, it means no translation was found
                            if (description != "")
                            {
                                reqObject["description"] = description;
                            }
                        }

                        reqObject["amount"] = req.m_amount;
                        reqObject["amountPerLevel"] = req.m_amountPerLevel;
                        reqObject["m_recover"] = req.m_recover;
                        reqObject["m_extraAmountOnlyOneIngredient"] = req.m_extraAmountOnlyOneIngredient;

                        requirementsArray.Add(reqObject);
                    }
                    thisJsonObject["m_resources"] = requirementsArray;
                    jsonObject[pieceName] = thisJsonObject;
                }
            }

            string json = IndentJson(jsonObject.ToString());

            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "building_requirements.json");

            File.WriteAllText(filePath, json);
            LogError($"Building requirements exported to {filePath}");
        }

        private void PopulateMonsterPrefabs()
        {
            var monsterList = new JsonArray();

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                Character prechar = prefab.GetComponent<Character>();
                Humanoid prehum = prefab.GetComponent<Humanoid>();

                if (prechar != null)
                {
                    JsonObject thisJsonObject = new JsonObject();
                    thisJsonObject["name"] = prechar.name;
                    thisJsonObject["itemName"] = prechar.m_name;
                    monsterList.Add(thisJsonObject);
                }

                if (prehum != null)
                {
                    JsonObject thisJsonObject = new JsonObject();
                    thisJsonObject["name"] = prehum.name;
                    thisJsonObject["itemName"] = prehum.m_name;
                    monsterList.Add(thisJsonObject);
                }
            }

            string json = monsterList.ToString();
            json = IndentJson(json);

            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "monsters.json");

            File.WriteAllText(filePath, json);
            LogError($"Monster prefab list exported to {filePath}");
        }

        private void PopulateAllItems()
        {
            var allItemsList = new JsonArray();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab.HasAnyComponent("ItemDrop"))
                {
                    var jsonObject = new JsonObject();
                    var itemDrop = prefab.GetComponent<ItemDrop>();
                    jsonObject["name"] = prefab.name;
                    jsonObject["shared"] = itemDrop.m_itemData.m_shared.ToString();

                    allItemsList.Add(jsonObject);
                }
            }

            string json = allItemsList.ToString();
            json = IndentJson(json);

            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "all_items_list.json");

            File.WriteAllText(filePath, json);
            LogError($"all_items_list exported to {filePath}");
        }

        private void PopulateAllWeapons()
        {
            var jsonObject = new JsonObject();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab.HasAnyComponent("ItemDrop"))
                {
                    ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                    if (itemDrop && itemDrop.m_itemData.IsWeapon())
                    {
                        var itemDropJson = new JsonObject();
                        itemDropJson["Durability"] = itemDrop.m_itemData.m_durability;
                        itemDropJson["Quality"] = itemDrop.m_itemData.m_quality;
                        itemDropJson["m_aiAttackRange"] = itemDrop.m_itemData.m_shared.m_aiAttackRange;
                        itemDropJson["Weight"] = itemDrop.m_itemData.m_shared.m_weight;
                        itemDropJson["Attack"] = itemDrop.m_itemData.m_shared.m_attack.m_attackAnimation;
                        itemDropJson["SecondaryAttack"] = itemDrop.m_itemData.m_shared.m_secondaryAttack.m_attackAnimation;
                        itemDropJson["DamageModifiers"] = itemDrop.m_itemData.m_shared.m_damages;
                        itemDropJson["DamagesPerLevel"] = itemDrop.m_itemData.m_shared.m_damagesPerLevel;

                        jsonObject[prefab.name] = itemDropJson;
                    }
                }
            }

            string json = jsonObject.ToString();
            json = IndentJson(json);

            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "all_weapons.json");

            File.WriteAllText(filePath, json);
            LogError($"All weapons exported to {filePath}");
        }

        public Piece.Requirement[] GetCraftingRequirements(string itemName)
        {
            if (craftingRequirements.ContainsKey(itemName))
            {
                return craftingRequirements[itemName];
            }
            return null;
        }
    }
}
