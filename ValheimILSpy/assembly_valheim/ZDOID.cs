using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct ZDOID : IEquatable<ZDOID>, IComparable<ZDOID>
{
	private static readonly long NullUser = 0L;

	private static readonly long UnknownFormerUser = 1L;

	private static readonly ushort UnknownFormerUserKey = 1;

	public static uint m_loadID = 0u;

	private static readonly List<long> m_userIDs = new List<long> { NullUser, UnknownFormerUser };

	public static readonly ZDOID None = new ZDOID(0L, 0u);

	private static ushort m_userIDCount = 2;

	public long UserID => GetUserID(UserKey);

	private ushort UserKey { get; set; }

	public uint ID { get; private set; }

	public static ushort AddUser(long userID)
	{
		int num = m_userIDs.IndexOf(userID);
		if (num >= 0)
		{
			if (userID == 0L)
			{
				return 0;
			}
			return (ushort)num;
		}
		m_userIDs.Add(userID);
		return m_userIDCount++;
	}

	public static long GetUserID(ushort userKey)
	{
		return m_userIDs[userKey];
	}

	public ZDOID(BinaryReader reader)
	{
		UserKey = AddUser(reader.ReadInt64());
		ID = reader.ReadUInt32();
	}

	public ZDOID(long userID, uint id)
	{
		UserKey = AddUser(userID);
		ID = id;
	}

	public void SetID(uint id)
	{
		ID = id;
		UserKey = UnknownFormerUserKey;
	}

	public override string ToString()
	{
		return GetUserID(UserKey) + ":" + ID;
	}

	public static bool operator ==(ZDOID a, ZDOID b)
	{
		if (a.UserKey == b.UserKey)
		{
			return a.ID == b.ID;
		}
		return false;
	}

	public static bool operator !=(ZDOID a, ZDOID b)
	{
		if (a.UserKey == b.UserKey)
		{
			return a.ID != b.ID;
		}
		return true;
	}

	public bool Equals(ZDOID other)
	{
		if (other.UserKey == UserKey)
		{
			return other.ID == ID;
		}
		return false;
	}

	public override bool Equals(object other)
	{
		if (other is ZDOID zDOID)
		{
			return this == zDOID;
		}
		return false;
	}

	public int CompareTo(ZDOID other)
	{
		if (UserKey != other.UserKey)
		{
			if (UserKey >= other.UserKey)
			{
				return 1;
			}
			return -1;
		}
		if (ID < other.ID)
		{
			return -1;
		}
		if (ID <= other.ID)
		{
			return 0;
		}
		return 1;
	}

	public override int GetHashCode()
	{
		return GetUserID(UserKey).GetHashCode() ^ ID.GetHashCode();
	}

	public bool IsNone()
	{
		if (UserKey == 0)
		{
			return ID == 0;
		}
		return false;
	}
}
