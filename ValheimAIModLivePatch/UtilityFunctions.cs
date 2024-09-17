using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimAIModLoader
{
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        private static T SphereSearchForGameObjectWithComponent<T>(Vector3 p, float radius) where T : Component
        {
            int layerMask = ~0; // This will check all layers
            Collider[] colliders = Physics.OverlapSphere(p, radius, layerMask, QueryTriggerInteraction.Collide);
            List<T> res = new List<T>();

            foreach (Collider collider in colliders)
            {
                T character = GetComponentInParentOrSelf<T>(collider.gameObject);

                if (character != null)
                {
                    res.Add(character);
                }
            }

            if (res.Count > 0)
                return res.OrderBy(go => go.transform.position.DistanceTo(p)).First();

            return null;
        }

        private static List<T> SphereSearchForGameObjectsWithComponent<T>(Vector3 p, float radius) where T : Component
        {
            int layerMask = ~0; // This will check all layers
            Collider[] colliders = Physics.OverlapSphere(p, radius, layerMask, QueryTriggerInteraction.Collide);
            List<T> res = new List<T>();

            foreach (Collider collider in colliders)
            {
                T character = GetComponentInParentOrSelf<T>(collider.gameObject);

                if (character != null)
                {
                    res.Add(character);
                }
            }

            return res;
        }

        private static void SphereSearchForGameObjects(Vector3 p, float radius)
        {
            int layerMask = ~0; // This will check all layers

            Collider[] colliders = Physics.OverlapSphere(p, radius, layerMask, QueryTriggerInteraction.Collide);

            foreach (Collider collider in colliders)
            {
                GameObject obj = collider.gameObject;

                // Check for various component types
                Character character = GetComponentInParentOrSelf<Character>(obj);
                ItemDrop itemDrop = GetComponentInParentOrSelf<ItemDrop>(obj);
                TreeBase tree = GetComponentInParentOrSelf<TreeBase>(obj);
                Pickable pickable = GetComponentInParentOrSelf<Pickable>(obj);

                if (character != null)
                {
                    if (character.IsPlayer())
                    {
                        //Debug.Log($"Player detected: {obj.name}");
                    }
                    else
                    {
                        Debug.Log($"Character detected: {character.name} (Type: {character.GetHoverName()})");
                    }
                }
                else if (itemDrop != null)
                {
                    Debug.Log($"Item drop detected: {itemDrop.name} (Item: {itemDrop.m_itemData.m_dropPrefab.name})");
                }
                else if (tree != null)
                {
                    Debug.Log($"Tree detected: {tree.name}");
                }
                else if (pickable != null)
                {
                    Debug.Log($"Pickable object detected: {pickable.name}");
                }
                else
                {
                    //Debug.Log($"Other object detected: {obj.name}");
                }
            }
        }

        private static GameObject PerformRaycast(Character player)
        {
            Vector3 rayStart = player.GetEyePoint();
            Vector3 rayDirection = player.GetLookDir();
            float rayDistance = 50f; // Adjust this value to change the raycast distance

            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance))
            {
                GameObject go = FindTopLevelObject(hit.collider.gameObject);
                Debug.Log($"raycast hit {go.name}");
                return go;
            }
            else
            {
                player.Message(MessageHud.MessageType.TopLeft, "Raycast didn't hit anything", 0, null);
            }

            return null;
        }



        public static bool IsStringEqual(string a, string b, bool bCleanKey)
        {
            if (bCleanKey)
                return CleanKey(a).ToLower().Equals(CleanKey(b).ToLower());

            return a.ToLower().Equals(b.ToLower());
        }

        public static bool IsStringStartingWith(string a, string b, bool bCleanKey)
        {
            if (bCleanKey)
                return CleanKey(a).ToLower().StartsWith(CleanKey(b).ToLower());

            return a.ToLower().StartsWith(b.ToLower());
        }

        public static bool DoesStringContains(string a, string b, bool bCleanKey)
        {
            if (bCleanKey)
                return CleanKey(a).ToLower().Contains(CleanKey(b).ToLower());

            return a.ToLower().StartsWith(b.ToLower());
        }

        public static string CleanKey(string key)
        {
            int lastParenIndex = key.LastIndexOf('(');
            if (lastParenIndex != -1)
            {
                key = key.Substring(0, lastParenIndex);
            }

            key = key.Trim();

            return key;
        }

        private static string RemoveCustomText(string text)
        {
            string[] lines = text.Split('\n');
            System.Collections.Generic.List<string> newLines = new System.Collections.Generic.List<string>();

            foreach (string line in lines)
            {
                if (!line.Contains("<color=orange>") && !line.Contains("<color=purple>"))
                {
                    newLines.Add(line);
                }
            }

            return string.Join("\n", newLines);
        }

        private static float DistanceBetween(GameObject a, GameObject b)
        {
            return a.transform.position.DistanceTo(b.transform.position);
        }

        private static float CalculateXYDistance(Vector3 point1, Vector3 point2)
        {
            float deltaX = point2.x - point1.x;
            float deltaY = point2.y - point1.y;
            return Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        private static GameObject GetClosestFromArray(GameObject[] gos, Vector3 position)
        {
            return gos.OrderBy(go => Vector3.Distance(position, go.transform.position)).FirstOrDefault();
        }

        public void SetTimer(float duration, Action onComplete)
        {
            StartCoroutine(TimerCoroutine(duration, onComplete));
        }

        private System.Collections.IEnumerator TimerCoroutine(float duration, Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }



        private static GameObject FindTopLevelObject(GameObject obj)
        {
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
            }
            return obj;
        }

        public static bool HasAnyChildComponent(GameObject gameObject, List<Type> componentTypes)
        {
            Component[] objectComponents = gameObject.GetComponents<Component>();

            foreach (Component objectComponent in objectComponents)
            {
                Type objectComponentType = objectComponent.GetType();

                foreach (Type componentType in componentTypes)
                {
                    if (componentType.IsAssignableFrom(objectComponentType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static Component GetParentFromChildComponent(GameObject gameObject, Type parentType)
        {
            Component[] objectComponents = gameObject.GetComponents<Component>();

            foreach (Component objectComponent in objectComponents)
            {
                Type objectComponentType = objectComponent.GetType();

                if (parentType.IsAssignableFrom(objectComponentType))
                {
                    return objectComponent;
                }
            }

            return null;
        }

        static T GetComponentInParentOrSelf<T>(GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.GetComponentInParent<T>();
            }
            return component;
        }

        // helper function to copy component values (not tested/not being used)
        T CopyComponent<T>(GameObject source, GameObject destination) where T : Component
        {
            T sourceComp = source.GetComponent<T>();
            if (sourceComp != null)
            {
                //T newComp = destination.AddComponent<T>();
                T newComp = destination.GetComponent<T>();
                System.Reflection.FieldInfo[] fields = typeof(T).GetFields();
                foreach (System.Reflection.FieldInfo field in fields)
                {
                    field.SetValue(newComp, field.GetValue(sourceComp));
                }
                return newComp;
            }
            return null;
        }

        private static T GetComponentFromGameObject<T>(GameObject go) where T : Component
        {
            if (go == null) return null;

            Component comp = GetParentFromChildComponent(go, typeof(T));

            if (comp is T typedComp)
            {
                return typedComp;
            }

            return null;
        }

        private static Character GetCharacterFromGameObject(GameObject go)
        {
            if (go == null) return null;

            Component comp = GetParentFromChildComponent(go, typeof(Character));

            if (comp && comp is Character)
            {
                return comp as Character;
            }

            return null;
        }



        public static string IndentJson(string json)
        {
            int indentLevel = 0;
            bool inQuotes = false;
            var sb = new StringBuilder();

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!inQuotes)
                        {
                            sb.AppendLine();
                            Indent(++indentLevel, sb);
                        }
                        break;
                    case '}':
                    case ']':
                        if (!inQuotes)
                        {
                            sb.AppendLine();
                            Indent(--indentLevel, sb);
                        }
                        sb.Append(ch);
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!inQuotes)
                        {
                            sb.AppendLine();
                            Indent(indentLevel, sb);
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!inQuotes)
                            sb.Append(" ");
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && json[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            inQuotes = !inQuotes;
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }

        private static void Indent(int count, StringBuilder sb)
        {
            for (int i = 0; i < count; i++)
                sb.Append("    ");
        }

        private static readonly byte[] Key = new byte[32]
        {
            23, 124, 67, 88, 190, 12, 45, 91,
            255, 7, 89, 45, 168, 42, 109, 187,
            23, 100, 76, 217, 154, 200, 43, 79,
            19, 176, 62, 9, 201, 33, 95, 128
        };
        private static readonly byte[] IV = new byte[16]
        {
        88, 145, 23, 200, 56, 178, 12, 90,
        167, 34, 78, 191, 78, 23, 12, 78
        };
        public static string Decrypt(string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var msDecrypt = new System.IO.MemoryStream(Convert.FromBase64String(cipherText)))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}
