using System;
using System.IO;
using Steamworks;

public class FileReader
{
	public BinaryReader m_binary;

	public StreamReader m_stream;

	private string m_path;

	private MemoryStream m_mem;

	private FileStream m_file;

	public FileHelpers.FileHelperType m_type { get; private set; }

	public FileHelpers.FileSource m_fileSource { get; private set; }

	public FileReader(string path, FileHelpers.FileSource fileSource, FileHelpers.FileHelperType type = FileHelpers.FileHelperType.Binary)
	{
		m_path = path;
		m_type = type;
		m_fileSource = fileSource;
		if (m_fileSource == FileHelpers.FileSource.Cloud)
		{
			if (!SteamRemoteStorage.FileExists(path))
			{
				throw new FileNotFoundException();
			}
			int fileSize = SteamRemoteStorage.GetFileSize(path);
			byte[] array = new byte[fileSize];
			if (SteamRemoteStorage.FileRead(path, array, fileSize) == 0)
			{
				throw new Exception("Steam Cloud file missing, likely removed manually");
			}
			m_mem = new MemoryStream(array);
			if (m_type == FileHelpers.FileHelperType.Binary)
			{
				m_binary = new BinaryReader(m_mem);
				return;
			}
			if (m_type == FileHelpers.FileHelperType.Stream)
			{
				m_stream = new StreamReader(m_mem);
				return;
			}
			throw new NotImplementedException();
		}
		m_file = File.OpenRead(m_path);
		if (m_type == FileHelpers.FileHelperType.Binary)
		{
			m_binary = new BinaryReader(m_file);
			return;
		}
		if (m_type == FileHelpers.FileHelperType.Stream)
		{
			m_stream = new StreamReader(m_file);
			return;
		}
		throw new NotImplementedException();
	}

	public void Dispose()
	{
		if (m_fileSource == FileHelpers.FileSource.Cloud)
		{
			byte[] array = m_mem.ToArray();
			SteamRemoteStorage.FileRead(m_path, array, array.Length);
		}
		m_binary?.Dispose();
		m_stream?.Dispose();
	}

	public static explicit operator BinaryReader(FileReader w)
	{
		return w.m_binary;
	}

	public static explicit operator StreamReader(FileReader w)
	{
		return w.m_stream;
	}
}
