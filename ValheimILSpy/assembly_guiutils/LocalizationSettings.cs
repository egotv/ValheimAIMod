using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationSettings", menuName = "Game/LocalizationSettings", order = 1)]
public class LocalizationSettings : ScriptableObject
{
	[Tooltip("Allows to define which CSV files are loaded and in which order, duplicate keys will be overwritten by the last file that contains it.")]
	[SerializeField]
	private List<TextAsset> m_localizations;

	public List<TextAsset> Localizations => m_localizations;
}
