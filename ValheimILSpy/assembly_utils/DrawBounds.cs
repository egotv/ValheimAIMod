using UnityEngine;

public class DrawBounds : MonoBehaviour
{
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		MeshFilter[] componentsInChildren = GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter obj in componentsInChildren)
		{
			Gizmos.matrix = obj.transform.localToWorldMatrix;
			Mesh sharedMesh = obj.sharedMesh;
			Gizmos.DrawWireCube(sharedMesh.bounds.center, sharedMesh.bounds.size);
		}
	}
}
