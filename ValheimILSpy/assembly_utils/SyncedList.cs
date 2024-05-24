using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SyncedList
{
	private const float m_loadInterval = 10f;

	private float m_lastLoadCheckTime;

	private List<string> m_comments = new List<string>();

	private List<string> m_list = new List<string>();

	private string m_fileName;

	private DateTime m_lastLoadDate = DateTime.MinValue;

	public SyncedList(string fileName, string defaultFileComment)
	{
		m_fileName = fileName;
		if (!File.Exists(m_fileName))
		{
			m_comments.Add("// " + defaultFileComment);
			Save();
		}
		else
		{
			Load();
		}
	}

	public List<string> GetList()
	{
		CheckLoad();
		return m_list;
	}

	public int Count()
	{
		CheckLoad();
		return m_list.Count;
	}

	public bool Contains(string s)
	{
		CheckLoad();
		return m_list.Contains(s);
	}

	public void Add(string s)
	{
		Load();
		if (!m_list.Contains(s))
		{
			m_list.Add(s);
			Save();
		}
	}

	public void Remove(string s)
	{
		Load();
		if (m_list.Remove(s))
		{
			Save();
		}
	}

	private void Save()
	{
		using (StreamWriter streamWriter = new StreamWriter(m_fileName))
		{
			foreach (string comment in m_comments)
			{
				streamWriter.WriteLine(comment);
			}
			foreach (string item in m_list)
			{
				streamWriter.WriteLine(item);
			}
		}
		m_lastLoadDate = File.GetLastWriteTime(m_fileName);
	}

	private void CheckLoad()
	{
		if (Time.realtimeSinceStartup - m_lastLoadCheckTime > 10f)
		{
			Load();
			m_lastLoadCheckTime = Time.realtimeSinceStartup;
		}
	}

	private void Load()
	{
		try
		{
			DateTime lastWriteTime = File.GetLastWriteTime(m_fileName);
			if (lastWriteTime <= m_lastLoadDate)
			{
				return;
			}
			m_lastLoadDate = lastWriteTime;
			m_comments.Clear();
			m_list.Clear();
			using StreamReader streamReader = new StreamReader(m_fileName);
			string text;
			while ((text = streamReader.ReadLine()) != null)
			{
				if (text.Length > 0)
				{
					if (text.StartsWith("//"))
					{
						m_comments.Add(text);
					}
					else
					{
						m_list.Add(text);
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
