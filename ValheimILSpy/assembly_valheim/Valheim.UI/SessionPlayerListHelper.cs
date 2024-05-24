using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Valheim.UI;

public static class SessionPlayerListHelper
{
	public static IEnumerator SetSpriteFromUri(this Image image, string uri)
	{
		UnityWebRequest www = UnityWebRequestTexture.GetTexture(uri);
		yield return www.SendWebRequest();
		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.Log(www.error);
			yield break;
		}
		Texture2D content = DownloadHandlerTexture.GetContent(www);
		image.sprite = Sprite.Create(content, new Rect(0f, 0f, content.width, content.height), new Vector2(0.5f, 0.5f));
		image.transform.localScale = new Vector3(1f, 1f, 1f);
	}

	public static void SetSpriteFromSteamImageId(this Image image, int imageId)
	{
		uint pnWidth;
		uint pnHeight;
		if (imageId <= 0)
		{
			image.SetTransparent();
		}
		else if (SteamUtils.GetImageSize(imageId, out pnWidth, out pnHeight))
		{
			uint num = pnWidth * pnHeight * 4;
			byte[] array = new byte[num];
			Texture2D texture2D = new Texture2D((int)pnWidth, (int)pnHeight, TextureFormat.RGBA32, mipChain: false, linear: true);
			if (SteamUtils.GetImageRGBA(imageId, array, (int)num))
			{
				texture2D.LoadRawTextureData(array);
				texture2D.FlipInYDirection();
				image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
			}
		}
	}

	private static void SetTransparent(this Image image)
	{
		Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
		texture2D.SetPixels(new Color[1]
		{
			new Color(0f, 0f, 0f, 0f)
		});
		image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
	}

	private static void FlipInYDirection(this Texture2D texture)
	{
		Color[] pixels = texture.GetPixels();
		Color[] array = new Color[pixels.Length];
		int num = 0;
		for (int num2 = texture.height - 1; num2 >= 0; num2--)
		{
			for (int i = 0; i < texture.width; i++)
			{
				array[num] = pixels[num2 * texture.height + i];
				num++;
			}
		}
		texture.SetPixels(array);
		texture.Apply();
	}

	public static bool TryFindPlayerByZDOID(this List<ZNet.PlayerInfo> players, ZDOID playerID, out ZNet.PlayerInfo? playerInfo)
	{
		playerInfo = null;
		for (int i = 0; i < players.Count; i++)
		{
			ZNet.PlayerInfo value = players[i];
			if (value.m_characterID == playerID)
			{
				playerInfo = value;
				return true;
			}
		}
		return false;
	}

	public static bool TryFindPlayerByPlayername(this List<ZNet.PlayerInfo> players, string name, out ZNet.PlayerInfo? playerInfo)
	{
		playerInfo = null;
		for (int i = 0; i < players.Count; i++)
		{
			ZNet.PlayerInfo value = players[i];
			if (value.m_name == name)
			{
				playerInfo = value;
				return true;
			}
		}
		return false;
	}

	public static bool IsBanned(string characterName)
	{
		return ZNet.instance.Banned.Contains(characterName);
	}
}
