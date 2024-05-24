using System.Collections.Generic;
using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "EmoteGroupConfig", menuName = "Valheim/EmoteGroupConfig", order = 1)]
public class EmoteGroupConfig : ScriptableObject, IRadialConfig
{
	[SerializeField]
	protected EmoteElement m_emotePrefab;

	[SerializeField]
	protected EmoteMappings m_emoteMappings;

	[SerializeField]
	protected Sprite m_icon;

	public string LocalizedName => Localization.instance.Localize("$radial_emotes");

	public Sprite Sprite => m_icon;

	public void SetRadial(DynamicRadialMenu radial, int page)
	{
		radial.SetClose(useBackButtonToClose: false);
		List<RadialMenuElement> list = new List<RadialMenuElement>();
		for (int i = 0; i < 20; i++)
		{
			EmoteElement emoteElement = Object.Instantiate(m_emotePrefab);
			EmoteDataMapping mapping = m_emoteMappings.GetMapping((Emotes)i);
			emoteElement.Init(mapping);
			list.Add(emoteElement);
		}
		radial.MaxElementsPerPage = 12;
		radial.SetElements(list.ToArray(), page, addBackButton: true, fillUp: true);
	}
}
