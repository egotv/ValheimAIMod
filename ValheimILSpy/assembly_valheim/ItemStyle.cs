using UnityEngine;

public class ItemStyle : MonoBehaviour, IEquipmentVisual
{
	public void Setup(int style)
	{
		GetComponent<Renderer>().material.SetFloat("_Style", style);
	}
}
