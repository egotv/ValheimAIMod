using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "EmoteMappings", menuName = "Valheim/EmoteMappings", order = 1)]
public class EmoteMappings : ScriptableObject
{
	[SerializeField]
	protected EmoteDataMapping[] _emotes;

	public EmoteDataMapping GetMapping(Emotes emote)
	{
		if (_emotes != null)
		{
			for (int i = 0; i < _emotes.Length; i++)
			{
				if (_emotes[i].Emote == emote)
				{
					return _emotes[i];
				}
			}
		}
		EmoteDataMapping result = default(EmoteDataMapping);
		result.Emote = Emotes.Count;
		return result;
	}
}
