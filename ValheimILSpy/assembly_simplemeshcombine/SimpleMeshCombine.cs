using System.Collections;
using UnityEngine;

[AddComponentMenu("Simple Mesh Combine")]
public class SimpleMeshCombine : MonoBehaviour
{
	public GameObject[] combinedGameOjects;

	public GameObject combined;

	public string meshName = "Combined_Meshes";

	public bool _canGenerateLightmapUV;

	public int vCount;

	public bool generateLightmapUV;

	public float lightmapScale = 1f;

	public GameObject copyTarget;

	public bool destroyOldColliders;

	public bool keepStructure = true;

	public Mesh autoOverwrite;

	public bool setStatic = true;

	public void EnableRenderers(bool e)
	{
		for (int i = 0; i < combinedGameOjects.Length && !(combinedGameOjects[i] == null); i++)
		{
			Renderer component = combinedGameOjects[i].GetComponent<Renderer>();
			if (component != null)
			{
				component.enabled = e;
			}
		}
	}

	public MeshFilter[] FindEnabledMeshes()
	{
		MeshFilter[] array = null;
		int num = 0;
		array = base.transform.GetComponentsInChildren<MeshFilter>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].GetComponent<MeshRenderer>() != null && array[i].GetComponent<MeshRenderer>().enabled)
			{
				num++;
			}
		}
		MeshFilter[] array2 = new MeshFilter[num];
		num = 0;
		for (int j = 0; j < array.Length; j++)
		{
			if (array[j].GetComponent<MeshRenderer>() != null && array[j].GetComponent<MeshRenderer>().enabled)
			{
				array2[num] = array[j];
				num++;
			}
		}
		return array2;
	}

	public void CombineMeshes()
	{
		GameObject gameObject = new GameObject();
		gameObject.name = "_Combined Mesh [" + base.name + "]";
		gameObject.gameObject.AddComponent<MeshFilter>();
		gameObject.gameObject.AddComponent<MeshRenderer>();
		MeshFilter[] array = null;
		array = FindEnabledMeshes();
		ArrayList arrayList = new ArrayList();
		ArrayList arrayList2 = new ArrayList();
		combinedGameOjects = new GameObject[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			combinedGameOjects[i] = array[i].gameObject;
			MeshRenderer component = array[i].GetComponent<MeshRenderer>();
			array[i].transform.gameObject.GetComponent<Renderer>().enabled = false;
			if (array[i].sharedMesh == null)
			{
				break;
			}
			for (int j = 0; j < array[i].sharedMesh.subMeshCount; j++)
			{
				if (component == null)
				{
					break;
				}
				if (j < component.sharedMaterials.Length && j < array[i].sharedMesh.subMeshCount)
				{
					int num = Contains(arrayList, component.sharedMaterials[j]);
					if (num == -1)
					{
						arrayList.Add(component.sharedMaterials[j]);
						num = arrayList.Count - 1;
					}
					arrayList2.Add(new ArrayList());
					CombineInstance combineInstance = default(CombineInstance);
					combineInstance.transform = component.transform.localToWorldMatrix;
					combineInstance.mesh = array[i].sharedMesh;
					combineInstance.subMeshIndex = j;
					(arrayList2[num] as ArrayList).Add(combineInstance);
				}
			}
		}
		Mesh[] array2 = new Mesh[arrayList.Count];
		CombineInstance[] array3 = new CombineInstance[arrayList.Count];
		for (int k = 0; k < arrayList.Count; k++)
		{
			CombineInstance[] combine = (arrayList2[k] as ArrayList).ToArray(typeof(CombineInstance)) as CombineInstance[];
			array2[k] = new Mesh();
			array2[k].CombineMeshes(combine, mergeSubMeshes: true, useMatrices: true);
			array3[k] = default(CombineInstance);
			array3[k].mesh = array2[k];
			array3[k].subMeshIndex = 0;
		}
		Mesh mesh2 = (gameObject.GetComponent<MeshFilter>().sharedMesh = new Mesh());
		Mesh mesh3 = mesh2;
		mesh3.Clear();
		mesh3.CombineMeshes(array3, mergeSubMeshes: false, useMatrices: false);
		gameObject.GetComponent<MeshFilter>().sharedMesh = mesh3;
		Mesh[] array4 = array2;
		foreach (Mesh obj in array4)
		{
			obj.Clear();
			Object.DestroyImmediate(obj);
		}
		MeshRenderer meshRenderer = gameObject.GetComponent<MeshFilter>().GetComponent<MeshRenderer>();
		if (meshRenderer == null)
		{
			meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
		}
		Material[] materials = arrayList.ToArray(typeof(Material)) as Material[];
		meshRenderer.materials = materials;
		combined = gameObject.gameObject;
		EnableRenderers(e: false);
		gameObject.transform.parent = base.transform;
		gameObject.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
		vCount = gameObject.GetComponent<MeshFilter>().sharedMesh.vertexCount;
		if (vCount > 65536)
		{
			Debug.LogWarning("Vertex Count: " + vCount + "- Vertex Count too high, please divide mesh combine into more groups. Max 65536 for each mesh");
			_canGenerateLightmapUV = false;
		}
		else
		{
			_canGenerateLightmapUV = true;
		}
		if (setStatic)
		{
			combined.isStatic = true;
		}
	}

	public int Contains(ArrayList l, Material n)
	{
		for (int i = 0; i < l.Count; i++)
		{
			if (l[i] as Material == n)
			{
				return i;
			}
		}
		return -1;
	}
}
