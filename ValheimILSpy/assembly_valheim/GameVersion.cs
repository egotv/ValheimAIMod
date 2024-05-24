using System;

public struct GameVersion
{
	public int m_major;

	public int m_minor;

	public int m_patch;

	public GameVersion(int major, int minor, int patch)
	{
		m_major = major;
		m_minor = minor;
		m_patch = patch;
	}

	public static GameVersion ParseGameVersion(string versionString)
	{
		TryParseGameVersion(versionString, out var version);
		return version;
	}

	public static bool TryParseGameVersion(string versionString, out GameVersion version)
	{
		version = default(GameVersion);
		string[] array = versionString.Split('.', StringSplitOptions.None);
		if (array.Length < 2)
		{
			return false;
		}
		if (!TryGetFirstIntFromString(array[0], out var output2) || !TryGetFirstIntFromString(array[1], out var output3))
		{
			return false;
		}
		if (array.Length == 2)
		{
			version = new GameVersion(output2, output3, 0);
			return true;
		}
		int output4;
		if (array[2].StartsWith("rc"))
		{
			if (!TryGetFirstIntFromString(array[2].Substring(2), out output4))
			{
				return false;
			}
			output4 = -output4;
		}
		else if (!TryGetFirstIntFromString(array[2], out output4))
		{
			return false;
		}
		version = new GameVersion(output2, output3, output4);
		return true;
		static bool TryGetFirstIntFromString(string input, out int output)
		{
			output = 0;
			char[] array2 = new char[input.Length];
			int num = 0;
			for (int i = 0; i < input.Length; i++)
			{
				if ((num == 0 && input[i] == '-') || char.IsNumber(input[i]))
				{
					array2[num++] = input[i];
				}
				else if (num > 0)
				{
					break;
				}
			}
			if (num > 0)
			{
				return int.TryParse(new string(array2, 0, num), out output);
			}
			return false;
		}
	}

	public bool Equals(GameVersion other)
	{
		if (m_major == other.m_major && m_minor == other.m_minor)
		{
			return m_patch == other.m_patch;
		}
		return false;
	}

	private static bool IsVersionNewer(GameVersion other, GameVersion reference)
	{
		if (other.m_major > reference.m_major)
		{
			return true;
		}
		if (other.m_major == reference.m_major && other.m_minor > reference.m_minor)
		{
			return true;
		}
		if (other.m_major == reference.m_major && other.m_minor == reference.m_minor)
		{
			if (reference.m_patch >= 0)
			{
				return other.m_patch > reference.m_patch;
			}
			if (other.m_patch >= 0)
			{
				return true;
			}
			return other.m_patch < reference.m_patch;
		}
		return false;
	}

	public bool IsValid()
	{
		return this != default(GameVersion);
	}

	public override string ToString()
	{
		if (!IsValid())
		{
			return "";
		}
		if (m_patch == 0)
		{
			return m_major + "." + m_minor;
		}
		if (m_patch < 0)
		{
			return m_major + "." + m_minor + ".rc" + -m_patch;
		}
		return m_major + "." + m_minor + "." + m_patch;
	}

	public override bool Equals(object other)
	{
		if (other == null)
		{
			return false;
		}
		if (!(other is GameVersion))
		{
			return false;
		}
		return Equals((GameVersion)other);
	}

	public override int GetHashCode()
	{
		return ((313811945 * -1521134295 + m_major.GetHashCode()) * -1521134295 + m_minor.GetHashCode()) * -1521134295 + m_patch.GetHashCode();
	}

	public static bool operator ==(GameVersion lhs, GameVersion rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(GameVersion lhs, GameVersion rhs)
	{
		return !(lhs == rhs);
	}

	public static bool operator >(GameVersion lhs, GameVersion rhs)
	{
		return IsVersionNewer(lhs, rhs);
	}

	public static bool operator <(GameVersion lhs, GameVersion rhs)
	{
		return IsVersionNewer(rhs, lhs);
	}

	public static bool operator >=(GameVersion lhs, GameVersion rhs)
	{
		if (!(lhs == rhs))
		{
			return lhs > rhs;
		}
		return true;
	}

	public static bool operator <=(GameVersion lhs, GameVersion rhs)
	{
		if (!(lhs == rhs))
		{
			return lhs < rhs;
		}
		return true;
	}
}
